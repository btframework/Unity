# OppServer — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclObexOppServer` and `wclRfCommServer` classes) for accepting files, answering vCard and capability requests, and managing multiple concurrent OBEX sessions.

This demo is a Unity port of the reference **OBEX ObjectPush Server** sample shipped with the framework. See also the OBEX overview at **https://www.btframework.com/obex.htm**.

---

## Overview

The OppServer demo starts an RFCOMM server that publishes the OBEX Object Push service (`OBEXObjectPushServiceClass_UUID`) with a standard SDP record. Every incoming OBEX session is delivered as a new client on the server. The demo asks the user whether to accept the session, whether to accept each incoming file, and where to save it. Requests for the standard `text/x-vCard` and `x-obex/capability` objects are answered by selecting a file from disk.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | API used |
|------|-----------|
| Server lifecycle | `wclRfCommServer.Listen`, `Close`, `Service`, `ServiceName`, `Authentication`, `OnListen`, `OnClosed` |
| SDP record | `GetSdpAttributes`, `OBEX_PROTOCOL_UUID16`, `OBEXObjectPushServiceClass_UUID16`, `COD_SERVICE_OBJECT_XFER` |
| Client handling | `OnConnect`, `OnDisconnect`, `wclRfCommServerClientConnection` |
| OBEX data processor | `OnCreateProcessor`, `OnDestroyProcessor`, `wclObexOppServer` |
| OBEX session | `OnConnect`, `OnDisconnected`, `Accept`, `Reject` |
| File transfer | `OnPutBegin`, `OnPutCompleted`, `OnPutProgress`, `OnGetRequest`, `OnGetCompleted`, `OnProgress` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A Bluetooth Classic stack and a radio that can publish the OBEX OPP service
- A remote device that can send files via Bluetooth (a phone, a PC, another development board)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `OppServerController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → OppServer → Build Scene**

This creates the canvas, all controls, the `OppServerController` GameObject, and saves the scene to `Assets/Scenes/OppServer.unity`.

### 3. Run the scene

Enter Play mode and click **Listen**. The server becomes discoverable and connectable. Pair a remote device, then send a file to it via OBEX Object Push; the demo asks for confirmation and then prompts for a save location.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Listen** | Opens the Bluetooth manager, switches the classic radio to discoverable and connectable, and starts listening on the OBEX OPP service |
| **Close** | Stops listening and closes the Bluetooth manager |
| **Client list** | One row per active OBEX session, showing the client's Bluetooth address |
| **Disconnect** | Force-disconnects the selected client |
| **Clear** | Empties the event log |

During a session the demo displays standard Windows dialogs for session acceptance, file acceptance, and file saving.

---

## Behavioural notes

- **Synchronous dialogs.** `OnConnect`, `OnPutBegin`, and `OnPutCompleted` are delivered on the OBEX worker thread and must complete synchronously before returning control. The demo uses native Win32 message boxes and file dialogs for these confirmations. Non-blocking UI updates (log lines, client list) are queued to the main thread.
- **SDP record.** `GetSdpAttributes` publishes a standard OBEX Object Push SDP record containing the OBEX protocol descriptor, the OPP profile descriptor, and the object transfer class of device, so that remote devices can discover the service by UUID.
- **Data processor lifetime.** The OBEX server object is created inside `OnCreateProcessor` and disposed inside `OnDestroyProcessor`, ensuring that each accepted RFCOMM link has exactly one OBEX processor.
- **File save location.** The demo opens a Save File dialog for each accepted file. Files are written with `FileMode.CreateNew`; the dialog warns before overwriting.
- **Client disconnect.** Disconnecting a client from the demo stops the OBEX session and the RFCOMM link; the corresponding row is removed from the client list.

---

## Project structure

Assets/
├── Scripts/
│ ├── OppServerSceneBuilder.cs # Editor: Tools → OppServer → Build Scene
│ └── OppServerController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── OppServer.unity # Saved by the scene builder


`OppServerSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `OppServerController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`, `OpenFileDialog`, `SaveFileDialog`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **OBEX overview:** https://www.btframework.com/obex.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.