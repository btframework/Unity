using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclAudio;

public class AudioVolumeDemo : MonoBehaviour
{
    [Header("Endpoints")]
    public TMP_Dropdown endpointsDropdown;
    public Button openButton;
    public Button closeButton;

    [Header("Master")]
    public VerticalBarUI masterBar;
    public Slider masterSlider;
    public Toggle muteToggle;

    [Header("Channels")]
    public Transform channelsContainer;
    public GameObject channelBarPrefab;

    [Header("Log")]
    public TMP_Text logText;
    public ScrollRect logScrollRect;
    public Button clearButton;

    // ------------------------------------------------------------------
    private wclAudioSwitcher _audioSwitcher;
    private wclAudioMeter    _audioMeter;
    private wclAudioVolume   _audioVolume;

    private readonly List<string> _endpointIds   = new List<string>();
    private readonly List<string> _endpointNames = new List<string>();

    private VerticalBarUI[] _channelBars;

    private bool  _timerEnabled;
    private float _timerAccum;
    private const float TimerInterval = 0.1f;

    private void Awake()
    {
        openButton .onClick.AddListener(OnOpenClicked);
        closeButton.onClick.AddListener(OnCloseClicked);
        clearButton.onClick.AddListener(OnClearClicked);

        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        muteToggle  .onValueChanged.AddListener(OnMuteToggleChanged);
    }

    private void Start()
    {
        _audioSwitcher = new wclAudioSwitcher();
        _audioSwitcher.OnClosed               += AudioSwitcherClosed;
        _audioSwitcher.OnOpened               += AudioSwitcherOpened;
        _audioSwitcher.OnDeviceRemoved        += AudioSwitcherDeviceRemoved;
        _audioSwitcher.OnDeviceAdded          += AudioSwitcherDeviceAdded;
        _audioSwitcher.OnStateChanged         += AudioSwitcherStateChanged;
        _audioSwitcher.OnDefaultDeviceChanged += AudioSwitcherDefaultDeviceChanged;

        _audioMeter = new wclAudioMeter();
        _audioMeter.OnOpened       += AudioMeterOpened;
        _audioMeter.OnClosed       += AudioMeterClosed;
        _audioMeter.OnDisconnected += AudioMeterDisconnected;

        _audioVolume = new wclAudioVolume();
        _audioVolume.OnOpened       += AudioVolumeOpened;
        _audioVolume.OnClosed       += AudioVolumeClosed;
        _audioVolume.OnDisconnected += AudioVolumeDisconnected;
        _audioVolume.OnChanged      += AudioVolumeChanged;

        masterBar   .gameObject.SetActive(false);
        masterSlider.gameObject.SetActive(false);
        muteToggle  .gameObject.SetActive(false);

        Int32 res = _audioSwitcher.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio switcher open failed", res);
    }

    private void Update()
    {
        if (!_timerEnabled) return;
        _timerAccum += Time.deltaTime;
        if (_timerAccum < TimerInterval) return;
        _timerAccum = 0f;
        UpdateChannelsValue();
    }

    private void OnDestroy()
    {
        try
        {
            _audioVolume   ?.Close();
            _audioMeter    ?.Close();
            _audioSwitcher ?.Close();
        }
        catch (Exception e) { Debug.LogException(e); }

        if (_audioSwitcher != null)
        {
            _audioSwitcher.OnClosed               -= AudioSwitcherClosed;
            _audioSwitcher.OnOpened               -= AudioSwitcherOpened;
            _audioSwitcher.OnDeviceRemoved        -= AudioSwitcherDeviceRemoved;
            _audioSwitcher.OnDeviceAdded          -= AudioSwitcherDeviceAdded;
            _audioSwitcher.OnStateChanged         -= AudioSwitcherStateChanged;
            _audioSwitcher.OnDefaultDeviceChanged -= AudioSwitcherDefaultDeviceChanged;
        }

        if (_audioMeter != null)
        {
            _audioMeter.OnOpened       -= AudioMeterOpened;
            _audioMeter.OnClosed       -= AudioMeterClosed;
            _audioMeter.OnDisconnected -= AudioMeterDisconnected;
        }

        if (_audioVolume != null)
        {
            _audioVolume.OnOpened       -= AudioVolumeOpened;
            _audioVolume.OnClosed       -= AudioVolumeClosed;
            _audioVolume.OnDisconnected -= AudioVolumeDisconnected;
            _audioVolume.OnChanged      -= AudioVolumeChanged;
        }
    }

    private void AudioSwitcherOpened(object sender, EventArgs e)
    {
        Trace("Audio switcher opened");

        wclAudioDevice[] devices;
        Int32 res = _audioSwitcher.Enum(wclAudioDeviceState.asActive, out devices);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Enum audio endpoints failed", res);
            return;
        }

        if (devices == null || devices.Length == 0)
        {
            Trace("No active audio endpoints found");
            return;
        }

        foreach (var device in devices)
            AddDevice(device);

        if (_endpointIds.Count == 0)
            Trace("No output audio endpoints found");
        else
            SelectActiveDevice();
    }

    private void AudioSwitcherClosed(object sender, EventArgs e)
    {
        Trace("Audio switcher closed");
        _endpointIds.Clear();
        _endpointNames.Clear();
        RefreshEndpointsDropdown();
    }

    private void AudioSwitcherDefaultDeviceChanged(object sender, string id,
        wclAudioDeviceDataFlow flow, wclAudioDeviceRole role)
    {
        Trace("Active device changed");
        if (!_audioMeter.Active)
            SelectActiveDevice();
    }

    private void AudioSwitcherStateChanged(object sender, string id, wclAudioDeviceState state)
    {
        if (state == wclAudioDeviceState.asActive)
            AudioSwitcherDeviceAdded(_audioSwitcher, id);
        else
            DeleteDevice(id);
    }

    private void AudioSwitcherDeviceAdded(object sender, string id)
    {
        wclAudioDevice device;
        if (_audioSwitcher.GetDeviceDetails(id, out device) == wclErrors.WCL_E_SUCCESS)
            AddDevice(device);
    }

    private void AudioSwitcherDeviceRemoved(object sender, string id)
    {
        DeleteDevice(id);
    }

    private void AudioMeterOpened(object sender, EventArgs e)
    {
        Trace("Audio meter opened");
        endpointsDropdown.interactable = false;
        masterBar.gameObject.SetActive(true);

        wclAudioHardwareFeature features;
        Int32 res = _audioMeter.GetHardwareFeatures(out features);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get hardware features failed", res);
        else
        {
            Trace("Hardware features");
            if ((features & wclAudioHardwareFeature.hfVolume) != 0) Trace("  Volume");
            if ((features & wclAudioHardwareFeature.hfMute)   != 0) Trace("  Mute");
            if ((features & wclAudioHardwareFeature.hfMeter)  != 0) Trace("  Meter");
        }

        UInt32 channels;
        res = _audioMeter.GetChannels(out channels);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get channels count failed", res);
            return;
        }

        if (channels == 0)
        {
            Trace("No metering channels available");
            _audioMeter.Close();
            return;
        }

        Trace("Channels: " + channels);

        // Remove previous bars
        for (int i = channelsContainer.childCount - 1; i >= 0; i--)
            Destroy(channelsContainer.GetChild(i).gameObject);

        _channelBars = new VerticalBarUI[channels];
        for (uint i = 0; i < channels; i++)
        {
            var go = Instantiate(channelBarPrefab, channelsContainer);
            _channelBars[i] = go.GetComponent<VerticalBarUI>();
        }

        UpdateChannelsValue();
        _timerEnabled = true;
        _timerAccum = 0f;
    }

    private void AudioMeterClosed(object sender, EventArgs e)
    {
        Trace("Audio meter closed");
        _timerEnabled = false;
        endpointsDropdown.interactable = true;

        if (_channelBars != null)
        {
            for (int i = 0; i < _channelBars.Length; i++)
                if (_channelBars[i] != null) Destroy(_channelBars[i].gameObject);
            _channelBars = null;
        }

        masterBar.SetValue01(0f);
        masterBar.gameObject.SetActive(false);
    }

    private void AudioMeterDisconnected(object sender, EventArgs e)
    {
        Trace("Audio meter device " + _audioMeter.Id + " disconnected");
    }

    // ==================================================================
    //  Volume events
    // ==================================================================
    private void AudioVolumeOpened(object sender, EventArgs e)
    {
        Trace("Audio volume opened");
        endpointsDropdown.interactable = false;

        wclAudioHardwareFeature features;
        Int32 res = _audioVolume.GetHardwareFeatures(out features);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get hardware features failed", res);
        else
        {
            Trace("Hardware features");
            if ((features & wclAudioHardwareFeature.hfVolume) != 0) Trace("  Volume");
            if ((features & wclAudioHardwareFeature.hfMute)   != 0) Trace("  Mute");
            if ((features & wclAudioHardwareFeature.hfMeter)  != 0) Trace("  Meter");
        }

        float volume;
        res = _audioVolume.GetVolume(out volume);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get volume failed", res);
        else
        {
            masterSlider.gameObject.SetActive(true);
            masterSlider.SetValueWithoutNotify((int)(volume * 100f));
        }

        bool mute;
        res = _audioVolume.GetMute(out mute);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get mute failed", res);
        else
        {
            muteToggle.gameObject.SetActive(true);
            muteToggle.SetIsOnWithoutNotify(mute);
        }
    }

    private void AudioVolumeClosed(object sender, EventArgs e)
    {
        Trace("Audio volume closed");
        endpointsDropdown.interactable = true;

        masterSlider.gameObject.SetActive(false);
        masterSlider.SetValueWithoutNotify(0f);

        muteToggle.gameObject.SetActive(false);
    }

    private void AudioVolumeDisconnected(object sender, EventArgs e)
    {
        Trace("Audio volume device " + _audioVolume.Id + " disconnected");
    }

    private void AudioVolumeChanged(object sender, bool muted, float volume, float[] volumes)
    {
        if (!_audioVolume.Active) return;

        Trace("Volume changed");
        muteToggle  .SetIsOnWithoutNotify(muted);
        masterSlider.SetValueWithoutNotify((int)(volume * 100f));
    }

    // ==================================================================
    //  UI handlers
    // ==================================================================
    private void OnOpenClicked()
    {
        if (endpointsDropdown.value < 0 || endpointsDropdown.value >= _endpointIds.Count)
        {
            Trace("No audio endpoints found");
            return;
        }

        string id = _endpointIds[endpointsDropdown.value];

        Int32 res = _audioMeter.Open(id);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio meter open failed", res);

        res = _audioVolume.Open(id);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio volume open failed", res);
    }

    private void OnCloseClicked()
    {
        Int32 res = _audioVolume.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio volume close failed", res);

        res = _audioMeter.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio meter close failed", res);
    }

    private void OnClearClicked()
    {
        if (logText != null) logText.text = string.Empty;
    }

    private void OnMasterSliderChanged(float value)
    {
        if (_audioVolume == null || !_audioVolume.Active) return;
        Int32 res = _audioVolume.SetVolume(value / 100f);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Set volume failed", res);
    }

    private void OnMuteToggleChanged(bool value)
    {
        if (_audioVolume == null || !_audioVolume.Active) return;
        Int32 res = _audioVolume.SetMute(value);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Set mute failed", res);
    }

    // ==================================================================
    //  Devices
    // ==================================================================
    private void AddDevice(wclAudioDevice device)
    {
        // As in the original - add only active render devices.
        if (device.State != wclAudioDeviceState.asActive) return;
        if (device.Flow  != wclAudioDeviceDataFlow.dfRender) return;

        // The original did not filter duplicates, but we add a simple guard -
        // otherwise on some drivers the list may be duplicated.
        if (_endpointIds.Contains(device.Id)) return;

        _endpointIds.Add(device.Id);
        _endpointNames.Add(device.FriendlyName);
        RefreshEndpointsDropdown();
        SelectActiveDevice();
    }

    private void DeleteDevice(string id)
    {
        int idx = _endpointIds.IndexOf(id);
        if (idx < 0) return;

        _endpointIds.RemoveAt(idx);
        _endpointNames.RemoveAt(idx);
        RefreshEndpointsDropdown();

        if (_endpointIds.Count > 0)
            SelectActiveDevice();
    }

    private void SelectActiveDevice()
    {
        for (int i = 0; i < _endpointIds.Count; i++)
        {
            wclAudioDevice device;
            Int32 res = _audioSwitcher.GetDeviceDetails(_endpointIds[i], out device);
            if (res != wclErrors.WCL_E_SUCCESS) continue;

            if ((device.Roles & wclAudioDeviceRole.drMultimedia) != 0)
            {
                endpointsDropdown.SetValueWithoutNotify(i);
                return;
            }
        }
        // Failed to find a "multimedia" device - leave as is.
    }

    private void RefreshEndpointsDropdown()
    {
        if (endpointsDropdown == null) return;

        endpointsDropdown.ClearOptions();
        endpointsDropdown.AddOptions(_endpointNames);
        endpointsDropdown.RefreshShownValue();
    }

    // ==================================================================
    //  Meter polling
    // ==================================================================
    private void UpdateChannelsValue()
    {
        float peak;
        Int32 res = _audioMeter.GetPeak(out peak);
        if (res == wclErrors.WCL_E_SUCCESS)
        {
            // Same as the WinForms logic: if the volume slider is visible,
            // the peak is multiplied by the slider position (0..100).
            float display;
            if (masterSlider.gameObject.activeSelf)
                display = peak * (masterSlider.value / 100f);
            else
                display = peak;

            masterBar.SetValue01(display);
        }

        if (_channelBars != null && _channelBars.Length > 0)
        {
            float[] values;
            res = _audioMeter.GetChannelsPeak(out values);
            if (res == wclErrors.WCL_E_SUCCESS && values != null && values.Length > 0)
            {
                int n = Mathf.Min(values.Length, _channelBars.Length);
                for (int i = 0; i < n; i++)
                    if (_channelBars[i] != null) _channelBars[i].SetValue01(values[i]);
            }
        }
    }

    // ==================================================================
    //  Logging
    // ==================================================================
    private void Trace(string message)
    {
        Debug.Log("[AudioVolume] " + message);
        if (logText == null) return;

        logText.text += (logText.text.Length > 0 ? "\n" : "") + message;

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void Trace(string message, Int32 error)
    {
        Trace(message + ": 0x" + error.ToString("X8"));
    }
}