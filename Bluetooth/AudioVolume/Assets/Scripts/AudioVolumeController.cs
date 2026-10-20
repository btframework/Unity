using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclAudio;

public class AudioVolumeController : MonoBehaviour
{
    [Header("Controls")]
    public Button        btOpen;
    public Button        btClose;
    public Button        btClear;
    public TMP_Dropdown  cbEndpoints;
    public Toggle        cbMute;
    public Slider        tbMaster;

    [Header("Progress bars")]
    public GameObject    pbMasterRoot;
    public Image         pbMasterFill;
    public RectTransform barsParent;

    [Header("Log")]
    public TextMeshProUGUI lbLogText;

    // ==========================================================
    //  Library objects
    // ==========================================================
    private wclAudioSwitcher AudioSwitcher;
    private wclAudioMeter    AudioMeter;
    private wclAudioVolume   AudioVolume;

    // ==========================================================
    //  UI state
    // ==========================================================
    private readonly List<string>        logLinesRaw     = new List<string>();
    private readonly List<EndpointEntry> endpointEntries = new List<EndpointEntry>();
    private readonly List<Image>         channelBars     = new List<Image>();

    private int selectedEndpointIndex = -1;

    private bool _suppressDropdownEvent = false;
    private bool _suppressSliderEvent   = false;
    private bool _suppressToggleEvent   = false;

    private bool  meterTimerEnabled       = false;
    private float nextMeterTick           = 0f;
    private const float MeterTickInterval = 0.1f;

    // 1x1 white sprite used to make Image.Type.Filled work.
    // Without a sprite, Unity's Image ignores fillAmount/fillMethod.
    private Sprite _whiteSprite;

    private class EndpointEntry
    {
        public wclAudioDevice Device;
    }

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        _whiteSprite = CreateWhiteSprite();

        if (pbMasterFill != null && pbMasterFill.sprite == null)
        {
            pbMasterFill.sprite = _whiteSprite;
            pbMasterFill.type = Image.Type.Filled;
            pbMasterFill.fillMethod = Image.FillMethod.Vertical;
            pbMasterFill.fillOrigin = 0;
        }

        if (btOpen)  btOpen.onClick.AddListener(OnOpenClick);
        if (btClose) btClose.onClick.AddListener(OnCloseClick);
        if (btClear) btClear.onClick.AddListener(OnClearClick);

        if (cbEndpoints != null)
        {
            cbEndpoints.onValueChanged.AddListener(idx =>
            {
                if (_suppressDropdownEvent) return;
                selectedEndpointIndex = idx;
            });
        }

        if (tbMaster != null)
        {
            tbMaster.onValueChanged.AddListener(v =>
            {
                if (_suppressSliderEvent) return;
                OnMasterVolumeChanged(v);
            });
        }

        if (cbMute != null)
        {
            cbMute.onValueChanged.AddListener(on =>
            {
                if (_suppressToggleEvent) return;
                OnMuteChanged(on);
            });
        }
    }

    private void Start()
    {
        AudioSwitcher = new wclAudioSwitcher();
        AudioSwitcher.OnClosed               += AudioSwitcherClosed;
        AudioSwitcher.OnOpened               += AudioSwitcherOpened;
        AudioSwitcher.OnDeviceRemoved        += AudioSwitcherDeviceRemoved;
        AudioSwitcher.OnDeviceAdded          += AudioSwitcherDeviceAdded;
        AudioSwitcher.OnStateChanged         += AudioSwitcherStateChanged;
        AudioSwitcher.OnDefaultDeviceChanged += AudioSwitcherDefaultDeviceChanged;

        AudioMeter = new wclAudioMeter();
        AudioMeter.OnOpened       += AudioMeterOpened;
        AudioMeter.OnClosed       += AudioMeterClosed;
        AudioMeter.OnDisconnected += AudioMeterDisconnected;

        AudioVolume = new wclAudioVolume();
        AudioVolume.OnOpened       += AudioVolumeOpened;
        AudioVolume.OnClosed       += AudioVolumeClosed;
        AudioVolume.OnDisconnected += AudioVolumeDisconnected;
        AudioVolume.OnChanged      += AudioVolumeChanged;

        int Res = AudioSwitcher.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio switcher open failed", Res);
    }

    private void Update()
    {
        if (!meterTimerEnabled) return;
        if (Time.time < nextMeterTick) return;
        nextMeterTick = Time.time + MeterTickInterval;
        UpdateChannelsValue();
    }

    private void OnDestroy()
    {
        meterTimerEnabled = false;
        try { AudioVolume?.Close(); }   catch { }
        try { AudioMeter?.Close(); }    catch { }
        try { AudioSwitcher?.Close(); } catch { }

        if (_whiteSprite != null)
        {
            if (_whiteSprite.texture != null) Destroy(_whiteSprite.texture);
            Destroy(_whiteSprite);
            _whiteSprite = null;
        }
    }

    // ==========================================================
    //  White sprite helper
    // ==========================================================
    private static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, false);
        var spr = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f,
            (uint)SpriteMeshType.FullRect);
        spr.name = "RuntimeWhiteFillSprite";
        return spr;
    }

    private Sprite WhiteSprite => _whiteSprite != null ? _whiteSprite : (_whiteSprite = CreateWhiteSprite());

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void OnOpenClick()
    {
        if (selectedEndpointIndex < 0 || selectedEndpointIndex >= endpointEntries.Count)
        {
            Trace("No audio endpoints found");
            return;
        }

        string id = endpointEntries[selectedEndpointIndex].Device.Id;

        int Res = AudioMeter.Open(id);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio meter open failed", Res);

        Res = AudioVolume.Open(id);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio volume open failed", Res);
    }

    private void OnCloseClick()
    {
        int Res = AudioVolume.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio volume close failed", Res);

        Res = AudioMeter.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Audio meter close failed", Res);
    }

    private void OnClearClick()
    {
        logLinesRaw.Clear();
        RefreshLogDisplay();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioVolume == null || !AudioVolume.Active) return;
        int Res = AudioVolume.SetVolume(value / 100f);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Set volume failed", Res);
    }

    private void OnMuteChanged(bool muted)
    {
        if (AudioVolume == null || !AudioVolume.Active) return;
        int Res = AudioVolume.SetMute(muted);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Set mute failed", Res);
    }

    // ==========================================================
    //  Audio switcher events
    // ==========================================================
    private void AudioSwitcherOpened(object sender, EventArgs e)
    {
        Trace("Audio switcher opened");

        int Res = AudioSwitcher.Enum(wclAudioDeviceState.asActive, out wclAudioDevice[] devices);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Enum audio endpoints failed", Res);
            return;
        }

        if (devices == null || devices.Length == 0)
        {
            Trace("No active audio endpoints found");
            return;
        }

        foreach (var device in devices)
            AddDevice(device);

        if (endpointEntries.Count == 0)
            Trace("No output audio endpoints found");
        else
            SelectActiveDevice();
    }

    private void AudioSwitcherClosed(object sender, EventArgs e)
    {
        Trace("Audio switcher closed");
        endpointEntries.Clear();
        RebuildEndpointsDropdown();
    }

    private void AudioSwitcherDeviceAdded(object sender, string id)
    {
        if (AudioSwitcher.GetDeviceDetails(id, out wclAudioDevice device) == wclErrors.WCL_E_SUCCESS)
            AddDevice(device);
    }

    private void AudioSwitcherDeviceRemoved(object sender, string id)
    {
        DeleteDevice(id);
    }

    private void AudioSwitcherStateChanged(object sender, string id, wclAudioDeviceState state)
    {
        if (state == wclAudioDeviceState.asActive)
            AudioSwitcherDeviceAdded(AudioSwitcher, id);
        else
            DeleteDevice(id);
    }

    private void AudioSwitcherDefaultDeviceChanged(object sender, string id,
        wclAudioDeviceDataFlow flow, wclAudioDeviceRole role)
    {
        Trace("Active device changed");
        if (AudioMeter == null || !AudioMeter.Active)
            SelectActiveDevice();
    }

    // ==========================================================
    //  Audio meter events
    // ==========================================================
    private void AudioMeterOpened(object sender, EventArgs e)
    {
        Trace("Audio meter opened");
        cbEndpoints.interactable = false;
        pbMasterRoot.SetActive(true);

        int Res = AudioMeter.GetHardwareFeatures(out wclAudioHardwareFeature features);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Get hardware features failed", Res);
        else
        {
            Trace("Hardware features");
            if ((features & wclAudioHardwareFeature.hfVolume) != 0) Trace("  Volume");
            if ((features & wclAudioHardwareFeature.hfMute)   != 0) Trace("  Mute");
            if ((features & wclAudioHardwareFeature.hfMeter)  != 0) Trace("  Meter");
        }

        Res = AudioMeter.GetChannels(out uint channels);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get channels count failed", Res);
            return;
        }

        if (channels == 0)
        {
            Trace("No mettering channels available");
            AudioMeter.Close();
            return;
        }

        Trace("Channels: " + channels);
        ClearChannelBars();

        for (uint i = 0; i < channels; i++)
        {
            var bar = CreateChannelBar((int)i);
            channelBars.Add(bar);
        }

        UpdateChannelsValue();
        meterTimerEnabled = true;
        nextMeterTick = Time.time + MeterTickInterval;
    }

    private void AudioMeterClosed(object sender, EventArgs e)
    {
        Trace("Audio meter closed");
        meterTimerEnabled = false;
        cbEndpoints.interactable = true;

        ClearChannelBars();

        if (pbMasterFill != null) pbMasterFill.fillAmount = 0f;
        if (pbMasterRoot != null) pbMasterRoot.SetActive(false);
    }

    private void AudioMeterDisconnected(object sender, EventArgs e)
    {
        Trace("Audio meter device " + AudioMeter.Id + " disconnected");
    }

    // ==========================================================
    //  Audio volume events
    // ==========================================================
    private void AudioVolumeOpened(object sender, EventArgs e)
    {
        Trace("Audio volume opened");
        cbEndpoints.interactable = false;

        int Res = AudioVolume.GetHardwareFeatures(out wclAudioHardwareFeature features);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Get hardware features failed", Res);
        else
        {
            Trace("Hardware features");
            if ((features & wclAudioHardwareFeature.hfVolume) != 0) Trace("  Volume");
            if ((features & wclAudioHardwareFeature.hfMute)   != 0) Trace("  Mute");
            if ((features & wclAudioHardwareFeature.hfMeter)  != 0) Trace("  Meter");
        }

        Res = AudioVolume.GetVolume(out float volume);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Get volume failed", Res);
        else
        {
            tbMaster.gameObject.SetActive(true);
            SetSliderValue((int)(volume * 100f));
        }

        Res = AudioVolume.GetMute(out bool mute);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace("Get mute failed", Res);
        else
        {
            cbMute.gameObject.SetActive(true);
            SetToggleValue(mute);
        }
    }

    private void AudioVolumeClosed(object sender, EventArgs e)
    {
        Trace("Audio volume closed");
        cbEndpoints.interactable = true;

        tbMaster.gameObject.SetActive(false);
        SetSliderValue(0);

        cbMute.gameObject.SetActive(false);
    }

    private void AudioVolumeDisconnected(object sender, EventArgs e)
    {
        Trace("Audio volume device " + AudioVolume.Id + " disconnected");
    }

    private void AudioVolumeChanged(object sender, bool muted, float volume, float[] volumes)
    {
        if (AudioVolume == null || !AudioVolume.Active) return;
        Trace("Volume changed");
        SetToggleValue(muted);
        SetSliderValue((int)(volume * 100f));
    }

    // ==========================================================
    //  Endpoints list management
    // ==========================================================
    private void AddDevice(wclAudioDevice device)
    {
        if (device.State != wclAudioDeviceState.asActive)   return;
        if (device.Flow  != wclAudioDeviceDataFlow.dfRender) return;

        var entry = new EndpointEntry { Device = device };
        endpointEntries.Add(entry);
        RebuildEndpointsDropdown();
        SelectActiveDevice();
    }

    private void DeleteDevice(string id)
    {
        if (endpointEntries.Count == 0) return;

        for (int i = 0; i < endpointEntries.Count; i++)
        {
            if (endpointEntries[i].Device.Id == id)
            {
                endpointEntries.RemoveAt(i);
                RebuildEndpointsDropdown();
                if (endpointEntries.Count > 0)
                    SelectActiveDevice();
                break;
            }
        }
    }

    private void SelectActiveDevice()
    {
        selectedEndpointIndex = -1;

        if (endpointEntries.Count > 0)
        {
            for (int i = 0; i < endpointEntries.Count; i++)
            {
                var entry = endpointEntries[i];
                if (AudioSwitcher.GetDeviceDetails(entry.Device.Id, out wclAudioDevice fresh) == wclErrors.WCL_E_SUCCESS)
                    entry.Device = fresh;

                if ((entry.Device.Roles & wclAudioDeviceRole.drMultimedia) != 0)
                {
                    selectedEndpointIndex = i;
                    _suppressDropdownEvent = true;
                    cbEndpoints.value = i;
                    _suppressDropdownEvent = false;
                    return;
                }
            }
        }
    }

    private void RebuildEndpointsDropdown()
    {
        _suppressDropdownEvent = true;

        var options = new List<TMP_Dropdown.OptionData>(endpointEntries.Count);
        foreach (var e in endpointEntries)
            options.Add(new TMP_Dropdown.OptionData(e.Device.FriendlyName));

        cbEndpoints.ClearOptions();
        if (options.Count > 0)
        {
            cbEndpoints.AddOptions(options);
            if (selectedEndpointIndex >= 0 && selectedEndpointIndex < endpointEntries.Count)
                cbEndpoints.value = selectedEndpointIndex;
            else
                cbEndpoints.value = 0;
        }
        cbEndpoints.RefreshShownValue();

        _suppressDropdownEvent = false;
    }

    // ==========================================================
    //  Meter values
    // ==========================================================
    private void UpdateChannelsValue()
    {
        int Res = AudioMeter.GetPeak(out float value);
        if (Res == wclErrors.WCL_E_SUCCESS)
        {
            if (tbMaster != null && tbMaster.gameObject.activeSelf)
            {
                float scaled = value * (tbMaster.value / 100f);
                if (pbMasterFill != null) pbMasterFill.fillAmount = scaled;
            }
            else
            {
                if (pbMasterFill != null) pbMasterFill.fillAmount = value;
            }
        }

        if (channelBars.Count > 0)
        {
            Res = AudioMeter.GetChannelsPeak(out float[] values);
            if (Res == wclErrors.WCL_E_SUCCESS && values != null && values.Length > 0)
            {
                for (int i = 0; i < values.Length && i < channelBars.Count; i++)
                {
                    if (channelBars[i] != null)
                        channelBars[i].fillAmount = values[i];
                }
            }
        }
    }

    // ==========================================================
    //  Channel bars
    // ==========================================================
    private Image CreateChannelBar(int index)
    {
        var root = new GameObject("ChannelBar_" + index,
            typeof(RectTransform), typeof(Image), typeof(Outline));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(barsParent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(index * 20f, 0f);
        rt.sizeDelta = new Vector2(15f, 97f);

        var bg = root.GetComponent<Image>();
        bg.color = new Color(0.95f, 0.95f, 0.95f);
        var outline = root.GetComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.5f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(root.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1, 1);
        fillRT.offsetMax = new Vector2(-1, -1);

        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.75f, 0.2f);
        fillImg.sprite = WhiteSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = 0; // Bottom
        fillImg.fillAmount = 0f;

        return fillImg;
    }

    private void ClearChannelBars()
    {
        foreach (var bar in channelBars)
        {
            if (bar == null) continue;
            var root = bar.transform.parent != null ? bar.transform.parent.gameObject : bar.gameObject;
            Destroy(root);
        }
        channelBars.Clear();
    }

    // ==========================================================
    //  Slider / toggle helpers (avoid feedback loops)
    // ==========================================================
    private void SetSliderValue(int v)
    {
        if (tbMaster == null) return;
        _suppressSliderEvent = true;
        tbMaster.value = v;
        _suppressSliderEvent = false;
    }

    private void SetToggleValue(bool v)
    {
        if (cbMute == null) return;
        _suppressToggleEvent = true;
        cbMute.isOn = v;
        _suppressToggleEvent = false;
    }

    // ==========================================================
    //  Log
    // ==========================================================
    private void Trace(string msg)
    {
        logLinesRaw.Add(msg);
        RefreshLogDisplay();
    }

    private void Trace(string msg, int error)
    {
        Trace(msg + ": 0x" + error.ToString("X8"));
    }

    private void RefreshLogDisplay()
    {
        if (lbLogText != null)
            lbLogText.text = string.Join("\n", logLinesRaw);
    }
}