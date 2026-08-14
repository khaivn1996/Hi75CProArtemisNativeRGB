using System.Threading;
using RGB.NET.Core;
using Artemis.Plugins.Devices.LeobogHi75CPro.Mapping;

namespace Artemis.Plugins.Devices.LeobogHi75CPro.Devices;

public sealed class Hi75CProRgbDevice
    : AbstractRGBDevice<Hi75CProKeyboardDeviceInfo>,
      IKeyboard
{
    private const float KeyUnitMm = 19f;

    private readonly Hi75CProUpdateQueue _updateQueue;

    //
    // Shutdown may be requested by our provider and,
    // depending on RGB.NET/Artemis lifecycle, disposal
    // may also happen elsewhere.
    //
    // Keep our explicit shutdown idempotent.
    //
    private int _shutdown;

    IKeyboardDeviceInfo IKeyboard.DeviceInfo
        => DeviceInfo;

    public Hi75CProRgbDevice()
        : this(new Hi75CProUpdateQueue())
    {
    }

    private Hi75CProRgbDevice(
        Hi75CProUpdateQueue updateQueue)
        : base(
            new Hi75CProKeyboardDeviceInfo(),
            updateQueue)
    {
        _updateQueue = updateQueue;

        InitializeLeds();

        //
        // Only start HID/render handling after the complete
        // 80-LED RGB.NET device exists.
        //
        _updateQueue.Start();
    }

    internal void Shutdown()
    {
        if (Interlocked.Exchange(
                ref _shutdown,
                1) != 0)
        {
            return;
        }

        //
        // This is the important missing lifecycle link.
        //
        // Hi75CProUpdateQueue.Dispose() will:
        // - cancel the physical HID worker
        // - stop reconnect attempts
        // - wait for the worker
        // - dispose the HID transport
        //
        _updateQueue.Dispose();
    }

    private void InitializeLeds()
    {
        Hi75CProLedMap.Validate();

        foreach (Hi75CProKeyDefinition key
                 in Hi75CProLedMap.Keys)
        {
            Point location = new(
                key.GridX * KeyUnitMm,
                key.GridY * KeyUnitMm);

            Size size = new(
                KeyUnitMm,
                KeyUnitMm);

            AddLed(
                key.LedId,
                location,
                size,

                //
                // Standard Artemis/RGB.NET LedId above.
                //
                // Raw hardware index only lives in CustomData.
                //
                key.RawLedIndex);
        }
    }
}