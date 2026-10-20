# GattServer — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclGattServer` class) for building a Bluetooth LE peripheral: defining services and characteristics, answering read and write requests, sending notifications and indications, and controlling per-client connection parameters.

This demo is a Unity port of the reference **GATT Server** sample documented at:
**https://www.btframework.com/gattserver.htm**

---

## Overview

The GattServer demo starts a BLE peripheral that exposes a single service (`0xFFF0`) with six characteristics (empty, short readable, long readable, writable, notifiable, and indicatable). Client connections and disconnections are delivered as events. Read and write requests are answered per request, with the long characteristic demonstrating how a value larger than the current MTU is delivered in chunks. Notifications and indications can be sent either to a single connected client or to every subscribed client, with a configurable repeat count.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclGattServer` API used |
|------|---------------------------|
| Lifecycle | `Initialize`, `Uninitialize`, `Start`, `Stop`, `Initialized`, `OnStarted`, `OnStopped` |
| Services | `AddService`, `wclGattLocalService`, `wclGattUuid` |
| Characteristics | `AddCharacteristic`, `wclGattLocalCharacteristic`, `wclGattLocalCharacteristicParameters`, `wclGattLocalCharacteristicProperty` |
| Client handling | `OnClientConnected`, `OnClientDisconnected`, `wclGattServerClient` |
| Read requests | `OnRead`, `wclGattLocalCharacteristicReadRequest`, `Respond`, `RespondWithError` |
| Write requests | `OnWrite`, `wclGattLocalCharacteristicWriteRequest`, `Respond`, `RespondWithError` |
| Subscriptions | `OnSubscribed`, `OnUnsubscribed` |
| Notifications | `Notify(data)`, `Notify(address, data)` |
| Connection parameters | `GetMaxPduSize`, `GetConnectionParams`, `GetConnectionPhyInfo`, `GetMaxNotificationSize`, `SetConnectionParams`, `OnMaxPduSizeChanged`, `OnConnectionParamsChanged`, `OnConnectionPhyChanged`, `OnNotificationSizeChanged` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 version 1703 or newer with the Microsoft Bluetooth driver, or a **BLED112** USB dongle for older Windows versions
- A Bluetooth LE central (a phone, a PC, or another development board)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `GattServerController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → GattServer → Build Scene**

This creates the canvas, all controls, the `GattServerController` GameObject, and saves the scene to `Assets/Scenes/GattServer.unity`.

### 3. Run the scene

Enter Play mode and click **Start**. The server begins advertising. Connect from any BLE central; the client appears in the top view. When the central subscribes to characteristic `0xFFF5` or `0xFFF6`, a sub-row appears under the client row. Use **Notify** or **Notify All** to send the data shown in the data field, with the repeat count shown in the repeats field.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Start / Stop** | Starts or stops the server and the underlying radio |
| **Disconnect** | Force-disconnects the selected client |
| **Client list** | Top-level rows are clients; indented rows are their active subscriptions |
| **Data / Repeats** | Hex data and repeat count used by **Notify** and **Notify All** |
| **Notify** | Sends the data to the client associated with the selected subscription row, via the address-based overload |
| **Noify All** | Sends the data to every subscribed client, via the broadcast overload |
| **Params / Set parameters** | Changes the connection interval, latency, and timeout for the selected client, using a predefined profile or custom values |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Server lifecycle.** `Initialize` binds the server to the LE radio returned by `GetLeRadio`. A successful `Start` triggers `OnStarted` and switches the buttons' enabled state.
- **Read request offset.** The long readable characteristic returns the remainder of a 1024-byte buffer starting at `Request.Offset`. This is how a client that reads the value in chunks using the maximum PDU size receives the whole value.
- **Write request response.** When `Request.WithResponse` is `true`, the demo calls `Respond`. For write-without-response requests, no response is sent.
- **Subscription tracking.** When a client subscribes, a sub-row is inserted under the client with the characteristic's UUID. When the client unsubscribes, the sub-row is removed. The framework calls `OnUnsubscribed` before `OnClientDisconnected`, so a disconnection never leaves a stale sub-row.
- **Notification size.** `GetMaxNotificationSize` returns the largest value the client will accept in a single notification. The demo refreshes this value whenever `OnNotificationSizeChanged` fires.

---

## Project structure

Assets/
├── Scripts/
│ ├── GattServerSceneBuilder.cs # Editor: Tools → GattServer → Build Scene
│ └── GattServerController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── GattServer.unity # Saved by the scene builder


`GattServerSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `GattServerController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **BLE GATT Server (reference article):** https://www.btframework.com/gattserver.htm
- **BLE GATT Communication:** https://www.btframework.com/gatt.htm
- **GATT Connection State Change Detection:** https://www.btframework.com/gattclientconnection.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.