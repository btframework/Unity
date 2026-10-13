using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;

public class BluetoothManagerDemo : MonoBehaviour
{
    // ---------- Top bar ----------
    public Button    openButton;
    public Button    closeButton;
    public Toggle    handlePairingToggle;
    public Button    refreshRadioButton;
    public Button    setConnectableButton;
    public Button    setDiscoverableButton;
    public TMP_InputField nameInput;
    public Button    setNameButton;
    public Button    turnOnButton;
    public Button    turnOffButton;
    public Toggle    dontTurnOffToggle;

    // ---------- Radios ----------
    public Transform  radiosContent;
    public GameObject radioRowPrefab;

    // ---------- Radio actions ----------
    public Button       discoverButton;
    public TMP_InputField timeoutInput;
    public TMP_Dropdown discoveringModeDropdown;
    public Button       terminateButton;
    public Button       enumPairedButton;
    public Button       rssiButton;
    public Button       isInRangeButton;
    public Button       refreshDeviceButton;
    public Button       enumConnectedButton;

    // ---------- Pairing ----------
    public Button       pairButton;
    public TMP_InputField pinInput;
    public Button       unpairButton;
    public TMP_Dropdown mitmDropdown;
    public TMP_Dropdown ioCapDropdown;
    public TMP_Dropdown bleProtectionDropdown;
    public TMP_Dropdown pairingMethodDropdown;

    // ---------- Devices ----------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------- Services + vCOMs + Install ----------
    public Button       enumServicesButton;
    public Button       enumInstalledServicesButton;
    public Button       createVcomButton;
    public Button       destroyVcomButton;
    public Button       enumVcomsButton;
    public Toggle       enumPreinstalledToggle;
    public Button       installDeviceButton;
    public Button       uninstallDeviceButton;
    public Button       disconnectButton;

    public Transform  servicesContent;
    public GameObject serviceRowPrefab;

    public Transform  comPortsContent;
    public GameObject comPortRowPrefab;

    // ---------- Events log ----------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;
    public Button     clearEventsButton;

    // ---------- Confirm dialog ----------
    public GameObject confirmPanel;
    public TMP_Text   confirmText;
    public Button     confirmYesButton;
    public Button     confirmNoButton;
    public Button     confirmCancelButton;

    // ------------------------------------------------------------------
    private wclBluetoothManager    _manager;
    private wclPowerEventsMonitor  _powerMonitor;

    private readonly List<TableRowUI> _radioRows    = new List<TableRowUI>();
    private readonly List<TableRowUI> _deviceRows   = new List<TableRowUI>();
    private readonly List<TableRowUI> _serviceRows  = new List<TableRowUI>();
    private readonly List<TableRowUI> _comPortRows  = new List<TableRowUI>();

    private TableRowUI _selectedRadio;
    private TableRowUI _selectedDevice;
    private TableRowUI _selectedService;
    private TableRowUI _selectedComPort;

    private bool _wasOpened;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        openButton            .onClick.AddListener(OnOpenClicked);
        closeButton           .onClick.AddListener(OnCloseClicked);
        refreshRadioButton    .onClick.AddListener(OnRefreshRadioClicked);
        setConnectableButton  .onClick.AddListener(() => ShowSetBoolDialog(true));
        setDiscoverableButton .onClick.AddListener(() => ShowSetBoolDialog(false));
        setNameButton         .onClick.AddListener(OnSetNameClicked);
        turnOnButton          .onClick.AddListener(OnTurnOnClicked);
        turnOffButton         .onClick.AddListener(OnTurnOffClicked);

        discoverButton        .onClick.AddListener(OnDiscoverClicked);
        terminateButton       .onClick.AddListener(OnTerminateClicked);
        enumPairedButton      .onClick.AddListener(OnEnumPairedClicked);
        rssiButton            .onClick.AddListener(OnRssiClicked);
        isInRangeButton       .onClick.AddListener(OnIsInRangeClicked);
        refreshDeviceButton   .onClick.AddListener(OnRefreshDeviceClicked);
        enumConnectedButton   .onClick.AddListener(OnEnumConnectedClicked);

        pairButton            .onClick.AddListener(OnPairClicked);
        unpairButton          .onClick.AddListener(OnUnpairClicked);

        enumServicesButton          .onClick.AddListener(OnEnumServicesClicked);
        enumInstalledServicesButton .onClick.AddListener(OnEnumInstalledServicesClicked);
        createVcomButton            .onClick.AddListener(OnCreateVcomClicked);
        destroyVcomButton           .onClick.AddListener(OnDestroyVcomClicked);
        enumVcomsButton             .onClick.AddListener(OnEnumVcomsClicked);
        installDeviceButton         .onClick.AddListener(OnInstallDeviceClicked);
        uninstallDeviceButton       .onClick.AddListener(OnUninstallDeviceClicked);
        disconnectButton            .onClick.AddListener(OnDisconnectClicked);

        clearEventsButton.onClick.AddListener(() => ClearChildren(eventsContent));

        confirmYesButton   .onClick.AddListener(() => ConfirmResult(1));
        confirmNoButton    .onClick.AddListener(() => ConfirmResult(2));
        confirmCancelButton.onClick.AddListener(() => ConfirmResult(0));
        confirmPanel.SetActive(false);
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();
        _manager.AfterOpen                    += ManagerAfterOpen;
        _manager.BeforeClose                  += ManagerBeforeClose;
        _manager.OnAuthenticationCompleted    += ManagerOnAuthenticationCompleted;
        _manager.OnDeviceFound                += ManagerOnDeviceFound;
        _manager.OnDiscoveringCompleted       += ManagerOnDiscoveringCompleted;
        _manager.OnDiscoveringStarted         += ManagerOnDiscoveringStarted;
        _manager.OnNumericComparison          += ManagerOnNumericComparison;
        _manager.OnPasskeyNotification        += ManagerOnPasskeyNotification;
        _manager.OnPasskeyRequest             += ManagerOnPasskeyRequest;
        _manager.OnPinRequest                 += ManagerOnPinRequest;
        _manager.OnStatusChanged              += ManagerOnStatusChanged;
        _manager.OnConfirm                    += ManagerOnConfirm;
        _manager.OnIoCapabilityRequest        += ManagerOnIoCapabilityRequest;
        _manager.OnOobDataRequest             += ManagerOnOobDataRequest;
        _manager.OnProtectionLevelRequest     += ManagerOnProtectionLevelRequest;
        _manager.OnClosed                     += ManagerOnClosed;

        _powerMonitor = new wclPowerEventsMonitor();
        _powerMonitor.OnPowerStateChanged += PowerMonitorOnPowerStateChanged;
        _powerMonitor.Open();

        discoveringModeDropdown.value    = 0;
        mitmDropdown.value               = 3;
        ioCapDropdown.value              = 1;
        bleProtectionDropdown.value      = 0;
        pairingMethodDropdown.value      = 0;

        timeoutInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        timeoutInput.text = "10";
    }

    private void OnDestroy()
    {
        if (_manager == null) return;

        try { _manager.Close(); }
        catch (Exception e) { Debug.LogException(e); }
        _manager = null;

        try { _powerMonitor?.Close(); }
        catch (Exception e) { Debug.LogException(e); }
        _powerMonitor = null;
    }

    // ==================================================================
    //  Simple modal confirm (замена MessageBox Yes/No/Cancel)
    // ==================================================================
    private Action<int> _confirmCallback;

    private void ShowConfirm(string message, Action<int> callback)
    {
        _confirmCallback = callback;
        confirmText.text = message;
        confirmPanel.SetActive(true);
    }

    private void ConfirmResult(int result)
    {
        confirmPanel.SetActive(false);
        var cb = _confirmCallback;
        _confirmCallback = null;
        cb?.Invoke(result); // 0 = cancel, 1 = yes, 2 = no
    }

    private void ShowSetBoolDialog(bool connectable)
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }

        string title = connectable ? "Set Connectable" : "Set Discoverable";
        string what  = connectable ? "connectable" : "discoverable";

        ShowConfirm($"{title}\n[YES] - turn {what} ON, [NO] - turn {what} OFF",
            result =>
            {
                if (result == 0) return;
                var radio = _selectedRadio.Tag as wclBluetoothRadio;
                if (radio == null) return;

                Int32 res = connectable
                    ? radio.SetConnectable(result == 1)
                    : radio.SetDiscoverable(result == 1);

                if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
                RefreshRadio(_selectedRadio);
            });
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void ManagerAfterOpen(object s, EventArgs e)
        => TraceEvent(null, "AfterOpen", "Handle pairing", _manager.HandlePairing.ToString());

    private void ManagerBeforeClose(object s, EventArgs e)
        => TraceEvent(null, "BeforeClose", "", "");

    private void ManagerOnClosed(object s, EventArgs e)
    {
        ClearRadios();
        TraceEvent(null, "Closed", "", "");
    }

    private void ManagerOnAuthenticationCompleted(object s, wclBluetoothRadio radio, long addr, int error)
    {
        TraceEvent(radio, "AuthenticationCompleted", "Address", addr.ToString("X12"));
        TraceEvent(null, "", "Error", "0x" + error.ToString("X8"));
    }

    private void ManagerOnDiscoveringStarted(object s, wclBluetoothRadio radio)
    {
        ClearDevices();
        TraceEvent(radio, "DiscoveringStarted", "", "");
    }

    private void ManagerOnDiscoveringCompleted(object s, wclBluetoothRadio radio, int error)
    {
        TraceEvent(radio, "DiscoveringCompleted", "Error", "0x" + error.ToString("X8"));
        RefreshDevices();
    }

    private void ManagerOnDeviceFound(object s, wclBluetoothRadio radio, long addr)
    {
        TraceEvent(radio, "DeviceFound", "Address", addr.ToString("X12"));
        AddDevice(radio, addr);
    }

    private void ManagerOnConfirm(object s, wclBluetoothRadio radio, long addr, out bool confirm)
    {
        confirm = true;
        TraceEvent(radio, "Just Works pairing", "Address", addr.ToString("X12"));
    }

    private void ManagerOnPinRequest(object s, wclBluetoothRadio radio, long addr, out string pin)
    {
        pin = pinInput.text;
        TraceEvent(radio, "PinRequest", "Address", addr.ToString("X12"));
        TraceEvent(null, "", "PIN", pin);
    }

    private void ManagerOnPasskeyRequest(object s, wclBluetoothRadio radio, long addr, out uint passkey)
    {
        passkey = 123456;
        TraceEvent(radio, "PasskeyRequest", "Address", addr.ToString());
        TraceEvent(null, "", "Passkey", passkey.ToString());
    }

    private void ManagerOnPasskeyNotification(object s, wclBluetoothRadio radio, long addr, uint passkey)
    {
        TraceEvent(radio, "PasskeyNotification", "Address", addr.ToString("X12"));
        TraceEvent(null, "", "Passkey", passkey.ToString());
    }

    private void ManagerOnNumericComparison(object s, wclBluetoothRadio radio, long addr,
        uint number, out bool confirm)
    {
        confirm = true;
        TraceEvent(radio, "NumericComparison", "Address", addr.ToString("X12"));
        TraceEvent(null, "", "Number", number.ToString());
        TraceEvent(null, "", "Confirm", confirm.ToString());
    }

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

    private void ManagerOnOobDataRequest(object s, wclBluetoothRadio radio, long addr,
        out wclBluetoothOobData oobData)
    {
        oobData = null;
    }

    private void ManagerOnProtectionLevelRequest(object s, wclBluetoothRadio radio, long addr,
        out wclBluetoothLeProtectionLevel protection)
    {
        switch (bleProtectionDropdown.value)
        {
            case 0: protection = wclBluetoothLeProtectionLevel.pplDefault; break;
            case 1: protection = wclBluetoothLeProtectionLevel.pplNone; break;
            case 2: protection = wclBluetoothLeProtectionLevel.pplEncryption; break;
            case 3: protection = wclBluetoothLeProtectionLevel.pplEncryptionAndAuthentication; break;
            default: protection = wclBluetoothLeProtectionLevel.pplDefault; break;
        }
    }

    private void ManagerOnStatusChanged(object s, wclBluetoothRadio radio)
    {
        string status = radio.Available
            ? "Available"
            : (radio.Plugged ? "Not available, Plugged" : "Unplugged");
        TraceEvent(radio, "StatusChanged", "New status", status);

        TableRowUI found = null;
        foreach (var row in _radioRows)
        {
            if (ReferenceEquals(row.Tag, radio)) { found = row; break; }
        }

        if (found == null)
        {
            var rowGO = Instantiate(radioRowPrefab, radiosContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnRadioRowClicked);
            row.Tag = radio;
            _radioRows.Add(row);
            found = row;
        }

        RefreshRadio(found);

        if (dontTurnOffToggle.isOn && radio.Plugged && !radio.Available)
        {
            // В WinForms-версии была пауза 200 мс и повтор. В Unity Thread.Sleep
            // на главном потоке нельзя, поэтому просто повторяем один раз.
            Int32 res = radio.TurnOn();
            if (res != wclErrors.WCL_E_SUCCESS)
                TraceEvent(radio, "Turn ON", "Error", res.ToString("X8"));
        }
    }

    private void PowerMonitorOnPowerStateChanged(object s, wclPowerState state)
    {
        switch (state)
        {
            case wclPowerState.psResumeAutomatic:
                TraceEvent(null, "Power", "State", "psResumeAutomatic");
                if (_wasOpened) { _wasOpened = false; _manager.Open(); }
                break;
            case wclPowerState.psResume:
                TraceEvent(null, "Power", "State", "psResume");
                break;
            case wclPowerState.psSuspend:
                TraceEvent(null, "Power", "State", "psSuspend");
                if (_manager.Active) { _wasOpened = true; _manager.Close(); }
                break;
            case wclPowerState.psPowerStatusChanged:
                TraceEvent(null, "Power", "State", "psPowerStatusChanged");
                break;
            case wclPowerState.psUnknown:
                TraceEvent(null, "Power", "State", "psUnknown");
                break;
        }
    }

    // ==================================================================
    //  Button handlers
    // ==================================================================
    private void OnOpenClicked()
    {
        Int32 res = _manager.Open(handlePairingToggle.isOn);
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
    }

    private void OnCloseClicked()
    {
        Int32 res = _manager.Close();
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
    }

    private void OnRefreshRadioClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        RefreshRadio(_selectedRadio);
    }

    private void OnSetNameClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        Int32 res = radio.SetName(nameInput.text);
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
        RefreshRadio(_selectedRadio);
    }

    private void OnTurnOnClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        Int32 res = radio.TurnOn();
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
        RefreshRadio(_selectedRadio);
    }

    private void OnTurnOffClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        Int32 res = radio.TurnOff();
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
        RefreshRadio(_selectedRadio);
    }

    private void OnDiscoverClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        var kind = (discoveringModeDropdown.value == 0)
            ? wclBluetoothDiscoverKind.dkClassic
            : wclBluetoothDiscoverKind.dkBle;

        byte timeout;
        byte.TryParse(timeoutInput.text, out timeout);

        Int32 res = radio.Discover(timeout, kind);
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
    }

    private void OnTerminateClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        Int32 res = radio.Terminate();
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
    }

    private void OnEnumPairedClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        ClearDevices();

        long[] devices;
        Int32 res = radio.EnumPairedDevices(out devices);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }

        if (devices != null)
        {
            foreach (var addr in devices) AddDevice(radio, addr);
            RefreshDevices();
        }
    }

    private void OnEnumConnectedClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        ClearDevices();

        long[] devices;
        Int32 res = radio.EnumConnectedDevices(out devices);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }

        if (devices != null)
        {
            foreach (var addr in devices) AddDevice(radio, addr);
            RefreshDevices();
        }
    }

    private void OnRssiClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        sbyte rssi;
        Int32 res = radio.GetRemoteRssi(addr, out rssi);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }
        Log("RSSI: " + rssi.ToString() + "dB");
    }

    private void OnIsInRangeClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        bool inRange;
        Int32 res = radio.IsRemoteDeviceInRange(addr, out inRange);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }

        Log(inRange ? "Device is in range" : "Device is NOT in range");
    }

    private void OnRefreshDeviceClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        RefreshDevice(_selectedDevice);
    }

    private void OnPairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        var method = PairingMethodFromDropdown();

        Int32 res = radio.RemotePair(addr, method);
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
    }

    private void OnUnpairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        var method = PairingMethodFromDropdown();

        Int32 res = radio.RemoteUnpair(addr, method);
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
        RefreshDevice(_selectedDevice);
    }

    private void OnDisconnectClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        Int32 res = radio.RemoteDisconnect(addr);
        if (res != wclErrors.WCL_E_SUCCESS) ShowError(res);
    }

    private void OnEnumServicesClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        ClearServices();

        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);

        wclBluetoothService[] services;
        Guid g = Guid.Empty;
        Int32 res = radio.EnumRemoteServices(addr, g, out services);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }
        if (services == null) return;

        foreach (var s in services)
        {
            var rowGO = Instantiate(serviceRowPrefab, servicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnServiceRowClicked);
            row.SetCell(0, radio.ApiName);
            row.SetCell(1, addr.ToString("X12"));
            row.SetCell(2, s.Handle.ToString("X8"));
            row.SetCell(3, s.Uuid.ToString());
            row.SetCell(4, s.Channel.ToString());
            row.SetCell(5, s.Name);
            row.SetCell(6, s.Comment);
            _serviceRows.Add(row);
        }
    }

    private void OnEnumInstalledServicesClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        ClearServices();

        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);

        Guid[] services;
        Int32 res = radio.EnumInstalledServices(addr, out services);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }
        if (services == null) return;

        foreach (var s in services)
        {
            var rowGO = Instantiate(serviceRowPrefab, servicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnServiceRowClicked);
            row.SetCell(0, radio.ApiName);
            row.SetCell(1, addr.ToString("X12"));
            row.SetCell(3, s.ToString());
            _serviceRows.Add(row);
        }
    }

    private void OnCreateVcomClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        if (_selectedService == null) { Log("Select service"); return; }

        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        Guid service = new Guid(_selectedService.GetCell(3));

        ushort number;
        Int32 res = radio.CreateComPort(addr, service, out number);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }

        TraceEvent(radio, "Create vCOM", "Number", number.ToString());
        RefreshVcoms();
    }

    private void OnDestroyVcomClicked()
    {
        if (_selectedComPort == null) { Log("Select vCOM"); return; }
        var radio = _selectedComPort.Tag as wclBluetoothRadio;
        if (radio == null) return;

        ushort number = Convert.ToUInt16(_selectedComPort.GetCell(3));
        Int32 res = radio.DestroyComPort(number);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }

        TraceEvent(radio, "Destroy vCOM", "Number", number.ToString());
        RefreshVcoms();
    }

    private void OnEnumVcomsClicked()
    {
        if (_selectedRadio == null) { Log("Select radio"); return; }
        RefreshVcoms();
    }

    private void OnInstallDeviceClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        if (_selectedService == null) { Log("Select service"); return; }

        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        Guid service = new Guid(_selectedService.GetCell(3));

        Int32 res = radio.InstallDevice(addr, service);
        if (res == wclBluetoothErrors.WCL_E_BLUETOOTH_DEVICE_ALREADY_INSTALLED)
        {
            radio.UninstallDevice(addr, service);
            res = radio.InstallDevice(addr, service);
        }

        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }
        Log("Device installed");
    }

    private void OnUninstallDeviceClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        if (_selectedService == null) { Log("Select service"); return; }

        var radio = _selectedDevice.Tag as wclBluetoothRadio;
        if (radio == null) return;
        long addr = Convert.ToInt64(_selectedDevice.GetCell(1), 16);
        Guid service = new Guid(_selectedService.GetCell(3));

        Int32 res = radio.UninstallDevice(addr, service);
        if (res != wclErrors.WCL_E_SUCCESS) { ShowError(res); return; }
        Log("Device uninstalled");
    }

    // ==================================================================
    //  Row refresh helpers
    // ==================================================================
    private void RefreshRadio(TableRowUI row)
    {
        var radio = row.Tag as wclBluetoothRadio;
        if (radio == null) return;

        row.SetCell(0, radio.ApiName);
        row.SetCell(1, radio.Available.ToString());

        long addr;
        Int32 res = radio.GetAddress(out addr);
        row.SetCell(2, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : addr.ToString("X12"));

        bool b;
        res = radio.GetConnectable(out b);
        row.SetCell(3, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : b.ToString());

        res = radio.GetDiscoverable(out b);
        row.SetCell(4, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : b.ToString());

        byte bb; ushort ww;
        res = radio.GetHciVersion(out bb, out ww);
        row.SetCell(5, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : bb + "." + ww);

        res = radio.GetLmpVersion(out bb, out ww);
        row.SetCell(6, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : bb + "." + ww);

        res = radio.GetManufacturer(out ww);
        row.SetCell(7, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : ww.ToString());

        string s;
        res = radio.GetName(out s);
        row.SetCell(8, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : s);

        uint cod;
        res = radio.GetCod(out cod);
        row.SetCell(9, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : "0x" + cod.ToString("X8"));
    }

    private void RefreshDevice(TableRowUI row)
    {
        var radio = row.Tag as wclBluetoothRadio;
        if (radio == null) return;

        long addr;
        try { addr = Convert.ToInt64(row.GetCell(1), 16); } catch { return; }

        uint cod;
        Int32 res = radio.GetRemoteCod(addr, out cod);
        row.SetCell(2, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : cod.ToString("X8"));

        wclBluetoothDeviceType dt;
        res = radio.GetRemoteDeviceType(addr, out dt);
        if (res != wclErrors.WCL_E_SUCCESS) row.SetCell(3, ErrStr(res));
        else
        {
            switch (dt)
            {
                case wclBluetoothDeviceType.dtClassic: row.SetCell(3, "Classic"); break;
                case wclBluetoothDeviceType.dtBle:     row.SetCell(3, "BLE"); break;
                case wclBluetoothDeviceType.dtMixed:   row.SetCell(3, "Mixed"); break;
                default:                                row.SetCell(3, "Unknown"); break;
            }
        }

        string devName;
        res = radio.GetRemoteName(addr, out devName);
        row.SetCell(4, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : devName);

        bool bb;
        res = radio.GetRemotePaired(addr, out bb);
        row.SetCell(5, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : bb.ToString());

        res = radio.GetRemoteConnectedStatus(addr, out bb);
        row.SetCell(6, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : bb.ToString());

        wclBluetoothAddressType at;
        res = radio.GetRemoteAddressType(addr, out at);
        if (res != wclErrors.WCL_E_SUCCESS) row.SetCell(7, ErrStr(res));
        else
        {
            switch (at)
            {
                case wclBluetoothAddressType.atClassic:     row.SetCell(7, "Classic"); break;
                case wclBluetoothAddressType.atPublic:      row.SetCell(7, "LE Public"); break;
                case wclBluetoothAddressType.atRandom:      row.SetCell(7, "LE Random"); break;
                case wclBluetoothAddressType.atUnspecified: row.SetCell(7, "Unspecified"); break;
                default:                                    row.SetCell(7, "Unknown"); break;
            }
        }
    }

    private void RefreshDevices()
    {
        foreach (var row in _deviceRows) RefreshDevice(row);
    }

    private void RefreshVcoms()
    {
        ClearChildren(comPortsContent);
        _comPortRows.Clear();
        _selectedComPort = null;

        if (_selectedRadio == null) return;
        var radio = _selectedRadio.Tag as wclBluetoothRadio;
        if (radio == null) return;

        Int32 res;
        wclVirtualComPort[] ports;
        if (enumPreinstalledToggle.isOn)
        {
            res = radio.EnumComPorts(out ports);
        }
        else
        {
            ports = radio.ComPorts;
            res = wclErrors.WCL_E_SUCCESS;
        }

        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Error enumerating vCOMs: 0x" + res.ToString("X8"));
            return;
        }
        if (ports == null) return;

        foreach (var p in ports)
        {
            var rowGO = Instantiate(comPortRowPrefab, comPortsContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnComPortRowClicked);
            row.SetCell(0, radio.ApiName);
            row.SetCell(1, p.Address.ToString("X12"));
            row.SetCell(2, p.Service.ToString());
            row.SetCell(3, p.Number.ToString());
            row.Tag = radio;
            _comPortRows.Add(row);
        }
    }

    // ==================================================================
    //  Row selection
    // ==================================================================
    private void OnRadioRowClicked(TableRowUI row)
    {
        if (_selectedRadio != null) _selectedRadio.SetSelected(false);
        _selectedRadio = row;
        if (_selectedRadio != null) _selectedRadio.SetSelected(true);
    }

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

    private void OnComPortRowClicked(TableRowUI row)
    {
        if (_selectedComPort != null) _selectedComPort.SetSelected(false);
        _selectedComPort = row;
        if (_selectedComPort != null) _selectedComPort.SetSelected(true);
    }

    // ==================================================================
    //  Clearing
    // ==================================================================
    private void ClearRadios()
    {
        ClearChildren(radiosContent);
        _radioRows.Clear();
        _selectedRadio = null;
        ClearDevices();
    }

    private void ClearDevices()
    {
        ClearChildren(devicesContent);
        _deviceRows.Clear();
        _selectedDevice = null;
        ClearServices();
    }

    private void ClearServices()
    {
        ClearChildren(servicesContent);
        _serviceRows.Clear();
        _selectedService = null;
        ClearChildren(comPortsContent);
        _comPortRows.Clear();
        _selectedComPort = null;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // ==================================================================
    //  Devices
    // ==================================================================
    private void AddDevice(wclBluetoothRadio radio, long addr)
    {
        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.Tag = radio;
        row.SetCell(0, radio.ApiName);
        row.SetCell(1, addr.ToString("X12"));
        _deviceRows.Add(row);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private wclBluetoothPairingMethod PairingMethodFromDropdown()
    {
        switch (pairingMethodDropdown.value)
        {
            case 1:  return wclBluetoothPairingMethod.pmClassic;
            case 2:  return wclBluetoothPairingMethod.pmLe;
            default: return wclBluetoothPairingMethod.pmAuto;
        }
    }

    private static string ErrStr(int err) => "Error: 0x" + err.ToString("X8");

    private void ShowError(int error) => Log("Error: 0x" + error.ToString("X8"));

    // ==================================================================
    //  Event log
    // ==================================================================
    private void TraceEvent(wclBluetoothRadio radio, string evt, string parameter, string value)
    {
        string driver = radio != null ? radio.ApiName : "";

        var rowGO = Instantiate(eventRowPrefab, eventsContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, driver);
        row.SetCell(1, evt);
        row.SetCell(2, parameter);
        row.SetCell(3, value);

        // Автоскролл
        var sr = eventsContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }

    private void Log(string msg)
    {
        Debug.Log("[BT Manager] " + msg);
        TraceEvent(null, "LOG", "", msg);
    }
}