# WiFiDirect — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclWiFiDirectAdvertiser` class) for advertising a Wi-Fi Direct device, accepting incoming connections, and handling the pairing workflow.

This demo is a Unity port of the reference **WiFi Direct** sample shipped with the Wi-Fi Framework.

---

## Overview

The WiFiDirect demo starts a Wi-Fi Direct advertiser that can be discovered by other Wi-Fi Direct devices. When a remote device initiates a connection, the advertiser asks the user whether to accept the device, runs the pairing procedure selected in the UI (invitation or group-owner negotiation), and reports the connection result. The advertiser also handles PIN-based configuration methods and can force-disconnect an already-connected device.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclWiFiDirectAdvertiser` API used |
|------|--------------------------------------|
| Lifecycle | `Start`, `Stop`, `Active`, `OnStarted`, `OnStopped` |
| Advertisement configuration | `Discoverability` (`adIntensive`, `adNone`, `adNormal`), `AutonomousGroupOwnerEnabled` |
| Connection events | `OnDeviceConnected`, `OnDeviceDisconnected`, `OnDeviceConnectError` |
| Pairing | `OnAcceptDevice`, `OnPairGetParams`, `OnPairConfirm`, `OnPairDisplayPin`, `OnPairProvidePin`, `OnPairCompleted` |
| Device collection | `Count`, `this[i]`, `wclWiFiDirectDevice.Disconnect` |
| Device information | `wclWiFiDirectDevice.Id`, `Name`, `LocalAddress`, `RemoteAddress` |
| Pairing configuration | `wclWiFiDirectConfigurationMethod` (`cmDisplayPin`, `cmProvidePin`, `cmPushButton`), `wclWiFiDirectPairingProcedure` (`ppInvitation`, `ppGroupOwnerNegotiation`) |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- A Wi-Fi Direct capable adapter
- Administrator rights may be required for some pairing methods
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `WiFiDirectController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → WiFiDirect → Build Scene**

This creates the canvas, all controls, the `WiFiDirectController` GameObject, and saves the scene to `Assets/Scenes/WiFiDirect.unity`.

### 3. Run the scene

Enter Play mode and click **Start**. The advertiser begins broadcasting. When a remote Wi-Fi Direct device initiates a connection, the advertiser prompts for acceptance, asks for pairing parameters, and runs the selected pairing procedure. Connected devices appear in the device list.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discoverability** dropdown | `adIntensive`, `adNone`, `adNormal` |
| **Autonomous Group Owner** | `AutonomousGroupOwnerEnabled` |
| **Pairing procedure** dropdown | `ppInvitation` or `ppGroupOwnerNegotiation` |
| **Group owner intent** | Group owner intent value passed to `OnPairGetParams` |
| **Display PIN / Provide PIN / Push Button** | Configuration methods advertised during pairing |
| **Start / Stop** | Starts or stops the advertiser |
| **Device list** | Device name, ID, status (Connecting / Connected) |
| **Disconnect** | Force-disconnects the selected device |
| **Clear** | Empties the log |
| **Log** | Events, pairing results, connection status, and IP addresses |

---

## Behavioural notes

- **Acceptance dialog.** `OnAcceptDevice` shows a native Yes/No message box. The device is only added to the list if the user accepts.
- **Pairing parameters.** `OnPairGetParams` reads the pairing procedure, group owner intent, and configuration-method checkboxes from the UI at the moment the framework asks for them. This matches the reference sample, where the user sets those parameters before starting the advertiser.
- **Display PIN and Provide PIN.** `OnPairDisplayPin` shows the PIN in a message box for the user to enter on the remote device. `OnPairProvidePin` normally opens an input dialog; because Unity has no blocking input dialog, the demo returns a fixed PIN. Push-button pairing is enabled by default, so this path is rarely taken.
- **Connection result.** `OnDeviceConnected` records "Connected" in the device list and logs the local and remote IP addresses. `OnDeviceDisconnected` removes the device from the list. `OnDeviceConnectError` logs the error without removing the row so the user can retry or disconnect explicitly.
- **Disconnect.** The Disconnect button iterates `wclWiFiDirectAdvertiser.Count` and calls `Disconnect()` on the matching `wclWiFiDirectDevice`. This mirrors the reference sample.
- **Shutdown.** `Stop` is called in `OnDestroy`, wrapped in try/catch, matching the reference `FormClosed` handler.

---

## Project structure

WiFiDirect/
├── README.md
├── Scripts/
│ ├── WiFiDirectSceneBuilder.cs # Editor: Tools → WiFiDirect → Build Scene
│ └── WiFiDirectController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── WiFiDirect.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView`, `ListBox`, and `MessageBox` UI shims.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.