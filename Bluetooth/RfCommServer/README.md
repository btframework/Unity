# RfCommServer — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclRfCommServer` class) for publishing a Bluetooth Classic service over RFCOMM, accepting connections, exchanging data, and controlling per-client buffer sizes.

This demo is a Unity port of the reference **Bluetooth RFCOMM Server** sample documented at:
**https://www.btframework.com/rfcomm.htm**

---

## Overview

The RfCommServer demo publishes the Serial Port Profile (`SerialPortServiceClass_UUID`) with a custom service name, becomes discoverable and connectable, accepts incoming RFCOMM connections, exchanges ASCII data with each connected client, and lets the user force-disconnect any client. Authentication and encryption are configurable per session.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclRfCommServer` API used |
|------|----------------------------|
| Server lifecycle | `Listen`, `Close`, `Authentication`, `Encryption`, `Service`, `ServiceName`, `OnListen`, `OnClosed` |
| Client handling | `OnConnect`, `OnDisconnect`, `wclRfCommServerClientConnection` |
| Data transfer | `OnData`, `Write`, `Encoding.ASCII` |
| Buffers | `GetReadBufferSize`, `SetReadBufferSize`, `GetWriteBufferSize`, `SetWriteBufferSize` |
| Device lookup | `GetRemoteName` on the classic radio |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A Bluetooth Classic stack and a radio that can publish the SPP service
- A remote device that can initiate an SPP connection (a PC, a phone, another development board)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `RfCommServerController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → RfCommServer → Build Scene**

This creates the canvas, all controls, the `RfCommServerController` GameObject, and saves the scene to `Assets/Scenes/RfCommServer.unity`.

### 3. Run the scene

Enter Play mode and click **Listen**. The server becomes discoverable and connectable with the configured service name. Connect from a remote device using the same service; the connection appears in the client list. Type text into the send field and press **Send** to transmit it to the selected client.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Listen** | Opens the manager, gets a classic radio, switches the radio to discoverable and connectable, and starts listening |
| **Close** | Stops listening |
| **Authentication / Encryption** | Applied to the server before listening |
| **Service name** | Published in the SDP record |
| **Client list** | One row per connected client, showing the Bluetooth address and resolved name |
| **Disconnect** | Force-disconnects the selected client |
| **Send** | Sends the entered text as ASCII to the selected client |
| **Get buffers / Set buffers** | Reads or writes the read and write buffer sizes for the selected client |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Discoverable / connectable.** `Listen` sets the radio to both discoverable and connectable before calling `wclRfCommServer.Listen`. Those two calls may fail on some Windows configurations; the demo ignores their return codes and relies on `Listen` to report the real error.
- **Client name.** When a client connects, the demo calls `GetRemoteName` on the classic radio to populate the name column. The name may be empty on devices that do not respond to the name request promptly.
- **Disconnection.** `OnDisconnect` fires when the client closes the link, whether that closure was initiated from the client or by `Disconnect` on the server side. The corresponding row is removed from the list.
- **Per-client buffers.** Buffer sizes are per connection. The demo reads and writes them on the selected client only.
- **Data.** Data is delivered as raw bytes in `OnData`. The demo converts to ASCII for display; production applications should treat it as binary unless the protocol is known.

---

## Project structure

Assets/
├── Scripts/
│ ├── RfCommServerSceneBuilder.cs # Editor: Tools → RfCommServer → Build Scene
│ └── RfCommServerController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── RfCommServer.unity # Saved by the scene builder


`RfCommServerSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `RfCommServerController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **Bluetooth Classic profiles overview:** https://www.btframework.com/bluetoothprofiles.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.