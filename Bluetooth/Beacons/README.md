# Beacons — Unity Demo

A Unity example demonstrating the use of the **Bluetooth Framework** (`wclBluetoothLeBeaconWatcher` and `wclBluetoothLeAdvertiser` classes) for receiving, decoding, and transmitting Bluetooth LE advertisement frames: beacons, Apple Continuity, Eddystone, AltBeacon, Microsoft CDP, Drone Remote ID, and raw manufacturer-specific data.

This demo is a Unity port of the reference **Beacons** sample documented at:
**https://www.btframework.com/blebeacon.htm**

---

## Overview

The Beacons demo covers both sides of the Bluetooth LE advertisement workflow. The watcher side starts scanning on the selected channel with the chosen scanning mode and scan parameters, decodes every supported advertisement type, and shows each frame under its originating device in a two-level list. The advertiser side builds one or more advertisement objects from the selected predefined types (iBeacon, Proximity Beacon, AltBeacon, Eddystone UID, Eddystone URL, manufacturer-specific, 16/32/128-bit service data, 128-bit SOL UUID, or a custom raw payload), configures the interval, PHY, and extended-advertisement options, and starts broadcasting on the chosen radio.

The user interface is generated programmatically by an editor script. There are no hand-authored scenes or prefabs to maintain.

---

## Features demonstrated

| Area | `wclBluetoothLeBeaconWatcher` / `wclBluetoothLeAdvertiser` API used |
|------|------------------------------------------------------------------------|
| Manager lifecycle | `wclBluetoothManager.Open`, `Close`, `Active`, `GetLeRadio`, `AfterOpen`, `BeforeClose`, `OnClosed` |
| Watcher lifecycle | `Start(radio)`, `Stop`, `OnStarted`, `OnStopped` |
| Watcher configuration | `ScanningMode`, `ScanParametersType`, `ScanInterval`, `ScanWindow`, `AllowExtendedAdvertisements` |
| Advertising PDU decoding | `OnAdvertisementAppearanceFrame`, `OnAdvertisementExtFrameInformation`, `OnAdvertisementFrameInformation` |
| Service data and SOL | `OnAdvertisementService16DataFrame`, `OnAdvertisementService32DataFrame`, `OnAdvertisementService128DataFrame`, `OnAdvertisementServiceSol16Frame`, `OnAdvertisementServiceSol32Frame`, `OnAdvertisementServiceSol128Frame` |
| Miscellaneous frames | `OnAdvertisementRawFrame`, `OnAdvertisementReceived`, `OnAdvertisementTxPowerLevelFrame`, `OnAdvertisementUuidFrame`, `OnManufacturerRawFrame` |
| Beacon decoding | `OnAltBeaconFrame`, `OnEddystoneTlmFrame`, `OnEddystoneUidFrame`, `OnEddystoneUrlFrame`, `OnMicrosoftCdpBeaconFrame`, `OnDriAsdMessage` |
| Apple Continuity decoding | `OnAppleiBeaconFrame`, `OnAppleHomeKitFrame`, `OnAppleNearbyActionFrame`, `OnAppleNearbyInfoFrame`, `OnAppleAirdropFrame`, `OnAppleAirplaySourceFrame`, `OnAppleAirplayTargetFrame`, `OnAppleAirprintFrame`, `OnAppleProximityPairingFrame`, `OnAppleFindMyFrame`, `OnAppleHandoffFrame`, `OnAppleHeySiriFrame`, `OnAppleMagicSwitchFrame`, `OnAppleTetheringSourceFrame`, `OnAppleTetheringTargetFrame`, `OnAppleUnknownFrame` |
| Advertiser lifecycle | `Start(radio)`, `Stop`, `OnStarted`, `OnStopped` |
| Advertiser configuration | `Clear`, `Add(adv)`, `Count`, `Interval`, `UseExtendedAdvertisement`, `Anonymous`, `IncludeTxRssi`, `PreferredTxRssi`, `PrimaryPhy`, `SecondaryPhy`, `RestoreDefaults` |
| Predefined advertisements | `wclBluetoothLeiBeaconAdvertisement`, `wclBluetoothLeProximityBeaconAdvertisement`, `wclBluetoothLeAltBeaconAdvertisement`, `wclBluetoothLeEddystoneUidBeaconAdvertisement`, `wclBluetoothLeEddystoneUrlBeaconAdvertisement`, `wclBluetoothLeManufacturerAdvertisement`, `wclBluetoothLe16ServiceAdvertisement`, `wclBluetoothLe32ServiceAdvertisement`, `wclBluetoothLe128ServiceAdvertisement`, `wclBluetoothLe16ServiceDataAdvertisement`, `wclBluetoothLe32ServiceDataAdvertisement`, `wclBluetoothLe128ServiceDataAdvertisement` |
| Custom advertisements | `wclBluetoothLeCustomAdvertisement`, `wclBluetoothLeRawAdvertisement`, `wclUUIDs.LE_GAP_AD_TYPE_*` |

---

## Requirements

- **Unity** 2021.3 LTS or newer (TextMeshPro and the Editor API are used)
- **Bluetooth Framework** installed and referenced by the project
  - Product page: https://www.btframework.com/bluetoothframework.htm
  - Installation guide: https://www.btframework.com/howtoinstall.htm
- **Platform:** Windows 10 or newer
- A Bluetooth LE radio that supports advertising (for the advertiser side)
- Extended advertisements require a Bluetooth 5 radio and Windows 10 or newer with the Microsoft Bluetooth driver
- Both **Mono** and **IL2CPP** scripting backends are supported

---

## Getting started

### 1. Install the Bluetooth Framework

Ensure that the framework assemblies (`wclCommon`, `wclBluetooth`, and their dependencies) are present in the Unity project and are referenced by the assembly that contains `BeaconsController.cs`.

### 2. Build the demo scene

From the Unity menu select:

> **Tools → Beacons → Build Scene**

This creates the canvas, all controls, the `BeaconsController` GameObject, and saves the scene to `Assets/Scenes/Beacons.unity`.

### 3. Run the scene

Enter Play mode. To receive advertisements, choose a scanning mode, optionally enable extended advertisements or set custom scan parameters, and press **Start** in the Beacon Watcher section. To broadcast, select at least one advertisement type in the BLE Advertiser section, optionally enable extended advertisement, choose the PHY, and press **Start** in that section.

---

## Using the demo UI

### Beacon Watcher

| Control | Function |
|---------|----------|
| **Scan mode** | Active or passive scanning (`ScanningMode`) |
| **Scan parameters type** | Default, Coexistence Optimized, Low Latency, or Custom (`ScanParametersType`) |
| **Scan interval / window** | Enabled only when Custom is selected (`ScanInterval`, `ScanWindow`) |
| **Extended** | Enables decoding of extended advertisements (`AllowExtendedAdvertisements`) |
| **Start / Stop** | Starts or stops the watcher on the selected LE radio |
| **Device list** | Top-level rows are unique Bluetooth addresses; indented rows are the advertisement frames received from each device |
| **Frame details** | Field/value pairs decoded from the selected frame |

### BLE Advertiser

| Control | Function |
|---------|----------|
| **Interval** | Advertising interval in milliseconds (`Interval`) |
| **Default** | Restores the framework default settings (`RestoreDefaults`) |
| **Start / Stop** | Starts or stops the advertiser |
| **iBeacon, Proximity Beacon, ALT Beacon, Eddystone UID, Eddystone URL, Manufacturer, 16/32/128 UUID service, 16/32/128 UUID data, 128 SOL UUID, Custom RAW** | Selects which advertisement objects to add to the advertiser before starting |
| **Extended advertisement** | Enables Bluetooth 5 extended advertisement (`UseExtendedAdvertisement`) |
| **Anonymous** | Enables anonymous advertisement headers (`Anonymous`) |
| **Include TX RSSI** | Includes the transmit power in the advertisement header (`IncludeTxRssi`) |
| **Primary PHY / Secondary PHY** | Selects the PHYs used for extended advertisement (`PrimaryPhy`, `SecondaryPhy`) |

### Log

| Control | Function |
|---------|----------|
| **Log** | Text of framework events and error codes |
| **Clear** | Empties the log |
| **Autoscroll** | When checked, keeps the log scrolled to the latest entry |

---

## Behavioural notes

- **Two-level device list.** For each unique address, the demo inserts a top-level row (with an empty `Tag`). Every decoded frame is inserted beneath it (with the `FrameStorage` object as `Tag`). Selecting a frame row populates the details view; selecting a device row clears it. This mirrors the layout of the original WinForms sample.
- **Frame deduplication.** When a frame with the same type has already been received from the same device, the existing row's `Tag` is updated in place rather than adding a duplicate. The details view refreshes automatically when the row is selected.
- **Extended advertisement capability.** `UseExtendedAdvertisement` requires a Bluetooth 5 radio and the Microsoft driver on Windows 10 or newer. If the hardware or the OS does not support it, `Start` returns `WCL_E_BLUETOOTH_LE_EXT_ADV_NOT_SUPPORTED`. The demo logs the error code in that case.
- **Reserved advertisement types.** The advertiser does not allow `00`, `01`, `08`, `09`, `1B`, `1C`, and — under the Microsoft driver — several additional types including the 16/32/128-bit service UUID lists, TX Power, COD, Pairing Hash, and others. The demo only constructs advertisement objects through the predefined classes, so it does not attempt to send reserved types directly.
- **Custom raw advertisement.** The custom raw option builds a 20-byte payload that contains one 16-bit service data field and one manufacturer-specific field. `wclBluetoothLeRawAdvertisement` accepts the whole payload and sends it as-is, so any well-formed field layout can be tested.
- **Animated / PHY options.** The **Anonymous** and **Include TX RSSI** checkboxes are only enabled while **Extended advertisement** is checked, matching the WinForms behaviour. The **Primary PHY** and **Secondary PHY** dropdowns are always available; they only take effect when extended advertisement is enabled.
- **Scan parameter controls.** The **Scan interval** and **Scan window** fields are only enabled when **Scan parameters type** is set to Custom. For the other three parameter types, the framework chooses the values automatically.
- **Apple Continuity.** All sixteen Apple Continuity frame types have dedicated decoders in this demo: AirDrop, AirPlay Source, AirPlay Target, AirPrint, HomeKit, iBeacon, Nearby Action, Nearby Info, Proximity Pairing, Find My, Handoff, Hey Siri, Magic Switch, Tethering Source, Tethering Target, and the unknown fallback. Each decoder writes its fields into the details view as name/value rows.
- **Drone Remote ID.** Frames of this type are delivered through `OnDriAsdMessage` and shown as a raw byte payload in the details view.
- **Manager lifecycle.** The manager is opened on first use of either the watcher or the advertiser via `GetRadio`. When the manager is closing, `BeforeClose` stops both the watcher and the advertiser before the manager is released.

---

## Project structure

Assets/
├── Scripts/
│ ├── BeaconsSceneBuilder.cs # Editor: Tools → Beacons → Build Scene
│ └── BeaconsController.cs # Runtime: demo logic and UI shims
└── Scenes/
└── Beacons.unity # Saved by the scene builder


`BeaconsSceneBuilder.cs` is wrapped in `#if UNITY_EDITOR` and has no runtime footprint. `BeaconsController.cs` is a plain `MonoBehaviour` that uses only the public Bluetooth Framework API; it also contains the small UI helper classes (`ListView`, `ListBox`, `MessageBox`) that emulate the equivalent WinForms controls, and the `FrameStorage` class hierarchy used to keep the parsed advertisement fields for the details view.

---

## Links

- **Bluetooth Framework (product page):** https://www.btframework.com/bluetoothframework.htm
- **Bluetooth LE Advertisement (reference article):** https://www.btframework.com/blebeacon.htm
- **BLE Sniffer (raw frame capture):** https://www.btframework.com/blesniffer.htm
- **Extended Advertisement format:** https://www.btframework.com/bleextadv.htm
- **Beacon formats overview:** https://www.btframework.com/beaconformats.htm
- **API reference:** https://docs.btframework.com/bluetooth/index.htm

---

## License

The Bluetooth Framework is a commercial product of [Soft Service Company](https://www.btframework.com/) distributed under its own license agreement. This demo project is an illustrative sample and does not include the Bluetooth Framework binaries; obtain them separately from the vendor.