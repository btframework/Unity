using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclAudio;

public class AudioSwitcherDemo : MonoBehaviour
{
    [Header("Top row")]
    public Button enumButton;
    public TMP_Dropdown roleDropdown;
    public Button setDefaultButton;
    public Button refreshButton;
    public TMP_Dropdown flowDropdown;
    public Button getDefaultButton;

    [Header("State toggles")]
    public Toggle activeToggle;
    public Toggle disabledToggle;
    public Toggle unpluggedToggle;
    public Toggle notPresentToggle;

    [Header("Bluetooth")]
    public Button connectButton;
    public Button disconnectButton;
    public Toggle useMacToggle;

    [Header("Devices table")]
    public Transform devicesContent;
    public GameObject deviceRowPrefab;

    [Header("Log")]
    public TMP_Text logText;
    public ScrollRect logScrollRect;
    public Button clearButton;

    private wclAudioSwitcher _switcher;
    private readonly Dictionary<string, AudioSwitcherRowUI> _rows =
        new Dictionary<string, AudioSwitcherRowUI>();
    private string _selectedId;

    // ================================================================
    //  Lifecycle
    // ================================================================
    private void Awake()
    {
        enumButton       .onClick.AddListener(OnEnumClicked);
        setDefaultButton .onClick.AddListener(OnSetDefaultClicked);
        refreshButton    .onClick.AddListener(OnRefreshClicked);
        getDefaultButton .onClick.AddListener(OnGetDefaultClicked);
        connectButton    .onClick.AddListener(OnConnectClicked);
        disconnectButton .onClick.AddListener(OnDisconnectClicked);
        clearButton      .onClick.AddListener(OnClearClicked);
    }

    private void Start()
    {
        _switcher = new wclAudioSwitcher();
        _switcher.OnOpened               += SwitcherOnOpened;
        _switcher.OnClosed               += SwitcherOnClosed;
        _switcher.OnDefaultDeviceChanged += SwitcherOnDefaultDeviceChanged;
        _switcher.OnDeviceAdded          += SwitcherOnDeviceAdded;
        _switcher.OnDeviceRemoved        += SwitcherOnDeviceRemoved;
        _switcher.OnStateChanged         += SwitcherOnStateChanged;

        // Значения по умолчанию — как в fmMain_Load
        if (activeToggle != null) activeToggle.isOn = true;
        if (roleDropdown != null) roleDropdown.value = 0; // Console
        if (flowDropdown != null) flowDropdown.value = 0; // Capture

        Int32 res = _switcher.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Open failed: 0x" + res.ToString("X8"));
    }

    private void OnDestroy()
    {
        if (_switcher == null) return;

        try { _switcher.Close(); }
        catch (Exception e) { Debug.LogException(e); }

        _switcher.OnOpened               -= SwitcherOnOpened;
        _switcher.OnClosed               -= SwitcherOnClosed;
        _switcher.OnDefaultDeviceChanged -= SwitcherOnDefaultDeviceChanged;
        _switcher.OnDeviceAdded          -= SwitcherOnDeviceAdded;
        _switcher.OnDeviceRemoved        -= SwitcherOnDeviceRemoved;
        _switcher.OnStateChanged         -= SwitcherOnStateChanged;
    }

    // ================================================================
    //  Framework events
    // ================================================================
    private void SwitcherOnOpened(object sender, EventArgs e)
    {
        Log("Audio switcher opened");
        EnumDevices();
    }

    private void SwitcherOnClosed(object sender, EventArgs e)
    {
        ClearDeviceRows();
        Log("Audio switcher closed");
    }

    private void SwitcherOnDefaultDeviceChanged(object sender, string id,
        wclAudioDeviceDataFlow flow, wclAudioDeviceRole role)
    {
        string str = "Default device changed: " + id + "; Flow: ";
        str += (flow == wclAudioDeviceDataFlow.dfRender) ? "Render" : "Capture";
        str += "; Role: ";
        switch (role)
        {
            case wclAudioDeviceRole.drConsole:        str += "Console";        break;
            case wclAudioDeviceRole.drMultimedia:     str += "Multimedia";     break;
            case wclAudioDeviceRole.drCommunications: str += "Communications"; break;
        }
        Log(str);
    }

    private void SwitcherOnDeviceAdded(object sender, string id)
        => Log("Device added: " + id);

    private void SwitcherOnDeviceRemoved(object sender, string id)
        => Log("Device removed: " + id);

    private void SwitcherOnStateChanged(object sender, string id, wclAudioDeviceState state)
    {
        string str;
        switch (state)
        {
            case wclAudioDeviceState.asActive:     str = "Active";     break;
            case wclAudioDeviceState.asDisabled:   str = "Disabled";   break;
            case wclAudioDeviceState.asNotPresent: str = "NotPresent"; break;
            case wclAudioDeviceState.asUnplugged:  str = "Unplugged";  break;
            default:                               str = state.ToString(); break;
        }
        Log("Device " + id + " state changed: " + str);
    }

    // ================================================================
    //  UI handlers
    // ================================================================
    private void OnEnumClicked()       => EnumDevices();
    private void OnSetDefaultClicked() => SetDefault();
    private void OnRefreshClicked()    => RefreshSelected();
    private void OnConnectClicked()    => ConnectOrDisconnect(true);
    private void OnDisconnectClicked() => ConnectOrDisconnect(false);

    private void OnClearClicked()
    {
        if (logText != null) logText.text = string.Empty;
    }

    private void OnGetDefaultClicked()
    {
        wclAudioDeviceRole role = RoleFromDropdown();

        wclAudioDeviceDataFlow flow =
            (flowDropdown != null && flowDropdown.value == 0)
                ? wclAudioDeviceDataFlow.dfCapture
                : wclAudioDeviceDataFlow.dfRender;

        string id;
        Int32 res = _switcher.GetDefault(role, flow, out id);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Unable to get default device: 0x" + res.ToString("X8"));
        else
            Log("Default device ID: " + id);
    }

    // ================================================================
    //  Logic
    // ================================================================
    private void EnumDevices()
    {
        wclAudioDeviceState states = 0;
        if (activeToggle     != null && activeToggle.isOn)     states |= wclAudioDeviceState.asActive;
        if (disabledToggle   != null && disabledToggle.isOn)   states |= wclAudioDeviceState.asDisabled;
        if (notPresentToggle != null && notPresentToggle.isOn) states |= wclAudioDeviceState.asNotPresent;
        if (unpluggedToggle  != null && unpluggedToggle.isOn)  states |= wclAudioDeviceState.asUnplugged;

        if (states == 0)
        {
            Log("Select device state.");
            return;
        }

        ClearDeviceRows();

        wclAudioDevice[] devices;
        Int32 res = _switcher.Enum(states, out devices);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enum failed: 0x" + res.ToString("X8"));
            return;
        }

        if (devices == null || devices.Length == 0) return;

        foreach (var device in devices)
            AddDeviceRow(device);
    }

    private void AddDeviceRow(wclAudioDevice device)
    {
        if (_rows.ContainsKey(device.Id)) return;

        var go = Instantiate(deviceRowPrefab, devicesContent);
        var row = go.GetComponent<AudioSwitcherRowUI>();
        row.Init(device.Id, OnRowClicked);
        UpdateRow(row, device);
        _rows[device.Id] = row;
    }

    private void UpdateRow(AudioSwitcherRowUI row, wclAudioDevice device)
    {
        string flowStr = (device.Flow == wclAudioDeviceDataFlow.dfRender)
            ? "Render" : "Capture";

        string rolesStr = "[ ";
        if ((device.Roles & wclAudioDeviceRole.drConsole)        != 0) rolesStr += "Console ";
        if ((device.Roles & wclAudioDeviceRole.drMultimedia)     != 0) rolesStr += "Multimedia ";
        if ((device.Roles & wclAudioDeviceRole.drCommunications) != 0) rolesStr += "Communications ";
        rolesStr += "]";

        string stateStr;
        switch (device.State)
        {
            case wclAudioDeviceState.asActive:     stateStr = "Active";     break;
            case wclAudioDeviceState.asDisabled:   stateStr = "Disabled";   break;
            case wclAudioDeviceState.asNotPresent: stateStr = "NotPresent"; break;
            case wclAudioDeviceState.asUnplugged:  stateStr = "Unplugged";  break;
            default:                               stateStr = device.State.ToString(); break;
        }

        string macStr = "";
        string serviceStr = "";
        if (device.IsBluetooth)
        {
            macStr = device.Mac.ToString("X12");
            serviceStr = device.Service.ToString();
        }

        row.SetData(device.Id, device.FriendlyName, device.Description,
                    flowStr, rolesStr, stateStr, macStr, serviceStr);
    }

    private void SetDefault()
    {
        if (string.IsNullOrEmpty(_selectedId)) { Log("Select device"); return; }

        wclAudioDeviceRole role = RoleFromDropdown();

        Int32 res = _switcher.SetDefault(role, _selectedId);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Unable to set default device: 0x" + res.ToString("X8"));
    }

    private void RefreshSelected()
    {
        if (string.IsNullOrEmpty(_selectedId)) { Log("Select device"); return; }

        wclAudioDevice device;
        Int32 res = _switcher.GetDeviceDetails(_selectedId, out device);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Refresh device error: 0x" + res.ToString("X8"));
            return;
        }

        if (_rows.TryGetValue(_selectedId, out var row))
            UpdateRow(row, device);
    }

    private void ConnectOrDisconnect(bool connect)
    {
        if (string.IsNullOrEmpty(_selectedId)) { Log("Select device"); return; }

        bool useMac = useMacToggle != null && useMacToggle.isOn;

        string macStr = "";
        if (_rows.TryGetValue(_selectedId, out var row))
            macStr = row.Mac;

        Int32 res;
        if (useMac)
        {
            if (string.IsNullOrEmpty(macStr))
            {
                Log("Selected device is not Bluetooth enabled");
                return;
            }

            long mac = Convert.ToInt64(macStr, 16);
            res = connect ? _switcher.Connect(mac) : _switcher.Disconnect(mac);
        }
        else
        {
            res = connect ? _switcher.Connect(_selectedId) : _switcher.Disconnect(_selectedId);
        }

        if (res != wclErrors.WCL_E_SUCCESS)
            Log((connect ? "Connect" : "Disconnect") + " error: 0x" + res.ToString("X8"));
    }

    // ================================================================
    //  Helpers
    // ================================================================
    private wclAudioDeviceRole RoleFromDropdown()
    {
        int idx = roleDropdown != null ? roleDropdown.value : 0;
        switch (idx)
        {
            case 0:  return wclAudioDeviceRole.drConsole;
            case 1:  return wclAudioDeviceRole.drMultimedia;
            default: return wclAudioDeviceRole.drCommunications;
        }
    }

    private void OnRowClicked(string id)
    {
        foreach (var kv in _rows)
            kv.Value.SetSelected(kv.Key == id);
        _selectedId = id;
    }

    private void ClearDeviceRows()
    {
        foreach (var kv in _rows)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _rows.Clear();
        _selectedId = null;
    }

    private void Log(string message)
    {
        Debug.Log("[AudioSwitcher] " + message);
        if (logText == null) return;

        logText.text += (logText.text.Length > 0 ? "\n" : "") + message;

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}