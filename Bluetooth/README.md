# Bluetooth Framework — Unity Examples

A collection of Unity examples demonstrating the **[Bluetooth Framework](https://www.btframework.com/bluetoothframework.htm)** on Windows. Each example is a self-contained Unity project that reproduces one sample from the framework documentation using the same public API, the same control layout, and the same user-visible behaviour as the original WinForms reference sample.

The examples are independent. Every folder contains its own `README.md` with the reference article link, API coverage, requirements, and behavioural notes for that particular sample.

---

## Overview

Each example follows the same structure:

- A **scene builder** editor script (`<Name>SceneBuilder.cs`, wrapped in `#if UNITY_EDITOR`) that creates the canvas, all controls, and the controller object programmatically, wires every UI element to its corresponding controller field, and saves the scene to `Assets/Scenes/<Name>.unity`. There are no hand-authored scenes or prefabs to maintain.
- A **runtime controller** (`<Name>Controller.cs`, a plain `MonoBehaviour`) that reproduces the reference sample's logic 1:1 — event handlers, error formatting, trace strings, and per-API call order — using only the public Bluetooth Framework API.
- A **UI shim** in the same controller file for the WinForms controls that Unity does not provide (`ListView`, `ListBox`, `MessageBox`, `ProgressBar`, `OpenFileDialog`, `SaveFileDialog`, `Slider`). These are plain C# classes, not `MonoBehaviour`, and emulate the equivalent WinForms behaviour closely enough that the controller code can be written as if it were running inside a WinForms form.

Native calls into the Bluetooth Framework are performed from the main thread inside `Update` through a small dispatch queue. This avoids the thread-affinity problems that can occur when the framework is called directly from a Unity UI event handler.

---

## Examples

| Example | Reference article | Framework classes | What it demonstrates |
|---------|-------------------|-------------------|----------------------|
| **BleSniffer** | [blesniffer.htm](https://www.btframework.com/blesniffer.htm) | `wclBleSniffer` | Capturing and decoding raw Bluetooth LE frames with a CC2540 USB dongle |
| **BluetoothManager** | [bluetooth_howto.htm](https://www.btframework.com/bluetooth_howto.htm) | `wclBluetoothManager`, `wclBluetoothRadio`, `wclPowerEventsMonitor` | Radio enumeration, device discovery, pairing, services, virtual COM ports |
| **AudioSwitcher** | [bluetoothaudio.htm](https://www.btframework.com/bluetoothaudio.htm) | `wclAudioSwitcher` | Enumerating audio devices, switching the system default, reconnecting paired devices, per-service disconnect |
| **AudioReceiver** | [bluetoothaudio.htm](https://www.btframework.com/bluetoothaudio.htm) | `wclBluetoothAudioWatcher`, `wclBluetoothAudioReceiver` | Turning the PC into an A2DP audio receiver |
| **AudioVolume** | [bluetoothaudio.htm](https://www.btframework.com/bluetoothaudio.htm) | `wclAudioSwitcher`, `wclAudioMeter`, `wclAudioVolume` | Live audio meter, per-channel peaks, master volume and mute control |
| **FtpClient** | [obex.htm](https://www.btframework.com/obex.htm) | `wclObexFtpClient`, `wclRfCommClient` | Browsing a remote file system over OBEX File Transfer Profile |
| **OppClient** | [obex.htm](https://www.btframework.com/obex.htm) | `wclObexOppClient`, `wclRfCommClient` | Sending files, requesting vCards and capability objects over OBEX OPP |
| **OppServer** | [obex.htm](https://www.btframework.com/obex.htm) | `wclObexOppServer`, `wclRfCommServer` | Accepting files over OBEX OPP, publishing the SDP record |
| **GattClient** | [gatt.htm](https://www.btframework.com/gatt.htm) | `wclGattClient` | Full central-side GATT workflow: services, characteristics, values, descriptors, notifications, connection parameters |
| **GattServer** | [gattserver.htm](https://www.btframework.com/gattserver.htm) | `wclGattServer` | Full peripheral-side GATT workflow: services, characteristics, read/write handling, notifications, per-client parameters |
| **RfCommClient** | [rfcomm.htm](https://www.btframework.com/rfcomm.htm) | `wclRfCommClient` | RFCOMM connection, service discovery by name, data exchange, buffer sizes |
| **RfCommServer** | [rfcomm.htm](https://www.btframework.com/rfcomm.htm) | `wclRfCommServer` | Publishing an SPP service, accepting clients, data exchange, per-client buffers |
| **WiiRemote** | [bluetoothprofiles.htm](https://www.btframework.com/bluetoothprofiles.htm) | `wclWiiRemoteClient` | Nintendo Wii Remote buttons, accelerometer, IR camera, LEDs, rumble, Nunchuk and Balance Board extensions |
| **Beacons** | [blebeacon.htm](https://www.btframework.com/blebeacon.htm) | `wclBluetoothLeBeaconWatcher`, `wclBluetoothLeAdvertiser` | Decoding every supported advertisement type, and building / transmitting iBeacon, Eddystone, AltBeacon, Microsoft CDP, Apple Continuity, and custom raw advertisements |

Every example has a dedicated `README.md` next to its scripts describing the API coverage, requirements, and behavioural notes for that specific sample.

---

## Requirements

- **Unity** 2021.3 LTS or newer. TextMeshPro and the Editor API are used by every scene builder.
- **Bluetooth Framework** installed and referenced by the project.
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows. A few examples additionally require specific Windows versions or hardware:
  - **BleSniffer** requires a CC2540 USB dongle with the WinUSB driver installed.
  - **GattClient** requires Windows 10 1703 or newer for direct in-app discovery. Older Windows versions require the peripheral to be paired first through Windows UI.
  - **GattServer** requires Windows 10 version 1703 or newer with the Microsoft Bluetooth driver, or a BLED112 USB dongle.
  - **Beacons**, when extended advertisement is enabled, requires a Bluetooth 5 radio and Windows 10 or newer with the Microsoft Bluetooth driver.
  - **AudioSwitcher**, **AudioReceiver**, and **AudioVolume** require Windows 10 or newer.
- Both **Mono** and **IL2CPP** scripting backends are supported.

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, `wclCommunication`, `wclAudio`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains the example controllers.

### 2. Install the example scripts

Copy the `Scripts` folder of the desired example into the Unity project's `Assets` tree. Each example is independent; multiple examples can coexist in the same project as long as their scene builders are invoked one at a time.

### 3. Build the example scene

Open the corresponding menu item:

> **Tools → &lt;Example Name&gt; → Build Scene**

The scene builder creates the canvas, the controls, the controller object, and saves the scene to `Assets/Scenes/<ExampleName>.unity`.

### 4. Run the example

Open the generated scene and press **Play**. The exact startup behaviour of each example is described in its own `README.md`.

---

## Common design decisions

These decisions apply to every example in the collection and are worth understanding before reading individual controllers.

- **Programmatic UI.** No scenes or prefabs are authored by hand. The scene builder scripts construct the entire UI at build time using `GameObject` and `RectTransform` calls, matching the layout and control sizes of the original WinForms reference. Coordinates are taken directly from the reference `.Designer.cs` files, so the visual result is a close match to the WinForms sample.
- **TextMesh Pro only.** Every label, button text, input field, dropdown label, list view cell, and log line is rendered through TextMesh Pro. Legacy `UnityEngine.UI.Text` is not used anywhere.
- **Plain C# UI shims.** `ListView`, `ListBox`, `MessageBox`, `ProgressBar`, `OpenFileDialog`, `SaveFileDialog`, and `Slider` are implemented as plain C# classes inside the controller file. `ListView` in particular reproduces the WinForms model — `Items.Add`, `Items.Insert`, `Items.Remove`, `Items.RemoveAt`, `SubItems[i].Text`, `Tag`, `SelectedItems`, `Index` — so that controller code matches the reference sample almost line for line.
- **Native dialogs.** `MessageBox`, `OpenFileDialog`, and `SaveFileDialog` are backed by the native Win32 functions (`MessageBoxW`, `GetOpenFileNameW`, `GetSaveFileNameW`). They are blocking, exactly as in WinForms, and can be called from any thread.
- **Dispatch queue.** Calls into the Bluetooth Framework from button handlers, and UI updates from framework events, are queued and executed from `Update` on the main thread. `out`-parameters in framework events are still set synchronously so the framework sees the correct value.
- **Dropdown sizing.** `TMP_Dropdown` is adjusted in `Update` so that the last option in the popup is fully visible, matching the WinForms `ComboBox` behaviour.
- **Cleanup.** Every controller stops or closes its framework objects in `OnDestroy`, wrapped in try/catch so that shutdown proceeds even if the framework returns an error.

---

## Project structure

Each example follows the same layout:

<ExampleName>/
├── README.md
├── Scripts/
│ ├── <ExampleName>SceneBuilder.cs # Editor: Tools → <ExampleName> → Build Scene
│ └── <ExampleName>Controller.cs # Runtime: demo logic and UI shims
└── Scenes/
└── <ExampleName>.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API together with the UI shims described above.

---

## Links

- **Bluetooth Framework (product page and licensing):** https://www.btframework.com/bluetoothframework.htm
- **Installation guide:** https://www.btframework.com/howtoinstall.htm
- **How-To guides:** https://www.btframework.com/bluetooth_howto.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm
- **Reference articles:**
  - BLE Advertisement: https://www.btframework.com/blebeacon.htm
  - BLE Extended Advertisement: https://www.btframework.com/bleextadv.htm
  - BLE Sniffer: https://www.btframework.com/blesniffer.htm
  - BLE GATT Communication: https://www.btframework.com/gatt.htm
  - BLE GATT Server: https://www.btframework.com/gattserver.htm
  - GATT Connection State Change Detection: https://www.btframework.com/gattclientconnection.htm
  - Classic Bluetooth (RFCOMM): https://www.btframework.com/rfcomm.htm
  - OBEX overview: https://www.btframework.com/obex.htm
  - Bluetooth Audio Devices: https://www.btframework.com/bluetoothaudio.htm
  - Bluetooth Classic profiles overview: https://www.btframework.com/bluetoothprofiles.htm
  - Wireless Communication Library (underlying library): https://www.btframework.com/wcl.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. The examples in this collection are illustrative samples and do not include the Bluetooth Framework binaries; obtain them separately from the vendor.