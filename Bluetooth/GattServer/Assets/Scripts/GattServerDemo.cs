using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;

public class GattServerDemo : MonoBehaviour
{
    // ---------------- Top ----------------
    public Button startButton;
    public Button stopButton;
    public Button disconnectButton;

    // ---------------- Clients tree ----------------
    public Transform  clientsContent;
    public GameObject clientNodePrefab;      // empty container with VerticalLayoutGroup
    public GameObject clientRowPrefab;       // client row (13 columns)
    public GameObject subscribedRowPrefab;   // child row (characteristic)

    // ---------------- Notify ----------------
    public TMP_InputField dataInput;
    public TMP_InputField repeatsInput;
    public Button         notifyButton;
    public Button         notifyAllButton;

    // ---------------- Params ----------------
    public TMP_Dropdown paramsDropdown;
    public Button       setParametersButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;
    public Button     clearLogButton;

    // ==================================================================
    //  State
    // ==================================================================
    private const ushort SHORT_DATA_LEN = 5;
    private const ushort LONG_DATA_LEN  = 1024;

    private readonly byte[] _shortData = new byte[SHORT_DATA_LEN];
    private readonly byte[] _longData  = new byte[LONG_DATA_LEN];

    private wclBluetoothManager _manager;
    private wclGattServer _server;

    private class ClientNode
    {
        public wclGattServerClient Client;
        public GameObject Container;
        public TableRowUI Row;                        // client row
        public readonly List<TableRowUI> Subs = new List<TableRowUI>();
    }

    private readonly List<ClientNode> _clients = new List<ClientNode>();
    private TableRowUI _selectedRow;                  // may be a client row or a subscribed (characteristic) row

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        startButton       .onClick.AddListener(OnStartClicked);
        stopButton        .onClick.AddListener(OnStopClicked);
        disconnectButton  .onClick.AddListener(OnDisconnectClicked);
        notifyButton      .onClick.AddListener(OnNotifyClicked);
        notifyAllButton   .onClick.AddListener(OnNotifyAllClicked);
        setParametersButton.onClick.AddListener(OnSetParametersClicked);
        clearLogButton    .onClick.AddListener(() => ClearChildren(logContent));

        dataInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        repeatsInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // Initialize data - same as in fmMain_Load
        for (uint i = 0; i < SHORT_DATA_LEN; i++)
            _shortData[i] = wclHelpers.LoByte(wclHelpers.LoWord(i + 1));
        for (uint i = 0; i < LONG_DATA_LEN; i++)
            _longData[i] = wclHelpers.LoByte(wclHelpers.LoWord(i));
    }

    private void Start()
    {
        paramsDropdown.value = 0;

        _manager = new wclBluetoothManager();
        _manager.AfterOpen    += ManagerAfterOpen;
        _manager.BeforeClose  += ManagerBeforeClose;
        _manager.OnClosed     += ManagerClosed;

        _server = new wclGattServer();
        _server.OnStarted                    += ServerStarted;
        _server.OnStopped                    += ServerStopped;
        _server.OnClientConnected            += ServerClientConnected;
        _server.OnMaxPduSizeChanged          += ServerMaxPduSizeChanged;
        _server.OnConnectionParamsChanged    += ServerConnectionParamsChanged;
        _server.OnConnectionPhyChanged       += ServerConnectionPhyChanged;
        _server.OnNotificationSizeChanged    += ServerNotificationSizeChanged;
        _server.OnClientDisconnected         += ServerClientDisconnected;
        _server.OnWrite                      += ServerWrite;
        _server.OnSubscribed                 += ServerSubscribed;
        _server.OnUnsubscribed               += ServerUnsubscribed;
        _server.OnRead                       += ServerRead;

        stopButton.interactable = false;
        disconnectButton.interactable = false;
        notifyButton.interactable = false;
        notifyAllButton.interactable = false;
        setParametersButton.interactable = false;
    }

    private void OnDestroy()
    {
        try { StopServer(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void ManagerAfterOpen(object s, EventArgs e)   => Trace("Bluetooth Manager opened");
    private void ManagerBeforeClose(object s, EventArgs e) => Trace("Bluetooth Manager is closing");
    private void ManagerClosed(object s, EventArgs e)      => Trace("Bluetooth Manager closed");

    // ==================================================================
    //  Server state
    // ==================================================================
    private void ServerStarted(object s, EventArgs e)
    {
        stopButton.interactable = true;
        startButton.interactable = false;
        Trace("Server started");
    }

    private void ServerStopped(object s, EventArgs e)
    {
        stopButton.interactable = false;
        startButton.interactable = true;
        Trace("Server stopped");
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ServerClientConnected(object s, wclGattServerClient client)
    {
        Trace("Client connected: " + client.Address.ToString("X12"));

        var node = new ClientNode { Client = client };

        node.Container = Instantiate(clientNodePrefab, clientsContent);

        var rowGO = Instantiate(clientRowPrefab, node.Container.transform);
        node.Row = rowGO.GetComponent<TableRowUI>();
        node.Row.Init(OnClientRowClicked);
        node.Row.Tag = node;
        node.Row.SetCell(0, client.Address.ToString("X12"));

        _clients.Add(node);

        UpdateMaxPduSize(client);
        UpdateConnectionParams(client);
        UpdatePhy(client);
        UpdateNotificationSize(client);
    }

    private void ServerClientDisconnected(object s, wclGattServerClient client)
    {
        Trace("Client disconnected: " + client.Address.ToString("X12"));

        var node = FindNode(client);
        if (node == null) return;

        if (_selectedRow == node.Row) _selectedRow = null;
        Destroy(node.Container);
        _clients.Remove(node);

        UpdateNotifyAllStatus();
    }

    private void ServerMaxPduSizeChanged(object s, wclGattServerClient client)
    {
        UpdateMaxPduSize(client);
        Trace("Max PDU size changed: " + client.Address.ToString("X12"));
    }

    private void ServerConnectionParamsChanged(object s, wclGattServerClient client)
    {
        UpdateConnectionParams(client);
        Trace("Connection parameters changed: " + client.Address.ToString("X12"));
    }

    private void ServerConnectionPhyChanged(object s, wclGattServerClient client)
    {
        UpdatePhy(client);
        Trace("PHY changed: " + client.Address.ToString("X12"));
    }

    private void ServerNotificationSizeChanged(object s, wclGattServerClient client)
    {
        UpdateNotificationSize(client);
        Trace("Notification size changed: " + client.Address.ToString("X12"));
    }

    private void ServerSubscribed(object s, wclGattServerClient client, wclGattLocalCharacteristic ch)
    {
        Trace("Client " + client.Address.ToString("X12") + " subscribed to");
        Trace4("Characteristic: " + ch.Uuid.ShortUuid.ToString("X4"));

        var node = FindNode(client);
        if (node == null) return;

        var rowGO = Instantiate(subscribedRowPrefab, node.Container.transform);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnSubscribedRowClicked);
        row.Tag = new SubTag { Node = node, Characteristic = ch };
        row.SetCell(0, "");                             // empty first column
        row.SetCell(1, ch.Uuid.ShortUuid.ToString("X4"));
        node.Subs.Add(row);

        UpdateNotificationSize(client);
        UpdateNotifyAllStatus();
    }

    private void ServerUnsubscribed(object s, wclGattServerClient client, wclGattLocalCharacteristic ch)
    {
        Trace("Client " + client.Address.ToString("X12") + " unsubscribed from");
        Trace4("Characteristic: " + ch.Uuid.ShortUuid.ToString("X4"));

        var node = FindNode(client);
        if (node == null) return;

        for (int i = 0; i < node.Subs.Count; i++)
        {
            var tag = node.Subs[i].Tag as SubTag;
            if (tag != null && ReferenceEquals(tag.Characteristic, ch))
            {
                if (_selectedRow == node.Subs[i]) _selectedRow = null;
                Destroy(node.Subs[i].gameObject);
                node.Subs.RemoveAt(i);
                break;
            }
        }

        UpdateNotificationSize(client);
        UpdateNotifyAllStatus();
    }

    private void ServerWrite(object s, wclGattServerClient client, wclGattLocalCharacteristic ch,
        wclGattLocalCharacteristicWriteRequest request)
    {
        Trace("Write request received from: " + client.Address.ToString("X12"));
        Trace4("Characteristic: " + ch.Uuid.ShortUuid.ToString("X4"));
        Trace4("Size: " + (request.Data != null ? request.Data.Length.ToString() : "0"));
        Trace4("Offset: " + request.Offset.ToString());

        string str = "";
        if (request.Data != null)
            for (int i = 0; i < request.Data.Length; i++)
                str += request.Data[i].ToString("X2");
        Trace8(str);

        if (request.WithResponse)
        {
            Trace4("Sending response");
            Int32 res = request.Respond();
            if (res != wclErrors.WCL_E_SUCCESS) Trace8("Send response", res);
            else Trace8("Response sent");
        }
    }

    private void ServerRead(object s, wclGattServerClient client, wclGattLocalCharacteristic ch,
        wclGattLocalCharacteristicReadRequest request)
    {
        Trace("Read request received from: " + client.Address.ToString("X12"));
        Trace4("Characteristic: " + ch.Uuid.ShortUuid.ToString("X4"));
        Trace4("Offset: " + request.Offset.ToString());
        Trace4("Buffer size: " + request.Size.ToString());

        Int32 res;
        if (ch.Uuid.ShortUuid == 0xFFF2)
        {
            res = request.Respond(_shortData);
        }
        else
        {
            int offset = (int)request.Offset;
            int len = LONG_DATA_LEN - offset;
            byte[] toSend = new byte[len];
            Array.Copy(_longData, offset, toSend, 0, len);
            res = request.Respond(toSend);
        }

        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace8("Set data", res);
            Trace8("Respond with error");
            res = request.RespondWithError(wclBluetoothErrors.WCL_E_BLUETOOTH_LE_UNLIKELY);
            if (res != wclErrors.WCL_E_SUCCESS) Trace12("Respond", res);
        }
        else
        {
            Trace8("Data sent");
        }
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnStartClicked()
    {
        StartServer();
    }

    private void OnStopClicked()
    {
        StopServer();
    }

    private void OnDisconnectClicked()
    {
        var tag = _selectedRow?.Tag as ClientNode;
        if (tag == null) { Trace("Select client"); return; }

        Int32 res = tag.Client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Disconnect error: 0x" + res.ToString("X8"));
    }

    private void OnNotifyClicked()
    {
        // Notify to the client that owns the selected subscribed (characteristic) row
        var sub = _selectedRow?.Tag as SubTag;
        if (sub == null) { Trace("Select subscribed characteristic"); return; }

        Notify(sub.Node.Client.Address);
    }

    private void OnNotifyAllClicked()
    {
        Notify(0);
    }

    private void OnSetParametersClicked()
    {
        var tag = _selectedRow?.Tag as ClientNode;
        if (tag == null) { Trace("Select client"); return; }

        var client = tag.Client;
        Trace("Set connection parameters for " + client.Address.ToString("X12"));

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
            res = client.SetConnectionParams(value);
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
            res = client.SetConnectionParams(p);
        }

        if (res != wclErrors.WCL_E_SUCCESS) Trace4("Set connection parameters", res);
        else Trace4("Connection parameters set");
    }

    // ==================================================================
    //  Notify
    // ==================================================================
    private void Notify(long address)
    {
        int len = dataInput.text.Length;
        if (len == 0) { Trace("Data is empty"); return; }

        int repeats;
        if (string.IsNullOrEmpty(repeatsInput.text)) repeats = 1;
        else if (!int.TryParse(repeatsInput.text, out repeats)) { Trace("Invalid repeats"); return; }

        if (repeats > 20) { Trace("Too many notification repeats"); return; }

        // Get the characteristic from the current selection
        var sub = _selectedRow?.Tag as SubTag;
        if (sub == null) { Trace("Select subscribed characteristic"); return; }
        var ch = sub.Characteristic;

        if (address == 0) Trace("Sending notification to all subscribed clients");
        else              Trace("Sending notification to " + address.ToString("X12"));
        Trace4("Characteristic: " + ch.Uuid.ShortUuid.ToString("X4"));

        string str = dataInput.text;
        if (len % 2 != 0) { str = "0" + str; len++; }

        int dataLen = len / 2;
        byte[] data = new byte[dataLen];
        try
        {
            for (int i = 0; i < dataLen; i++)
                data[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
        }
        catch (Exception e) { Trace("Invalid hex: " + e.Message); return; }

        for (int i = 0; i < repeats; i++)
        {
            long start = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            Int32 res = (address == 0)
                ? ch.Notify(data)
                : ch.Notify(address, data);
            long end = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            if (res != wclErrors.WCL_E_SUCCESS)
                Trace8("Attempt [" + (i + 1) + "]", res);
            else
                Trace8("Attempt [" + (i + 1) + "] took " + (end - start) + " ms");
        }

        Trace("Notification procedure completed");
    }

    // ==================================================================
    //  Start / Stop sequence
    // ==================================================================
    private void StartServer()
    {
        wclBluetoothRadio radio;
        if (!InitializeBluetooth(out radio)) return;

        if (!InitializeServer(radio)) { UninitializeBluetooth(); return; }

        var service = AddService();
        if (service == null) { UninitializeServer(); UninitializeBluetooth(); return; }

        if (!AddCharacteristics(service)) { UninitializeServer(); UninitializeBluetooth(); return; }

        Trace("Starting GATT server");
        Int32 res = _server.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("GATT server start", res);
            UninitializeServer();
            UninitializeBluetooth();
        }
    }

    private void StopServer()
    {
        if (_server == null) return;

        Trace("Stopping GATT server");
        Int32 res = _server.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Stop GATT server", res);

        UninitializeServer();
        UninitializeBluetooth();
    }

    private bool InitializeBluetooth(out wclBluetoothRadio radio)
    {
        radio = null;

        Trace("Opening Bluetooth Manager");
        Int32 res = _manager.Open();
        if (res != wclErrors.WCL_E_SUCCESS) { Trace("Open Bluetooth Manager", res); return false; }

        Trace("Get working LE Radio");
        res = _manager.GetLeRadio(out radio);
        if (res != wclErrors.WCL_E_SUCCESS) { Trace("Get working LE Radio", res); _manager.Close(); return false; }

        Trace("Working radio found. Use " + radio.ApiName + " radio");
        return true;
    }

    private void UninitializeBluetooth()
    {
        if (_manager.Active)
        {
            Trace("Closing Bluetooth Manager");
            _manager.Close();
        }
    }

    private bool InitializeServer(wclBluetoothRadio radio)
    {
        Trace("Initializing GATT server");
        Int32 res = _server.Initialize(radio);
        if (res != wclErrors.WCL_E_SUCCESS) { Trace("GATT server initialization", res); return false; }
        Trace("GATT server initialized");
        return true;
    }

    private void UninitializeServer()
    {
        if (_server.Initialized)
        {
            Trace("Uninitializing GATT server");
            Int32 res = _server.Uninitialize();
            if (res != wclErrors.WCL_E_SUCCESS)
                Trace("GATT server uninitialization", res);
        }
    }

    private wclGattLocalService AddService()
    {
        Trace("Adding GATT service");

        var uuid = new wclGattUuid { IsShortUuid = true, ShortUuid = 0xFFF0 };
        wclGattLocalService service;
        Int32 res = _server.AddService(uuid, out service);
        if (res != wclErrors.WCL_E_SUCCESS) { Trace4("Add GATT service", res); return null; }

        Trace("GATT service added");
        return service;
    }

    private bool AddCharacteristics(wclGattLocalService service)
    {
        Trace4("Adding GATT characteristics");

        var uuid = new wclGattUuid { IsShortUuid = true };
        var prms = new wclGattLocalCharacteristicParameters { Descriptors = null, PresentationFormats = null };
        wclGattLocalCharacteristic ch;

        // 0xFFF1 - empty
        Trace8("Add empty characteristic");
        uuid.ShortUuid = 0xFFF1;
        prms.Props = 0;
        prms.UserDescription = "Empty characteristic";
        if (!AddCh(service, uuid, prms, out ch, 12, "Add empty characteristic")) return false;
        Trace12("Empty characteristic added");

        // 0xFFF2 - short readable
        Trace8("Add short readable characteristic");
        uuid.ShortUuid = 0xFFF2;
        prms.Props = wclGattLocalCharacteristicProperty.cpReadable;
        prms.UserDescription = "Short readable characteristic";
        if (!AddCh(service, uuid, prms, out ch, 12, "Add short readable characteristic")) return false;
        Trace12("Short readable characteristic added");

        // 0xFFF3 - long readable
        Trace8("Add long readable characteristic");
        uuid.ShortUuid = 0xFFF3;
        prms.Props = wclGattLocalCharacteristicProperty.cpReadable;
        prms.UserDescription = "Long readable characteristic";
        if (!AddCh(service, uuid, prms, out ch, 12, "Add long readable characteristic")) return false;
        Trace12("Long readable characteristic added");

        // 0xFFF4 - writable
        Trace8("Add writable characteristic");
        uuid.ShortUuid = 0xFFF4;
        prms.Props = wclGattLocalCharacteristicProperty.cpWritable |
                     wclGattLocalCharacteristicProperty.cpWritableWithoutResponse;
        prms.UserDescription = "Writable characteristic";
        if (!AddCh(service, uuid, prms, out ch, 12, "Add writable characteristic")) return false;
        Trace12("Writable characteristic added");

        // 0xFFF5 - notifiable
        Trace8("Add notifiable characteristic");
        uuid.ShortUuid = 0xFFF5;
        prms.Props = wclGattLocalCharacteristicProperty.cpNotifiable;
        prms.UserDescription = "Notifiable characteristic";
        if (!AddCh(service, uuid, prms, out ch, 12, "Add notifiable characteristic")) return false;
        Trace12("Notifiable characteristic added");

        // 0xFFF6 - indicatable
        Trace8("Add indicatable characteristic");
        uuid.ShortUuid = 0xFFF6;
        prms.Props = wclGattLocalCharacteristicProperty.cpIndicatable;
        prms.UserDescription = "Indicatable characteristic";
        if (!AddCh(service, uuid, prms, out ch, 12, "Add indicatable characteristic")) return false;
        Trace12("Indicatable characteristic added");

        Trace4("All GATT charactertistics added");
        return true;
    }

    private bool AddCh(wclGattLocalService service, wclGattUuid uuid,
        wclGattLocalCharacteristicParameters prms, out wclGattLocalCharacteristic ch,
        byte indent, string label)
    {
        Int32 res = service.AddCharacteristic(uuid, prms, out ch);
        if (res != wclErrors.WCL_E_SUCCESS) { Trace(indent, label, res); return false; }
        return true;
    }

    // ==================================================================
    //  Row helpers
    // ==================================================================
    private class SubTag
    {
        public ClientNode Node;
        public wclGattLocalCharacteristic Characteristic;
    }

    private ClientNode FindNode(wclGattServerClient client)
    {
        foreach (var n in _clients)
            if (ReferenceEquals(n.Client, client)) return n;
        return null;
    }

    private void OnClientRowClicked(TableRowUI row)
    {
        SelectRow(row);
    }

    private void OnSubscribedRowClicked(TableRowUI row)
    {
        SelectRow(row);
    }

    private void SelectRow(TableRowUI row)
    {
        // Clear selection on all rows
        foreach (var n in _clients)
        {
            n.Row.SetSelected(false);
            foreach (var s in n.Subs) s.SetSelected(false);
        }

        _selectedRow = row;
        if (_selectedRow != null) _selectedRow.SetSelected(true);

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool isClient = _selectedRow != null && _selectedRow.Tag is ClientNode;
        bool isSub    = _selectedRow != null && _selectedRow.Tag is SubTag;

        disconnectButton.interactable = isClient;
        setParametersButton.interactable = isClient;
        notifyButton.interactable = isSub;

        UpdateNotifyAllStatus();
    }

    private void UpdateNotifyAllStatus()
    {
        bool hasSub = false;
        foreach (var n in _clients)
            if (n.Subs.Count > 0) { hasSub = true; break; }

        bool isSub = _selectedRow != null && _selectedRow.Tag is SubTag;
        notifyAllButton.interactable = hasSub && isSub;
    }

    // ==================================================================
    //  Updates of client info
    // ==================================================================
    private void UpdateMaxPduSize(wclGattServerClient client)
    {
        var node = FindNode(client);
        if (node == null) return;

        ushort size;
        node.Row.SetCell(2, client.GetMaxPduSize(out size) == wclErrors.WCL_E_SUCCESS
            ? size.ToString() : "-----");
    }

    private void UpdateConnectionParams(wclGattServerClient client)
    {
        var node = FindNode(client);
        if (node == null) return;

        wclBluetoothLeConnectionParameters p;
        if (client.GetConnectionParams(out p) == wclErrors.WCL_E_SUCCESS)
        {
            node.Row.SetCell(3, p.Interval.ToString());
            node.Row.SetCell(4, p.Latency.ToString());
            node.Row.SetCell(5, p.LinkTimeout.ToString());
        }
        else
        {
            node.Row.SetCell(3, "-----");
            node.Row.SetCell(4, "-----");
            node.Row.SetCell(5, "-----");
        }
    }

    private void UpdatePhy(wclGattServerClient client)
    {
        var node = FindNode(client);
        if (node == null) return;

        wclBluetoothLeConnectionPhy phy;
        if (client.GetConnectionPhyInfo(out phy) == wclErrors.WCL_E_SUCCESS)
        {
            node.Row.SetCell(6,  phy.Receive.IsCoded.ToString());
            node.Row.SetCell(7,  phy.Receive.IsUncoded1MPhy.ToString());
            node.Row.SetCell(8,  phy.Receive.IsUncoded2MPhy.ToString());
            node.Row.SetCell(9,  phy.Transmit.IsCoded.ToString());
            node.Row.SetCell(10, phy.Transmit.IsUncoded1MPhy.ToString());
            node.Row.SetCell(11, phy.Transmit.IsUncoded2MPhy.ToString());
        }
        else
        {
            for (int i = 6; i <= 11; i++) node.Row.SetCell(i, "-----");
        }
    }

    private void UpdateNotificationSize(wclGattServerClient client)
    {
        var node = FindNode(client);
        if (node == null) return;

        ushort size;
        node.Row.SetCell(12, client.GetMaxNotificationSize(out size) == wclErrors.WCL_E_SUCCESS
            ? size.ToString() : "-----");
    }

    // ==================================================================
    //  Log
    // ==================================================================
    private void Trace(string msg)
    {
        Debug.Log("[GattServer] " + msg);
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

    private void Trace(byte spaces, string msg) => Trace(new string(' ', spaces) + msg);
    private void Trace(string msg, int err) => Trace(msg + " failed: 0x" + err.ToString("X8"));
    private void Trace(byte spaces, string msg, int err)
        => Trace(new string(' ', spaces) + msg + " failed: 0x" + err.ToString("X8"));

    private void Trace4(string msg)  => Trace(4, msg);
    private void Trace8(string msg)  => Trace(8, msg);
    private void Trace12(string msg) => Trace(12, msg);

    private void Trace4(string msg, int err)  => Trace(4, msg, err);
    private void Trace8(string msg, int err)  => Trace(8, msg, err);
    private void Trace12(string msg, int err) => Trace(12, msg, err);

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}