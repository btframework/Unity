# AudioReceiver — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclBluetoothAudioWatcher` and `wclBluetoothAudioReceiver` classes) for turning a PC into an A2DP audio receiver: enumerating paired Bluetooth audio sources, listening for incoming audio connections, and optionally forcing a connection from the PC side.

This demo is a Unity port of the reference **AudioReceiver** sample documented at:
**https://www.btframework.com/bluetoothaudio.htm**

---

## Overview

The AudioReceiver demo watches for Bluetooth audio devices that have already been paired with the system, lists them with their device ID and friendly name, and exposes three operations on the receiver: start listening for incoming A2DP connections from a selected source, force a connection from the PC to the selected source, and close the receiver. Watcher and receiver lifecycle events are written to a log so that the full state of the demo is visible at a glance.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclBluetoothAudioWatcher` / `wclBluetoothAudioReceiver` API used |
|------|--------------------------------------------------------------------|
| Watcher lifecycle | `Start`, `Stop`, `OnStarted`, `OnStopped` |
| Device tracking | `OnDeviceAdded`, `OnDeviceRemoved` |
| Receiver lifecycle | `Listen(id)`, `Connect`, `Close`, `OnListen`, `OnClosed` |
| Connection events | `OnConnected`, `OnDisconnected` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- A Bluetooth Classic stack and a radio that can accept an A2DP source connection
- A Bluetooth audio source already paired with the PC (a phone or another PC)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Pair the audio source first

The receiver only works with devices that are already paired. Pair the source device (phone or other PC) through the standard Windows Add Device dialog, or use the `BluetoothManager` demo to pair it programmatically. Once paired, enumerate the device's services and ensure the A2DP service is installed.

### 2. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, `wclAudio`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `AudioReceiverController.cs`.

### 3. Build the demo scene

From the Unity menu select:

> **Tools → AudioReceiver → Build Scene**

This creates the canvas, all controls, the `AudioReceiverController` GameObject, and saves the scene to `Assets/Scenes/AudioReceiver.unity`.

### 4. Run the scene

Enter Play mode and click **Start** to enable the watcher. Every paired Bluetooth audio device that the watcher can see appears in the list. Select one and click **Open** to make the PC listen for an incoming A2DP connection from that device, or click **Connect** to initiate the connection from the PC side.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Start / Stop** | Starts or stops `wclBluetoothAudioWatcher` |
| **Device list** | Populated from `OnDeviceAdded` with the device ID and friendly name; entries are removed from `OnDeviceRemoved` |
| **Open** | Calls `AudioReceiver.Listen(id)` for the selected device |
| **Close** | Calls `AudioReceiver.Close` |
| **Connect** | Calls `AudioReceiver.Connect` to force a connection from the PC to the selected device |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Watcher must be started before devices appear.** Until `Start` succeeds the device list is empty. `OnStarted` clears the list and `OnDeviceAdded` begins filling it.
- **A2DP only.** The receiver supports only the A2DP profile. If the source device is paired for HFP or HSP only, `Listen` will not receive an incoming audio connection.
- **One active receiver at a time.** A single `wclBluetoothAudioReceiver` object listens on a single device. To switch sources, close the receiver and open it again for the new device.
- **Forcing a connection.** `Connect` may return `WCL_E_SUCCESS` even if the remote device refuses the connection; the actual outcome is delivered through `OnConnected` / `OnDisconnected`.
- **Reconnection after a remote disconnect.** If the remote device terminates the connection from its side, calling `Connect` on the PC side again may require closing and reopening the receiver.
- **Receiver cleanup.** `Close` releases the receiver. The demo calls it from `FormClosed`; production applications should call it whenever the receiver is no longer needed.

---

## Project structure

Assets/
├── Scripts/
│ ├── AudioReceiverSceneBuilder.cs # Editor: Tools → AudioReceiver → Build Scene
│ └── AudioReceiverController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── AudioReceiver.unity # Saved by the scene builder


`AudioReceiverSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `AudioReceiverController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Bluetooth Audio Devices (reference article):** https://www.btframework.com/bluetoothaudio.htm
- **Classic Bluetooth profiles overview:** https://www.btframework.com/bluetoothprofiles.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.