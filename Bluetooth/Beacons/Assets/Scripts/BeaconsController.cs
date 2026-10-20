using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclBluetooth;

public class BeaconsController : MonoBehaviour
{
    // ==========================================================
    //  UI references
    // ==========================================================
    [Header("Watcher")]
    public Button       btWatcherStart, btWatcherStop;
    public TMP_Dropdown cbWatcherScanMode, cbScanParametersType;
    public TMP_InputField edWatcherScanInterval, edWatcherScanWindow;
    public Toggle       cbWatcherEnableExtended;

    [Header("Advertiser")]
    public Button       btAdvertiserDefault, btAdvertiserStart, btAdvertiserStop;
    public TMP_InputField edAdvertiserInterval;
    public TMP_Dropdown cbPrimaryPhy, cbSecondaryPhy;
    public Toggle cbAdvertiserBeacon, cbAdvertiserProximityBeacon, cbAdvertiserAltBeacon;
    public Toggle cbAdvertiserEddystoneUid, cbAdvertiserEddystoneUrl, cbAdvertiserManufacturer;
    public Toggle cbAdvertiser16Uuid, cbAdvertiser32Uuid, cbAdvertiser128Uuid;
    public Toggle cbAdvertiser16UuidData, cbAdvertiser32UuidData, cbAdvertiser128UuidData;
    public Toggle cbAdvertiser128SolUuid, cbAdvertiserCustom, cbAdvertiserExtended;
    public Toggle cbAdvertiserAnonymous, cbAdvertiserTxRssi;

    [Header("Log")]
    public Button btClearLog;
    public Toggle cbAutoScroll;
    public ScrollRect lbLogScroll;
    public TextMeshProUGUI lbLogText;

    [Header("List views")]
    public RectTransform lvDevicesContent;
    public RectTransform lvFrameDetailsContent;
    public ScrollRect    lvDevicesScroll;
    public ScrollRect    lvFrameDetailsScroll;

    // ==========================================================
    //  Library objects
    // ==========================================================
    private wclBluetoothManager      BluetoothManager;
    private wclBluetoothLeBeaconWatcher BeaconWatcher;
    private wclBluetoothLeAdvertiser    LeAdvertiser;

    // ==========================================================
    //  UI state
    // ==========================================================
    private readonly List<string>        logLines          = new List<string>();
    private readonly List<RowEntry>      lvDevicesRows     = new List<RowEntry>();
    private readonly List<GameObject>    lvFrameDetailsRows = new List<GameObject>();

    private RowEntry selectedRow;

    private bool _suppressEvents = false;

    // ==========================================================
    //  Frame enum + storage hierarchy
    // ==========================================================
    public enum AdvertisementFrame
    {
        afAppearance, afExtInformation, afInformation, afRaw, afDetails,
        afService16Data, afService32Data, afService128Data,
        afSol16, afSol32, afSol128, afTxPower, afUuid,
        afAltBeacon, afEddystoneTlm, afEddystoneUid, afEddystoneUrl,
        afManufacturerRaw, afMicrosoftCdpBeacon,
        afAppleAirdrop, afAppleAirplaySource, afAppleAirplayTarget, afAppleAirprint,
        afAppleHomeKit, afAppleiBeacon, afAppleNearbyAction, afAppleNearbyInfo,
        afAppleProximityPairing, afAppleUnknown, afAppleFindMy, afAppleHandoff,
        afAppleHeySiri, afAppleMagicSwitch, afAppleTetheringSource,
        afAppleTetheringTarget, afDriAsd, afUnknown
    }

    public abstract class FrameStorage
    {
        public AdvertisementFrame Frame   { get; private set; }
        public long  Address   { get; private set; }
        public long  Timestamp { get; private set; }
        public sbyte Rssi      { get; private set; }

        protected FrameStorage(AdvertisementFrame frame, long address, long timestamp, sbyte rssi)
        {
            Frame = frame; Address = address; Timestamp = timestamp; Rssi = rssi;
        }
    }

    public abstract class GuidFrameStorage : FrameStorage
    {
        public Guid Uuid { get; private set; }
        protected GuidFrameStorage(AdvertisementFrame f, long a, long t, sbyte r, Guid u)
            : base(f, a, t, r) { Uuid = u; }
    }

    public abstract class DataFrameStorage : FrameStorage
    {
        public byte[] Data { get; private set; }
        protected DataFrameStorage(AdvertisementFrame f, long a, long t, sbyte r, byte[] d)
            : base(f, a, t, r) { Data = d; }
    }

    public abstract class GuidDataFrameStorage : DataFrameStorage
    {
        public Guid Uuid { get; private set; }
        protected GuidDataFrameStorage(AdvertisementFrame f, long a, long t, sbyte r, Guid u, byte[] d)
            : base(f, a, t, r, d) { Uuid = u; }
    }

    public sealed class AppearanceFrame : FrameStorage
    {
        public ushort Appearance { get; private set; }
        public AppearanceFrame(long a, long t, sbyte r, ushort v) : base(AdvertisementFrame.afAppearance, a, t, r) { Appearance = v; }
    }

    public sealed class ExtInformationFrame : FrameStorage
    {
        public wclBluetoothAddressType AddressType { get; private set; }
        public wclBluetoothLeExtendedFrameFlag Flags { get; private set; }
        public sbyte TxPower { get; private set; }
        public wclBluetoothLeAdvertisementPhy PrimaryPhy { get; private set; }
        public wclBluetoothLeAdvertisementPhy SecondaryPhy { get; private set; }

        public ExtInformationFrame(long a, long t, sbyte r,
            wclBluetoothAddressType at, wclBluetoothLeExtendedFrameFlag fl,
            sbyte tx, wclBluetoothLeAdvertisementPhy pp, wclBluetoothLeAdvertisementPhy sp)
            : base(AdvertisementFrame.afExtInformation, a, t, r)
        { AddressType = at; Flags = fl; TxPower = tx; PrimaryPhy = pp; SecondaryPhy = sp; }
    }

    public sealed class InformationFrame : FrameStorage
    {
        public wclBluetoothLeAdvertisementFlag Flags { get; private set; }
        public string Name { get; private set; }
        public wclBluetoothLeAdvertisementType PacketType { get; private set; }

        public InformationFrame(long a, long t, sbyte r,
            wclBluetoothLeAdvertisementFlag fl, string n, wclBluetoothLeAdvertisementType pt)
            : base(AdvertisementFrame.afInformation, a, t, r)
        { Flags = fl; Name = n ?? ""; PacketType = pt; }
    }

    public sealed class RawFrame : DataFrameStorage
    {
        public byte DataType { get; private set; }
        public RawFrame(long a, long t, sbyte r, byte[] d, byte dt) : base(AdvertisementFrame.afRaw, a, t, r, d) { DataType = dt; }
    }

    public sealed class DetailsFrame : DataFrameStorage
    {
        public DetailsFrame(long a, long t, sbyte r, byte[] d) : base(AdvertisementFrame.afDetails, a, t, r, d) { }
    }

    public sealed class Service16DataFrame : DataFrameStorage
    {
        public ushort Uuid { get; private set; }
        public Service16DataFrame(long a, long t, sbyte r, byte[] d, ushort u) : base(AdvertisementFrame.afService16Data, a, t, r, d) { Uuid = u; }
    }

    public sealed class Service32DataFrame : DataFrameStorage
    {
        public uint Uuid { get; private set; }
        public Service32DataFrame(long a, long t, sbyte r, byte[] d, uint u) : base(AdvertisementFrame.afService32Data, a, t, r, d) { Uuid = u; }
    }

    public sealed class Service128DataFrame : GuidDataFrameStorage
    {
        public Service128DataFrame(long a, long t, sbyte r, Guid u, byte[] d) : base(AdvertisementFrame.afService128Data, a, t, r, u, d) { }
    }

    public sealed class Sol16Frame : FrameStorage
    {
        public ushort Uuid { get; private set; }
        public Sol16Frame(long a, long t, sbyte r, ushort u) : base(AdvertisementFrame.afSol16, a, t, r) { Uuid = u; }
    }

    public sealed class Sol32Frame : FrameStorage
    {
        public uint Uuid { get; private set; }
        public Sol32Frame(long a, long t, sbyte r, uint u) : base(AdvertisementFrame.afSol32, a, t, r) { Uuid = u; }
    }

    public sealed class Sol128Frame : GuidFrameStorage
    {
        public Sol128Frame(long a, long t, sbyte r, Guid u) : base(AdvertisementFrame.afSol128, a, t, r, u) { }
    }

    public sealed class TxPowerFrame : FrameStorage
    {
        public sbyte TxPower { get; private set; }
        public TxPowerFrame(long a, long t, sbyte r, sbyte tx) : base(AdvertisementFrame.afTxPower, a, t, r) { TxPower = tx; }
    }

    public sealed class UuidFrame : GuidFrameStorage
    {
        public UuidFrame(long a, long t, sbyte r, Guid u) : base(AdvertisementFrame.afUuid, a, t, r, u) { }
    }

    public abstract class BeaconFrame : GuidDataFrameStorage
    {
        public ushort CompanyId { get; private set; }
        public ushort Major { get; private set; }
        public ushort Minor { get; private set; }
        public sbyte TxRssi { get; private set; }
        protected BeaconFrame(AdvertisementFrame f, long a, long t, sbyte r, byte[] d, Guid u,
            ushort cid, ushort mj, ushort mn, sbyte tx)
            : base(f, a, t, r, u, d) { CompanyId = cid; Major = mj; Minor = mn; TxRssi = tx; }
    }

    public sealed class AltBeaconFrame : BeaconFrame
    {
        public byte Reserved { get; private set; }
        public AltBeaconFrame(long a, long t, sbyte r, byte[] d, Guid u,
            ushort cid, ushort mj, ushort mn, sbyte tx, byte rsv)
            : base(AdvertisementFrame.afAltBeacon, a, t, r, d, u, cid, mj, mn, tx) { Reserved = rsv; }
    }

    public sealed class MicrosoftCdpBeaconFrame : FrameStorage
    {
        public bool AddressAsDeviceId { get; private set; }
        public wclBluetoothLeCdpBeaconDeviceType DeviceType { get; private set; }
        public wclBluetoothLeCdpBeaconExtendedDeviceStatus ExtendedDeviceStatus { get; private set; }
        public byte[] Hash { get; private set; }
        public byte[] Salt { get; private set; }
        public wclBluetoothLeCdpBeaconScenarioType ScenarioType { get; private set; }
        public bool ShareNearBy { get; private set; }
        public byte SubVersion { get; private set; }
        public byte Version { get; private set; }

        public MicrosoftCdpBeaconFrame(long a, long t, sbyte r, bool addrAsId,
            wclBluetoothLeCdpBeaconDeviceType dt, wclBluetoothLeCdpBeaconExtendedDeviceStatus eds,
            byte[] hash, byte[] salt, wclBluetoothLeCdpBeaconScenarioType st,
            bool share, byte sv, byte v)
            : base(AdvertisementFrame.afMicrosoftCdpBeacon, a, t, r)
        {
            AddressAsDeviceId = addrAsId; DeviceType = dt; ExtendedDeviceStatus = eds;
            Hash = hash; Salt = salt; ScenarioType = st; ShareNearBy = share;
            SubVersion = sv; Version = v;
        }
    }

    public abstract class AppleFrame : FrameStorage
    {
        protected AppleFrame(AdvertisementFrame f, wclBluetoothLeAdvertisementInfo i)
            : base(f, i.Address, i.Timestamp, i.Rssi) { }
    }

    public sealed class AppleAirdropFrame : AppleFrame
    {
        public wclBluetoothLeAppleAirdropFrameData Data { get; private set; }
        public AppleAirdropFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirdropFrameData d) : base(AdvertisementFrame.afAppleAirdrop, i) { Data = d; }
    }
    public sealed class AppleAirplaySourceFrame : AppleFrame
    {
        public wclBluetoothLeAppleAirplaySourceFrameData Data { get; private set; }
        public AppleAirplaySourceFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirplaySourceFrameData d) : base(AdvertisementFrame.afAppleAirplaySource, i) { Data = d; }
    }
    public sealed class AppleAirplayTargetFrame : AppleFrame
    {
        public wclBluetoothLeAppleAirplayTargetFrameData Data { get; private set; }
        public AppleAirplayTargetFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirplayTargetFrameData d) : base(AdvertisementFrame.afAppleAirplayTarget, i) { Data = d; }
    }
    public sealed class AppleAirprintFrame : AppleFrame
    {
        public wclBluetoothLeAppleAirprintFrameData Data { get; private set; }
        public AppleAirprintFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirprintFrameData d) : base(AdvertisementFrame.afAppleAirprint, i) { Data = d; }
    }
    public sealed class AppleHomeKitFrame : AppleFrame
    {
        public wclBluetoothLeAppleHomeKitFrameData Data { get; private set; }
        public AppleHomeKitFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleHomeKitFrameData d) : base(AdvertisementFrame.afAppleHomeKit, i) { Data = d; }
    }
    public sealed class AppleiBeaconFrame : AppleFrame
    {
        public wclBluetoothLeAppleiBeaconFrameData Data { get; private set; }
        public AppleiBeaconFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleiBeaconFrameData d) : base(AdvertisementFrame.afAppleiBeacon, i) { Data = d; }
    }
    public sealed class AppleNearbyActionFrame : AppleFrame
    {
        public wclBluetoothLeAppleNearbyActionFrameData Data { get; private set; }
        public AppleNearbyActionFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleNearbyActionFrameData d) : base(AdvertisementFrame.afAppleNearbyAction, i) { Data = d; }
    }
    public sealed class AppleNearbyInfoFrame : AppleFrame
    {
        public wclBluetoothLeAppleNearbyInfoFrameData Data { get; private set; }
        public AppleNearbyInfoFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleNearbyInfoFrameData d) : base(AdvertisementFrame.afAppleNearbyInfo, i) { Data = d; }
    }
    public sealed class AppleProximityPairingFrame : AppleFrame
    {
        public wclBluetoothLeAppleProximityPairingFrameData Data { get; private set; }
        public AppleProximityPairingFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleProximityPairingFrameData d) : base(AdvertisementFrame.afAppleProximityPairing, i) { Data = d; }
    }
    public sealed class AppleUnknownFrame : AppleFrame
    {
        public wclBluetoothLeAppleUnknownFrameData Data { get; private set; }
        public AppleUnknownFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleUnknownFrameData d) : base(AdvertisementFrame.afAppleUnknown, i) { Data = d; }
    }
    public sealed class AppleFindMyFrame : AppleFrame
    {
        public wclBluetoothLeAppleFindMyFrameData Data { get; private set; }
        public AppleFindMyFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleFindMyFrameData d) : base(AdvertisementFrame.afAppleFindMy, i) { Data = d; }
    }
    public sealed class AppleHandoffFrame : AppleFrame
    {
        public wclBluetoothLeAppleHandoffFrameData Data { get; private set; }
        public AppleHandoffFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleHandoffFrameData d) : base(AdvertisementFrame.afAppleHandoff, i) { Data = d; }
    }
    public sealed class AppleHeySiriFrame : AppleFrame
    {
        public wclBluetoothLeAppleHeySiriFrameData Data { get; private set; }
        public AppleHeySiriFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleHeySiriFrameData d) : base(AdvertisementFrame.afAppleHeySiri, i) { Data = d; }
    }
    public sealed class AppleMagicSwitchFrame : AppleFrame
    {
        public wclBluetoothLeAppleMagicSwitchFrameData Data { get; private set; }
        public AppleMagicSwitchFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleMagicSwitchFrameData d) : base(AdvertisementFrame.afAppleMagicSwitch, i) { Data = d; }
    }
    public sealed class AppleTetheringSourceFrame : AppleFrame
    {
        public wclBluetoothLeAppleTetheringSourceFrameData Data { get; private set; }
        public AppleTetheringSourceFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleTetheringSourceFrameData d) : base(AdvertisementFrame.afAppleTetheringSource, i) { Data = d; }
    }
    public sealed class AppleTetheringTargetFrame : AppleFrame
    {
        public wclBluetoothLeAppleTetheringTargetFrameData Data { get; private set; }
        public AppleTetheringTargetFrame(wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleTetheringTargetFrameData d) : base(AdvertisementFrame.afAppleTetheringTarget, i) { Data = d; }
    }

    public sealed class EddystoneTlmFrame : DataFrameStorage
    {
        public uint AdvCnt { get; private set; }
        public ushort Batt { get; private set; }
        public uint SecCnt { get; private set; }
        public double Temp { get; private set; }
        public EddystoneTlmFrame(long a, long t, sbyte r, byte[] d, uint ac, ushort bt, uint sc, double tp)
            : base(AdvertisementFrame.afEddystoneTlm, a, t, r, d)
        { AdvCnt = ac; Batt = bt; SecCnt = sc; Temp = tp; }
    }

    public sealed class EddystoneUidFrame : GuidDataFrameStorage
    {
        public sbyte TxRssi { get; private set; }
        public EddystoneUidFrame(long a, long t, sbyte r, byte[] d, Guid u, sbyte tx)
            : base(AdvertisementFrame.afEddystoneUid, a, t, r, u, d) { TxRssi = tx; }
    }

    public sealed class EddystoneUrlFrame : FrameStorage
    {
        public sbyte TxRssi { get; private set; }
        public string Url { get; private set; }
        public EddystoneUrlFrame(long a, long t, sbyte r, sbyte tx, string url)
            : base(AdvertisementFrame.afEddystoneUrl, a, t, r) { TxRssi = tx; Url = url ?? ""; }
    }

    public sealed class ManufacturerFrame : DataFrameStorage
    {
        public ushort CompanyId { get; private set; }
        public ManufacturerFrame(long a, long t, sbyte r, ushort cid, byte[] d)
            : base(AdvertisementFrame.afManufacturerRaw, a, t, r, d) { CompanyId = cid; }
    }

    public sealed class DriAsdFrame : FrameStorage
    {
        public byte[] Raw { get; private set; }
        public DriAsdFrame(long a, long t, sbyte r, byte[] raw) : base(AdvertisementFrame.afDriAsd, a, t, r) { Raw = raw; }
    }

    // ==========================================================
    //  Row entry model
    // ==========================================================
    private class RowEntry
    {
        public bool          IsDevice;
        public string        Address;
        public FrameStorage  Frame;
        public GameObject    GO;
        public Image         Background;
        public TextMeshProUGUI Cell0;
        public TextMeshProUGUI Cell1;
    }

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        // Watcher buttons
        btWatcherStart.onClick.AddListener(OnWatcherStartClick);
        btWatcherStop.onClick.AddListener(OnWatcherStopClick);

        // Advertiser buttons
        btAdvertiserDefault.onClick.AddListener(OnAdvertiserDefaultClick);
        btAdvertiserStart.onClick.AddListener(OnAdvertiserStartClick);
        btAdvertiserStop.onClick.AddListener(OnAdvertiserStopClick);

        // Log
        btClearLog.onClick.AddListener(OnClearLogClick);

        // Scan parameters type change
        cbScanParametersType.onValueChanged.AddListener(idx =>
        {
            if (_suppressEvents) return;
            edWatcherScanInterval.interactable = (idx == 3);
            edWatcherScanWindow.interactable   = edWatcherScanInterval.interactable;
        });

        // Extended advertiser change
        cbAdvertiserExtended.onValueChanged.AddListener(isOn =>
        {
            if (_suppressEvents) return;
            cbAdvertiserAnonymous.interactable = isOn;
            cbAdvertiserTxRssi.interactable    = isOn;
        });
    }

    private void Start()
    {
        BluetoothManager = new wclBluetoothManager();
        BluetoothManager.AfterOpen   += OnBluetoothAfterOpen;
        BluetoothManager.BeforeClose += OnBluetoothBeforeClose;
        BluetoothManager.OnClosed    += OnBluetoothClosed;

        BeaconWatcher = new wclBluetoothLeBeaconWatcher();
        BeaconWatcher.OnStarted += OnWatcherStarted;
        BeaconWatcher.OnStopped += OnWatcherStopped;

        BeaconWatcher.OnAdvertisementAppearanceFrame          += OnAdvAppearanceFrame;
        BeaconWatcher.OnAdvertisementExtFrameInformation      += OnAdvExtFrameInformation;
        BeaconWatcher.OnAdvertisementFrameInformation         += OnAdvFrameInformation;
        BeaconWatcher.OnAdvertisementRawFrame                 += OnAdvRawFrame;
        BeaconWatcher.OnAdvertisementReceived                 += OnAdvReceived;
        BeaconWatcher.OnAdvertisementService16DataFrame       += OnAdvService16DataFrame;
        BeaconWatcher.OnAdvertisementService32DataFrame       += OnAdvService32DataFrame;
        BeaconWatcher.OnAdvertisementService128DataFrame      += OnAdvService128DataFrame;
        BeaconWatcher.OnAdvertisementServiceSol16Frame        += OnAdvSol16Frame;
        BeaconWatcher.OnAdvertisementServiceSol32Frame        += OnAdvSol32Frame;
        BeaconWatcher.OnAdvertisementServiceSol128Frame       += OnAdvSol128Frame;
        BeaconWatcher.OnAdvertisementTxPowerLevelFrame        += OnAdvTxPowerFrame;
        BeaconWatcher.OnAdvertisementUuidFrame                += OnAdvUuidFrame;
        BeaconWatcher.OnAltBeaconFrame                        += OnAltBeaconFrame;
        BeaconWatcher.OnDriAsdMessage                         += OnDriAsdMessage;
        BeaconWatcher.OnEddystoneTlmFrame                     += OnEddystoneTlmFrame;
        BeaconWatcher.OnEddystoneUidFrame                     += OnEddystoneUidFrame;
        BeaconWatcher.OnEddystoneUrlFrame                     += OnEddystoneUrlFrame;
        BeaconWatcher.OnManufacturerRawFrame                  += OnManufacturerRawFrame;
        BeaconWatcher.OnMicrosoftCdpBeaconFrame               += OnMicrosoftCdpBeaconFrame;
        BeaconWatcher.OnAppleiBeaconFrame                     += OnAppleiBeaconFrame;
        BeaconWatcher.OnAppleHomeKitFrame                     += OnAppleHomeKitFrame;
        BeaconWatcher.OnAppleNearbyActionFrame                += OnAppleNearbyActionFrame;
        BeaconWatcher.OnAppleNearbyInfoFrame                  += OnAppleNearbyInfoFrame;
        BeaconWatcher.OnAppleUnknownFrame                     += OnAppleUnknownFrame;
        BeaconWatcher.OnAppleAirplaySourceFrame               += OnAppleAirplaySourceFrame;
        BeaconWatcher.OnAppleAirplayTargetFrame               += OnAppleAirplayTargetFrame;
        BeaconWatcher.OnAppleAirdropFrame                     += OnAppleAirdropFrame;
        BeaconWatcher.OnAppleAirprintFrame                    += OnAppleAirprintFrame;
        BeaconWatcher.OnAppleProximityPairingFrame            += OnAppleProximityPairingFrame;
        BeaconWatcher.OnAppleTetheringSourceFrame             += OnAppleTetheringSourceFrame;
        BeaconWatcher.OnAppleTetheringTargetFrame             += OnAppleTetheringTargetFrame;
        BeaconWatcher.OnAppleMagicSwitchFrame                 += OnAppleMagicSwitchFrame;
        BeaconWatcher.OnAppleHeySiriFrame                     += OnAppleHeySiriFrame;
        BeaconWatcher.OnAppleHandoffFrame                     += OnAppleHandoffFrame;
        BeaconWatcher.OnAppleFindMyFrame                      += OnAppleFindMyFrame;

        LeAdvertiser = new wclBluetoothLeAdvertiser();
        LeAdvertiser.OnStarted += OnAdvertiserStarted;
        LeAdvertiser.OnStopped += OnAdvertiserStopped;

        // Initial state, mirrors fmMainLoad
        AdvertiserDefaults();

        _suppressEvents = true;
        if (BeaconWatcher.ScanningMode == wclBluetoothLeScanningMode.smActive)
            cbWatcherScanMode.value = 0;
        else
            cbWatcherScanMode.value = 1;
        edWatcherScanInterval.text = BeaconWatcher.ScanInterval.ToString();
        edWatcherScanWindow.text   = BeaconWatcher.ScanWindow.ToString();
        cbWatcherEnableExtended.isOn = BeaconWatcher.AllowExtendedAdvertisements;
        cbScanParametersType.value = 0;
        cbPrimaryPhy.value   = 0;
        cbSecondaryPhy.value = 0;
        cbAutoScroll.isOn    = true;
        _suppressEvents = false;

        edWatcherScanInterval.interactable = false;
        edWatcherScanWindow.interactable   = false;
        btAdvertiserStop.interactable = false;
        btWatcherStop.interactable    = false;
    }

    private void OnDestroy()
    {
        try { LeAdvertiser?.Stop();     } catch { }
        try { BeaconWatcher?.Stop();    } catch { }
        try { BluetoothManager?.Close(); } catch { }
    }

    // ==========================================================
    //  Advertiser defaults
    // ==========================================================
    private void AdvertiserDefaults()
    {
        LeAdvertiser.RestoreDefaults();

        _suppressEvents = true;
        edAdvertiserInterval.text = LeAdvertiser.Interval.ToString();

        cbAdvertiserBeacon.isOn           = true;
        cbAdvertiserProximityBeacon.isOn  = false;
        cbAdvertiserAltBeacon.isOn        = false;
        cbAdvertiserEddystoneUid.isOn     = false;
        cbAdvertiserEddystoneUrl.isOn     = false;
        cbAdvertiser128SolUuid.isOn       = false;
        cbAdvertiserManufacturer.isOn     = false;
        cbAdvertiser16Uuid.isOn           = false;
        cbAdvertiser32Uuid.isOn           = false;
        cbAdvertiser128Uuid.isOn          = false;
        cbAdvertiser16UuidData.isOn       = false;
        cbAdvertiser32UuidData.isOn       = false;
        cbAdvertiser128UuidData.isOn      = false;
        cbAdvertiserCustom.isOn           = false;

        cbAdvertiserExtended.isOn  = false;
        cbAdvertiserAnonymous.isOn = false;
        cbAdvertiserTxRssi.isOn    = false;
        _suppressEvents = false;

        cbAdvertiserAnonymous.interactable = false;
        cbAdvertiserTxRssi.interactable    = false;
    }

    // ==========================================================
    //  Enable / disable panels
    // ==========================================================
    private void AdvertiserEnableControls(bool stopped)
    {
        btAdvertiserStart.interactable   = stopped;
        btAdvertiserStop.interactable    = !stopped;
        btAdvertiserDefault.interactable = stopped;
        edAdvertiserInterval.interactable = stopped;

        cbAdvertiserBeacon.interactable          = stopped;
        cbAdvertiserProximityBeacon.interactable = stopped;
        cbAdvertiserAltBeacon.interactable       = stopped;
        cbAdvertiserEddystoneUid.interactable    = stopped;
        cbAdvertiserEddystoneUrl.interactable    = stopped;
        cbAdvertiser128SolUuid.interactable      = stopped;
        cbAdvertiserManufacturer.interactable    = stopped;
        cbAdvertiser16Uuid.interactable          = stopped;
        cbAdvertiser32Uuid.interactable          = stopped;
        cbAdvertiser128Uuid.interactable         = stopped;
        cbAdvertiser16UuidData.interactable      = stopped;
        cbAdvertiser32UuidData.interactable      = stopped;
        cbAdvertiser128UuidData.interactable     = stopped;
        cbAdvertiserCustom.interactable          = stopped;
        cbAdvertiserExtended.interactable        = stopped;

        cbAdvertiserAnonymous.interactable = (stopped && cbAdvertiserExtended.isOn);
        cbAdvertiserTxRssi.interactable    = (stopped && cbAdvertiserExtended.isOn);
    }

    private void WatcherEnableControls(bool stopped)
    {
        cbWatcherScanMode.interactable        = stopped;
        cbWatcherEnableExtended.interactable  = stopped;
        cbScanParametersType.interactable     = stopped;

        bool custom = (cbScanParametersType.value == 3);
        edWatcherScanInterval.interactable = stopped && custom;
        edWatcherScanWindow.interactable   = stopped && custom;

        btWatcherStart.interactable = stopped;
        btWatcherStop.interactable  = !stopped;
    }

    // ==========================================================
    //  Logging
    // ==========================================================
    private void Trace(string msg)
    {
        logLines.Add(msg);
        RefreshLog();
        if (cbAutoScroll.isOn)
        {
            Canvas.ForceUpdateCanvases();
            if (lbLogScroll != null) lbLogScroll.verticalNormalizedPosition = 0f;
        }
    }

    private void Trace(string msg, int res)
    {
        Trace(msg + " failed: 0x" + res.ToString("X8"));
    }

    private void RefreshLog()
    {
        if (lbLogText != null)
            lbLogText.text = string.Join("\n", logLines);
    }

    private void OnClearLogClick()
    {
        logLines.Clear();
        RefreshLog();
    }

    // ==========================================================
    //  Radio
    // ==========================================================
    private wclBluetoothRadio GetRadio()
    {
        int res = wclErrors.WCL_E_SUCCESS;
        if (!BluetoothManager.Active)
            res = BluetoothManager.Open();

        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Bluetooth Manager open", res);
            return null;
        }

        res = BluetoothManager.GetLeRadio(out wclBluetoothRadio radio);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get working radio", res);
            return null;
        }

        Trace("Use " + radio.ApiName + " radio");
        return radio;
    }

    // ==========================================================
    //  Watcher / advertiser button handlers
    // ==========================================================
    private void OnWatcherStartClick()
    {
        var radio = GetRadio();
        if (radio == null) return;

        BeaconWatcher.AllowExtendedAdvertisements = cbWatcherEnableExtended.isOn;

        BeaconWatcher.ScanningMode = (cbWatcherScanMode.value == 0)
            ? wclBluetoothLeScanningMode.smActive
            : wclBluetoothLeScanningMode.smPassive;

        switch (cbScanParametersType.value)
        {
            case 0: BeaconWatcher.ScanParametersType = wclBluetoothLeScanParametersType.ptDefault; break;
            case 1: BeaconWatcher.ScanParametersType = wclBluetoothLeScanParametersType.ptCoexistenceOptimized; break;
            case 2: BeaconWatcher.ScanParametersType = wclBluetoothLeScanParametersType.ptLowLatency; break;
            default: BeaconWatcher.ScanParametersType = wclBluetoothLeScanParametersType.ptCustom; break;
        }

        ushort scanInterval = ParseUShort(edWatcherScanInterval.text);
        ushort scanWindow   = ParseUShort(edWatcherScanWindow.text);
        BeaconWatcher.ScanInterval = scanInterval;
        BeaconWatcher.ScanWindow   = scanWindow;

        int res = BeaconWatcher.Start(radio);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Start Watched", res);
    }

    private void OnWatcherStopClick()
    {
        int res = BeaconWatcher.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Beacon Watcher stop", res);
    }

    private void OnAdvertiserDefaultClick()
    {
        AdvertiserDefaults();
    }

    private void OnAdvertiserStopClick()
    {
        int res = LeAdvertiser.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Advertiser stop:", res);
    }

    private void OnAdvertiserStartClick()
    {
        Guid BEACON_UUID = new Guid("{09039835-4A80-443B-87AA-DC565D09EA61}");

        var radio = GetRadio();
        if (radio == null) return;

        LeAdvertiser.Clear();
        wclBluetoothLeAdvertisement adv;
        byte[] data;

        if (cbAdvertiserBeacon.isOn)
        {
            adv = new wclBluetoothLeiBeaconAdvertisement(-5, 0x0101, 0x0202, BEACON_UUID);
            AddAdvertisement(adv);
        }
        if (cbAdvertiserProximityBeacon.isOn)
        {
            adv = new wclBluetoothLeProximityBeaconAdvertisement(-5, 0x0101, 0x0202, BEACON_UUID, 0xFFFE);
            AddAdvertisement(adv);
        }
        if (cbAdvertiserAltBeacon.isOn)
        {
            adv = new wclBluetoothLeAltBeaconAdvertisement(-5, 0x0101, 0x0202, BEACON_UUID, 0xFFFE, 0x11);
            AddAdvertisement(adv);
        }
        if (cbAdvertiserEddystoneUid.isOn)
        {
            adv = new wclBluetoothLeEddystoneUidBeaconAdvertisement(-5, BEACON_UUID);
            AddAdvertisement(adv);
        }
        if (cbAdvertiserEddystoneUrl.isOn)
        {
            adv = new wclBluetoothLeEddystoneUrlBeaconAdvertisement(-5, "https://www.btframework.com");
            AddAdvertisement(adv);
        }
        if (cbAdvertiser128SolUuid.isOn)
        {
            data = new byte[16] { 0xD0, 0x00, 0x2D, 0x12, 0x1E, 0x4B, 0x0F, 0xA4,
                                  0x99, 0x4E, 0xCE, 0xB5, 0x31, 0xF4, 0x05, 0x79 };
            adv = new wclBluetoothLeCustomAdvertisement(wclUUIDs.LE_GAP_AD_TYPE_SERVICE_SOL_UUIDS_128, data);
            AddAdvertisement(adv);
        }
        if (cbAdvertiserManufacturer.isOn)
        {
            data = new byte[] { 0x12, 0x34 };
            adv = new wclBluetoothLeManufacturerAdvertisement(0x010E, data);
            AddAdvertisement(adv);
        }
        if (cbAdvertiser16Uuid.isOn)
        {
            adv = new wclBluetoothLe16ServiceAdvertisement(0x9835);
            AddAdvertisement(adv);
        }
        if (cbAdvertiser32Uuid.isOn)
        {
            adv = new wclBluetoothLe32ServiceAdvertisement(0x09039835);
            AddAdvertisement(adv);
        }
        if (cbAdvertiser128Uuid.isOn)
        {
            adv = new wclBluetoothLe128ServiceAdvertisement(BEACON_UUID);
            AddAdvertisement(adv);
        }
        if (cbAdvertiser16UuidData.isOn)
        {
            data = new byte[] { 0x12, 0x34 };
            adv = new wclBluetoothLe16ServiceDataAdvertisement(0x9835, data);
            AddAdvertisement(adv);
        }
        if (cbAdvertiser32UuidData.isOn)
        {
            data = new byte[] { 0x12, 0x34 };
            adv = new wclBluetoothLe32ServiceDataAdvertisement(0x09039835, data);
            AddAdvertisement(adv);
        }
        if (cbAdvertiser128UuidData.isOn)
        {
            data = new byte[] { 0x12, 0x34 };
            adv = new wclBluetoothLe128ServiceDataAdvertisement(BEACON_UUID, data);
            AddAdvertisement(adv);
        }
        if (cbAdvertiserCustom.isOn)
        {
            data = new byte[20];
            data[0] = 0x09;
            data[1] = wclUUIDs.LE_GAP_AD_TYPE_SERVICE_DATA_16;
            data[2] = 0x12; data[3] = 0x34;
            data[4] = 0xFF; data[5] = 0xFE; data[6] = 0xFD; data[7] = 0xFC;
            data[8] = 0xFB; data[9] = 0xFA;
            data[10] = 0x09;
            data[11] = wclUUIDs.LE_GAP_AD_TYPE_MANUFACTURER;
            data[12] = 0x10; data[13] = 0x12;
            data[14] = 0x01; data[15] = 0x02; data[16] = 0x03;
            data[17] = 0x04; data[18] = 0x05; data[19] = 0x06;
            adv = new wclBluetoothLeRawAdvertisement(data);
            AddAdvertisement(adv);
        }

        if (LeAdvertiser.Count == 0)
        {
            Trace("Select at least one advertisement type.");
            return;
        }

        LeAdvertiser.Interval = ParseUShort(edAdvertiserInterval.text);

        LeAdvertiser.UseExtendedAdvertisement = cbAdvertiserExtended.isOn;
        LeAdvertiser.Anonymous                = cbAdvertiserAnonymous.isOn;
        LeAdvertiser.IncludeTxRssi            = cbAdvertiserTxRssi.isOn;
        LeAdvertiser.PreferredTxRssi          = 10;

        LeAdvertiser.PrimaryPhy   = PhyFromIndex(cbPrimaryPhy.value);
        LeAdvertiser.SecondaryPhy = PhyFromIndex(cbSecondaryPhy.value);

        int res = LeAdvertiser.Start(radio);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Start advertiser", res);
    }

    private static wclBluetoothLeAdvertisementPhy PhyFromIndex(int idx)
    {
        switch (idx)
        {
            case 1: return wclBluetoothLeAdvertisementPhy.phUncoded1M;
            case 2: return wclBluetoothLeAdvertisementPhy.phUncoded2M;
            case 3: return wclBluetoothLeAdvertisementPhy.phCoded;
            default: return wclBluetoothLeAdvertisementPhy.phUnspecified;
        }
    }

    private static ushort ParseUShort(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        ushort v;
        ushort.TryParse(s, out v);
        return v;
    }

    private void AddAdvertisement(wclBluetoothLeAdvertisement adv)
    {
        int res = LeAdvertiser.Add(adv);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Add advertisement", res);
    }

    // ==========================================================
    //  Advertiser / bluetooth / watcher lifecycle events
    // ==========================================================
    private void OnAdvertiserStarted(object sender, EventArgs e)
    {
        Trace("LE Advertiser started");
        AdvertiserEnableControls(false);
    }

    private void OnAdvertiserStopped(object sender, EventArgs e)
    {
        Trace("LE Advertiser stopped");
        AdvertiserEnableControls(true);
    }

    private void OnWatcherStarted(object sender, EventArgs e)
    {
        Trace("Beacon Watcher started");
        WatcherEnableControls(false);
    }

    private void OnWatcherStopped(object sender, EventArgs e)
    {
        Trace("Beacon Watcher stopped");
        WatcherEnableControls(true);
        ClearFrameDetails();
        ClearDevices();
    }

    private void OnBluetoothAfterOpen(object sender, EventArgs e)   => Trace("Bluetooth Manager opened");
    private void OnBluetoothBeforeClose(object sender, EventArgs e)
    {
        Trace("Bluetooth Manager is closing");
        try { LeAdvertiser.Stop();     } catch { }
        try { BeaconWatcher.Stop();    } catch { }
    }
    private void OnBluetoothClosed(object sender, EventArgs e)      => Trace("Bluetooth Manager closed");

    // ==========================================================
    //  Frame event handlers (mirror main.cs)
    // ==========================================================
    private void OnAdvAppearanceFrame(object s, long a, long t, sbyte r, ushort appearance)
        => AddFrame(new AppearanceFrame(a, t, r, appearance));

    private void OnAdvExtFrameInformation(object s, long a, long t, sbyte r,
        wclBluetoothAddressType at, sbyte tx, wclBluetoothLeExtendedFrameFlag fl,
        wclBluetoothLeAdvertisementPhy pp, wclBluetoothLeAdvertisementPhy sp)
        => AddFrame(new ExtInformationFrame(a, t, r, at, fl, tx, pp, sp));

    private void OnAdvFrameInformation(object s, long a, long t, sbyte r,
        string n, wclBluetoothLeAdvertisementType pt, wclBluetoothLeAdvertisementFlag fl)
        => AddFrame(new InformationFrame(a, t, r, fl, n, pt));

    private void OnAdvRawFrame(object s, long a, long t, sbyte r, byte dt, byte[] d)
        => AddFrame(new RawFrame(a, t, r, d, dt));

    private void OnAdvReceived(object s, long a, long t, sbyte r, byte[] d)
        => AddFrame(new DetailsFrame(a, t, r, d));

    private void OnAdvService16DataFrame(object s, long a, long t, sbyte r, ushort u, byte[] d)
        => AddFrame(new Service16DataFrame(a, t, r, d, u));

    private void OnAdvService32DataFrame(object s, long a, long t, sbyte r, uint u, byte[] d)
        => AddFrame(new Service32DataFrame(a, t, r, d, u));

    private void OnAdvService128DataFrame(object s, long a, long t, sbyte r, Guid u, byte[] d)
        => AddFrame(new Service128DataFrame(a, t, r, u, d));

    private void OnAdvSol16Frame(object s, long a, long t, sbyte r, ushort u)
        => AddFrame(new Sol16Frame(a, t, r, u));

    private void OnAdvSol32Frame(object s, long a, long t, sbyte r, uint u)
        => AddFrame(new Sol32Frame(a, t, r, u));

    private void OnAdvSol128Frame(object s, long a, long t, sbyte r, Guid u)
        => AddFrame(new Sol128Frame(a, t, r, u));

    private void OnAdvTxPowerFrame(object s, long a, long t, sbyte r, sbyte tx)
        => AddFrame(new TxPowerFrame(a, t, r, tx));

    private void OnAdvUuidFrame(object s, long a, long t, sbyte r, Guid u)
        => AddFrame(new UuidFrame(a, t, r, u));

    private void OnAltBeaconFrame(object s, long a, long t, sbyte r, ushort cid,
        ushort mj, ushort mn, Guid u, sbyte tx, byte rsv, byte[] d)
        => AddFrame(new AltBeaconFrame(a, t, r, d, u, cid, mj, mn, tx, rsv));

    private void OnDriAsdMessage(object s, long a, long t, sbyte r, byte[] raw)
        => AddFrame(new DriAsdFrame(a, t, r, raw));

    private void OnEddystoneTlmFrame(object s, long a, long t, sbyte r,
        uint ac, ushort bt, uint sc, double tp, byte[] d)
        => AddFrame(new EddystoneTlmFrame(a, t, r, d, ac, bt, sc, tp));

    private void OnEddystoneUidFrame(object s, long a, long t, sbyte r, sbyte tx, Guid u, byte[] d)
        => AddFrame(new EddystoneUidFrame(a, t, r, d, u, tx));

    private void OnEddystoneUrlFrame(object s, long a, long t, sbyte r, sbyte tx, string url)
        => AddFrame(new EddystoneUrlFrame(a, t, r, tx, url));

    private void OnManufacturerRawFrame(object s, long a, long t, sbyte r, ushort cid, byte[] d)
        => AddFrame(new ManufacturerFrame(a, t, r, cid, d));

    private void OnMicrosoftCdpBeaconFrame(object s, long a, long t, sbyte r,
        wclBluetoothLeCdpBeaconScenarioType st, byte v, wclBluetoothLeCdpBeaconDeviceType dt,
        byte sv, bool share, bool addrAsId, wclBluetoothLeCdpBeaconExtendedDeviceStatus eds,
        byte[] salt, byte[] hash)
        => AddFrame(new MicrosoftCdpBeaconFrame(a, t, r, addrAsId, dt, eds, hash, salt, st, share, sv, v));

    private void OnAppleiBeaconFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleiBeaconFrameData d)
        => AddFrame(new AppleiBeaconFrame(i, d));
    private void OnAppleHomeKitFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleHomeKitFrameData d)
        => AddFrame(new AppleHomeKitFrame(i, d));
    private void OnAppleNearbyActionFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleNearbyActionFrameData d)
        => AddFrame(new AppleNearbyActionFrame(i, d));
    private void OnAppleNearbyInfoFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleNearbyInfoFrameData d)
        => AddFrame(new AppleNearbyInfoFrame(i, d));
    private void OnAppleUnknownFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleUnknownFrameData d)
        => AddFrame(new AppleUnknownFrame(i, d));
    private void OnAppleAirplaySourceFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirplaySourceFrameData d)
        => AddFrame(new AppleAirplaySourceFrame(i, d));
    private void OnAppleAirplayTargetFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirplayTargetFrameData d)
        => AddFrame(new AppleAirplayTargetFrame(i, d));
    private void OnAppleAirdropFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirdropFrameData d)
        => AddFrame(new AppleAirdropFrame(i, d));
    private void OnAppleAirprintFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleAirprintFrameData d)
        => AddFrame(new AppleAirprintFrame(i, d));
    private void OnAppleProximityPairingFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleProximityPairingFrameData d)
        => AddFrame(new AppleProximityPairingFrame(i, d));
    private void OnAppleTetheringSourceFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleTetheringSourceFrameData d)
        => AddFrame(new AppleTetheringSourceFrame(i, d));
    private void OnAppleTetheringTargetFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleTetheringTargetFrameData d)
        => AddFrame(new AppleTetheringTargetFrame(i, d));
    private void OnAppleMagicSwitchFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleMagicSwitchFrameData d)
        => AddFrame(new AppleMagicSwitchFrame(i, d));
    private void OnAppleHeySiriFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleHeySiriFrameData d)
        => AddFrame(new AppleHeySiriFrame(i, d));
    private void OnAppleHandoffFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleHandoffFrameData d)
        => AddFrame(new AppleHandoffFrame(i, d));
    private void OnAppleFindMyFrame(object s, wclBluetoothLeAdvertisementInfo i, wclBluetoothLeAppleFindMyFrameData d)
        => AddFrame(new AppleFindMyFrame(i, d));

    // ==========================================================
    //  lvDevices: row creation and update
    // ==========================================================
    private void ClearDevices()
    {
        foreach (var r in lvDevicesRows)
            if (r.GO != null) Destroy(r.GO);
        lvDevicesRows.Clear();
        selectedRow = null;
    }

    private RowEntry FindDeviceRow(string addr)
    {
        foreach (var r in lvDevicesRows)
            if (r.IsDevice && r.Address == addr) return r;
        return null;
    }

    private RowEntry CreateRowVisual(bool isDevice)
    {
        var go = new GameObject(isDevice ? "Device" : "Frame",
            typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(lvDevicesContent, false);

        var img = go.GetComponent<Image>();
        img.color = isDevice ? new Color(0.94f, 0.94f, 0.94f) : Color.white;

        var hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 18;
        le.minHeight = 18;

        var cell0 = CreateListCell(go.transform, 100);
        var cell1 = CreateListCell(go.transform, 100);

        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = Color.white;
        c.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        c.selectedColor    = Color.white;
        btn.colors = c;

        var entry = new RowEntry
        {
            IsDevice = isDevice,
            GO = go,
            Background = img,
            Cell0 = cell0,
            Cell1 = cell1
        };
        btn.onClick.AddListener(() => SelectRow(entry));
        return entry;
    }

    private static TextMeshProUGUI CreateListCell(Transform parent, float width)
    {
        var go = new GameObject("Cell", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.margin = new Vector4(4, 0, 2, 0);
        tmp.raycastTarget = false;
        return tmp;
    }

    private RowEntry AddDevice(long address)
    {
        string addrStr = address.ToString("X12");
        var existing = FindDeviceRow(addrStr);
        if (existing != null) return existing;

        var row = CreateRowVisual(true);
        row.Address = addrStr;
        row.Cell0.text = addrStr;
        row.Cell1.text = "";
        lvDevicesRows.Add(row);
        return row;
    }

    private void AddFrame(FrameStorage frame)
    {
        var deviceRow = AddDevice(frame.Address);
        int deviceIdx = lvDevicesRows.IndexOf(deviceRow);

        // Search for an existing frame row of same type after this device
        RowEntry frameRow = null;
        for (int i = deviceIdx + 1; i < lvDevicesRows.Count; i++)
        {
            if (lvDevicesRows[i].IsDevice) break;
            if (lvDevicesRows[i].Frame.Frame == frame.Frame)
            {
                frameRow = lvDevicesRows[i];
                break;
            }
        }

        if (frameRow == null)
        {
            frameRow = CreateRowVisual(false);
            frameRow.Cell0.text = "";
            frameRow.Cell1.text = FrameTypeToString(frame.Frame);
            lvDevicesRows.Insert(deviceIdx + 1, frameRow);

            int deviceSibling = deviceRow.GO.transform.GetSiblingIndex();
            frameRow.GO.transform.SetSiblingIndex(deviceSibling + 1);
        }

        frameRow.Frame = frame;

        // Update device friendly name on Information frame
        if (frame.Frame == AdvertisementFrame.afInformation)
        {
            var info = (InformationFrame)frame;
            if (!string.IsNullOrEmpty(info.Name))
                deviceRow.Cell1.text = info.Name;
        }

        if (selectedRow == frameRow)
            ShowFrameData(frame);
    }

    private void SelectRow(RowEntry entry)
    {
        if (selectedRow != null && selectedRow.Background != null)
            selectedRow.Background.color = selectedRow.IsDevice
                ? new Color(0.94f, 0.94f, 0.94f)
                : Color.white;

        selectedRow = entry;
        if (selectedRow != null && selectedRow.Background != null)
            selectedRow.Background.color = new Color(0.7f, 0.85f, 1f);

        if (entry.IsDevice)
            ClearFrameDetails();
        else
            ShowFrameData(entry.Frame);
    }

    private static string FrameTypeToString(AdvertisementFrame frame)
    {
        switch (frame)
        {
            case AdvertisementFrame.afAppearance:            return "Appearance";
            case AdvertisementFrame.afExtInformation:        return "Ext Information";
            case AdvertisementFrame.afInformation:           return "Information";
            case AdvertisementFrame.afRaw:                   return "Raw";
            case AdvertisementFrame.afDetails:               return "Details";
            case AdvertisementFrame.afService16Data:         return "Service 16";
            case AdvertisementFrame.afService32Data:         return "Service 32";
            case AdvertisementFrame.afService128Data:        return "Service 128";
            case AdvertisementFrame.afSol16:                 return "16 SOL";
            case AdvertisementFrame.afSol32:                 return "32 SOL";
            case AdvertisementFrame.afSol128:                return "128 SOL";
            case AdvertisementFrame.afTxPower:               return "Tx Power";
            case AdvertisementFrame.afUuid:                  return "UUID";
            case AdvertisementFrame.afAltBeacon:             return "Alt Beacon";
            case AdvertisementFrame.afEddystoneTlm:          return "Eddystone TLM";
            case AdvertisementFrame.afEddystoneUid:          return "Eddystone UID";
            case AdvertisementFrame.afEddystoneUrl:          return "Eddystone URL";
            case AdvertisementFrame.afManufacturerRaw:      return "Manufacturer";
            case AdvertisementFrame.afMicrosoftCdpBeacon:    return "Microsoft CDP";
            case AdvertisementFrame.afAppleAirdrop:          return "Apple AirDrop";
            case AdvertisementFrame.afAppleAirplaySource:    return "Apple Airplay Source";
            case AdvertisementFrame.afAppleAirplayTarget:    return "Apple Airplay Target";
            case AdvertisementFrame.afAppleAirprint:         return "Apple AirPrint";
            case AdvertisementFrame.afAppleHomeKit:          return "Apple Home Kit";
            case AdvertisementFrame.afAppleiBeacon:          return "Apple iBeacon";
            case AdvertisementFrame.afAppleNearbyAction:     return "Apple Nearby Action";
            case AdvertisementFrame.afAppleNearbyInfo:       return "Apple Nearby Info";
            case AdvertisementFrame.afAppleProximityPairing: return "Apple Proximity Pairing";
            case AdvertisementFrame.afAppleUnknown:          return "Apple Unknown";
            case AdvertisementFrame.afAppleFindMy:           return "Apple Find My";
            case AdvertisementFrame.afAppleHandoff:          return "Apple Handoff";
            case AdvertisementFrame.afAppleHeySiri:          return "Apple Hey Siri";
            case AdvertisementFrame.afAppleMagicSwitch:      return "Apple Magic Switch";
            case AdvertisementFrame.afAppleTetheringSource:  return "Apple Tethering Source";
            case AdvertisementFrame.afAppleTetheringTarget:  return "Apple Tethering Target";
            case AdvertisementFrame.afDriAsd:                return "DRI ASD";
            default:                                         return "Unknown";
        }
    }

    // ==========================================================
    //  lvFrameDetails
    // ==========================================================
    private void ClearFrameDetails()
    {
        foreach (var go in lvFrameDetailsRows)
            if (go != null) Destroy(go);
        lvFrameDetailsRows.Clear();
    }

    private void ShowData(string name, string value)
    {
        var rowGO = new GameObject("Row", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(lvFrameDetailsContent, false);
        rowGO.GetComponent<Image>().color = Color.white;

        var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        var le = rowGO.GetComponent<LayoutElement>();
        le.preferredHeight = 18;
        le.minHeight = 18;

        var cell0 = CreateListCell(rowGO.transform, 150);
        cell0.text = name ?? "";
        var cell1 = CreateListCell(rowGO.transform, 350);
        cell1.text = value ?? "";

        lvFrameDetailsRows.Add(rowGO);
    }

    private static string ConvertTime(long timestamp)
    {
        try { return DateTime.FromFileTime(timestamp).ToString(); }
        catch { return timestamp.ToString(); }
    }

    private void ShowFrameBaseData(FrameStorage frame)
    {
        ShowData("Address",   frame.Address.ToString("X12"));
        ShowData("Timestamp", ConvertTime(frame.Timestamp));
        ShowData("RSSI",      frame.Rssi.ToString());
    }

    private void ShowFrameRawData(byte[] data)
    {
        string first = "Data";
        if (data != null && data.Length > 0)
        {
            string str = "";
            byte c = 0;
            for (int i = 0; i < data.Length; i++)
            {
                str += data[i].ToString("X2") + " ";
                if (c == 15)
                {
                    ShowData(first, str);
                    first = "";
                    str = "";
                    c = 0;
                }
                else c++;
            }
            if (str != "") ShowData(first, str);
        }
    }

    private void ShowDriFrameRawData(byte[] raw)
    {
        if (raw != null && raw.Length > 0) ShowFrameRawData(raw);
    }

    // ==========================================================
    //  Frame display dispatcher
    // ==========================================================
    private void ShowFrameData(FrameStorage frame)
    {
        ClearFrameDetails();
        switch (frame.Frame)
        {
            case AdvertisementFrame.afAppearance:            ShowAppearanceFrame((AppearanceFrame)frame); break;
            case AdvertisementFrame.afExtInformation:        ShowExtInformationFrame((ExtInformationFrame)frame); break;
            case AdvertisementFrame.afInformation:           ShowInformationFrame((InformationFrame)frame); break;
            case AdvertisementFrame.afRaw:                   ShowRawFrame((RawFrame)frame); break;
            case AdvertisementFrame.afDetails:               ShowDetailsFrame((DetailsFrame)frame); break;
            case AdvertisementFrame.afService16Data:         ShowService16DataFrame((Service16DataFrame)frame); break;
            case AdvertisementFrame.afService32Data:         ShowService32DataFrame((Service32DataFrame)frame); break;
            case AdvertisementFrame.afService128Data:        ShowService128DataFrame((Service128DataFrame)frame); break;
            case AdvertisementFrame.afSol16:                 ShowSol16Frame((Sol16Frame)frame); break;
            case AdvertisementFrame.afSol32:                 ShowSol32Frame((Sol32Frame)frame); break;
            case AdvertisementFrame.afSol128:                ShowSol128Frame((Sol128Frame)frame); break;
            case AdvertisementFrame.afTxPower:               ShowTxPowerFrame((TxPowerFrame)frame); break;
            case AdvertisementFrame.afUuid:                  ShowUuidFrame((UuidFrame)frame); break;
            case AdvertisementFrame.afAltBeacon:             ShowAltBeaconFrame((AltBeaconFrame)frame); break;
            case AdvertisementFrame.afEddystoneTlm:          ShowEddystoneTlmFrame((EddystoneTlmFrame)frame); break;
            case AdvertisementFrame.afEddystoneUid:          ShowEddystoneUidFrame((EddystoneUidFrame)frame); break;
            case AdvertisementFrame.afEddystoneUrl:          ShowEddystoneUrlFrame((EddystoneUrlFrame)frame); break;
            case AdvertisementFrame.afManufacturerRaw:       ShowManufacturerFrame((ManufacturerFrame)frame); break;
            case AdvertisementFrame.afMicrosoftCdpBeacon:    ShowMicrosoftCdpBeaconFrame((MicrosoftCdpBeaconFrame)frame); break;
            case AdvertisementFrame.afAppleAirdrop:          ShowAppleAirdropFrame((AppleAirdropFrame)frame); break;
            case AdvertisementFrame.afAppleAirplaySource:    ShowAppleAirplaySourceFrame((AppleAirplaySourceFrame)frame); break;
            case AdvertisementFrame.afAppleAirplayTarget:    ShowAppleAirplayTargetFrame((AppleAirplayTargetFrame)frame); break;
            case AdvertisementFrame.afAppleAirprint:         ShowAppleAirprintFrame((AppleAirprintFrame)frame); break;
            case AdvertisementFrame.afAppleHomeKit:          ShowAppleHomeKitFrame((AppleHomeKitFrame)frame); break;
            case AdvertisementFrame.afAppleiBeacon:          ShowAppleiBeaconFrame((AppleiBeaconFrame)frame); break;
            case AdvertisementFrame.afAppleNearbyAction:     ShowAppleNearbyActionFrame((AppleNearbyActionFrame)frame); break;
            case AdvertisementFrame.afAppleNearbyInfo:       ShowAppleNearbyInfoFrame((AppleNearbyInfoFrame)frame); break;
            case AdvertisementFrame.afAppleProximityPairing: ShowAppleProximityPairingFrame((AppleProximityPairingFrame)frame); break;
            case AdvertisementFrame.afAppleUnknown:          ShowAppleUnknownFrame((AppleUnknownFrame)frame); break;
            case AdvertisementFrame.afAppleFindMy:           ShowAppleFindMyFrame((AppleFindMyFrame)frame); break;
            case AdvertisementFrame.afAppleHandoff:          ShowAppleHandoffFrame((AppleHandoffFrame)frame); break;
            case AdvertisementFrame.afAppleHeySiri:          ShowAppleHeySiriFrame((AppleHeySiriFrame)frame); break;
            case AdvertisementFrame.afAppleMagicSwitch:      ShowAppleMagicSwitchFrame((AppleMagicSwitchFrame)frame); break;
            case AdvertisementFrame.afAppleTetheringSource:  ShowAppleTetheringSourceFrame((AppleTetheringSourceFrame)frame); break;
            case AdvertisementFrame.afAppleTetheringTarget:  ShowAppleTetheringTargetFrame((AppleTetheringTargetFrame)frame); break;
            case AdvertisementFrame.afDriAsd:                ShowDriAsdFrame((DriAsdFrame)frame); break;
        }
    }

    // ==========================================================
    //  Per-frame display methods (ported 1:1 from main.cs)
    // ==========================================================
    private void ShowAppearanceFrame(AppearanceFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Appearance", f.Appearance.ToString("X4"));
    }

    private void ShowExtInformationFrame(ExtInformationFrame f)
    {
        ShowFrameBaseData(f);

        string str;
        switch (f.AddressType)
        {
            case wclBluetoothAddressType.atClassic:     str = "Classic";     break;
            case wclBluetoothAddressType.atPublic:      str = "Public";      break;
            case wclBluetoothAddressType.atRandom:      str = "Random";      break;
            case wclBluetoothAddressType.atUnspecified: str = "Unpecified";  break;
            default:                                    str = "Unknonw";     break;
        }
        ShowData("Address type", str);

        str = "";
        if ((f.Flags & wclBluetoothLeExtendedFrameFlag.efAnonymous)    != 0) str += " [Anonymous]";
        if ((f.Flags & wclBluetoothLeExtendedFrameFlag.efConnectable)  != 0) str += " [Connectable]";
        if ((f.Flags & wclBluetoothLeExtendedFrameFlag.efDirected)     != 0) str += " [Directed]";
        if ((f.Flags & wclBluetoothLeExtendedFrameFlag.efScannable)    != 0) str += " [Scannable]";
        if ((f.Flags & wclBluetoothLeExtendedFrameFlag.efScanResponse) != 0) str += " [Scan Response]";
        str += " ";
        ShowData("Flags", str);

        ShowData("TX Power", f.TxPower.ToString());

        ShowData("Primary PHY",   PhyToStr(f.PrimaryPhy));
        ShowData("Secondary PHY", PhyToStr(f.SecondaryPhy));
    }

    private static string PhyToStr(wclBluetoothLeAdvertisementPhy phy)
    {
        switch (phy)
        {
            case wclBluetoothLeAdvertisementPhy.phUncoded1M: return "Uncoded 1M";
            case wclBluetoothLeAdvertisementPhy.phUncoded2M: return "Uncoded 2M";
            case wclBluetoothLeAdvertisementPhy.phCoded:     return "Coded";
            default:                                         return "Unspecified";
        }
    }

    private void ShowInformationFrame(InformationFrame f)
    {
        ShowFrameBaseData(f);

        string str = "";
        if ((f.Flags & wclBluetoothLeAdvertisementFlag.afLimitedDiscoverableMode)  != 0) str += " [Limited Discoverable Mode]";
        if ((f.Flags & wclBluetoothLeAdvertisementFlag.afGeneralDiscoverableMode)  != 0) str += " [General Discoverable Mode]";
        if ((f.Flags & wclBluetoothLeAdvertisementFlag.afClassicNotSupported)      != 0) str += " [Classic Not Supported]";
        if ((f.Flags & wclBluetoothLeAdvertisementFlag.afDualModeControllerCapable)!= 0) str += " [Dual Mode Controller Capable]";
        if ((f.Flags & wclBluetoothLeAdvertisementFlag.afDualModeHostCapable)      != 0) str += " [Dual Mode Host Capable]";
        str += " ";
        ShowData("Flags", str);
        ShowData("Name", f.Name);

        switch (f.PacketType)
        {
            case wclBluetoothLeAdvertisementType.atConnectableUndirected:    str = "Connectable Undirected";     break;
            case wclBluetoothLeAdvertisementType.atConnectableDirected:      str = "Connectable Directed";       break;
            case wclBluetoothLeAdvertisementType.atScannableUndirected:      str = "Scannable Undirected";       break;
            case wclBluetoothLeAdvertisementType.atNonConnectableUndirected: str = "Non Connectable Undirected"; break;
            case wclBluetoothLeAdvertisementType.atScanResponse:             str = "Scan Response";              break;
            case wclBluetoothLeAdvertisementType.atExtended:                 str = "Extended";                   break;
            default:                                                          str = "Unknown";                    break;
        }
        ShowData("Packet Type", str);
    }

    private void ShowRawFrame(RawFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Data Type", f.DataType.ToString("X2"));
        ShowFrameRawData(f.Data);
    }

    private void ShowDetailsFrame(DetailsFrame f)
    {
        ShowFrameBaseData(f);
        ShowFrameRawData(f.Data);
    }

    private void ShowService16DataFrame(Service16DataFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString("X4"));
        ShowFrameRawData(f.Data);
    }

    private void ShowService32DataFrame(Service32DataFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString("X8"));
        ShowFrameRawData(f.Data);
    }

    private void ShowService128DataFrame(Service128DataFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString());
        ShowFrameRawData(f.Data);
    }

    private void ShowSol16Frame(Sol16Frame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString("X4"));
    }

    private void ShowSol32Frame(Sol32Frame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString("X8"));
    }

    private void ShowSol128Frame(Sol128Frame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString());
    }

    private void ShowTxPowerFrame(TxPowerFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("TX Power", f.TxPower.ToString());
    }

    private void ShowUuidFrame(UuidFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID", f.Uuid.ToString());
    }

    private void ShowAltBeaconFrame(AltBeaconFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID",      f.Uuid.ToString());
        ShowData("CompanyId", f.CompanyId.ToString("X4"));
        ShowData("Major",     f.Major.ToString("X4"));
        ShowData("Minor",     f.Minor.ToString("X4"));
        ShowData("TX RSSI",   f.TxRssi.ToString());
        ShowData("Reserved",  f.Reserved.ToString("X2"));
        ShowFrameRawData(f.Data);
    }

    private void ShowEddystoneTlmFrame(EddystoneTlmFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Advertisements Counter", f.AdvCnt.ToString());
        ShowData("Battery status",          f.Batt.ToString());
        ShowData("Seconds",                 f.SecCnt.ToString());
        ShowData("Temperature",             f.Temp.ToString());
        ShowFrameRawData(f.Data);
    }

    private void ShowEddystoneUidFrame(EddystoneUidFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID",    f.Uuid.ToString());
        ShowData("TX RSSI", f.TxRssi.ToString());
        ShowFrameRawData(f.Data);
    }

    private void ShowEddystoneUrlFrame(EddystoneUrlFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("TX RSSI", f.TxRssi.ToString());
        ShowData("URL",     f.Url);
    }

    private void ShowManufacturerFrame(ManufacturerFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Company ID", f.CompanyId.ToString("X4"));
        ShowFrameRawData(f.Data);
    }

    private void ShowMicrosoftCdpBeaconFrame(MicrosoftCdpBeaconFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Address As Device ID", f.AddressAsDeviceId.ToString());

        string devType;
        switch (f.DeviceType)
        {
            case wclBluetoothLeCdpBeaconDeviceType.dtXboxOne:         devType = "Xbox one";        break;
            case wclBluetoothLeCdpBeaconDeviceType.dtiPhone:          devType = "iPhone";          break;
            case wclBluetoothLeCdpBeaconDeviceType.dtiPad:            devType = "iPad";            break;
            case wclBluetoothLeCdpBeaconDeviceType.dtAndroid:         devType = "Android";         break;
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsDesktop:  devType = "Windows Desktop"; break;
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsPhone:    devType = "Windows Phone";   break;
            case wclBluetoothLeCdpBeaconDeviceType.dtLinux:           devType = "Linux";           break;
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsIoT:      devType = "Windows IoT";     break;
            case wclBluetoothLeCdpBeaconDeviceType.dtSurfaceHub:      devType = "Surface Hub";     break;
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsLaptop:   devType = "Windows Laptop";  break;
            case wclBluetoothLeCdpBeaconDeviceType.dtWindowsTablet:   devType = "Windows Tablet";  break;
            default: devType = ((byte)f.DeviceType).ToString("X2"); break;
        }
        ShowData("Device Type", devType);

        string str = "";
        if ((f.ExtendedDeviceStatus & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsRemoteSessionsHosted)           != 0) str += " [Remote Sessions Hosted]";
        if ((f.ExtendedDeviceStatus & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsRemoteSessionsNotHosted)        != 0) str += " [Remote Sessions Not Hosted]";
        if ((f.ExtendedDeviceStatus & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsNearShareAuthPolicySameUser)    != 0) str += " [Near Share Auth Policy Same User]";
        if ((f.ExtendedDeviceStatus & wclBluetoothLeCdpBeaconExtendedDeviceStatus.edsNearShareAuthPolicyPermissive)  != 0) str += " [Near Share Auth Policy Permissive]";
        str += " ";
        ShowData("Extended Device Status", str);

        str = "";
        if (f.Hash != null) foreach (var b in f.Hash) str += b.ToString("X2") + " ";
        ShowData("Hash", str);

        str = "";
        if (f.Salt != null) foreach (var b in f.Salt) str += b.ToString("X2") + " ";
        ShowData("Salt", str);

        string st = (f.ScenarioType == wclBluetoothLeCdpBeaconScenarioType.stBluetooth)
            ? "Bluetooth" : ((byte)f.ScenarioType).ToString("X2");
        ShowData("Scenario Type", st);

        ShowData("Share Near By", f.ShareNearBy.ToString());
        ShowData("Subversion",    f.SubVersion.ToString("X2"));
        ShowData("Version",       f.Version.ToString("X2"));
    }

    private void ShowAppleAirdropFrame(AppleAirdropFrame f)
    {
        ShowFrameBaseData(f);
        string str = "";
        foreach (var b in f.Data.Prefix) str += b.ToString("X2");
        ShowData("Prefix",   str);
        ShowData("Version",  f.Data.Version.ToString("X2"));
        ShowData("Apple ID", f.Data.AppleId.ToString("X4"));
        ShowData("Phone",    f.Data.PhoneNumber.ToString("X4"));
        ShowData("E-Mail",   f.Data.Email.ToString("X4"));
        ShowData("E-Mail 2", f.Data.Email2.ToString("X4"));
        ShowData("Suffix",   f.Data.Suffix.ToString("X2"));
    }

    private void ShowAppleAirplaySourceFrame(AppleAirplaySourceFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Data byte", f.Data.Data.ToString("X2"));
    }

    private void ShowAppleAirplayTargetFrame(AppleAirplayTargetFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Flags", f.Data.Flags.ToString("X2"));
        ShowData("Seed",  f.Data.Seed.ToString("X2"));

        string str = ((byte)(f.Data.Address >> 24)).ToString() + "." +
                     ((byte)((f.Data.Address & 0x00FF0000) >> 16)).ToString() + "." +
                     ((byte)((f.Data.Address & 0x0000FF00) >> 8)).ToString() + "." +
                     ((byte)(f.Data.Address & 0x000000FF)).ToString();
        ShowData("IPv4 address", str);
    }

    private void ShowAppleAirprintFrame(AppleAirprintFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Address type",  f.Data.AddressType.ToString("X2"));
        ShowData("Resource type", f.Data.ResourceType.ToString("X2"));
        ShowData("Security type", f.Data.SecurityType.ToString("X2"));
        ShowData("Port",          f.Data.Port.ToString());

        string str = "";
        for (byte i = 0; i < 15; i++) str += f.Data.Address[i].ToString() + ".";
        str += f.Data.Address[15].ToString();
        ShowData("Address", str);
        ShowData("Power", f.Data.Power.ToString());
    }

    private void ShowAppleHomeKitFrame(AppleHomeKitFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Status", f.Data.Status.ToString("X2"));

        string str = "";
        foreach (var b in f.Data.DeviceId) str += b.ToString("X2");
        ShowData("Device ID", str);

        string cat;
        switch (f.Data.Category)
        {
            case 0x0000: cat = "Unknown"; break;
            case 0x0001: cat = "Other"; break;
            case 0x0002: cat = "Bridge"; break;
            case 0x0003: cat = "Fan"; break;
            case 0x0004: cat = "Garage Door Opener"; break;
            case 0x0005: cat = "Lightbulb"; break;
            case 0x0006: cat = "Door Lock"; break;
            case 0x0007: cat = "Outlet"; break;
            case 0x0008: cat = "Switch"; break;
            case 0x0009: cat = "Thermostat"; break;
            case 0x000A: cat = "Sensor"; break;
            case 0x000B: cat = "Security System"; break;
            case 0x000C: cat = "Door"; break;
            case 0x000D: cat = "Window"; break;
            case 0x000E: cat = "Window Covering"; break;
            case 0x000F: cat = "Programmable Switch"; break;
            case 0x0010: cat = "Range Extender"; break;
            case 0x0011: cat = "IP Camera"; break;
            case 0x0012: cat = "Video Doorbell"; break;
            case 0x0013: cat = "Air Purifier"; break;
            case 0x0014: cat = "Heater"; break;
            case 0x0015: cat = "Air Conditioner"; break;
            case 0x0016: cat = "Humidifier"; break;
            case 0x0017: cat = "Dehumidifier"; break;
            case 0x001C: cat = "Sprinklers"; break;
            case 0x001D: cat = "Faucets"; break;
            case 0x001E: cat = "Shower Systems"; break;
            default: cat = "Unknown: 0x" + f.Data.Category.ToString("X4"); break;
        }
        ShowData("Category",      cat);
        ShowData("State Number",  f.Data.StateNumber.ToString("X4"));
        ShowData("Config Number", f.Data.ConfigNumber.ToString("X2"));
        ShowData("Version",       f.Data.Version.ToString("X2"));
    }

    private void ShowAppleiBeaconFrame(AppleiBeaconFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("UUID",    f.Data.Uuid.ToString());
        ShowData("Major",   f.Data.Major.ToString("X4"));
        ShowData("Minor",   f.Data.Minor.ToString("X4"));
        ShowData("TX RSSI", f.Data.TxRssi.ToString());
    }

    private void ShowAppleNearbyActionFrame(AppleNearbyActionFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Flags", f.Data.Flags.ToString("X2"));

        string action;
        switch (f.Data.Action)
        {
            case 0x01: action = "Apple TV Setup"; break;
            case 0x04: action = "Mobile Backup"; break;
            case 0x05: action = "Watch Setup"; break;
            case 0x06: action = "Apple TV Pair"; break;
            case 0x07: action = "Internet Relay"; break;
            case 0x08: action = "WiFi Password"; break;
            case 0x09: action = "iOS Setup"; break;
            case 0x0A: action = "Repair"; break;
            case 0x0B: action = "Speaker Setupd"; break;
            case 0x0C: action = "Apple Pay"; break;
            case 0x0D: action = "Whole Home Audio Setup"; break;
            case 0x0E: action = "Developer Tools Pairing Request"; break;
            case 0x0F: action = "Answered Call"; break;
            case 0x10: action = "Ended Call"; break;
            case 0x11: action = "DD Ping"; break;
            case 0x12: action = "DD Pong"; break;
            case 0x13: action = "Remote Auto Fill"; break;
            case 0x14: action = "Companion Link Proximity"; break;
            case 0x15: action = "Remote Management"; break;
            case 0x16: action = "Remote Auto Fill Pong"; break;
            case 0x17: action = "Remote Display"; break;
            default: action = "Unknown: 0x" + f.Data.Action.ToString("X2"); break;
        }
        ShowData("Action", action);
        ShowData("Tag",    f.Data.Tag.ToString("X6"));

        string device;
        switch (f.Data.Device)
        {
            case 0x02: device = "iPhone"; break;
            case 0x04: device = "iPod"; break;
            case 0x06: device = "iPad"; break;
            case 0x08: device = "Audio accessory (HomePod)"; break;
            case 0x0A: device = "Mac"; break;
            case 0x0C: device = "AppleTV"; break;
            case 0x0E: device = "Watch"; break;
            default: device = "Unknown: 0x" + f.Data.Device.ToString("X2"); break;
        }
        ShowData("Device", device);

        if (f.Data.Params != null && f.Data.Params.Length > 0)
        {
            string str = "";
            foreach (var b in f.Data.Params) str += b.ToString("X2");
            ShowData("Params", str);
        }
    }

    private void ShowAppleNearbyInfoFrame(AppleNearbyInfoFrame f)
    {
        ShowFrameBaseData(f);

        ShowData("Primary device", (f.Data.Status & 0x01) != 0 ? "YES" : "NO");
        ShowData("AirDrop",        (f.Data.Status & 0x04) != 0 ? "RECEIVING" : "NOT ACTIVE");

        string action;
        switch (f.Data.Action)
        {
            case 0x00: action = "None"; break;
            case 0x01: action = "Activity reporting disabled"; break;
            case 0x03: action = "Idle user"; break;
            case 0x05: action = "Audio playing while screen locked"; break;
            case 0x07: action = "Active user (screen on)"; break;
            case 0x09: action = "Screen on with video playing"; break;
            case 0x0A: action = "Watch on wrist and unlocked"; break;
            case 0x0B: action = "Recent user interaction"; break;
            case 0x0D: action = "User is driving a vehicle"; break;
            case 0x0E: action = "Phone or Facetime Call"; break;
            default: action = "Unknown: 0x" + f.Data.Action.ToString("X2"); break;
        }
        ShowData("Action", action);

        ShowData("AirDrop state",     (f.Data.Flags & 0x01) != 0 ? "Connected and Screen ON" : "Disconnected");
        ShowData("Auth tag length",   (f.Data.Flags & 0x02) != 0 ? "4 bytes" : "3 bytes");
        ShowData("WiFi",              (f.Data.Flags & 0x04) != 0 ? "ON" : "OFF");
        ShowData("Auth Tag Present",  (f.Data.Flags & 0x10) != 0 ? "YES" : "NO");
        ShowData("Apple Watch",       (f.Data.Flags & 0x20) != 0 ? "LOCKED" : "UNLOCKED");
        ShowData("Apple Watch AutoLock", (f.Data.Flags & 0x40) != 0 ? "ENABLED" : "DISABLED");
        ShowData("AutoLock",          (f.Data.Flags & 0x80) != 0 ? "ENABLED" : "DISABLED");

        if ((f.Data.Flags & 0x10) != 0)
        {
            ShowData("Auth Tag", (f.Data.Flags & 0x02) != 0
                ? f.Data.Tag.ToString("X8")
                : f.Data.Tag.ToString("X6"));
        }
    }

    private void ShowAppleProximityPairingFrame(AppleProximityPairingFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Prefix", f.Data.Prefix.ToString("X2"));

        string model;
        switch (f.Data.DeviceModel)
        {
            case 0x2002: model = "AirPods"; break;
            case 0x2003: model = "Powerbeats 3"; break;
            case 0x2005: model = "BeatsX"; break;
            case 0x2006: model = "Beats Solo 3"; break;
            case 0x2007: model = "Beats Studio 3"; break;
            case 0x2009: model = "Beats Studio 3"; break;
            case 0x200A: model = "AirPods Max"; break;
            case 0x200B: model = "Powerbeats Pro"; break;
            case 0x200C: model = "Beats Solo Pro"; break;
            case 0x200D: model = "Powerbeats 4"; break;
            case 0x200E: model = "AirPods Pro"; break;
            case 0x200F: model = "AirPods 2"; break;
            case 0x2010: model = "Beats Flex"; break;
            case 0x2011: model = "Beats Studio Buds"; break;
            case 0x2012: model = "Beats Fit Pro"; break;
            case 0x2013: model = "AirPods 3"; break;
            case 0x2014: model = "AirPods Pro 2"; break;
            default: model = "Unknown: 0x" + f.Data.DeviceModel.ToString("X4"); break;
        }
        ShowData("Device mode", model);

        string status;
        switch (f.Data.Status)
        {
            case 0x01: status = "AirPods: Both out of case, not in ear"; break;
            case 0x02: status = "Right in ear, Left in case"; break;
            case 0x03: status = "AirPods: Right in ear, Left out of case"; break;
            case 0x0B: status = "Both AirPods in ear"; break;
            case 0x11: status = "AirPods: Right out of case, Left in case"; break;
            case 0x13: status = "AirPods: Right in ear, Left in case"; break;
            case 0x21: status = "Both taken out of ears, Pause Audio"; break;
            case 0x22: status = "Left in ear, Right in case"; break;
            case 0x23: status = "AirPods: Left in ear, Right out of case"; break;
            case 0x2B: status = "Both AirPods in ear"; break;
            case 0x31: status = "AirPods: Left out of case, Right in case"; break;
            case 0x33: status = "AirPods: Left in ear, Right in case"; break;
            case 0x51: status = "Case: Left out of case, Right in case"; break;
            case 0x53: status = "Case: Left in ear, Right in case"; break;
            case 0x55: status = "Case: Both AirPods in case"; break;
            case 0x71: status = "Case: Right out of case, Left in case"; break;
            case 0x73: status = "Case: Right in ear, Left in case"; break;
            case 0x75: status = "Case: Both AirPods in case"; break;
            default: status = "Unknown: 0x" + f.Data.Status.ToString("X2"); break;
        }
        ShowData("Status", status);

        ShowData("Left Battery",  f.Data.LeftBattery.ToString());
        ShowData("Right Battery", f.Data.RightBattery.ToString());
        ShowData("Case charging",  f.Data.CaseCharging ? "Yes" : "No");
        ShowData("Right charging", f.Data.RightCharging ? "Yes" : "No");
        ShowData("Left charging",  f.Data.LeftCharging ? "Yes" : "No");
        ShowData("Case Battery",   f.Data.CaseBattery.ToString());
        ShowData("Lid open counter", f.Data.Counter.ToString());

        string color;
        switch (f.Data.Color)
        {
            case 0x00: color = "White"; break;
            case 0x01: color = "Black"; break;
            case 0x02: color = "Red"; break;
            case 0x03: color = "Blue"; break;
            case 0x04: color = "Pink"; break;
            case 0x05: color = "Gray"; break;
            case 0x06: color = "Silver"; break;
            case 0x07: color = "Gold"; break;
            case 0x08: color = "Rose Gold"; break;
            case 0x09: color = "Space Gray"; break;
            case 0x0A: color = "Dark Blue"; break;
            case 0x0B: color = "Light Blue"; break;
            case 0x0C: color = "Yellow"; break;
            default: color = "Unknown: 0x" + f.Data.Color.ToString("X2"); break;
        }
        ShowData("Color", color);
        ShowData("Suffix", f.Data.Suffix.ToString("X2"));

        string str = "";
        foreach (var b in f.Data.Data) str += b.ToString("X2");
        ShowData("Encrypted", str);
    }

    private void ShowAppleUnknownFrame(AppleUnknownFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Message ID", f.Data.Id.ToString("X2"));

        if (f.Data.Payload != null && f.Data.Payload.Length > 0)
        {
            string str = "";
            foreach (var b in f.Data.Payload) str += b.ToString("X2");
            ShowData("Payload", str);
        }
    }

    private void ShowDriAsdFrame(DriAsdFrame f)
    {
        ShowFrameBaseData(f);
        ShowDriFrameRawData(f.Raw);
    }

    private void ShowAppleFindMyFrame(AppleFindMyFrame f)
    {
        ShowFrameBaseData(f);

        string status;
        switch (f.Data.Status)
        {
            case 0x00: status = "Owner did not connect within key rotation period (15 min.)"; break;
            case 0x24: status = "Owner connected with key roation period, Battery Full"; break;
            case 0x64: status = "Owner connected with key roation period, Battery Medium"; break;
            case 0xA4: status = "Owner connected with key roation period, Battery Low"; break;
            case 0xE4: status = "Owner connected with key roation period, Battery Critically Low"; break;
            default: status = f.Data.Status.ToString("X2"); break;
        }
        ShowData("Status", status);

        string str = "";
        foreach (var b in f.Data.Key) str += b.ToString("X2");
        ShowData("Key", str);

        string bits;
        switch (f.Data.KeyBits)
        {
            case 0x00: bits = "bits 6 and 7 not set in public key"; break;
            case 0x01: bits = "bit 6 set in public key"; break;
            case 0x02: bits = "bit 7 set in public key"; break;
            case 0x03: bits = "bits 6 and 7 set in public key"; break;
            default: bits = f.Data.KeyBits.ToString("X2"); break;
        }
        ShowData("Key bits", bits);
        ShowData("Hint", f.Data.Hint.ToString("X2"));
    }

    private void ShowAppleHandoffFrame(AppleHandoffFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Clipboard", f.Data.Clipboard ? "Yes" : "No");
        ShowData("Sequence",  f.Data.Sequence.ToString());
        ShowData("Auth tag",  f.Data.AuthTag.ToString("X2"));

        string str = "";
        foreach (var b in f.Data.Data) str += b.ToString("X2");
        ShowData("Data", str);
    }

    private void ShowAppleHeySiriFrame(AppleHeySiriFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Hash", f.Data.Hash.ToString("X4"));
        ShowData("Snr",  f.Data.Snr.ToString());

        string conf;
        switch (f.Data.Confidence)
        {
            case 0x03: conf = "Not on Wrist"; break;
            case 0x1F: conf = "Wrist detection disabled"; break;
            case 0x3F: conf = "On Wrist"; break;
            default: conf = f.Data.Confidence.ToString("X2"); break;
        }
        ShowData("Confidence", conf);

        string cls;
        switch (f.Data.DeviceClass)
        {
            case 0x0002: cls = "iPhone"; break;
            case 0x0003: cls = "iPad"; break;
            case 0x0007: cls = "HomePod"; break;
            case 0x0009: cls = "MacBook"; break;
            case 0x000A: cls = "Watch"; break;
            default: cls = f.Data.DeviceClass.ToString("X4"); break;
        }
        ShowData("Device class", cls);
        ShowData("Rand", f.Data.Rand.ToString("X2"));
    }

    private void ShowAppleMagicSwitchFrame(AppleMagicSwitchFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Data", f.Data.Data.ToString("X4"));

        string conf;
        switch (f.Data.Confidence)
        {
            case 0x03: conf = "Not on Wrist"; break;
            case 0x1F: conf = "Wrist detection disabled"; break;
            case 0x3F: conf = "On Wrist"; break;
            default: conf = f.Data.Confidence.ToString("X2"); break;
        }
        ShowData("Confidence", conf);
    }

    private void ShowAppleTetheringSourceFrame(AppleTetheringSourceFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Version", f.Data.Version.ToString("X2"));
        ShowData("Flags",   f.Data.Flags.ToString("X2"));
        ShowData("Battery", f.Data.Battery.ToString());

        string cellType;
        switch (f.Data.CellType)
        {
            case 0x0000: cellType = "4G (GSM)"; break;
            case 0x0001: cellType = "1xRTT"; break;
            case 0x0002: cellType = "GPRS"; break;
            case 0x0003: cellType = "EDGE"; break;
            case 0x0004: cellType = "3G (EV-DO)"; break;
            case 0x0005: cellType = "3G"; break;
            case 0x0006: cellType = "4G"; break;
            case 0x0007: cellType = "LTE"; break;
            default: cellType = f.Data.CellType.ToString("X4"); break;
        }
        ShowData("Cell type", cellType);
        ShowData("Bars", f.Data.Bars.ToString());
    }

    private void ShowAppleTetheringTargetFrame(AppleTetheringTargetFrame f)
    {
        ShowFrameBaseData(f);
        ShowData("Id", f.Data.Id.ToString("X8"));
    }
}