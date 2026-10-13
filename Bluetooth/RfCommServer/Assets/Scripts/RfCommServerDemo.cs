using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;
using wclCommunication;

public class RfCommServerDemo : MonoBehaviour
{
    // ---------------- Top ----------------
    public Toggle authenticationToggle;
    public Toggle encryptionToggle;
    public Button listenButton;
    public Button closeButton;

    public TMP_Text        serviceNameLabel;
    public TMP_InputField  serviceNameInput;

    // ---------------- Clients ----------------
    public Transform  clientsContent;
    public GameObject clientRowPrefab;

    public Button disconnectButton;

    // ---------------- Send ----------------
    public TMP_InputField textInput;
    public Button         sendButton;

    // ---------------- Buffers ----------------
    public Button         getBuffersButton;
    public TMP_Text       readBufferLabel;
    public TMP_InputField readBufferInput;
    public TMP_Text       writeBufferLabel;
    public TMP_InputField writeBufferInput;
    public Button         setBuffersButton;

    // ---------------- Log ----------------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;
    public Button     clearEventsButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _manager;
    private wclRfCommServer     _server;

    private TableRowUI _selectedClient;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        listenButton      .onClick.AddListener(OnListenClicked);
        closeButton       .onClick.AddListener(OnCloseClicked);
        disconnectButton  .onClick.AddListener(OnDisconnectClicked);
        sendButton        .onClick.AddListener(OnSendClicked);
        getBuffersButton  .onClick.AddListener(OnGetBuffersClicked);
        setBuffersButton  .onClick.AddListener(OnSetBuffersClicked);
        clearEventsButton .onClick.AddListener(() => ClearChildren(eventsContent));

        readBufferInput .contentType = TMP_InputField.ContentType.IntegerNumber;
        writeBufferInput.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();
        _manager.OnNumericComparison   += ManagerOnNumericComparison;
        _manager.OnPasskeyNotification += ManagerOnPasskeyNotification;
        _manager.OnPasskeyRequest      += ManagerOnPasskeyRequest;
        _manager.OnPinRequest          += ManagerOnPinRequest;

        _server = new wclRfCommServer();
        _server.OnClosed     += ServerOnClosed;
        _server.OnConnect    += ServerOnConnect;
        _server.OnData       += ServerOnData;
        _server.OnDisconnect += ServerOnDisconnect;
        _server.OnListen     += ServerOnListen;

        _manager.Open();

        authenticationToggle.isOn = _server.Authentication;
        encryptionToggle.isOn     = _server.Encryption;
    }

    private void OnDestroy()
    {
        try { _server?.Close(); } catch (Exception e) { Debug.LogException(e); }
        try { _manager?.Close(); } catch (Exception e) { Debug.LogException(e); }
        _server = null;
        _manager = null;

        if (_manager != null)
        {
            _manager.OnNumericComparison   -= ManagerOnNumericComparison;
            _manager.OnPasskeyNotification -= ManagerOnPasskeyNotification;
            _manager.OnPasskeyRequest      -= ManagerOnPasskeyRequest;
            _manager.OnPinRequest          -= ManagerOnPinRequest;
        }

        if (_server != null)
        {
            _server.OnClosed     -= ServerOnClosed;
            _server.OnConnect    -= ServerOnConnect;
            _server.OnData       -= ServerOnData;
            _server.OnDisconnect -= ServerOnDisconnect;
            _server.OnListen     -= ServerOnListen;
        }
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void ManagerOnNumericComparison(object s, wclBluetoothRadio r, long addr, uint number, out bool confirm)
    {
        confirm = true;
        Log("Numeric comparison: " + number);
    }

    private void ManagerOnPasskeyNotification(object s, wclBluetoothRadio r, long addr, uint passkey)
        => Log("Passkey notification: " + passkey);

    private void ManagerOnPasskeyRequest(object s, wclBluetoothRadio r, long addr, out uint passkey)
    {
        passkey = 123456;
        Log("Passkey request: " + passkey);
    }

    private void ManagerOnPinRequest(object s, wclBluetoothRadio r, long addr, out string pin)
    {
        pin = "0000";
        Log("Pin request: " + pin);
    }

    // ==================================================================
    //  Server events
    // ==================================================================
    private void ServerOnListen(object sender, EventArgs e)
    {
        var srv = (wclRfCommServer)sender;
        Log("Server listening on radio " + srv.Radio.ApiName
            + " on channel: " + srv.AssignedChannel);
    }

    private void ServerOnClosed(object sender, int reason)
    {
        var srv = (wclRfCommServer)sender;
        Log("Server closed on radio " + srv.Radio.ApiName
            + " with reason: 0x" + reason.ToString("X8"));

        ClearChildren(clientsContent);
        _selectedClient = null;

        readBufferInput.text  = "";
        writeBufferInput.text = "";
    }

    private void ServerOnConnect(object sender, wclRfCommServerClientConnection client, int error)
    {
        var srv = (wclRfCommServerConnection)client.Server;

        Log("Client connected: " + client.Address.ToString("X12")
            + " on Radio " + srv.Radio.ApiName
            + " error: 0x" + error.ToString("X8"));

        var rowGO = Instantiate(clientRowPrefab, clientsContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnClientRowClicked);
        row.Tag = client;
        row.SetCell(0, client.Address.ToString("X12"));
        row.SetCell(1, GetDeviceName(client.Address));
    }

    private void ServerOnData(object sender, wclRfCommServerClientConnection client, byte[] data)
    {
        if (data == null || data.Length == 0) return;

        var srv = (wclRfCommServerConnection)client.Server;
        string str = Encoding.ASCII.GetString(data);

        Log("Received " + data.Length + " bytes: " + str
            + " from " + client.Address.ToString("X12")
            + " on radio " + srv.Radio.ApiName);
    }

    private void ServerOnDisconnect(object sender, wclRfCommServerClientConnection client, int reason)
    {
        var srv = (wclRfCommServerConnection)client.Server;

        Log("Client disconnected: " + client.Address.ToString("X12")
            + " on radio " + srv.Radio.ApiName
            + " reason: 0x" + reason.ToString("X8"));

        RemoveClient(client);
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnListenClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;

        radio.SetDiscoverable(true);
        radio.SetConnectable(true);

        _server.Authentication = authenticationToggle.isOn;
        _server.Encryption     = encryptionToggle.isOn;

        _server.Service     = wclUUIDs.SerialPortServiceClass_UUID;
        _server.ServiceName = serviceNameInput.text;

        Int32 res = _server.Listen(radio);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnCloseClicked()
    {
        Int32 res = _server.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnDisconnectClicked()
    {
        if (_selectedClient == null) { Log("Select client"); return; }
        var client = _selectedClient.Tag as wclRfCommServerClientConnection;
        if (client == null) return;

        Int32 res = client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnSendClicked()
    {
        if (_selectedClient == null) { Log("Select client"); return; }
        var client = _selectedClient.Tag as wclRfCommServerClientConnection;
        if (client == null) return;

        byte[] ansi = Encoding.ASCII.GetBytes(textInput.text);
        UInt32 sent;
        Int32 res = client.Write(ansi, out sent);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
        else
            Log("Sent: " + sent + " from " + ansi.Length);
    }

    private void OnGetBuffersClicked()
    {
        if (_selectedClient == null) { Log("Select client"); return; }
        var client = _selectedClient.Tag as wclRfCommServerClientConnection;
        if (client == null) return;

        UInt32 size;
        Int32 res = client.GetReadBufferSize(out size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get read buffer error: 0x" + res.ToString("X8"));
        else
            readBufferInput.text = size.ToString();

        size = 0;
        res = client.GetWriteBufferSize(out size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get write buffer error: 0x" + res.ToString("X8"));
        else
            writeBufferInput.text = size.ToString();
    }

    private void OnSetBuffersClicked()
    {
        if (_selectedClient == null) { Log("Select client"); return; }
        var client = _selectedClient.Tag as wclRfCommServerClientConnection;
        if (client == null) return;

        UInt32 size = (UInt32)ParseInt(readBufferInput.text, 0);
        Int32 res = client.SetReadBufferSize(size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set read buffer error: 0x" + res.ToString("X8"));

        size = (UInt32)ParseInt(writeBufferInput.text, 0);
        res = client.SetWriteBufferSize(size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set write buffer error: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Row handler
    // ==================================================================
    private void OnClientRowClicked(TableRowUI row)
    {
        if (_selectedClient != null) _selectedClient.SetSelected(false);
        _selectedClient = row;
        if (_selectedClient != null) _selectedClient.SetSelected(true);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio radio;
        Int32 res = _manager.GetClassicRadio(out radio);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get working radio failed: 0x" + res.ToString("X8"));
            return null;
        }
        return radio;
    }

    private string GetDeviceName(long address)
    {
        var radio = GetRadio();
        if (radio == null) return "";

        string name;
        Int32 res = radio.GetRemoteName(address, out name);
        return res == wclErrors.WCL_E_SUCCESS
            ? name
            : "Error: 0x" + res.ToString("X8");
    }

    private void RemoveClient(wclRfCommServerClientConnection client)
    {
        foreach (Transform child in clientsContent)
        {
            var row = child.GetComponent<TableRowUI>();
            if (row == null) continue;
            if (ReferenceEquals(row.Tag, client))
            {
                if (_selectedClient == row) _selectedClient = null;
                Destroy(row.gameObject);
                return;
            }
        }
    }

    private static int ParseInt(string s, int fallback)
        => int.TryParse(s, out var v) ? v : fallback;

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
        Debug.Log("[RfCommServer] " + msg);
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