# OppClient — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclObexOppClient` and `wclRfCommClient` classes) for sending files, receiving objects, and requesting vCards or device capabilities over OBEX Object Push Profile.

This demo is a Unity port of the reference **OBEX ObjectPush Client** sample shipped with the framework. See also the OBEX overview at **https://www.btframework.com/obex.htm**.

---

## Overview

The OppClient demo connects to a remote Bluetooth device that exposes the OBEX Object Push service (`OBEXObjectPushServiceClass_UUID`), opens an OBEX session, and lets the user send a file chosen from the local file system, request a default vCard, or request the device capability object. The received object is written to the working directory. Transfer progress is reported both in a progress bar and in a log.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | API used |
|------|-----------|
| Connection | `wclRfCommClient.Connect`, `Disconnect`, `Service`, `OnConnect`, `OnDisconnect` |
| OBEX session | `wclObexOppClient.Connect`, `Disconnect`, `OnConnect`, `OnDisconnect` |
| Sending | `Put`, `OnPutComplete` |
| Receiving | `Get`, `OnGetComplete` |
| Abort | `Abort` |
| Progress | `OnProgress` |
| Discovery | `wclBluetoothManager.OnDeviceFound`, `OnDiscoveringStarted`, `OnDiscoveringCompleted` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A remote Bluetooth device that publishes the OBEX Object Push service
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `OppClientController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → OppClient → Build Scene**

This creates the canvas, all controls, the `OppClientController` GameObject, and saves the scene to `Assets/Scenes/OppClient.unity`.

### 3. Run the scene

Enter Play mode. Click **Discover**, select a device, click **Connect**, then **Open Session**. Use **Send file** to push the file chosen in the file name field, or use **Get vCard** / **Get caps** to request the standard objects.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discover / Connect / Disconnect** | Device discovery and RFCOMM connection |
| **Open Session / Close Session** | Opens or closes the OBEX session |
| **Abort** | Aborts the current OBEX operation |
| **Select File / Send file** | Upload the file chosen in `edFileName` via `Put` |
| **Get vCard** | Requests a default vCard object (`text/x-vCard`) |
| **Get caps** | Requests the device capability object (`x-obex/capability`) |
| **Progress bar** | Reports `OnProgress` for the current transfer |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Asynchronous connect.** `wclRfCommClient.Connect` only starts the procedure; the result arrives in `OnConnect`. The OBEX client is created inside `OnCreateProcessor`, so `Open Session` can be used as soon as the RFCOMM link is up.
- **Streams are owned by the demo.** For `Put` and `Get`, the demo passes a `FileStream` and closes and disposes it once the completion event fires, or immediately if the operation fails early.
- **`Get vCard` and `Get caps` write to files.** The received objects are written to `vcard.vcf` and `caps.xml` in the current working directory. Those file names are hardcoded in the WinForms sample and preserved here. If the files already exist, `FileMode.CreateNew` fails; delete them before retrying.
- **Progress reporting.** `OnProgress` reports `Size` and `Position` in bytes. The demo mirrors these into the progress bar and clears it when the operation completes.

---

## Project structure

Assets/
├── Scripts/
│ ├── OppClientSceneBuilder.cs # Editor: Tools → OppClient → Build Scene
│ └── OppClientController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── OppClient.unity # Saved by the scene builder


`OppClientSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `OppClientController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`, `ProgressBar`, `OpenFileDialog`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **OBEX overview:** https://www.btframework.com/obex.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.