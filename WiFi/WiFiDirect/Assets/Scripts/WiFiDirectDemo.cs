using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiDirectDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public TMP_Dropdown discoverabilityDropdown;
    public TMP_Dropdown pairingProcedureDropdown;
    public Toggle       autonomousGroupOwnerToggle;
    public TMP_InputField groupOwnerIntentInput;

    public Toggle displayPinToggle;
    public Toggle providePinToggle;
    public Toggle pushButtonToggle;

    public Button startButton;
    public Button stopButton;

    // ---------------- Devices table ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;
    public Button     disconnectButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;
    public Button     clearLogButton;

    // ---------------- Confirm panel (Yes/No) ----------------
    public GameObject confirmPanel;
    public TMP_Text   confirmText;
    public Button     confirmYesButton;
    public Button     confirmNoButton;

    // ---------------- PIN input panel ----------------
    public GameObject     pinPanel;
    public TMP_Text       pinText;
    public TMP_InputField pinInput;
    public Button         pinOkButton;
    public Button         pinCancelButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiDirectAdvertiser _advertiser;
    private TableRowUI _selectedDevice;

    private Action<bool> _confirmCallback;
    private Action<string> _pinCallback;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        startButton        .onClick.AddListener(OnStartClicked);
        stopButton         .onClick.AddListener(OnStopClicked);
        disconnectButton   .onClick.AddListener(OnDisconnectClicked);
        clearLogButton     .onClick.AddListener(() => ClearChildren(logContent));

        confirmYesButton   .onClick.AddListener(() => ConfirmResult(true));
        confirmNoButton    .onClick.AddListener(() => ConfirmResult(false));

        pinOkButton        .onClick.AddListener(() => PinResult(pinInput.text));
        pinCancelButton    .onClick.AddListener(() => PinResult(null));

        confirmPanel.SetActive(false);
        pinPanel    .SetActive(false);
    }

    private void Start()
    {
        EnsureDropdown(discoverabilityDropdown, new[] { "Intensive", "None", "Normal" }, 2);
        EnsureDropdown(pairingProcedureDropdown, new[] { "ppInvitation", "ppGroupOwnerNegotiation" }, 1);

        pushButtonToggle.isOn = true;
        groupOwnerIntentInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        groupOwnerIntentInput.text = "14";

        _advertiser = new wclWiFiDirectAdvertiser();
        _advertiser.OnAcceptDevice       += AdvertiserOnAcceptDevice;
        _advertiser.OnStarted            += AdvertiserOnStarted;
        _advertiser.OnStopped            += AdvertiserOnStopped;
        _advertiser.OnPairProvidePin     += AdvertiserOnPairProvidePin;
        _advertiser.OnPairDisplayPin     += AdvertiserOnPairDisplayPin;
        _advertiser.OnPairConfirm        += AdvertiserOnPairConfirm;
        _advertiser.OnPairCompleted      += AdvertiserOnPairCompleted;
        _advertiser.OnDeviceDisconnected += AdvertiserOnDeviceDisconnected;
        _advertiser.OnPairGetParams      += AdvertiserOnPairGetParams;
        _advertiser.OnDeviceConnected    += AdvertiserOnDeviceConnected;
    }

    private void OnDestroy()
    {
        if (_advertiser == null) return;

        try { _advertiser.Stop(); } catch (Exception e) { Debug.LogException(e); }

        _advertiser.OnAcceptDevice       -= AdvertiserOnAcceptDevice;
        _advertiser.OnStarted            -= AdvertiserOnStarted;
        _advertiser.OnStopped            -= AdvertiserOnStopped;
        _advertiser.OnPairProvidePin     -= AdvertiserOnPairProvidePin;
        _advertiser.OnPairDisplayPin     -= AdvertiserOnPairDisplayPin;
        _advertiser.OnPairConfirm        -= AdvertiserOnPairConfirm;
        _advertiser.OnPairCompleted      -= AdvertiserOnPairCompleted;
        _advertiser.OnDeviceDisconnected -= AdvertiserOnDeviceDisconnected;
        _advertiser.OnPairGetParams      -= AdvertiserOnPairGetParams;
        _advertiser.OnDeviceConnected    -= AdvertiserOnDeviceConnected;

        _advertiser = null;
    }

    // ==================================================================
    //  Advertiser events
    // ==================================================================
    private void AdvertiserOnStarted(object s, EventArgs e)
        => Log("Advertiser has been started");

    private void AdvertiserOnStopped(object s, EventArgs e)
    {
        ClearChildren(devicesContent);
        _selectedDevice = null;
        Log("Advertiser has been stopped");
    }

    private void AdvertiserOnPairGetParams(object s, wclWiFiDirectDevice device,
        out short groupOwnerIntent, out wclWiFiDirectConfigurationMethod configurationMethods,
        out wclWiFiDirectPairingProcedure pairingProcedure)
    {
        Log(device.Name + " querying pairing params");

        switch (pairingProcedureDropdown.value)
        {
            case 0:  pairingProcedure = wclWiFiDirectPairingProcedure.ppInvitation; break;
            case 1:  pairingProcedure = wclWiFiDirectPairingProcedure.ppGroupOwnerNegotiation; break;
            default: pairingProcedure = wclWiFiDirectPairingProcedure.ppGroupOwnerNegotiation; break;
        }

        short.TryParse(groupOwnerIntentInput.text, out groupOwnerIntent);

        configurationMethods = 0;
        if (displayPinToggle.isOn) configurationMethods |= wclWiFiDirectConfigurationMethod.cmDisplayPin;
        if (providePinToggle.isOn) configurationMethods |= wclWiFiDirectConfigurationMethod.cmProvidePin;
        if (pushButtonToggle.isOn) configurationMethods |= wclWiFiDirectConfigurationMethod.cmPushButton;
    }

    private void AdvertiserOnPairProvidePin(object s, wclWiFiDirectDevice device, out string pin)
    {
        // It is impossible to synchronously request user input from a callback (it would block the UI).
        // Therefore we return an empty PIN, and request input asynchronously — through the panel.
        // In the simplest case, a fixed PIN "0000" can be returned.
        pin = "0000";
        Log(device.Name + " requested PIN. Auto-answered with '0000'");

        // If interactive input is needed, uncomment the code below, but keep in mind
        // that the callback itself returns control immediately, and the panel will appear
        // only after the operation completes.
        // ShowPinDialog(device.Name, enteredPin =>
        // {
        //     if (!string.IsNullOrEmpty(enteredPin)) pin = enteredPin;
        // });
    }

    private void AdvertiserOnPairDisplayPin(object s, wclWiFiDirectDevice device, string pin)
        => Log("Use " + pin + " PIN to pair with " + device.Name);

    private void AdvertiserOnPairConfirm(object s, wclWiFiDirectDevice device, out bool confirm)
    {
        // See comment in AdvertiserOnPairProvidePin: UI cannot be blocked.
        // We accept automatically.
        confirm = true;
        Log(device.Name + " pairing confirm > accepted");

        // ShowConfirm(device.Name + " connection", result => { /* does not affect the current callback */ });
    }

    private void AdvertiserOnPairCompleted(object s, wclWiFiDirectDevice device, int result)
        => Log(device.Name + " paired. Result: 0x" + result.ToString("X8"));

    private void AdvertiserOnAcceptDevice(object s, wclWiFiDirectDevice device, out bool accept)
    {
        Log("Accepting device " + device.Id);

        // Originally MessageBox.Show(YesNo) was here. In Unity, blocking is not allowed —
        // we accept automatically and add the device to the table.
        accept = true;

        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.SetCell(0, device.Name);
        row.SetCell(1, device.Id);
        row.SetCell(2, "Connecting");
        row.Tag = device.Id;
    }

    private void AdvertiserOnDeviceConnected(object s, wclWiFiDirectDevice device, int error)
    {
        var row = FindDeviceRow(device.Id);
        if (row == null) return;

        if (error == wclErrors.WCL_E_SUCCESS)
        {
            row.SetCell(2, "Connected");

            Log(device.Name + " connected");
            Log("  Local IP: " + device.LocalAddress);
            Log("  Remote IP: " + device.RemoteAddress);
        }
        else
        {
            Log(device.Name + " connect error: 0x" + error.ToString("X8"));
            if (row == _selectedDevice) _selectedDevice = null;
            Destroy(row.gameObject);
        }
    }

    private void AdvertiserOnDeviceDisconnected(object s, wclWiFiDirectDevice device, int reason)
    {
        var row = FindDeviceRow(device.Id);
        if (row == null) return;

        Log(device.Name + " disconnected. Reason: 0x" + reason.ToString("X8"));
        if (row == _selectedDevice) _selectedDevice = null;
        Destroy(row.gameObject);
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnStartClicked()
    {
        if (_advertiser.Active)
        {
            Log("Already running");
            return;
        }

        switch (discoverabilityDropdown.value)
        {
            case 0: _advertiser.Discoverability = wclWiFiDirectAdvertisementDiscoverability.adIntensive; break;
            case 1: _advertiser.Discoverability = wclWiFiDirectAdvertisementDiscoverability.adNone; break;
            case 2: _advertiser.Discoverability = wclWiFiDirectAdvertisementDiscoverability.adNormal; break;
        }

        _advertiser.AutonomousGroupOwnerEnabled = autonomousGroupOwnerToggle.isOn;

        Int32 res = _advertiser.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Start error: 0x" + res.ToString("X8"));
    }

    private void OnStopClicked()
    {
        Int32 res = _advertiser.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Stop error: 0x" + res.ToString("X8"));
    }

    private void OnDisconnectClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        string id = _selectedDevice.Tag as string;
        if (string.IsNullOrEmpty(id)) return;

        for (int i = 0; i < _advertiser.Count; i++)
        {
            if (_advertiser[i].Id == id)
            {
                Int32 res = _advertiser[i].Disconnect();
                if (res != wclErrors.WCL_E_SUCCESS)
                    Log("Disconnect error: 0x" + res.ToString("X8"));
                break;
            }
        }
    }

    // ==================================================================
    //  Rows
    // ==================================================================
    private void OnDeviceRowClicked(TableRowUI row)
    {
        if (_selectedDevice != null) _selectedDevice.SetSelected(false);
        _selectedDevice = row;
        if (_selectedDevice != null) _selectedDevice.SetSelected(true);
    }

    private TableRowUI FindDeviceRow(string id)
    {
        foreach (Transform child in devicesContent)
        {
            var row = child.GetComponent<TableRowUI>();
            if (row == null) continue;
            if ((row.Tag as string) == id) return row;
        }
        return null;
    }

    // ==================================================================
    //  Confirm / PIN panels
    // ==================================================================
    private void ShowConfirm(string message, Action<bool> callback)
    {
        _confirmCallback = callback;
        confirmText.text = message;
        confirmPanel.SetActive(true);
    }

    private void ConfirmResult(bool yes)
    {
        confirmPanel.SetActive(false);
        var cb = _confirmCallback;
        _confirmCallback = null;
        cb?.Invoke(yes);
    }

    private void ShowPinDialog(string deviceName, Action<string> callback)
    {
        _pinCallback = callback;
        pinText.text = "Enter PIN for " + deviceName;
        pinInput.text = "";
        pinPanel.SetActive(true);
    }

    private void PinResult(string entered)
    {
        pinPanel.SetActive(false);
        var cb = _pinCallback;
        _pinCallback = null;
        cb?.Invoke(entered);
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
            dd.AddOptions(new System.Collections.Generic.List<string>(options));
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
        Debug.Log("[WiFiDirect] " + msg);
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