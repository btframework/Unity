using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;

public class BeaconsDemo : MonoBehaviour
{
    // ---------------------------------------------------------- UI refs
    [Header("Watcher")]
    public TMP_Dropdown scanModeDropdown;
    public TMP_Dropdown scanParamsDropdown;
    public TMP_InputField scanIntervalInput;
    public TMP_InputField scanWindowInput;
    public Toggle enableExtendedToggle;
    public Button watcherStartButton;
    public Button watcherStopButton;

    [Header("Devices tree")]
    public Transform devicesContent;         // куда складываем DeviceNode
    public GameObject deviceNodePrefab;      // пустой контейнер с VerticalLayoutGroup (DeviceRow + FrameRows)
    public GameObject rowPrefab;             // BeaconRow

    [Header("Frame details")]
    public Transform detailsContent;
    public GameObject detailRowPrefab;

    [Header("Advertiser")]
    public TMP_InputField advertiserIntervalInput;
    public Button advertiserDefaultButton;
    public Button advertiserStartButton;
    public Button advertiserStopButton;

    public Toggle advBeaconToggle;
    public Toggle advProximityToggle;
    public Toggle advAltBeaconToggle;
    public Toggle advEddystoneUidToggle;
    public Toggle advEddystoneUrlToggle;
    public Toggle advManufacturerToggle;
    public Toggle adv16UuidToggle;
    public Toggle adv32UuidToggle;
    public Toggle adv128UuidToggle;
    public Toggle adv16UuidDataToggle;
    public Toggle adv32UuidDataToggle;
    public Toggle adv128UuidDataToggle;
    public Toggle adv128SolUuidToggle;
    public Toggle advCustomToggle;

    public Toggle advExtendedToggle;
    public Toggle advAnonymousToggle;
    public Toggle advTxRssiToggle;

    public TMP_Dropdown primaryPhyDropdown;
    public TMP_Dropdown secondaryPhyDropdown;

    [Header("Log")]
    public TMP_Text logText;
    public ScrollRect logScrollRect;
    public Button clearLogButton;
    public Toggle autoScrollToggle;

    // ---------------------------------------------------------- framework
    private wclBluetoothManager        _btManager;
    private wclBluetoothLeBeaconWatcher _watcher;
    private wclBluetoothLeAdvertiser   _advertiser;

    // ---------------------------------------------------------- device tree
    private class DeviceNode
    {
        public long Address;
        public string Name;
        public GameObject Container;
        public BeaconRowUI DeviceRow;
        public readonly List<FrameNode> Frames = new List<FrameNode>();
    }

    private class FrameNode
    {
        public FrameStorage Storage;
        public BeaconRowUI Row;
        public string Label;
    }

    private readonly List<DeviceNode> _devices = new List<DeviceNode>();
    private readonly Dictionary<long, DeviceNode> _devicesByAddress = new Dictionary<long, DeviceNode>();
    private BeaconRowUI _selectedRow;

    // ---------------------------------------------------------- lifecycle
    private void Awake()
    {
        watcherStartButton.onClick.AddListener(OnWatcherStartClicked);
        watcherStopButton .onClick.AddListener(OnWatcherStopClicked);
        advertiserDefaultButton.onClick.AddListener(OnAdvertiserDefaultClicked);
        advertiserStartButton  .onClick.AddListener(OnAdvertiserStartClicked);
        advertiserStopButton   .onClick.AddListener(OnAdvertiserStopClicked);
        clearLogButton         .onClick.AddListener(OnClearLogClicked);

        advExtendedToggle.onValueChanged.AddListener(OnAdvertiserExtendedChanged);
        scanParamsDropdown.onValueChanged.AddListener(OnScanParamsChanged);

        // Разрешаем только цифры в input-полях.
        scanIntervalInput .contentType = TMP_InputField.ContentType.IntegerNumber;
        scanWindowInput   .contentType = TMP_InputField.ContentType.IntegerNumber;
        advertiserIntervalInput.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    private void Start()
    {
        _btManager = new wclBluetoothManager();
        _btManager.AfterOpen   += BluetoothManagerAfterOpen;
        _btManager.BeforeClose += BluetoothManagerBeforeClose;
        _btManager.OnClosed    += BluetoothManagerClosed;

        _watcher = new wclBluetoothLeBeaconWatcher();
        HookWatcherEvents(_watcher);

        _advertiser = new wclBluetoothLeAdvertiser();
        _advertiser.OnStarted += LeAdvertiserStarted;
        _advertiser.OnStopped += LeAdvertiserStopped;

        AdvertiserDefaults();

        // Изначальные состояния UI
        scanModeDropdown.value = (_watcher.ScanningMode == wclBluetoothLeScanningMode.smActive) ? 0 : 1;
        scanIntervalInput.text = _watcher.ScanInterval.ToString();
        scanWindowInput.text   = _watcher.ScanWindow.ToString();
        enableExtendedToggle.isOn = _watcher.AllowExtendedAdvertisements;
        scanParamsDropdown.value = 0;
        scanIntervalInput.interactable = false;
        scanWindowInput.interactable   = false;

        primaryPhyDropdown.value   = 0;
        secondaryPhyDropdown.value = 0;

        advertiserStopButton.interactable = false;
        watcherStopButton.interactable    = false;

        autoScrollToggle.isOn = true;
    }

    private void OnDestroy()
    {
        if (_btManager == null) return;

        try { _btManager.Close(); }
        catch (Exception e) { Debug.LogException(e); }

        _btManager.AfterOpen   -= BluetoothManagerAfterOpen;
        _btManager.BeforeClose -= BluetoothManagerBeforeClose;
        _btManager.OnClosed    -= BluetoothManagerClosed;

        UnhookWatcherEvents(_watcher);
        if (_advertiser != null)
        {
            _advertiser.OnStarted -= LeAdvertiserStarted;
            _advertiser.OnStopped -= LeAdvertiserStopped;
        }
    }

    // ================================================================
    //  Watcher event wiring (все подписки/отписки в одном месте)
    // ================================================================
    private void HookWatcherEvents(wclBluetoothLeBeaconWatcher w)
    {
        w.OnStarted += BeaconWatcherStarted;
        w.OnStopped += BeaconWatcherStopped;
        w.OnAdvertisementAppearanceFrame     += OnAppearanceFrame;
        w.OnAdvertisementExtFrameInformation += OnExtInformationFrame;
        w.OnAdvertisementFrameInformation    += OnInformationFrame;
        w.OnAdvertisementRawFrame            += OnRawFrame;
        w.OnAdvertisementReceived            += OnAdvertisementReceived;
        w.OnAdvertisementService128DataFrame += OnService128Data;
        w.OnAdvertisementService16DataFrame  += OnService16Data;
        w.OnAdvertisementService32DataFrame  += OnService32Data;
        w.OnAdvertisementServiceSol128Frame  += OnSol128;
        w.OnAdvertisementServiceSol16Frame   += OnSol16;
        w.OnAdvertisementServiceSol32Frame   += OnSol32;
        w.OnAdvertisementTxPowerLevelFrame   += OnTxPower;
        w.OnAdvertisementUuidFrame           += OnUuid;
        w.OnAltBeaconFrame                   += OnAltBeacon;
        w.OnDriAsdMessage                    += OnDriAsd;
        w.OnEddystoneTlmFrame                += OnEddystoneTlm;
        w.OnEddystoneUidFrame                += OnEddystoneUid;
        w.OnEddystoneUrlFrame                += OnEddystoneUrl;
        w.OnManufacturerRawFrame             += OnManufacturer;
        w.OnMicrosoftCdpBeaconFrame          += OnMicrosoftCdp;
        w.OnAppleiBeaconFrame                += OnAppleiBeacon;
        w.OnAppleHomeKitFrame                += OnAppleHomeKit;
        w.OnAppleNearbyActionFrame           += OnAppleNearbyAction;
        w.OnAppleNearbyInfoFrame             += OnAppleNearbyInfo;
        w.OnAppleUnknownFrame                += OnAppleUnknown;
        w.OnAppleAirplaySourceFrame          += OnAppleAirplaySource;
        w.OnAppleAirplayTargetFrame          += OnAppleAirplayTarget;
        w.OnAppleAirdropFrame                += OnAppleAirdrop;
        w.OnAppleAirprintFrame               += OnAppleAirprint;
        w.OnAppleProximityPairingFrame       += OnAppleProximityPairing;
        w.OnAppleTetheringSourceFrame        += OnAppleTetheringSource;
        w.OnAppleTetheringTargetFrame        += OnAppleTetheringTarget;
        w.OnAppleMagicSwitchFrame            += OnAppleMagicSwitch;
        w.OnAppleHeySiriFrame                += OnAppleHeySiri;
        w.OnAppleHandoffFrame                += OnAppleHandoff;
        w.OnAppleFindMyFrame                 += OnAppleFindMy;
    }

    private void UnhookWatcherEvents(wclBluetoothLeBeaconWatcher w)
    {
        if (w == null) return;
        w.OnStarted -= BeaconWatcherStarted;
        w.OnStopped -= BeaconWatcherStopped;
        w.OnAdvertisementAppearanceFrame     -= OnAppearanceFrame;
        w.OnAdvertisementExtFrameInformation -= OnExtInformationFrame;
        w.OnAdvertisementFrameInformation    -= OnInformationFrame;
        w.OnAdvertisementRawFrame            -= OnRawFrame;
        w.OnAdvertisementReceived            -= OnAdvertisementReceived;
        w.OnAdvertisementService128DataFrame -= OnService128Data;
        w.OnAdvertisementService16DataFrame  -= OnService16Data;
        w.OnAdvertisementService32DataFrame  -= OnService32Data;
        w.OnAdvertisementServiceSol128Frame  -= OnSol128;
        w.OnAdvertisementServiceSol16Frame   -= OnSol16;
        w.OnAdvertisementServiceSol32Frame   -= OnSol32;
        w.OnAdvertisementTxPowerLevelFrame   -= OnTxPower;
        w.OnAdvertisementUuidFrame           -= OnUuid;
        w.OnAltBeaconFrame                   -= OnAltBeacon;
        w.OnDriAsdMessage                    -= OnDriAsd;
        w.OnEddystoneTlmFrame                -= OnEddystoneTlm;
        w.OnEddystoneUidFrame                -= OnEddystoneUid;
        w.OnEddystoneUrlFrame                -= OnEddystoneUrl;
        w.OnManufacturerRawFrame             -= OnManufacturer;
        w.OnMicrosoftCdpBeaconFrame          -= OnMicrosoftCdp;
        w.OnAppleiBeaconFrame                -= OnAppleiBeacon;
        w.OnAppleHomeKitFrame                -= OnAppleHomeKit;
        w.OnAppleNearbyActionFrame           -= OnAppleNearbyAction;
        w.OnAppleNearbyInfoFrame             -= OnAppleNearbyInfo;
        w.OnAppleUnknownFrame                -= OnAppleUnknown;
        w.OnAppleAirplaySourceFrame          -= OnAppleAirplaySource;
        w.OnAppleAirplayTargetFrame          -= OnAppleAirplayTarget;
        w.OnAppleAirdropFrame                -= OnAppleAirdrop;
        w.OnAppleAirprintFrame               -= OnAppleAirprint;
        w.OnAppleProximityPairingFrame       -= OnAppleProximityPairing;
        w.OnAppleTetheringSourceFrame        -= OnAppleTetheringSource;
        w.OnAppleTetheringTargetFrame        -= OnAppleTetheringTarget;
        w.OnAppleMagicSwitchFrame            -= OnAppleMagicSwitch;
        w.OnAppleHeySiriFrame                -= OnAppleHeySiri;
        w.OnAppleHandoffFrame                -= OnAppleHandoff;
        w.OnAppleFindMyFrame                 -= OnAppleFindMy;
    }

    // ================================================================
    //  BeaconWatcher event handlers
    // ================================================================
    private void OnAppearanceFrame(object s, long addr, long ts, sbyte rssi, ushort appearance)
        => AddFrame(new AppearanceFrame(addr, ts, rssi, appearance));

    private void OnExtInformationFrame(object s, long addr, long ts, sbyte rssi,
        wclBluetoothAddressType at, sbyte tx, wclBluetoothLeExtendedFrameFlag flags,
        wclBluetoothLeAdvertisementPhy pPhy, wclBluetoothLeAdvertisementPhy sPhy)
        => AddFrame(new ExtInformationFrame(addr, ts, rssi, at, flags, tx, pPhy, sPhy));

    private void OnInformationFrame(object s, long addr, long ts, sbyte rssi,
        string name, wclBluetoothLeAdvertisementType type, wclBluetoothLeAdvertisementFlag flags)
        => AddFrame(new InformationFrame(addr, ts, rssi, flags, name, type));

    private void OnRawFrame(object s, long addr, long ts, sbyte rssi, byte dataType, byte[] data)
        => AddFrame(new RawFrame(addr, ts, rssi, data, dataType));

    private void OnAdvertisementReceived(object s, long addr, long ts, sbyte rssi, byte[] data)
        => AddFrame(new DetailsFrame(addr, ts, rssi, data));

    private void OnService128Data(object s, long addr, long ts, sbyte rssi, Guid uuid, byte[] data)
        => AddFrame(new Service128DataFrame(addr, ts, rssi, uuid, data));

    private void OnService16Data(object s, long addr, long ts, sbyte rssi, ushort uuid, byte[] data)
        => AddFrame(new Service16DataFrame(addr, ts, rssi, data, uuid));

    private void OnService32Data(object s, long addr, long ts, sbyte rssi, uint uuid, byte[] data)
        => AddFrame(new Service32DataFrame(addr, ts, rssi, data, uuid));

    private void OnSol128(object s, long addr, long ts, sbyte rssi, Guid uuid)
        => AddFrame(new Sol128Frame(addr, ts, rssi, uuid));

    private void OnSol16(object s, long addr, long ts, sbyte rssi, ushort uuid)
        => AddFrame(new Sol16Frame(addr, ts, rssi, uuid));

    private void OnSol32(object s, long addr, long ts, sbyte rssi, uint uuid)
        => AddFrame(new Sol32Frame(addr, ts, rssi, uuid));

    private void OnTxPower(object s, long addr, long ts, sbyte rssi, sbyte tx)
        => AddFrame(new TxPowerFrame(addr, ts, rssi, tx));

    private void OnUuid(object s, long addr, long ts, sbyte rssi, Guid uuid)
        => AddFrame(new UuidFrame(addr, ts, rssi, uuid));

    private void OnAltBeacon(object s, long addr, long ts, sbyte rssi,
        ushort companyId, ushort major, ushort minor, Guid uuid, sbyte txRssi, byte reserved, byte[] data)
        => AddFrame(new AltBeaconFrame(addr, ts, rssi, data, uuid, companyId, major, minor, txRssi, reserved));

    private void OnDriAsd(object s, long addr, long ts, sbyte rssi, byte[] raw)
        => AddFrame(new DriAsdFrame(addr, ts, rssi, raw));

    private void OnEddystoneTlm(object s, long addr, long ts, sbyte rssi,
        uint advCnt, ushort batt, uint secCnt, double temp, byte[] data)
        => AddFrame(new EddystoneTlmFrame(addr, ts, rssi, data, advCnt, batt, secCnt, temp));

    private void OnEddystoneUid(object s, long addr, long ts, sbyte rssi, sbyte txRssi, Guid uuid, byte[] data)
        => AddFrame(new EddystoneUidFrame(addr, ts, rssi, data, uuid, txRssi));

    private void OnEddystoneUrl(object s, long addr, long ts, sbyte rssi, sbyte txRssi, string url)
        => AddFrame(new EddystoneUrlFrame(addr, ts, rssi, txRssi, url));

    private void OnManufacturer(object s, long addr, long ts, sbyte rssi, ushort companyId, byte[] data)
        => AddFrame(new ManufacturerFrame(addr, ts, rssi, companyId, data));

    private void OnMicrosoftCdp(object s, long addr, long ts, sbyte rssi,
        wclBluetoothLeCdpBeaconScenarioType scenario, byte version,
        wclBluetoothLeCdpBeaconDeviceType devType, byte subVersion, bool shareNearBy,
        bool addressAsDevId, wclBluetoothLeCdpBeaconExtendedDeviceStatus ext,
        byte[] salt, byte[] hash)
        => AddFrame(new MicrosoftCdpBeaconFrame(addr, ts, rssi, addressAsDevId, devType, ext,
            hash, salt, scenario, shareNearBy, subVersion, version));

    private void OnAppleiBeacon(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleiBeaconFrameData data)
        => AddFrame(new AppleiBeaconFrame(info, data));

    private void OnAppleHomeKit(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleHomeKitFrameData data)
        => AddFrame(new AppleHomeKitFrame(info, data));

    private void OnAppleNearbyAction(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleNearbyActionFrameData data)
        => AddFrame(new AppleNearbyActionFrame(info, data));

    private void OnAppleNearbyInfo(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleNearbyInfoFrameData data)
        => AddFrame(new AppleNearbyInfoFrame(info, data));

    private void OnAppleUnknown(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleUnknownFrameData data)
        => AddFrame(new AppleUnknownFrame(info, data));

    private void OnAppleAirplaySource(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirplaySourceFrameData data)
        => AddFrame(new AppleAirplaySourceFrame(info, data));

    private void OnAppleAirplayTarget(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirplayTargetFrameData data)
        => AddFrame(new AppleAirplayTargetFrame(info, data));

    private void OnAppleAirdrop(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirdropFrameData data)
        => AddFrame(new AppleAirdropFrame(info, data));

    private void OnAppleAirprint(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirprintFrameData data)
        => AddFrame(new AppleAirprintFrame(info, data));

    private void OnAppleProximityPairing(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleProximityPairingFrameData data)
        => AddFrame(new AppleProximityPairingFrame(info, data));

    private void OnAppleTetheringSource(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleTetheringSourceFrameData data)
        => AddFrame(new AppleTetheringSourceFrame(info, data));

    private void OnAppleTetheringTarget(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleTetheringTargetFrameData data)
        => AddFrame(new AppleTetheringTargetFrame(info, data));

    private void OnAppleMagicSwitch(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleMagicSwitchFrameData data)
        => AddFrame(new AppleMagicSwitchFrame(info, data));

    private void OnAppleHeySiri(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleHeySiriFrameData data)
        => AddFrame(new AppleHeySiriFrame(info, data));

    private void OnAppleHandoff(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleHandoffFrameData data)
        => AddFrame(new AppleHandoffFrame(info, data));

    private void OnAppleFindMy(object s, wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleFindMyFrameData data)
        => AddFrame(new AppleFindMyFrame(info, data));

    // ================================================================
    //  Watcher state
    // ================================================================
    private void BeaconWatcherStarted(object s, EventArgs e)
    {
        Trace("Beacon Watcher started");
        WatcherEnableControls(false);
    }

    private void BeaconWatcherStopped(object s, EventArgs e)
    {
        Trace("Beacon Watcher stopped");
        WatcherEnableControls(true);
        ClearTree();
        ClearDetails();
    }

    // ================================================================
    //  Manager events
    // ================================================================
    private void BluetoothManagerAfterOpen(object s, EventArgs e)  => Trace("Bluetooth Manager opened");
    private void BluetoothManagerClosed(object s, EventArgs e)     => Trace("Bluetooth Manager closed");

    private void BluetoothManagerBeforeClose(object s, EventArgs e)
    {
        Trace("Bluetooth Manager is closing");
        _advertiser?.Stop();
        _watcher  ?.Stop();
    }

    private void LeAdvertiserStarted(object s, EventArgs e)
    {
        Trace("LE Advertiser started");
        AdvertiserEnableControls(false);
    }

    private void LeAdvertiserStopped(object s, EventArgs e)
    {
        Trace("LE Advertiser stopped");
        AdvertiserEnableControls(true);
    }

    // ================================================================
    //  Device tree
    // ================================================================
    private DeviceNode GetOrAddDevice(long address)
    {
        if (_devicesByAddress.TryGetValue(address, out var existing)) return existing;

        var container = Instantiate(deviceNodePrefab, devicesContent);
        var vlg = container.GetComponent<VerticalLayoutGroup>();

        var deviceRowGO = Instantiate(rowPrefab, container.transform);
        var deviceRow = deviceRowGO.GetComponent<BeaconRowUI>();
        deviceRow.Init(OnRowClicked);
        deviceRow.SetIndent(0);
        deviceRow.SetDeviceText(address.ToString("X12"));
        deviceRow.SetFrameTypeText("");

        var node = new DeviceNode
        {
            Address = address,
            Name = "",
            Container = container,
            DeviceRow = deviceRow,
        };
        _devices.Add(node);
        _devicesByAddress[address] = node;

        return node;
    }

    private void AddFrame(FrameStorage frame)
    {
        var device = GetOrAddDevice(frame.Address);

        // Ищем существующий фрейм того же типа у этого же устройства.
        FrameNode existing = null;
        foreach (var f in device.Frames)
        {
            if (f.Storage.Frame == frame.Frame) { existing = f; break; }
        }

        if (existing != null)
        {
            existing.Storage = frame;
            existing.Row.Frame = frame;
        }
        else
        {
            var rowGO = Instantiate(rowPrefab, device.Container.transform);
            var row = rowGO.GetComponent<BeaconRowUI>();
            row.Init(OnRowClicked);
            row.SetIndent(30);
            row.SetDeviceText("");
            row.SetFrameTypeText(FrameTypeName(frame.Frame));
            row.Frame = frame;

            device.Frames.Add(new FrameNode
            {
                Storage = frame,
                Row = row,
                Label = FrameTypeName(frame.Frame),
            });
        }

        // Обновим имя устройства по InformationFrame, как в оригинале.
        if (frame.Frame == AdvertisementFrame.afInformation)
        {
            var info = (InformationFrame)frame;
            if (!string.IsNullOrEmpty(info.Name))
            {
                device.Name = info.Name;
                device.DeviceRow.SetFrameTypeText(info.Name);
            }
        }

        // Если строка выбрана — обновим детали.
        if (_selectedRow != null && ReferenceEquals(_selectedRow.Frame, frame))
            ShowFrameData(frame);
    }

    private static string FrameTypeName(AdvertisementFrame f)
    {
        switch (f)
        {
            case AdvertisementFrame.afAppearance:           return "Appearance";
            case AdvertisementFrame.afExtInformation:       return "Ext Information";
            case AdvertisementFrame.afInformation:          return "Information";
            case AdvertisementFrame.afRaw:                  return "Raw";
            case AdvertisementFrame.afDetails:              return "Details";
            case AdvertisementFrame.afService16Data:        return "Service 16";
            case AdvertisementFrame.afService32Data:        return "Service 32";
            case AdvertisementFrame.afService128Data:       return "Service 128";
            case AdvertisementFrame.afSol16:                return "16 SOL";
            case AdvertisementFrame.afSol32:                return "32 SOL";
            case AdvertisementFrame.afSol128:               return "128 SOL";
            case AdvertisementFrame.afTxPower:              return "Tx Power";
            case AdvertisementFrame.afUuid:                 return "UUID";
            case AdvertisementFrame.afAltBeacon:            return "Alt Beacon";
            case AdvertisementFrame.afEddystoneTlm:         return "Eddystone TLM";
            case AdvertisementFrame.afEddystoneUid:         return "Eddystone UID";
            case AdvertisementFrame.afEddystoneUrl:         return "Eddystone URL";
            case AdvertisementFrame.afManufacturerRaw:      return "Manufacturer";
            case AdvertisementFrame.afMicrosoftCdpBeacon:   return "Microsoft CDP";
            case AdvertisementFrame.afAppleAirdrop:         return "Apple AirDrop";
            case AdvertisementFrame.afAppleAirplaySource:   return "Apple Airplay Source";
            case AdvertisementFrame.afAppleAirplayTarget:   return "Apple Airplay Target";
            case AdvertisementFrame.afAppleAirprint:        return "Apple AirPrint";
            case AdvertisementFrame.afAppleHomeKit:         return "Apple Home Kit";
            case AdvertisementFrame.afAppleiBeacon:         return "Apple iBeacon";
            case AdvertisementFrame.afAppleNearbyAction:    return "Apple Nearby Action";
            case AdvertisementFrame.afAppleNearbyInfo:      return "Apple Nearby Info";
            case AdvertisementFrame.afAppleProximityPairing:return "Apple Proximity Pairing";
            case AdvertisementFrame.afAppleUnknown:         return "Apple Unknown";
            case AdvertisementFrame.afAppleFindMy:          return "Apple Find My";
            case AdvertisementFrame.afAppleHandoff:         return "Apple Handoff";
            case AdvertisementFrame.afAppleHeySiri:         return "Apple Hey Siri";
            case AdvertisementFrame.afAppleMagicSwitch:     return "Apple Magic Switch";
            case AdvertisementFrame.afAppleTetheringSource: return "Apple Tethering Source";
            case AdvertisementFrame.afAppleTetheringTarget: return "Apple Tethering Target";
            case AdvertisementFrame.afDriAsd:               return "DRI ASD";
            default:                                        return "Unknown";
        }
    }

    private void ClearTree()
    {
        foreach (var d in _devices)
        {
            for (int i = d.Container.transform.childCount - 1; i >= 0; i--)
                Destroy(d.Container.transform.GetChild(i).gameObject);
            Destroy(d.Container);
        }
        _devices.Clear();
        _devicesByAddress.Clear();
        _selectedRow = null;
    }

    private void OnRowClicked(BeaconRowUI row)
    {
        if (_selectedRow != null) _selectedRow.SetSelected(false);
        _selectedRow = row;
        if (_selectedRow != null) _selectedRow.SetSelected(true);

        if (row != null && row.Frame != null) ShowFrameData(row.Frame);
        else ClearDetails();
    }

    // ================================================================
    //  Buttons
    // ================================================================
    private void OnWatcherStartClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;

        _watcher.AllowExtendedAdvertisements = enableExtendedToggle.isOn;

        _watcher.ScanningMode = (scanModeDropdown.value == 0)
            ? wclBluetoothLeScanningMode.smActive
            : wclBluetoothLeScanningMode.smPassive;

        switch (scanParamsDropdown.value)
        {
            case 0: _watcher.ScanParametersType = wclBluetoothLeScanParametersType.ptDefault; break;
            case 1: _watcher.ScanParametersType = wclBluetoothLeScanParametersType.ptCoexistenceOptimized; break;
            case 2: _watcher.ScanParametersType = wclBluetoothLeScanParametersType.ptLowLatency; break;
            default:_watcher.ScanParametersType = wclBluetoothLeScanParametersType.ptCustom; break;
        }

        _watcher.ScanInterval = ParseUShort(scanIntervalInput.text, _watcher.ScanInterval);
        _watcher.ScanWindow   = ParseUShort(scanWindowInput.text,   _watcher.ScanWindow);

        Int32 res = _watcher.Start(radio);
        if (res != wclErrors.WCL_E_SUCCESS) Trace("Start Watched", res);
    }

    private void OnWatcherStopClicked()
    {
        Int32 res = _watcher.Stop();
        if (res != wclErrors.WCL_E_SUCCESS) Trace("Beacon Watcher stop", res);
    }

    private void OnAdvertiserStartClicked()
    {
        Guid BEACON_UUID = new Guid("{09039835-4A80-443B-87AA-DC565D09EA61}");

        var radio = GetRadio();
        if (radio == null) return;

        _advertiser.Clear();

        wclBluetoothLeAdvertisement adv;

        if (advBeaconToggle.isOn)
        {
            adv = new wclBluetoothLeiBeaconAdvertisement(-5, 0x0101, 0x0202, BEACON_UUID);
            AddAdvertisement(adv);
        }
        if (advProximityToggle.isOn)
        {
            adv = new wclBluetoothLeProximityBeaconAdvertisement(-5, 0x0101, 0x0202, BEACON_UUID, 0xFFFE);
            AddAdvertisement(adv);
        }
        if (advAltBeaconToggle.isOn)
        {
            adv = new wclBluetoothLeAltBeaconAdvertisement(-5, 0x0101, 0x0202, BEACON_UUID, 0xFFFE, 0x11);
            AddAdvertisement(adv);
        }
        if (advEddystoneUidToggle.isOn)
        {
            adv = new wclBluetoothLeEddystoneUidBeaconAdvertisement(-5, BEACON_UUID);
            AddAdvertisement(adv);
        }
        if (advEddystoneUrlToggle.isOn)
        {
            adv = new wclBluetoothLeEddystoneUrlBeaconAdvertisement(-5, "https://www.btframework.com");
            AddAdvertisement(adv);
        }

        byte[] data;
        if (adv128SolUuidToggle.isOn)
        {
            data = new byte[16] { 0xD0, 0x00, 0x2D, 0x12, 0x1E, 0x4B, 0x0F, 0xA4,
                                  0x99, 0x4E, 0xCE, 0xB5, 0x31, 0xF4, 0x05, 0x79 };
            adv = new wclBluetoothLeCustomAdvertisement(wclUUIDs.LE_GAP_AD_TYPE_SERVICE_SOL_UUIDS_128, data);
            AddAdvertisement(adv);
        }
        if (advManufacturerToggle.isOn)
        {
            adv = new wclBluetoothLeManufacturerAdvertisement(0x010E, new byte[] { 0x12, 0x34 });
            AddAdvertisement(adv);
        }
        if (adv16UuidToggle.isOn)
        {
            adv = new wclBluetoothLe16ServiceAdvertisement(0x9835);
            AddAdvertisement(adv);
        }
        if (adv32UuidToggle.isOn)
        {
            adv = new wclBluetoothLe32ServiceAdvertisement(0x09039835);
            AddAdvertisement(adv);
        }
        if (adv128UuidToggle.isOn)
        {
            adv = new wclBluetoothLe128ServiceAdvertisement(BEACON_UUID);
            AddAdvertisement(adv);
        }
        if (adv16UuidDataToggle.isOn)
        {
            adv = new wclBluetoothLe16ServiceDataAdvertisement(0x9835, new byte[] { 0x12, 0x34 });
            AddAdvertisement(adv);
        }
        if (adv32UuidDataToggle.isOn)
        {
            adv = new wclBluetoothLe32ServiceDataAdvertisement(0x09039835, new byte[] { 0x12, 0x34 });
            AddAdvertisement(adv);
        }
        if (adv128UuidDataToggle.isOn)
        {
            adv = new wclBluetoothLe128ServiceDataAdvertisement(BEACON_UUID, new byte[] { 0x12, 0x34 });
            AddAdvertisement(adv);
        }
        if (advCustomToggle.isOn)
        {
            data = new byte[20]
            {
                0x09, wclUUIDs.LE_GAP_AD_TYPE_SERVICE_DATA_16,
                0x12, 0x34, 0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA,
                0x09, wclUUIDs.LE_GAP_AD_TYPE_MANUFACTURER,
                0x10, 0x12, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06
            };
            adv = new wclBluetoothLeRawAdvertisement(data);
            AddAdvertisement(adv);
        }

        if (_advertiser.Count == 0)
        {
            Trace("Select at least one advertisement type.");
            return;
        }

        _advertiser.Interval = ParseUShort(advertiserIntervalInput.text, _advertiser.Interval);

        _advertiser.UseExtendedAdvertisement = advExtendedToggle.isOn;
        _advertiser.Anonymous = advAnonymousToggle.isOn;
        _advertiser.IncludeTxRssi = advTxRssiToggle.isOn;
        _advertiser.PreferredTxRssi = 10;

        _advertiser.PrimaryPhy   = PhyFromDropdown(primaryPhyDropdown.value);
        _advertiser.SecondaryPhy = PhyFromDropdown(secondaryPhyDropdown.value);

        Int32 res = _advertiser.Start(radio);
        if (res != wclErrors.WCL_E_SUCCESS) Trace("Start advertiser", res);
    }

    private void OnAdvertiserStopClicked()
    {
        Int32 res = _advertiser.Stop();
        if (res != wclErrors.WCL_E_SUCCESS) Trace("Advertiser stop:", res);
    }

    private void OnAdvertiserDefaultClicked() => AdvertiserDefaults();

    private void OnClearLogClicked()
    {
        if (logText != null) logText.text = string.Empty;
    }

    private void OnAdvertiserExtendedChanged(bool value)
    {
        advAnonymousToggle.interactable = value;
        advTxRssiToggle.interactable    = value;
    }

    private void OnScanParamsChanged(int idx)
    {
        bool custom = (idx == 3);
        scanIntervalInput.interactable = custom;
        scanWindowInput.interactable = custom;
    }

    // ================================================================
    //  Helpers
    // ================================================================
    private void AddAdvertisement(wclBluetoothLeAdvertisement adv)
    {
        Int32 res = _advertiser.Add(adv);
        if (res != wclErrors.WCL_E_SUCCESS) Trace("Add advertisement", res);
    }

    private wclBluetoothLeAdvertisementPhy PhyFromDropdown(int idx)
    {
        switch (idx)
        {
            case 1: return wclBluetoothLeAdvertisementPhy.phUncoded1M;
            case 2: return wclBluetoothLeAdvertisementPhy.phUncoded2M;
            case 3: return wclBluetoothLeAdvertisementPhy.phCoded;
            default:return wclBluetoothLeAdvertisementPhy.phUnspecified;
        }
    }

    private static ushort ParseUShort(string s, ushort fallback)
    {
        return ushort.TryParse(s, out var v) ? v : fallback;
    }

    private wclBluetoothRadio GetRadio()
    {
        Int32 res = wclErrors.WCL_E_SUCCESS;
        if (!_btManager.Active) res = _btManager.Open();
        if (res != wclErrors.WCL_E_SUCCESS) { Trace("Bluetooth Manager open", res); return null; }

        wclBluetoothRadio radio;
        res = _btManager.GetLeRadio(out radio);
        if (res != wclErrors.WCL_E_SUCCESS) { Trace("Get working radio", res); return null; }

        Trace("Use " + radio.ApiName + " radio");
        return radio;
    }

    private void AdvertiserDefaults()
    {
        _advertiser.RestoreDefaults();
        advertiserIntervalInput.text = _advertiser.Interval.ToString();

        advBeaconToggle.isOn = true;
        advProximityToggle.isOn = false;
        advAltBeaconToggle.isOn = false;
        advEddystoneUidToggle.isOn = false;
        advEddystoneUrlToggle.isOn = false;
        adv128SolUuidToggle.isOn = false;
        advManufacturerToggle.isOn = false;
        adv16UuidToggle.isOn = false;
        adv32UuidToggle.isOn = false;
        adv128UuidToggle.isOn = false;
        adv16UuidDataToggle.isOn = false;
        adv32UuidDataToggle.isOn = false;
        adv128UuidDataToggle.isOn = false;
        advCustomToggle.isOn = false;

        advExtendedToggle.isOn = false;
        advAnonymousToggle.isOn = false;
        advTxRssiToggle.isOn = false;

        advAnonymousToggle.interactable = false;
        advTxRssiToggle.interactable = false;
    }

    private void AdvertiserEnableControls(bool stopped)
    {
        advertiserStartButton.interactable = stopped;
        advertiserStopButton.interactable  = !stopped;
        advertiserDefaultButton.interactable = stopped;

        advertiserIntervalInput.interactable = stopped;

        advBeaconToggle.interactable = stopped;
        advProximityToggle.interactable = stopped;
        advAltBeaconToggle.interactable = stopped;
        advEddystoneUidToggle.interactable = stopped;
        advEddystoneUrlToggle.interactable = stopped;
        adv128SolUuidToggle.interactable = stopped;
        advManufacturerToggle.interactable = stopped;
        adv16UuidToggle.interactable = stopped;
        adv32UuidToggle.interactable = stopped;
        adv128UuidToggle.interactable = stopped;
        adv16UuidDataToggle.interactable = stopped;
        adv32UuidDataToggle.interactable = stopped;
        adv128UuidDataToggle.interactable = stopped;
        advCustomToggle.interactable = stopped;

        advExtendedToggle.interactable = stopped;
        advAnonymousToggle.interactable = stopped && advExtendedToggle.isOn;
        advTxRssiToggle.interactable    = stopped && advExtendedToggle.isOn;
    }

    private void WatcherEnableControls(bool stopped)
    {
        scanModeDropdown.interactable = stopped;
        enableExtendedToggle.interactable = stopped;

        scanParamsDropdown.interactable = stopped;
        bool customEnabled = stopped && scanParamsDropdown.value == 3;
        scanIntervalInput.interactable = customEnabled;
        scanWindowInput.interactable   = customEnabled;

        watcherStartButton.interactable = stopped;
        watcherStopButton.interactable  = !stopped;
    }

    // ================================================================
    //  Log
    // ================================================================
    private void Trace(string msg)
    {
        Debug.Log("[Beacons] " + msg);
        if (logText == null) return;

        logText.text += (logText.text.Length > 0 ? "\n" : "") + msg;

        if (autoScrollToggle != null && autoScrollToggle.isOn && logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void Trace(string msg, Int32 res) => Trace(msg + " failed: 0x" + res.ToString("X8"));

    // ================================================================
    //  Frame details
    // ================================================================
    private void ClearDetails()
    {
        if (detailsContent == null) return;
        for (int i = detailsContent.childCount - 1; i >= 0; i--)
            Destroy(detailsContent.GetChild(i).gameObject);
    }

    private void ShowData(string name, string value)
    {
        if (detailRowPrefab == null || detailsContent == null) return;
        var go = Instantiate(detailRowPrefab, detailsContent);
        var row = go.GetComponent<BeaconDetailRowUI>();
        row.Set(name, value);
    }

    private DateTime ConvertTime(long timestamp) => DateTime.FromFileTime(timestamp);

    private void ShowFrameBaseData(FrameStorage f)
    {
        ShowData("Address",   f.Address.ToString("X12"));
        ShowData("Timestamp", ConvertTime(f.Timestamp).ToString());
        ShowData("RSSI",      f.Rssi.ToString());
    }

    private void ShowFrameRawData(byte[] data)
    {
        string first = "Data";
        if (data == null || data.Length == 0) return;

        string s = "";
        byte c = 0;
        for (int i = 0; i < data.Length; i++)
        {
            s += data[i].ToString("X2") + " ";
            if (c == 15) { ShowData(first, s); first = ""; s = ""; c = 0; }
            else c++;
        }
        if (s != "") ShowData(first, s);
    }

    private void ShowFrameData(FrameStorage f)
    {
        ClearDetails();
        switch (f.Frame)
        {
            case AdvertisementFrame.afAppearance:
            {
                var x = (AppearanceFrame)f;
                ShowFrameBaseData(x);
                ShowData("Appearance", x.Appearance.ToString("X4"));
                break;
            }
            case AdvertisementFrame.afExtInformation:
            {
                var x = (ExtInformationFrame)f;
                ShowFrameBaseData(x);
                ShowData("Address type", AddressTypeStr(x.AddressType));
                ShowData("Flags", ExtFlagsStr(x.Flags));
                ShowData("TX Power", x.TxPower.ToString());
                ShowData("Primary PHY",   PhyStr(x.PrimaryPhy));
                ShowData("Secondary PHY", PhyStr(x.SecondaryPhy));
                break;
            }
            case AdvertisementFrame.afInformation:
            {
                var x = (InformationFrame)f;
                ShowFrameBaseData(x);
                ShowData("Flags", InfoFlagsStr(x.Flags));
                ShowData("Name", x.Name);
                ShowData("Packet Type", PacketTypeStr(x.PacketType));
                break;
            }
            case AdvertisementFrame.afRaw:
            {
                var x = (RawFrame)f;
                ShowFrameBaseData(x);
                ShowData("Data Type", x.DataType.ToString("X2"));
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afDetails:
            {
                var x = (DetailsFrame)f;
                ShowFrameBaseData(x);
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afService16Data:
            {
                var x = (Service16DataFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString("X4"));
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afService32Data:
            {
                var x = (Service32DataFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString("X8"));
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afService128Data:
            {
                var x = (Service128DataFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString());
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afSol16:
            {
                var x = (Sol16Frame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString("X4"));
                break;
            }
            case AdvertisementFrame.afSol32:
            {
                var x = (Sol32Frame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString("X8"));
                break;
            }
            case AdvertisementFrame.afSol128:
            {
                var x = (Sol128Frame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString());
                break;
            }
            case AdvertisementFrame.afTxPower:
            {
                var x = (TxPowerFrame)f;
                ShowFrameBaseData(x);
                ShowData("TX Power", x.TxPower.ToString());
                break;
            }
            case AdvertisementFrame.afUuid:
            {
                var x = (UuidFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString());
                break;
            }
            case AdvertisementFrame.afAltBeacon:
            {
                var x = (AltBeaconFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString());
                ShowData("CompanyId", x.CompanyId.ToString("X4"));
                ShowData("Major", x.Major.ToString("X4"));
                ShowData("Minor", x.Minor.ToString("X4"));
                ShowData("TX RSSI", x.TxRssi.ToString());
                ShowData("Reserved", x.Reserved.ToString("X2"));
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afEddystoneTlm:
            {
                var x = (EddystoneTlmFrame)f;
                ShowFrameBaseData(x);
                ShowData("Advertisements Counter", x.AdvCnt.ToString());
                ShowData("Battery status", x.Batt.ToString());
                ShowData("Seconds", x.SecCnt.ToString());
                ShowData("Temperature", x.Temp.ToString());
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afEddystoneUid:
            {
                var x = (EddystoneUidFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Uuid.ToString());
                ShowData("TX RSSI", x.TxRssi.ToString());
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afEddystoneUrl:
            {
                var x = (EddystoneUrlFrame)f;
                ShowFrameBaseData(x);
                ShowData("TX RSSI", x.TxRssi.ToString());
                ShowData("URL", x.Url);
                break;
            }
            case AdvertisementFrame.afManufacturerRaw:
            {
                var x = (ManufacturerFrame)f;
                ShowFrameBaseData(x);
                ShowData("Company ID", x.CompanyId.ToString("X4"));
                ShowFrameRawData(x.Data);
                break;
            }
            case AdvertisementFrame.afMicrosoftCdpBeacon:
                ShowMicrosoftCdp((MicrosoftCdpBeaconFrame)f);
                break;
            case AdvertisementFrame.afAppleAirdrop:
                ShowAppleAirdrop((AppleAirdropFrame)f);
                break;
            case AdvertisementFrame.afAppleAirplaySource:
            {
                var x = (AppleAirplaySourceFrame)f;
                ShowFrameBaseData(x);
                ShowData("Data byte", x.Data.Data.ToString("X2"));
                break;
            }
            case AdvertisementFrame.afAppleAirplayTarget:
                ShowAppleAirplayTarget((AppleAirplayTargetFrame)f);
                break;
            case AdvertisementFrame.afAppleAirprint:
                ShowAppleAirprint((AppleAirprintFrame)f);
                break;
            case AdvertisementFrame.afAppleHomeKit:
                ShowAppleHomeKit((AppleHomeKitFrame)f);
                break;
            case AdvertisementFrame.afAppleiBeacon:
            {
                var x = (AppleiBeaconFrame)f;
                ShowFrameBaseData(x);
                ShowData("UUID", x.Data.Uuid.ToString());
                ShowData("Major", x.Data.Major.ToString("X4"));
                ShowData("Minor", x.Data.Minor.ToString("X4"));
                ShowData("TX RSSI", x.Data.TxRssi.ToString());
                break;
            }
            case AdvertisementFrame.afAppleNearbyAction:
                ShowAppleNearbyAction((AppleNearbyActionFrame)f);
                break;
            case AdvertisementFrame.afAppleNearbyInfo:
                ShowAppleNearbyInfo((AppleNearbyInfoFrame)f);
                break;
            case AdvertisementFrame.afAppleProximityPairing:
                ShowAppleProximityPairing((AppleProximityPairingFrame)f);
                break;
            case AdvertisementFrame.afAppleUnknown:
            {
                var x = (AppleUnknownFrame)f;
                ShowFrameBaseData(x);
                ShowData("Message ID", x.Data.Id.ToString("X2"));
                if (x.Data.Payload != null && x.Data.Payload.Length > 0)
                    ShowData("Payload", HexStr(x.Data.Payload));
                break;
            }
            case AdvertisementFrame.afAppleFindMy:
                ShowAppleFindMy((AppleFindMyFrame)f);
                break;
            case AdvertisementFrame.afAppleHandoff:
                ShowAppleHandoff((AppleHandoffFrame)f);
                break;
            case AdvertisementFrame.afAppleHeySiri:
                ShowAppleHeySiri((AppleHeySiriFrame)f);
                break;
            case AdvertisementFrame.afAppleMagicSwitch:
                ShowAppleMagicSwitch((AppleMagicSwitchFrame)f);
                break;
            case AdvertisementFrame.afAppleTetheringSource:
                ShowAppleTetheringSource((AppleTetheringSourceFrame)f);
                break;
            case AdvertisementFrame.afAppleTetheringTarget:
            {
                var x = (AppleTetheringTargetFrame)f;
                ShowFrameBaseData(x);
                ShowData("Id", x.Data.Id.ToString("X8"));
                break;
            }
            case AdvertisementFrame.afDriAsd:
            {
                var x = (DriAsdFrame)f;
                ShowFrameBaseData(x);
                if (x.Raw != null && x.Raw.Length > 0) ShowFrameRawData(x.Raw);
                break;
            }
        }
    }

    // -------------------- Apple show helpers --------------------
    private void ShowMicrosoftCdp(MicrosoftCdpBeaconFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Address As Device ID", x.AddressAsDeviceId.ToString());
        ShowData("Device Type", DeviceTypeStr(x.DeviceType));
        ShowData("Extended Device Status", ExtDeviceStatusStr(x.ExtendedDeviceStatus));
        ShowData("Hash", HexStr(x.Hash));
        ShowData("Salt", HexStr(x.Salt));
        ShowData("Scenario Type", x.ScenarioType == wclBluetoothLeCdpBeaconScenarioType.stBluetooth
            ? "Bluetooth" : ((byte)x.ScenarioType).ToString("X2"));
        ShowData("Share Near By", x.ShareNearBy.ToString());
        ShowData("Subversion", x.SubVersion.ToString("X2"));
        ShowData("Version", x.Version.ToString("X2"));
    }

    private void ShowAppleAirdrop(AppleAirdropFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Prefix", HexStr(x.Data.Prefix));
        ShowData("Version", x.Data.Version.ToString("X2"));
        ShowData("Apple ID", x.Data.AppleId.ToString("X4"));
        ShowData("Phone", x.Data.PhoneNumber.ToString("X4"));
        ShowData("E-Mail", x.Data.Email.ToString("X4"));
        ShowData("E-Mail 2", x.Data.Email2.ToString("X4"));
        ShowData("Suffix", x.Data.Suffix.ToString("X2"));
    }

    private void ShowAppleAirplayTarget(AppleAirplayTargetFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Flags", x.Data.Flags.ToString("X2"));
        ShowData("Seed", x.Data.Seed.ToString("X2"));
        string ip = ((byte)(x.Data.Address >> 24)) + "." +
                    ((byte)((x.Data.Address & 0x00FF0000) >> 16)) + "." +
                    ((byte)((x.Data.Address & 0x0000FF00) >> 8)) + "." +
                    ((byte)(x.Data.Address & 0x000000FF));
        ShowData("IPv4 address", ip);
    }

    private void ShowAppleAirprint(AppleAirprintFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Address type",  x.Data.AddressType.ToString("X2"));
        ShowData("Resource type", x.Data.ResourceType.ToString("X2"));
        ShowData("Security type", x.Data.SecurityType.ToString("X2"));
        ShowData("Port", x.Data.Port.ToString());

        string addr = "";
        for (byte i = 0; i < 15; i++) addr += x.Data.Address[i] + ".";
        addr += x.Data.Address[15];
        ShowData("Address", addr);
        ShowData("Power", x.Data.Power.ToString());
    }

    private void ShowAppleHomeKit(AppleHomeKitFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Status", x.Data.Status.ToString("X2"));
        ShowData("Device ID", HexStr(x.Data.DeviceId));
        ShowData("Category", HomeKitCategoryStr(x.Data.Category));
        ShowData("State Number", x.Data.StateNumber.ToString("X4"));
        ShowData("Config Number", x.Data.ConfigNumber.ToString("X2"));
        ShowData("Version", x.Data.Version.ToString("X2"));
    }

    private void ShowAppleNearbyAction(AppleNearbyActionFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Flags", x.Data.Flags.ToString("X2"));
        ShowData("Action", NearbyActionStr(x.Data.Action));
        ShowData("Tag", x.Data.Tag.ToString("X6"));
        ShowData("Device", NearbyDeviceStr(x.Data.Device));
        if (x.Data.Params != null && x.Data.Params.Length > 0)
            ShowData("Params", HexStr(x.Data.Params));
    }

    private void ShowAppleNearbyInfo(AppleNearbyInfoFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Primary device", ((x.Data.Status & 0x01) != 0) ? "YES" : "NO");
        ShowData("AirDrop", ((x.Data.Status & 0x04) != 0) ? "RECEIVING" : "NOT ACTIVE");
        ShowData("Action", NearbyInfoActionStr(x.Data.Action));
        ShowData("AirDrop state", ((x.Data.Flags & 0x01) != 0) ? "Connected and Screen ON" : "Disconnected");
        ShowData("Auth tag length", ((x.Data.Flags & 0x02) != 0) ? "4 bytes" : "3 bytes");
        ShowData("WiFi", ((x.Data.Flags & 0x04) != 0) ? "ON" : "OFF");
        ShowData("Auth Tag Present", ((x.Data.Flags & 0x10) != 0) ? "YES" : "NO");
        ShowData("Apple Watch", ((x.Data.Flags & 0x20) != 0) ? "LOCKED" : "UNLOCKED");
        ShowData("Apple Watch AutoLock", ((x.Data.Flags & 0x40) != 0) ? "ENABLED" : "DISABLED");
        ShowData("AutoLock", ((x.Data.Flags & 0x80) != 0) ? "ENABLED" : "DISABLED");
        if ((x.Data.Flags & 0x10) != 0)
        {
            if ((x.Data.Flags & 0x02) != 0) ShowData("Auth Tag", x.Data.Tag.ToString("X8"));
            else                            ShowData("Auth Tag", x.Data.Tag.ToString("X6"));
        }
    }

    private void ShowAppleProximityPairing(AppleProximityPairingFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Prefix", x.Data.Prefix.ToString("X2"));
        ShowData("Device mode", ProximityModelStr(x.Data.DeviceModel));
        ShowData("Status", ProximityStatusStr(x.Data.Status));
        ShowData("Left Battery", x.Data.LeftBattery.ToString());
        ShowData("Right Battery", x.Data.RightBattery.ToString());
        ShowData("Case charging", x.Data.CaseCharging ? "Yes" : "No");
        ShowData("Right charging", x.Data.RightCharging ? "Yes" : "No");
        ShowData("Left charging", x.Data.LeftCharging ? "Yes" : "No");
        ShowData("Case Battery", x.Data.CaseBattery.ToString());
        ShowData("Lid open counter", x.Data.Counter.ToString());
        ShowData("Color", ProximityColorStr(x.Data.Color));
        ShowData("Suffix", x.Data.Suffix.ToString("X2"));
        ShowData("Encrypted", HexStr(x.Data.Data));
    }

    private void ShowAppleFindMy(AppleFindMyFrame x)
    {
        ShowFrameBaseData(x);
        switch (x.Data.Status)
        {
            case 0x00: ShowData("Status", "Owner did not connect within key rotation period (15 min.)"); break;
            case 0x24: ShowData("Status", "Owner connected with key roation period, Battery Full"); break;
            case 0x64: ShowData("Status", "Owner connected with key roation period, Battery Medium"); break;
            case 0xA4: ShowData("Status", "Owner connected with key roation period, Battery Low"); break;
            case 0xE4: ShowData("Status", "Owner connected with key roation period, Battery Critically Low"); break;
            default:   ShowData("Status", x.Data.Status.ToString("X2")); break;
        }
        ShowData("Key", HexStr(x.Data.Key));
        switch (x.Data.KeyBits)
        {
            case 0x00: ShowData("Key bits", "bits 6 and 7 not set in public key"); break;
            case 0x01: ShowData("Key bits", "bit 6 set in public key"); break;
            case 0x02: ShowData("Key bits", "bit 7 set in public key"); break;
            case 0x03: ShowData("Key bits", "bits 6 and 7 set in public key"); break;
            default:   ShowData("Key bits", x.Data.KeyBits.ToString("X2")); break;
        }
        ShowData("Hint", x.Data.Hint.ToString("X2"));
    }

    private void ShowAppleHandoff(AppleHandoffFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Clipboard", x.Data.Clipboard ? "Yes" : "No");
        ShowData("Sequence", x.Data.Sequence.ToString());
        ShowData("Auth tag", x.Data.AuthTag.ToString("X2"));
        ShowData("Data", HexStr(x.Data.Data));
    }

    private void ShowAppleHeySiri(AppleHeySiriFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Hash", x.Data.Hash.ToString("X4"));
        ShowData("Snr", x.Data.Snr.ToString());
        switch (x.Data.Confidence)
        {
            case 0x03: ShowData("Confidence", "Not on Wrist"); break;
            case 0x1F: ShowData("Confidence", "Wrist detection disabled"); break;
            case 0x3F: ShowData("Confidence", "On Wrist"); break;
            default:   ShowData("Confidence", x.Data.Confidence.ToString("X2")); break;
        }
        switch (x.Data.DeviceClass)
        {
            case 0x0002: ShowData("Device class", "iPhone"); break;
            case 0x0003: ShowData("Device class", "iPad"); break;
            case 0x0007: ShowData("Device class", "HomePod"); break;
            case 0x0009: ShowData("Device class", "MacBook"); break;
            case 0x000A: ShowData("Device class", "Watch"); break;
            default:     ShowData("Device class", x.Data.DeviceClass.ToString("X4")); break;
        }
        ShowData("Rand", x.Data.Rand.ToString("X2"));
    }

    private void ShowAppleMagicSwitch(AppleMagicSwitchFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Data", x.Data.Data.ToString("X4"));
        switch (x.Data.Confidence)
        {
            case 0x03: ShowData("Confidence", "Not on Wrist"); break;
            case 0x1F: ShowData("Confidence", "Wrist detection disabled"); break;
            case 0x3F: ShowData("Confidence", "On Wrist"); break;
            default:   ShowData("Confidence", x.Data.Confidence.ToString("X2")); break;
        }
    }

    private void ShowAppleTetheringSource(AppleTetheringSourceFrame x)
    {
        ShowFrameBaseData(x);
        ShowData("Version", x.Data.Version.ToString("X2"));
        ShowData("Flags", x.Data.Flags.ToString("X2"));
        ShowData("Battery", x.Data.Battery.ToString());
        ShowData("Cell type", CellTypeStr(x.Data.CellType));
        ShowData("Bars", x.Data.Bars.ToString());
    }

    // -------------------- lookup-таблицы --------------------
    private static string HexStr(byte[] data)
    {
        if (data == null || data.Length == 0) return "";
        string s = "";
        foreach (var b in data) s += b.ToString("X2");
        return s;
    }

    private static string AddressTypeStr(wclBluetoothAddressType t)
    {
        switch (t)
        {
            case wclBluetoothAddressType.atClassic:     return "Classic";
            case wclBluetoothAddressType.atPublic:      return "Public";
            case wclBluetoothAddressType.atRandom:      return "Random";
            case wclBluetoothAddressType.atUnspecified: return "Unpecified";
            default:                                    return "Unknown";
        }
    }

    private static string ExtFlagsStr(wclBluetoothLeExtendedFrameFlag f)
    {
        string s = "";
        if ((f & wclBluetoothLeExtendedFrameFlag.efAnonymous)     != 0) s += " [Anonymous]";
        if ((f & wclBluetoothLeExtendedFrameFlag.efConnectable)   != 0) s += " [Connectable]";
        if ((f & wclBluetoothLeExtendedFrameFlag.efDirected)      != 0) s += " [Directed]";
        if ((f & wclBluetoothLeExtendedFrameFlag.efScannable)     != 0) s += " [Scannable]";
        if ((f & wclBluetoothLeExtendedFrameFlag.efScanResponse)  != 0) s += " [Scan Response]";
        return s + " ";
    }

    private static string InfoFlagsStr(wclBluetoothLeAdvertisementFlag f)
    {
        string s = "";
        if ((f & wclBluetoothLeAdvertisementFlag.afLimitedDiscoverableMode)   != 0) s += " [Limited Discoverable Mode]";
        if ((f & wclBluetoothLeAdvertisementFlag.afGeneralDiscoverableMode)   != 0) s += " [General Discoverable Mode]";
        if ((f & wclBluetoothLeAdvertisementFlag.afClassicNotSupported)       != 0) s += " [Classic Not Supported]";
        if ((f & wclBluetoothLeAdvertisementFlag.afDualModeControllerCapable) != 0) s += " [Dual Mode Controller Capable]";
        if ((f & wclBluetoothLeAdvertisementFlag.afDualModeHostCapable)       != 0) s += " [Dual Mode Host Capable]";
        return s + " ";
    }

    private static string PhyStr(wclBluetoothLeAdvertisementPhy p)
    {
        switch (p)
        {
            case wclBluetoothLeAdvertisementPhy.phUncoded1M: return "Uncoded 1M";
            case wclBluetoothLeAdvertisementPhy.phUncoded2M: return "Uncoded 2M";
            case wclBluetoothLeAdvertisementPhy.phCoded:     return "Coded";
            default:                                         return "Unspecified";
        }
    }

    private static string PacketTypeStr(wclBluetoothLeAdvertisementType t)
    {
        switch (t)
        {
            case wclBluetoothLeAdvertisementType.atConnectableUndirected:    return "Connectable Undirected";
            case wclBluetoothLeAdvertisementType.atConnectableDirected:      return "Connectable Directed";
            case wclBluetoothLeAdvertisementType.atScannableUndirected:      return "Scannable Undirected";
            case wclBluetoothLeAdvertisementType.atNonConnectableUndirected: return "Non Connectable Undirected";
            case wclBluetoothLeAdvertisementType.atScanResponse:             return "Scan Response";
            case wclBluetoothLeAdvertisementType.atExtended:                 return "Extended";
            default:                                                         return "Unknown";
        }
    }

    private static string DeviceTypeStr(wclBluetoothLeCdpBeaconDeviceType t)
    {
        switch (t)
        {
            case wclBluetoothLeCdpBeaconDeviceType.dtXboxOne:        return "Xbox one";
            case wclBluetoothLeCdpBeaconDeviceType.dtiPhone:         return "iPhone";
            case wclBluetoothLeCdpBeaconDeviceType.dtiPad:           return "iPad";
            case wclBluetoothLeCdpBeaconDeviceType.dtAndroid:        return "Android";
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsDesktop: return "Windows Desktop";
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsPhone:   return "Windows Phone";
            case wclBluetoothLeCdpBeaconDeviceType.dtLinux:          return "Linux";
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsIoT:     return "Windows IoT";
            case wclBluetoothLeCdpBeaconDeviceType.dtSurfaceHub:     return "Surface Hub";
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsLaptop:  return "Windows Laptop";
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsTablet:  return "Windows Tablet";
            default:                                                  return ((byte)t).ToString("X2");
        }
    }

    private static string ExtDeviceStatusStr(wclBluetoothLeCdpBeaconExtendedDeviceStatus s)
    {
        string str = "";
        if ((s & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsRemoteSessionsHosted)          != 0) str += " [Remote Sessions Hosted]";
        if ((s & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsRemoteSessionsNotHosted)       != 0) str += " [Remote Sessions Not Hosted]";
        if ((s & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsNearShareAuthPolicySameUser)   != 0) str += " [Near Share Auth Policy Same User]";
        if ((s & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsNearShareAuthPolicyPermissive) != 0) str += " [Near Share Auth Policy Permissive]";
        return str + " ";
    }

    private static string HomeKitCategoryStr(ushort c)
    {
        switch (c)
        {
            case 0x0000: return "Unknown";
            case 0x0001: return "Other";
            case 0x0002: return "Bridge";
            case 0x0003: return "Fan";
            case 0x0004: return "Garage Door Opener";
            case 0x0005: return "Lightbulb";
            case 0x0006: return "Door Lock";
            case 0x0007: return "Outlet";
            case 0x0008: return "Switch";
            case 0x0009: return "Thermostat";
            case 0x000A: return "Sensor";
            case 0x000B: return "Security System";
            case 0x000C: return "Door";
            case 0x000D: return "Window";
            case 0x000E: return "Window Covering";
            case 0x000F: return "Programmable Switch";
            case 0x0010: return "Range Extender";
            case 0x0011: return "IP Camera";
            case 0x0012: return "Video Doorbell";
            case 0x0013: return "Air Purifier";
            case 0x0014: return "Heater";
            case 0x0015: return "Air Conditioner";
            case 0x0016: return "Humidifier";
            case 0x0017: return "Dehumidifier";
            case 0x001C: return "Sprinklers";
            case 0x001D: return "Faucets";
            case 0x001E: return "Shower Systems";
            default:     return "Unknown: 0x" + c.ToString("X4");
        }
    }

    private static string NearbyActionStr(byte a)
    {
        switch (a)
        {
            case 0x01: return "Apple TV Setup";
            case 0x04: return "Mobile Backup";
            case 0x05: return "Watch Setup";
            case 0x06: return "Apple TV Pair";
            case 0x07: return "Internet Relay";
            case 0x08: return "WiFi Password";
            case 0x09: return "iOS Setup";
            case 0x0A: return "Repair";
            case 0x0B: return "Speaker Setupd";
            case 0x0C: return "Apple Pay";
            case 0x0D: return "Whole Home Audio Setup";
            case 0x0E: return "Developer Tools Pairing Request";
            case 0x0F: return "Answered Call";
            case 0x10: return "Ended Call";
            case 0x11: return "DD Ping";
            case 0x12: return "DD Pong";
            case 0x13: return "Remote Auto Fill";
            case 0x14: return "Companion Link Proximity";
            case 0x15: return "Remote Management";
            case 0x16: return "Remote Auto Fill Pong";
            case 0x17: return "Remote Display";
            default:   return "Unknown: 0x" + a.ToString("X2");
        }
    }

    private static string NearbyDeviceStr(byte d)
    {
        switch (d)
        {
            case 0x02: return "iPhone";
            case 0x04: return "iPod";
            case 0x06: return "iPad";
            case 0x08: return "Audio accessory (HomePod)";
            case 0x0A: return "Mac";
            case 0x0C: return "AppleTV";
            case 0x0E: return "Watch";
            default:   return "Unknown: 0x" + d.ToString("X2");
        }
    }

    private static string NearbyInfoActionStr(byte a)
    {
        switch (a)
        {
            case 0x00: return "None";
            case 0x01: return "Activity reporting disabled";
            case 0x03: return "Idle user";
            case 0x05: return "Audio playing while screen locked";
            case 0x07: return "Active user (screen on)";
            case 0x09: return "Screen on with video playing";
            case 0x0A: return "Watch on wrist and unlocked";
            case 0x0B: return "Recent user interaction";
            case 0x0D: return "User is driving a vehicle";
            case 0x0E: return "Phone or Facetime Call";
            default:   return "Unknown: 0x" + a.ToString("X2");
        }
    }

    private static string ProximityModelStr(ushort m)
    {
        switch (m)
        {
            case 0x2002: return "AirPods";
            case 0x2003: return "Powerbeats 3";
            case 0x2005: return "BeatsX";
            case 0x2006: return "Beats Solo 3";
            case 0x2007: return "Beats Studio 3";
            case 0x2009: return "Beats Studio 3";
            case 0x200A: return "AirPods Max";
            case 0x200B: return "Powerbeats Pro";
            case 0x200C: return "Beats Solo Pro";
            case 0x200D: return "Powerbeats 4";
            case 0x200E: return "AirPods Pro";
            case 0x200F: return "AirPods 2";
            case 0x2010: return "Beats Flex";
            case 0x2011: return "Beats Studio Buds";
            case 0x2012: return "Beats Fit Pro";
            case 0x2013: return "AirPods 3";
            case 0x2014: return "AirPods Pro 2";
            default:     return "Unknown: 0x" + m.ToString("X4");
        }
    }

    private static string ProximityStatusStr(byte s)
    {
        switch (s)
        {
            case 0x01: return "AirPods: Both out of case, not in ear";
            case 0x02: return "Right in ear, Left in case";
            case 0x03: return "AirPods: Right in ear, Left out of case";
            case 0x0B: return "Both AirPods in ear";
            case 0x11: return "AirPods: Right out of case, Left in case";
            case 0x13: return "AirPods: Right in ear, Left in case";
            case 0x21: return "Both taken out of ears, Pause Audio";
            case 0x22: return "Left in ear, Right in case";
            case 0x23: return "AirPods: Left in ear, Right out of case";
            case 0x2B: return "Both AirPods in ear";
            case 0x31: return "AirPods: Left out of case, Right in case";
            case 0x33: return "AirPods: Left in ear, Right in case";
            case 0x51: return "Case: Left out of case, Right in case";
            case 0x53: return "Case: Left in ear, Right in case";
            case 0x55: return "Case: Both AirPods in case";
            case 0x71: return "Case: Right out of case, Left in case";
            case 0x73: return "Case: Right in ear, Left in case";
            case 0x75: return "Case: Both AirPods in case";
            default:   return "Unknown: 0x" + s.ToString("X2");
        }
    }

    private static string ProximityColorStr(byte c)
    {
        switch (c)
        {
            case 0x00: return "White";
            case 0x01: return "Black";
            case 0x02: return "Red";
            case 0x03: return "Blue";
            case 0x04: return "Pink";
            case 0x05: return "Gray";
            case 0x06: return "Silver";
            case 0x07: return "Gold";
            case 0x08: return "Rose Gold";
            case 0x09: return "Space Gray";
            case 0x0A: return "Dark Blue";
            case 0x0B: return "Light Blue";
            case 0x0C: return "Yellow";
            default:   return "Unknown: 0x" + c.ToString("X2");
        }
    }

    private static string CellTypeStr(ushort c)
    {
        switch (c)
        {
            case 0x0000: return "4G (GSM)";
            case 0x0001: return "1xRTT";
            case 0x0002: return "GPRS";
            case 0x0003: return "EDGE";
            case 0x0004: return "3G (EV-DO)";
            case 0x0005: return "3G";
            case 0x0006: return "4G";
            case 0x0007: return "LTE";
            default:     return c.ToString("X4");
        }
    }
}