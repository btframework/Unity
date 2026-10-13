using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclCommunication;
using wclBluetooth;

public class FtpClientDemo : MonoBehaviour
{
    // ---------------- Top bar ----------------
    public Button discoverButton;
    public Button connectButton;
    public Button disconnectButton;

    // ---------------- Devices table ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------------- Session / progress ----------------
    public Button openSessionButton;
    public Button closeSessionButton;
    public Image  progressFill;
    public TMP_Text progressLabel;

    // ---------------- File send ----------------
    public Button         selectFileButton;
    public TMP_InputField fileNameInput;
    public Button         sendFileButton;
    public Button         abortButton;

    // ---------------- New dir ----------------
    public TMP_InputField newDirNameInput;
    public Button         mkDirButton;

    // ---------------- FS operations ----------------
    public Button dirButton;
    public Button deleteButton;
    public Button rootButton;

    // ---------------- Files table ----------------
    public Transform  filesContent;
    public GameObject fileRowPrefab;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;
    public Button     clearLogButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _btManager;
    private wclRfCommClient     _rfCommClient;
    private wclObexFtpClient    _ftp;

    private TableRowUI _selectedDevice;
    private TableRowUI _selectedFile;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        discoverButton  .onClick.AddListener(OnDiscoverClicked);
        connectButton   .onClick.AddListener(OnConnectClicked);
        disconnectButton.onClick.AddListener(OnDisconnectClicked);
        openSessionButton .onClick.AddListener(OnOpenSessionClicked);
        closeSessionButton.onClick.AddListener(OnCloseSessionClicked);
        selectFileButton  .onClick.AddListener(OnSelectFileClicked);
        sendFileButton    .onClick.AddListener(OnSendFileClicked);
        abortButton       .onClick.AddListener(OnAbortClicked);
        mkDirButton       .onClick.AddListener(OnMkDirClicked);
        dirButton         .onClick.AddListener(OnDirClicked);
        deleteButton      .onClick.AddListener(OnDeleteClicked);
        rootButton        .onClick.AddListener(OnRootClicked);
        clearLogButton    .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        _btManager = new wclBluetoothManager();
        _btManager.AfterOpen                 += BtManagerAfterOpen;
        _btManager.OnAuthenticationCompleted += BtManagerOnAuthenticationCompleted;
        _btManager.OnDiscoveringCompleted    += BtManagerOnDiscoveringCompleted;
        _btManager.OnDiscoveringStarted      += BtManagerOnDiscoveringStarted;
        _btManager.OnNumericComparison       += BtManagerOnNumericComparison;
        _btManager.OnPasskeyNotification     += BtManagerOnPasskeyNotification;
        _btManager.OnPasskeyRequest          += BtManagerOnPasskeyRequest;
        _btManager.OnPinRequest              += BtManagerOnPinRequest;
        _btManager.OnDeviceFound             += BtManagerOnDeviceFound;

        _rfCommClient = new wclRfCommClient();
        _rfCommClient.OnDisconnect       += RfCommOnDisconnect;
        _rfCommClient.OnConnect          += RfCommOnConnect;
        _rfCommClient.OnDestroyProcessor += RfCommOnDestroyProcessor;
        _rfCommClient.OnCreateProcessor  += RfCommOnCreateProcessor;

        // ObjectPush/OBEX FTP profile UUID
        _rfCommClient.Service = wclUUIDs.OBEXFileTransferServiceClass_UUID;

        Int32 res = _btManager.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Bluetooth Manager Open failed: 0x" + res.ToString("X8"));
    }

    private void OnDestroy()
    {
        try
        {
            _rfCommClient?.Disconnect();
            _btManager   ?.Close();
        }
        catch (Exception e) { Debug.LogException(e); }

        if (_btManager != null)
        {
            _btManager.AfterOpen                 -= BtManagerAfterOpen;
            _btManager.OnAuthenticationCompleted -= BtManagerOnAuthenticationCompleted;
            _btManager.OnDiscoveringCompleted    -= BtManagerOnDiscoveringCompleted;
            _btManager.OnDiscoveringStarted      -= BtManagerOnDiscoveringStarted;
            _btManager.OnNumericComparison       -= BtManagerOnNumericComparison;
            _btManager.OnPasskeyNotification     -= BtManagerOnPasskeyNotification;
            _btManager.OnPasskeyRequest          -= BtManagerOnPasskeyRequest;
            _btManager.OnPinRequest              -= BtManagerOnPinRequest;
            _btManager.OnDeviceFound             -= BtManagerOnDeviceFound;
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
    //  Bluetooth manager events
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
            row.SetCell(1, res != wclErrors.WCL_E_SUCCESS ? ErrStr(res) : name);
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
        if (devicesContent == null || deviceRowPrefab == null) return;

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
        _ftp = new wclObexFtpClient(connection);
        _ftp.OnConnect          += FtpOnConnect;
        _ftp.OnDisconnect       += FtpOnDisconnect;
        _ftp.OnGetComplete      += FtpOnGetComplete;
        _ftp.OnPutComplete      += FtpOnPutComplete;
        _ftp.OnProgress         += FtpOnProgress;
        _ftp.OnDirComplete      += FtpOnDirComplete;
        _ftp.OnChangeDirComplete+= FtpOnChangeDirComplete;
        _ftp.OnDeleteComplete   += FtpOnDeleteComplete;
        _ftp.OnMakeDirComplete  += FtpOnMakeDirComplete;
    }

    private void RfCommOnDestroyProcessor(object s, wclClientDataConnection connection)
    {
        if (_ftp != null && ReferenceEquals(connection.Processor, _ftp))
        {
            _ftp.Dispose();
            _ftp = null;
        }
    }

    // ==================================================================
    //  OBEX FTP events
    // ==================================================================
    private void FtpOnConnect(object s, int err, string desc)
        => Log("OBEX session opened with result: 0x" + err.ToString("X8") + ". Description: " + desc);

    private void FtpOnDisconnect(object s, int err, string desc)
        => Log("OBEX session closed with result: 0x" + err.ToString("X8") + ". Description: " + desc);

    private void FtpOnProgress(object s, uint size, uint position)
        => SetProgress(position, size);

    private void FtpOnPutComplete(object s, int err, string desc, Stream stream)
    {
        Log("File sending completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        CloseStream(stream);
    }

    private void FtpOnGetComplete(object s, int err, string desc, Stream stream)
    {
        Log("File receiving completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        CloseStream(stream);
    }

    private void FtpOnChangeDirComplete(object s, int err, string desc)
    {
        Log("Change dir completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        ClearChildren(filesContent);
        _selectedFile = null;
        _ftp.Dir();
    }

    private void FtpOnDeleteComplete(object s, int err, string desc)
    {
        Log("Delete completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        ClearChildren(filesContent);
        _selectedFile = null;
        _ftp.Dir();
    }

    private void FtpOnMakeDirComplete(object s, int err, string desc)
    {
        Log("Make dir completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();
        ClearChildren(filesContent);
        _selectedFile = null;
        _ftp.Dir();
    }

    private void FtpOnDirComplete(object s, int err, string desc, wclObexFileObject[] dirs)
    {
        Log("Dir completed with result: 0x" + err.ToString("X8") + ". Description: " + desc);
        ResetProgress();

        if (dirs == null) return;

        foreach (var obj in dirs)
        {
            var rowGO = Instantiate(fileRowPrefab, filesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnFileRowClicked);

            // Двойной клик
            var dbl = rowGO.GetComponent<DoubleClickEmitter>();
            if (dbl == null) dbl = rowGO.AddComponent<DoubleClickEmitter>();
            dbl.onDoubleClick = () => OnFileRowDoubleClicked(row);

            row.SetCell(0, obj.Name);
            row.SetCell(1, obj.IsDirectory ? "Folder" : "File");
            row.SetCell(2, obj.Size.ToString());

            string perm = "";
            if ((obj.Permissions & wclObexFilePermission.opRead)   != 0) perm += "R";
            if ((obj.Permissions & wclObexFilePermission.opWrite)  != 0) perm += "W";
            if ((obj.Permissions & wclObexFilePermission.opDelete) != 0) perm += "D";
            row.SetCell(3, perm);

            row.SetCell(4, !obj.Modified.Equals(DateTime.MinValue) ? obj.Modified.ToString() : "");
            row.SetCell(5, !obj.Created .Equals(DateTime.MinValue) ? obj.Created .ToString() : "");
            row.SetCell(6, !obj.Accessed.Equals(DateTime.MinValue) ? obj.Accessed.ToString() : "");
            row.SetCell(7, obj.Description);
        }
    }

    // ==================================================================
    //  Button handlers
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
        if (_ftp == null) { Log("Not connected"); return; }
        Int32 res = _ftp.Connect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Open session failed: 0x" + res.ToString("X8"));
    }

    private void OnCloseSessionClicked()
    {
        if (_ftp == null) { Log("Not connected"); return; }
        Int32 res = _ftp.Disconnect("");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Close session failed: 0x" + res.ToString("X8"));
    }

    private void OnSelectFileClicked()
    {
        string path = PickFile();
        if (!string.IsNullOrEmpty(path)) fileNameInput.text = path;
    }

    private void OnSendFileClicked()
    {
        if (_ftp == null) { Log("Not connected"); return; }
        if (string.IsNullOrEmpty(fileNameInput.text)) { Log("Select file"); return; }

        FileStream stream;
        try { stream = new FileStream(fileNameInput.text, FileMode.Open); }
        catch (Exception e) { Log("Open file failed: " + e.Message); return; }

        Int32 res = _ftp.Put(Path.GetFileName(fileNameInput.text), "", stream);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Send file failed: 0x" + res.ToString("X8"));
            CloseStream(stream);
        }
    }

    private void OnAbortClicked()
    {
        if (_ftp == null) { Log("Not connected"); return; }
        Int32 res = _ftp.Abort("");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Abort failed: 0x" + res.ToString("X8"));
    }

    private void OnMkDirClicked()
    {
        if (_ftp == null) { Log("Not connected"); return; }
        Int32 res = _ftp.MkDir(newDirNameInput.text);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Make dir failed: 0x" + res.ToString("X8"));
    }

    private void OnDirClicked()
    {
        if (_ftp == null) { Log("Not connected"); return; }
        Int32 res = _ftp.Dir();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Dir failed: 0x" + res.ToString("X8"));
        else
        {
            ClearChildren(filesContent);
            _selectedFile = null;
        }
    }

    private void OnDeleteClicked()
    {
        if (_selectedFile == null) return;
        if (_ftp == null) { Log("Not connected"); return; }

        Int32 res = _ftp.Delete(_selectedFile.GetCell(0));
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Delete failed: 0x" + res.ToString("X8"));
    }

    private void OnRootClicked()
    {
        if (_ftp == null) { Log("Not connected"); return; }
        Int32 res = _ftp.ChangeDir("");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Root failed: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Row handlers
    // ==================================================================
    private void OnDeviceRowClicked(TableRowUI row)
    {
        if (_selectedDevice != null) _selectedDevice.SetSelected(false);
        _selectedDevice = row;
        if (_selectedDevice != null) _selectedDevice.SetSelected(true);
    }

    private void OnFileRowClicked(TableRowUI row)
    {
        if (_selectedFile != null) _selectedFile.SetSelected(false);
        _selectedFile = row;
        if (_selectedFile != null) _selectedFile.SetSelected(true);
    }

    private void OnFileRowDoubleClicked(TableRowUI row)
    {
        if (row == null) return;
        if (_ftp == null) { Log("Not connected"); return; }

        string type = row.GetCell(1);
        string name = row.GetCell(0);

        if (type == "Folder")
        {
            Int32 res = _ftp.ChangeDir(name);
            if (res != wclErrors.WCL_E_SUCCESS)
                Log("Change path failed: 0x" + res.ToString("X8"));
        }
        else
        {
            string target = Path.Combine(Application.persistentDataPath, name);
            FileStream stream;
            try
            {
                stream = new FileStream(target, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (Exception e) { Log("Open local file failed: " + e.Message); return; }

            Int32 res = _ftp.Get(name, stream);
            if (res != wclErrors.WCL_E_SUCCESS)
            {
                Log("Get failed: 0x" + res.ToString("X8"));
                CloseStream(stream);
            }
        }
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio radio;
        Int32 res = _btManager.GetClassicRadio(out radio);
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
        // В билде OpenFileDialog нет — используем папку persistentDataPath.
        // Положи файл туда вручную, либо интегрируй сторонний файловый диалог.
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

    private static string ErrStr(int err) => "Error: 0x" + err.ToString("X8");

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
        Debug.Log("[FtpClient] " + msg);
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