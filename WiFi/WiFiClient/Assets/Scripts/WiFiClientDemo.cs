using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiClientDemo : MonoBehaviour
{
    // ---------------- Top ----------------
    public Button openButton;
    public Button closeButton;
    public Button enumInterfacesButton;
    public Button disconnectButton;
    public Button getInterfaceStateButton;
    public Button turnOnButton;
    public Button turnOffButton;

    // ---------------- IP / Dual STA ----------------
    public Button currentIpButton;
    public Button getIpSettingsButton;
    public Button enableDhcpButton;
    public Button enableStaticIpButton;
    public Button getDualStaStateButton;
    public Button enableDualStaButton;
    public Button disableDualStaButton;
    public Button enumSecondaryInterfacesButton;

    // ---------------- Interfaces table ----------------
    public Transform  interfacesContent;
    public GameObject interfaceRowPrefab;

    // ---------------- Networks row ----------------
    public Button enumNetworksButton;
    public Toggle adHocToggle;
    public Toggle hiddenToggle;
    public Button scanButton;
    public TMP_InputField scanSsidInput;

    // ---------------- Networks table ----------------
    public Transform  networksContent;
    public GameObject networkRowPrefab;

    // ---------------- BSS row ----------------
    public Button enumBssButton;
    public Toggle useSsidToggle;
    public TMP_Dropdown bssTypeDropdown;

    // ---------------- BSS table ----------------
    public Transform  bssContent;
    public GameObject bssRowPrefab;

    // ---------------- Profiles row ----------------
    public Button enumProfilesButton;
    public Button deleteProfileButton;
    public Button renameProfileButton;
    public Button getProfileXmlButton;
    public Toggle plainTextToggle;
    public Button setProfileXmlButton;
    public Button setXmlUserDataButton;
    public Button editUiButton;

    // ---------------- Profiles table ----------------
    public Transform  profilesContent;
    public GameObject profileRowPrefab;

    // ---------------- Connect row ----------------
    public Button       connectButton;
    public TMP_Dropdown connectModeDropdown;
    public TMP_InputField passkeyInput;
    public Toggle       discoverSecureToggle;
    public Toggle       useBssMacToggle;

    // ---------------- ACM row 1 ----------------
    public Button getShowDeniedButton;
    public Button getPowerSettingsButton;
    public Button getAllowExplicitButton;
    public Button getBlockPeriodButton;
    public Button getVirtualStationButton;

    // ---------------- ACM row 2 ----------------
    public Button setShowDeniedButton;
    public Button getOnlyGpButton;
    public Button setAllowExplicitButton;
    public Button setBlockPeriodButton;
    public Button setVirtualStationButton;

    // ---------------- Confirm panel ----------------
    public GameObject confirmPanel;
    public TMP_Text   confirmText;
    public Button     confirmYesButton;
    public Button     confirmNoButton;
    public Button     confirmCancelButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiClient          _client;
    private wclWiFiProfilesManager _profilesManager;

    private TableRowUI _selectedInterface;
    private TableRowUI _selectedNetwork;
    private TableRowUI _selectedBss;
    private TableRowUI _selectedProfile;

    private Action<int> _confirmCallback;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        openButton                   .onClick.AddListener(OnOpenClicked);
        closeButton                  .onClick.AddListener(OnCloseClicked);
        enumInterfacesButton         .onClick.AddListener(OnEnumInterfacesClicked);
        disconnectButton             .onClick.AddListener(OnDisconnectClicked);
        getInterfaceStateButton      .onClick.AddListener(OnGetInterfaceStateClicked);
        turnOnButton                 .onClick.AddListener(OnTurnOnClicked);
        turnOffButton                .onClick.AddListener(OnTurnOffClicked);

        currentIpButton              .onClick.AddListener(OnCurrentIpClicked);
        getIpSettingsButton          .onClick.AddListener(OnGetIpSettingsClicked);
        enableDhcpButton             .onClick.AddListener(OnEnableDhcpClicked);
        enableStaticIpButton         .onClick.AddListener(OnEnableStaticIpClicked);
        getDualStaStateButton        .onClick.AddListener(OnGetDualStaStateClicked);
        enableDualStaButton          .onClick.AddListener(OnEnableDualStaClicked);
        disableDualStaButton         .onClick.AddListener(OnDisableDualStaClicked);
        enumSecondaryInterfacesButton.onClick.AddListener(OnEnumSecondaryInterfacesClicked);

        enumNetworksButton.onClick.AddListener(OnEnumNetworksClicked);
        scanButton        .onClick.AddListener(OnScanClicked);
        enumBssButton     .onClick.AddListener(OnEnumBssClicked);

        enumProfilesButton    .onClick.AddListener(OnEnumProfilesClicked);
        deleteProfileButton   .onClick.AddListener(OnDeleteProfileClicked);
        renameProfileButton   .onClick.AddListener(OnRenameProfileClicked);
        getProfileXmlButton   .onClick.AddListener(OnGetProfileXmlClicked);
        setProfileXmlButton   .onClick.AddListener(OnSetProfileXmlClicked);
        setXmlUserDataButton  .onClick.AddListener(OnSetXmlUserDataClicked);
        editUiButton          .onClick.AddListener(OnEditUiClicked);

        connectButton.onClick.AddListener(OnConnectClicked);

        getShowDeniedButton       .onClick.AddListener(OnGetShowDeniedClicked);
        getPowerSettingsButton    .onClick.AddListener(OnGetPowerSettingsClicked);
        getAllowExplicitButton    .onClick.AddListener(OnGetAllowExplicitClicked);
        getBlockPeriodButton      .onClick.AddListener(OnGetBlockPeriodClicked);
        getVirtualStationButton   .onClick.AddListener(OnGetVirtualStationClicked);

        setShowDeniedButton       .onClick.AddListener(OnSetShowDeniedClicked);
        getOnlyGpButton           .onClick.AddListener(OnGetOnlyGpClicked);
        setAllowExplicitButton    .onClick.AddListener(OnSetAllowExplicitClicked);
        setBlockPeriodButton      .onClick.AddListener(OnSetBlockPeriodClicked);
        setVirtualStationButton   .onClick.AddListener(OnSetVirtualStationClicked);

        confirmYesButton   .onClick.AddListener(() => ConfirmResult(1));
        confirmNoButton    .onClick.AddListener(() => ConfirmResult(2));
        confirmCancelButton.onClick.AddListener(() => ConfirmResult(0));
        confirmPanel.SetActive(false);
    }

    private void Start()
    {
        EnsureDropdown(bssTypeDropdown, new[] { "bssInfrastructure", "bssIndependent", "bssAny" }, 2);
        EnsureDropdown(connectModeDropdown, new[]
        {
            "using network profile",
            "using selected profile",
            "using selected network",
            "using temporary profile"
        }, 0);

        _client = new wclWiFiClient();
        _client.AfterOpen   += ClientAfterOpen;
        _client.BeforeClose += ClientBeforeClose;

        _profilesManager = new wclWiFiProfilesManager();

        UpdateUI(false);
    }

    private void OnDestroy()
    {
        if (_client != null)
        {
            try { _client.Close(); } catch (Exception e) { Debug.LogException(e); }

            _client.AfterOpen   -= ClientAfterOpen;
            _client.BeforeClose -= ClientBeforeClose;
        }
        _client = null;
        _profilesManager = null;
    }

    private void Update()
    {
        // Nothing to do here - everything is button driven.
    }

    // ==================================================================
    //  Lifecycle callbacks
    // ==================================================================
    private void ClientBeforeClose(object s, EventArgs e)
    {
        _profilesManager.Close();

        ClearChildren(interfacesContent);
        ClearChildren(networksContent);
        ClearChildren(bssContent);
        ClearChildren(profilesContent);

        _selectedInterface = null;
        _selectedNetwork   = null;
        _selectedBss       = null;
        _selectedProfile   = null;

        UpdateUI(false);
    }

    private void ClientAfterOpen(object s, EventArgs e)
    {
        _profilesManager.Open();
        UpdateUI(true);
    }

    // ==================================================================
    //  Top bar
    // ==================================================================
    private void OnOpenClicked()  => ShowResult(_client.Open());
    private void OnCloseClicked() => ShowResult(_client.Close());

    private void OnEnumInterfacesClicked()
    {
        ClearChildren(interfacesContent);

        wclWiFiInterfaceData[] ifaces;
        if (!ShowResult(_client.EnumInterfaces(out ifaces))) return;
        if (ifaces == null) return;

        foreach (var iface in ifaces)
        {
            var rowGO = Instantiate(interfaceRowPrefab, interfacesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnInterfaceRowClicked);
            row.Tag = iface;
            row.SetCell(0, iface.Id.ToString());
            row.SetCell(1, iface.Description);
            row.SetCell(2, iface.Primary.ToString());
        }
    }

    private void OnDisconnectClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        ShowResult(_client.Disconnect(id));
    }

    private void OnGetInterfaceStateClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;

        try
        {
            wclWiFiInterfaceState state;
            if (ShowResult(iface.GetState(out state)))
                Log("Interface state: " + state);
        }
        finally { iface.Close(); }
    }

    private void OnTurnOnClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        SwitchState(id, false);
    }

    private void OnTurnOffClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        SwitchState(id, true);
    }

    private void SwitchState(Guid id, bool off)
    {
        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;

        try
        {
            if (off) ShowResult(iface.TurnOff());
            else     ShowResult(iface.TurnOn());
        }
        finally { iface.Close(); }
    }

    // ==================================================================
    //  IP / Dual STA
    // ==================================================================
    private void OnCurrentIpClicked()   => ShowIpInfo(true);
    private void OnGetIpSettingsClicked() => ShowIpInfo(false);

    private void ShowIpInfo(bool current)
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;

        try
        {
            bool isStatic;
            string dns1, dns2, address, gateway, mask;
            Int32 res = current
                ? iface.GetCurrentIp(out isStatic, out address, out mask, out gateway, out dns1, out dns2)
                : iface.GetIpSettings(out isStatic, out address, out mask, out gateway, out dns1, out dns2);

            if (!ShowResult(res)) return;

            string msg = (isStatic ? "Static IP" : "DHCP") + "\n"
                + "Address: " + address + "\n"
                + "Subnet mask: " + mask + "\n"
                + "Default gateway: " + gateway + "\n"
                + "Name server 1: " + dns1 + "\n"
                + "Name server 2: " + dns2;
            Log(msg);
        }
        finally { iface.Close(); }
    }

    private void OnEnableDhcpClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;
        try { ShowResult(iface.EnableDhcp()); }
        finally { iface.Close(); }
    }

    private void OnEnableStaticIpClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;
        try
        {
            ShowResult(iface.SetStaticIp("192.168.1.210", "255.255.255.0",
                "192.168.1.1", "192.168.1.1", ""));
        }
        finally { iface.Close(); }
    }

    private void OnGetDualStaStateClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;
        try
        {
            bool state;
            if (ShowResult(iface.GetSecondarySta(out state)))
                Log("Dual-STA state: " + state);
        }
        finally { iface.Close(); }
    }

    private void OnEnableDualStaClicked()  => SetDualState(true);
    private void OnDisableDualStaClicked() => SetDualState(false);

    private void SetDualState(bool enable)
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;
        try { ShowResult(iface.SetSecondarySta(enable)); }
        finally { iface.Close(); }
    }

    private void OnEnumSecondaryInterfacesClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        var iface = new wclWiFiInterface(id);
        if (!ShowResult(iface.Open())) return;

        try
        {
            wclWiFiInterfaceData[] ifaces;
            if (!ShowResult(iface.EnumInterfaces(out ifaces))) return;

            ClearChildren(interfacesContent);
            if (ifaces == null) return;

            foreach (var data in ifaces)
            {
                var rowGO = Instantiate(interfaceRowPrefab, interfacesContent);
                var row = rowGO.GetComponent<TableRowUI>();
                row.Init(OnInterfaceRowClicked);
                row.Tag = data;
                row.SetCell(0, data.Id.ToString());
                row.SetCell(1, data.Description);
                row.SetCell(2, data.Primary.ToString());
            }
        }
        finally { iface.Close(); }
    }

    // ==================================================================
    //  Networks
    // ==================================================================
    private void OnEnumNetworksClicked()
    {
        ClearChildren(networksContent);
        _selectedNetwork = null;

        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        wclWiFiAvailableNetworkFilter filters = 0;
        if (adHocToggle.isOn)  filters |= wclWiFiAvailableNetworkFilter.ffIncludeAllAdhocProfiles;
        if (hiddenToggle.isOn) filters |= wclWiFiAvailableNetworkFilter.ffIncludeAllManualHiddenProfiles;

        wclWiFiAvailableNetwork[] networks;
        if (!ShowResult(_client.EnumAvailableNetworks(id, filters, out networks))) return;
        if (networks == null) return;

        foreach (var n in networks)
        {
            var rowGO = Instantiate(networkRowPrefab, networksContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnNetworkRowClicked);
            row.Tag = n;

            row.SetCell(0, n.ProfileName);
            row.SetCell(1, n.Ssid);
            row.SetCell(2, n.BssType.ToString());
            row.SetCell(3, n.NumberOfBssids.ToString());
            row.SetCell(4, n.NetworkConnectable.ToString());
            row.SetCell(5, n.NotConnectableReason.ToString());
            row.SetCell(6, n.PhyTypes.ToString());
            row.SetCell(7, n.MorePhyTypes.ToString());
            row.SetCell(8, n.SignalQuality.ToString());
            row.SetCell(9, n.SecurityEnabled.ToString());
            row.SetCell(10, n.DefaultAuthAlgorithm.ToString());
            row.SetCell(11, n.DefaultCipherAlgorithm.ToString());
            row.SetCell(12, n.Flags.ToString());
        }
    }

    private void OnScanClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        if (ShowResult(_client.Scan(id, scanSsidInput.text)))
            Log("Scan started. Use EventsDemo to see scan events.");
    }

    // ==================================================================
    //  BSS
    // ==================================================================
    private void OnEnumBssClicked()
    {
        ClearChildren(bssContent);
        _selectedBss = null;

        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        string ssid = "";
        bool securityEnabled = false;
        wclWiFiBssType bssType = wclWiFiBssType.bssAny;

        if (useSsidToggle.isOn)
        {
            if (_selectedNetwork == null) { Log("Select network"); return; }
            ssid = _selectedNetwork.GetCell(1);
            securityEnabled = ParseBool(_selectedNetwork.GetCell(9));
            bssType = GetNetworkBssType();
        }

        wclWiFiBss[] bssList;
        if (!ShowResult(_client.EnumBss(id, ssid, bssType, securityEnabled, out bssList))) return;
        if (bssList == null) return;

        foreach (var bss in bssList)
        {
            var rowGO = Instantiate(bssRowPrefab, bssContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnBssRowClicked);
            row.Tag = bss;

            row.SetCell(0, bss.Ssid);
            row.SetCell(1, bss.PhyId.ToString());
            row.SetCell(2, bss.Mac);
            row.SetCell(3, bss.BssType.ToString());
            row.SetCell(4, bss.PhyType.ToString());
            row.SetCell(5, bss.Rssi.ToString());
            row.SetCell(6, bss.LinkQuality.ToString());
            row.SetCell(7, bss.InRegDomain.ToString());
            row.SetCell(8, bss.BeaconPeriod.ToString());
            row.SetCell(9, bss.Timestamp.ToString());
            row.SetCell(10, bss.HostTimestamp.ToString());
            row.SetCell(11, bss.Capability.ToString());
            row.SetCell(12, bss.ChCenterFrequency.ToString());

            string s = "";
            if (bss.IeRaw != null)
                for (int x = 0; x < bss.IeRaw.Length; x++)
                    s += bss.IeRaw[x].ToString("X2");
            row.SetCell(13, s);
        }
    }

    private wclWiFiBssType GetNetworkBssType()
    {
        if (_selectedNetwork == null)
            return (wclWiFiBssType)bssTypeDropdown.value;

        string bss = _selectedNetwork.GetCell(2);
        if (bss == "bssInfrastructure") return wclWiFiBssType.bssInfrastructure;
        if (bss == "bssIndependent")    return wclWiFiBssType.bssIndependent;
        return wclWiFiBssType.bssAny;
    }

    // ==================================================================
    //  Profiles
    // ==================================================================
    private void OnEnumProfilesClicked() => EnumProfiles();

    private void EnumProfiles()
    {
        ClearChildren(profilesContent);
        _selectedProfile = null;

        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        wclWiFiProfileData[] profiles;
        if (!ShowResult(_profilesManager.GetProfileList(id, out profiles))) return;
        if (profiles == null) return;

        foreach (var p in profiles)
        {
            var rowGO = Instantiate(profileRowPrefab, profilesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnProfileRowClicked);
            row.Tag = p;
            row.SetCell(0, p.Name);
            row.SetCell(1, p.Flags.ToString());
        }
    }

    private void OnDeleteProfileClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (_selectedProfile == null) { Log("Select profile"); return; }

        string name = _selectedProfile.GetCell(0);
        if (ShowResult(_profilesManager.DeleteProfile(id, name)))
            EnumProfiles();
    }

    private void OnRenameProfileClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (_selectedProfile == null) { Log("Select profile"); return; }

        string name = _selectedProfile.GetCell(0);
        if (ShowResult(_profilesManager.RenameProfile(id, name, "NEW_" + name)))
            EnumProfiles();
    }

    private void OnGetProfileXmlClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (_selectedProfile == null) { Log("Select profile"); return; }

        string name = _selectedProfile.GetCell(0);

        wclWiFiProfileFlag flags = 0;
        if (plainTextToggle.isOn) flags |= wclWiFiProfileFlag.pfGetPlaintextKey;

        string xml;
        if (!ShowResult(_profilesManager.GetProfile(id, name, ref flags, out xml))) return;

        string target = PickSaveFile(name + ".xml");
        if (string.IsNullOrEmpty(target)) return;

        try { File.WriteAllText(target, xml); Log("Profile saved to: " + target); }
        catch (Exception e) { Log("Save XML failed: " + e.Message); }
    }

    private void OnSetProfileXmlClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;

        string source = PickOpenFile("xml");
        if (string.IsNullOrEmpty(source)) return;

        try
        {
            string xml = File.ReadAllText(source);
            ShowResult(_profilesManager.SetProfile(id, 0, xml, true));
        }
        catch (Exception e) { Log("Load XML failed: " + e.Message); }
    }

    private void OnSetXmlUserDataClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (_selectedProfile == null) { Log("Select profile"); return; }

        string name = _selectedProfile.GetCell(0);

        string source = PickOpenFile("xml");
        if (string.IsNullOrEmpty(source)) return;

        try
        {
            string xml = File.ReadAllText(source);
            ShowResult(_profilesManager.SetProfileEapXmlUserData(id, name, false, xml));
        }
        catch (Exception e) { Log("Load user data failed: " + e.Message); }
    }

    private void OnEditUiClicked()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (_selectedProfile == null) { Log("Select profile"); return; }

        string name = _selectedProfile.GetCell(0);
        ShowResult(_profilesManager.ShowUIEdit(id, name));
    }

    // ==================================================================
    //  Connect
    // ==================================================================
    private void OnConnectClicked()
    {
        switch (connectModeDropdown.value)
        {
            case 0: ConnectUsingNetworkProfile(); break;
            case 1: ConnectUsingSelectedProfile(); break;
            case 2: ConnectUsingSelectedNetwork(); break;
            case 3: ConnectUsingTemporaryProfile(); break;
        }
    }

    private void ConnectUsingNetworkProfile()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (!ValidateNetworkAndBss(out string mac)) return;

        ShowResult(_client.Connect(id, wclWiFiConnectionMode.cmProfile,
            _selectedNetwork.GetCell(0), "", GetNetworkBssType(), 0, mac));
    }

    private void ConnectUsingSelectedProfile()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (!ValidateNetworkAndBss(out string mac)) return;
        if (_selectedProfile == null) { Log("Select profile"); return; }

        ShowResult(_client.Connect(id, wclWiFiConnectionMode.cmProfile,
            _selectedProfile.GetCell(0), "", GetNetworkBssType(), 0, mac));
    }

    private void ConnectUsingSelectedNetwork()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (!ValidateNetworkAndBss(out string mac)) return;

        var mode = discoverSecureToggle.isOn
            ? wclWiFiConnectionMode.cmDiscoverySecure
            : wclWiFiConnectionMode.cmDiscoveryUnsecure;

        ShowResult(_client.Connect(id, mode, "", _selectedNetwork.GetCell(1),
            GetNetworkBssType(), 0, mac));
    }

    private void ConnectUsingTemporaryProfile()
    {
        Guid id = GetSelectedInterfaceId();
        if (id == Guid.Empty) return;
        if (!ValidateNetworkAndBss(out string mac)) return;

        string ssid = _selectedNetwork.GetCell(1);
        string hex = _profilesManager.SsidToHex(ssid);

        string authAlg   = AuthAlgToXml(_selectedNetwork.GetCell(10));
        string cipherAlg = CipherAlgToXml(_selectedNetwork.GetCell(11));

        string xml = "<?xml version=\"1.0\"?>"
            + "<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">"
            + "<name>" + ssid + "</name>"
            + "<SSIDConfig><SSID><hex>" + hex + "</hex><name>" + ssid + "</name></SSID></SSIDConfig>"
            + "<connectionType>ESS</connectionType>"
            + "<connectionMode>manual</connectionMode>"
            + "<autoSwitch>false</autoSwitch>"
            + "<MSM><security><authEncryption>"
            + "<authentication>" + authAlg + "</authentication>"
            + "<encryption>" + cipherAlg + "</encryption>"
            + "<useOneX>false</useOneX></authEncryption>"
            + "<sharedKey><keyType>passPhrase</keyType><protected>false</protected>"
            + "<keyMaterial>" + passkeyInput.text + "</keyMaterial></sharedKey>"
            + "</security></MSM></WLANProfile>";

        ShowResult(_client.Connect(id, wclWiFiConnectionMode.cmTemporaryProfile, xml,
            ssid, GetNetworkBssType(), 0, mac));
    }

    private bool ValidateNetworkAndBss(out string mac)
    {
        mac = "";
        if (_selectedNetwork == null) { Log("Select network"); return false; }

        if (useBssMacToggle.isOn)
        {
            if (_selectedBss == null) { Log("Select Bss"); return false; }
            mac = _selectedBss.GetCell(2);
        }
        return true;
    }

    private static string AuthAlgToXml(string s)
    {
        switch (s)
        {
            case "auOpen":       return "open";
            case "auSharedKey":  return "shared";
            case "auWpa":        return "WPA";
            case "auWpaPsk":     return "WPAPSK";
            case "auRsna":       return "WPA2";
            case "auWpa3":       return "WPA3ENT192";
            case "auWpa3Sae":    return "WPA3SAE";
            case "auWpa3Ent":    return "WPA3ENT";
            case "auOwe":        return "OWE";
            default:             return "WPA2PSK";
        }
    }

    private static string CipherAlgToXml(string s)
    {
        switch (s)
        {
            case "caNone":   return "none";
            case "caTkip":   return "TKIP";
            case "caWep40":
            case "caWep104":
            case "caWep":    return "WEP";
            case "caGcmp256": return "GCMP256";
            default:          return "AES";
        }
    }

    // ==================================================================
    //  ACM
    // ==================================================================
    private void OnGetShowDeniedClicked()
    {
        bool enabled;
        if (ShowResult(_client.AcmGetShowDeniedNetworks(out enabled)))
            Log(enabled ? "Show denied networks" : "Do not show denied networks");
    }

    private void OnSetShowDeniedClicked()
    {
        ShowConfirm("Show denied networks?", res =>
        {
            if (res == 0) return;
            ShowResult(_client.AcmSetShowDeniedNetworks(res == 1));
        });
    }

    private void OnGetPowerSettingsClicked()
    {
        wclWiFiPowerSetting setting;
        if (ShowResult(_client.AcmGetPowerSetting(out setting)))
            Log(setting.ToString());
    }

    private void OnGetOnlyGpClicked()
    {
        bool enabled;
        if (ShowResult(_client.AcmGetOnlyUseGpProfiles(out enabled)))
            Log(enabled ? "Only use GP profiles ENABLED" : "Only use GP profiles DISABLED");
    }

    private void OnGetAllowExplicitClicked()
    {
        bool enabled;
        if (ShowResult(_client.AcmGetAllowExplicitCreds(out enabled)))
            Log(enabled ? "Explicit creds ALLOWED" : "Explicit creds NOT ALLOWED");
    }

    private void OnSetAllowExplicitClicked()
    {
        ShowConfirm("Allow explicit creds?", res =>
        {
            if (res == 0) return;
            ShowResult(_client.AcmSetAllowExplicitCreds(res == 1));
        });
    }

    private void OnGetBlockPeriodClicked()
    {
        uint period;
        if (ShowResult(_client.AcmGetBlockPeriod(out period)))
            Log("Block period: " + period);
    }

    private void OnSetBlockPeriodClicked()
    {
        ShowResult(_client.AcmSetBlockPeriod(10));
    }

    private void OnGetVirtualStationClicked()
    {
        bool allowed;
        if (ShowResult(_client.AcmGetVirtualStationExtensibility(out allowed)))
            Log(allowed
                ? "Virtual station extensibility ALLOWED"
                : "Virtual station extensibility NOT ALLOWED");
    }

    private void OnSetVirtualStationClicked()
    {
        ShowConfirm("Allow virtual station extensibility?", res =>
        {
            if (res == 0) return;
            ShowResult(_client.AcmSetVirtualStationExtensibility(res == 1));
        });
    }

    // ==================================================================
    //  Confirm panel
    // ==================================================================
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
        cb?.Invoke(result);
    }

    // ==================================================================
    //  Rows selection
    // ==================================================================
    private void OnInterfaceRowClicked(TableRowUI row)
    {
        if (_selectedInterface != null) _selectedInterface.SetSelected(false);
        _selectedInterface = row;
        if (_selectedInterface != null) _selectedInterface.SetSelected(true);
    }

    private void OnNetworkRowClicked(TableRowUI row)
    {
        if (_selectedNetwork != null) _selectedNetwork.SetSelected(false);
        _selectedNetwork = row;
        if (_selectedNetwork != null) _selectedNetwork.SetSelected(true);
    }

    private void OnBssRowClicked(TableRowUI row)
    {
        if (_selectedBss != null) _selectedBss.SetSelected(false);
        _selectedBss = row;
        if (_selectedBss != null) _selectedBss.SetSelected(true);
    }

    private void OnProfileRowClicked(TableRowUI row)
    {
        if (_selectedProfile != null) _selectedProfile.SetSelected(false);
        _selectedProfile = row;
        if (_selectedProfile != null) _selectedProfile.SetSelected(true);
    }

    // ==================================================================
    //  UI enabling
    // ==================================================================
    private void UpdateUI(bool enable)
    {
        openButton.interactable  = !enable;
        closeButton.interactable = enable;

        enumInterfacesButton.interactable  = enable;
        disconnectButton.interactable      = enable;
        getInterfaceStateButton.interactable = enable;
        turnOnButton.interactable          = enable;
        turnOffButton.interactable         = enable;

        currentIpButton.interactable        = enable;
        getIpSettingsButton.interactable    = enable;
        enableDhcpButton.interactable       = enable;
        enableStaticIpButton.interactable   = enable;
        getDualStaStateButton.interactable  = enable;
        enableDualStaButton.interactable    = enable;
        disableDualStaButton.interactable   = enable;
        enumSecondaryInterfacesButton.interactable = enable;

        enumNetworksButton.interactable = enable;
        scanButton.interactable         = enable;
        enumBssButton.interactable      = enable;

        enumProfilesButton.interactable   = enable;
        deleteProfileButton.interactable  = enable;
        renameProfileButton.interactable  = enable;
        getProfileXmlButton.interactable  = enable;
        setProfileXmlButton.interactable  = enable;
        setXmlUserDataButton.interactable = enable;
        editUiButton.interactable         = enable;

        connectButton.interactable = enable;

        getShowDeniedButton.interactable     = enable;
        getPowerSettingsButton.interactable  = enable;
        getAllowExplicitButton.interactable  = enable;
        getBlockPeriodButton.interactable    = enable;
        getVirtualStationButton.interactable = enable;

        setShowDeniedButton.interactable     = enable;
        getOnlyGpButton.interactable         = enable;
        setAllowExplicitButton.interactable  = enable;
        setBlockPeriodButton.interactable    = enable;
        setVirtualStationButton.interactable = enable;
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private Guid GetSelectedInterfaceId()
    {
        if (_selectedInterface == null) { Log("Select interface"); return Guid.Empty; }
        try { return new Guid(_selectedInterface.GetCell(0)); }
        catch { Log("Invalid interface id"); return Guid.Empty; }
    }

    private bool ShowResult(Int32 res)
    {
        if (res == wclErrors.WCL_E_SUCCESS) return true;
        Log("Error: 0x" + res.ToString("X8"));
        return false;
    }

    private static bool ParseBool(string s) => bool.TryParse(s, out var v) && v;

    private static string PickOpenFile(string ext)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel("Open " + ext, "", ext);
#else
        return Path.Combine(Application.persistentDataPath, "profile." + ext);
#endif
    }

    private static string PickSaveFile(string defaultName)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.SaveFilePanel("Save XML", "", defaultName, "xml");
#else
        return Path.Combine(Application.persistentDataPath, defaultName);
#endif
    }

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
        Debug.Log("[WiFiClient] " + msg);
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