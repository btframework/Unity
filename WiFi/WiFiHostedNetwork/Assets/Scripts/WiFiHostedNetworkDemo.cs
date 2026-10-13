using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiHostedNetworkDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button openButton;
    public Button closeButton;
    public Button clearButton;

    // ---------------- Get operations ----------------
    public Button getConnectionSettingsButton;
    public Button getKeyButton;
    public Button getProfileButton;
    public Button getSecuritySettingsButton;
    public Button getStatusButton;
    public Button getStateButton;

    // ---------------- Data table ----------------
    public Transform  dataContent;
    public GameObject dataRowPrefab;

    // ---------------- Refresh / Restart ----------------
    public Button refreshSecuritySettingsButton;
    public Button restartButton;

    // ---------------- Settings fields ----------------
    public TMP_Text       ssidLabel;
    public TMP_InputField ssidInput;
    public TMP_Text       numberOfPeersLabel;
    public TMP_InputField numberOfPeersInput;
    public Button         setSettingsButton;

    public TMP_Text       keyLabel;
    public TMP_InputField keyInput;
    public Toggle         persistentToggle;
    public Button         setKeyButton;

    // ---------------- Enable / Disable ----------------
    public Button enableButton;
    public Button disableButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiHostedNetwork _network;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        openButton                    .onClick.AddListener(OnOpenClicked);
        closeButton                   .onClick.AddListener(OnCloseClicked);
        clearButton                   .onClick.AddListener(OnClearClicked);

        getConnectionSettingsButton   .onClick.AddListener(OnGetConnectionSettingsClicked);
        getKeyButton                  .onClick.AddListener(OnGetKeyClicked);
        getProfileButton              .onClick.AddListener(OnGetProfileClicked);
        getSecuritySettingsButton     .onClick.AddListener(OnGetSecuritySettingsClicked);
        getStatusButton               .onClick.AddListener(OnGetStatusClicked);
        getStateButton                .onClick.AddListener(OnGetStateClicked);

        refreshSecuritySettingsButton .onClick.AddListener(OnRefreshSecuritySettingsClicked);
        restartButton                 .onClick.AddListener(OnRestartClicked);

        setSettingsButton             .onClick.AddListener(OnSetSettingsClicked);
        setKeyButton                  .onClick.AddListener(OnSetKeyClicked);

        enableButton                  .onClick.AddListener(OnEnableClicked);
        disableButton                 .onClick.AddListener(OnDisableClicked);
    }

    private void Start()
    {
        numberOfPeersInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        numberOfPeersInput.text = "5";

        ssidInput.text = "MyHostedNetwork";
        keyInput.text  = "123456789";

        _network = new wclWiFiHostedNetwork();
        _network.AfterOpen   += NetworkAfterOpen;
        _network.BeforeClose += NetworkBeforeClose;
    }

    private void OnDestroy()
    {
        if (_network == null) return;

        try { _network.Close(); } catch (Exception e) { Debug.LogException(e); }

        _network.AfterOpen   -= NetworkAfterOpen;
        _network.BeforeClose -= NetworkBeforeClose;
        _network = null;
    }

    // ==================================================================
    //  Lifecycle events
    // ==================================================================
    private void NetworkAfterOpen(object s, EventArgs e)   => Log("WiFi Hosted Network opened");
    private void NetworkBeforeClose(object s, EventArgs e) => Log("WiFi Hosted Network closing");

    // ==================================================================
    //  Buttons — top row
    // ==================================================================
    private void OnOpenClicked()  => ShowResult(_network.Open());
    private void OnCloseClicked() => ShowResult(_network.Close());

    private void OnClearClicked() => ClearData();

    // ==================================================================
    //  Get operations
    // ==================================================================
    private void OnGetConnectionSettingsClicked()
    {
        ClearData();

        wclWiFiHostedNetworkConnectionSettings settings;
        if (!ShowResult(_network.GetConnectionSettings(out settings))) return;

        AddData("SSID", settings.Ssid);
        AddData("Max. number of peers", settings.MaxNumberOfPeers.ToString());
    }

    private void OnGetKeyClicked()
    {
        ClearData();

        byte[] keyData;
        bool isPassPhrase;
        bool persistent;
        if (!ShowResult(_network.GetKey(out keyData, out isPassPhrase, out persistent))) return;

        string str;
        if (isPassPhrase)
        {
            str = Encoding.ASCII.GetString(keyData);
        }
        else
        {
            str = "";
            if (keyData != null)
                for (int i = 0; i < keyData.Length; i++)
                    str += keyData[i].ToString("X2");
        }

        AddData("Key length", keyData != null ? keyData.Length.ToString() : "0");
        AddData("Key data", str);
        AddData("Is pass phrase", isPassPhrase.ToString());
        AddData("Persistent", persistent.ToString());
    }

    private void OnGetProfileClicked()
    {
        ClearData();

        string xml;
        if (ShowResult(_network.GetProfile(out xml)))
            AddData("Profile XML", xml);
    }

    private void OnGetSecuritySettingsClicked()
    {
        ClearData();

        wclWiFiAuthCipherPair settings;
        if (!ShowResult(_network.GetSecuritySettings(out settings))) return;

        AddData("Auth algorithm", settings.AuthAlgorithm.ToString());
        AddData("Cipher algorithm", settings.CipherAlgorithm.ToString());
    }

    private void OnGetStatusClicked()
    {
        ClearData();

        wclWiFiHostedNetworkStatus status;
        if (!ShowResult(_network.GetStatus(out status))) return;

        AddData("State", status.State.ToString());
        AddData("Interface ID", status.Id.ToString());
        AddData("BSS ID", status.BssId);
        AddData("Phy", status.Phy.ToString());
        AddData("ChannelFrequency", status.ChannelFrequency.ToString());

        string address;
        Int32 res = _network.GetLocalIp(out address);
        AddData("Local IP", res == wclErrors.WCL_E_SUCCESS
            ? address
            : "Get failed: 0x" + res.ToString("X8"));

        if (status.Peers != null)
        {
            for (int i = 0; i < status.Peers.Length; i++)
            {
                string idx = "Peer[" + i + "]";
                AddData(idx + " MAC", status.Peers[i].Mac);

                res = _network.GetRemoteIp(status.Peers[i].Mac, out address);
                AddData(idx + " IP", res == wclErrors.WCL_E_SUCCESS
                    ? address
                    : "Get failed: 0x" + res.ToString("X8"));

                AddData(idx + " auth state", status.Peers[i].AuthState.ToString());
            }
        }
    }

    private void OnGetStateClicked()
    {
        ClearData();

        bool state;
        if (ShowResult(_network.GetState(out state)))
            AddData("Enabled", state.ToString());
    }

    // ==================================================================
    //  Refresh / Restart
    // ==================================================================
    private void OnRefreshSecuritySettingsClicked()
        => ShowResult(_network.RefreshSecuritySettings());

    private void OnRestartClicked()
        => ShowResult(_network.Restart());

    // ==================================================================
    //  Set operations
    // ==================================================================
    private void OnSetSettingsClicked()
    {
        var settings = new wclWiFiHostedNetworkConnectionSettings();
        settings.Ssid = ssidInput.text;
        settings.MaxNumberOfPeers = ParseUShort(numberOfPeersInput.text, 0);
        ShowResult(_network.SetConnectionSettings(settings));
    }

    private void OnSetKeyClicked()
    {
        byte[] tmp = Encoding.ASCII.GetBytes(keyInput.text);
        byte[] keyData = new byte[tmp.Length + 1];
        tmp.CopyTo(keyData, 0);
        ShowResult(_network.SetKey(keyData, true, persistentToggle.isOn));
    }

    // ==================================================================
    //  Enable / Disable
    // ==================================================================
    private void OnEnableClicked()  => ShowResult(_network.SetState(true));
    private void OnDisableClicked() => ShowResult(_network.SetState(false));

    // ==================================================================
    //  Helpers
    // ==================================================================
    private bool ShowResult(Int32 res)
    {
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Error: 0x" + res.ToString("X8"));
            return false;
        }
        return true;
    }

    private static ushort ParseUShort(string s, ushort fallback)
        => ushort.TryParse(s, out var v) ? v : fallback;

    private void ClearData()
    {
        if (dataContent == null) return;
        for (int i = dataContent.childCount - 1; i >= 0; i--)
            Destroy(dataContent.GetChild(i).gameObject);
    }

    private void AddData(string name, string value)
    {
        if (dataContent == null || dataRowPrefab == null) return;

        var go = Instantiate(dataRowPrefab, dataContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, name);
        row.SetCell(1, value);
    }

    private void Log(string msg) => AddData("", msg);
}