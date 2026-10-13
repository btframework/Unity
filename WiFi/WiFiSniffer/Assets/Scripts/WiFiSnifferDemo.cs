using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiSnifferDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button         enumInterfacesButton;
    public Button         startCaptureButton;
    public Button         stopCaptureButton;
    public Toggle         doNotChangeModeToggle;
    public TMP_Text       ifaceModeCaption;
    public TMP_Text       ifaceModeLabel;

    // ---------------- Interfaces table ----------------
    public Transform  interfacesContent;
    public GameObject interfaceRowPrefab;

    // ---------------- Info row ----------------
    public TMP_Text macAddressTitle;
    public TMP_Text macAddressLabel;
    public TMP_Text channelTitle;
    public TMP_Text channelLabel;
    public TMP_Text phyTitle;
    public TMP_Text phyLabel;
    public TMP_Text warningLabel;

    // ---------------- Set channel row ----------------
    public TMP_Text       setChannelTitle;
    public TMP_InputField channelInput;
    public Button         setChannelButton;

    // ---------------- Set PHY row ----------------
    public TMP_Text     setPhyTitle;
    public TMP_Dropdown phyDropdown;
    public Button       setPhyButton;

    // ---------------- Clear ----------------
    public Button clearButton;

    // ---------------- Frames ----------------
    public TMP_Text       rawFramesTitle;
    public Transform      framesContent;
    public GameObject     frameRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiClient    _client;
    private wclWiFiSniffer   _sniffer;
    private TableRowUI       _selectedInterface;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        enumInterfacesButton.onClick.AddListener(OnEnumInterfacesClicked);
        startCaptureButton  .onClick.AddListener(OnStartCaptureClicked);
        stopCaptureButton   .onClick.AddListener(OnStopCaptureClicked);
        setChannelButton    .onClick.AddListener(OnSetChannelClicked);
        setPhyButton        .onClick.AddListener(OnSetPhyClicked);
        clearButton         .onClick.AddListener(() => ClearChildren(framesContent));

        channelInput.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    private void Start()
    {
        EnsureDropdown(phyDropdown, new[] { "802.11a", "802.11b", "802.11g", "802.11n" }, 0);
        channelInput.text = "6";

        _sniffer = new wclWiFiSniffer();
        _client  = new wclWiFiClient();

        _sniffer.AfterOpen          += SnifferAfterOpen;
        _sniffer.BeforeClose        += SnifferBeforeClose;
        _sniffer.OnRawFrameReceived += SnifferOnRawFrameReceived;
    }

    private void OnDestroy()
    {
        if (_sniffer != null)
        {
            try { _sniffer.Close(); } catch (Exception e) { Debug.LogException(e); }
            _sniffer.AfterOpen          -= SnifferAfterOpen;
            _sniffer.BeforeClose        -= SnifferBeforeClose;
            _sniffer.OnRawFrameReceived -= SnifferOnRawFrameReceived;
            _sniffer = null;
        }
        _client = null;
    }

    // ==================================================================
    //  Sniffer events
    // ==================================================================
    private void SnifferBeforeClose(object s, EventArgs e)
    {
        macAddressLabel.text = "000000000000";
        phyLabel.text        = "NONE";
        channelLabel.text    = "0";
        ifaceModeLabel.text  = "Unknown";
    }

    private void SnifferAfterOpen(object s, EventArgs e)
    {
        RefreshChannel();
        RefreshPhy();

        long mac;
        Int32 res = _sniffer.GetMacAddr(out mac);
        if (res == wclErrors.WCL_E_SUCCESS)
        {
            macAddressLabel.text = mac.ToString("X12");
            ifaceModeLabel.text  = OperationModeStr(_sniffer.Mode);
        }
        else
        {
            macAddressLabel.text = "Error: 0x" + res.ToString("X8");
        }
    }

    private void SnifferOnRawFrameReceived(object s, byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0) return;

        string line = "";
        for (int i = 0; i < buffer.Length; i++)
        {
            line += buffer[i].ToString("X2") + " ";
            if (line.Length == 96)
            {
                AddFrameRow(line);
                line = "";
            }
        }
        if (line.Length > 0)
            AddFrameRow(line);
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnEnumInterfacesClicked()
    {
        ClearChildren(interfacesContent);
        _selectedInterface = null;

        Int32 res = _client.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            ShowError("Unable to open WiFi Client", res);
            return;
        }

        try
        {
            wclWiFiInterfaceData[] ifaces;
            res = _client.EnumInterfaces(out ifaces);
            if (res != wclErrors.WCL_E_SUCCESS)
            {
                ShowError("Unable to enumerate WiFi Interfaces", res);
                return;
            }

            if (ifaces == null || ifaces.Length == 0)
            {
                Log("No WiFi Interfaces were found");
                return;
            }

            foreach (var iface in ifaces)
            {
                var rowGO = Instantiate(interfaceRowPrefab, interfacesContent);
                var row = rowGO.GetComponent<TableRowUI>();
                row.Init(OnInterfaceRowClicked);
                row.Tag = iface.Id;
                row.SetCell(0, iface.Description);
                row.SetCell(1, iface.Id.ToString());
            }
        }
        finally
        {
            _client.Close();
        }
    }

    private void OnStartCaptureClicked()
    {
        if (_selectedInterface == null) { Log("Select interface"); return; }

        Guid id;
        try { id = new Guid(_selectedInterface.GetCell(1)); }
        catch { Log("Invalid interface id"); return; }

        try
        {
            _sniffer.DoNotChangeMode = doNotChangeModeToggle.isOn;
            Int32 res = _sniffer.Open(id);
            if (res != wclErrors.WCL_E_SUCCESS)
                ShowError("Unable start capturing", res);
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void OnStopCaptureClicked()
    {
        Int32 res = _sniffer.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            ShowError("Unable to stop capturing", res);
    }

    private void OnSetChannelClicked()
    {
        UInt32 ch = (UInt32)ParseInt(channelInput.text, 0);
        Int32 res = _sniffer.SetChannel(ch);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            ShowError("Unable to change channel", res);
            return;
        }
        RefreshChannel();
    }

    private void OnSetPhyClicked()
    {
        var phy = (wclWiFiSnifferPhy)phyDropdown.value;
        Int32 res = _sniffer.SetPhy(phy);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            ShowError("Unable to change PHY", res);
            return;
        }
        RefreshPhy();
    }

    // ==================================================================
    //  Refresh helpers
    // ==================================================================
    private void RefreshChannel()
    {
        UInt32 ch;
        Int32 res = _sniffer.GetChannel(out ch);
        channelLabel.text = res == wclErrors.WCL_E_SUCCESS
            ? ch.ToString()
            : "Error: 0x" + res.ToString("X8");
    }

    private void RefreshPhy()
    {
        wclWiFiSnifferPhy phy;
        Int32 res = _sniffer.GetPhy(out phy);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            phyLabel.text = "Error: 0x" + res.ToString("X8");
            return;
        }

        switch (phy)
        {
            case wclWiFiSnifferPhy.ph802_11a: phyLabel.text = "802.11a"; break;
            case wclWiFiSnifferPhy.ph802_11b: phyLabel.text = "802.11b"; break;
            case wclWiFiSnifferPhy.ph802_11g: phyLabel.text = "802.11g"; break;
            case wclWiFiSnifferPhy.ph802_11n: phyLabel.text = "802.11n"; break;
            default:                          phyLabel.text = "UNKNOWN"; break;
        }
    }

    // ==================================================================
    //  Rows
    // ==================================================================
    private void OnInterfaceRowClicked(TableRowUI row)
    {
        if (_selectedInterface != null) _selectedInterface.SetSelected(false);
        _selectedInterface = row;
        if (_selectedInterface != null) _selectedInterface.SetSelected(true);
    }

    private void AddFrameRow(string hexLine)
    {
        if (framesContent == null || frameRowPrefab == null) return;

        var go = Instantiate(frameRowPrefab, framesContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, hexLine);

        var sr = framesContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private void ShowError(string msg, Int32 error)
        => Log(msg + ": 0x" + error.ToString("X8"));

    private static string OperationModeStr(wclWiFiOperationMode mode)
    {
        switch (mode)
        {
            case wclWiFiOperationMode.omUnknown:               return "Unknown";
            case wclWiFiOperationMode.omStation:               return "Station";
            case wclWiFiOperationMode.omAccessPoint:           return "Access point";
            case wclWiFiOperationMode.omExtensibleStation:     return "Extensible Station";
            case wclWiFiOperationMode.omExtensibleAccessPoint: return "Extensible Access Point";
            case wclWiFiOperationMode.omWiFiDirectDevice:      return "WiFiDirect Device";
            case wclWiFiOperationMode.omWiFiDirectGroupOwner:  return "WiFiDirect Group Owner";
            case wclWiFiOperationMode.omWiFiDirectClient:      return "WiFiDirect Client";
            case wclWiFiOperationMode.omManufacturing:         return "Manufacturing";
            case wclWiFiOperationMode.omNetworkMonitor:        return "Network Monitor";
            default:                                            return "Unknown";
        }
    }

    private static int ParseInt(string s, int fallback)
        => int.TryParse(s, out var v) ? v : fallback;

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

    private void Log(string msg)
    {
        Debug.Log("[WiFiSniffer] " + msg);
        if (framesContent == null || frameRowPrefab == null) return;

        var go = Instantiate(frameRowPrefab, framesContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, msg);

        var sr = framesContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }
}