using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiDirectSoftAPDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public TMP_InputField ssidInput;
    public TMP_InputField passphraseInput;
    public Button         startButton;
    public Button         stopButton;
    public Button         getIpSettingsButton;
    public Button         setIpSettingsButton;
    public Button         resetIpSettingsButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ---------------- Confirm panel ----------------
    public GameObject confirmPanel;
    public TMP_Text   confirmText;
    public Button     confirmYesButton;
    public Button     confirmNoButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiSoftAP _softAP;
    private Action<bool> _confirmCallback;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        startButton         .onClick.AddListener(OnStartClicked);
        stopButton          .onClick.AddListener(OnStopClicked);
        getIpSettingsButton .onClick.AddListener(OnGetIpSettingsClicked);
        setIpSettingsButton .onClick.AddListener(OnSetIpSettingsClicked);
        resetIpSettingsButton.onClick.AddListener(OnResetIpSettingsClicked);

        confirmYesButton.onClick.AddListener(() => ConfirmResult(true));
        confirmNoButton .onClick.AddListener(() => ConfirmResult(false));

        confirmPanel.SetActive(false);
    }

    private void Start()
    {
        _softAP = new wclWiFiSoftAP();
        _softAP.OnStarted             += SoftApOnStarted;
        _softAP.OnStopped             += SoftApOnStopped;
        _softAP.OnDeviceConnected     += SoftApOnDeviceConnected;
        _softAP.OnDeviceConnectError  += SoftApOnDeviceConnectError;
        _softAP.OnDeviceDisconnected  += SoftApOnDeviceDisconnected;
        _softAP.OnDeviceAccept        += SoftApOnDeviceAccept;
    }

    private void OnDestroy()
    {
        if (_softAP == null) return;

        try { _softAP.Stop(); } catch (Exception e) { Debug.LogException(e); }

        _softAP.OnStarted            -= SoftApOnStarted;
        _softAP.OnStopped            -= SoftApOnStopped;
        _softAP.OnDeviceConnected    -= SoftApOnDeviceConnected;
        _softAP.OnDeviceConnectError -= SoftApOnDeviceConnectError;
        _softAP.OnDeviceDisconnected -= SoftApOnDeviceDisconnected;
        _softAP.OnDeviceAccept       -= SoftApOnDeviceAccept;

        _softAP = null;
    }

    // ==================================================================
    //  SoftAP events
    // ==================================================================
    private void SoftApOnStarted(object s, EventArgs e)
    {
        Log("Started");

        string str;
        Int32 res = _softAP.GetSsid(out str);
        Log("SSID: " + (res == wclErrors.WCL_E_SUCCESS ? str : "Error 0x" + res.ToString("X8")));

        res = _softAP.GetPassphrase(out str);
        Log("Passphrase: " + (res == wclErrors.WCL_E_SUCCESS ? str : "Error 0x" + res.ToString("X8")));
    }

    private void SoftApOnStopped(object s, EventArgs e) => Log("Stopped");

    private void SoftApOnDeviceConnected(object s, wclWiFiDirectDevice device)
    {
        Log("Device connected: " + device.Name);
        Log("  Local IP: "  + device.LocalAddress);
        Log("  Remote IP: " + device.RemoteAddress);
    }

    private void SoftApOnDeviceDisconnected(object s, wclWiFiDirectDevice device)
    {
        Log("Device disconnected: " + device.Name);
        Log("  Local IP: "  + device.LocalAddress);
        Log("  Remote IP: " + device.RemoteAddress);
    }

    private void SoftApOnDeviceConnectError(object s, wclWiFiDirectDevice device, int error)
        => Log("Device " + device.Name + " connect error: " + error.ToString("X8"));

    private void SoftApOnDeviceAccept(object s, wclWiFiDirectDevice device, out bool accept)
    {
        // Синхронный callback — блокировать UI нельзя, поэтому принимаем автоматически.
        // Если у тебя события приходят из фонового потока, сюда можно вставить
        // ManualResetEventSlim + ShowConfirm + Wait(5s).
        accept = true;
        Log("Accepting device: " + device.Id);
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnStartClicked()
    {
        Int32 res = _softAP.Start(ssidInput.text, passphraseInput.text);
        Log("Started 0x" + res.ToString("X8"));
    }

    private void OnStopClicked()
    {
        Int32 res = _softAP.Stop();
        Log("Stop 0x" + res.ToString("X8"));
    }

    private void OnGetIpSettingsClicked()
    {
        string address, mask, gateway, dns1, dns2;
        Int32 res = _softAP.GetIpSettings(out address, out mask, out gateway, out dns1, out dns2);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Read IP settings failed: 0x" + res.ToString("X8"));
            return;
        }

        Log("IP settings:");
        Log("  Address: " + address);
        Log("  Mask: "    + mask);
        Log("  Gateway: " + gateway);
        Log("  Dns1: "    + dns1);
        Log("  Dns2: "    + dns2);
    }

    private void OnSetIpSettingsClicked()
    {
        Int32 res = _softAP.SetIpSettings("192.168.2.10", "255.255.255.0",
            "192.168.2.1", "", "");
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Write IP settings failed: 0x" + res.ToString("X8"));
    }

    private void OnResetIpSettingsClicked()
    {
        Int32 res = _softAP.ResetIpSettings();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Reset IP settings failed: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Confirm panel
    // ==================================================================
    private void ShowConfirm(string message, Action<bool> callback)
    {
        _confirmCallback = callback;
        confirmText.text = message;
        confirmPanel.SetActive(true);
    }

    private void ConfirmResult(bool yes)
    {
        confirmPanel.SetActive(false);
        var cb = _confirmCallback;
        _confirmCallback = null;
        cb?.Invoke(yes);
    }

    // ==================================================================
    //  Log
    // ==================================================================
    private void Log(string msg)
    {
        Debug.Log("[WiFiDirectSoftAP] " + msg);
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