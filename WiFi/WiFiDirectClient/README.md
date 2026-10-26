# WiFiDirectClient — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclWiFiDirectDeviceWatcher` and `wclWiFiDirectClient` classes) for discovering Wi-Fi Direct devices, connecting to one of them, and managing the pairing workflow.

This demo is a Unity port of the reference **WiFi Direct Client** sample shipped with the Wi-Fi Framework.

---

## Overview

The WiFiDirectClient demo watches for nearby Wi-Fi Direct devices, lists them with their IDs and names, and connects to the selected one. The pairing workflow is driven by the same configuration options as the WiFiDirect demo: pairing procedure (invitation or group-owner negotiation), group owner intent, and configuration methods (Display PIN, Provide PIN, Push Button). Pairing events, connection results, local and remote IP addresses, and disconnection reasons are all logged. The demo also exposes Is Paired, Unpair, and Enum paired actions through `wclWiFiDirectDeviceWatcher`.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclWiFiDirectDeviceWatcher` / `wclWiFiDirectClient` API used |
|------|-------------------------------------------------------------------|
| Watcher lifecycle | `Discover`, `Terminate`, `OnDiscoveringStarted`, `OnDiscoveringCompleted`, `OnDeviceFound` |
| Pairing state | `IsPaired`, `EnumPairedDevices`, `Unpair` |
| Client lifecycle | `Connect`, `Disconnect`, `OnDeviceConnected`, `OnDeviceDisconnected` |
| Pairing | `OnPairGetParams`, `OnPairConfirm`, `OnPairDisplayPin`, `OnPairProvidePin`, `OnPairCompleted` |
| Device info | `wclWiFiDirectDevice.Id`, `Name` |
| Connection info | `wclWiFiDirectClient.LocalAddress`, `RemoteAddress` |
| Pairing configuration | `wclWiFiDirectConfigurationMethod` (`cmDisplayPin`, `cmProvidePin`, `cmPushButton`), `wclWiFiDirectPairingProcedure` (`ppInvitation`, `ppGroupOwnerNegotiation`) |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- A Wi-Fi Direct capable adapter
- A remote Wi-Fi Direct device to test against
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `WiFiDirectClientController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → WiFiDirectClient → Build Scene**

This creates the canvas, all controls, the `WiFiDirectClientController` GameObject, and saves the scene to `Assets/Scenes/WiFiDirectClient.unity`.

### 3. Run the scene

Enter Play mode and click **Discover**. Nearby Wi-Fi Direct devices appear in the list. Select one and click **Connect**. The pairing procedure is driven by the parameters chosen in the UI, the connection result and IP addresses are logged, and disconnection is reported in the log.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discover / Terminate** | Starts or stops the device watcher |
| **Enum paired** | Lists already-paired Wi-Fi Direct devices |
| **Is Paired** | Reads `Watcher.IsPaired(Id, out IsPaired)` |
| **Unpair** | Calls `Watcher.Unpair(Id)` |
| **Device list** | Device name and ID; single-select |
| **Connect / Disconnect** | Connects to or disconnects from the selected device |
| **Group owner intent** | Value passed to `OnPairGetParams` |
| **Pairing procedure** dropdown | `ppInvitation` or `ppGroupOwnerNegotiation` |
| **Display PIN / Provide PIN / Push Button** | Configuration methods advertised during pairing |
| **Log** | Discovering, pairing, connection, and error events |
| **Clear** | Empties the log |

---

## Behavioural notes

- **Is Paired and Unpair.** Both operations are performed by `wclWiFiDirectDeviceWatcher`, not by the client. This matches the reference sample, where the watcher also manages pairing state for devices it has seen.
- **Enum paired.** `EnumPairedDevices` returns the list of paired device IDs. The list is cleared before it is repopulated, so a fresh enumeration shows only currently paired devices.
- **Pairing parameters.** `OnPairGetParams` reads the pairing procedure, group owner intent, and configuration-method checkboxes from the UI at the moment the framework asks for them. Push-button pairing is enabled by default, so PIN-based methods are only used when explicitly enabled.
- **Provide PIN.** `OnPairProvidePin` normally opens an input dialog. Because Unity has no blocking input dialog, the demo returns a fixed PIN. This path is rarely taken with push-button pairing enabled.
- **Pairing confirmation.** `OnPairConfirm` shows a native Yes/No message box, blocking, matching the reference sample.
- **Connection and disconnection.** `OnDeviceConnected` logs the result code and, on success, the local and remote IP addresses. `OnDeviceDisconnected` logs only the reason code, matching the reference sample.
- **Shutdown.** `Client.Disconnect` and `Watcher.Terminate` are called in `OnDestroy`, wrapped in try/catch, matching the reference `FormClosed` handler.

---

## Project structure

WiFiDirectClient/
├── README.md
├── Scripts/
│ ├── WiFiDirectClientSceneBuilder.cs # Editor: Tools → WiFiDirectClient → Build Scene
│ └── WiFiDirectClientController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── WiFiDirectClient.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView`, `ListBox`, and `MessageBox` UI shims.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.