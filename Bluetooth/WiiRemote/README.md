# WiiRemote — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclWiiRemoteClient` class) for connecting to a Nintendo Wii Remote and reading its buttons, accelerometer, IR camera, LEDs, rumble, battery level, and the Nunchuk and Balance Board extensions.

This demo is a Unity port of the reference **Wii Remote** sample shipped with the framework.

---

## Overview

The WiiRemote demo connects to a Nintendo Wii Remote over Bluetooth Classic, displays live button state, accelerometer values, IR sensor data (mode, sensitivity, per-sensor position, size, and detection), battery level, and LED state. It exposes controls for enabling or disabling the accelerometer, adjusting IR sensitivity, turning rumble on or off, reading or writing the LED state, and requesting the current status. Extension detection is supported for the Nunchuk and the Balance Board, with dedicated controls for the Nunchuk's accelerometer, joystick, C, and Z inputs, and for the Balance Board's four weight sensors.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclWiiRemoteClient` API used |
|------|-------------------------------|
| Discovery | `wclBluetoothRadio.Discover(dkClassic)`, `OnDeviceFound`, `OnDiscoveringStarted`, `OnDiscoveringCompleted` |
| Connection | `Connect`, `Disconnect`, `State`, `Address`, `OnConnect`, `OnDisconnect` |
| Status | `GetStatus`, `OnStatusChanged`, `wclWiiRemoteLeds`, `Leds` |
| LEDs | `SetLeds`, `Leds` |
| Rumble | `SetRumble` |
| Accelerometer | `EnableAccel`, `DisableAccel`, `OnAccelChanged`, `wclWiiRemoteAccel` |
| Buttons | `OnButtonsChanged`, `wclWiiRemoteButtons` |
| IR camera | `SetIrSensitivity`, `OnIrChanged`, `wclWiiRemoteIrSensors`, `wclWiiRemoteIrMode`, `wclWiiRemoteIrSensitivity` |
| Extensions | `Extension`, `OnExtensionAttached`, `OnExtensionDetached` |
| Nunchuk | `OnNunchukChanged`, `wclWiiRemoteNunchuk` |
| Balance Board | `OnBalanceBoardChanged`, `wclWiiRemoteBalanceBoard`, `SensorsKg` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows
- A Bluetooth Classic stack and a radio that can connect to a Wii Remote
- A Nintendo Wii Remote (and, optionally, a Nunchuk or a Balance Board)
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies are present in the Unity project and are referenced by the assembly that contains `WiiRemoteController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → WiiRemote → Build Scene**

This creates the canvas, all controls, the `WiiRemoteController` GameObject, and saves the scene to `Assets/Scenes/WiiRemote.unity`.

### 3. Run the scene

Enter Play mode and click **Discover**. Press **1** and **2** on the Wii Remote to put it into pairing mode, select it in the device list, and click **Connect**. Once connected, the LEDs are set, and the button, accelerometer, IR, and extension panels start updating.

---

## Using the demo UI

| Control | Function |
|---------|----------|
| **Discover** | Starts classic discovery |
| **Connect / Disconnect** | Opens or closes the link to the selected Wii Remote |
| **Batt level** | Value returned by `OnStatusChanged` |
| **Get status / Get LEDs / Set LEDs** | Status queries and LED control |
| **LED1..LED4** | Individual LED state |
| **Rumble ON / Rumble OFF** | `SetRumble` |
| **Buttons** | Live state of A, B, Plus, Home, Minus, One, Two, Up, Down, Left, Right |
| **Accel** | X, Y, Z from `OnAccelChanged`; **Enable** / **Disable** control the accelerometer |
| **IR** | Current mode, sensitivity, and the four sensors' position, size, and detection state; **Enable** / **Disable** control the IR camera |
| **Nunchuk** | Attached state, accelerometer, joystick, C, Z |
| **Balance board** | Attached state and the four sensor values in kilograms |
| **Clear** | Empties the event log |

---

## Behavioural notes

- **Asynchronous connect.** `Connect` only starts the procedure. The result is delivered in `OnConnect`. On success, the demo calls `SetLeds` using the current LED checkbox values.
- **Extension hot-plugging.** Attaching or removing a Nunchuk or Balance Board fires `OnExtensionAttached` / `OnExtensionDetached`. The demo resets the corresponding controls when the extension is detached.
- **IR sensitivity.** `SetIrSensitivity` accepts values from `wiiIrLevelOff` to `wiiIrLevelMax`. The demo's **Enable** button uses `wiiIrLevelMax`; **Disable** uses `wiiIrLevelOff`.
- **Status refresh.** `GetStatus` is provided for manual refresh; the framework also fires `OnStatusChanged` whenever the battery level or LED state changes.
- **Errors.** Errors from `Connect`, `GetStatus`, `SetLeds`, `EnableAccel`, `DisableAccel`, `SetIrSensitivity`, and `SetRumble` are written to the event log and also reported through a message box, matching the WinForms sample.

---

## Project structure

Assets/
├── Scripts/
│ ├── WiiRemoteSceneBuilder.cs # Editor: Tools → WiiRemote → Build Scene
│ └── WiiRemoteController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── WiiRemote.unity # Saved by the scene builder


`WiiRemoteSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `WiiRemoteController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Bluetooth Classic profiles overview:** https://www.btframework.com/bluetoothprofiles.htm
- **Classic Bluetooth (RFCOMM):** https://www.btframework.com/rfcomm.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.