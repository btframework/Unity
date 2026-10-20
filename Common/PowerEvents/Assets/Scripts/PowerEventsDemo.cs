using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;

public class PowerEventsDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button startButton;
    public Button stopButton;
    public Button statusButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclPowerEventsMonitor _monitor;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        startButton ?.onClick.AddListener(OnStartClicked);
        stopButton  ?.onClick.AddListener(OnStopClicked);
        statusButton?.onClick.AddListener(OnStatusClicked);
    }

    private void Start()
    {
        _monitor = new wclPowerEventsMonitor();
        _monitor.OnPowerStateChanged += PowerStateChanged;
        _monitor.OnStarted           += MonitorStarted;
        _monitor.OnStopped           += MonitorStopped;
    }

    private void OnDestroy()
    {
        if (_monitor == null) return;

        try { _monitor.Stop(); } catch (Exception e) { Debug.LogException(e); }

        _monitor.OnPowerStateChanged -= PowerStateChanged;
        _monitor.OnStarted           -= MonitorStarted;
        _monitor.OnStopped           -= MonitorStopped;

        _monitor = null;
    }

    // ==================================================================
    //  Monitor events
    // ==================================================================
    private void PowerStateChanged(object sender, wclPowerState state)
    {
        switch (state)
        {
            case wclPowerState.psPowerStatusChanged:
                Log("Power status changed");
                break;
            case wclPowerState.psResumeAutomatic:
                Log("Resumed");
                break;
            case wclPowerState.psResume:
                Log("Resumed by user");
                break;
            case wclPowerState.psSuspend:
                Log("Suspended");
                break;
            case wclPowerState.psUnknown:
                Log("Unknonw");
                break;
        }
    }

    private void MonitorStarted(object sender, EventArgs e) => Log("Monitor started");
    private void MonitorStopped(object sender, EventArgs e) => Log("Monitor stopped");

    // ==================================================================
    //  Buttons
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

    private void OnStatusClicked()
    {
        wclPowerStatus status;
        if (!_monitor.GetPowerStatus(out status))
        {
            Log("Get status failed");
            return;
        }

        switch (status.ACLineStatus)
        {
            case wclACLineStatus.lsOffline:
                Log("AC: Offline");
                break;
            case wclACLineStatus.lsOnline:
                Log("AC: Online");
                break;
            case wclACLineStatus.lsBackup:
                Log("AC: Backup");
                break;
            case wclACLineStatus.lsUnknown:
                Log("AC: Unknown");
                break;
        }

        string str = "[";
        if ((wclBatteryChargeStatus.csCapacityHigh     & status.BatteryChargeStatus) != 0) str += " csCapacityHigh";
        if ((wclBatteryChargeStatus.csCapacityLow      & status.BatteryChargeStatus) != 0) str += " csCapacityLow";
        if ((wclBatteryChargeStatus.csCapacityCritical & status.BatteryChargeStatus) != 0) str += " csCapacityCritical";
        if ((wclBatteryChargeStatus.csCharging         & status.BatteryChargeStatus) != 0) str += " csCharging";
        if ((wclBatteryChargeStatus.csNoSystemBattery  & status.BatteryChargeStatus) != 0) str += " csNoSystemBattery";
        str += " ]";
        Log("Batt: " + str);

        Log("Batt percent: " + status.BatteryLifePercent.ToString());

        if (status.BatterySavingState)
            Log("Battery saving");

        if (status.BatteryLifeTime != UInt32.MaxValue)
            Log("Batt life: " + status.BatteryLifeTime.ToString());

        if (status.BatteryFullLifeTime != UInt32.MaxValue)
            Log("Batt full life: " + status.BatteryFullLifeTime.ToString());
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    /// <summary>
    /// Appends a line to the log list. The row prefab already has its cells
    /// array populated by the scene builder, so no per-instance fixups are
    /// needed here.
    /// </summary>
    private void Log(string msg)
    {
        Debug.Log("[PowerEvents] " + msg);
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
}