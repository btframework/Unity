using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;
using wclCommunication;

public class RfCommClientDemo : MonoBehaviour
{
    // ---------------- Top bar ----------------
    public Button discoverButton;
    public Toggle enumPairedToggle;
    public Button pairButton;
    public Button unpairButton;

    // ---------------- Devices ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------------- Connect options ----------------
    public Toggle          authenticationToggle;
    public Toggle          encryptionToggle;
    public TMP_InputField  timeoutInput;
    public Toggle          serviceNameToggle;
    public TMP_InputField  serviceNameInput;
    public Button          connectButton;
    public Button          disconnectButton;

    // ---------------- Send ----------------
    public TMP_InputField textInput;
    public Button         sendButton;

    // ---------------- Buffers ----------------
    public TMP_InputField readBufferInput;
    public TMP_InputField writeBufferInput;
    public Button         writeBuffersButton;
    public Button         getBuffersButton;

    // ---------------- Log ----------------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;
    public Button     clearEventsButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _manager;
    private wclRfCommClient     _client;

    private TableRowUI _selectedDevice;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        discoverButton     .onClick.AddListener(OnDiscoverClicked);
        pairButton         .onClick.AddListener(OnPairClicked);
        unpairButton       .onClick.AddListener(OnUnpairClicked);
        connectButton      .onClick.AddListener(OnConnectClicked);
        disconnectButton   .onClick.AddListener(OnDisconnectClicked);
        sendButton         .onClick.AddListener(OnSendClicked);
        writeBuffersButton .onClick.AddListener(OnWriteBuffersClicked);
        getBuffersButton   .onClick.AddListener(OnGetBuffersClicked);
        clearEventsButton  .onClick.AddListener(() => ClearChildren(eventsContent));

        timeoutInput    .contentType = TMP_InputField.ContentType.IntegerNumber;
        readBufferInput .contentType = TMP_InputField.ContentType.IntegerNumber;
        writeBufferInput.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();
        _manager.OnNumericComparison       += ManagerOnNumericComparison;
        _manager.OnPasskeyNotification     += ManagerOnPasskeyNotification;
        _manager.OnPasskeyRequest          += ManagerOnPasskeyRequest;
        _manager.OnPinRequest              += ManagerOnPinRequest;
        _manager.OnConfirm                 += ManagerOnConfirm;
        _manager.OnAuthenticationCompleted += ManagerOnAuthenticationCompleted;
        _manager.OnDeviceFound             += ManagerOnDeviceFound;
        _manager.OnDiscoveringCompleted    += ManagerOnDiscoveringCompleted;
        _manager.OnDiscoveringStarted      += ManagerOnDiscoveringStarted;

        _client = new wclRfCommClient();
        _client.OnConnect    += ClientOnConnect;
        _client.OnDisconnect += ClientOnDisconnect;
        _client.OnData       += ClientOnData;

        _manager.Open();

        // Начальные значения из настроек клиента
        authenticationToggle.isOn = _client.Authentication;
        encryptionToggle.isOn     = _client.Encryption;

        timeoutInput.text = "10000";
        readBufferInput.text  = "0";
        writeBufferInput.text = "0";
    }

    private void OnDestroy()
    {
        try { _client?.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
        try { _manager?.Close(); } catch (Exception e) { Debug.LogException(e); }
        _client = null;
        _manager = null;

        if (_manager != null)
        {
            _manager.OnNumericComparison       -= ManagerOnNumericComparison;
            _manager.OnPasskeyNotification     -= ManagerOnPasskeyNotification;
            _manager.OnPasskeyRequest          -= ManagerOnPasskeyRequest;
            _manager.OnPinRequest              -= ManagerOnPinRequest;
            _manager.OnConfirm                 -= ManagerOnConfirm;
            _manager.OnAuthenticationCompleted -= ManagerOnAuthenticationCompleted;
            _manager.OnDeviceFound             -= ManagerOnDeviceFound;
            _manager.OnDiscoveringCompleted    -= ManagerOnDiscoveringCompleted;
            _manager.OnDiscoveringStarted      -= ManagerOnDiscoveringStarted;
        }

        if (_client != null)
        {
            _client.OnConnect    -= ClientOnConnect;
            _client.OnDisconnect -= ClientOnDisconnect;
            _client.OnData       -= ClientOnData;
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

    private void ManagerOnConfirm(object s, wclBluetoothRadio r, long addr, out bool confirm)
    {
        confirm = true;
        Log("Just works pairing");
    }

    private void ManagerOnAuthenticationCompleted(object s, wclBluetoothRadio r, long addr, int error)
        => Log("Authentication completed: " + error.ToString("X8"));

    private void ManagerOnDiscoveringStarted(object s, wclBluetoothRadio r)
    {
        ClearChildren(devicesContent);
        _selectedDevice = null;
        Log("Discovering started");
    }

    private void ManagerOnDiscoveringCompleted(object s, wclBluetoothRadio r, int error)
    {
        if (devicesContent.childCount == 0)
        {
            Log("No devices were found.");
        }
        else
        {
            foreach (Transform child in devicesContent)
            {
                var row = child.GetComponent<TableRowUI>();
                if (row == null) continue;
                long addr;
                try { addr = Convert.ToInt64(row.GetCell(0), 16); } catch { continue; }
                string name;
                Int32 res = r.GetRemoteName(addr, out name);
                row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? name : "Error: 0x" + res.ToString("X8"));
            }
        }
        Log("Discovering completed");
    }

    private void ManagerOnDeviceFound(object s, wclBluetoothRadio r, long addr)
    {
        wclBluetoothDeviceType devType = wclBluetoothDeviceType.dtMixed;
        Int32 res = r.GetRemoteDeviceType(addr, out devType);

        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.Tag = r;
        row.SetCell(0, addr.ToString("X12"));
        row.SetCell(1, "");

        if (res != wclErrors.WCL_E_SUCCESS)
            row.SetCell(2, "Error: 0x" + res.ToString("X8"));
        else
            row.SetCell(2, DeviceTypeStr(devType));

        Log("Device found: " + addr.ToString("X12"));
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientOnConnect(object s, int error)
    {
        if (error == wclErrors.WCL_E_SUCCESS)
        {
            Log("Connected");
            GetBuffers();
        }
        else
        {
            Log("Connect error: 0x" + error.ToString("X8"));
        }
    }

    private void ClientOnDisconnect(object s, int reason)
    {
        Log("Disconnected: 0x" + reason.ToString("X8"));
        readBufferInput.text  = "0";
        writeBufferInput.text = "0";
    }

    private void ClientOnData(object s, byte[] data)
    {
        if (data == null || data.Length == 0) return;
        string str = Encoding.ASCII.GetString(data);
        Log("Received " + data.Length + " bytes: " + str);
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnDiscoverClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;

        if (enumPairedToggle.isOn)
        {
            long[] devices;
            Int32 res = radio.EnumPairedDevices(out devices);
            if (res != wclErrors.WCL_E_SUCCESS)
            {
                Log("Enum paired devices failed: 0x" + res.ToString("X8"));
                return;
            }

            ClearChildren(devicesContent);
            _selectedDevice = null;

            if (devices != null)
            {
                foreach (var devAddr in devices)
                    ManagerOnDeviceFound(_manager, radio, devAddr);

                ManagerOnDiscoveringCompleted(_manager, radio, wclErrors.WCL_E_SUCCESS);
            }
        }
        else
        {
            Int32 res = radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
            if (res != wclErrors.WCL_E_SUCCESS)
                Log("Error starting discovering: 0x" + res.ToString("X8"));
        }
    }

    private void OnPairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = GetRadio();
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
        Int32 res = radio.RemotePair(addr, wclBluetoothPairingMethod.pmClassic);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Paired failed: 0x" + res.ToString("X8"));
    }

    private void OnUnpairClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }
        var radio = GetRadio();
        if (radio == null) return;

        long addr = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
        Int32 res = radio.RemoteUnpair(addr, wclBluetoothPairingMethod.pmClassic);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Unpaired failed: 0x" + res.ToString("X8"));
    }

    private void OnConnectClicked()
    {
        if (_selectedDevice == null) { Log("Select device"); return; }

        var radio = GetRadio();
        if (radio == null) return;

        try
        {
            _client.Address = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
            _client.Authentication = authenticationToggle.isOn;
            _client.Encryption     = encryptionToggle.isOn;
            _client.Timeout        = (uint)ParseInt(timeoutInput.text, 30);

            _client.Channel = 0;
            _client.Service = wclUUIDs.SerialPortServiceClass_UUID;

            bool connect = false;
            if (serviceNameToggle.isOn)
            {
                Guid service = _client.Service;
                wclBluetoothService[] services;
                Int32 res = radio.EnumRemoteServices(_client.Address, service, out services);
                if (res != wclErrors.WCL_E_SUCCESS)
                {
                    Log("Service enumerating error: 0x" + res.ToString("X8"));
                    return;
                }

                if (services == null || services.Length == 0)
                {
                    Log("Services not found");
                    return;
                }

                foreach (var svc in services)
                {
                    if (svc.Name == serviceNameInput.text)
                    {
                        _client.Channel = svc.Channel;
                        connect = true;
                        break;
                    }
                }

                if (!connect) Log("Service not found");
            }
            else
            {
                connect = true;
            }

            if (connect)
            {
                Int32 res = _client.Connect(radio);
                if (res != wclErrors.WCL_E_SUCCESS)
                    Log("Error: 0x" + res.ToString("X8"));
            }
        }
        catch (Exception e)
        {
            Log("Connect exception: " + e.Message);
        }
    }

    private void OnDisconnectClicked()
    {
        Int32 res = _client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnSendClicked()
    {
        byte[] ansi = Encoding.ASCII.GetBytes(textInput.text);
        UInt32 sent;
        Int32 res = _client.Write(ansi, out sent);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
        else
            Log("Sent: " + sent + " from " + ansi.Length);
    }

    private void OnWriteBuffersClicked()
    {
        UInt32 size = (UInt32)ParseInt(readBufferInput.text, 0);
        Int32 res = _client.SetReadBufferSize(size);
        if (res == wclErrors.WCL_E_SUCCESS) Log("Read buffer size changed.");
        else Log("Set read buffer size error: 0x" + res.ToString("X8"));

        size = (UInt32)ParseInt(writeBufferInput.text, 0);
        res = _client.SetWriteBufferSize(size);
        if (res == wclErrors.WCL_E_SUCCESS) Log("Write buffer size changed.");
        else Log("Set write buffer size error: 0x" + res.ToString("X8"));
    }

    private void OnGetBuffersClicked() => GetBuffers();

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

    private void GetBuffers()
    {
        UInt32 size;
        Int32 res = _client.GetReadBufferSize(out size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get read buffer error: 0x" + res.ToString("X8"));
        else
            readBufferInput.text = size.ToString();

        size = 0;
        res = _client.GetWriteBufferSize(out size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get write buffer error: 0x" + res.ToString("X8"));
        else
            writeBufferInput.text = size.ToString();
    }

    private static int ParseInt(string s, int fallback)
        => int.TryParse(s, out var v) ? v : fallback;

    private static string DeviceTypeStr(wclBluetoothDeviceType t)
    {
        switch (t)
        {
            case wclBluetoothDeviceType.dtClassic: return "Classic";
            case wclBluetoothDeviceType.dtBle:     return "BLE";
            case wclBluetoothDeviceType.dtMixed:   return "Mixed";
            default:                                return "Unknown";
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
        Debug.Log("[RfCommClient] " + msg);
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