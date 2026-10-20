# RfCommClient — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclRfCommClient` class) for connecting to a Bluetooth Classic service over RFCOMM, exchanging data, and controlling the read and write buffer sizes.

This demo is a Unity port of the reference **Bluetooth RFCOMM Client** sample documented at:
**https://www.btframework.com/rfcomm.htm**

---

## Overview

The RfCommClient demo connects to a remote Bluetooth Classic device that exposes the Serial Port Profile (`SerialPortServiceClass_UUID`), sends ASCII text, receives incoming data, and adjusts the read and write buffer sizes. Connection parameters (authentication, encryption, timeout) are configurable. Discovery supports both a normal classic scan and an enumeration of already-paired devices.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclRfCommClient` API used |
|------|----------------------------|
| Discovery | `wclBluetoothRadio.Discover(dkClassic)`, `EnumPairedDevices`, `OnDeviceFound`, `OnDiscoveringStarted`, `OnDiscoveringCompleted` |
| Pairing | `RemotePair`, `RemoteUnpair`, `OnNumericComparison`, `OnPasskeyNotification`, `OnPasskeyRequest`, `OnPinRequest`, `OnConfirm`, `OnAuthenticationCompleted` |
| Connection | `Connect`, `Disconnect`, `Address`, `Service`, `Channel`, `Authentication`, `Encryption`, `Timeout`, `State`, `OnConnect`, `OnDisconnect` |
| Service selection | `EnumRemoteServices`, `wclBluetoothService.Name`, `wclBluetoothService.Channel` |
| Data transfer | `Write`, `OnData`, `Encoding.ASCII` |
| Buffers | `GetReadBufferSize`, `SetReadBufferSize`, `GetWriteBufferSize`, `SetWriteBufferSize` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A remote Bluetooth Classic device that exposes the SPP service
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `RfCommClientController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → RfCommClient → Build Scene**

This creates the canvas, all controls, the `RfCommClientController` GameObject, and saves the scene to `Assets/Scenes/RfCommClient.unity`.

### 3. Run the scene

Enter Play mode. Click **Discover** (or enable **Enum paired** first), select a device, then click **Connect**. Type text into the send field and press **Send** to transmit it. Incoming data appears in the event log.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discover** | Starts classic discovery, or enumerates paired devices when **Enum paired** is enabled |
| **Enum paired** | Switches discovery between scanning and paired-device enumeration |
| **Pair / Unpair** | Calls `RemotePair` / `RemoteUnpair` with `pmClassic` |
| **Connect / Disconnect** | Opens or closes the RFCOMM link |
| **Authentication / Encryption** | Applied to the client before connecting |
| **Timeout** | Applied as `wclRfCommClient.Timeout`, in milliseconds |
| **Use service name** | When enabled, resolves the RFCOMM channel by matching `wclBluetoothService.Name` against the service name field |
| **Send** | Sends the entered text as ASCII via `Write` |
| **Get buffers / Set buffers** | Reads or writes the read and write buffer sizes |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Asynchronous connect.** `Connect` only starts the procedure; the actual result is delivered in `OnConnect`. If the returned code from `Connect` is `WCL_E_SUCCESS`, the connection procedure was started; the real outcome is in the event.
- **Service resolution.** When **Use service name** is enabled, the demo calls `EnumRemoteServices` with the SPP UUID filter and picks the service whose `Name` matches the text field. It then sets `Client.Channel` to that service's channel number. If the name does not match any service, the demo reports "Service not found" and does not connect.
- **Buffers.** The buffer sizes are driver-dependent. Values that exceed the maximum supported by the radio are rejected by `SetReadBufferSize` / `SetWriteBufferSize`. The demo reads the current values on connect and whenever **Get buffers** is pressed.
- **Data.** Data is delivered as raw bytes in `OnData`. The demo converts to ASCII for display; production applications should treat it as binary unless the protocol is known.

---

## Project structure

Assets/
├── Scripts/
│ ├── RfCommClientSceneBuilder.cs # Editor: Tools → RfCommClient → Build Scene
│ └── RfCommClientController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── RfCommClient.unity # Saved by the scene builder


`RfCommClientSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `RfCommClientController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **Bluetooth Classic profiles overview:** https://www.btframework.com/bluetoothprofiles.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.