<p align="center">
  <img
    src="Assets/logo.svg"
    width="240"
    alt="LEOBOG Hi75C Pro Artemis Native RGB"
  />
</p>

# LEOBOG Hi75C Pro — Artemis Native RGB

Native wired USB RGB device support for the **LEOBOG Hi75C Pro** keyboard in [Artemis RGB](https://artemis-rgb.com/).

This plugin integrates the keyboard directly with Artemis so Artemis can control per-key RGB effects such as solid colors, gradients, key-reactive effects, waves, and other supported Artemis lighting profiles.

## Current support

- LEOBOG Hi75C Pro
- Wired USB connection
- 80 individually addressable keyboard LEDs
- Native Artemis / RGB.NET device integration
- Standard keyboard LED IDs
- Artemis keyboard layout support
- Solid color effects
- Spatial gradients
- Animated gradients
- Key press / reactive effects
- Ripple / key wave effects
- Automatic USB reconnect
- Clean plugin Enable / Disable lifecycle
- Clean Artemis shutdown handling

## Connection support

| Connection | Status |
|---|---|
| Wired USB | Supported |
| 2.4 GHz wireless | Not supported yet |
| Bluetooth | Not supported |

Wireless modes are intentionally not treated as compatible with the wired protocol until they are independently verified.

## Tested hardware

### USB identity

```text
VID: 258A
PID: 010C
```

Verified vendor HID endpoint:

```text
Interface  : 1
Usage Page : FF00
Usage      : 0001
Collection : 0006
```

Verified model ID:

```text
0xA3 / 163
```

## RGB implementation

The keyboard uses a 520-byte HID Feature Report.

Verified RGB report header:

```text
06 08 00 00 01 00 7A 01
```

The plugin maintains a dedicated approximately 20 FPS physical RGB stream because the keyboard requires continuous refresh while software RGB control is active.

## Safety

The plugin validates the keyboard model before enabling RGB output.

RGB output is only enabled after the verified model query reports:

```text
0xA3
```

No unverified HID commands or firmware modifications are used.

## Installation

1. Download the latest plugin ZIP release.
2. Open Artemis.
3. Go to **Settings → Plugins → Import plugin**.
4. Select the downloaded ZIP file.
5. Restart Artemis once after the first installation.
6. The LEOBOG Hi75C Pro should appear under **Settings → Devices**.

The restart is only required after the initial plugin installation. Normal
plugin Enable/Disable and USB reconnect operations do not require restarting
Artemis.

## USB reconnect

If the keyboard is unplugged while Artemis is running, the plugin:

1. Stops using the invalid HID transport.
2. Waits for the keyboard to return.
3. Opens a fresh HID transport.
4. Verifies the model ID again.
5. Restores the latest Artemis RGB frame.

Reconnect attempts stop cleanly when the plugin is disabled or Artemis exits.

## Plugin lifecycle

When the plugin is disabled:

- the RGB worker is stopped;
- reconnect activity is cancelled;
- the HID handle is released;
- the keyboard can return to its onboard RGB effect.

When the plugin is enabled again, a fresh device provider, RGB device, update queue, and HID transport are created.

## Requirements

- Windows
- Artemis RGB
- LEOBOG Hi75C Pro connected over wired USB

The current implementation is only declared as Windows-compatible because that is the platform on which HID behavior, reconnect handling, and plugin lifecycle have been validated.

## Development

Project target:

```text
.NET 10
x64
```

Main dependencies:

```text
ArtemisRGB.Core
ArtemisRGB.UI.Shared
ArtemisRGB.Plugins.BuildTask
RGB.NET
HidSharp
```

Development builds are copied into:

```text
C:\ProgramData\Artemis\Plugins\Artemis.Plugins.Devices.LeobogHi75CPro
```

## Status

Current native RGB functionality validated:

```text
Device detection            PASS
Model verification          PASS
Static RGB                  PASS
RGB.NET integration         PASS
Keyboard layout             PASS
Dynamic gradients           PASS
Reactive key effects        PASS
Ripple / key wave           PASS
20 FPS sustained output     PASS
USB disconnect/reconnect    PASS
Plugin Enable/Disable       PASS
Artemis shutdown cleanup    PASS
```

## Author

**KhaiIT**

## Repository

https://github.com/khaivn1996/Hi75CProArtemisNativeRGB
