using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using TMPro;

using wclWiFi;
using wclCommon;

// ==========================================================
//  WinForms-style dialog enums + MessageBox
// ==========================================================
public enum DialogResult { None, OK, Cancel, Yes, No }
public enum MessageBoxButtons { OK, OKCancel, YesNo, YesNoCancel }
public enum MessageBoxIcon { None, Error, Question, Warning, Information }

public static class MessageBox
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public static DialogResult Show(string text) => Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);
    public static DialogResult Show(string text, string caption) => Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) => Show(text, caption, buttons, MessageBoxIcon.None);

    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        uint flags = 0;
        switch (buttons)
        {
            case MessageBoxButtons.OK:          flags = 0x00000000; break;
            case MessageBoxButtons.OKCancel:    flags = 0x00000001; break;
            case MessageBoxButtons.YesNo:       flags = 0x00000004; break;
            case MessageBoxButtons.YesNoCancel: flags = 0x00000003; break;
        }
        switch (icon)
        {
            case MessageBoxIcon.Error:       flags |= 0x00000010; break;
            case MessageBoxIcon.Question:    flags |= 0x00000020; break;
            case MessageBoxIcon.Warning:     flags |= 0x00000030; break;
            case MessageBoxIcon.Information: flags |= 0x00000040; break;
        }
        int r = MessageBoxW(IntPtr.Zero, text ?? string.Empty, caption ?? string.Empty, flags);
        switch (r)
        {
            case 1: return DialogResult.OK;
            case 2: return DialogResult.Cancel;
            case 6: return DialogResult.Yes;
            case 7: return DialogResult.No;
            default: return DialogResult.None;
        }
    }
}

// ==========================================================
//  WinForms-style ListBox
// ==========================================================
public class ListBox
{
    private readonly List<string> _lines = new List<string>();
    private readonly TextMeshProUGUI _text;

    public ListBox(TextMeshProUGUI text) { _text = text; Items = new ListBoxItems(this); }

    public ListBoxItems Items { get; private set; }

    internal void Add(string s) { _lines.Add(s ?? ""); Refresh(); }
    internal void Clear() { _lines.Clear(); Refresh(); }
    internal int Count => _lines.Count;

    private void Refresh()
    {
        if (_text != null) _text.text = string.Join("\n", _lines);
    }
}

public class ListBoxItems
{
    private readonly ListBox _owner;
    internal ListBoxItems(ListBox owner) { _owner = owner; }

    public int Count => _owner.Count;
    public void Add(string s) => _owner.Add(s);
    public void Clear() => _owner.Clear();
}

// ==========================================================
//  Controller
// ==========================================================
[Preserve]
public class WiFiDirectSoftAPController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btStart;
    public Button btStop;
    public Button btGetIpSettings;
    public Button btSetIpSettings;
    public Button btResetIpSettings;

    [Header("Inputs")]
    public TMP_InputField edSsid;
    public TMP_InputField edPassphrase;

    [Header("ListBox")]
    public TextMeshProUGUI lbLogText;

    private ListBox lbLog;

    private wclWiFiSoftAP wclSoftAP;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lbLog = new ListBox(lbLogText);

        btStart.onClick.AddListener(() => Dispatch(btStart_Click));
        btStop.onClick.AddListener(() => Dispatch(btStop_Click));
        btGetIpSettings.onClick.AddListener(() => Dispatch(btGetIpSettings_Click));
        btSetIpSettings.onClick.AddListener(() => Dispatch(btSetIpSettings_Click));
        btResetIpSettings.onClick.AddListener(() => Dispatch(btResetIpSettings_Click));
    }

    private void Start() { Dispatch(DoStart); }

    private void DoStart()
    {
        wclSoftAP = new wclWiFiSoftAP();
        wclSoftAP.OnStarted               += new EventHandler(wclSoftAP_OnStarted);
        wclSoftAP.OnStopped               += new EventHandler(wclSoftAP_OnStopped);
        wclSoftAP.OnDeviceConnected       += new wclWiFiDirectSoftAPConnectedEvent(wclSoftAP_OnDeviceConnected);
        wclSoftAP.OnDeviceConnectError    += new wclWiFiDirectSoftAPConnectErrorEvent(wclSoftAP_OnDeviceConnectError);
        wclSoftAP.OnDeviceDisconnected    += new wclWiFiDirectSoftAPDisconnectedEvent(wclSoftAP_OnDeviceDisconnected);
        wclSoftAP.OnDeviceAccept          += new wclWiFiDirectSoftAPAcceptEvent(wclSoftAP_OnDeviceAccept);
    }

    private void Update()
    {
        while (true)
        {
            Action a;
            lock (_pendingActions)
            {
                if (_pendingActions.Count == 0) break;
                a = _pendingActions.Dequeue();
            }
            try { a?.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    private void OnDestroy()
    {
        if (wclSoftAP != null) { try { wclSoftAP.Stop(); } catch { } }
        wclSoftAP = null;
    }

    // ==========================================================
    //  Dispatcher
    // ==========================================================
    private void Dispatch(Action a) { lock (_pendingActions) { _pendingActions.Enqueue(a); } }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btStart_Click()
    {
        Int32 Res = wclSoftAP.Start(edSsid.text, edPassphrase.text);
        lbLog.Items.Add("Started 0x" + Res.ToString("X8"));
    }

    private void btStop_Click()
    {
        Int32 Res = wclSoftAP.Stop();
        lbLog.Items.Add("Stop 0x" + Res.ToString("X8"));
    }

    private void btGetIpSettings_Click()
    {
        String Address;
        String Mask;
        String Gateway;
        String Dns1;
        String Dns2;
        Int32 Res = wclSoftAP.GetIpSettings(out Address, out Mask, out Gateway, out Dns1, out Dns2);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            lbLog.Items.Add("Read IP settings failed: 0x" + Res.ToString("X8"));
        }
        else
        {
            lbLog.Items.Add("IP settings:");
            lbLog.Items.Add("  Address: " + Address);
            lbLog.Items.Add("  Mask: " + Mask);
            lbLog.Items.Add("  Gateway: " + Gateway);
            lbLog.Items.Add("  Dns1: " + Dns1);
            lbLog.Items.Add("  Dns2: " + Dns2);
        }
    }

    private void btSetIpSettings_Click()
    {
        Int32 Res = wclSoftAP.SetIpSettings("192.168.2.10", "255.255.255.0",
            "192.168.2.1", "", "");
        if (Res != wclErrors.WCL_E_SUCCESS)
            lbLog.Items.Add("Write IP settings failed: 0x" + Res.ToString("X8"));
    }

    private void btResetIpSettings_Click()
    {
        Int32 Res = wclSoftAP.ResetIpSettings();
        if (Res != wclErrors.WCL_E_SUCCESS)
            lbLog.Items.Add("Reset IP settings failed: 0x" + Res.ToString("X8"));
    }

    // ==========================================================
    //  SoftAP events
    // ==========================================================
    private void wclSoftAP_OnStarted(object sender, EventArgs e)
    {
        // Capture SSID and passphrase synchronously, then dispatch only the UI update.
        string ssid;
        Int32 res1 = wclSoftAP.GetSsid(out ssid);
        if (res1 != wclErrors.WCL_E_SUCCESS)
            ssid = "Error 0x" + res1.ToString("X8");

        string pass;
        Int32 res2 = wclSoftAP.GetPassphrase(out pass);
        if (res2 != wclErrors.WCL_E_SUCCESS)
            pass = "Error 0x" + res2.ToString("X8");

        string s = ssid;
        string p = pass;

        Dispatch(() =>
        {
            lbLog.Items.Add("Started");
            lbLog.Items.Add("SSID: " + s);
            lbLog.Items.Add("Passphrase: " + p);
        });
    }

    private void wclSoftAP_OnStopped(object sender, EventArgs e)
    {
        Dispatch(() => lbLog.Items.Add("Stopped"));
    }

    private void wclSoftAP_OnDeviceConnected(object Sender, wclWiFiDirectDevice Device)
    {
        string name = Device.Name ?? "";
        string localAddr = Device.LocalAddress ?? "";
        string remoteAddr = Device.RemoteAddress ?? "";

        Dispatch(() =>
        {
            lbLog.Items.Add("Device connected: " + name);
            lbLog.Items.Add("  Local IP: " + localAddr);
            lbLog.Items.Add("  Remote IP: " + remoteAddr);
        });
    }

    private void wclSoftAP_OnDeviceConnectError(object Sender, wclWiFiDirectDevice Device, int Error)
    {
        string name = Device.Name ?? "";
        string err = Error.ToString("X8");

        Dispatch(() => lbLog.Items.Add("Device " + name + " connect error: " + err));
    }

    private void wclSoftAP_OnDeviceDisconnected(object Sender, wclWiFiDirectDevice Device)
    {
        string name = Device.Name ?? "";
        string localAddr = Device.LocalAddress ?? "";
        string remoteAddr = Device.RemoteAddress ?? "";

        Dispatch(() =>
        {
            lbLog.Items.Add("Device disconnected: " + name);
            lbLog.Items.Add("  Local IP: " + localAddr);
            lbLog.Items.Add("  Remote IP: " + remoteAddr);
        });
    }

    private void wclSoftAP_OnDeviceAccept(object Sender, wclWiFiDirectDevice Device, out bool Accept)
    {
        // Synchronous out: blocking MessageBox matches WinForms behaviour.
        string id = Device.Id ?? "";
        Accept = (MessageBox.Show("Accept device: " + id, "Accept",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
    }
}