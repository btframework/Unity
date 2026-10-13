using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclCommunication;
using wclSerialFramework;

public class SerialOppClientDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public TMP_Text       selectComPortLabel;
    public TMP_Dropdown   portsDropdown;
    public Button         refreshButton;
    public Button         connectButton;
    public Button         disconnectButton;

    // ---------------- OBEX row ----------------
    public Button obexConnectButton;
    public Button obexDisconnectButton;

    // ---------------- File row ----------------
    public TMP_Text       filePathLabel;
    public TMP_InputField filePathInput;
    public Button         selectFileButton;
    public Button         sendFileButton;

    // ---------------- Progress ----------------
    public Image  progressFill;
    public TMP_Text progressLabel;

    // ---------------- Clear + Log ----------------
    public Button     clearButton;
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclSerialMonitor _monitor;
    private wclSerialClient  _client;
    private wclObexOppClient _oppClient;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        refreshButton       .onClick.AddListener(OnRefreshClicked);
        connectButton       .onClick.AddListener(OnConnectClicked);
        disconnectButton    .onClick.AddListener(OnDisconnectClicked);
        obexConnectButton   .onClick.AddListener(OnObexConnectClicked);
        obexDisconnectButton.onClick.AddListener(OnObexDisconnectClicked);
        selectFileButton    .onClick.AddListener(OnSelectFileClicked);
        sendFileButton      .onClick.AddListener(OnSendFileClicked);
        clearButton         .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        _monitor = new wclSerialMonitor();
        _monitor.OnSerialDeviceAdded   += MonitorSerialDeviceAdded;
        _monitor.OnSerialDeviceRemoved += MonitorSerialDeviceRemoved;

        _client = new wclSerialClient();
        _client.OnConnect    += ClientConnect;
        _client.OnDisconnect += ClientDisconnect;
        _client.OnData       += ClientData;

        _oppClient = new wclObexOppClient();
        _oppClient.OnConnect     += OppConnect;
        _oppClient.OnDisconnect  += OppDisconnect;
        _oppClient.OnProgress    += OppProgress;
        _oppClient.OnPutComplete += OppPutComplete;
        _oppClient.OnWrite       += OppWrite;

        Int32 res = _monitor.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Start monitoring failed", res);

        RefreshComPorts();
    }

    private void OnDestroy()
    {
        try { _oppClient?.Disconnect("Application closed"); } catch (Exception e) { Debug.LogException(e); }
        try { _client?.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
        try { _monitor?.Stop(); } catch (Exception e) { Debug.LogException(e); }

        if (_oppClient != null)
        {
            _oppClient.OnConnect     -= OppConnect;
            _oppClient.OnDisconnect  -= OppDisconnect;
            _oppClient.OnProgress    -= OppProgress;
            _oppClient.OnPutComplete -= OppPutComplete;
            _oppClient.OnWrite       -= OppWrite;
            _oppClient.Dispose();
        }

        if (_client != null)
        {
            _client.OnConnect    -= ClientConnect;
            _client.OnDisconnect -= ClientDisconnect;
            _client.OnData       -= ClientData;
        }

        if (_monitor != null)
        {
            _monitor.OnSerialDeviceAdded   -= MonitorSerialDeviceAdded;
            _monitor.OnSerialDeviceRemoved -= MonitorSerialDeviceRemoved;
        }

        _oppClient = null;
        _client    = null;
        _monitor   = null;
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnRefreshClicked() => RefreshComPorts();

    private void OnConnectClicked()
    {
        if (portsDropdown.options.Count == 0) { Log("No COM ports found"); return; }

        try
        {
            Int32 res = _client.Connect(portsDropdown.options[portsDropdown.value].text);
            if (res != wclErrors.WCL_E_SUCCESS)
                Log("Connect failed: 0x" + res.ToString("X8"));
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
            Log("Disconnect failed: 0x" + res.ToString("X8"));
    }

    private void OnObexConnectClicked()
    {
        Int32 res = _oppClient.Connect(0x00FF);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("OBEX connect failed: 0x" + res.ToString("X8"));
    }

    private void OnObexDisconnectClicked()
    {
        Int32 res = _oppClient.Disconnect("Disconnect by user");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("OBEX disconnect failed: 0x" + res.ToString("X8"));
    }

    private void OnSelectFileClicked()
    {
        string path = PickFile();
        if (!string.IsNullOrEmpty(path)) filePathInput.text = path;
    }

    private void OnSendFileClicked()
    {
        if (string.IsNullOrEmpty(filePathInput.text)) { Log("Select file"); return; }

        try
        {
            var stream = new FileStream(filePathInput.text, FileMode.Open);
            Int32 res = _oppClient.Put(Path.GetFileName(filePathInput.text),
                "Serial OPP", stream);
            if (res != wclErrors.WCL_E_SUCCESS)
            {
                Log("Send failed: 0x" + res.ToString("X8"));
                stream.Dispose();
            }
            else
            {
                ResetProgress();
            }
        }
        catch (Exception e)
        {
            Log("Send exception: " + e.Message);
        }
    }

    // ==================================================================
    //  Monitor events
    // ==================================================================
    private void MonitorSerialDeviceAdded(object s, string name)
        => Trace("Device added: " + name);

    private void MonitorSerialDeviceRemoved(object s, string name)
        => Trace("Device removed: " + name);

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientConnect(object s, Int32 error)
    {
        if (error != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Connect failed", error);
            return;
        }

        Trace("Connected");

        wclSerialConfig config;
        if (_client.GetConfig(out config) == wclErrors.WCL_E_SUCCESS)
        {
            config.BaudRate = 115200;
            config.ByteSize = 8;
            _client.SetConfig(config);
        }
    }

    private void ClientDisconnect(object s, Int32 reason)
        => Trace("Disconnected with reason", reason);

    private void ClientData(object s, byte[] data)
    {
        // Forward to OBEX
        if (_oppClient != null) _oppClient.ProcessData(data);
    }

    // ==================================================================
    //  OBEX events
    // ==================================================================
    private void OppConnect(object s, Int32 error, string description)
    {
        if (error != wclErrors.WCL_E_SUCCESS)
        {
            Trace("OBEX connect failed", error);
            Trace("  [" + description + "]");
        }
        else
        {
            Trace("OBEX connected");
        }
    }

    private void OppDisconnect(object s, Int32 error, string description)
    {
        Trace("OBEX disconnect with reason", error);
        Trace("  [" + description + "]");
    }

    private void OppProgress(object s, UInt32 length, UInt32 position)
    {
        SetProgress(position, length);
    }

    private void OppPutComplete(object s, Int32 error, string description, Stream stream)
    {
        Trace("Put completed", error);
        Trace("  [" + description + "]");
        stream?.Dispose();
    }

    private void OppWrite(object s, byte[] data, ref UInt32 written, ref Int32 error)
    {
        // Bridge OBEX data to serial port
        error = _client.Write(data, out written);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private void RefreshComPorts()
    {
        List<wclSerialDevice> devices;
        Int32 res = _monitor.EnumSerialDevices(out devices);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Error: 0x" + res.ToString("X8"));
            return;
        }

        portsDropdown.ClearOptions();
        if (devices == null || devices.Count == 0) return;

        var names = new List<string>();
        foreach (var device in devices) names.Add(device.DeviceName);

        portsDropdown.AddOptions(names);
        portsDropdown.SetValueWithoutNotify(0);
        portsDropdown.RefreshShownValue();
    }

    private static string PickFile()
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel("Select file", "", "*");
#else
        return Path.Combine(Application.persistentDataPath, "upload.bin");
#endif
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

    private void Log(string msg)
    {
        Debug.Log("[SerialOppClient] " + msg);
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

    private void Trace(string msg) => Log(msg);
    private void Trace(string msg, int error) => Log(msg + ": 0x" + error.ToString("X8"));
}