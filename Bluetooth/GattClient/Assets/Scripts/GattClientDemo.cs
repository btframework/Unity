using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;
using wclCommunication;

public class GattClientDemo : MonoBehaviour
{
    // ---------------- Top bar ----------------
    public Button discoverButton;
    public Button terminateButton;
    public Button pairButton;
    public Button unpairButton;
    public Button enumPairedButton;

    public TMP_Dropdown mitmDropdown;
    public TMP_Dropdown ioCapDropdown;
    public TMP_Dropdown protectionDropdown;
    public TMP_Dropdown opFlagDropdown;

    // ---------------- Devices table ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------------- Connect bar ----------------
    public Button connectButton;
    public Button disconnectButton;
    public Toggle connectOnReadToggle;
    public Toggle forceIndicationsToggle;
    public Button getServicesButton;
    public Button getIncludedServicesButton;

    // ---------------- Connection params ----------------
    public Button getParamsButton;
    public TMP_Dropdown paramsDropdown;
    public Button setParamsButton;

    // ---------------- Services ----------------
    public Transform  servicesContent;
    public GameObject serviceRowPrefab;

    // ---------------- Characteristics ----------------
    public Transform  characteristicsContent;
    public GameObject characteristicRowPrefab;
    public Button getCharacteristicsButton;
    public Button getCharValueButton;
    public TMP_InputField charValueInput;
    public Button setValueButton;
    public TMP_Dropdown writeKindDropdown;

    // ---------------- Descriptors ----------------
    public Transform  descriptorsContent;
    public GameObject descriptorRowPrefab;
    public Button getDescriptorsButton;
    public Button getDescValueButton;

    // ---------------- Descriptor value ----------------
    public Transform  descriptorValueContent;
    public GameObject descriptorValueRowPrefab;

    // ---------------- Subscribe ----------------
    public Toggle fastSubscribeToggle;
    public TMP_Dropdown subscribeKindDropdown;
    public Button subscribeButton;
    public Button unsubscribeButton;
    public Button writeSubscribeButton;
    public Button writeUnsubscribeButton;

    // ---------------- PDU / PHY ----------------
    public Button getMaxPduSizeButton;
    public Button getPhyButton;

    // ---------------- Events ----------------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;
    public Button clearEventsButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _manager;
    private wclGattClient _client;

    private wclGattService[]         _services;
    private wclGattCharacteristic[]  _characteristics;
    private wclGattDescriptor[]      _descriptors;

    private TableRowUI _selectedDevice;
    private TableRowUI _selectedService;
    private TableRowUI _selectedCharacteristic;
    private TableRowUI _selectedDescriptor;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        discoverButton       .onClick.AddListener(OnDiscoverClicked);
        terminateButton      .onClick.AddListener(OnTerminateClicked);
        pairButton           .onClick.AddListener(OnPairClicked);
        unpairButton         .onClick.AddListener(OnUnpairClicked);
        enumPairedButton     .onClick.AddListener(OnEnumPairedClicked);

        connectButton              .onClick.AddListener(OnConnectClicked);
        disconnectButton           .onClick.AddListener(OnDisconnectClicked);
        getServicesButton          .onClick.AddListener(OnGetServicesClicked);
        getIncludedServicesButton  .onClick.AddListener(OnGetIncludedServicesClicked);

        getParamsButton            .onClick.AddListener(OnGetParamsClicked);
        setParamsButton            .onClick.AddListener(OnSetParamsClicked);

        getCharacteristicsButton   .onClick.AddListener(OnGetCharacteristicsClicked);
        getCharValueButton         .onClick.AddListener(OnGetCharValueClicked);
        setValueButton             .onClick.AddListener(OnSetValueClicked);

        getDescriptorsButton       .onClick.AddListener(OnGetDescriptorsClicked);
        getDescValueButton         .onClick.AddListener(OnGetDescValueClicked);

        subscribeButton            .onClick.AddListener(OnSubscribeClicked);
        unsubscribeButton          .onClick.AddListener(OnUnsubscribeClicked);
        writeSubscribeButton       .onClick.AddListener(OnWriteSubscribeClicked);
        writeUnsubscribeButton     .onClick.AddListener(OnWriteUnsubscribeClicked);

        getMaxPduSizeButton        .onClick.AddListener(OnGetMaxPduSizeClicked);
        getPhyButton               .onClick.AddListener(OnGetPhyClicked);

        clearEventsButton          .onClick.AddListener(() => ClearChildren(eventsContent));

        fastSubscribeToggle.onValueChanged.AddListener(OnFastSubscribeChanged);
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();
        _manager.OnNumericComparison       += ManagerOnNumericComparison;
        _manager.OnPasskeyNotification     += ManagerOnPasskeyNotification;
        _manager.OnPasskeyRequest          += ManagerOnPasskeyRequest;
        _manager.OnPinRequest              += ManagerOnPinRequest;
        _manager.OnAuthenticationCompleted += ManagerOnAuthenticationCompleted;
        _manager.OnConfirm                 += ManagerOnConfirm;
        _manager.OnProtectionLevelRequest  += ManagerOnProtectionLevelRequest;
        _manager.OnDeviceFound             += ManagerOnDeviceFound;
        _manager.OnDiscoveringCompleted    += ManagerOnDiscoveringCompleted;
        _manager.OnDiscoveringStarted      += ManagerOnDiscoveringStarted;
        _manager.OnIoCapabilityRequest     += ManagerOnIoCapabilityRequest;

        _client = new wclGattClient();
        _client.OnCharacteristicChanged    += ClientOnCharacteristicChanged;
        _client.OnConnect                  += ClientOnConnect;
        _client.OnDisconnect               += ClientOnDisconnect;
        _client.OnConnectionParamsChanged  += ClientOnConnectionParamsChanged;
        _client.OnMaxPduSizeChanged        += ClientOnMaxPduSizeChanged;
        _client.OnConnectionPhyChanged     += ClientOnConnectionPhyChanged;

        opFlagDropdown.value          = 0;
        protectionDropdown.value      = 0;
        paramsDropdown.value          = 0;
        writeKindDropdown.value       = 3;
        subscribeKindDropdown.value   = 2;
        mitmDropdown.value            = 0;
        ioCapDropdown.value           = 0;

        _manager.Open();
        Cleanup();
    }

    private void OnDestroy()
    {
        try { _client?.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
        _client = null;

        try { _manager?.Close(); } catch (Exception e) { Debug.LogException(e); }
        _manager = null;

        Cleanup();
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void ManagerOnIoCapabilityRequest(object s, wclBluetoothRadio radio, long addr,
        out wclBluetoothMitmProtection mitm, out wclBluetoothIoCapability ioCap, out bool oobPresent)
    {
        switch (mitmDropdown.value)
        {
            case 0: mitm = wclBluetoothMitmProtection.mitmProtectionNotRequired; break;
            case 1: mitm = wclBluetoothMitmProtection.mitmProtectionRequired; break;
            case 2: mitm = wclBluetoothMitmProtection.mitmProtectionNotRequiredBonding; break;
            case 3: mitm = wclBluetoothMitmProtection.mitmProtectionRequiredBonding; break;
            case 4: mitm = wclBluetoothMitmProtection.mitmProtectionNotRequiredGeneralBonding; break;
            case 5: mitm = wclBluetoothMitmProtection.mitmProtectionRequiredGeneralBonding; break;
            default: mitm = wclBluetoothMitmProtection.mitmProtectionNotDefined; break;
        }

        switch (ioCapDropdown.value)
        {
            case 0: ioCap = wclBluetoothIoCapability.iocapDisplayOnly; break;
            case 1: ioCap = wclBluetoothIoCapability.iocapDisplayYesNo; break;
            case 2: ioCap = wclBluetoothIoCapability.iocapKeyboardOnly; break;
            case 3: ioCap = wclBluetoothIoCapability.iocapNoInputNoOutput; break;
            case 4: ioCap = wclBluetoothIoCapability.iocapDisplayKeyboard; break;
            default: ioCap = wclBluetoothIoCapability.iocapNotDefined; break;
        }

        oobPresent = false;
    }

    private void ManagerOnNumericComparison(object s, wclBluetoothRadio radio, long addr,
        uint number, out bool confirm)
    {
        confirm = true;
        TraceEvent(addr, "Numeric comparison", "Number", number.ToString());
    }

    private void ManagerOnPasskeyNotification(object s, wclBluetoothRadio radio, long addr, uint passkey)
        => TraceEvent(addr, "Passkey notification", "Passkey", passkey.ToString());

    private void ManagerOnPasskeyRequest(object s, wclBluetoothRadio radio, long addr, out uint passkey)
    {
        passkey = 123456;
        TraceEvent(addr, "Passkey request", "Passkey", passkey.ToString());
    }

    private void ManagerOnPinRequest(object s, wclBluetoothRadio radio, long addr, out string pin)
    {
        pin = "123456";
        TraceEvent(addr, "Pin request", "PIN", pin);
    }

    private void ManagerOnConfirm(object s, wclBluetoothRadio radio, long addr, out bool confirm)
    {
        confirm = true;
        TraceEvent(addr, "Just works pairing", "Accept", "True");
    }

    private void ManagerOnAuthenticationCompleted(object s, wclBluetoothRadio radio, long addr, int error)
        => TraceEvent(addr, "Authentication completed", "Error", "0x" + error.ToString("X8"));

    private void ManagerOnProtectionLevelRequest(object s, wclBluetoothRadio radio, long addr,
        out wclBluetoothLeProtectionLevel protection)
    {
        switch (protectionDropdown.value)
        {
            case 0: protection = wclBluetoothLeProtectionLevel.pplNone; break;
            case 1: protection = wclBluetoothLeProtectionLevel.pplDefault; break;
            case 2: protection = wclBluetoothLeProtectionLevel.pplEncryption; break;
            case 3: protection = wclBluetoothLeProtectionLevel.pplEncryptionAndAuthentication; break;
            default: protection = wclBluetoothLeProtectionLevel.pplDefault; break;
        }
    }

    private void ManagerOnDiscoveringStarted(object s, wclBluetoothRadio radio)
    {
        ClearChildren(devicesContent);
        _selectedDevice = null;
        TraceEvent(0, "Discovering started", "", "");
    }

    private void ManagerOnDiscoveringCompleted(object s, wclBluetoothRadio radio, int error)
    {
        if (devicesContent.childCount == 0)
        {
            Log("No BLE devices were found.");
        }
        else
        {
            foreach (Transform child in devicesContent)
            {
                var row = child.GetComponent<TableRowUI>();
                if (row == null) continue;
                long addr;
                try { addr = Convert.ToInt64(row.GetCell(0), 16); } catch { continue; }
                string name;
                Int32 res = radio.GetRemoteName(addr, out name);
                row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? name : "Error: 0x" + res.ToString("X8"));
            }
        }
        TraceEvent(0, "Discovering completed", "", "");
    }

    private void ManagerOnDeviceFound(object s, wclBluetoothRadio radio, long addr)
    {
        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.Tag = radio;
        row.SetCell(0, addr.ToString("X12"));
        row.SetCell(1, "");

        wclBluetoothDeviceType devType;
        Int32 res = radio.GetRemoteDeviceType(addr, out devType);
        row.SetCell(2, res != wclErrors.WCL_E_SUCCESS ? "Error: 0x" + res.ToString("X8") : DeviceTypeStr(devType));

        bool paired;
        res = radio.GetRemotePaired(addr, out paired);
        row.SetCell(3, res != wclErrors.WCL_E_SUCCESS ? "Error: 0x" + res.ToString("X8") : paired.ToString());

        TraceEvent(addr, "Device found", "", "");
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientOnConnect(object s, int error)
    {
        var c = (wclGattClient)s;
        TraceEvent(c.Address, "Connected", "Error", "0x" + error.ToString("X8"));
        if (error != wclErrors.WCL_E_SUCCESS) return;

        TraceEvent(_client.Address, "Max PDU", "", "");
        GetMaxPduSize();
        TraceEvent(_client.Address, "Connection params", "", "");
        GetConnectionParams();
        TraceEvent(_client.Address, "Connection PHY", "", "");
        GetConnectionPhy();
    }

    private void ClientOnDisconnect(object s, int reason)
    {
        var c = (wclGattClient)s;
        TraceEvent(c.Address, "Disconnected", "Reason", "0x" + reason.ToString("X8"));
        Cleanup();
    }

    private void ClientOnCharacteristicChanged(object s, ushort handle, byte[] value)
    {
        TraceEvent(((wclGattClient)s).Address, "ValueChanged", "Handle", handle.ToString("X4"));
        TraceEvent(0, "", "Value", value != null && value.Length > 0 ? HexStr(value) : "");
    }

    private void ClientOnConnectionParamsChanged(object s, EventArgs e)
    {
        TraceEvent(_client.Address, "Connection params changed", "", "");
        GetConnectionParams();
    }

    private void ClientOnMaxPduSizeChanged(object s, EventArgs e)
    {
        TraceEvent(_client.Address, "Max PDU size changed", "", "");
        GetMaxPduSize();
    }

    private void ClientOnConnectionPhyChanged(object s, EventArgs e)
    {
        TraceEvent(_client.Address, "Connection PHY changed", "", "");
        GetConnectionPhy();
    }

    // ==================================================================
    //  Button handlers
    // ==================================================================
    private void OnDiscoverClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;
        Int32 res = radio.Discover(10, wclBluetoothDiscoverKind.dkBle);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error starting discovering: 0x" + res.ToString("X8"));
    }

    private void OnTerminateClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;
        Int32 res = radio.Terminate();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error terminating discovering: 0x" + res.ToString("X8"));
    }

    private void OnPairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
        Int32 res = radio.RemotePair(addr, wclBluetoothPairingMethod.pmLe);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnUnpairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
        Int32 res = radio.RemoteUnpair(addr, wclBluetoothPairingMethod.pmLe);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnEnumPairedClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;

        ClearChildren(devicesContent);
        _selectedDevice = null;

        long[] devices;
        Int32 res = radio.EnumPairedDevices(wclBluetoothDiscoverKind.dkBle, out devices);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Enum paired failed: 0x" + res.ToString("X8")); return; }
        if (devices == null) return;

        foreach (var devAddr in devices)
        {
            var rowGO = Instantiate(deviceRowPrefab, devicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnDeviceRowClicked);
            row.Tag = radio;
            row.SetCell(0, devAddr.ToString("X12"));

            string name;
            res = radio.GetRemoteName(devAddr, out name);
            row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? name : "Error: 0x" + res.ToString("X8"));

            wclBluetoothDeviceType devType;
            res = radio.GetRemoteDeviceType(devAddr, out devType);
            row.SetCell(2, res != wclErrors.WCL_E_SUCCESS ? "Error: 0x" + res.ToString("X8") : DeviceTypeStr(devType));

            bool paired;
            res = radio.GetRemotePaired(devAddr, out paired);
            row.SetCell(3, res != wclErrors.WCL_E_SUCCESS ? "Error: 0x" + res.ToString("X8") : paired.ToString());
        }
    }

    private void OnConnectClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        try
        {
            var radio = _selectedDevice.Tag as wclBluetoothRadio;
            _client.Address = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
            _client.ConnectOnRead = connectOnReadToggle.isOn;
            _client.ForceNotifications = forceIndicationsToggle.isOn;
            Int32 res = _client.Connect(radio);
            if (res != wclErrors.WCL_E_SUCCESS)
                Log("Error: 0x" + res.ToString("X8"));
        }
        catch (Exception ex) { Log(ex.Message); }
    }

    private void OnDisconnectClicked()
    {
        Int32 res = _client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnGetServicesClicked()
    {
        ClearChildren(servicesContent);
        _selectedService = null;
        _services = null;

        Int32 res = _client.ReadServices(OpFlag(), out _services);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Error: 0x" + res.ToString("X8")); return; }
        if (_services == null) return;

        foreach (var service in _services)
        {
            var rowGO = Instantiate(serviceRowPrefab, servicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnServiceRowClicked);
            row.Tag = service;
            row.SetCell(0, UuidToString(service.Uuid));
            row.SetCell(1, service.Uuid.IsShortUuid.ToString());
            row.SetCell(2, service.Handle.ToString("X4"));
        }
    }

    private void OnGetIncludedServicesClicked()
    {
        if (_selectedService == null) { Log("Select service"); return; }

        var service = (wclGattService)_selectedService.Tag;
        ClearChildren(servicesContent);
        _selectedService = null;
        _services = null;

        Int32 res = _client.ReadIncludedServices(service, OpFlag(), out _services);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Error: 0x" + res.ToString("X8")); return; }
        if (_services == null) return;

        foreach (var incService in _services)
        {
            var rowGO = Instantiate(serviceRowPrefab, servicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnServiceRowClicked);
            row.Tag = incService;
            row.SetCell(0, UuidToString(incService.Uuid));
            row.SetCell(1, incService.Uuid.IsShortUuid.ToString());
            row.SetCell(2, incService.Handle.ToString("X4"));
        }
    }

    private void OnGetCharacteristicsClicked()
    {
        _characteristics = null;
        ClearChildren(characteristicsContent);
        _selectedCharacteristic = null;

        if (_selectedService == null) { Log("Select service"); return; }

        var service = (wclGattService)_selectedService.Tag;
        Int32 res = _client.ReadCharacteristics(service, OpFlag(), out _characteristics);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Error: 0x" + res.ToString("X8")); return; }
        if (_characteristics == null) return;

        foreach (var ch in _characteristics)
        {
            var rowGO = Instantiate(characteristicRowPrefab, characteristicsContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnCharacteristicRowClicked);
            row.Tag = ch;
            row.SetCell(0, UuidToString(ch.Uuid));
            row.SetCell(1, ch.Uuid.IsShortUuid.ToString());
            row.SetCell(2, ch.ServiceHandle.ToString("X4"));
            row.SetCell(3, ch.Handle.ToString("X4"));
            row.SetCell(4, ch.ValueHandle.ToString("X4"));
            row.SetCell(5, ch.IsBroadcastable.ToString());
            row.SetCell(6, ch.IsReadable.ToString());
            row.SetCell(7, ch.IsWritable.ToString());
            row.SetCell(8, ch.IsWritableWithoutResponse.ToString());
            row.SetCell(9, ch.IsSignedWritable.ToString());
            row.SetCell(10, ch.IsNotifiable.ToString());
            row.SetCell(11, ch.IsIndicatable.ToString());
            row.SetCell(12, ch.HasExtendedProperties.ToString());
        }
    }

    private void OnGetCharValueClicked()
    {
        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;
        byte[] value;
        Int32 res = _client.ReadCharacteristicValue(ch, OpFlag(), out value, Protection());
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Error: 0x" + res.ToString("X8")); return; }
        if (value == null) return;

        if (value.Length == 0) Log("Value is empty");
        else Log("Value: " + HexStr(value));
    }

    private void OnSetValueClicked()
    {
        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;

        string str = charValueInput.text;
        if (str.Length % 2 != 0) str = "0" + str;

        byte[] val = new byte[str.Length / 2];
        try
        {
            for (int i = 0; i < val.Length; i++)
                val[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
        }
        catch (Exception e) { Log("Invalid hex: " + e.Message); return; }

        wclGattWriteKind wk;
        switch (writeKindDropdown.value)
        {
            case 0: wk = wclGattWriteKind.wkWithResponse; break;
            case 1: wk = wclGattWriteKind.wkWithoutResponse; break;
            case 2: wk = wclGattWriteKind.wkAutoSplit; break;
            default: wk = wclGattWriteKind.wkAuto; break;
        }

        Int32 res = _client.WriteCharacteristicValue(ch, val, Protection(), wk);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnGetDescriptorsClicked()
    {
        _descriptors = null;
        ClearChildren(descriptorsContent);
        _selectedDescriptor = null;

        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;
        Int32 res = _client.ReadDescriptors(ch, OpFlag(), out _descriptors);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Error: 0x" + res.ToString("X8")); return; }
        if (_descriptors == null) return;

        foreach (var desc in _descriptors)
        {
            var rowGO = Instantiate(descriptorRowPrefab, descriptorsContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnDescriptorRowClicked);
            row.Tag = desc;
            row.SetCell(0, UuidToString(desc.Uuid));
            row.SetCell(1, desc.Uuid.IsShortUuid.ToString());
            row.SetCell(2, desc.ServiceHandle.ToString("X4"));
            row.SetCell(3, desc.CharacteristicHandle.ToString("X4"));
            row.SetCell(4, desc.Handle.ToString("X4"));
            row.SetCell(5, DescriptorTypeStr(desc.DescriptorType));
        }
    }

    private void OnGetDescValueClicked()
    {
        ClearChildren(descriptorValueContent);
        if (_selectedDescriptor == null) { Log("Select descriptor"); return; }

        var desc = (wclGattDescriptor)_selectedDescriptor.Tag;
        wclGattDescriptorValue value;
        Int32 res = _client.ReadDescriptorValue(desc, OpFlag(), out value, Protection());
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Error: 0x" + res.ToString("X8")); return; }

        switch (value.AType)
        {
            case wclGattDescriptorType.dtCharacteristicExtendedProperties:
                ListProp("Type", "dtCharacteristicExtendedProperties");
                ListProp("IsReliableWriteEnabled", value.CharacteristicExtendedProperties.IsReliableWriteEnabled.ToString());
                ListProp("IsAuxiliariesWritable", value.CharacteristicExtendedProperties.IsAuxiliariesWritable.ToString());
                break;

            case wclGattDescriptorType.dtCharacteristicUserDescription:
                ListProp("Type", "dtCharacteristicUserDescription");
                ListProp("Description", value.UserDescription.Description);
                break;

            case wclGattDescriptorType.dtClientCharacteristicConfiguration:
                ListProp("Type", "dtClientCharacteristicConfiguration");
                ListProp("IsSubscribeToNotification", value.ClientCharacteristicConfiguration.IsSubscribeToNotification.ToString());
                ListProp("IsSubscribeToIndication", value.ClientCharacteristicConfiguration.IsSubscribeToIndication.ToString());
                break;

            case wclGattDescriptorType.dtServerCharacteristicConfiguration:
                ListProp("Type", "dtServerCharacteristicConfiguration");
                ListProp("IsBroadcast", value.ServerCharacteristicConfiguration.IsBroadcast.ToString());
                break;

            case wclGattDescriptorType.dtCharacteristicFormat:
                ListProp("Type", "dtCharacteristicFormat");
                ListProp("Format", value.CharacteristicFormat.Format.ToString());
                ListProp("Exponent", value.CharacteristicFormat.Exponent.ToString("X2"));
                ListProp("Unit", value.CharacteristicFormat.AUnit.ToString("X4"));
                ListProp("NameSpace", value.CharacteristicFormat.NameSpace.ToString("X2"));
                ListProp("Description", value.CharacteristicFormat.Description.ToString("X4"));
                break;

            case wclGattDescriptorType.dtCharacteristicAggregateFormat:
                ListProp("Type", "dtCharacteristicAggregateFormat");
                if (value.CharacteristicAggregateFormat.Handles != null)
                    for (int i = 0; i < value.CharacteristicAggregateFormat.Handles.Length; i++)
                        ListProp("Handle [" + i + "]", value.CharacteristicAggregateFormat.Handles[i].ToString("X4"));
                break;

            default:
                ListProp("Type", "UNKNOWN");
                break;
        }

        if (value.Data != null)
            ListProp("Data", HexStr(value.Data));
    }

    private void OnSubscribeClicked()
    {
        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;
        var sk = SubscribeKindFromDropdown();

        if (sk == wclGattSubscribeKind.skManual && ch.IsNotifiable && ch.IsIndicatable)
            ch.IsIndicatable = false;

        Int32 res;
        if (fastSubscribeToggle.isOn)
        {
            res = (sk == wclGattSubscribeKind.skManual)
                ? _client.SubscribeForNotifications(ch)
                : _client.SubscribeForNotifications(ch, OpFlag(), Protection(), sk);
        }
        else
        {
            res = (sk == wclGattSubscribeKind.skManual)
                ? _client.Subscribe(ch)
                : _client.Subscribe(ch, sk);
        }

        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnUnsubscribeClicked()
    {
        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;
        var sk = SubscribeKindFromDropdown();

        if (sk == wclGattSubscribeKind.skManual && ch.IsNotifiable && ch.IsIndicatable)
            ch.IsIndicatable = false;

        Int32 res;
        if (fastSubscribeToggle.isOn)
        {
            res = (sk == wclGattSubscribeKind.skManual)
                ? _client.UnsubscribeFromNotifications(ch)
                : _client.UnsubscribeFromNotifications(ch, OpFlag(), Protection(), sk);
        }
        else
        {
            res = (sk == wclGattSubscribeKind.skManual)
                ? _client.Unsubscribe(ch)
                : _client.Unsubscribe(ch, sk);
        }

        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnWriteSubscribeClicked()
    {
        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;
        var sk = SubscribeKindFromDropdown();

        Int32 res;
        if (sk == wclGattSubscribeKind.skManual)
        {
            if (ch.IsNotifiable && ch.IsIndicatable) ch.IsIndicatable = false;
            res = _client.WriteClientConfiguration(ch, true, OpFlag(), Protection());
        }
        else
        {
            res = _client.WriteClientConfiguration(ch, true, OpFlag(), Protection(), sk);
        }

        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnWriteUnsubscribeClicked()
    {
        if (_selectedCharacteristic == null) { Log("Select characteristic"); return; }

        var ch = (wclGattCharacteristic)_selectedCharacteristic.Tag;
        var sk = SubscribeKindFromDropdown();

        Int32 res;
        if (sk == wclGattSubscribeKind.skManual)
        {
            if (ch.IsNotifiable && ch.IsIndicatable) ch.IsIndicatable = false;
            res = _client.WriteClientConfiguration(ch, false, OpFlag(), Protection());
        }
        else
        {
            res = _client.WriteClientConfiguration(ch, false, OpFlag(), Protection(), sk);
        }

        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnGetParamsClicked()
    {
        TraceEvent(_client.Address, "Connection params", "", "");
        GetConnectionParams();
    }

    private void OnSetParamsClicked()
    {
        Int32 res;
        if (paramsDropdown.value == 3)
        {
            var value = new wclBluetoothLeConnectionParametersValue
            {
                MinInterval = 84,
                MaxInterval = 84,
                Latency = 0,
                LinkTimeout = 800,
            };
            res = _client.SetConnectionParams(value);
        }
        else
        {
            wclBluetoothLeConnectionParametersType p;
            switch (paramsDropdown.value)
            {
                case 0: p = wclBluetoothLeConnectionParametersType.ppBalanced; break;
                case 1: p = wclBluetoothLeConnectionParametersType.ppPowerOptimized; break;
                case 2: p = wclBluetoothLeConnectionParametersType.ppThroughputOptimized; break;
                default: throw new wclEInvalidArgument("Invalid connection parameters.");
            }
            res = _client.SetConnectionParams(p);
        }
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnGetMaxPduSizeClicked()
    {
        TraceEvent(_client.Address, "Max PDU", "", "");
        GetMaxPduSize();
    }

    private void OnGetPhyClicked()
    {
        TraceEvent(_client.Address, "Connection PHY", "", "");
        GetConnectionPhy();
    }

    private void OnFastSubscribeChanged(bool value)
    {
        writeSubscribeButton.interactable = !value;
        writeUnsubscribeButton.interactable = !value;
    }

    // ==================================================================
    //  Row handlers
    // ==================================================================
    private void OnDeviceRowClicked(TableRowUI row)
    {
        if (_selectedDevice != null) _selectedDevice.SetSelected(false);
        _selectedDevice = row;
        if (_selectedDevice != null) _selectedDevice.SetSelected(true);
    }

    private void OnServiceRowClicked(TableRowUI row)
    {
        if (_selectedService != null) _selectedService.SetSelected(false);
        _selectedService = row;
        if (_selectedService != null) _selectedService.SetSelected(true);
    }

    private void OnCharacteristicRowClicked(TableRowUI row)
    {
        if (_selectedCharacteristic != null) _selectedCharacteristic.SetSelected(false);
        _selectedCharacteristic = row;
        if (_selectedCharacteristic != null) _selectedCharacteristic.SetSelected(true);
    }

    private void OnDescriptorRowClicked(TableRowUI row)
    {
        if (_selectedDescriptor != null) _selectedDescriptor.SetSelected(false);
        _selectedDescriptor = row;
        if (_selectedDescriptor != null) _selectedDescriptor.SetSelected(true);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio radio;
        Int32 res = _manager.GetLeRadio(out radio);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get working radio failed: 0x" + res.ToString("X8"));
            return null;
        }
        return radio;
    }

    private wclGattOperationFlag OpFlag()
    {
        switch (opFlagDropdown.value)
        {
            case 1: return wclGattOperationFlag.goReadFromDevice;
            case 2: return wclGattOperationFlag.goReadFromCache;
            default: return wclGattOperationFlag.goNone;
        }
    }

    private wclGattProtectionLevel Protection()
    {
        switch (protectionDropdown.value)
        {
            case 0: return wclGattProtectionLevel.plNone;
            case 1: return wclGattProtectionLevel.plAuthentication;
            case 2: return wclGattProtectionLevel.plEncryption;
            case 3: return wclGattProtectionLevel.plEncryptionAndAuthentication;
            default: return wclGattProtectionLevel.plNone;
        }
    }

    private wclGattSubscribeKind SubscribeKindFromDropdown()
    {
        switch (subscribeKindDropdown.value)
        {
            case 0: return wclGattSubscribeKind.skNotification;
            case 1: return wclGattSubscribeKind.skIndication;
            default: return wclGattSubscribeKind.skManual;
        }
    }

    private void GetMaxPduSize()
    {
        ushort size;
        Int32 res = _client.GetMaxPduSize(out size);
        TraceEvent(_client.Address, "", "Error", res != wclErrors.WCL_E_SUCCESS ? res.ToString("X8") : "");
        if (res == wclErrors.WCL_E_SUCCESS)
            TraceEvent(_client.Address, "", "Size", size.ToString());
    }

    private void GetConnectionParams()
    {
        wclBluetoothLeConnectionParameters p;
        Int32 res = _client.GetConnectionParams(out p);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            TraceEvent(0, "", "Error", res.ToString("X8"));
            return;
        }
        TraceEvent(0, "", "Connection interval", p.Interval.ToString());
        TraceEvent(0, "", "Connection latency", p.Latency.ToString());
        TraceEvent(0, "", "Link timeout", p.LinkTimeout.ToString());
    }

    private void GetConnectionPhy()
    {
        wclBluetoothLeConnectionPhy phy;
        Int32 res = _client.GetConnectionPhyInfo(out phy);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            TraceEvent(0, "", "Error", res.ToString("X8"));
            return;
        }

        TraceEvent(0, "Transmit PHY", "", "");
        TraceEvent(0, "", "Coded", phy.Transmit.IsCoded.ToString());
        TraceEvent(0, "", "1M", phy.Transmit.IsUncoded1MPhy.ToString());
        TraceEvent(0, "", "2M", phy.Transmit.IsUncoded2MPhy.ToString());

        TraceEvent(0, "Receive PHY", "", "");
        TraceEvent(0, "", "Coded", phy.Receive.IsCoded.ToString());
        TraceEvent(0, "", "1M", phy.Receive.IsUncoded1MPhy.ToString());
        TraceEvent(0, "", "2M", phy.Receive.IsUncoded2MPhy.ToString());
    }

    private static string UuidToString(wclGattUuid uuid)
        => uuid.IsShortUuid ? uuid.ShortUuid.ToString("X4") : uuid.LongUuid.ToString();

    private static string DeviceTypeStr(wclBluetoothDeviceType t)
    {
        switch (t)
        {
            case wclBluetoothDeviceType.dtClassic: return "Classic";
            case wclBluetoothDeviceType.dtBle:     return "BLE";
            case wclBluetoothDeviceType.dtMixed:   return "Mixed";
            default:                                return "Unknown";
        }
    }

    private static string DescriptorTypeStr(wclGattDescriptorType t)
    {
        switch (t)
        {
            case wclGattDescriptorType.dtCharacteristicExtendedProperties:  return "dtCharacteristicExtendedProperties";
            case wclGattDescriptorType.dtCharacteristicUserDescription:     return "dtCharacteristicUserDescription";
            case wclGattDescriptorType.dtClientCharacteristicConfiguration: return "dtClientCharacteristicConfiguration";
            case wclGattDescriptorType.dtServerCharacteristicConfiguration: return "dtServerCharacteristicConfiguration";
            case wclGattDescriptorType.dtCharacteristicFormat:              return "dtCharacteristicFormat";
            case wclGattDescriptorType.dtCharacteristicAggregateFormat:     return "dtCharacteristicAggregateFormat";
            default:                                                        return "dtCustomDescriptor";
        }
    }

    private static string HexStr(byte[] data)
    {
        if (data == null || data.Length == 0) return "";
        string s = "";
        for (int i = 0; i < data.Length; i++) s += data[i].ToString("X2");
        return s;
    }

    private void ListProp(string name, string value)
    {
        if (descriptorValueRowPrefab == null) return;
        var go = Instantiate(descriptorValueRowPrefab, descriptorValueContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, name);
        row.SetCell(1, value);
    }

    private void Cleanup()
    {
        _services = null;
        _characteristics = null;
        _descriptors = null;

        ClearChildren(servicesContent);
        ClearChildren(characteristicsContent);
        ClearChildren(descriptorsContent);
        ClearChildren(descriptorValueContent);

        _selectedService = null;
        _selectedCharacteristic = null;
        _selectedDescriptor = null;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // ==================================================================
    //  Events log
    // ==================================================================
    private void TraceEvent(long address, string evt, string param, string value)
    {
        if (eventsContent == null || eventRowPrefab == null) return;

        string addrStr = address != 0 ? address.ToString("X12") : "";

        var go = Instantiate(eventRowPrefab, eventsContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, addrStr);
        row.SetCell(1, evt);
        row.SetCell(2, param);
        row.SetCell(3, value);

        var sr = eventsContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }

    private void Log(string msg)
    {
        Debug.Log("[GattClient] " + msg);
        TraceEvent(0, "LOG", "", msg);
    }
}