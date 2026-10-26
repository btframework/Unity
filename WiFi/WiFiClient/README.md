# WiFiClient — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclWiFiClient`, `wclWiFiProfilesManager`, and `wclWiFiInterface` classes) for enumerating wireless interfaces, discovering and connecting to networks, managing wireless profiles, configuring IP settings, and toggling the radio and Dual-STA mode.

This demo is a Unity port of the reference **WiFi Client** sample shipped with the Wi-Fi Framework.

---

## Overview

The WiFiClient demo exercises the full client-side Wi-Fi API. It lists the wireless interfaces detected by the system, enumerates available and hidden networks and BSS entries, manages profiles through `wclWiFiProfilesManager`, connects to a network using any of four connection modes, controls the interface's IP configuration (DHCP, static, current IP, stored IP settings), turns the radio on and off, and toggles Dual-STA.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclWiFiClient` / `wclWiFiProfilesManager` / `wclWiFiInterface` API used |
|------|---------------------------------------------------------------------------|
| Client lifecycle | `Open`, `Close`, `AfterOpen`, `BeforeClose` |
| Interface enumeration | `EnumInterfaces`, `wclWiFiInterfaceData` |
| Networks | `EnumAvailableNetworks`, `wclWiFiAvailableNetworkFilter` |
| BSS | `EnumBss`, `wclWiFiBss`, `wclWiFiBssType` |
| Scanning | `Scan` |
| Connections | `Connect` (`cmProfile`, `cmDiscoverySecure`, `cmDiscoveryUnsecure`, `cmTemporaryProfile`), `Disconnect` |
| Profile management | `GetProfileList`, `GetProfile`, `SetProfile`, `DeleteProfile`, `RenameProfile`, `GetProfile` with `wclWiFiProfileFlag.pfGetPlaintextKey`, `SetProfileEapXmlUserData`, `ShowUIEdit`, `SsidToHex` |
| ACM settings | `AcmGetShowDeniedNetworks`, `AcmSetShowDeniedNetworks`, `AcmGetPowerSetting`, `AcmGetOnlyUseGpProfiles`, `AcmGetAllowExplicitCreds`, `AcmSetAllowExplicitCreds`, `AcmGetBlockPeriod`, `AcmSetBlockPeriod`, `AcmGetVirtualStationExtensibility`, `AcmSetVirtualStationExtensibility` |
| Interface state | `wclWiFiInterface.Open`, `Close`, `GetState`, `TurnOn`, `TurnOff`, `GetCurrentIp`, `GetIpSettings`, `EnableDhcp`, `SetStaticIp`, `GetSecondarySta`, `SetSecondarySta`, `EnumInterfaces` |
| Wireless service | `wclWiFiClient.InterfaceState`, `wclWiFiInterfaceState` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- An 802.11 wireless adapter installed and enabled
- Administrator rights for profile operations, ACM settings, and static IP configuration
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `WiFiClientController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → WiFiClient → Build Scene**

This creates the canvas, all controls, the `WiFiClientController` GameObject, and saves the scene to `Assets/Scenes/WiFiClient.unity`.

### 3. Run the scene

Enter Play mode and click **Open**. This enables every other control. Click **Enum interfaces**, select an interface, and then use the network, BSS, profile, IP, or ACM controls to exercise the API.

---

## Using the demo UI

### Row 1 – client and interface

| Control | Function |
|---------|----------|
| **Open / Close** | Opens or closes the `wclWiFiClient` |
| **Enum interfaces** | Lists all wireless interfaces |
| **Disconnect selected interface** | Calls `Client.Disconnect(Id)` |
| **Get interface state** | Opens a temporary `wclWiFiInterface` and reads `GetState` |
| **Turn ON / Turn OFF** | Switches the interface on or off |
| **Current IP / Get IP settings / Enable DHCP / Enable static IP** | Reads or changes the interface's IP configuration |
| **Get dual STA state / Enable dual STA / Disable dual STA** | Reads or toggles Dual-STA |
| **Enum sec. interfaces** | Enumerates the interfaces exposed by `wclWiFiInterface.EnumInterfaces` |

### Networks and BSS

| Control | Function |
|---------|----------|
| **Include Ad Hoc profiles / Include hidden profiles** | Filters for `EnumAvailableNetworks` |
| **Enum networks** | Lists available networks with all their properties |
| **Scan / SSID** | Calls `Client.Scan(Id, Ssid)` |
| **Enum BSS / Use SSID / BSS type** | Lists BSS entries; if Use SSID is checked, the SSID, security flag, and BSS type are taken from the selected network |

### Profiles

| Control | Function |
|---------|----------|
| **Enum profiles** | Lists the wireless profiles on the selected interface |
| **Delete profile / Rename profile** | Deletes or renames the selected profile (`NEW_<name>`) |
| **Get profile XML** | Saves the profile XML; the **Get passphrase as plain text** checkbox sets `wclWiFiProfileFlag.pfGetPlaintextKey` |
| **Set profile XML** | Loads a profile from an XML file |
| **Set XML user data** | Calls `SetProfileEapXmlUserData` |
| **Show Edit UI** | Calls `ProfilesManager.ShowUIEdit` |

### Connect

| Control | Function |
|---------|----------|
| **Connect** | Uses the mode selected in the adjacent dropdown: network profile, selected profile, selected network (discovery), or temporary profile |
| **Passkey** | Used only by the temporary-profile mode |
| **Discover secure** | Uses `cmDiscoverySecure` instead of `cmDiscoveryUnsecure` |
| **Use BSS MAC** | When checked, the BSS MAC from the BSS list is passed to `Connect` |

### ACM Settings

Each pair of buttons corresponds to one `wclWiFiClient.Acm...` getter/setter pair. **Set** operations prompt for Yes/No and then call the setter.

---

## Behavioural notes

- **Profile XML uses the Win32 API.** `GetProfile` returns the raw XML string; the demo writes it to a file chosen by the user via `SaveFileDialog`. `SetProfile` reads the XML from a file chosen via `OpenFileDialog`.
- **Temporary profile.** In the temporary-profile connection mode, the demo builds a WLANProfile XML from the selected network's authentication and cipher algorithms (mapped by name), the SSID, and the passkey. This matches the reference sample exactly, including the mapping table and the passphrase encoding.
- **Interface state.** `Get interface state` and the Dual-STA controls create a temporary `wclWiFiInterface` for the selected interface, open it, call the required method, and close it. `Client` is used for everything else.
- **ACM settings.** Every `AcmSet...` method prompts with a Yes/No dialog before applying the change. This mirrors the reference sample and protects against accidental changes that require elevated privileges.
- **Profile listing.** The profiles list is populated from `ProfilesManager.GetProfileList(Id)` and is refreshed automatically after delete or rename.
- **Error reporting.** Every API call is checked with `ShowResult(Int32)`, which displays a message box with the formatted error code on failure.

---

## Project structure

WiFiClient/
├── README.md
├── Scripts/
│ ├── WiFiClientSceneBuilder.cs # Editor: Tools → WiFiClient → Build Scene
│ └── WiFiClientController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── WiFiClient.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView`, `MessageBox`, `OpenFileDialog`, and `SaveFileDialog` UI shims.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.
