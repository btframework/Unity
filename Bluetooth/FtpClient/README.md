# FtpClient — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclObexFtpClient` and `wclRfCommClient` classes) for browsing a remote device's file system and transferring files over OBEX File Transfer Profile.

This demo is a Unity port of the reference **OBEX FTP Client** sample shipped with the framework. See also the OBEX overview at **https://www.btframework.com/obex.htm**.

---

## Overview

The FtpClient demo connects to a remote Bluetooth device that exposes the OBEX File Transfer service (`OBEXFileTransferServiceClass_UUID`), opens an OBEX session, and gives the user the ability to list the current directory, navigate the remote file system, download individual files, upload files, create and remove directories, and delete files. Transfer progress is reported both in a progress bar and in a log.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | API used |
|------|-----------|
| Connection | `wclRfCommClient.Connect`, `Disconnect`, `Service`, `OnConnect`, `OnDisconnect` |
| OBEX session | `wclObexFtpClient.Connect`, `Disconnect`, `OnConnect`, `OnDisconnect` |
| Directory operations | `Dir`, `ChangeDir`, `MkDir`, `OnDirComplete`, `OnChangeDirComplete`, `OnMakeDirComplete` |
| File operations | `Get`, `Put`, `Delete`, `Abort`, `OnGetComplete`, `OnPutComplete`, `OnDeleteComplete` |
| Progress | `OnProgress` |
| Discovery | `wclBluetoothManager.OnDeviceFound`, `OnDiscoveringStarted`, `OnDiscoveringCompleted` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A remote Bluetooth device that publishes the OBEX File Transfer service
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, `wclCommunication`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `FtpClientController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → FtpClient → Build Scene**

This creates the canvas, all controls, the `FtpClientController` GameObject, and saves the scene to `Assets/Scenes/FtpClient.unity`.

### 3. Run the scene

Enter Play mode. Click **Discover**, select a device that exposes the File Transfer service, click **Connect**. Once the RFCOMM connection is established, click **Open Session**, then use **Dir** to populate the remote file list. Double-click a folder to enter it, or a file to download it.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discover / Connect / Disconnect** | Device discovery and RFCOMM connection to the selected device |
| **Open Session / Close Session** | Opens or closes the OBEX session |
| **Select File / Send file** | Upload the file chosen in `edFileName` via `Put` |
| **Abort** | Aborts the current OBEX operation |
| **Get vCard / Get caps** | Downloads a default vCard or a device capability object |
| **MkDir** | Creates a directory on the remote device |
| **Dir / Root / Delete** | Refresh the current directory, return to the root, or delete the selected entry |
| **File list** | Double-click to change directory (folders) or to download (files) |
| **Progress bar** | Reports `OnProgress` for the current transfer |

---

## Behavioural notes

- **Asynchronous connect.** `wclRfCommClient.Connect` only starts the procedure; the result arrives in `OnConnect`. The demo creates the OBEX client from within `OnCreateProcessor`, so the OBEX session can be opened immediately after the RFCOMM link is up.
- **Streams are owned by the demo.** Both `Put` and `Get` hand the framework a `Stream` object. After the corresponding completion event fires, the demo closes and disposes the stream. If the operation fails early, the stream is disposed immediately.
- **Transfer progress.** `OnProgress` reports `Size` and `Position` in bytes. The demo mirrors these into the progress bar and clears it when the operation completes.
- **Directory refresh.** Every successful `MkDir`, `Delete`, or `ChangeDir` reissues `Dir`, so the file list always reflects the current remote state.
- **Save location.** Downloaded files are written to the current working directory of the Unity process; use `Path.Combine(Application.persistentDataPath, name)` if you need a stable path.

---

## Project structure

Assets/
├── Scripts/
│ ├── FtpClientSceneBuilder.cs # Editor: Tools → FtpClient → Build Scene
│ └── FtpClientController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── FtpClient.unity # Saved by the scene builder


`FtpClientSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `FtpClientController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`, `ProgressBar`, `OpenFileDialog`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **OBEX overview:** https://www.btframework.com/obex.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.