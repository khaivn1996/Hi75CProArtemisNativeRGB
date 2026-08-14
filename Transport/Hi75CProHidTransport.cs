using System;
using System.IO;
using System.Linq;
using HidSharp;
using Artemis.Plugins.Devices.LeobogHi75CPro.Protocol;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Transport;

internal sealed class Hi75CProHidTransport : IDisposable
{
    private HidStream? _stream;

    public string? DevicePath { get; private set; }

    public bool IsOpen => _stream != null;

    public bool TryOpen(TextWriter log)
    {
        if (_stream != null)
            return true;

        try
        {
            HidDevice[] devices = DeviceList.Local
                .GetHidDevices(
                    Hi75CProConstants.VendorId,
                    Hi75CProConstants.ProductId)
                .ToArray();

            log.WriteLine(
                $"[Hi75C] Found {devices.Length} HID path(s) matching " +
                $"{Hi75CProConstants.VendorId:X4}:" +
                $"{Hi75CProConstants.ProductId:X4}");

            HidDevice[] candidates = devices
                .Where(IsExpectedEndpoint)
                .ToArray();

            log.WriteLine(
                $"[Hi75C] MI_01/COL06 candidate(s) = {candidates.Length}");

            if (candidates.Length != 1)
            {
                log.WriteLine(
                    $"[Hi75C] ERROR: Expected exactly 1 endpoint, " +
                    $"got {candidates.Length}.");

                return false;
            }

            HidDevice endpoint = candidates[0];

            DevicePath = endpoint.DevicePath;

            log.WriteLine(
                $"[Hi75C] Selected path = {DevicePath}");

            int maxFeatureLength =
                endpoint.GetMaxFeatureReportLength();

            log.WriteLine(
                $"[Hi75C] Max Feature Report Length = " +
                $"{maxFeatureLength}");

            if (maxFeatureLength !=
                Hi75CProConstants.ReportLength)
            {
                log.WriteLine(
                    $"[Hi75C] ERROR: Expected feature length " +
                    $"{Hi75CProConstants.ReportLength}, got " +
                    $"{maxFeatureLength}.");

                return false;
            }

            if (!endpoint.TryOpen(out HidStream stream))
            {
                log.WriteLine(
                    "[Hi75C] ERROR: TryOpen failed.");

                return false;
            }

            _stream = stream;

            log.WriteLine(
                "[Hi75C] HID open = SUCCESS");

            return true;
        }
        catch (Exception ex)
        {
            log.WriteLine(
                $"[Hi75C] HID open exception: " +
                $"{ex.GetType().FullName}: {ex.Message}");

            return false;
        }
    }

    public byte? TryReadModelId(TextWriter log)
    {
        if (_stream == null)
        {
            log.WriteLine(
                "[Hi75C] ERROR: HID is not open.");

            return null;
        }

        try
        {
            byte[] request =
                new byte[Hi75CProConstants.ReportLength];

            Buffer.BlockCopy(
                Hi75CProConstants.ModelQuery,
                0,
                request,
                0,
                Hi75CProConstants.ModelQuery.Length);

            log.WriteLine(
                "[Hi75C] Sending verified model query.");

            _stream.SetFeature(request);

            byte[] response =
                new byte[Hi75CProConstants.ReportLength];

            response[0] =
                Hi75CProConstants.ModelReportId;

            _stream.GetFeature(response);

            byte modelId =
                response[
                    Hi75CProConstants.ModelResponseIndex];

            log.WriteLine(
                $"[Hi75C] response[" +
                $"{Hi75CProConstants.ModelResponseIndex}] = " +
                $"0x{modelId:X2} ({modelId})");

            return modelId;
        }
        catch (Exception ex)
        {
            log.WriteLine(
                $"[Hi75C] Model query exception: " +
                $"{ex.GetType().FullName}: {ex.Message}");

            return null;
        }
    }

    public bool TrySendRgbFrame(
        byte[] packet,
        TextWriter log,
        bool verbose = false)
    {
        if (_stream == null)
        {
            log.WriteLine(
                "[Hi75C] ERROR: RGB write requested while HID is closed.");

            return false;
        }

        try
        {
            if (packet.Length !=
                Hi75CProConstants.ReportLength)
            {
                log.WriteLine(
                    $"[Hi75C] ERROR: RGB packet length = " +
                    $"{packet.Length}.");

                return false;
            }

            if (!Hi75CProProtocol.HasValidRgbHeader(packet))
            {
                log.WriteLine(
                    "[Hi75C] ERROR: RGB header validation failed.");

                return false;
            }

            _stream.SetFeature(packet);

            if (verbose)
            {
                log.WriteLine(
                    "[Hi75C] RGB SetFeature = SUCCESS");
            }

            return true;
        }
        catch (Exception ex)
        {
            log.WriteLine(
                $"[Hi75C] RGB write exception: " +
                $"{ex.GetType().FullName}: {ex.Message}");

            return false;
        }
    }

    public void Dispose()
    {
        HidStream? stream = _stream;

        _stream = null;

        if (stream == null)
            return;

        try
        {
            stream.Dispose();
        }
        catch
        {
            // HID cleanup must never crash Artemis.
        }
    }

    private static bool IsExpectedEndpoint(
        HidDevice device)
    {
        string path;

        try
        {
            path = device.DevicePath ?? string.Empty;
        }
        catch
        {
            return false;
        }

        bool interfaceMatches =
            path.Contains(
                $"MI_{Hi75CProConstants.InterfaceNumber:X2}",
                StringComparison.OrdinalIgnoreCase);

        bool collectionMatches =
            path.Contains(
                $"COL{Hi75CProConstants.CollectionNumber:X2}",
                StringComparison.OrdinalIgnoreCase);

        return interfaceMatches &&
               collectionMatches;
    }
}