# BluetoothManager — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclBluetoothManager`, `wclBluetoothRadio`, and `wclPowerEventsMonitor` classes) for managing Bluetooth radios, discovering devices, enumerating paired, connected, and installed devices, and creating virtual COM ports.

This demo is a Unity port of the reference **Bluetooth Manager** sample shipped with the framework and described in the GATT and RFCOMM articles.

---

## Overview

The BluetoothManager demo exercises the full Bluetooth Classic management API. It lists all radios detected by the system, tracks their state through power events, performs device discovery, enumerates paired and connected devices, enumerates the services published by a device, and creates or destroys virtual COM ports for them.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | API used |
|------|-----------|
| Manager lifecycle | `wclBluetoothManager.Open`, `Close`, `Active`, `HandlePairing`, `AfterOpen`, `BeforeClose`, `OnClosed` |
| Radio discovery | `OnStatusChanged`, `Available`, `Plugged`, `ApiName`, `GetAddress`, `GetCod`, `GetHciVersion`, `GetLmpVersion` |
| Power events | `wclPowerEventsMonitor`, `OnPowerStateChanged`, `psSuspend`, `psResume`, `psResumeAutomatic` |
| Device discovery | `Discover`, `OnDiscoveringStarted`, `OnDiscoveringCompleted`, `OnDeviceFound`, `Terminate` |
| Device properties | `GetRemoteName`, `GetRemoteCod`, `GetRemoteDeviceType`, `GetRemotePaired`, `GetRemoteConnectedStatus`, `GetRemoteAddressType`, `GetRemoteRssi` |
| Pairing | `RemotePair`, `RemoteUnpair`, `OnAuthenticationCompleted`, `OnNumericComparison`, `OnPasskeyRequest`, `OnPinRequest`, `OnConfirm`, `OnIoCapabilityRequest`, `OnProtectionLevelRequest` |
| Service enumeration | `EnumRemoteServices`, `EnumInstalledServices` |
| Radio state | `SetConnectable`, `SetDiscoverable`, `SetName`, `TurnOn`, `TurnOff` |
| Virtual COM ports | `EnumComPorts`, `ComPorts`, `CreateComPort`, `DestroyComPort` |
| Device installation | `InstallDevice`, `UninstallDevice`, `IsRemoteDeviceInRange`, `RemoteDisconnect` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- At least one Bluetooth radio
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `BluetoothManagerController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → BluetoothManager → Build Scene**

This creates the canvas, all controls, and the `BluetoothManagerController` GameObject, then saves the scene to `Assets/Scenes/BluetoothManager.unity`.

### 3. Run the scene

Enter Play mode. Press **Open** to open the Bluetooth manager. A list of available radios appears in the top view. Select a radio to enable the per-radio actions (refresh, connectable, discoverable, name, ON/OFF). Press **Discover** to find nearby devices; select one to enable the per-device actions (pair, unpair, services, RSSI, is in range, disconnect). Every operation is logged in the bottom view.

---

## Using the demo UI

The window is divided into five sections:

- **Radios** — driver name, availability, address, connectable, discoverable, HCI version, LMP version, manufacturer, name, COD. Every action that changes a radio's state (refresh, connectable, discoverable, set name, turn on/off, discover, terminate, enum paired, enum connected, enum vCOMs) operates on the selected row.
- **Devices** — driver name, address, COD, device type, name, paired, connected, address type. Every device action (refresh, enum services, enum installed services, pair, unpair, RSSI, is in range, disconnect, install device, uninstall device, create vCOM) operates on the selected row.
- **Services** — driver, address, handle, UUID, channel, name, comment.
- **Virtual COM ports** — driver, address, service UUID, port number.
- **Events log** — one line per framework event.

---

## Behavioural notes

- **Power events.** `wclPowerEventsMonitor` runs a background thread and posts `OnPowerStateChanged` notifications. When the system suspends, the demo closes the manager and remembers that it was open; when it resumes, the manager is reopened.
- **Asynchronous discovery.** `Discover` only starts the procedure. Results are delivered through `OnDeviceFound`; the end of the scan arrives through `OnDiscoveringCompleted`, at which point device names are resolved and every row is refreshed.
- **Pairing events.** `OnConfirm`, `OnNumericComparison`, `OnPasskeyRequest`, `OnPinRequest`, and `OnIoCapabilityRequest` are answered automatically using the values selected in the dropdowns at the top of the form. For interactive pairing, applications would prompt the user instead.
- **Auto power-on.** When "Don't turn OFF" is enabled, the demo attempts to re-enable any plugged-in but unavailable radio by calling `TurnOn` from the status-changed handler.
- **Virtual COM ports.** Creating or destroying a vCOM updates the ports view and the events log. Enumerating "preinstalled" ports calls `EnumComPorts` on the radio; the alternative uses the cached `ComPorts` list.

---

## Project structure

Assets/
├── Scripts/
│ ├── BluetoothManagerSceneBuilder.cs # Editor: Tools → BluetoothManager → Build Scene
│ └── BluetoothManagerController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── BluetoothManager.unity # Saved by the scene builder


`BluetoothManagerSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `BluetoothManagerController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls, since Unity does not provide them.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **BLE GATT Communication:** https://www.btframework.com/gatt.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.