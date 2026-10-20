# AudioSwitcher — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclAudioSwitcher` class) for enumerating audio devices, switching the system default, reconnecting to already-paired Bluetooth audio devices, and disconnecting specific parts of an audio device without unpairing.

This demo is a Unity port of the reference **AudioSwitcher** sample documented at:
**https://www.btframework.com/bluetoothaudio.htm**

---

## Overview

The AudioSwitcher demo lists all audio endpoints visible to Windows, shows their ID, friendly name, description, flow, roles, state, MAC address, and service, and lets the user change the default device for the selected role. It also demonstrates the Bluetooth-specific ability to reconnect to an already-paired audio device without going through the Windows Add Device dialog, and to disconnect a single service (for example, the microphone of a headset) while leaving the other service connected.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclAudioSwitcher` API used |
|------|------------------------------|
| Switcher lifecycle | `Open`, `Close`, `OnOpened`, `OnClosed` |
| Device enumeration | `Enum(state, out devices)`, `wclAudioDevice`, `wclAudioDeviceState` |
| Default device | `SetDefault(role, id)`, `GetDefault(role, flow, out id)`, `OnDefaultDeviceChanged` |
| Device change events | `OnDeviceAdded`, `OnDeviceRemoved`, `OnStateChanged` |
| Bluetooth device handling | `Connect(mac)`, `Connect(id)`, `Disconnect(mac)`, `Disconnect(id)`, `GetDeviceDetails(id, out device)` |
| Roles and flow | `wclAudioDeviceRole` (Console, Multimedia, Communications), `wclAudioDeviceDataFlow` (Render, Capture) |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, `wclAudio`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `AudioSwitcherController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → AudioSwitcher → Build Scene**

This creates the canvas, all controls, the `AudioSwitcherController` GameObject, and saves the scene to `Assets/Scenes/AudioSwitcher.unity`.

### 3. Run the scene

Enter Play mode. Click **Enum** to populate the device list. Use the checkboxes to select which device states to include (active, disabled, unplugged, not present). Select a row and use the action buttons at the top.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Enum** | Refreshes the list of audio devices using the selected state flags |
| **Role** dropdown | Console, Multimedia, or Communications — used by `Set default` and `GetDefault` |
| **Flow** dropdown | Capture or Render — used by `GetDefault` |
| **Active / Disabled / Unplugged / Not present** | State filters for `Enum` |
| **Set default** | Calls `SetDefault` for the selected device and role |
| **Refresh** | Re-reads the selected device's details via `GetDeviceDetails` |
| **Connect / Disconnect** | Calls `Connect` or `Disconnect` for the selected device, either by ID or by MAC |
| **Use MAC** | When checked, `Connect` and `Disconnect` use the device MAC instead of the system ID |
| **GetDefault** | Calls `GetDefault` for the current role and flow and prints the returned ID |
| **Clear** | Empties the event log |

The device list shows: ID, friendly name, description, flow, roles, state, MAC, and connected service UUID (for Bluetooth devices only).

---

## Behavioural notes

- **Reconnect to a paired device.** If a Bluetooth audio device is already paired but not connected, enable the **Unplugged** filter, select the device, and press **Connect**. The framework reconnects without using the Windows Add Device dialog and without re-pairing.
- **Disconnect a single service.** The **Disconnect** button terminates only the specific service identified by the selected row. For a headset that exposes both an audio and a microphone service, this disconnects one part without affecting the other.
- **Default device changes.** Windows changes the default device automatically when a new endpoint connects. The `OnDefaultDeviceChanged` event reflects those changes in the log.
- **Non-Bluetooth devices.** For endpoints that are not Bluetooth, the MAC and service columns are empty. `Connect` and `Disconnect` return an error when `Use MAC` is enabled and the selected device has no MAC.

---

## Project structure

Assets/
├── Scripts/
│ ├── AudioSwitcherSceneBuilder.cs # Editor: Tools → AudioSwitcher → Build Scene
│ └── AudioSwitcherController.cs # Runtime: demo logic
└── Scenes/
└── AudioSwitcher.unity # Saved by the scene builder


`AudioSwitcherSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `AudioSwitcherController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Bluetooth Audio Devices (reference article):** https://www.btframework.com/bluetoothaudio.htm
- **Classic Bluetooth profiles overview:** https://www.btframework.com/bluetoothprofiles.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.