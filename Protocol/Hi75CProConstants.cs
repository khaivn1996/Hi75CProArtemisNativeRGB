namespace Artemis.Plugins.Devices.LeobogHi75CPro.Protocol;

internal static class Hi75CProConstants
{
    public const int VendorId = 0x258A;
    public const int ProductId = 0x010C;

    public const int InterfaceNumber = 1;
    public const int CollectionNumber = 6;

    public const int ReportLength = 520;

    public const byte ModelReportId = 0x06;
    public const int ModelResponseIndex = 13;
    public const byte ModelId = 0xA3; // 163

    public const int LogicalLedCount = 80;
    public const int MaxRawLedIndex = 89;
    public const int RgbDataOffset = 8;

    public static readonly byte[] ModelQuery =
    {
        0x06,
        0x82,
        0x01,
        0x00,
        0x01,
        0x00,
        0x06
    };

    public static readonly byte[] RgbHeader =
    {
        0x06,
        0x08,
        0x00,
        0x00,
        0x01,
        0x00,
        0x7A,
        0x01
    };
}