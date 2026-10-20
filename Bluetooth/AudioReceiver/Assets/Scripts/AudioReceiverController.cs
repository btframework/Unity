using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclAudio;

public class AudioReceiverController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btStart;
    public Button btStop;
    public Button btOpen;
    public Button btClose;
    public Button btConnect;
    public Button btClear;

    [Header("Devices List (ListView)")]
    public ScrollRect     lvDevicesScroll;
    public RectTransform  lvDevicesContent;

    [Header("Log (ListBox)")]
    public TextMeshProUGUI lbLogText;

    private wclBluetoothAudioWatcher  AudioWatcher;
    private wclBluetoothAudioReceiver AudioReceiver;

    private readonly List<string>          logLines   = new List<string>();
    private readonly List<DeviceRowEntry>  deviceRows = new List<DeviceRowEntry>();
    private DeviceRowEntry selectedRow;

    private class DeviceRowEntry
    {
        public string     Id;
        public string     Name;
        public GameObject GO;
        public Button     Button;
        public Image      Background;
    }

    private void Awake()
    {
        if (btStart)   btStart.onClick.AddListener(OnStartClick);
        if (btStop)    btStop.onClick.AddListener(OnStopClick);
        if (btOpen)    btOpen.onClick.AddListener(OnOpenClick);
        if (btClose)   btClose.onClick.AddListener(OnCloseClick);
        if (btConnect) btConnect.onClick.AddListener(OnConnectClick);
        if (btClear)   btClear.onClick.AddListener(OnClearClick);
    }

    private void Start()
    {
        AudioWatcher = new wclBluetoothAudioWatcher();
        AudioWatcher.OnDeviceAdded   += AudioWatcherDeviceAdded;
        AudioWatcher.OnDeviceRemoved += AudioWatcherDeviceRemoved;
        AudioWatcher.OnStarted       += AudioWatcherStarted;
        AudioWatcher.OnStopped       += AudioWatcherStopped;

        AudioReceiver = new wclBluetoothAudioReceiver();
        AudioReceiver.OnClosed       += AudioReceiver_OnClosed;
        AudioReceiver.OnListen       += AudioReceiver_OnListen;
        AudioReceiver.OnConnected    += AudioReceiver_OnConnected;
        AudioReceiver.OnDisconnected += AudioReceiver_OnDisconnected;
    }

    private void OnDestroy()
    {
        try { AudioWatcher?.Stop(); }  catch { }
        try { AudioReceiver?.Close(); } catch { }
    }

    private void OnStartClick()
    {
        int Res = AudioWatcher.Start();
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Watcher start failed: 0x" + Res.ToString("X8"));
    }

    private void OnStopClick()
    {
        int Res = AudioWatcher.Stop();
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Watcher stop failed: 0x" + Res.ToString("X8"));
    }

    private void OnOpenClick()
    {
        if (selectedRow == null)
        {
            AddLog("Select device");
        }
        else
        {
            int Res = AudioReceiver.Listen(selectedRow.Id);
            if (Res != wclErrors.WCL_E_SUCCESS)
                AddLog("Open failed: 0x" + Res.ToString("X8"));
        }
    }

    private void OnCloseClick()
    {
        int Res = AudioReceiver.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Close failed: 0x" + Res.ToString("X8"));
    }

    private void OnConnectClick()
    {
        int Res = AudioReceiver.Connect();
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Connect failed: 0x" + Res.ToString("X8"));
    }

    private void OnClearClick()
    {
        logLines.Clear();
        RefreshLogDisplay();
    }

    private void AudioReceiver_OnDisconnected(object sender, EventArgs e) => AddLog("Remote device disconnected");
    private void AudioReceiver_OnConnected  (object sender, EventArgs e) => AddLog("Remote device connected");
    private void AudioReceiver_OnListen     (object sender, EventArgs e) => AddLog("Audio receiver listening");
    private void AudioReceiver_OnClosed     (object sender, EventArgs e) => AddLog("Audio receiver closed");

    private void AudioWatcherDeviceAdded(object sender, string id, string name)
    {
        AddDeviceRow(id, name);
        AddLog("Device added: " + id);
    }

    private void AudioWatcherDeviceRemoved(object sender, string id)
    {
        var entry = deviceRows.Find(r => r.Id == id);
        if (entry != null)
        {
            if (selectedRow == entry) selectedRow = null;
            deviceRows.Remove(entry);
            if (entry.GO != null) Destroy(entry.GO);
        }
        AddLog("Device removed: " + id);
    }

    private void AudioWatcherStarted(object sender, EventArgs e)
    {
        AddLog("Watcher started");
        foreach (var r in deviceRows)
            if (r.GO != null) Destroy(r.GO);
        deviceRows.Clear();
        selectedRow = null;
    }

    private void AudioWatcherStopped(object sender, EventArgs e)
    {
        AddLog("Watcher stopped");
    }

    private void AddLog(string line)
    {
        logLines.Add(line);
        RefreshLogDisplay();
    }

    private void RefreshLogDisplay()
    {
        if (lbLogText != null)
            lbLogText.text = string.Join("\n", logLines);
    }

    private void AddDeviceRow(string id, string name)
    {
        if (lvDevicesContent == null) return;

        var rowGO = new GameObject("Row_" + id, typeof(RectTransform), typeof(Image), typeof(Button));
        rowGO.transform.SetParent(lvDevicesContent, false);

        var rt = rowGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 18);

        var img = rowGO.GetComponent<Image>();
        img.color = Color.white;

        var btn = rowGO.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        colors.selectedColor    = Color.white;
        btn.colors = colors;

        var le = rowGO.AddComponent<LayoutElement>();
        le.minHeight = 18;
        le.preferredHeight = 18;

        CreateRowText(rowGO.transform, "IdText",   id,   2f,   246f);
        CreateRowText(rowGO.transform, "NameText", name, 250f, 148f);

        var entry = new DeviceRowEntry { Id = id, Name = name, GO = rowGO, Button = btn, Background = img };
        deviceRows.Add(entry);

        btn.onClick.AddListener(() => SelectRow(entry));
    }

    private void CreateRowText(Transform parent, string name, string text, float x, float w)
    {
        var tGO = new GameObject(name, typeof(RectTransform));
        tGO.transform.SetParent(parent, false);
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 0.5f);
        tRT.anchorMax = new Vector2(0, 0.5f);
        tRT.pivot     = new Vector2(0, 0.5f);
        tRT.anchoredPosition = new Vector2(x, 0);
        tRT.sizeDelta = new Vector2(w, 18);

        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
    }

    private void SelectRow(DeviceRowEntry entry)
    {
        if (selectedRow != null && selectedRow.Background != null)
            selectedRow.Background.color = Color.white;
        selectedRow = entry;
        if (selectedRow != null && selectedRow.Background != null)
            selectedRow.Background.color = new Color(0.7f, 0.85f, 1f);
    }
}