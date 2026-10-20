using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclAudio;

public class AudioSwitcherController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btEnum;
    public Button btSetDefault;
    public Button btRefresh;
    public Button btConnect;
    public Button btDisconnect;
    public Button btGetDefault;
    public Button btClear;

    [Header("Checkboxes")]
    public Toggle cbActive;
    public Toggle cbDisabled;
    public Toggle cbUnplugged;
    public Toggle cbNotPresent;
    public Toggle cbUseMac;

    [Header("Comboboxes")]
    public TMP_Dropdown cbRole;
    public TMP_Dropdown cbFlow;

    [Header("ListView")]
    public ScrollRect    lvDevicesScroll;
    public RectTransform lvDevicesContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbLogText;

    private wclAudioSwitcher Switcher;

    private readonly List<string>         logLines   = new List<string>();
    private readonly List<DeviceRowData>  deviceRows = new List<DeviceRowData>();
    private DeviceRowData selectedRow;

    private class DeviceRowData
    {
        public string            Id;
        public wclAudioDevice    Device;
        public GameObject        GO;
        public Image             Background;
        public TextMeshProUGUI[] Cells;
    }

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        if (btEnum)       btEnum.onClick.AddListener(OnEnumClick);
        if (btSetDefault) btSetDefault.onClick.AddListener(OnSetDefaultClick);
        if (btRefresh)    btRefresh.onClick.AddListener(OnRefreshClick);
        if (btConnect)    btConnect.onClick.AddListener(OnConnectClick);
        if (btDisconnect) btDisconnect.onClick.AddListener(OnDisconnectClick);
        if (btGetDefault) btGetDefault.onClick.AddListener(OnGetDefaultClick);
        if (btClear)      btClear.onClick.AddListener(OnClearClick);
    }

    private void Start()
    {
        Switcher = new wclAudioSwitcher();
        Switcher.OnOpened              += Switcher_OnOpened;
        Switcher.OnClosed              += Switcher_OnClosed;
        Switcher.OnDefaultDeviceChanged += Switcher_OnDefaultDeviceChanged;
        Switcher.OnDeviceAdded         += Switcher_OnDeviceAdded;
        Switcher.OnDeviceRemoved       += Switcher_OnDeviceRemoved;
        Switcher.OnStateChanged        += Switcher_OnStateChanged;

        int Res = Switcher.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Open failed: 0x" + Res.ToString("X8"));
    }

    private void OnDestroy()
    {
        try { Switcher?.Close(); } catch { }
    }

    // ==========================================================
    //  Library events
    // ==========================================================
    private void Switcher_OnOpened(object sender, EventArgs e)
    {
        AddLog("Audio switcher opened");
        EnumDevices();
    }

    private void Switcher_OnClosed(object sender, EventArgs e)
    {
        ClearDeviceRows();
        AddLog("Audio switcher closed");
    }

    private void Switcher_OnDefaultDeviceChanged(object sender, string id,
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
        AddLog(str);
    }

    private void Switcher_OnDeviceAdded(object sender, string id)
    {
        AddLog("Device added: " + id);
    }

    private void Switcher_OnDeviceRemoved(object sender, string id)
    {
        AddLog("Device removed: " + id);
    }

    private void Switcher_OnStateChanged(object sender, string id, wclAudioDeviceState state)
    {
        AddLog("Device " + id + " state changed: " + StateToStr(state));
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void OnEnumClick()
    {
        EnumDevices();
    }

    private void OnSetDefaultClick()
    {
        if (selectedRow == null)
        {
            AddLog("Select device");
            return;
        }

        wclAudioDeviceRole role;
        switch (cbRole.value)
        {
            case 0:  role = wclAudioDeviceRole.drConsole;        break;
            case 1:  role = wclAudioDeviceRole.drMultimedia;     break;
            default: role = wclAudioDeviceRole.drCommunications; break;
        }

        int Res = Switcher.SetDefault(role, selectedRow.Id);
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Unable to set default device: 0x" + Res.ToString("X8"));
    }

    private void OnRefreshClick()
    {
        if (selectedRow == null)
        {
            AddLog("Select device");
            return;
        }

        int Res = Switcher.GetDeviceDetails(selectedRow.Id, out wclAudioDevice device);
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Refresh device error: 0x" + Res.ToString("X8"));
        else
            UpdateDevice(selectedRow, device);
    }

    private void OnConnectClick()
    {
        if (selectedRow == null)
        {
            AddLog("Select device");
            return;
        }

        int Res;
        if (cbUseMac != null && cbUseMac.isOn)
        {
            string macStr = selectedRow.Cells[6].text;
            if (string.IsNullOrEmpty(macStr))
            {
                AddLog("Selected device is not Bluetooth enabled");
                return;
            }
            Res = Switcher.Connect(Convert.ToInt64(macStr, 16));
        }
        else
        {
            Res = Switcher.Connect(selectedRow.Id);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Connect error: 0x" + Res.ToString("X8"));
    }

    private void OnDisconnectClick()
    {
        if (selectedRow == null)
        {
            AddLog("Select device");
            return;
        }

        int Res;
        if (cbUseMac != null && cbUseMac.isOn)
        {
            string macStr = selectedRow.Cells[6].text;
            if (string.IsNullOrEmpty(macStr))
            {
                AddLog("Selected device is not Bluetooth enabled");
                return;
            }
            Res = Switcher.Disconnect(Convert.ToInt64(macStr, 16));
        }
        else
        {
            Res = Switcher.Disconnect(selectedRow.Id);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Disconnect error: 0x" + Res.ToString("X8"));
    }

    private void OnGetDefaultClick()
    {
        wclAudioDeviceRole role;
        switch (cbRole.value)
        {
            case 0:  role = wclAudioDeviceRole.drConsole;        break;
            case 1:  role = wclAudioDeviceRole.drMultimedia;     break;
            default: role = wclAudioDeviceRole.drCommunications; break;
        }

        wclAudioDeviceDataFlow flow = (cbFlow.value == 0)
            ? wclAudioDeviceDataFlow.dfCapture
            : wclAudioDeviceDataFlow.dfRender;

        int Res = Switcher.GetDefault(role, flow, out string id);
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLog("Unable to get default device: 0x" + Res.ToString("X8"));
        else
            AddLog("Default device ID: " + id);
    }

    private void OnClearClick()
    {
        logLines.Clear();
        RefreshLogDisplay();
    }

    // ==========================================================
    //  Devices enumeration
    // ==========================================================
    private void EnumDevices()
    {
        wclAudioDeviceState states = 0;
        if (cbActive     != null && cbActive.isOn)     states |= wclAudioDeviceState.asActive;
        if (cbDisabled   != null && cbDisabled.isOn)   states |= wclAudioDeviceState.asDisabled;
        if (cbNotPresent != null && cbNotPresent.isOn) states |= wclAudioDeviceState.asNotPresent;
        if (cbUnplugged  != null && cbUnplugged.isOn)  states |= wclAudioDeviceState.asUnplugged;

        if (states == 0)
        {
            AddLog("Select device state.");
            return;
        }

        ClearDeviceRows();

        int Res = Switcher.Enum(states, out wclAudioDevice[] devices);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            AddLog("Enum failed: 0x" + Res.ToString("X8"));
            return;
        }

        if (devices != null && devices.Length > 0)
        {
            foreach (var device in devices)
                AddDeviceRow(device);
        }
    }

    private void ClearDeviceRows()
    {
        foreach (var r in deviceRows)
            if (r.GO != null) Destroy(r.GO);
        deviceRows.Clear();
        selectedRow = null;
    }

    // ==========================================================
    //  Device row (ListView item)
    // ==========================================================
    private void AddDeviceRow(wclAudioDevice device)
    {
        if (lvDevicesContent == null) return;

        var rowGO = new GameObject("Row_" + device.Id,
            typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(lvDevicesContent, false);

        var img = rowGO.GetComponent<Image>();
        img.color = Color.white;

        var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        var le = rowGO.GetComponent<LayoutElement>();
        le.preferredHeight = 18;
        le.minHeight = 18;

        var cells = new TextMeshProUGUI[8];
        for (int i = 0; i < 8; i++)
        {
            var cellGO = new GameObject("c" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(rowGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = 100;
            cellLe.minWidth = 100;
            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.black;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(4, 0, 2, 0);
            tmp.raycastTarget = false;
            cells[i] = tmp;
        }

        var row = new DeviceRowData
        {
            Id         = device.Id,
            Device     = device,
            GO         = rowGO,
            Background = img,
            Cells      = cells
        };

        UpdateDevice(row, device);
        deviceRows.Add(row);

        var btn = rowGO.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = Color.white;
        c.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        c.selectedColor    = Color.white;
        btn.colors = c;
        btn.onClick.AddListener(() => SelectRow(row));
    }

    private void UpdateDevice(DeviceRowData row, wclAudioDevice device)
    {
        row.Device = device;
        row.Cells[0].text = device.Id;
        row.Cells[1].text = device.FriendlyName;
        row.Cells[2].text = device.Description;
        row.Cells[3].text = (device.Flow == wclAudioDeviceDataFlow.dfRender) ? "Render" : "Capture";
        row.Cells[4].text = RolesToStr(device.Roles);
        row.Cells[5].text = StateToStr(device.State);
        row.Cells[6].text = device.IsBluetooth ? device.Mac.ToString("X12") : "";
        row.Cells[7].text = device.IsBluetooth ? device.Service.ToString()   : "";
    }

    private static string RolesToStr(wclAudioDeviceRole roles)
    {
        string s = "[ ";
        if ((roles & wclAudioDeviceRole.drConsole)        != 0) s += "Console ";
        if ((roles & wclAudioDeviceRole.drMultimedia)     != 0) s += "Multimedia ";
        if ((roles & wclAudioDeviceRole.drCommunications) != 0) s += "Communications ";
        s += "]";
        return s;
    }

    private static string StateToStr(wclAudioDeviceState state)
    {
        switch (state)
        {
            case wclAudioDeviceState.asActive:     return "Active";
            case wclAudioDeviceState.asDisabled:   return "Disabled";
            case wclAudioDeviceState.asNotPresent: return "NotPresent";
            case wclAudioDeviceState.asUnplugged:  return "Unplugged";
            default:                               return "";
        }
    }

    private void SelectRow(DeviceRowData row)
    {
        if (selectedRow != null && selectedRow.Background != null)
            selectedRow.Background.color = Color.white;
        selectedRow = row;
        if (selectedRow != null && selectedRow.Background != null)
            selectedRow.Background.color = new Color(0.7f, 0.85f, 1f);
    }

    // ==========================================================
    //  Log
    // ==========================================================
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
}