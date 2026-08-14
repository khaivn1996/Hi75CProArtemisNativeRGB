using RGB.NET.Core;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Devices;

public sealed class Hi75CProKeyboardDeviceInfo : IKeyboardDeviceInfo
{
    public RGBDeviceType DeviceType
        => RGBDeviceType.Keyboard;

    public string DeviceName
        => "LEOBOG Hi75C Pro";

    public string Manufacturer
        => "LEOBOG";

    public string Model
        => "Hi75C Pro";

    public KeyboardLayoutType Layout
        => KeyboardLayoutType.ANSI;

    public object? LayoutMetadata { get; set; }
}