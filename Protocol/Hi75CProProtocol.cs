using System;
using Artemis.Plugins.Devices.LeobogHi75CPro.Mapping;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Protocol;

internal static class Hi75CProProtocol
{
    public static byte[] CreateRgbFrame()
    {
        byte[] packet =
            new byte[Hi75CProConstants.ReportLength];

        Buffer.BlockCopy(
            Hi75CProConstants.RgbHeader,
            0,
            packet,
            0,
            Hi75CProConstants.RgbHeader.Length);

        return packet;
    }

    public static void SetRawLedColor(
        byte[] packet,
        int rawLedIndex,
        byte red,
        byte green,
        byte blue)
    {
        if (packet.Length != Hi75CProConstants.ReportLength)
            throw new ArgumentException(
                "Invalid Hi75C Pro RGB packet length.",
                nameof(packet));

        if (rawLedIndex < 0 ||
            rawLedIndex > Hi75CProConstants.MaxRawLedIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawLedIndex));
        }

        int offset =
            Hi75CProConstants.RgbDataOffset +
            rawLedIndex * 3;

        if (offset + 2 >= packet.Length)
            throw new InvalidOperationException(
                "RGB offset exceeds packet boundary.");

        packet[offset + 0] = red;
        packet[offset + 1] = green;
        packet[offset + 2] = blue;
    }

    public static byte[] BuildSolidRgbFrame(
        byte red,
        byte green,
        byte blue)
    {
        Hi75CProLedMap.Validate();

        byte[] packet =
            CreateRgbFrame();

        foreach (int rawLedIndex
                 in Hi75CProLedMap.RawLedIndices)
        {
            SetRawLedColor(
                packet,
                rawLedIndex,
                red,
                green,
                blue);
        }

        return packet;
    }

    public static bool HasValidRgbHeader(
        byte[] packet)
    {
        if (packet.Length != Hi75CProConstants.ReportLength)
            return false;

        for (int i = 0;
             i < Hi75CProConstants.RgbHeader.Length;
             i++)
        {
            if (packet[i] !=
                Hi75CProConstants.RgbHeader[i])
            {
                return false;
            }
        }

        return true;
    }
}