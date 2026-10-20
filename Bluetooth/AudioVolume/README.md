# AudioVolume — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclAudioSwitcher`, `wclAudioMeter`, and `wclAudioVolume` classes) for enumerating active render endpoints, displaying per-channel peak levels, and changing the master volume and mute state of the selected endpoint.

This demo is a Unity port of the reference **AudioVolume** sample documented at:
**https://www.btframework.com/bluetoothaudio.htm**

---

## Overview

The AudioVolume demo combines the audio switcher, the audio meter, and the audio volume classes into a single, continuously updating volume control. It lists active render endpoints, keeps the selection in sync with the system default device, and opens both a meter and a volume controller on the selected endpoint. A trackbar sets the master volume, a checkbox sets the mute state, and a set of progress bars displays the master peak and per-channel peaks at a fixed refresh interval.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclAudioSwitcher` / `wclAudioMeter` / `wclAudioVolume` API used |
|------|--------------------------------------------------------------------|
| Switcher lifecycle | `Open`, `Close`, `OnOpened`, `OnClosed` |
| Device tracking | `Enum(asActive)`, `GetDeviceDetails`, `OnDeviceAdded`, `OnDeviceRemoved`, `OnStateChanged`, `OnDefaultDeviceChanged` |
| Meter lifecycle | `Open(id)`, `Close`, `OnOpened`, `OnClosed`, `OnDisconnected` |
| Meter data | `GetHardwareFeatures`, `GetChannels`, `GetPeak`, `GetChannelsPeak` |
| Volume lifecycle | `Open(id)`, `Close`, `OnOpened`, `OnClosed`, `OnDisconnected` |
| Volume control | `GetHardwareFeatures`, `GetVolume`, `SetVolume`, `GetMute`, `SetMute`, `OnChanged` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- At least one active render audio endpoint
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, `wclAudio`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `AudioVolumeController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → AudioVolume → Build Scene**

This creates the canvas, all controls, the `AudioVolumeController` GameObject, and saves the scene to `Assets/Scenes/AudioVolume.unity`.

### 3. Run the scene

Enter Play mode. The switcher opens automatically and populates the endpoint list with all active render devices. The system default multimedia endpoint is selected. Press **Open** to open both a meter and a volume controller on the selected endpoint. The master progress bar, the per-channel bars, the trackbar, and the mute checkbox become active. Use **Close** to release the meter and the volume controller.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Endpoints** dropdown | Active render audio endpoints reported by `wclAudioSwitcher.Enum(asActive)`; the default multimedia endpoint is selected automatically |
| **Open** | Opens `wclAudioMeter` and `wclAudioVolume` on the selected endpoint |
| **Close** | Closes both the meter and the volume controller |
| **Master progress bar** | Shows the master peak; scaled by the current trackbar value when the trackbar is visible |
| **Channel progress bars** | One bar per metering channel; created dynamically from `GetChannels` |
| **Trackbar** | Sets the master volume (0 to 100) via `SetVolume` |
| **Mute** checkbox | Sets the mute state via `SetMute` |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Automatic endpoint list.** The switcher opens on start. Every active render endpoint is added to the list, and whenever the system default device changes the demo re-selects the corresponding entry, unless a meter is already open on another endpoint.
- **Trackbar visibility.** The trackbar and the mute checkbox are only shown while a volume controller is open. Their current values are populated from `GetVolume` and `GetMute` when the volume opens, and refreshed from `OnChanged` while it is active.
- **Master peak scaling.** When the trackbar is visible, the master progress bar is multiplied by the current volume so that it visually reflects the effective output level. When the trackbar is hidden (volume controller not open), the bar shows the raw peak.
- **Per-channel peaks.** If the endpoint exposes multiple metering channels, one progress bar is created for each channel at fixed positions to the right of the master bar. Bars are destroyed when the meter closes.
- **Refresh interval.** A timer polls `GetPeak` and `GetChannelsPeak` at a fixed interval. The demo uses that timer instead of an event-driven design because the meter API does not deliver change events for peak values.
- **Disconnection.** If the selected endpoint disappears from the system (Bluetooth device unplugged or turned off), `OnDisconnected` fires on the meter and the volume controller. The demo logs the event and leaves the trackbar and bars in place, but the corresponding methods will return errors until the endpoint is reopened.
- **Hardware features.** `GetHardwareFeatures` is reported for both the meter and the volume controller. It reflects which of `hfVolume`, `hfMute`, and `hfMeter` the underlying driver actually supports.
- **Cleanup.** Closing the meter removes the dynamic channel bars and hides the master bar. Closing the volume controller hides the trackbar and the mute checkbox. Both are also called on application exit.

---

## Project structure

Assets/
├── Scripts/
│ ├── AudioVolumeSceneBuilder.cs # Editor: Tools → AudioVolume → Build Scene
│ └── AudioVolumeController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── AudioVolume.unity # Saved by the scene builder


`AudioVolumeSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `AudioVolumeController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListBox`, `MessageBox`, `Slider`, `ProgressBar`) that emulate the equivalent WinForms controls, and a small fixed-interval driver that replaces the WinForms `Timer`.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Bluetooth Audio Devices (reference article):** https://www.btframework.com/bluetoothaudio.htm
- **Classic Bluetooth profiles overview:** https://www.btframework.com/bluetoothprofiles.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.