# WiFiEvents — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclWiFiEvents` class) for receiving the complete set of wireless events: ACM notifications, hosted-network and MSM notifications, 802.1X authentication updates, IP changes, and system power state changes.

This demo is a Unity port of the reference **WiFi Events** sample shipped with the Wi-Fi Framework.

---

## Overview

The WiFiEvents demo logs every event delivered by `wclWiFiEvents` and by `wclPowerEventsMonitor` into a single list view. The list has four columns (Interface ID, Event name, Parameter name, Parameter value) and grows in real time as the user interacts with the wireless subsystem. All ACM, hosted-network, MSM, 802.1X, IP, and power events are hooked.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclWiFiEvents` / `wclPowerEventsMonitor` API used |
|------|-------------------------------------------------------|
| Manager lifecycle | `Open`, `Close`, `AfterOpen`, `BeforeClose` |
| ACM notifications | `OnAcmAdHocNetworkStateChange`, `OnAcmAutoconfDisabled`, `OnAcmAutoconfEnabled`, `OnAcmBackgroundScanDisabled`, `OnAcmBackgroundScanEnabled`, `OnAcmBssTypeChange`, `OnAcmConnectionAttemptFail`, `OnAcmConnectionComplete`, `OnAcmConnectionStart`, `OnAcmDisconnected`, `OnAcmDisconnecting`, `OnAcmFilterListChange`, `OnAcmInterfaceArrival`, `OnAcmInterfaceRemoval`, `OnAcmNetworkAvailable`, `OnAcmNetworkNotAvailable`, `OnAcmOperationalStateChange`, `OnAcmPowerSettingChange`, `OnAcmProfileBlocked`, `OnAcmProfileChange`, `OnAcmProfileNameChange`, `OnAcmProfilesExhausted`, `OnAcmProfileUnblocked`, `OnAcmScanComplete`, `OnAcmScanFail`, `OnAcmScanListRefresh`, `OnAcmScreenPowerChange` |
| Hosted network | `OnHostedNetworkPeerStateChange`, `OnHostedNetworkRadioStateChange`, `OnHostedNetworkStateChange` |
| MSM notifications | `OnMsmAdapterOperationModeChange`, `OnMsmAdapterRemoval`, `OnMsmAssociated`, `OnMsmAssociating`, `OnMsmAuthenticating`, `OnMsmConnected`, `OnMsmDisassociating`, `OnMsmDisconnected`, `OnMsmPeerJoin`, `OnMsmPeerLeave`, `OnMsmRadioStateChange`, `OnMsmRoamingEnd`, `OnMsmRoamingStart`, `OnMsmSignalQualityChange`, `OnMsmLinkDegraded`, `OnMsmLinkImproved` |
| 802.1X | `OnOneXAuthRestarted`, `OnOneXAuthUpdate` (`wclWiFiOneXStatusUpdate`) |
| IP changes | `OnIpChanged` (`wclWiFiIpSettings` old and new) |
| Power events | `wclPowerEventsMonitor.Open`, `Close`, `OnPowerStateChanged`, `wclPowerState` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- An 802.11 wireless adapter installed and enabled
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `WiFiEventsController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → WiFiEvents → Build Scene**

This creates the canvas, all controls, the `WiFiEventsController` GameObject, and saves the scene to `Assets/Scenes/WiFiEvents.unity`.

### 3. Run the scene

Enter Play mode and click **Open**. From that point on, every event delivered by the framework is appended to the events list in real time. The power monitor is opened automatically and will report system suspend/resume events.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Open** | Calls `WiFiEvents.Open()` |
| **Close** | Calls `WiFiEvents.Close()` |
| **Clear** | Empties the events list |
| **Events list** | Interface ID, Event name, Parameter name, Parameter value |

Each event may produce one or more rows. Connection-related MSM and ACM events emit several rows (ConnectionMode, Profile name, SSID, BSS type, MAC, Security enabled, First peer, Last peer, Reason). `OneXAuthUpdate` emits a large, fixed sequence of rows covering every field of `wclWiFiOneXStatusUpdate`.

---

## Behavioural notes

- **Event flood.** The demo receives every event the framework delivers. If a Wi-Fi scan is running or a network is coming up, the events list can grow rapidly. **Clear** is provided to reset the list without touching the framework.
- **OneXAuthUpdate detail.** `OnOneXAuthUpdate` expands `wclWiFiOneXStatusUpdate` into a fixed list of rows: authentication status, backend support, supplicant and authentication modes, timeouts, auth identity, quarantine state, session ID, identity, username, domain, EAP error fields, and EAP method type fields. The expansion is 1:1 with the reference sample.
- **MSM and ACM connection events.** Both event families share a `wclWiFi*ConnectionEventData` structure; the demo has one `TraceMsmConnectionEvent` and one `TraceAcmConnectionEvent` helper that write all fields in a fixed order.
- **Power events.** `wclPowerEventsMonitor` runs on its own thread and posts `OnPowerStateChanged` events. The demo opens the monitor in `Start` and closes it in `OnDestroy`.
- **Isolation from the framework.** `out` and `ref` parameters are set synchronously inside the event handler; the UI update is queued to the main thread through the standard dispatch mechanism used by every demo.

---

## Project structure

WiFiEvents/
├── README.md
├── Scripts/
│ ├── WiFiEventsSceneBuilder.cs # Editor: Tools → WiFiEvents → Build Scene
│ └── WiFiEventsController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── WiFiEvents.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView` and `MessageBox` UI shims.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.html

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.