using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclCommunication;
using wclSerialFramework;

public class SerialOppServerDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public TMP_Text     selectComPortLabel;
    public TMP_Dropdown portsDropdown;
    public Button       refreshButton;
    public Button       connectButton;
    public Button       disconnectButton;

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
    private wclObexOppServer _oppServer;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        refreshButton    .onClick.AddListener(OnRefreshClicked);
        connectButton    .onClick.AddListener(OnConnectClicked);
        disconnectButton .onClick.AddListener(OnDisconnectClicked);
        clearButton      .onClick.AddListener(() => ClearChildren(logContent));
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

        _oppServer = new wclObexOppServer();
        _oppServer.OnWrite        += OppServerWrite;
        _oppServer.OnConnect      += OppServerConnect;
        _oppServer.OnDisconnected += OppServerDisconnected;
        _oppServer.OnPutProgress  += OppServerPutProgress;
        _oppServer.OnPutBegin     += OppServerPutBegin;
        _oppServer.OnPutCompleted += OppServerPutCompleted;

        Int32 res = _monitor.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Start monitoring failed", res);

        RefreshComPorts();
    }

    private void OnDestroy()
    {
        try { _client?.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
        try { _monitor?.Stop(); } catch (Exception e) { Debug.LogException(e); }

        if (_oppServer != null)
        {
            _oppServer.OnWrite        -= OppServerWrite;
            _oppServer.OnConnect      -= OppServerConnect;
            _oppServer.OnDisconnected -= OppServerDisconnected;
            _oppServer.OnPutProgress  -= OppServerPutProgress;
            _oppServer.OnPutBegin     -= OppServerPutBegin;
            _oppServer.OnPutCompleted -= OppServerPutCompleted;
            _oppServer.Dispose();
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

        _oppServer = null;
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

    // ==================================================================
    //  Monitor events
    // ==================================================================
    private void MonitorSerialDeviceAdded(object s, string name)
        => Trace("Device add: " + name);

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
        if (_oppServer != null) _oppServer.ProcessData(data);
    }

    // ==================================================================
    //  OBEX server events
    // ==================================================================
    private void OppServerWrite(object s, byte[] data, ref UInt32 written, ref Int32 error)
    {
        error = _client.Write(data, out written);
    }

    private void OppServerConnect(object s, string description)
    {
        Trace("OBEX client connected [" + description + "]");
        _oppServer.Accept("Accept connection", 0x00FF);
    }

    private void OppServerDisconnected(object s, Int32 reason, string description)
    {
        Trace("OBEX client disconnected with reason", reason);
        Trace("  [" + description + "]");
    }

    private void OppServerPutProgress(object s, UInt32 length, UInt32 position, out bool @continue)
    {
        SetProgress(position, length);
        @continue = true;
    }

    private void OppServerPutBegin(object s, string name, string description, string mime,
        UInt32 length, out bool accept)
    {
        accept = true;
        Trace("Begin sending file: " + name);
    }

    private void OppServerPutCompleted(object s, Int32 error, Stream stream, out bool accept)
    {
        if (error != wclErrors.WCL_E_SUCCESS)
        {
            Trace("File receiving error", error);
            accept = false;
        }
        else
        {
            accept = true;
            Trace("File received");
        }
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

    private void SetProgress(uint value, uint max)
    {
        if (progressFill != null)
            progressFill.fillAmount = (max > 0) ? Mathf.Clamp01((float)value / max) : 0f;
        if (progressLabel != null)
            progressLabel.text = max > 0 ? $"{value} / {max}" : "";
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    /// <summary>
    /// Appends a line to the log list. The row prefab already has its cells
    /// array populated by the scene builder, so no per-instance fixups are
    /// needed here.
    /// </summary>
    private void Log(string msg)
    {
        Debug.Log("[SerialOppServer] " + msg);
        if (logContent == null || logRowPrefab == null) return;

        var go = Instantiate(logRowPrefab, logContent);
        var row = go.GetComponent<TableRowUI>();
        if (row != null)
            row.SetCell(0, msg);
        else
        {
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = msg;
        }

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