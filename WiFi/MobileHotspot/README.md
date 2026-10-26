# MobileHotspot — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclMobileHotspot` class) for starting and stopping a Wi-Fi Direct Mobile Hotspot, reading and changing its SSID and passphrase, selecting the radio band, querying the connected clients and the maximum client count, and toggling the no-connections timeout.

This demo is a Unity port of the reference **Mobile Hotspot** sample shipped with the Wi-Fi Framework.

---

## Overview

The MobileHotspot demo drives a Windows Mobile Hotspot through the Wi-Fi Framework. It opens the hotspot object, starts and stops the tethering interface, reads and writes the SSID and passphrase, switches the radio band between Auto, 2.4 GHz, and 5.8 GHz, reports the maximum and current client counts, lists every connected client with its MAC address and name, and checks or changes the no-connections timeout flag.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclMobileHotspot` API used |
|------|-------------------------------|
| Lifecycle | `Open`, `Close`, `OnOpened`, `OnClosed` |
| State | `Start`, `Stop`, `GetState`, `wclMobileHotspotState` (`mhsUnknown`, `mhsOn`, `mhsOff`, `mhsInTransition`) |
| SSID | `GetSsid`, `SetSsid` |
| Passphrase | `GetPassphrase`, `SetPassphrase` |
| Clients | `GetMaxClientCount`, `GetClientCount`, `GetClients`, `wclMobileHotspotClient` (`Mac`, `Name`) |
| Band | `SetBand`, `GetBand`, `IsBandSupported`, `wclMobileHotspotBand` (`mhbAuto`, `mhbTwoPointFourGigahertz`, `mhbFiveGigahertz`) |
| No-connections timeout | `IsNoConnectionsTimeoutEnabled`, `EnableNoConnectionsTimeout`, `DisableNoConnectionsTimeout` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer with a Wi-Fi Direct capable adapter
- Administrator rights, required by the Windows Mobile Hotspot API
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `MobileHotspotController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → MobileHotspot → Build Scene**

This creates the canvas, all controls, the `MobileHotspotController` GameObject, and saves the scene to `Assets/Scenes/MobileHotspot.unity`.

### 3. Run the scene

Enter Play mode and click **Open**. Once the hotspot is open, **Start** turns the tethering interface on; **Stop** turns it off; **Close** releases the hotspot object. Every operation is reported through a message box and, on failure, an error code is appended in `0xXXXXXXXX` format.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Open / Close** | Opens or closes the `wclMobileHotspot`. `OnOpened` reads the current SSID and passphrase into the input fields; `OnClosed` clears them |
| **Start / Stop** | Turns the mobile hotspot on or off |
| **Get state** | Reads `GetState` and reports Unknown / On / Off / Switching |
| **Get SSID** | Reads `GetSsid` and writes it into the SSID field |
| **Set SSID** | Writes the SSID field into `SetSsid` |
| **Get password** | Reads `GetPassphrase` and writes it into the password field |
| **Set password** | Writes the password field into `SetPassphrase` |
| **Max clients** | Reads `GetMaxClientCount` and reports the limit |
| **Connected** | Reads `GetClientCount` and reports the number of currently connected clients |
| **Band** dropdown | Auto, 2.4 GHz, or 5.8 GHz |
| **Is supported** | Calls `IsBandSupported` for the selected band and reports the result |
| **Set band / Get band** | Writes `SetBand` or reads `GetBand` for the selected band |
| **Connected clients** | Calls `GetClients` and lists each client's MAC and name in the clients list |
| **Check timeout / Enable timeout / Disable timeout** | Reads or changes the no-connections timeout flag |

---

## Behavioural notes

- **State machine.** The hotspot must be `Open` before `Start` / `Stop` / `GetState` / `GetSsid` / `GetPassphrase` / `GetBand` succeed; the demo relies on the framework's error codes to detect misuse and reports them as `0xXXXXXXXX`. This matches the reference sample.
- **OnOpened and OnClosed.** `OnOpened` copies the current SSID and passphrase into the input fields so the user sees the effective values immediately. `OnClosed` clears both fields. Both events are delivered through the standard dispatch queue.
- **Band reporting.** `Get band` decodes `wclMobileHotspotBand` into a readable label ("Auto", "2.4", "5.8") and reports it through a message box. `Is supported` reports "Band is supported" or "Band is NOT supported" for the band selected in the dropdown.
- **Connected clients.** The clients list has two columns (MAC, Name) and is rebuilt on every **Connected clients** press. If `GetClients` returns an empty array, the list is cleared and only an error is reported if the API call itself failed.
- **User-visible feedback.** Every operation reports its result through a native Win32 message box (the `MessageBox` shim in this demo file). Errors additionally include the formatted error code. This mirrors the reference sample's use of `MessageBox.Show` for informational output.
- **Dropdown sizing.** `TMP_Dropdown` is adjusted in `Update` so that the last option in the band popup is fully visible, matching the WinForms `ComboBox` behaviour.
- **Cleanup.** `Close` is called in `OnDestroy`, wrapped in try/catch, matching the reference `FormClosed` handler.

---

## Project structure

MobileHotspot/
├── README.md
├── Scripts/
│ ├── MobileHotspotSceneBuilder.cs # Editor: Tools → MobileHotspot → Build Scene
│ └── MobileHotspotController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── MobileHotspot.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView` and `MessageBox` UI shims defined in the same file.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.