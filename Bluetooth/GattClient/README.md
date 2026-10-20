# GattClient — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclGattClient` class) for discovering Bluetooth LE peripherals, connecting to them, reading services, characteristics, and descriptors, writing characteristic values, subscribing to notifications, and reading per-connection parameters.

This demo is a Unity port of the reference **GATT Client** sample documented at:
**https://www.btframework.com/gatt.htm**

---

## Overview

The GattClient demo implements the full central-side GATT workflow on Windows. It discovers nearby BLE devices, connects to the selected one, reads the device's services, and drills down to characteristics, values, and descriptors. Subscriptions and unsubscriptions are supported with the standard CCCD write, and the demo also exercises the fast `SubscribeForNotifications` and `UnsubscribeFromNotifications` helpers. Per-connection parameters — MTU, interval, latency, timeout, and PHY — are shown and can be changed at run time.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclGattClient` API used |
|------|--------------------------|
| Discovery | `wclBluetoothRadio.Discover(dkBle)`, `OnDeviceFound`, `OnDiscoveringStarted`, `OnDiscoveringCompleted` |
| Connection | `Connect`, `Disconnect`, `OnConnect`, `OnDisconnect`, `Address`, `ConnectOnRead`, `ForceNotifications` |
| Services | `ReadServices`, `ReadIncludedServices`, `wclGattService` |
| Characteristics | `ReadCharacteristics`, `wclGattCharacteristic`, `IsBroadcastable`, `IsReadable`, `IsWritable`, `IsNotifiable`, `IsIndicatable` |
| Values | `ReadCharacteristicValue`, `WriteCharacteristicValue`, `wclGattWriteKind` |
| Descriptors | `ReadDescriptors`, `ReadDescriptorValue`, `wclGattDescriptor`, `wclGattDescriptorValue` |
| Notifications | `Subscribe`, `Unsubscribe`, `SubscribeForNotifications`, `UnsubscribeFromNotifications`, `WriteClientConfiguration`, `OnCharacteristicChanged` |
| Connection parameters | `GetMaxPduSize`, `GetConnectionParams`, `GetConnectionPhyInfo`, `SetConnectionParams`, `OnMaxPduSizeChanged`, `OnConnectionParamsChanged`, `OnConnectionPhyChanged` |
| Pairing | `OnNumericComparison`, `OnPasskeyNotification`, `OnPasskeyRequest`, `OnPinRequest`, `OnConfirm`, `OnIoCapabilityRequest`, `OnProtectionLevelRequest` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 1703 or newer for direct in-app discovery; older versions require pairing the device first through Windows UI
- A Bluetooth LE peripheral (or a development board such as an nRF52 or BLED112)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `GattClientController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → GattClient → Build Scene**

This creates the canvas, all controls, the `GattClientController` GameObject, and saves the scene to `Assets/Scenes/GattClient.unity`.

### 3. Run the scene

Enter Play mode. Click **Discover**, select a BLE device, click **Connect**. Once connected, click **Get services**, select a service, click **Get characteristics**, select a characteristic to read, write, or subscribe.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discover / Terminate / Enum paired** | Device discovery on the LE radio |
| **Pair / Unpair** | Bond management |
| **Connect / Disconnect** | GATT connection |
| **Connect on read** | Sets `ConnectOnRead` |
| **Force indications** | Sets `ForceNotifications` |
| **Get services / Get included services** | Populates the services view |
| **Get characteristics** | Populates the characteristics view for the selected service |
| **Get value / Set value** | Reads or writes the selected characteristic value |
| **Get descriptors / Get value** | Populates the descriptor view and reads the selected descriptor value |
| **Subscribe / Unsubscribe** | Standard `Subscribe` / `Unsubscribe` |
| **Write CCCD subscribe / Write CCCD unsubscribe** | Calls `WriteClientConfiguration` explicitly |
| **Fast subscribe** | When checked, uses `SubscribeForNotifications` / `UnsubscribeFromNotifications` |
| **Max PDU size / Get PHY / Get params** | Reads per-connection parameters |
| **Set params** | Changes the connection interval, latency, and timeout, using either a predefined profile or custom values |
| **Subscribe kind** dropdown | Notification, Indication, or Manual |
| **Write kind** dropdown | With Response, Without Response, Auto Split, or Auto |
| **Protection / Operation flag** dropdowns | Applied to every read, write, subscribe, and CCCD operation |

---

## Behavioural notes

- **Asynchronous connect.** `Connect` only starts the procedure; the result arrives in `OnConnect`. The demo only enables the read/write buttons while the state is `csConnected`.
- **Manual subscribe.** When **Subscribe kind** is set to Manual and the characteristic is both notifiable and indicatable, the demo sets `IsIndicatable = false` before subscribing. This avoids error `0x000510B6` (see the reference article for details).
- **Long value handling.** Reads and writes that exceed the current MTU are automatically split by the framework when the write kind is set to `wkAutoSplit`.
- **ForceNotifications.** For peripherals with a buggy CCCD implementation (for example DFRobot Bluno), enable **Force indications** to bypass the CCCD write.
- **Connection parameters.** `Set params` requests a change; the actual negotiated values are reported through `OnConnectionParamsChanged`. The demo refreshes the display whenever that event fires.

---

## Project structure

Assets/
├── Scripts/
│ ├── GattClientSceneBuilder.cs # Editor: Tools → GattClient → Build Scene
│ └── GattClientController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── GattClient.unity # Saved by the scene builder


`GattClientSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `GattClientController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `MessageBox`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **BLE GATT Communication (reference article):** https://www.btframework.com/gatt.htm
- **GATT Connection State Change Detection:** https://www.btframework.com/gattclientconnection.htm
- **BLE GATT Server:** https://www.btframework.com/gattserver.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.