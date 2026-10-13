using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclAudio;

public class BluetoothAudioReceiverDemo : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button stopButton;
    public Button openButton;
    public Button closeButton;
    public Button connectButton;
    public Button clearButton;

    [Header("Devices list")]
    public Transform devicesContent;
    public GameObject deviceRowPrefab;
    public TMP_Text selectedDeviceLabel;

    [Header("Log")]
    public TMP_Text logText;
    public ScrollRect logScrollRect;

    private wclBluetoothAudioWatcher _audioWatcher;
    private wclBluetoothAudioReceiver _audioReceiver;

    private string _selectedDeviceId;
    private readonly Dictionary<string, DeviceRowUI> _rows = new Dictionary<string, DeviceRowUI>();

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        openButton.onClick.AddListener(OnOpenClicked);
        closeButton.onClick.AddListener(OnCloseClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
        clearButton.onClick.AddListener(OnClearClicked);
    }

    private void Start()
    {
        // ВАЖНО: создаём на главном потоке — тогда все события придут сюда же.
        _audioWatcher = new wclBluetoothAudioWatcher();
        _audioWatcher.OnDeviceAdded   += AudioWatcherDeviceAdded;
        _audioWatcher.OnDeviceRemoved += AudioWatcherDeviceRemoved;
        _audioWatcher.OnStarted       += AudioWatcherStarted;
        _audioWatcher.OnStopped       += AudioWatcherStopped;

        _audioReceiver = new wclBluetoothAudioReceiver();
        _audioReceiver.OnClosed       += AudioReceiverOnClosed;
        _audioReceiver.OnListen       += AudioReceiverOnListen;
        _audioReceiver.OnConnected    += AudioReceiverOnConnected;
        _audioReceiver.OnDisconnected += AudioReceiverOnDisconnected;

        Log("Ready. Press Start to discover devices.");
    }

    private void OnDestroy()
    {
        try
        {
            _audioWatcher?.Stop();
            _audioReceiver?.Close();
        }
        catch (Exception e) { Debug.LogException(e); }

        if (_audioWatcher != null)
        {
            _audioWatcher.OnDeviceAdded   -= AudioWatcherDeviceAdded;
            _audioWatcher.OnDeviceRemoved -= AudioWatcherDeviceRemoved;
            _audioWatcher.OnStarted       -= AudioWatcherStarted;
            _audioWatcher.OnStopped       -= AudioWatcherStopped;
        }

        if (_audioReceiver != null)
        {
            _audioReceiver.OnClosed       -= AudioReceiverOnClosed;
            _audioReceiver.OnListen       -= AudioReceiverOnListen;
            _audioReceiver.OnConnected    -= AudioReceiverOnConnected;
            _audioReceiver.OnDisconnected -= AudioReceiverOnDisconnected;
        }
    }

    // ---------------------------------------------------------------- кнопки

    private void OnStartClicked()
    {
        Int32 res = _audioWatcher.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Watcher start failed: 0x" + res.ToString("X8"));
    }

    private void OnStopClicked()
    {
        Int32 res = _audioWatcher.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Watcher stop failed: 0x" + res.ToString("X8"));
    }

    private void OnOpenClicked()
    {
        if (string.IsNullOrEmpty(_selectedDeviceId))
        {
            Log("Select device");
            return;
        }

        Int32 res = _audioReceiver.Listen(_selectedDeviceId);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Open failed: 0x" + res.ToString("X8"));
    }

    private void OnCloseClicked()
    {
        Int32 res = _audioReceiver.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Close failed: 0x" + res.ToString("X8"));
    }

    private void OnConnectClicked()
    {
        Int32 res = _audioReceiver.Connect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Connect failed: 0x" + res.ToString("X8"));
    }

    private void OnClearClicked()
    {
        if (logText != null) logText.text = string.Empty;
    }

    // ---------------------------------------------------------------- события watcher'а

    private void AudioWatcherDeviceAdded(object sender, string id, string name)
    {
        if (!_rows.ContainsKey(id))
        {
            var go = Instantiate(deviceRowPrefab, devicesContent);
            var row = go.GetComponent<DeviceRowUI>();
            row.Init(id, name, OnDeviceRowClicked);
            _rows[id] = row;
        }
        Log("Device added: " + id);
    }

    private void AudioWatcherDeviceRemoved(object sender, string id)
    {
        if (_rows.TryGetValue(id, out var row))
        {
            if (_selectedDeviceId == id)
            {
                _selectedDeviceId = null;
                if (selectedDeviceLabel != null) selectedDeviceLabel.text = "Selected: (none)";
            }
            Destroy(row.gameObject);
            _rows.Remove(id);
        }
        Log("Device removed: " + id);
    }

    private void AudioWatcherStarted(object sender, EventArgs e)
    {
        Log("Watcher started");
        ClearDevices();
    }

    private void AudioWatcherStopped(object sender, EventArgs e)
    {
        Log("Watcher stopped");
    }

    // ---------------------------------------------------------------- события receiver'а

    private void AudioReceiverOnDisconnected(object sender, EventArgs e) => Log("Remote device disconnected");
    private void AudioReceiverOnConnected   (object sender, EventArgs e) => Log("Remote device connected");
    private void AudioReceiverOnListen      (object sender, EventArgs e) => Log("Audio receiver listening");
    private void AudioReceiverOnClosed      (object sender, EventArgs e) => Log("Audio receiver closed");

    // ---------------------------------------------------------------- вспомогательное

    private void OnDeviceRowClicked(string id)
    {
        foreach (var kv in _rows) kv.Value.SetSelected(kv.Key == id);
        _selectedDeviceId = id;
        if (selectedDeviceLabel != null) selectedDeviceLabel.text = "Selected: " + id;
    }

    private void ClearDevices()
    {
        foreach (var kv in _rows) if (kv.Value != null) Destroy(kv.Value.gameObject);
        _rows.Clear();
        _selectedDeviceId = null;
        if (selectedDeviceLabel != null) selectedDeviceLabel.text = "Selected: (none)";
    }

    private void Log(string message)
    {
        Debug.Log("[BT Audio] " + message);
        if (logText == null) return;

        logText.text += (logText.text.Length > 0 ? "\n" : "") + message;

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}