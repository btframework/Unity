# NetworkListManager — Unity Demo

A Unity example demonstrating the use of the **Wi-Fi Framework** (`wclNetworkListManager` class) for reading the Windows Network List Manager: connectivity state, network category and domain type, network and adapter enumeration, and remote IP settings.

This demo is a Unity port of the reference **Network List Manager** sample shipped with the Wi-Fi Framework.

---

## Overview

The NetworkListManager demo exercises the Windows Network List Manager through the Wi-Fi Framework. It reports the system-wide connectivity state, tracks network and connection change events in real time, enumerates the available networks with their properties, lets the user change the category of a network, and can enumerate both the machine's network adapters and the network interfaces exposed for each connection.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclNetworkListManager` API used |
|------|-----------------------------------|
| Manager lifecycle | `Open`, `Close`, `AfterOpen`, `BeforeClose` |
| Connectivity | `GetConnected`, `GetConnectedToInternet`, `GetConnectivity`, `OnConnectivityChanged` |
| Network enumeration | `GetNetworks`, `GetNetwork`, `wclNlmEnumNetwork`, `OnNetworkAdded`, `OnNetworkDeleted` |
| Network properties | `GetNetworkId`, `GetCategory`, `SetCategory`, `GetConnectivity`, `GetDescription`, `GetDomainType`, `GetName`, `SetName`, `SetDescription`, `GetTimeCreatedAndConnected`, `GetConnected`, `GetConnectedToInternet` |
| Network events | `OnNetworkConnectivityChanged`, `OnNetworkPropertyChanged` |
| Connections | `GetConnections`, `wclNlmConnection` (`GetAdapterId`, `GetId`, `GetConnected`, `GetConnectedToInternet`, `GetConnectivity`, `GetDomainType`) |
| Connection events | `OnConnectionConnectivityChanged`, `OnConnectionPropertyChanged` |
| Adapters | `EnumAdapters`, `wclNetworkAdapter`, `wclNetworkAdapterFlag`, `wclNetworkInterfaceStatus` |
| Network deletion | `DeleteNetwork` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Wi-Fi Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `NetworkListManagerController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → NetworkListManager → Build Scene**

This creates the canvas, all controls, the `NetworkListManagerController` GameObject, and saves the scene to `Assets/Scenes/NetworkListManager.unity`.

### 3. Run the scene

Enter Play mode and click **Open**. From that point on, connectivity and network change events are recorded in the event log. Use **Enum networks** to fill the network list, select a row, and use the action buttons to query or change the network.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Open / Close** | Opens or closes the `wclNetworkListManager` |
| **Is Connected / Is Internet / Connectivity** | Reads the system-wide connectivity state |
| **Network type** dropdown | Selects which networks to enumerate (Connected, Disconnected, All) |
| **Enum networks** | Fills the network list (`GetNetworks`) |
| **Network list** | ID, Category, Connectivity, Description, Domain type, Name, Creation Date Time, Connected Date Time, Connected, Connected To Internet |
| **New name or description** | Used by **Set name** and **Set description** |
| **Set name / Set description** | Renames or re-describes the selected network (requires administrator privileges) |
| **Category** dropdown | Public, Private, Domain Authenticated |
| **Set category** | Changes the category of the selected network |
| **Delete** | Calls `DeleteNetwork` on the selected network |
| **Refresh** | Re-reads the selected network's properties |
| **Get machine connections / Get network connections** | Populates the connections list from `GetConnections` or `Network.GetConnections` |
| **Enum adapters** | Populates the connections list with `EnumAdapters` data (Id, Friendly name, Description, MAC, IPs, DNS suffix, MTU, Flags, Status) |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Dynamic columns in the connections list.** The connections list changes its column set between `GetConnections` (6 columns) and `EnumAdapters` (9 columns). The `ListView` shim supports `Columns.Clear()` and `Columns.Add(name, width)` and rebuilds the header and row cells when the schema changes.
- **Permissions.** `SetName`, `SetDescription`, and `SetCategory` require administrator privileges; on failure the demo reports "Admin privilegies required" (spelling preserved from the original).
- **Adapter flags.** `EnumAdapters` reports `wclNetworkAdapterFlag` bits as a comma-separated list, and `wclNetworkInterfaceStatus` as a readable status word.
- **Event log.** Every `wclNetworkListManager` event is logged with the connection or network GUID and the changed parameter.
- **GUID parsing.** The ID column contains a `Guid` string; other actions reconstruct the GUID with `new Guid(string)`.

---

## Project structure

NetworkListManager/
├── README.md
├── Scripts/
│ ├── NetworkListManagerSceneBuilder.cs # Editor: Tools → NetworkListManager → Build Scene
│ └── NetworkListManagerController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── NetworkListManager.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the `ListView`, `ListBox`, and `MessageBox` UI shims.

---

## Links

- **Wi-Fi Framework (product page):** https://www.btframework.com/wififramework.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Wi-Fi Framework binaries; obtain them separately from the vendor.