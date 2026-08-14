using Artemis.Core;
using Artemis.Core.DeviceProviders;
using Artemis.Core.Services;
using Artemis.Plugins.Devices.LeobogHi75CPro.Devices;
using RGB.NET.Core;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Features;

[PluginFeature(
    Name = "LEOBOG Hi75C Pro Device Provider",
    Description = "Native wired USB device provider for the LEOBOG Hi75C Pro keyboard."
)]
public sealed class LeobogDeviceProvider : DeviceProvider
{
    private readonly IDeviceService _deviceService;

    //
    // IMPORTANT:
    //
    // An RGB.NET provider instance is used for one Artemis
    // Enable lifecycle only.
    //
    // After Disable we discard it and create a fresh one.
    //
    private Hi75CProRgbDeviceProvider
        _rgbDeviceProvider = new();

    private bool _registered;

    public override IRGBDeviceProvider RgbDeviceProvider
        => _rgbDeviceProvider;

    public LeobogDeviceProvider(
        IDeviceService deviceService)
    {
        _deviceService =
            deviceService;

        //
        // Physical layout is known.
        //
        CanDetectPhysicalLayout = true;

        //
        // EN/Telex etc. is an OS/input-method concern,
        // not a physical keyboard-layout property.
        //
        CanDetectLogicalLayout = false;
    }

    public override void Enable()
    {
        if (_registered)
            return;

        try
        {
            _deviceService.AddDeviceProvider(
                this);

            _registered = true;
        }
        catch
        {
            //
            // AddDeviceProvider may already have caused
            // RGB.NET to create our physical device.
            //
            // Never leave that queue/HID transport alive
            // if registration fails halfway through.
            //
            _rgbDeviceProvider.Shutdown();

            _rgbDeviceProvider =
                new Hi75CProRgbDeviceProvider();

            throw;
        }
    }

    public override void Disable()
    {
        if (!_registered)
            return;

        try
        {
            //
            // First detach the device from Artemis so that
            // rendering can no longer target this lifecycle.
            //
            _deviceService.RemoveDeviceProvider(
                this);
        }
        finally
        {
            _registered = false;

            //
            // Explicitly terminate our physical resources.
            //
            // This reaches Hi75CProUpdateQueue.Dispose().
            //
            _rgbDeviceProvider.Shutdown();

            //
            // Do NOT reuse the old AbstractRGBDeviceProvider
            // instance on the next Enable.
            //
            // Give the next lifecycle a clean provider,
            // device, queue and HID transport.
            //
            _rgbDeviceProvider =
                new Hi75CProRgbDeviceProvider();
        }
    }
}