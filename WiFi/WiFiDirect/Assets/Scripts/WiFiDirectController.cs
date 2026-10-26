using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclWiFi;

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
//  WinForms-style ListView (plain C#)
// ==========================================================
public class ListView
{
    public RectTransform Content;
    public float[] ColumnWidths;

    internal readonly List<ListViewItem> _items    = new List<ListViewItem>();
    internal readonly List<ListViewItem> _selected = new List<ListViewItem>();

    private static readonly Color SelectedColor = new Color(0.70f, 0.85f, 0.98f, 1f);
    private static readonly Color NormalColor   = Color.white;

    public ListViewItems         Items         { get; private set; }
    public ListViewSelectedItems SelectedItems { get; private set; }

    public ListView(RectTransform content, float[] widths)
    {
        Content = content;
        ColumnWidths = widths;
        Items = new ListViewItems(this);
        SelectedItems = new ListViewSelectedItems(this);
    }

    internal ListViewItem AddItem(string text)
    {
        var item = new ListViewItem(this);
        item.Index = _items.Count;
        item.Values.Add(text ?? string.Empty);
        if (Content != null && ColumnWidths != null && ColumnWidths.Length > 0)
            item.CreateRow(item.Index);
        _items.Add(item);
        return item;
    }

    internal bool RemoveItem(ListViewItem item)
    {
        if (item == null) return false;
        int index = _items.IndexOf(item);
        if (index < 0) return false;
        _items.RemoveAt(index);
        _selected.Remove(item);
        if (item.RowGO != null) UnityEngine.Object.Destroy(item.RowGO);
        for (int i = 0; i < _items.Count; i++) _items[i].Index = i;
        return true;
    }

    internal void ClearItems()
    {
        foreach (var it in _items)
            if (it.RowGO != null) UnityEngine.Object.Destroy(it.RowGO);
        _items.Clear();
        _selected.Clear();
    }

    internal void OnRowClicked(ListViewItem item)
    {
        foreach (var s in _selected)
            if (s.Background != null) s.Background.color = NormalColor;
        _selected.Clear();
        _selected.Add(item);
        if (item.Background != null) item.Background.color = SelectedColor;
    }
}

public class ListViewItem
{
    internal ListView Owner;
    internal GameObject RowGO;
    internal Image Background;
    internal TextMeshProUGUI[] Cells;
    internal readonly List<string> Values = new List<string>();
    public object Tag;
    public int Index;

    internal ListViewItem(ListView owner) { Owner = owner; }

    public string Text
    {
        get => Values.Count > 0 ? Values[0] : string.Empty;
        set
        {
            if (Values.Count == 0) Values.Add(value ?? string.Empty);
            else Values[0] = value ?? string.Empty;
            RefreshCell(0);
        }
    }

    public ListViewSubItems SubItems => new ListViewSubItems(this);

    internal void RefreshCell(int index)
    {
        if (Cells != null && index >= 0 && index < Cells.Length && Cells[index] != null)
            Cells[index].text = index < Values.Count ? Values[index] : string.Empty;
    }

    internal void CreateRow(int index)
    {
        var rowGO = new GameObject("Row" + index, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(Owner.Content, false);

        var img = rowGO.GetComponent<Image>();
        img.color = Color.white;
        Background = img;

        var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.spacing = 0; hlg.padding = new RectOffset(0, 0, 0, 0);

        var le = rowGO.GetComponent<LayoutElement>();
        le.preferredHeight = 18f; le.minHeight = 18f;

        Cells = new TextMeshProUGUI[Owner.ColumnWidths.Length];
        for (int i = 0; i < Owner.ColumnWidths.Length; i++)
        {
            var cellGO = new GameObject("c" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(rowGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = Owner.ColumnWidths[i]; cellLe.minWidth = Owner.ColumnWidths[i];
            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 11; tmp.alignment = TextAlignmentOptions.Left; tmp.color = Color.black;
            tmp.enableWordWrapping = false; tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(4, 0, 2, 0); tmp.raycastTarget = false;
            tmp.text = i < Values.Count ? Values[i] : string.Empty;
            Cells[i] = tmp;
        }

        var btn = rowGO.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;
        var captured = this;
        btn.onClick.AddListener(() => Owner.OnRowClicked(captured));

        RowGO = rowGO;
    }
}

public class ListViewSubItems
{
    private readonly ListViewItem _item;
    internal ListViewSubItems(ListViewItem item) { _item = item; }

    public int Count => _item.Values.Count;
    public ListViewSubItem this[int index] => new ListViewSubItem(_item, index);

    public int Add(string text)
    {
        int idx = _item.Values.Count;
        _item.Values.Add(text ?? string.Empty);
        _item.RefreshCell(idx);
        return idx;
    }
}

public class ListViewSubItem
{
    private readonly ListViewItem _item;
    private readonly int _index;
    internal ListViewSubItem(ListViewItem item, int index) { _item = item; _index = index; }

    public string Text
    {
        get => (_index >= 0 && _index < _item.Values.Count) ? _item.Values[_index] : string.Empty;
        set
        {
            while (_item.Values.Count <= _index) _item.Values.Add(string.Empty);
            _item.Values[_index] = value ?? string.Empty;
            _item.RefreshCell(_index);
        }
    }
}

public class ListViewItems : IEnumerable<ListViewItem>
{
    private readonly ListView _owner;
    internal ListViewItems(ListView owner) { _owner = owner; }

    public int Count => _owner._items.Count;
    public ListViewItem this[int i] => _owner._items[i];
    public ListViewItem Add(string text) => _owner.AddItem(text);
    public void Clear() => _owner.ClearItems();
    public bool Remove(ListViewItem item) => _owner.RemoveItem(item);

    public IEnumerator<ListViewItem> GetEnumerator() => _owner._items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ListViewSelectedItems
{
    private readonly ListView _owner;
    internal ListViewSelectedItems(ListView owner) { _owner = owner; }

    public int Count => _owner._selected.Count;
    public ListViewItem this[int i] => _owner._selected[i];
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
public class WiFiDirectController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btStart;
    public Button btStop;
    public Button btDisconnect;
    public Button btClear;

    [Header("Dropdowns")]
    public TMP_Dropdown cbDiscoverability;
    public TMP_Dropdown cbPairingProcedure;

    [Header("Checkboxes")]
    public Toggle cbAutonomousGroupOwner;
    public Toggle cbDisplayPIN;
    public Toggle cbProvidePIN;
    public Toggle cbPushButton;

    [Header("Inputs")]
    public TMP_InputField edGroupOwnerIntent;

    [Header("ListView Content")]
    public RectTransform lvDevicesContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbLogText;

    private ListView lvDevices;
    private ListBox  lbLog;

    private wclWiFiDirectAdvertiser wclWiFiDirectAdvertiser;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvDevices = new ListView(lvDevicesContent, new float[] { 135, 135, 80 });
        lbLog     = new ListBox(lbLogText);

        btStart.onClick.AddListener(() => Dispatch(btStart_Click));
        btStop.onClick.AddListener(() => Dispatch(btStop_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btClear.onClick.AddListener(btClear_Click);
    }

    private void Start() { Dispatch(DoStart); }

    private void DoStart()
    {
        cbDiscoverability.value = 2;
        cbPairingProcedure.value = 1;

        wclWiFiDirectAdvertiser = new wclWiFiDirectAdvertiser();
        wclWiFiDirectAdvertiser.OnAcceptDevice += new wclWiFiDirectAcceptDeviceEvent(wclWiFiDirectAdvertiser_OnAcceptDevice);
        wclWiFiDirectAdvertiser.OnStarted += new EventHandler(wclWiFiDirectAdvertiser_OnStarted);
        wclWiFiDirectAdvertiser.OnStopped += new EventHandler(wclWiFiDirectAdvertiser_OnStopped);
        wclWiFiDirectAdvertiser.OnPairProvidePin += new wclWiFiDirectPairProvidePinEvent(wclWiFiDirectAdvertiser_OnPairProvidePin);
        wclWiFiDirectAdvertiser.OnPairDisplayPin += new wclWiFiDirectPairDisplayPinEvent(wclWiFiDirectAdvertiser_OnPairDisplayPin);
        wclWiFiDirectAdvertiser.OnPairConfirm += new wclWiFiDirectPairConfirmEvent(wclWiFiDirectAdvertiser_OnPairConfirm);
        wclWiFiDirectAdvertiser.OnPairCompleted += new wclWiFiDirectPairCompletedEvent(wclWiFiDirectAdvertiser_OnPairCompleted);
        wclWiFiDirectAdvertiser.OnDeviceDisconnected += new wclWiFiDirectDisconnectedEvent(wclWiFiDirectAdvertiser_OnDeviceDisconnected);
        wclWiFiDirectAdvertiser.OnPairGetParams += new wclWiFiDirectPairGetParamsEvent(wclWiFiDirectAdvertiser_OnPairGetParams);
        wclWiFiDirectAdvertiser.OnDeviceConnected += new wclWiFiDirectConnectedEvent(wclWiFiDirectAdvertiser_OnDeviceConnected);
    }

    private void Update()
    {
        FitOpenDropdown(cbDiscoverability);
        FitOpenDropdown(cbPairingProcedure);

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
        if (wclWiFiDirectAdvertiser != null) { try { wclWiFiDirectAdvertiser.Stop(); } catch { } }
    }

    // ==========================================================
    //  Dispatcher / helpers
    // ==========================================================
    private void Dispatch(Action a) { lock (_pendingActions) { _pendingActions.Enqueue(a); } }

    private static void FitOpenDropdown(TMP_Dropdown dd)
    {
        if (dd == null || !dd.IsExpanded) return;
        RectTransform list = null;
        for (int i = 0; i < dd.transform.childCount; i++)
        {
            var c = dd.transform.GetChild(i);
            if (c.name == "Dropdown List") { list = c as RectTransform; break; }
        }
        if (list == null) return;
        var content = list.Find("Viewport/Content") as RectTransform;
        if (content == null) return;
        float h = content.rect.height + 10f;
        if (Mathf.Abs(list.sizeDelta.y - h) > 0.5f)
            list.sizeDelta = new Vector2(list.sizeDelta.x, h);
    }

    private ListViewItem GetItem(String Id)
    {
        foreach (ListViewItem Item in lvDevices.Items)
            if (Item.SubItems[1].Text == Id)
                return Item;
        return null;
    }

    // InputBox substitute: WinForms used Microsoft.VisualBasic.Interaction.InputBox,
    // which is a blocking dialog and is not available in Unity. Returning a fixed
    // PIN is acceptable for this demo since Push Button pairing is enabled by default.
    private string InputBox(string prompt, string title, string defaultValue)
    {
        return "00000000";
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClear_Click()
    {
        lbLog.Items.Clear();
    }

    private void btDisconnect_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select device");
        }
        else
        {
            for (int i = 0; i < wclWiFiDirectAdvertiser.Count; i++)
            {
                if (wclWiFiDirectAdvertiser[i].Id == lvDevices.SelectedItems[0].SubItems[1].Text)
                {
                    Int32 Res = wclWiFiDirectAdvertiser[i].Disconnect();
                    if (Res != wclErrors.WCL_E_SUCCESS)
                        MessageBox.Show("Disconnect error: 0x" + Res.ToString("X8"));
                    break;
                }
            }
        }
    }

    private void btStop_Click()
    {
        Int32 Res = wclWiFiDirectAdvertiser.Stop();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Stop error: 0x" + Res.ToString("X8"));
    }

    private void btStart_Click()
    {
        if (wclWiFiDirectAdvertiser.Active)
        {
            MessageBox.Show("Already running");
        }
        else
        {
            switch (cbDiscoverability.value)
            {
                case 0:
                    wclWiFiDirectAdvertiser.Discoverability = wclWiFiDirectAdvertisementDiscoverability.adIntensive;
                    break;
                case 1:
                    wclWiFiDirectAdvertiser.Discoverability = wclWiFiDirectAdvertisementDiscoverability.adNone;
                    break;
                case 2:
                    wclWiFiDirectAdvertiser.Discoverability = wclWiFiDirectAdvertisementDiscoverability.adNormal;
                    break;
            }
            wclWiFiDirectAdvertiser.AutonomousGroupOwnerEnabled = cbAutonomousGroupOwner.isOn;

            Int32 Res = wclWiFiDirectAdvertiser.Start();
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Start error: 0x" + Res.ToString("X8"));
        }
    }

    // ==========================================================
    //  Events
    // ==========================================================
    private void wclWiFiDirectAdvertiser_OnPairProvidePin(object Sender, wclWiFiDirectDevice Device, out string Pin)
    {
        // WinForms: Pin = Microsoft.VisualBasic.Interaction.InputBox(...);
        // Unity: InputBox is unavailable; fixed PIN returned.
        Pin = InputBox("Enter PIN for ", "Pairing", "");
        string pinCopy = Pin;
        string deviceName = Device.Name ?? "";
        Dispatch(() => lbLog.Items.Add(deviceName + " PairProvidePin: " + pinCopy));
    }

    private void wclWiFiDirectAdvertiser_OnPairGetParams(object Sender, wclWiFiDirectDevice Device,
        out short GroupOwnerIntent, out wclWiFiDirectConfigurationMethod ConfigurationMethods,
        out wclWiFiDirectPairingProcedure PairingProcedure)
    {
        // Synchronous: read UI fields before calculating out params.
        int pairingIdx = cbPairingProcedure.value;
        string goiText = edGroupOwnerIntent.text;
        bool displayPin = cbDisplayPIN.isOn;
        bool providePin = cbProvidePIN.isOn;
        bool pushButton = cbPushButton.isOn;
        string deviceName = Device.Name ?? "";

        Dispatch(() => lbLog.Items.Add(deviceName + " quering pairing params"));

        switch (pairingIdx)
        {
            case 0:  PairingProcedure = wclWiFiDirectPairingProcedure.ppInvitation; break;
            case 1:  PairingProcedure = wclWiFiDirectPairingProcedure.ppGroupOwnerNegotiation; break;
            default: PairingProcedure = wclWiFiDirectPairingProcedure.ppGroupOwnerNegotiation; break;
        }
        GroupOwnerIntent = Convert.ToInt16(goiText);
        ConfigurationMethods = 0;
        if (displayPin) ConfigurationMethods |= wclWiFiDirectConfigurationMethod.cmDisplayPin;
        if (providePin) ConfigurationMethods |= wclWiFiDirectConfigurationMethod.cmProvidePin;
        if (pushButton) ConfigurationMethods |= wclWiFiDirectConfigurationMethod.cmPushButton;
    }

    private void wclWiFiDirectAdvertiser_OnPairDisplayPin(object Sender, wclWiFiDirectDevice Device, string Pin)
    {
        // Synchronous MessageBox (blocking), matching the original WinForms behaviour.
        MessageBox.Show("Use " + Pin + " PIN to pair with " + Device.Name);
    }

    private void wclWiFiDirectAdvertiser_OnPairConfirm(object Sender, wclWiFiDirectDevice Device, out bool Confirm)
    {
        // Synchronous out; MessageBox is blocking.
        Confirm = MessageBox.Show("Confirm " + Device.Name + " connection", "Pairing",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private void wclWiFiDirectAdvertiser_OnPairCompleted(object Sender, wclWiFiDirectDevice Device, int Result)
    {
        string name = Device.Name ?? "";
        string result = "0x" + Result.ToString("X8");
        Dispatch(() => lbLog.Items.Add(name + " paired. Result: " + result));
    }

    private void wclWiFiDirectAdvertiser_OnDeviceConnected(object Sender, wclWiFiDirectDevice Device, int Error)
    {
        string id = Device.Id ?? "";
        string name = Device.Name ?? "";
        string localAddr = Device.LocalAddress ?? "";
        string remoteAddr = Device.RemoteAddress ?? "";
        bool success = (Error == wclErrors.WCL_E_SUCCESS);
        string errStr = "0x" + Error.ToString("X8");

        Dispatch(() =>
        {
            ListViewItem Item = GetItem(id);
            if (Item != null)
            {
                if (success)
                {
                    Item.SubItems[2].Text = "Connected";
                    lbLog.Items.Add(name + " connected");
                    lbLog.Items.Add("  Local IP: " + localAddr);
                    lbLog.Items.Add("  Remote IP: " + remoteAddr);
                }
                else
                {
                    lbLog.Items.Add(name + " connect error: " + errStr);
                    lvDevices.Items.Remove(Item);
                }
            }
        });
    }

    private void wclWiFiDirectAdvertiser_OnDeviceDisconnected(object Sender, wclWiFiDirectDevice Device, int Reason)
    {
        string id = Device.Id ?? "";
        string name = Device.Name ?? "";
        string reason = "0x" + Reason.ToString("X8");

        Dispatch(() =>
        {
            ListViewItem Item = GetItem(id);
            if (Item != null)
            {
                lbLog.Items.Add(name + " disconnected. Reason: " + reason);
                lvDevices.Items.Remove(Item);
            }
        });
    }

    private void wclWiFiDirectAdvertiser_OnAcceptDevice(object Sender, wclWiFiDirectDevice Device, out bool Accept)
    {
        string id = Device.Id ?? "";
        string name = Device.Name ?? "";

        Dispatch(() => lbLog.Items.Add("Accepting device " + id));

        Accept = MessageBox.Show("Accept connection from " + name + "?", "Connection",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        if (Accept)
        {
            Dispatch(() =>
            {
                ListViewItem Item = lvDevices.Items.Add(name);
                Item.SubItems.Add(id);
                Item.SubItems.Add("Connecting");
            });
        }
    }

    private void wclWiFiDirectAdvertiser_OnStopped(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            lvDevices.Items.Clear();
            lbLog.Items.Add("Advertiser has been stopped");
        });
    }

    private void wclWiFiDirectAdvertiser_OnStarted(object sender, EventArgs e)
    {
        Dispatch(() => lbLog.Items.Add("Advertiser has been started"));
    }
}