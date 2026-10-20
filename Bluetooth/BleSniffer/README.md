# BleSniffer — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclBleSniffer` class) for capturing raw Bluetooth Low Energy frames with a CC2540 USB dongle.

This demo is a Unity port of the reference **BleSniffer** sample documented at:
**https://www.btframework.com/blesniffer.htm**

---

## Overview

The BleSniffer demo captures and decodes over-the-air Bluetooth Low Energy packets using a CC2540-based USB dongle configured with the WinUSB driver. The sniffer passively listens on a selected primary advertising channel (37, 38, or 39), decodes common advertising PDUs, follows connection requests onto the data channels, and reports raw data PDUs as byte streams.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclBleSniffer` API used |
|------|---------------------------|
| Capture start and stop | `Start(channel)`, `Stop`, `OnStarted`, `OnStopped` |
| Advertising PDU decoding | `OnAdvIndReceived`, `OnAdvDirectIndReceived`, `OnAdvNonConnIndReceived`, `OnAdvScanIndReceived` |
| Scan request / response | `OnScanReqReceived`, `OnScanRspReceived` |
| Connection tracking | `OnConnectIndReceived`, `wclBleSnifferLlData` |
| Raw PDU fallback | `OnRawPacketReceived` |
| Packet metadata | `wclBluetoothLePacketHeader`, `wclBluetoothLeAdvertisingPduHeader` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A **CC2540-based USB dongle** with the **WinUSB** driver installed (see the Driver installation section of the reference article)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Prepare the CC2540 dongle

Replace the default Cebal driver with WinUSB using the Zadig tool, as described at https://www.btframework.com/blesniffer.htm#drivers.

### 2. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `BleSnifferController.cs`.

### 3. Build the demo scene

From the Unity menu select:

> **Tools → BleSniffer → Build Scene**

This operation creates the canvas, the `BleSnifferController` GameObject, wires every UI element, and saves the scene to `Assets/Scenes/BleSniffer.unity`.

### 4. Run the scene

Enter Play mode, pick a channel, and press **Start**. Captured packets are appended to the log with a small delay so that bursts do not stall the UI thread.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Channel** dropdown | Selects the primary advertising channel (37, 38, or 39) |
| **Start** | Calls `wclBleSniffer.Start` on the selected channel |
| **Stop** | Calls `wclBleSniffer.Stop` |
| **Clear** | Empties the packet log |
| **Packet log** | Displays each received PDU: header fields, AdvA, payload bytes, and LLData for `CONNECT_IND` |

---

## Behavioural notes

- **High-volume data.** BLE sniffing generates many frames per second. The demo batches log updates at the end of each frame; production applications should copy the payload and process it on a worker thread.
- **Unknown PDUs.** Data PDUs and any unrecognised advertising PDU are delivered through `OnRawPacketReceived` as a raw byte array. Applications decode them on their own.
- **Channel selection.** Only the three primary advertising channels are valid on start. Once a `CONNECT_IND` is captured the framework automatically follows the connection's channel hopping sequence.
- **Driver requirement.** The dongle must use WinUSB. If the original Cebal driver is still installed, `Start` returns an error.

---

## Project structure

Assets/
├── Scripts/
│ ├── BleSnifferSceneBuilder.cs # Editor: Tools → BleSniffer → Build Scene
│ └── BleSnifferController.cs # Runtime: demo logic
└── Scenes/
└── BleSniffer.unity # Saved by the scene builder


`BleSnifferSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `BleSnifferController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **BLE Sniffer (reference article):** https://www.btframework.com/blesniffer.htm
- **Bluetooth LE Advertisement:** https://www.btframework.com/blebeacon.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.