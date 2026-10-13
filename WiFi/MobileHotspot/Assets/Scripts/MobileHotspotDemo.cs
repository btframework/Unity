using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class MobileHotspotDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button openButton;
    public Button closeButton;
    public Button startButton;
    public Button stopButton;
    public Button getStateButton;

    // ---------------- SSID row ----------------
    public TMP_InputField ssidInput;
    public Button         getSsidButton;
    public Button         setSsidButton;
    public Button         maxClientsButton;

    // ---------------- Password row ----------------
    public TMP_InputField passwordInput;
    public Button         getPasswordButton;
    public Button         setPasswordButton;
    public Button         connectedCountButton;

    // ---------------- Band row ----------------
    public TMP_Dropdown bandDropdown;
    public Button       isSupportedButton;
    public Button       setBandButton;
    public Button       getBandButton;

    // ---------------- Timeout row ----------------
    public Button connectedClientsButton;
    public Button checkTimeoutButton;
    public Button enableTimeoutButton;
    public Button disableTimeoutButton;

    // ---------------- Connected clients table ----------------
    public Transform  clientsContent;
    public GameObject clientRowPrefab;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclMobileHotspot _hotspot;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        openButton            .onClick.AddListener(OnOpenClicked);
        closeButton           .onClick.AddListener(OnCloseClicked);
        startButton           .onClick.AddListener(OnStartClicked);
        stopButton            .onClick.AddListener(OnStopClicked);
        getStateButton        .onClick.AddListener(OnGetStateClicked);
        getSsidButton         .onClick.AddListener(OnGetSsidClicked);
        setSsidButton         .onClick.AddListener(OnSetSsidClicked);
        maxClientsButton      .onClick.AddListener(OnMaxClientsClicked);
        getPasswordButton     .onClick.AddListener(OnGetPasswordClicked);
        setPasswordButton     .onClick.AddListener(OnSetPasswordClicked);
        connectedCountButton  .onClick.AddListener(OnConnectedCountClicked);
        isSupportedButton     .onClick.AddListener(OnIsSupportedClicked);
        setBandButton         .onClick.AddListener(OnSetBandClicked);
        getBandButton         .onClick.AddListener(OnGetBandClicked);
        connectedClientsButton.onClick.AddListener(OnConnectedClientsClicked);
        checkTimeoutButton    .onClick.AddListener(OnCheckTimeoutClicked);
        enableTimeoutButton   .onClick.AddListener(OnEnableTimeoutClicked);
        disableTimeoutButton  .onClick.AddListener(OnDisableTimeoutClicked);
    }

    private void Start()
    {
        _hotspot = new wclMobileHotspot();
        _hotspot.OnClosed += HotspotOnClosed;
        _hotspot.OnOpened += HotspotOnOpened;

        bandDropdown.value = 0;
    }

    private void OnDestroy()
    {
        try { _hotspot?.Close(); } catch (Exception e) { Debug.LogException(e); }

        if (_hotspot != null)
        {
            _hotspot.OnClosed -= HotspotOnClosed;
            _hotspot.OnOpened -= HotspotOnOpened;
        }
        _hotspot = null;
    }

    // ==================================================================
    //  Events
    // ==================================================================
    private void HotspotOnOpened(object s, EventArgs e)
    {
        Log("Mobile hotspot opened.");

        string ssid;
        if (_hotspot.GetSsid(out ssid) == wclErrors.WCL_E_SUCCESS)
            ssidInput.text = ssid;

        string password;
        if (_hotspot.GetPassphrase(out password) == wclErrors.WCL_E_SUCCESS)
            passwordInput.text = password;
    }

    private void HotspotOnClosed(object s, EventArgs e)
    {
        Log("Mobile hotspot closed.");
        ssidInput.text = "";
        passwordInput.text = "";
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnOpenClicked()
    {
        Int32 res = _hotspot.Open();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Open failed", res);
    }

    private void OnCloseClicked()
    {
        Int32 res = _hotspot.Close();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Close failed", res);
    }

    private void OnStartClicked()
    {
        Int32 res = _hotspot.Start();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Start failed", res);
    }

    private void OnStopClicked()
    {
        Int32 res = _hotspot.Stop();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Stop failed", res);
    }

    private void OnGetStateClicked()
    {
        wclMobileHotspotState state;
        Int32 res = _hotspot.GetState(out state);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Get state failed", res); return; }

        switch (state)
        {
            case wclMobileHotspotState.mhsUnknown:      Log("State: unknown");  break;
            case wclMobileHotspotState.mhsOn:           Log("State: on");       break;
            case wclMobileHotspotState.mhsOff:          Log("State: off");      break;
            case wclMobileHotspotState.mhsInTransition: Log("State: switching");break;
        }
    }

    private void OnGetSsidClicked()
    {
        string ssid;
        Int32 res = _hotspot.GetSsid(out ssid);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Get SSID failed", res);
        else ssidInput.text = ssid;
    }

    private void OnSetSsidClicked()
    {
        Int32 res = _hotspot.SetSsid(ssidInput.text);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Set SSID failed", res);
    }

    private void OnGetPasswordClicked()
    {
        string pwd;
        Int32 res = _hotspot.GetPassphrase(out pwd);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Get passphrase failed", res);
        else passwordInput.text = pwd;
    }

    private void OnSetPasswordClicked()
    {
        Int32 res = _hotspot.SetPassphrase(passwordInput.text);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Set passphrase failed", res);
    }

    private void OnMaxClientsClicked()
    {
        UInt32 count;
        Int32 res = _hotspot.GetMaxClientCount(out count);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Get max client connections failed", res);
        else Log("Maximum allowed connections: " + count);
    }

    private void OnConnectedCountClicked()
    {
        UInt32 count;
        Int32 res = _hotspot.GetClientCount(out count);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Get connectioned clients count failed", res);
        else Log("Connected clients: " + count);
    }

    private void OnIsSupportedClicked()
    {
        wclMobileHotspotBand band = BandFromDropdown();
        bool supported;
        Int32 res = _hotspot.IsBandSupported(band, out supported);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Check band failed", res);
        else Log(supported ? "Band is supported" : "Band is NOT supported");
    }

    private void OnSetBandClicked()
    {
        Int32 res = _hotspot.SetBand(BandFromDropdown());
        if (res != wclErrors.WCL_E_SUCCESS) Log("Set band failed", res);
    }

    private void OnGetBandClicked()
    {
        wclMobileHotspotBand band;
        Int32 res = _hotspot.GetBand(out band);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Get band failed", res); return; }

        switch (band)
        {
            case wclMobileHotspotBand.mhbAuto:                 Log("Band: Auto"); break;
            case wclMobileHotspotBand.mhbTwoPointFourGigahertz:Log("Band: 2.4");  break;
            default:                                            Log("Band: 5.8");  break;
        }
    }

    private void OnConnectedClientsClicked()
    {
        ClearChildren(clientsContent);

        wclMobileHotspotClient[] clients;
        Int32 res = _hotspot.GetClients(out clients);
        if (res != wclErrors.WCL_E_SUCCESS) { Log("Get connected clients failed", res); return; }
        if (clients == null) return;

        foreach (var client in clients)
        {
            var rowGO = Instantiate(clientRowPrefab, clientsContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(null);
            row.SetCell(0, client.Mac);
            row.SetCell(1, client.Name);
        }
    }

    private void OnCheckTimeoutClicked()
    {
        bool enabled;
        Int32 res = _hotspot.IsNoConnectionsTimeoutEnabled(out enabled);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Get no connection timeout status failed", res);
        else Log(enabled ? "No connection timeout enabled" : "No connection timeout disabled");
    }

    private void OnEnableTimeoutClicked()
    {
        Int32 res = _hotspot.EnableNoConnectionsTimeout();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Enable No Connection Timeout failed", res);
    }

    private void OnDisableTimeoutClicked()
    {
        Int32 res = _hotspot.DisableNoConnectionsTimeout();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Disable No Connection Timeout failed", res);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private wclMobileHotspotBand BandFromDropdown()
    {
        switch (bandDropdown.value)
        {
            case 0:  return wclMobileHotspotBand.mhbAuto;
            case 1:  return wclMobileHotspotBand.mhbTwoPointFourGigahertz;
            default: return wclMobileHotspotBand.mhbFiveGigahertz;
        }
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
        Debug.Log("[MobileHotspot] " + msg);
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

    private void Log(string msg, int error) => Log(msg + ". Error: 0x" + error.ToString("X8"));
}