# Wi-Fi Framework — Unity Examples

A collection of Unity examples demonstrating the **[Wi-Fi Framework](https://www.btframework.com/wififramework.htm)** on Windows. Each example is a self-contained Unity project that reproduces one sample from the framework documentation using the same public API, the same control layout, and the same user-visible behaviour as the original WinForms reference sample.

The examples are independent. Every folder contains its own `README.md` with the reference article link, API coverage, requirements, and behavioural notes for that particular sample.

---

## Overview

Each example follows the same structure:

- A **scene builder** editor script (`<Name>SceneBuilder.cs`, wrapped in `#if UNITY_EDITOR`) that creates the canvas, all controls, and the controller object programmatically, wires every UI element to its corresponding controller field, and saves the scene to `Assets/Scenes/<Name>.unity`. There are no hand-authored scenes or prefabs to maintain.
- A **runtime controller** (`<Name>Controller.cs`, a plain `MonoBehaviour`) that reproduces the reference sample's logic 1:1 — event handlers, error formatting, trace strings, and per-API call order — using only the public Wi-Fi Framework API.
- A **UI shim** in the same controller file for the WinForms controls that Unity does not provide (`ListView`, `ListBox`, `MessageBox`, `ProgressBar`, `OpenFileDialog`, `SaveFileDialog`, `Slider`). These are plain C# classes, not `MonoBehaviour`, and emulate the equivalent WinForms behaviour closely enough that the controller code can be written as if it were running inside a WinForms form.

Native calls into the Wi-Fi Framework are performed from the main thread inside `Update` through a small dispatch queue. This avoids the thread-affinity and COM-apartment problems that can occur when the framework is called directly from a Unity UI event handler.

---

## Examples

| Example | Framework classes | What it demonstrates |
|---------|-------------------|----------------------|
| **NetworkListManager** | `wclNetworkListManager` | System connectivity state, network enumeration and properties, adapter enumeration, remote IP settings |
| **WiFiClient** | `wclWiFiClient`, `wclWiFiProfilesManager`, `wclWiFiInterface` | Interface enumeration, network and BSS discovery, profile management, four connection modes, IP configuration, Dual-STA |
| **WiFiEvents** | `wclWiFiEvents`, `wclPowerEventsMonitor` | Full event stream: ACM, hosted network, MSM, 802.1X, IP changes, power state changes |
| **WiFiDirect** | `wclWiFiDirectAdvertiser` | Advertising a Wi-Fi Direct device, accepting connections, running the pairing workflow |
| **WiFiDirectClient** | `wclWiFiDirectDeviceWatcher`, `wclWiFiDirectClient` | Discovering Wi-Fi Direct devices, connecting to one, pairing, managing paired devices |
| **WiFiDirectSoftAP** | `wclWiFiSoftAP` | Legacy Soft AP lifecycle, device acceptance, connection logging, IP settings |
| **WiFiHostedNetwork** | `wclWiFiHostedNetwork` | Hosted network configuration, key management, security settings, per-peer status |
| **MobileHotspot** | `wclMobileHotspot` | Wi-Fi Direct Mobile Hotspot: state, SSID, passphrase, band selection, connected clients, no-connections timeout |

Every example has a dedicated `README.md` next to its scripts describing the API coverage, requirements, and behavioural notes for that specific sample.

---

## Requirements

- **Unity** 2021.3 LTS or newer. TextMeshPro and the Editor API are used by every scene builder.
- **Wi-Fi Framework** installed and referenced by the project.
  - Product page: https://www.btframework.com/wififramework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer.
- An 802.11 wireless adapter installed and enabled. A Wi-Fi Direct capable adapter is required for **WiFiDirect**, **WiFiDirectClient**, **WiFiDirectSoftAP**, and **MobileHotspot**. **WiFiHostedNetwork** requires a driver that supports the Windows hosted network feature.
- Administrator rights are required for profile operations in **WiFiClient**, for `SetIpSettings` / `ResetIpSettings` in **WiFiDirectSoftAP**, for the hosted network operations in **WiFiHostedNetwork**, and for the Mobile Hotspot API used by **MobileHotspot**.
- Both **Mono** and **IL2CPP** scripting backends are supported.

---

## Getting started

### 1. Install the Wi-Fi Framework

Ensure that the framework assemblies (`wclCommon`, `wclWiFi`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains the example controllers.

### 2. Install the example scripts

Copy the `Scripts` folder of the desired example into the Unity project's `Assets` tree. Each example is independent; multiple examples can coexist in the same project as long as their scene builders are invoked one at a time.

### 3. Build the example scene

Open the corresponding menu item:

> **Tools → &lt;Example Name&gt; → Build Scene**

The scene builder creates the canvas, the controls, the controller object, and saves the scene to `Assets/Scenes/<ExampleName>.unity`.

### 4. Run the example

Open the generated scene and press **Play**. The exact startup behaviour of each example is described in its own `README.md`.

---

## Common design decisions

These decisions apply to every example in the collection and are worth understanding before reading individual controllers.

- **Programmatic UI.** No scenes or prefabs are authored by hand. The scene builder scripts construct the entire UI at build time using `GameObject` and `RectTransform` calls, matching the layout and control sizes of the original WinForms reference. Coordinates are taken directly from the reference `.Designer.cs` files, so the visual result is a close match to the WinForms sample.
- **TextMesh Pro only.** Every label, button text, input field, dropdown label, list view cell, and log line is rendered through TextMesh Pro. Legacy `UnityEngine.UI.Text` is not used anywhere.
- **Plain C# UI shims.** `ListView`, `ListBox`, `MessageBox`, `ProgressBar`, `OpenFileDialog`, `SaveFileDialog`, and `Slider` are implemented as plain C# classes inside the controller file. `ListView` in particular reproduces the WinForms model — `Items.Add`, `Items.Insert`, `Items.Remove`, `Items.RemoveAt`, `SubItems[i].Text`, `Tag`, `SelectedItems`, `Index`, `Columns.Add`, `Columns.Clear` — so that controller code matches the reference sample almost line for line.
- **Native dialogs.** `MessageBox`, `OpenFileDialog`, and `SaveFileDialog` are backed by the native Win32 functions (`MessageBoxW`, `GetOpenFileNameW`, `GetSaveFileNameW`). They are blocking, exactly as in WinForms, and can be called from any thread.
- **Dispatch queue.** Calls into the Wi-Fi Framework from button handlers, and UI updates from framework events, are queued and executed from `Update` on the main thread. `out`-parameters in framework events are still set synchronously so the framework sees the correct value.
- **Dropdown sizing.** `TMP_Dropdown` is adjusted in `Update` so that the last option in the popup is fully visible, matching the WinForms `ComboBox` behaviour.
- **Cleanup.** Every controller stops or closes its framework objects in `OnDestroy`, wrapped in try/catch so that shutdown proceeds even if the framework returns an error.
- **`[Preserve]` on controllers.** Every controller is annotated with `[UnityEngine.Scripting.Preserve]` so the IL2CPP linker does not strip the type or the framework members it references.
- **Variant UI shims in MobileHotspot.** The `MobileHotspotController` ships with its own simplified `ListView` and `MessageBox` shims — the `ListView` reads its columns from the header row at construction time rather than from an explicit `ColumnWidths` argument, and `MessageBox` only exposes `Show(text)` and `Show(text, caption)` for informational dialogs. The demo uses only two columns (MAC, Name) and only informational dialogs, so the simplified shims are sufficient. Other demos use the full-featured shims described above.

---

## Project structure

Each example follows the same layout:

<ExampleName>/
├── README.md
├── Scripts/
│ ├── <ExampleName>SceneBuilder.cs # Editor: Tools → <ExampleName> → Build Scene
│ └── <ExampleName>Controller.cs # Runtime: demo logic and UI shims
└── Scenes/
└── <ExampleName>.unity # Generated by the scene builder


The scene builder is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. The controller is a plain `MonoBehaviour` that uses only the public Wi-Fi Framework API together with the UI shims described above.

---

## Links

- **Wi-Fi Framework (product page and licensing):** https://www.btframework.com/wififramework.htm
- **Installation guide:** https://www.btframework.com/howtoinstall.htm
- **How-To guides:** https://www.btframework.com/wifi_howto.htm
- **API reference:** https://docs.btframework.com/wifi/index.htm
- **Wireless Communication Library (underlying library):** https://www.btframework.com/wcl.htm

---

## License

The Wi-Fi Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. The examples in this collection are illustrative samples and do not include the Wi-Fi Framework binaries; obtain them separately from the vendor.