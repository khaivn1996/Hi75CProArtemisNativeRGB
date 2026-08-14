using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RGB.NET.Core;
using Artemis.Plugins.Devices.LeobogHi75CPro.Mapping;
using Artemis.Plugins.Devices.LeobogHi75CPro.Protocol;
using Artemis.Plugins.Devices.LeobogHi75CPro.Transport;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Devices;

internal sealed class Hi75CProUpdateQueue : UpdateQueue
{
    private const double RgbNetUpdateIntervalSeconds = 0.05;
    private const int SendIntervalMs = 50;
    private const int ReconnectIntervalMs = 1000;

    // P1 production logging
    private static readonly bool EnableVerboseDiagnostics = false;

    // Khoảng 10 phút ở 20 FPS.
    // Chỉ dùng khi EnableVerboseDiagnostics = true.
    private const int DiagnosticFrameLogInterval = 12_000;

    private static readonly string LogPath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "Artemis",
            "Plugins",
            "Artemis.Plugins.Devices.LeobogHi75CPro",
            "hi75c-phase-g.log");

    private readonly DeviceUpdateTrigger _trigger;

    private Hi75CProHidTransport _transport = new();
    //
    // Frame Artemis is currently asking us to display.
    //
    private readonly byte[] _frame =
        Hi75CProProtocol.CreateRgbFrame();

    //
    // Snapshot used by the HID worker.
    // This prevents SetFeature from holding _frameLock
    // for ~12 ms while RGB.NET wants to update colors.
    //
    private readonly byte[] _sendFrame =
        Hi75CProProtocol.CreateRgbFrame();

    private readonly object _frameLock = new();
    private readonly object _ioLock = new();

    private readonly CancellationTokenSource _sendLoopCts =
        new();

    private readonly Stopwatch _clock =
        Stopwatch.StartNew();

    private Task? _sendLoopTask;

    private bool _started;
    private bool _ready;
    private bool _hasFrame;
    private bool _stopping;

    private bool _reconnectPending;
    private bool _logReconnectResume;
    private int _reconnectAttempt;
    private long _nextReconnectAtMs;

    private long _frameVersion;
    private long _lastSentVersion = -1;
    private long _frameChangedAtMs;

    private long _framesSent;
    private long _lastSendAtMs = -1;

    public Hi75CProUpdateQueue()
        : this(CreateTrigger())
    {
    }

    private Hi75CProUpdateQueue(
        DeviceUpdateTrigger trigger)
        : base(trigger)
    {
        _trigger = trigger;
    }

    private static DeviceUpdateTrigger CreateTrigger()
    {
        //
        // IMPORTANT:
        //
        // RGB.NET 3.1.0 ultimately passes:
        //
        //     UpdateFrequency * 1000
        //
        // to TimerHelper.Execute().
        //
        // Therefore 0.05 means approximately 50 ms
        // between RGB.NET UpdateQueue flushes.
        //
        // Do NOT use 20.0 here. That results in
        // approximately 20 seconds between updates.
        //
        return new DeviceUpdateTrigger(
            RgbNetUpdateIntervalSeconds)
        {
            MaxUpdateRate =
                RgbNetUpdateIntervalSeconds
        };
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;

        WriteLog(
            "Starting RGB.NET update trigger.");

        //
        // Start our independent physical HID stream.
        //
        _sendLoopTask =
            Task.Run(
                () => SendLoopAsync(
                    _sendLoopCts.Token));

        _trigger.Start();
    }

    protected override void OnStartup(
        object? sender,
        CustomUpdateData customData)
    {
        base.OnStartup(
            sender,
            customData);

        if (_stopping)
            return;

        lock (_ioLock)
        {
            if (_stopping)
                return;

            using TextWriter log =
                CreateLogWriter();

            log.WriteLine();
            log.WriteLine(
                "============================================================");

            log.WriteLine(
                "LEOBOG Hi75C Pro - PHASE G");

            log.WriteLine(
                "Dedicated 20 FPS HID stream");

            log.WriteLine(
                "============================================================");

            log.WriteLine(
                $"Timestamp : " +
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            if (!_transport.TryOpen(log))
            {
                log.WriteLine(
                    "[Hi75C/G] RESULT: HID_OPEN_FAILED");

                return;
            }

            byte? modelId =
                _transport.TryReadModelId(log);

            if (modelId !=
                Hi75CProConstants.ModelId)
            {
                log.WriteLine(
                    $"[Hi75C/G] MODEL GATE FAILED. " +
                    $"Expected " +
                    $"0x{Hi75CProConstants.ModelId:X2}, " +
                    $"got " +
                    $"{(modelId.HasValue
                        ? $"0x{modelId.Value:X2}"
                        : "NULL")}.");

                _transport.Dispose();

                return;
            }

            _ready = true;

            log.WriteLine(
                "[Hi75C/G] Model gate PASS: " +
                "0xA3 (163)");

            log.WriteLine(
                "[Hi75C/G] RESULT: " +
                "PHASE_G_TRANSPORT_READY");
        }
    }

    //
    // RGB.NET calls this whenever Artemis has fresh
    // LED colors for the device.
    //
    // IMPORTANT:
    // Nothing is sent to HID from this method.
    // We only replace the latest desired frame.
    //
    protected override bool Update(
        ReadOnlySpan<(object key, Color color)> dataSet)
    {
        if (_stopping)
            return true;

        int valid = 0;
        int changed = 0;
        int invalid = 0;

        lock (_frameLock)
        {
            for (int i = 0;
                 i < dataSet.Length;
                 i++)
            {
                (object key, Color color) =
                    dataSet[i];

                if (key is not int rawLedIndex)
                {
                    invalid++;
                    continue;
                }

                if (rawLedIndex < 0 ||
                    rawLedIndex >
                    Hi75CProConstants.MaxRawLedIndex)
                {
                    invalid++;
                    continue;
                }

                valid++;

                float alpha =
                    Math.Clamp(
                        color.A,
                        0f,
                        1f);

                byte red =
                    ToByte(
                        color.R * alpha);

                byte green =
                    ToByte(
                        color.G * alpha);

                byte blue =
                    ToByte(
                        color.B * alpha);

                int offset =
                    Hi75CProConstants.RgbDataOffset +
                    rawLedIndex * 3;

                if (_frame[offset] != red ||
                    _frame[offset + 1] != green ||
                    _frame[offset + 2] != blue)
                {
                    changed++;
                }

                Hi75CProProtocol.SetRawLedColor(
                    _frame,
                    rawLedIndex,
                    red,
                    green,
                    blue);
            }

            if (valid > 0)
            {
                _hasFrame = true;

                if (changed > 0)
                {
                    _frameVersion++;
                    _frameChangedAtMs =
                        _clock.ElapsedMilliseconds;

                    if (EnableVerboseDiagnostics)
                    {
                        WriteFrameDiagnosticLocked(
                            $"FRAME_CHANGE " +
                            $"version={_frameVersion}, " +
                            $"dataSet={dataSet.Length}, " +
                            $"valid={valid}, " +
                            $"changed={changed}, " +
                            $"invalid={invalid}");
                    }
                }
            }
        }

        return true;
    }


    private void HandleTransportFailure()
    {
        lock (_ioLock)
        {
            if (_stopping)
                return;

            _ready = false;

            ResetTransportLocked();

            _reconnectPending = true;
            _reconnectAttempt = 0;

            _nextReconnectAtMs =
                _clock.ElapsedMilliseconds +
                ReconnectIntervalMs;
        }

        WriteLog(
            "RGB SetFeature failed. " +
            "HID transport lost; automatic reconnect started.");
    }

    private bool TryReconnectIfDue()
    {
        if (_stopping)
            return false;

        if (_ready)
            return true;

        if (!_reconnectPending)
            return false;

        long now =
            _clock.ElapsedMilliseconds;

        if (now < _nextReconnectAtMs)
            return false;

        lock (_ioLock)
        {
            if (_stopping)
                return false;

            if (_ready)
                return true;

            if (!_reconnectPending)
                return false;

            now =
                _clock.ElapsedMilliseconds;

            if (now < _nextReconnectAtMs)
                return false;

            _nextReconnectAtMs =
                now + ReconnectIntervalMs;

            _reconnectAttempt++;

            int attempt =
                _reconnectAttempt;

            //
            // Avoid filling the production log while the
            // keyboard remains unplugged.
            //
            bool detailedLog =
                attempt == 1 ||
                attempt % 10 == 0;

            TextWriter log =
                detailedLog
                    ? CreateLogWriter()
                    : TextWriter.Null;

            try
            {
                if (detailedLog)
                {
                    log.WriteLine(
                        $"[Hi75C/G] RECONNECT attempt " +
                        $"#{attempt}");
                }

                //
                // Always use a fresh HidSharp transport.
                // Never reuse the dead USB handle.
                //
                ResetTransportLocked();

                if (!_transport.TryOpen(log))
                {
                    if (attempt % 10 == 0)
                    {
                        WriteLog(
                            $"Reconnect still waiting for " +
                            $"Hi75C Pro. attempt={attempt}");
                    }

                    ResetTransportLocked();

                    return false;
                }

                byte? modelId =
                    _transport.TryReadModelId(log);

                if (!modelId.HasValue)
                {
                    if (detailedLog)
                    {
                        log.WriteLine(
                            "[Hi75C/G] RECONNECT model " +
                            "query returned NULL.");
                    }

                    ResetTransportLocked();

                    return false;
                }

                //
                // Safety gate:
                // never send RGB to another model.
                //
                if (modelId.Value !=
                    Hi75CProConstants.ModelId)
                {
                    WriteLog(
                        $"Reconnect aborted: model gate " +
                        $"expected " +
                        $"0x{Hi75CProConstants.ModelId:X2}, " +
                        $"got 0x{modelId.Value:X2}.");

                    ResetTransportLocked();

                    _reconnectPending = false;

                    return false;
                }

                _ready = true;
                _reconnectPending = false;

                _lastSendAtMs = -1;
                _lastSentVersion = -1;

                _logReconnectResume = true;

                WriteLog(
                    $"HID reconnect SUCCESS after " +
                    $"{attempt} attempt(s). " +
                    $"Model gate PASS: 0xA3 (163).");

                return true;
            }
            catch (Exception ex)
            {
                ResetTransportLocked();

                if (detailedLog)
                {
                    WriteLog(
                        $"Reconnect attempt #{attempt} " +
                        $"failed: " +
                        $"{ex.GetType().Name}: " +
                        $"{ex.Message}");
                }

                return false;
            }
            finally
            {
                if (!ReferenceEquals(
                        log,
                        TextWriter.Null))
                {
                    try
                    {
                        log.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }

    private void ResetTransportLocked()
    {
        try
        {
            _transport.Dispose();
        }
        catch
        {
        }

        _transport =
            new Hi75CProHidTransport();
    }

    //
    // This is the Hi75C-specific physical heartbeat.
    //
    // It runs independently from RGB.NET's update trigger.
    //
    private async Task SendLoopAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using PeriodicTimer timer =
                new(
                    TimeSpan.FromMilliseconds(
                        SendIntervalMs));

            while (await timer.WaitForNextTickAsync(
                       cancellationToken))
            {
                if (_stopping)
                {
                    continue;
                }

                if (!_ready)
                {
                    if (!TryReconnectIfDue())
                    {
                        continue;
                    }
                }

                long version;
                long changedAt;

                lock (_frameLock)
                {
                    if (!_hasFrame)
                        continue;

                    Buffer.BlockCopy(
                        _frame,
                        0,
                        _sendFrame,
                        0,
                        _frame.Length);

                    version =
                        _frameVersion;

                    changedAt =
                        _frameChangedAtMs;
                }

                long sendStart =
                    _clock.ElapsedMilliseconds;

                bool success;

                lock (_ioLock)
                {
                    if (_stopping ||
                        !_ready)
                    {
                        continue;
                    }

                    success =
                        _transport.TrySendRgbFrame(
                            _sendFrame,
                            TextWriter.Null,
                            verbose: false);
                }

                long sendEnd =
                    _clock.ElapsedMilliseconds;

                if (!success)
                {
                    HandleTransportFailure();
                    continue;
                }

                _framesSent++;

                if (_logReconnectResume)
                {
                    _logReconnectResume = false;

                    WriteLog(
                        "RGB stream resumed successfully " +
                        "using latest Artemis frame.");
                }

                long interval =
                    _lastSendAtMs < 0
                        ? -1
                        : sendStart -
                          _lastSendAtMs;

                _lastSendAtMs =
                    sendStart;

                //
                // Most useful diagnostic:
                // how quickly a newly-rendered Artemis
                // frame reaches physical HID.
                //
                if (version != _lastSentVersion)
                {
                    if (EnableVerboseDiagnostics)
                    {
                        long latency =
                            changedAt <= 0
                                ? -1
                                : sendStart - changedAt;

                        WriteLog(
                            $"APPLY version={version}, " +
                            $"latency={latency}ms, " +
                            $"SetFeature={sendEnd - sendStart}ms");
                    }

                    _lastSentVersion = version;
                }

                if (_framesSent == 1)
                {
                    WriteLog(
                        "First Artemis RGB frame " +
                        "sent successfully.");
                }
                else if (EnableVerboseDiagnostics && _framesSent % DiagnosticFrameLogInterval == 0)
                {
                    WriteLog(
                        $"Frames sent: {_framesSent}, " +
                        $"lastInterval={interval}ms");
                }
            }
        }
        catch (OperationCanceledException)
        {
            //
            // Normal shutdown.
            //
        }
        catch (Exception ex)
        {
            WriteLog(
                $"SendLoop exception: " +
                $"{ex.GetType().FullName}: " +
                $"{ex.Message}");
        }
    }

    private void WriteFrameDiagnosticLocked(
        string reason)
    {
        int mapped = 0;
        int nonBlack = 0;

        bool first = true;
        bool solid = true;

        byte firstR = 0;
        byte firstG = 0;
        byte firstB = 0;

        foreach (Hi75CProKeyDefinition key
                 in Hi75CProLedMap.Keys)
        {
            int offset =
                Hi75CProConstants.RgbDataOffset +
                key.RawLedIndex * 3;

            byte r =
                _frame[offset];

            byte g =
                _frame[offset + 1];

            byte b =
                _frame[offset + 2];

            mapped++;

            if (r != 0 ||
                g != 0 ||
                b != 0)
            {
                nonBlack++;
            }

            if (first)
            {
                firstR = r;
                firstG = g;
                firstB = b;

                first = false;
            }
            else if (r != firstR ||
                     g != firstG ||
                     b != firstB)
            {
                solid = false;
            }
        }

        WriteLog(
            $"{reason} | " +
            $"mapped={mapped}/80, " +
            $"nonBlack={nonBlack}/80, " +
            $"solid=" +
            $"{(solid
                ? $"YES #{firstR:X2}" +
                  $"{firstG:X2}" +
                  $"{firstB:X2}"
                : "NO")}");
    }

    private static byte ToByte(
        float component)
    {
        component =
            Math.Clamp(
                component,
                0f,
                1f);

        return (byte)Math.Clamp(
            (int)MathF.Round(
                component * 255f),
            0,
            255);
    }

    public override void Dispose()
    {
        if (_stopping)
            return;

        _stopping = true;

        WriteLog(
            $"Stopping Phase G queue. " +
            $"Frames sent: {_framesSent}");

        //
        // Stop our physical stream first.
        //
        try
        {
            _sendLoopCts.Cancel();
        }
        catch
        {
        }

        try
        {
            _sendLoopTask?.Wait(
                TimeSpan.FromSeconds(2));
        }
        catch
        {
        }

        try
        {
            base.Dispose();
        }
        catch
        {
        }

        try
        {
            _trigger.Dispose();
        }
        catch
        {
        }

        lock (_ioLock)
        {
            _ready = false;

            try
            {
                _transport.Dispose();
            }
            catch
            {
            }
        }

        _sendLoopCts.Dispose();

        WriteLog(
            "HID transport closed. " +
            "Onboard lighting may resume.");
    }

    private static TextWriter CreateLogWriter()
    {
        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    LogPath)!);

            return new StreamWriter(
                LogPath,
                append: true,
                encoding:
                    new UTF8Encoding(false))
            {
                AutoFlush = true
            };
        }
        catch
        {
            return TextWriter.Null;
        }
    }

    private static void WriteLog(
        string message)
    {
        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    LogPath)!);

            File.AppendAllText(
            LogPath,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Hi75C/G] {message}{Environment.NewLine}",
            Encoding.UTF8);
        }
        catch
        {
            //
            // Diagnostics must never crash Artemis.
            //
        }
    }
}
