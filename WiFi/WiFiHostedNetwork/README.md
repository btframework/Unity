# WiFiHostedNetwork — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclWiFiHostedNetwork` class) for reading and changing the Windows Wireless Hosted Network configuration: SSID, peer limit, key, security settings, hosted-network state, and peer status.

This demo is a Unity port of the reference **WiFi Hosted Network** sample shipped with the Wi-Fi Framework.

---

## Overview

The WiFiHostedNetwork demo wraps the Windows Wireless Hosted Network API. Every getter and setter of `wclWiFiHostedNetwork` is exercised: connection settings, key (passphrase or raw key), profile XML, security settings, hosted-network state, per-peer status with local and remote IP addresses, and the `Restart` and `RefreshSecuritySettings` operations. Results are written into a two-column list (Parameter name / Parameter value) which is cleared before each action.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclWiFiHostedNetwork` API used |
|------|------------------------------------|
| Lifecycle | `Open`, `Close`, `AfterOpen`, `BeforeClose` |
| Connection settings | `GetConnectionSettings`, `SetConnectionSettings`, `wclWiFiHostedNetworkConnectionSettings` |
| Key | `GetKey`, `SetKey` |
| Profile | `GetProfile` |
| Security | `GetSecuritySettings`, `RefreshSecuritySettings`, `wclWiFiAuthCipherPair` |
| State | `GetState`, `SetState` |
| Status | `GetStatus`, `wclWiFiHostedNetworkStatus`, peer MAC, auth state, `GetLocalIp`, `GetRemoteIp` |
| Restart | `Restart` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer. Hosted Network requires the driver to support the `netsh wlan` hosted network feature.
- Administrator rights
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `WiFiHostedNetworkController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → WiFiHostedNetwork → Build Scene**

This creates the canvas, all controls, the `WiFiHostedNetworkController` GameObject, and saves the scene to `Assets/Scenes/WiFiHostedNetwork.unity`.

### 3. Run the scene

Enter Play mode and click **Open**. An "opened" message box appears. From that point on, every Get / Set / Enable / Disable button clears the data list and writes the result of the operation, or a formatted error code if the operation fails.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Open / Close** | Opens or closes the hosted network object; both fire message boxes on the `AfterOpen` and `BeforeClose` events |
| **Get Connection Settings** | Reads SSID and maximum number of peers |
| **Get Key** | Reads the current key; passphrase is decoded as ASCII, raw key is hex-formatted |
| **Get profile** | Reads the hosted-network profile XML |
| **Get security settings** | Reads the auth algorithm and cipher algorithm |
| **Get status** | Reads the current state, interface, BSS ID, PHY, channel frequency, local IP, and for each peer the MAC, remote IP, and auth state |
| **Get state** | Reads the enabled / disabled flag |
| **Refresh security settings** | Calls `RefreshSecuritySettings` |
| **Restart** | Calls `Restart` |
| **Set settings** | Applies SSID and maximum number of peers from the input fields |
| **Set key** | Applies the key from the input field; `IsPassPhrase = true`, `Persistent` from the checkbox |
| **Enable / Disable** | Enables or disables the hosted network |
| **Clear** | Empties the data list |

---

## Behavioural notes

- **Lifecycle message boxes.** `AfterOpen` shows "WiFi Hosted Network opened" and `BeforeClose` shows "WiFi Hosted Network closing". In the Unity port these are delivered through the dispatch queue and shown as native message boxes.
- **Key length and data.** `GetKey` reports the raw key length, the key data (as an ASCII string when it is a passphrase, hex otherwise), the `IsPassPhrase` flag, and the `Persistent` flag.
- **Peer enumeration.** `GetStatus` iterates `Status.Peers`, and for each peer it logs the MAC, the remote IP obtained via `GetRemoteIp`, and the peer's authentication state. The local IP is read once for the hosted network as a whole via `GetLocalIp`.
- **Key encoding on set.** `SetKey` sends the ASCII bytes of the key plus a single trailing null terminator, exactly as the reference sample does. This is required by the underlying Windows API and the demo preserves it.
- **Data list.** The list is cleared before every action, so at any point it displays only the result of the most recent operation.

---

## Project structure

WiFiHostedNetwork/
├── README.md
├── Scripts/
│ ├── WiFiHostedNetworkSceneBuilder.cs # Editor: Tools → WiFiHostedNetwork → Build Scene
│ └── WiFiHostedNetworkController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── WiFiHostedNetwork.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView` and `MessageBox` UI shims.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.