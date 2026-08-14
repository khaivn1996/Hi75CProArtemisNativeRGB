using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using RGB.NET.Core;
using Artemis.Plugins.Devices.LeobogHi75CPro.Protocol;
using Artemis.Plugins.Devices.LeobogHi75CPro.Transport;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Devices;

public sealed class Hi75CProRgbDeviceProvider
    : AbstractRGBDeviceProvider
{
    //
    // USB/HID may need a short stabilization period after
    // Windows re-enumerates the keyboard.
    //
    // Keep this retry bounded so plugin startup is never
    // allowed to wait indefinitely.
    //
    private const int DetectionAttemptCount = 5;
    private const int DetectionRetryDelayMs = 400;

    private static readonly string LogPath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "Artemis",
            "Plugins",
            "Artemis.Plugins.Devices.LeobogHi75CPro",
            "hi75c-phase-g.log");

    private readonly object _deviceLock = new();

    private Hi75CProRgbDevice? _activeDevice;

    protected override void InitializeSDK()
    {
        //
        // No vendor SDK.
        //
        // Direct HID transport is used by our device queue.
        //
    }

    protected override IEnumerable<IRGBDevice> LoadDevices()
    {
        bool verified = false;

        byte? lastModelId = null;

        for (int attempt = 1;
             attempt <= DetectionAttemptCount;
             attempt++)
        {
            //
            // Every detection attempt gets a fresh transport.
            //
            // Never reuse a handle from an unsuccessful
            // Windows/HID enumeration attempt.
            //
            using Hi75CProHidTransport transport =
                new();

            bool opened =
                transport.TryOpen(
                    TextWriter.Null);

            if (opened)
            {
                lastModelId =
                    transport.TryReadModelId(
                        TextWriter.Null);

                if (lastModelId ==
                    Hi75CProConstants.ModelId)
                {
                    verified = true;

                    if (attempt > 1)
                    {
                        WriteLog(
                            $"Device detection recovered " +
                            $"after {attempt} attempt(s). " +
                            $"Model gate PASS: " +
                            $"0x{Hi75CProConstants.ModelId:X2}.");
                    }

                    break;
                }

                //
                // 0x00 has been observed during the short
                // re-enumeration window of this verified
                // Hi75C Pro, so it is treated as transient.
                //
                // Any other non-zero model ID remains a hard
                // safety failure. Do not retry it and never
                // create an RGB device for it.
                //
                if (lastModelId.HasValue &&
                    lastModelId.Value != 0x00)
                {
                    WriteLog(
                        $"Device detection aborted: " +
                        $"unexpected model ID " +
                        $"0x{lastModelId.Value:X2}. " +
                        $"Expected " +
                        $"0x{Hi75CProConstants.ModelId:X2}.");

                    return Array.Empty<IRGBDevice>();
                }
            }

            if (attempt <
                DetectionAttemptCount)
            {
                Thread.Sleep(
                    DetectionRetryDelayMs);
            }
        }

        if (!verified)
        {
            WriteLog(
                $"Device detection failed after " +
                $"{DetectionAttemptCount} attempt(s). " +
                $"Last model ID = " +
                $"{(lastModelId.HasValue
                    ? $"0x{lastModelId.Value:X2}"
                    : "NULL")}.");

            return Array.Empty<IRGBDevice>();
        }

        Hi75CProRgbDevice device =
            new();

        lock (_deviceLock)
        {
            _activeDevice =
                device;
        }

        return new IRGBDevice[]
        {
            device
        };
    }

    internal void Shutdown()
    {
        Hi75CProRgbDevice? device;

        lock (_deviceLock)
        {
            device =
                _activeDevice;

            _activeDevice =
                null;
        }

        //
        // Do not hold _deviceLock while waiting for
        // Hi75CProUpdateQueue.Dispose().
        //
        device?.Shutdown();
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
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " +
                $"[Hi75C/Provider] " +
                $"{message}" +
                Environment.NewLine,
                Encoding.UTF8);
        }
        catch
        {
            //
            // Diagnostics must never break Artemis.
            //
        }
    }
}