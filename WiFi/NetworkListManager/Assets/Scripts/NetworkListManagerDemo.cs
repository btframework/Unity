using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class NetworkListManagerDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button openButton;
    public Button closeButton;
    public Button isConnectedButton;
    public Button isConnectedToInternetButton;
    public Button connectivityButton;
    public TMP_Dropdown networkTypeDropdown;
    public Button enumNetworksButton;
    public Button refreshButton;

    // ---------------- Networks table ----------------
    public Transform  networksContent;
    public GameObject networkRowPrefab;

    // ---------------- Edit row ----------------
    public TMP_InputField newNameOrDescriptionInput;
    public Button         setNameButton;
    public Button         setDescriptionButton;
    public TMP_Dropdown   categoryDropdown;
    public Button         setCategoryButton;
    public Button         deleteButton;

    // ---------------- Connections row ----------------
    public Button getMachineConnectionsButton;
    public Button getNetworkConnectionsButton;
    public Button enumAdaptersButton;

    public Transform  connectionsContent;
    public GameObject connectionRowPrefab;

    // ---------------- Log ----------------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;
    public Button     clearEventsButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclNetworkListManager _manager;
    private TableRowUI _selectedNetwork;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        openButton                    .onClick.AddListener(OnOpenClicked);
        closeButton                   .onClick.AddListener(OnCloseClicked);
        isConnectedButton             .onClick.AddListener(OnIsConnectedClicked);
        isConnectedToInternetButton   .onClick.AddListener(OnIsConnectedToInternetClicked);
        connectivityButton            .onClick.AddListener(OnConnectivityClicked);
        enumNetworksButton            .onClick.AddListener(OnEnumNetworksClicked);
        refreshButton                 .onClick.AddListener(OnRefreshClicked);
        setNameButton                 .onClick.AddListener(OnSetNameClicked);
        setDescriptionButton          .onClick.AddListener(OnSetDescriptionClicked);
        setCategoryButton             .onClick.AddListener(OnSetCategoryClicked);
        deleteButton                  .onClick.AddListener(OnDeleteClicked);
        getMachineConnectionsButton   .onClick.AddListener(OnGetMachineConnectionsClicked);
        getNetworkConnectionsButton   .onClick.AddListener(OnGetNetworkConnectionsClicked);
        enumAdaptersButton            .onClick.AddListener(OnEnumAdaptersClicked);
        clearEventsButton             .onClick.AddListener(() => ClearChildren(eventsContent));
    }

    private void Start()
    {
        EnsureDropdown(networkTypeDropdown, new[] { "Connected", "Disconnected", "All" }, 2);
        EnsureDropdown(categoryDropdown,    new[] { "Public", "Private", "Domain Authenticated" }, 0);

        _manager = new wclNetworkListManager();
        _manager.AfterOpen                       += ManagerAfterOpen;
        _manager.BeforeClose                     += ManagerBeforeClose;
        _manager.OnConnectivityChanged           += ManagerOnConnectivityChanged;
        _manager.OnNetworkAdded                  += ManagerOnNetworkAdded;
        _manager.OnNetworkConnectivityChanged    += ManagerOnNetworkConnectivityChanged;
        _manager.OnNetworkDeleted                += ManagerOnNetworkDeleted;
        _manager.OnNetworkPropertyChanged        += ManagerOnNetworkPropertyChanged;
        _manager.OnConnectionConnectivityChanged += ManagerOnConnectionConnectivityChanged;
        _manager.OnConnectionPropertyChanged     += ManagerOnConnectionPropertyChanged;
    }

    private static void EnsureDropdown(TMP_Dropdown dd, string[] options, int defaultValue)
    {
        if (dd == null) return;

        if (dd.options == null || dd.options.Count != options.Length)
        {
            dd.ClearOptions();
            var list = new System.Collections.Generic.List<string>(options);
            dd.AddOptions(list);
        }

        dd.value = Mathf.Clamp(defaultValue, 0, options.Length - 1);
        dd.RefreshShownValue();
    }

    private void OnDestroy()
    {
        try { _manager?.Close(); } catch (Exception e) { Debug.LogException(e); }

        if (_manager != null)
        {
            _manager.AfterOpen                       -= ManagerAfterOpen;
            _manager.BeforeClose                     -= ManagerBeforeClose;
            _manager.OnConnectivityChanged           -= ManagerOnConnectivityChanged;
            _manager.OnNetworkAdded                  -= ManagerOnNetworkAdded;
            _manager.OnNetworkConnectivityChanged    -= ManagerOnNetworkConnectivityChanged;
            _manager.OnNetworkDeleted                -= ManagerOnNetworkDeleted;
            _manager.OnNetworkPropertyChanged        -= ManagerOnNetworkPropertyChanged;
            _manager.OnConnectionConnectivityChanged -= ManagerOnConnectionConnectivityChanged;
            _manager.OnConnectionPropertyChanged     -= ManagerOnConnectionPropertyChanged;
        }
        _manager = null;
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void ManagerAfterOpen(object s, EventArgs e) => Log("AfterOpen");
    private void ManagerBeforeClose(object s, EventArgs e) => Log("BeforeClose");

    private void ManagerOnConnectivityChanged(object s, wclNlmConnectivity connectivity)
        => Log("Connectivity changed: " + connectivity);

    private void ManagerOnNetworkAdded(object s, Guid networkId)
        => Log("Network added: " + networkId);

    private void ManagerOnNetworkConnectivityChanged(object s, Guid networkId, wclNlmConnectivity connectivity)
        => Log("Network " + networkId + " connectivity changed: " + connectivity);

    private void ManagerOnNetworkDeleted(object s, Guid networkId)
        => Log("Network deleted: " + networkId);

    private void ManagerOnNetworkPropertyChanged(object s, Guid networkId, wclNlmNetworkPropertyChange change)
        => Log("Network " + networkId + " property changed: " + change);

    private void ManagerOnConnectionConnectivityChanged(object s, Guid connectionId, wclNlmConnectivity connectivity)
        => Log("Connection: " + connectionId + " connectivity changed: " + connectivity);

    private void ManagerOnConnectionPropertyChanged(object s, Guid connectionId, wclNlmConnectionProperty prop)
        => Log("Connection: " + connectionId + " property changed: " + prop);

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnOpenClicked()  => ShowError(_manager.Open());
    private void OnCloseClicked() => ShowError(_manager.Close());

    private void OnIsConnectedClicked()
    {
        bool connected;
        if (ShowError(_manager.GetConnected(out connected))) return;
        Log(connected ? "Connected" : "NOT Connected");
    }

    private void OnIsConnectedToInternetClicked()
    {
        bool connected;
        if (ShowError(_manager.GetConnectedToInternet(out connected))) return;
        Log(connected ? "Connected" : "NOT Connected");
    }

    private void OnConnectivityClicked()
    {
        wclNlmConnectivity connectivity;
        if (ShowError(_manager.GetConnectivity(out connectivity))) return;
        Log("Connectivity: " + connectivity);
    }

    private void OnEnumNetworksClicked()
    {
        ClearChildren(networksContent);
        _selectedNetwork = null;

        if (_manager == null) { Log("Manager is null"); return; }

        int flagValue = networkTypeDropdown.value;
        var flag = (wclNlmEnumNetwork)flagValue;

        Log($"Enum networks: dropdown.value={flagValue}, options={networkTypeDropdown.options.Count}, manager.Active={_manager.Active}");

        wclNlmNetwork[] networks;
        Int32 res = _manager.GetNetworks(flag, out networks);

        Log($"GetNetworks: res=0x{res:X8}, count={(networks == null ? 0 : networks.Length)}");

        if (ShowError(res)) return;
        if (networks == null || networks.Length == 0)
        {
            Log("No networks returned");
            return;
        }

        foreach (var network in networks)
        {
            var rowGO = Instantiate(networkRowPrefab, networksContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnNetworkRowClicked);
            row.Tag = network;

            Guid id;
            res = network.GetNetworkId(out id);
            row.SetCell(0, res == wclErrors.WCL_E_SUCCESS ? id.ToString() : "Error: 0x" + res.ToString("X8"));

            wclNlmNetworkCategory category;
            res = network.GetCategory(out category);
            row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? category.ToString() : "Error: 0x" + res.ToString("X8"));

            wclNlmConnectivity connectivity;
            res = network.GetConnectivity(out connectivity);
            row.SetCell(2, res == wclErrors.WCL_E_SUCCESS ? connectivity.ToString() : "Error: 0x" + res.ToString("X8"));

            string description;
            res = network.GetDescription(out description);
            row.SetCell(3, res == wclErrors.WCL_E_SUCCESS ? description : "Error: 0x" + res.ToString("X8"));

            wclNlmDomainType domain;
            res = network.GetDomainType(out domain);
            row.SetCell(4, res == wclErrors.WCL_E_SUCCESS ? domain.ToString() : "Error: 0x" + res.ToString("X8"));

            string name;
            res = network.GetName(out name);
            row.SetCell(5, res == wclErrors.WCL_E_SUCCESS ? name : "Error: 0x" + res.ToString("X8"));

            DateTime created, connectedTime;
            res = network.GetTimeCreatedAndConnected(out created, out connectedTime);
            if (res == wclErrors.WCL_E_SUCCESS)
            {
                row.SetCell(6, created.ToString());
                row.SetCell(7, connectedTime.ToString());
            }
            else
            {
                row.SetCell(6, "Error: 0x" + res.ToString("X8"));
                row.SetCell(7, "Error: 0x" + res.ToString("X8"));
            }

            bool connected;
            res = network.GetConnected(out connected);
            row.SetCell(8, res == wclErrors.WCL_E_SUCCESS ? connected.ToString() : "Error: 0x" + res.ToString("X8"));

            res = network.GetConnectedToInternet(out connected);
            row.SetCell(9, res == wclErrors.WCL_E_SUCCESS ? connected.ToString() : "Error: 0x" + res.ToString("X8"));
        }
    }

    private void OnRefreshClicked()
    {
        if (_selectedNetwork == null) { Log("Select network"); return; }

        var row = _selectedNetwork;
        var network = row.Tag as wclNlmNetwork;
        if (network == null) return;

        RefreshNetworkRow(row, network);
    }

    private void RefreshNetworkRow(TableRowUI row, wclNlmNetwork network)
    {
        wclNlmNetworkCategory category;
        Int32 res = network.GetCategory(out category);
        row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? category.ToString() : "Error: 0x" + res.ToString("X8"));

        wclNlmConnectivity connectivity;
        res = network.GetConnectivity(out connectivity);
        row.SetCell(2, res == wclErrors.WCL_E_SUCCESS ? connectivity.ToString() : "Error: 0x" + res.ToString("X8"));

        string description;
        res = network.GetDescription(out description);
        row.SetCell(3, res == wclErrors.WCL_E_SUCCESS ? description : "Error: 0x" + res.ToString("X8"));

        wclNlmDomainType domain;
        res = network.GetDomainType(out domain);
        row.SetCell(4, res == wclErrors.WCL_E_SUCCESS ? domain.ToString() : "Error: 0x" + res.ToString("X8"));

        string name;
        res = network.GetName(out name);
        row.SetCell(5, res == wclErrors.WCL_E_SUCCESS ? name : "Error: 0x" + res.ToString("X8"));

        DateTime created, connectedTime;
        res = network.GetTimeCreatedAndConnected(out created, out connectedTime);
        if (res == wclErrors.WCL_E_SUCCESS)
        {
            row.SetCell(6, created.ToString());
            row.SetCell(7, connectedTime.ToString());
        }
        else
        {
            row.SetCell(6, "Error: 0x" + res.ToString("X8"));
            row.SetCell(7, "Error: 0x" + res.ToString("X8"));
        }

        bool connected;
        res = network.GetConnected(out connected);
        row.SetCell(8, res == wclErrors.WCL_E_SUCCESS ? connected.ToString() : "Error: 0x" + res.ToString("X8"));

        res = network.GetConnectedToInternet(out connected);
        row.SetCell(9, res == wclErrors.WCL_E_SUCCESS ? connected.ToString() : "Error: 0x" + res.ToString("X8"));
    }

    private void OnSetNameClicked()
    {
        if (_selectedNetwork == null) { Log("Select network"); return; }

        var network = _selectedNetwork.Tag as wclNlmNetwork;
        if (network == null) return;

        if (ShowError(network.SetName(newNameOrDescriptionInput.text)))
            Log("Admin privilegies required");
    }

    private void OnSetDescriptionClicked()
    {
        if (_selectedNetwork == null) { Log("Select network"); return; }

        var network = _selectedNetwork.Tag as wclNlmNetwork;
        if (network == null) return;

        if (ShowError(network.SetDescription(newNameOrDescriptionInput.text)))
            Log("Admin privilegies required");
    }

    private void OnSetCategoryClicked()
    {
        if (_selectedNetwork == null) { Log("Select network"); return; }

        var network = _selectedNetwork.Tag as wclNlmNetwork;
        if (network == null) return;

        var category = (wclNlmNetworkCategory)categoryDropdown.value;
        if (ShowError(network.SetCategory(category)))
            Log("Admin privilegies required");
    }

    private void OnDeleteClicked()
    {
        if (_selectedNetwork == null) { Log("Select network"); return; }

        Guid id;
        try { id = new Guid(_selectedNetwork.GetCell(0)); }
        catch { Log("Invalid network id"); return; }

        Int32 res = _manager.DeleteNetwork(id);
        if (ShowError(res)) return;

        Destroy(_selectedNetwork.gameObject);
        _selectedNetwork = null;
    }

    private void OnGetMachineConnectionsClicked()
    {
        wclNlmConnection[] connections;
        if (ShowError(_manager.GetConnections(out connections))) return;
        ShowConnections(connections);
    }

    private void OnGetNetworkConnectionsClicked()
    {
        if (_selectedNetwork == null) { Log("Select network"); return; }

        var network = _selectedNetwork.Tag as wclNlmNetwork;
        if (network == null) return;

        wclNlmConnection[] connections;
        if (ShowError(network.GetConnections(out connections))) return;
        ShowConnections(connections);
    }

    private void OnEnumAdaptersClicked()
    {
        wclNetworkAdapter[] adapters;
        if (ShowError(_manager.EnumAdapters(out adapters))) return;

        ClearChildren(connectionsContent);

        if (adapters == null) return;

        foreach (var adapter in adapters)
        {
            var rowGO = Instantiate(connectionRowPrefab, connectionsContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(null);

            row.SetCell(0, adapter.Id);
            row.SetCell(1, adapter.FriendlyName);
            row.SetCell(2, adapter.Description);
            row.SetCell(3, adapter.Mac);

            string ips = "";
            if (adapter.IPs != null)
                foreach (var ip in adapter.IPs)
                    AddFlag(ref ips, ip);
            row.SetCell(4, "[" + ips + "]");

            row.SetCell(5, adapter.DnsSuffix);
            row.SetCell(6, adapter.Mtu.ToString());

            string flags = "";
            if ((adapter.Flags & wclNetworkAdapterFlag.afDdnsEnabled)               != 0) AddFlag(ref flags, "afDdnsEnabled");
            if ((adapter.Flags & wclNetworkAdapterFlag.afRegisterSuffix)            != 0) AddFlag(ref flags, "afRegisterSuffix");
            if ((adapter.Flags & wclNetworkAdapterFlag.afDhcpEnabled)               != 0) AddFlag(ref flags, "afDhcpEnabled");
            if ((adapter.Flags & wclNetworkAdapterFlag.afReceiveOnly)               != 0) AddFlag(ref flags, "afReceiveOnly");
            if ((adapter.Flags & wclNetworkAdapterFlag.afNoMulticast)               != 0) AddFlag(ref flags, "afNoMulticast");
            if ((adapter.Flags & wclNetworkAdapterFlag.afIpv6OtherStatefulConfig)   != 0) AddFlag(ref flags, "afIpv6OtherStatefulConfig");
            if ((adapter.Flags & wclNetworkAdapterFlag.afNetbiosOverTcpIpEnabled)   != 0) AddFlag(ref flags, "afNetbiosOverTcpIpEnabled");
            if ((adapter.Flags & wclNetworkAdapterFlag.afIpv4Enabled)               != 0) AddFlag(ref flags, "afIpv4Enabled");
            if ((adapter.Flags & wclNetworkAdapterFlag.afIpv6Enabled)               != 0) AddFlag(ref flags, "afIpv6Enabled");
            if ((adapter.Flags & wclNetworkAdapterFlag.afIpv6ManageAddressConfig)   != 0) AddFlag(ref flags, "afIpv6ManageAddressConfig");
            row.SetCell(7, "[" + flags + "]");

            row.SetCell(8, AdapterStatusStr(adapter.Status));
        }
    }

    private void ShowConnections(wclNlmConnection[] connections)
    {
        ClearChildren(connectionsContent);

        if (connections == null) return;

        foreach (var connection in connections)
        {
            var rowGO = Instantiate(connectionRowPrefab, connectionsContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(null);

            Guid id;
            Int32 res = connection.GetAdapterId(out id);
            row.SetCell(0, res == wclErrors.WCL_E_SUCCESS ? id.ToString() : "Error: 0x" + res.ToString("X8"));

            bool connected;
            res = connection.GetConnected(out connected);
            row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? connected.ToString() : "Error: 0x" + res.ToString("X8"));

            res = connection.GetConnectedToInternet(out connected);
            row.SetCell(2, res == wclErrors.WCL_E_SUCCESS ? connected.ToString() : "Error: 0x" + res.ToString("X8"));

            wclNlmConnectivity connectivity;
            res = connection.GetConnectivity(out connectivity);
            row.SetCell(3, res == wclErrors.WCL_E_SUCCESS ? connectivity.ToString() : "Error: 0x" + res.ToString("X8"));

            wclNlmDomainType domain;
            res = connection.GetDomainType(out domain);
            row.SetCell(4, res == wclErrors.WCL_E_SUCCESS ? domain.ToString() : "Error: 0x" + res.ToString("X8"));

            res = connection.GetId(out id);
            row.SetCell(5, res == wclErrors.WCL_E_SUCCESS ? id.ToString() : "Error: 0x" + res.ToString("X8"));
        }
    }

    // ==================================================================
    //  Row selection
    // ==================================================================
    private void OnNetworkRowClicked(TableRowUI row)
    {
        if (_selectedNetwork != null) _selectedNetwork.SetSelected(false);
        _selectedNetwork = row;
        if (_selectedNetwork != null) _selectedNetwork.SetSelected(true);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private bool ShowError(Int32 error)
    {
        if (error != wclErrors.WCL_E_SUCCESS)
        {
            Log("Error: 0x" + error.ToString("X8"));
            return true;
        }
        return false;
    }

    private static void AddFlag(ref string str, string flag)
    {
        if (str.Length > 0) str += ", ";
        str += flag;
    }

    private static string AdapterStatusStr(wclNetworkInterfaceStatus status)
    {
        switch (status)
        {
            case wclNetworkInterfaceStatus.isUp:             return "Up";
            case wclNetworkInterfaceStatus.isDown:           return "Down";
            case wclNetworkInterfaceStatus.isTesting:        return "Testing";
            case wclNetworkInterfaceStatus.isDormant:        return "Dormant";
            case wclNetworkInterfaceStatus.isNotPresent:     return "Not present";
            case wclNetworkInterfaceStatus.isLowerLayerDown: return "Low Layer Down";
            default:                                          return "Unknown";
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
        Debug.Log("[NetworkListManager] " + msg);
        if (eventsContent == null || eventRowPrefab == null) return;

        var go = Instantiate(eventRowPrefab, eventsContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, msg);

        var sr = eventsContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }
}