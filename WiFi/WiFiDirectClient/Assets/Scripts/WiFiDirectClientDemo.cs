using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiDirectClientDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button discoverButton;
    public Button terminateButton;
    public Button enumPairedButton;
    public Button isPairedButton;
    public Button unpairButton;

    // ---------------- Devices table ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------------- Pairing params ----------------
    public TMP_Text       groupOwnerIntentLabel;
    public TMP_InputField groupOwnerIntentInput;

    public TMP_Text     pairingProcedureLabel;
    public TMP_Dropdown pairingProcedureDropdown;

    public Toggle displayPinToggle;
    public Toggle providePinToggle;
    public Toggle pushButtonToggle;

    // ---------------- Connect / Disconnect / Clear ----------------
    public Button connectButton;
    public Button disconnectButton;
    public Button clearLogButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiDirectDeviceWatcher _watcher;
    private wclWiFiDirectClient        _client;

    private TableRowUI _selectedDevice;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        discoverButton   .onClick.AddListener(OnDiscoverClicked);
        terminateButton  .onClick.AddListener(OnTerminateClicked);
        enumPairedButton .onClick.AddListener(OnEnumPairedClicked);
        isPairedButton   .onClick.AddListener(OnIsPairedClicked);
        unpairButton     .onClick.AddListener(OnUnpairClicked);
        connectButton    .onClick.AddListener(OnConnectClicked);
        disconnectButton .onClick.AddListener(OnDisconnectClicked);
        clearLogButton   .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        EnsureDropdown(pairingProcedureDropdown,
            new[] { "ppInvitation", "ppGroupOwnerNegotiation" }, 0);

        pushButtonToggle.isOn = true;
        groupOwnerIntentInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        groupOwnerIntentInput.text = "14";

        _watcher = new wclWiFiDirectDeviceWatcher();
        _watcher.OnDeviceFound        += WatcherOnDeviceFound;
        _watcher.OnDiscoveringCompleted += WatcherOnDiscoveringCompleted;
        _watcher.OnDiscoveringStarted += WatcherOnDiscoveringStarted;

        _client = new wclWiFiDirectClient();
        _client.OnPairCompleted      += ClientOnPairCompleted;
        _client.OnPairConfirm        += ClientOnPairConfirm;
        _client.OnPairDisplayPin     += ClientOnPairDisplayPin;
        _client.OnPairGetParams      += ClientOnPairGetParams;
        _client.OnPairProvidePin     += ClientOnPairProvidePin;
        _client.OnDeviceConnected    += ClientOnDeviceConnected;
        _client.OnDeviceDisconnected += ClientOnDeviceDisconnected;
    }

    private void OnDestroy()
    {
        try { _client?.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
        try { _watcher?.Terminate(); } catch (Exception e) { Debug.LogException(e); }

        if (_watcher != null)
        {
            _watcher.OnDeviceFound          -= WatcherOnDeviceFound;
            _watcher.OnDiscoveringCompleted -= WatcherOnDiscoveringCompleted;
            _watcher.OnDiscoveringStarted   -= WatcherOnDiscoveringStarted;
        }

        if (_client != null)
        {
            _client.OnPairCompleted      -= ClientOnPairCompleted;
            _client.OnPairConfirm        -= ClientOnPairConfirm;
            _client.OnPairDisplayPin     -= ClientOnPairDisplayPin;
            _client.OnPairGetParams      -= ClientOnPairGetParams;
            _client.OnPairProvidePin     -= ClientOnPairProvidePin;
            _client.OnDeviceConnected    -= ClientOnDeviceConnected;
            _client.OnDeviceDisconnected -= ClientOnDeviceDisconnected;
        }

        _watcher = null;
        _client = null;
    }

    // ==================================================================
    //  Watcher events
    // ==================================================================
    private void WatcherOnDiscoveringStarted(object s, EventArgs e)
    {
        ClearChildren(devicesContent);
        _selectedDevice = null;
        Log("Discovering started");
    }

    private void WatcherOnDiscoveringCompleted(object s, int error)
        => Log("Discovering completed with result: 0x" + error.ToString("X8"));

    private void WatcherOnDeviceFound(object s, string id, string name)
    {
        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.Tag = id;
        row.SetCell(0, name);
        row.SetCell(1, id);

        Log("Device found");
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientOnPairGetParams(object s, wclWiFiDirectDevice device,
        out short groupOwnerIntent,
        out wclWiFiDirectConfigurationMethod configurationMethods,
        out wclWiFiDirectPairingProcedure pairingProcedure)
    {
        short.TryParse(groupOwnerIntentInput.text, out groupOwnerIntent);

        configurationMethods = 0;
        if (displayPinToggle.isOn)  configurationMethods |= wclWiFiDirectConfigurationMethod.cmDisplayPin;
        if (providePinToggle.isOn)  configurationMethods |= wclWiFiDirectConfigurationMethod.cmProvidePin;
        if (pushButtonToggle.isOn)  configurationMethods |= wclWiFiDirectConfigurationMethod.cmPushButton;

        pairingProcedure = pairingProcedureDropdown.value == 0
            ? wclWiFiDirectPairingProcedure.ppInvitation
            : wclWiFiDirectPairingProcedure.ppGroupOwnerNegotiation;
    }

    private void ClientOnPairProvidePin(object s, wclWiFiDirectDevice device, out string pin)
    {
        // В Unity из синхронного callback нельзя запросить ввод через UI.
        pin = "0000";
        Log(device.Name + " requested PIN. Auto-answered with '0000'");
    }

    private void ClientOnPairDisplayPin(object s, wclWiFiDirectDevice device, string pin)
        => Log("Use PIN: " + pin);

    private void ClientOnPairConfirm(object s, wclWiFiDirectDevice device, out bool confirm)
    {
        // То же ограничение: блокировать UI нельзя, поэтому подтверждаем.
        confirm = true;
        Log(device.Name + " pairing confirm > accepted");
    }

    private void ClientOnPairCompleted(object s, wclWiFiDirectDevice device, int result)
        => Log("Pairing completed with result: 0x" + result.ToString("X8"));

    private void ClientOnDeviceConnected(object s, wclWiFiDirectDevice device, int error)
    {
        Log("Connected with result: 0x" + error.ToString("X8"));
        if (error == wclErrors.WCL_E_SUCCESS)
        {
            Log("  Local IP: " + _client.LocalAddress);
            Log("  Remote IP: " + _client.RemoteAddress);
        }
    }

    private void ClientOnDeviceDisconnected(object s, wclWiFiDirectDevice device, int reason)
        => Log("Disconnected. Reason: 0x" + reason.ToString("X8"));

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnDiscoverClicked()
    {
        Int32 res = _watcher.Discover();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Discover error: 0x" + res.ToString("X8"));
    }

    private void OnTerminateClicked()
    {
        Int32 res = _watcher.Terminate();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Terminate error: 0x" + res.ToString("X8"));
    }

    private void OnEnumPairedClicked()
    {
        var paired = new List<string>();
        Int32 res = _watcher.EnumPairedDevices(paired);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enum paired error: 0x" + res.ToString("X8"));
            return;
        }

        ClearChildren(devicesContent);
        _selectedDevice = null;

        foreach (var id in paired)
        {
            var rowGO = Instantiate(deviceRowPrefab, devicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnDeviceRowClicked);
            row.Tag = id;
            row.SetCell(0, "");
            row.SetCell(1, id);
        }
    }

    private void OnIsPairedClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        bool isPaired;
        Int32 res = _watcher.IsPaired(_selectedDevice.GetCell(1), out isPaired);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get paired error: 0x" + res.ToString("X8"));
            return;
        }

        Log(isPaired ? "Device is paired" : "Device is NOT paired");
    }

    private void OnUnpairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        Int32 res = _watcher.Unpair(_selectedDevice.GetCell(1));
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Unpair error: 0x" + res.ToString("X8"));
        else
            Log("Unpaired");
    }

    private void OnConnectClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        Int32 res = _client.Connect(_selectedDevice.GetCell(1));
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Connect Error: 0x" + res.ToString("X8"));
    }

    private void OnDisconnectClicked()
    {
        Int32 res = _client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Disconnect error: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Row handler
    // ==================================================================
    private void OnDeviceRowClicked(TableRowUI row)
    {
        if (_selectedDevice != null) _selectedDevice.SetSelected(false);
        _selectedDevice = row;
        if (_selectedDevice != null) _selectedDevice.SetSelected(true);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private static void EnsureDropdown(TMP_Dropdown dd, string[] options, int defaultValue)
    {
        if (dd == null) return;

        if (dd.options == null || dd.options.Count != options.Length)
        {
            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
        }

        dd.value = Mathf.Clamp(defaultValue, 0, options.Length - 1);
        dd.RefreshShownValue();
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // ==================================================================
    //  Log
    // ==================================================================
    private void Log(string msg)
    {
        Debug.Log("[WiFiDirectClient] " + msg);
        if (logContent == null || logRowPrefab == null) return;

        var go = Instantiate(logRowPrefab, logContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, msg);

        var sr = logContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }
}