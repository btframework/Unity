using System;
using wclBluetooth;

public enum AdvertisementFrame
{
    afAppearance, afExtInformation, afInformation, afRaw, afDetails,
    afService16Data, afService32Data, afService128Data,
    afSol16, afSol32, afSol128, afTxPower, afUuid, afAltBeacon,
    afEddystoneTlm, afEddystoneUid, afEddystoneUrl,
    afManufacturerRaw, afMicrosoftCdpBeacon,
    afAppleAirdrop, afAppleAirplaySource, afAppleAirplayTarget, afAppleAirprint,
    afAppleHomeKit, afAppleiBeacon, afAppleNearbyAction, afAppleNearbyInfo,
    afAppleProximityPairing, afAppleUnknown, afAppleFindMy, afAppleHandoff,
    afAppleHeySiri, afAppleMagicSwitch,
    afAppleTetheringSource, afAppleTetheringTarget,
    afDriAsd, afUnknown
}

public abstract class FrameStorage
{
    private readonly long _address;
    private readonly AdvertisementFrame _frame;
    private readonly long _timestamp;
    private readonly sbyte _rssi;

    protected FrameStorage(AdvertisementFrame frame, long address, long timestamp, sbyte rssi)
    {
        _address = address; _frame = frame; _timestamp = timestamp; _rssi = rssi;
    }

    public long Address => _address;
    public AdvertisementFrame Frame => _frame;
    public long Timestamp => _timestamp;
    public sbyte Rssi => _rssi;
}

public abstract class GuidFrameStorage : FrameStorage
{
    protected GuidFrameStorage(AdvertisementFrame frame, long address, long timestamp, sbyte rssi, Guid uuid)
        : base(frame, address, timestamp, rssi) { Uuid = uuid; }
    public Guid Uuid { get; }
}

public abstract class DataFrameStorage : FrameStorage
{
    protected DataFrameStorage(AdvertisementFrame frame, long address, long timestamp, sbyte rssi, byte[] data)
        : base(frame, address, timestamp, rssi) { Data = data; }
    public byte[] Data { get; }
}

public abstract class GuidDataFrameStorage : DataFrameStorage
{
    protected GuidDataFrameStorage(AdvertisementFrame frame, long address, long timestamp, sbyte rssi, Guid uuid, byte[] data)
        : base(frame, address, timestamp, rssi, data) { Uuid = uuid; }
    public Guid Uuid { get; }
}

public sealed class AppearanceFrame : FrameStorage
{
    public AppearanceFrame(long address, long ts, sbyte rssi, ushort appearance)
        : base(AdvertisementFrame.afAppearance, address, ts, rssi) => Appearance = appearance;
    public ushort Appearance { get; }
}

public sealed class ExtInformationFrame : FrameStorage
{
    public ExtInformationFrame(long address, long ts, sbyte rssi,
        wclBluetoothAddressType addressType, wclBluetoothLeExtendedFrameFlag flags,
        sbyte txPower, wclBluetoothLeAdvertisementPhy primaryPhy, wclBluetoothLeAdvertisementPhy secondaryPhy)
        : base(AdvertisementFrame.afExtInformation, address, ts, rssi)
    {
        AddressType = addressType; Flags = flags; TxPower = txPower;
        PrimaryPhy = primaryPhy; SecondaryPhy = secondaryPhy;
    }
    public wclBluetoothAddressType AddressType { get; }
    public wclBluetoothLeExtendedFrameFlag Flags { get; }
    public sbyte TxPower { get; }
    public wclBluetoothLeAdvertisementPhy PrimaryPhy { get; }
    public wclBluetoothLeAdvertisementPhy SecondaryPhy { get; }
}

public sealed class InformationFrame : FrameStorage
{
    public InformationFrame(long address, long ts, sbyte rssi,
        wclBluetoothLeAdvertisementFlag flags, string name,
        wclBluetoothLeAdvertisementType packetType)
        : base(AdvertisementFrame.afInformation, address, ts, rssi)
    {
        Flags = flags; Name = name; PacketType = packetType;
    }
    public wclBluetoothLeAdvertisementFlag Flags { get; }
    public string Name { get; }
    public wclBluetoothLeAdvertisementType PacketType { get; }
}

public sealed class RawFrame : DataFrameStorage
{
    public RawFrame(long address, long ts, sbyte rssi, byte[] data, byte dataType)
        : base(AdvertisementFrame.afRaw, address, ts, rssi, data) => DataType = dataType;
    public byte DataType { get; }
}

public sealed class DetailsFrame : DataFrameStorage
{
    public DetailsFrame(long address, long ts, sbyte rssi, byte[] data)
        : base(AdvertisementFrame.afDetails, address, ts, rssi, data) { }
}

public sealed class Service16DataFrame : DataFrameStorage
{
    public Service16DataFrame(long address, long ts, sbyte rssi, byte[] data, ushort uuid)
        : base(AdvertisementFrame.afService16Data, address, ts, rssi, data) => Uuid = uuid;
    public ushort Uuid { get; }
}

public sealed class Service32DataFrame : DataFrameStorage
{
    public Service32DataFrame(long address, long ts, sbyte rssi, byte[] data, uint uuid)
        : base(AdvertisementFrame.afService32Data, address, ts, rssi, data) => Uuid = uuid;
    public uint Uuid { get; }
}

public sealed class Service128DataFrame : GuidDataFrameStorage
{
    public Service128DataFrame(long address, long ts, sbyte rssi, Guid uuid, byte[] data)
        : base(AdvertisementFrame.afService128Data, address, ts, rssi, uuid, data) { }
}

public sealed class Sol16Frame : FrameStorage
{
    public Sol16Frame(long address, long ts, sbyte rssi, ushort uuid)
        : base(AdvertisementFrame.afSol16, address, ts, rssi) => Uuid = uuid;
    public ushort Uuid { get; }
}

public sealed class Sol32Frame : FrameStorage
{
    public Sol32Frame(long address, long ts, sbyte rssi, uint uuid)
        : base(AdvertisementFrame.afSol32, address, ts, rssi) => Uuid = uuid;
    public uint Uuid { get; }
}

public sealed class Sol128Frame : GuidFrameStorage
{
    public Sol128Frame(long address, long ts, sbyte rssi, Guid uuid)
        : base(AdvertisementFrame.afSol128, address, ts, rssi, uuid) { }
}

public sealed class TxPowerFrame : FrameStorage
{
    public TxPowerFrame(long address, long ts, sbyte rssi, sbyte txPower)
        : base(AdvertisementFrame.afTxPower, address, ts, rssi) => TxPower = txPower;
    public sbyte TxPower { get; }
}

public sealed class UuidFrame : GuidFrameStorage
{
    public UuidFrame(long address, long ts, sbyte rssi, Guid uuid)
        : base(AdvertisementFrame.afUuid, address, ts, rssi, uuid) { }
}

public abstract class BeaconFrame : GuidDataFrameStorage
{
    protected BeaconFrame(AdvertisementFrame frame, long address, long ts, sbyte rssi,
        byte[] data, Guid uuid, ushort companyId, ushort major, ushort minor, sbyte txRssi)
        : base(frame, address, ts, rssi, uuid, data)
    {
        CompanyId = companyId; Major = major; Minor = minor; TxRssi = txRssi;
    }
    public ushort CompanyId { get; }
    public ushort Major { get; }
    public ushort Minor { get; }
    public sbyte TxRssi { get; }
}

public sealed class AltBeaconFrame : BeaconFrame
{
    public AltBeaconFrame(long address, long ts, sbyte rssi, byte[] data,
        Guid uuid, ushort companyId, ushort major, ushort minor, sbyte txRssi, byte reserved)
        : base(AdvertisementFrame.afAltBeacon, address, ts, rssi, data, uuid, companyId, major, minor, txRssi)
        => Reserved = reserved;
    public byte Reserved { get; }
}

public sealed class MicrosoftCdpBeaconFrame : FrameStorage
{
    public MicrosoftCdpBeaconFrame(long address, long ts, sbyte rssi, bool addressAsDeviceId,
        wclBluetoothLeCdpBeaconDeviceType deviceType,
        wclBluetoothLeCdpBeaconExtendedDeviceStatus extendedDeviceStatus,
        byte[] hash, byte[] salt,
        wclBluetoothLeCdpBeaconScenarioType scenarioType,
        bool shareNearBy, byte subVersion, byte version)
        : base(AdvertisementFrame.afMicrosoftCdpBeacon, address, ts, rssi)
    {
        AddressAsDeviceId = addressAsDeviceId; DeviceType = deviceType;
        ExtendedDeviceStatus = extendedDeviceStatus; Hash = hash; Salt = salt;
        ScenarioType = scenarioType; ShareNearBy = shareNearBy;
        SubVersion = subVersion; Version = version;
    }
    public bool AddressAsDeviceId { get; }
    public wclBluetoothLeCdpBeaconDeviceType DeviceType { get; }
    public wclBluetoothLeCdpBeaconExtendedDeviceStatus ExtendedDeviceStatus { get; }
    public byte[] Hash { get; }
    public byte[] Salt { get; }
    public wclBluetoothLeCdpBeaconScenarioType ScenarioType { get; }
    public bool ShareNearBy { get; }
    public byte SubVersion { get; }
    public byte Version { get; }
}

public abstract class AppleFrame : FrameStorage
{
    protected AppleFrame(AdvertisementFrame frame, wclBluetoothLeAdvertisementInfo info)
        : base(frame, info.Address, info.Timestamp, info.Rssi) { }
}

public sealed class AppleAirdropFrame : AppleFrame
{
    public AppleAirdropFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirdropFrameData data)
        : base(AdvertisementFrame.afAppleAirdrop, info) => Data = data;
    public wclBluetoothLeAppleAirdropFrameData Data { get; }
}

public sealed class AppleAirplaySourceFrame : AppleFrame
{
    public AppleAirplaySourceFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirplaySourceFrameData data)
        : base(AdvertisementFrame.afAppleAirplaySource, info) => Data = data;
    public wclBluetoothLeAppleAirplaySourceFrameData Data { get; }
}

public sealed class AppleAirplayTargetFrame : AppleFrame
{
    public AppleAirplayTargetFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirplayTargetFrameData data)
        : base(AdvertisementFrame.afAppleAirplayTarget, info) => Data = data;
    public wclBluetoothLeAppleAirplayTargetFrameData Data { get; }
}

public sealed class AppleAirprintFrame : AppleFrame
{
    public AppleAirprintFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleAirprintFrameData data)
        : base(AdvertisementFrame.afAppleAirprint, info) => Data = data;
    public wclBluetoothLeAppleAirprintFrameData Data { get; }
}

public sealed class AppleHomeKitFrame : AppleFrame
{
    public AppleHomeKitFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleHomeKitFrameData data)
        : base(AdvertisementFrame.afAppleHomeKit, info) => Data = data;
    public wclBluetoothLeAppleHomeKitFrameData Data { get; }
}

public sealed class AppleiBeaconFrame : AppleFrame
{
    public AppleiBeaconFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleiBeaconFrameData data)
        : base(AdvertisementFrame.afAppleiBeacon, info) => Data = data;
    public wclBluetoothLeAppleiBeaconFrameData Data { get; }
}

public sealed class AppleNearbyActionFrame : AppleFrame
{
    public AppleNearbyActionFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleNearbyActionFrameData data)
        : base(AdvertisementFrame.afAppleNearbyAction, info) => Data = data;
    public wclBluetoothLeAppleNearbyActionFrameData Data { get; }
}

public sealed class AppleNearbyInfoFrame : AppleFrame
{
    public AppleNearbyInfoFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleNearbyInfoFrameData data)
        : base(AdvertisementFrame.afAppleNearbyInfo, info) => Data = data;
    public wclBluetoothLeAppleNearbyInfoFrameData Data { get; }
}

public sealed class AppleProximityPairingFrame : AppleFrame
{
    public AppleProximityPairingFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleProximityPairingFrameData data)
        : base(AdvertisementFrame.afAppleProximityPairing, info) => Data = data;
    public wclBluetoothLeAppleProximityPairingFrameData Data { get; }
}

public sealed class AppleUnknownFrame : AppleFrame
{
    public AppleUnknownFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleUnknownFrameData data)
        : base(AdvertisementFrame.afAppleUnknown, info) => Data = data;
    public wclBluetoothLeAppleUnknownFrameData Data { get; }
}

public sealed class AppleFindMyFrame : AppleFrame
{
    public AppleFindMyFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleFindMyFrameData data)
        : base(AdvertisementFrame.afAppleFindMy, info) => Data = data;
    public wclBluetoothLeAppleFindMyFrameData Data { get; }
}

public sealed class AppleHandoffFrame : AppleFrame
{
    public AppleHandoffFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleHandoffFrameData data)
        : base(AdvertisementFrame.afAppleHandoff, info) => Data = data;
    public wclBluetoothLeAppleHandoffFrameData Data { get; }
}

public sealed class AppleHeySiriFrame : AppleFrame
{
    public AppleHeySiriFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleHeySiriFrameData data)
        : base(AdvertisementFrame.afAppleHeySiri, info) => Data = data;
    public wclBluetoothLeAppleHeySiriFrameData Data { get; }
}

public sealed class AppleMagicSwitchFrame : AppleFrame
{
    public AppleMagicSwitchFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleMagicSwitchFrameData data)
        : base(AdvertisementFrame.afAppleMagicSwitch, info) => Data = data;
    public wclBluetoothLeAppleMagicSwitchFrameData Data { get; }
}

public sealed class AppleTetheringSourceFrame : AppleFrame
{
    public AppleTetheringSourceFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleTetheringSourceFrameData data)
        : base(AdvertisementFrame.afAppleTetheringSource, info) => Data = data;
    public wclBluetoothLeAppleTetheringSourceFrameData Data { get; }
}

public sealed class AppleTetheringTargetFrame : AppleFrame
{
    public AppleTetheringTargetFrame(wclBluetoothLeAdvertisementInfo info, wclBluetoothLeAppleTetheringTargetFrameData data)
        : base(AdvertisementFrame.afAppleTetheringTarget, info) => Data = data;
    public wclBluetoothLeAppleTetheringTargetFrameData Data { get; }
}

public sealed class EddystoneTlmFrame : DataFrameStorage
{
    public EddystoneTlmFrame(long address, long ts, sbyte rssi, byte[] data,
        uint advCnt, ushort batt, uint secCnt, double temp)
        : base(AdvertisementFrame.afEddystoneTlm, address, ts, rssi, data)
    { AdvCnt = advCnt; Batt = batt; SecCnt = secCnt; Temp = temp; }
    public uint AdvCnt { get; }
    public ushort Batt { get; }
    public uint SecCnt { get; }
    public double Temp { get; }
}

public sealed class EddystoneUidFrame : GuidDataFrameStorage
{
    public EddystoneUidFrame(long address, long ts, sbyte rssi, byte[] data, Guid uuid, sbyte txRssi)
        : base(AdvertisementFrame.afEddystoneUid, address, ts, rssi, uuid, data) => TxRssi = txRssi;
    public sbyte TxRssi { get; }
}

public sealed class EddystoneUrlFrame : FrameStorage
{
    public EddystoneUrlFrame(long address, long ts, sbyte rssi, sbyte txRssi, string url)
        : base(AdvertisementFrame.afEddystoneUrl, address, ts, rssi)
    { TxRssi = txRssi; Url = url; }
    public sbyte TxRssi { get; }
    public string Url { get; }
}

public sealed class ManufacturerFrame : DataFrameStorage
{
    public ManufacturerFrame(long address, long ts, sbyte rssi, ushort companyId, byte[] data)
        : base(AdvertisementFrame.afManufacturerRaw, address, ts, rssi, data) => CompanyId = companyId;
    public ushort CompanyId { get; }
}

public sealed class DriAsdFrame : FrameStorage
{
    public DriAsdFrame(long address, long ts, sbyte rssi, byte[] raw)
        : base(AdvertisementFrame.afDriAsd, address, ts, rssi) => Raw = raw;
    public byte[] Raw { get; }
}