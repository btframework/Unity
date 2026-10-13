using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclSerialFramework;

public class SerialMonitorDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button startButton;
    public Button stopButton;
    public Button enumSerialButton;
    public Button enumUsbButton;
    public Button disableButton;
    public Button enableButton;

    // ---------------- Headers ----------------
    public GameObject serialHeaderBar;   // 3 columns: Device name | Friendly name | IsModem
    public GameObject usbHeaderBar;      // 7 columns: Instance | Friendly | VID | PID | Class | Manufacturer | Enabled

    // ---------------- Devices table ----------------
    public Transform  devicesContent;
    public GameObject serialRowPrefab;
    public GameObject usbRowPrefab;

    // ---------------- Clear ----------------
    public Button clearButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclSerialMonitor _monitor;

    private enum DeviceMode { None, Serial, Usb }
    private DeviceMode _mode = DeviceMode.None;

    private TableRowUI _selectedDevice;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        startButton      .onClick.AddListener(OnStartClicked);
        stopButton       .onClick.AddListener(OnStopClicked);
        enumSerialButton .onClick.AddListener(OnEnumSerialClicked);
        enumUsbButton    .onClick.AddListener(OnEnumUsbClicked);
        disableButton    .onClick.AddListener(OnDisableClicked);
        enableButton     .onClick.AddListener(OnEnableClicked);
        clearButton      .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        _monitor = new wclSerialMonitor();
        _monitor.OnStarted            += MonitorStarted;
        _monitor.OnStopped            += MonitorStopped;
        _monitor.OnSerialDeviceAdded  += MonitorSerialDeviceAdded;
        _monitor.OnSerialDeviceRemoved+= MonitorSerialDeviceRemoved;
        _monitor.OnUsbDeviceAdded     += MonitorUsbDeviceAdded;
        _monitor.OnUsbDeviceRemoved   += MonitorUsbDeviceRemoved;

        serialHeaderBar.SetActive(false);
        usbHeaderBar   .SetActive(false);
    }

    private void OnDestroy()
    {
        if (_monitor == null) return;

        try { _monitor.Stop(); } catch (Exception e) { Debug.LogException(e); }

        _monitor.OnStarted            -= MonitorStarted;
        _monitor.OnStopped            -= MonitorStopped;
        _monitor.OnSerialDeviceAdded  -= MonitorSerialDeviceAdded;
        _monitor.OnSerialDeviceRemoved-= MonitorSerialDeviceRemoved;
        _monitor.OnUsbDeviceAdded     -= MonitorUsbDeviceAdded;
        _monitor.OnUsbDeviceRemoved   -= MonitorUsbDeviceRemoved;

        _monitor = null;
    }

    // ==================================================================
    //  Monitor events
    // ==================================================================
    private void MonitorStarted(object s, EventArgs e) => Log("Monitor started");
    private void MonitorStopped(object s, EventArgs e) => Log("Monitor stopped");
    private void MonitorSerialDeviceAdded(object s, string name)   => Log("Device added: " + name);
    private void MonitorSerialDeviceRemoved(object s, string name) => Log("Device removed: " + name);
    private void MonitorUsbDeviceAdded(object s, string instance)  => Log("Device added: " + instance);
    private void MonitorUsbDeviceRemoved(object s, string instance)=> Log("Device removed: " + instance);

    // ==================================================================
    //  Buttons — top
    // ==================================================================
    private void OnStartClicked()
    {
        Int32 res = _monitor.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Start failed: 0x" + res.ToString("X8"));
    }

    private void OnStopClicked()
    {
        Int32 res = _monitor.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Stop failed: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Enum Serial
    // ==================================================================
    private void OnEnumSerialClicked()
    {
        ClearDevices();
        _mode = DeviceMode.Serial;
        serialHeaderBar.SetActive(true);
        usbHeaderBar   .SetActive(false);

        List<wclSerialDevice> devices;
        Int32 res = _monitor.EnumSerialDevices(out devices);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enum serial devices failed: 0x" + res.ToString("X8"));
            return;
        }

        if (devices == null || devices.Count == 0)
        {
            Log("No serial devices found");
            return;
        }

        Log("Found " + devices.Count + " serial devices");

        foreach (var device in devices)
        {
            var rowGO = Instantiate(serialRowPrefab, devicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnDeviceRowClicked);
            row.SetCell(0, device.DeviceName);
            row.SetCell(1, device.FriendlyName);
            row.SetCell(2, device.IsModem.ToString());
        }
    }

    // ==================================================================
    //  Enum USB
    // ==================================================================
    private void OnEnumUsbClicked()
    {
        ClearDevices();
        _mode = DeviceMode.Usb;
        serialHeaderBar.SetActive(false);
        usbHeaderBar   .SetActive(true);

        List<wclUsbDevice> devices;
        Int32 res = _monitor.EnumUsbDevices(out devices);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enum USB devices failed: 0x" + res.ToString("X8"));
            return;
        }

        if (devices == null || devices.Count == 0)
        {
            Log("No USB devices found");
            return;
        }

        Log("Found " + devices.Count + " USB devices");

        foreach (var device in devices)
        {
            var rowGO = Instantiate(usbRowPrefab, devicesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnDeviceRowClicked);
            row.SetCell(0, device.Instance);
            row.SetCell(1, device.FriendlyName);
            row.SetCell(2, device.VendorId.ToString("X4"));
            row.SetCell(3, device.ProductId.ToString("X4"));
            row.SetCell(4, device.ClassGuid.ToString());
            row.SetCell(5, device.Manufacturer);
            row.SetCell(6, device.Enabled.ToString());
        }
    }

    // ==================================================================
    //  Enable / Disable
    // ==================================================================
    private void OnDisableClicked() => SwitchUsbDevice(false);
    private void OnEnableClicked()  => SwitchUsbDevice(true);

    private void SwitchUsbDevice(bool enable)
    {
        if (_mode != DeviceMode.Usb)
        {
            Log("Enumerate USB devices");
            return;
        }

        if (devicesContent.childCount == 0)
        {
            Log("No USB devices found");
            return;
        }

        if (_selectedDevice == null)
        {
            Log("Select USB device");
            return;
        }

        string instance = _selectedDevice.GetCell(0);
        Int32 res = enable
            ? _monitor.EnableUsbDevice(instance)
            : _monitor.DisableUsbDevice(instance);

        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log(enable
                ? "Error enabling USB: 0x" + res.ToString("X8")
                : "Error disabling USB: 0x" + res.ToString("X8"));
            return;
        }

        Log(enable ? "Device enabled" : "Device disabled");
    }

    // ==================================================================
    //  Row selection
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
    private void ClearDevices()
    {
        ClearChildren(devicesContent);
        _selectedDevice = null;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void Log(string msg)
    {
        Debug.Log("[SerialMonitor] " + msg);
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