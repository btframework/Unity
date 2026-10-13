using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclCommunication;
using wclBluetooth;

public class OppClientDemo : MonoBehaviour
{
    // ---------------- Top ----------------
    public Button discoverButton;
    public Button connectButton;
    public Button disconnectButton;

    // ---------------- Devices ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------------- Session ----------------
    public Button openSessionButton;
    public Button closeSessionButton;
    public Button abortButton;

    // ---------------- Send ----------------
    public Button         selectFileButton;
    public TMP_InputField fileNameInput;
    public Button         sendFileButton;

    // ---------------- Progress ----------------
    public Image  progressFill;
    public TMP_Text progressLabel;

    // ---------------- Get special ----------------
    public Button getVCardButton;
    public Button getCapsButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;
    public Button     clearLogButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _manager;
    private wclRfCommClient     _rfCommClient;
    private wclObexOppClient    _opp;

    private TableRowUI _selectedDevice;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        discoverButton    .onClick.AddListener(OnDiscoverClicked);
        connectButton     .onClick.AddListener(OnConnectClicked);
        disconnectButton  .onClick.AddListener(OnDisconnectClicked);
        openSessionButton .onClick.AddListener(OnOpenSessionClicked);
        closeSessionButton.onClick.AddListener(OnCloseSessionClicked);
        abortButton       .onClick.AddListener(OnAbortClicked);
        selectFileButton  .onClick.AddListener(OnSelectFileClicked);
        sendFileButton    .onClick.AddListener(OnSendFileClicked);
        getVCardButton    .onClick.AddListener(OnGetVCardClicked);
        getCapsButton     .onClick.AddListener(OnGetCapsClicked);
        clearLogButton    .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();
        _manager.AfterOpen                 += BtManagerAfterOpen;
        _manager.OnAuthenticationCompleted += BtManagerOnAuthenticationCompleted;
        _manager.OnDiscoveringCompleted    += BtManagerOnDiscoveringCompleted;
        _manager.OnDiscoveringStarted      += BtManagerOnDiscoveringStarted;
        _manager.OnNumericComparison       += BtManagerOnNumericComparison;
        _manager.OnPasskeyNotification     += BtManagerOnPasskeyNotification;
        _manager.OnPasskeyRequest          += BtManagerOnPasskeyRequest;
        _manager.OnPinRequest              += BtManagerOnPinRequest;
        _manager.OnDeviceFound             += BtManagerOnDeviceFound;

        _rfCommClient = new wclRfCommClient();
        _rfCommClient.OnDisconnect       += RfCommOnDisconnect;
        _rfCommClient.OnConnect          += RfCommOnConnect;
        _rfCommClient.OnDestroyProcessor += RfCommOnDestroyProcessor;
        _rfCommClient.OnCreateProcessor  += RfCommOnCreateProcessor;

        // ObjectPush Profile UUID
        _rfCommClient.Service = wclUUIDs.OBEXObjectPushServiceClass_UUID;

        Int32 res = _manager.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Bluetooth Manager Open failed: 0x" + res.ToString("X8"));
    }

    private void OnDestroy()
    {
        try
        {
            _rfCommClient?.Disconnect();
            _manager?.Close();
        }
        catch (Exception e) { Debug.LogException(e); }

        if (_manager != null)
        {
            _manager.AfterOpen                 -= BtManagerAfterOpen;
            _manager.OnAuthenticationCompleted -= BtManagerOnAuthenticationCompleted;
            _manager.OnDiscoveringCompleted    -= BtManagerOnDiscoveringCompleted;
            _manager.OnDiscoveringStarted      -= BtManagerOnDiscoveringStarted;
            _manager.OnNumericComparison       -= BtManagerOnNumericComparison;
            _manager.OnPasskeyNotification     -= BtManagerOnPasskeyNotification;
            _manager.OnPasskeyRequest          -= BtManagerOnPasskeyRequest;
            _manager.OnPinRequest              -= BtManagerOnPinRequest;
            _manager.OnDeviceFound             -= BtManagerOnDeviceFound;
        }

        if (_rfCommClient != null)
        {
            _rfCommClient.OnDisconnect       -= RfCommOnDisconnect;
            _rfCommClient.OnConnect          -= RfCommOnConnect;
            _rfCommClient.OnDestroyProcessor -= RfCommOnDestroyProcessor;
            _rfCommClient.OnCreateProcessor  -= RfCommOnCreateProcessor;
        }
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void BtManagerAfterOpen(object s, EventArgs e) => Log("Bluetooth Manager has been opened");

    private void BtManagerOnAuthenticationCompleted(object s, wclBluetoothRadio r, long addr, int err)
        => Log("Authentication completed with result: 0x" + err.ToString("X8"));

    private void BtManagerOnDiscoveringStarted(object s, wclBluetoothRadio r)
    {
        Log("Discovering started");
        ClearChildren(devicesContent);
        _selectedDevice = null;
    }

    private void BtManagerOnDiscoveringCompleted(object s, wclBluetoothRadio r, int err)
    {
        Log("Discovering completed with result: 0x" + err.ToString("X8"));

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

    private void BtManagerOnNumericComparison(object s, wclBluetoothRadio r, long addr, uint number, out bool confirm)
    {
        Log("Numeric comparison. Accept");
        confirm = true;
    }

    private void BtManagerOnPasskeyNotification(object s, wclBluetoothRadio r, long addr, uint passkey)
        => Log("Passkey notification. Passkey: " + passkey);

    private void BtManagerOnPasskeyRequest(object s, wclBluetoothRadio r, long addr, out uint passkey)
    {
        Log("Passkey request. Passkey: 12345678");
        passkey = 12345678;
    }

    private void BtManagerOnPinRequest(object s, wclBluetoothRadio r, long addr, out string pin)
    {
        Log("PIN code request. PIN: 0000");
        pin = "0000";
    }

    private void BtManagerOnDeviceFound(object s, wclBluetoothRadio r, long addr)
    {
        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.SetCell(0, addr.ToString("X12"));
        row.SetCell(1, "");
    }

    // ==================================================================
    //  RFCOMM events
    // ==================================================================
    private void RfCommOnConnect(object s, int err)
        => Log("Connect. Operation result: 0x" + err.ToString("X8"));

    private void RfCommOnDisconnect(object s, int reason)
        => Log("Disconnected with reason: 0x" + reason.ToString("X8"));

    private void RfCommOnCreateProcessor(object s, wclClientDataConnection connection)
    {
        _opp = new wclObexOppClient(connection);
        _opp.OnConnect      += OppOnConnect;
        _opp.OnDisconnect   += OppOnDisconnect;
        _opp.OnGetComplete  += OppOnGetComplete;
        _opp.OnPutComplete  += OppOnPutComplete;
        _opp.OnProgress     += OppOnProgress;
    }

    private void RfCommOnDestroyProcessor(object s, wclClientDataConnection connection)
    {
        if (_opp != null && ReferenceEquals(connection.Processor, _opp))
        {
            _opp.Dispose();
            _opp = null;
        }
    }

    // ==================================================================
    //  OBEX OPP events
    // ==================================================================
    private void OppOnConnect(object s, int err, string desc)
        => Log("OBEX session opened with result: 0x" + err.ToString("X8") + ". Description: " + desc);

    private void OppOnDisconnect(object s, int err, string desc)
        => Log("OBEX session closed with result: 0x" + err.ToString("X8") + ". Description: " + desc);

    private void OppOnProgress(object s, uint size, uint position)
        => SetProgress(position, size);

    private void OppOnPutComplete(object s, int err, string desc, Stream stream)
    {
        Log("File sending completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        CloseStream(stream);
    }

    private void OppOnGetComplete(object s, int err, string desc, Stream stream)
    {
        Log("File receiving completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        CloseStream(stream);
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnDiscoverClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;

        Int32 res = radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Failed to start discovering: 0x" + res.ToString("X8"));
    }

    private void OnConnectClicked()
    {
        if (_rfCommClient.State != wclClientState.csDisconnected)
        {
            Log("Client is connected");
            return;
        }

        if (_selectedDevice == null) { Log("Select device"); return; }

        var radio = GetRadio();
        if (radio == null) return;

        long addr;
        try { addr = Convert.ToInt64(_selectedDevice.GetCell(0), 16); }
        catch { Log("Invalid address"); return; }

        _rfCommClient.Address = addr;
        Int32 res = _rfCommClient.Connect(radio);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Failed to connect: 0x" + res.ToString("X8"));
    }

    private void OnDisconnectClicked()
    {
        Int32 res = _rfCommClient.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Disconnect failed: 0x" + res.ToString("X8"));
    }

    private void OnOpenSessionClicked()
    {
        if (_opp == null) { Log("Not connected"); return; }
        Int32 res = _opp.Connect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Open session failed: 0x" + res.ToString("X8"));
    }

    private void OnCloseSessionClicked()
    {
        if (_opp == null) { Log("Not connected"); return; }
        Int32 res = _opp.Disconnect("");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Close session failed: 0x" + res.ToString("X8"));
    }

    private void OnAbortClicked()
    {
        if (_opp == null) { Log("Not connected"); return; }
        Int32 res = _opp.Abort("");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Abort failed: 0x" + res.ToString("X8"));
    }

    private void OnSelectFileClicked()
    {
        string path = PickFile();
        if (!string.IsNullOrEmpty(path)) fileNameInput.text = path;
    }

    private void OnSendFileClicked()
    {
        if (_opp == null) { Log("Not connected"); return; }
        if (string.IsNullOrEmpty(fileNameInput.text)) { Log("Select file"); return; }

        FileStream stream;
        try { stream = new FileStream(fileNameInput.text, FileMode.Open); }
        catch (Exception e) { Log("Open file failed: " + e.Message); return; }

        Int32 res = _opp.Put(Path.GetFileName(fileNameInput.text), "", stream);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Send file failed: 0x" + res.ToString("X8"));
            CloseStream(stream);
        }
    }

    private void OnGetVCardClicked()
    {
        if (_opp == null) { Log("Not connected"); return; }

        string target = Path.Combine(Application.persistentDataPath, "vcard.vcf");
        FileStream stream;
        try { stream = new FileStream(target, FileMode.Create); }
        catch (Exception e) { Log("Create file failed: " + e.Message); return; }

        Int32 res = _opp.Get("text/x-vCard", stream);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get vCard failed: 0x" + res.ToString("X8"));
            CloseStream(stream);
        }
    }

    private void OnGetCapsClicked()
    {
        if (_opp == null) { Log("Not connected"); return; }

        string target = Path.Combine(Application.persistentDataPath, "caps.xml");
        FileStream stream;
        try { stream = new FileStream(target, FileMode.Create); }
        catch (Exception e) { Log("Create file failed: " + e.Message); return; }

        Int32 res = _opp.Get("x-obex/capability", stream);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get caps failed: 0x" + res.ToString("X8"));
            CloseStream(stream);
        }
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

    private string PickFile()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel("Select file", "", "*");
#else
        return Path.Combine(Application.persistentDataPath, "upload.bin");
#endif
    }

    private static void CloseStream(Stream stream)
    {
        if (stream == null) return;
        try { stream.Close(); stream.Dispose(); }
        catch { /* ignore */ }
    }

    private void SetProgress(uint value, uint max)
    {
        if (progressFill != null)
            progressFill.fillAmount = (max > 0) ? Mathf.Clamp01((float)value / max) : 0f;
        if (progressLabel != null)
            progressLabel.text = max > 0 ? $"{value} / {max}" : "";
    }

    private void ResetProgress()
    {
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (progressLabel != null) progressLabel.text = "";
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
        Debug.Log("[OppClient] " + msg);
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