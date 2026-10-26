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
public class WiFiDirectClientController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btDiscover;
    public Button btTerminate;
    public Button btEnumPaired;
    public Button btIsPaired;
    public Button btUnpair;
    public Button btConnect;
    public Button btDisconnect;
    public Button btClear;

    [Header("Checkboxes")]
    public Toggle cbDisplayPIN;
    public Toggle cbProvidePIN;
    public Toggle cbPushButton;

    [Header("Dropdowns")]
    public TMP_Dropdown cbPairingProcedure;

    [Header("Inputs")]
    public TMP_InputField edGroupOwnerIntent;

    [Header("ListView Content")]
    public RectTransform lvDevicesContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbLogText;

    private ListView lvDevices;
    private ListBox  lbLog;

    private wclWiFiDirectDeviceWatcher Watcher;
    private wclWiFiDirectClient Client;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvDevices = new ListView(lvDevicesContent, new float[] { 150, 200 });
        lbLog     = new ListBox(lbLogText);

        btDiscover.onClick.AddListener(() => Dispatch(btDiscover_Click));
        btTerminate.onClick.AddListener(() => Dispatch(btTerminate_Click));
        btEnumPaired.onClick.AddListener(() => Dispatch(btEnumPaired_Click));
        btIsPaired.onClick.AddListener(() => Dispatch(btIsPaired_Click));
        btUnpair.onClick.AddListener(() => Dispatch(btUnpair_Click));
        btConnect.onClick.AddListener(() => Dispatch(btConnect_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btClear.onClick.AddListener(btClear_Click);
    }

    private void Start() { Dispatch(DoStart); }

    private void DoStart()
    {
        cbPairingProcedure.value = 0;

        Watcher = new wclWiFiDirectDeviceWatcher();
        Watcher.OnDeviceFound += new wclWiFiDirectDeviceFoundEvent(Watcher_OnDeviceFound);
        Watcher.OnDiscoveringCompleted += new wclWiFiDirectDiscoveringCompletedEvent(Watcher_OnDiscoveringCompleted);
        Watcher.OnDiscoveringStarted += new EventHandler(Watcher_OnDiscoveringStarted);

        Client = new wclWiFiDirectClient();
        Client.OnPairCompleted += new wclWiFiDirectPairCompletedEvent(Client_OnPairCompleted);
        Client.OnPairConfirm += new wclWiFiDirectPairConfirmEvent(Client_OnPairConfirm);
        Client.OnPairDisplayPin += new wclWiFiDirectPairDisplayPinEvent(Client_OnPairDisplayPin);
        Client.OnPairGetParams += new wclWiFiDirectPairGetParamsEvent(Client_OnPairGetParams);
        Client.OnPairProvidePin += new wclWiFiDirectPairProvidePinEvent(Client_OnPairProvidePin);
        Client.OnDeviceConnected += new wclWiFiDirectConnectedEvent(Client_OnDeviceConnected);
        Client.OnDeviceDisconnected += new wclWiFiDirectDisconnectedEvent(Client_OnDeviceDisconnected);
    }

    private void Update()
    {
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
        if (Client != null)  { try { Client.Disconnect(); } catch { } }
        if (Watcher != null) { try { Watcher.Terminate(); }  catch { } }
        Client = null;
        Watcher = null;
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

    // InputBox substitute: WinForms used Microsoft.VisualBasic.Interaction.InputBox,
    // a blocking dialog unavailable in Unity. Push Button pairing is on by default
    // (cbPushButton.isOn = true), so this path is rarely taken.
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

    private void btDiscover_Click()
    {
        Int32 Res = Watcher.Discover();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Discover error: 0x" + Res.ToString("X8"));
    }

    private void btTerminate_Click()
    {
        Int32 Res = Watcher.Terminate();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Terminate error: 0x" + Res.ToString("X8"));
    }

    private void btDisconnect_Click()
    {
        Int32 Res = Client.Disconnect();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Disconnect error: 0x" + Res.ToString("X8"));
    }

    private void btConnect_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select device");
        }
        else
        {
            Int32 Res = Client.Connect(lvDevices.SelectedItems[0].SubItems[1].Text);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Connect Error: 0x" + Res.ToString("X8"));
        }
    }

    private void btIsPaired_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select device");
        }
        else
        {
            Boolean IsPaired;
            Int32 Res = Watcher.IsPaired(lvDevices.SelectedItems[0].SubItems[1].Text, out IsPaired);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Get paired error: 0x" + Res.ToString("X8"));
            else
            {
                if (IsPaired)
                    MessageBox.Show("Device is paired");
                else
                    MessageBox.Show("Device is NOT paired");
            }
        }
    }

    private void btUnpair_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select device");
        }
        else
        {
            Int32 Res = Watcher.Unpair(lvDevices.SelectedItems[0].SubItems[1].Text);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Unpair error: 0x" + Res.ToString("X8"));
            else
                MessageBox.Show("Unpaired");
        }
    }

    private void btEnumPaired_Click()
    {
        List<String> Paired = new List<String>();
        Int32 Res = Watcher.EnumPairedDevices(Paired);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Enum paired error: 0x" + Res.ToString("X8"));
        }
        else
        {
            lvDevices.Items.Clear();
            if (Paired.Count > 0)
            {
                foreach (String Id in Paired)
                {
                    ListViewItem Item = lvDevices.Items.Add("");
                    Item.SubItems.Add(Id);
                }
            }
        }
    }

    // ==========================================================
    //  Watcher events
    // ==========================================================
    private void Watcher_OnDiscoveringStarted(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            lvDevices.Items.Clear();
            lbLog.Items.Add("Discovering started");
        });
    }

    private void Watcher_OnDiscoveringCompleted(object Sender, int Error)
    {
        string err = "0x" + Error.ToString("X8");
        Dispatch(() => lbLog.Items.Add("Discovering completed with result: " + err));
    }

    private void Watcher_OnDeviceFound(object Sender, string Id, string Name)
    {
        string id   = Id ?? "";
        string name = Name ?? "";
        Dispatch(() =>
        {
            ListViewItem Item = lvDevices.Items.Add(name);
            Item.SubItems.Add(id);
            lbLog.Items.Add("Device found");
        });
    }

    // ==========================================================
    //  Client events
    // ==========================================================
    private void Client_OnDeviceDisconnected(object Sender, wclWiFiDirectDevice Device, int Reason)
    {
        string reason = "0x" + Reason.ToString("X8");
        Dispatch(() => lbLog.Items.Add("Disconnected. Reason: " + reason));
    }

    private void Client_OnDeviceConnected(object Sender, wclWiFiDirectDevice Device, int Error)
    {
        string err = "0x" + Error.ToString("X8");
        bool success = (Error == wclErrors.WCL_E_SUCCESS);
        string localAddr  = success ? (Client.LocalAddress ?? "") : "";
        string remoteAddr = success ? (Client.RemoteAddress ?? "") : "";

        Dispatch(() =>
        {
            lbLog.Items.Add("Connected with result: " + err);
            if (success)
            {
                lbLog.Items.Add("  Local IP: " + localAddr);
                lbLog.Items.Add("  Remote IP: " + remoteAddr);
            }
        });
    }

    private void Client_OnPairProvidePin(object Sender, wclWiFiDirectDevice Device, out string Pin)
    {
        // WinForms: Pin = Microsoft.VisualBasic.Interaction.InputBox(...);
        // Unity: InputBox is unavailable; fixed PIN returned.
        Pin = InputBox("Enter PIN for ", "Pairing", "");
    }

    private void Client_OnPairGetParams(object Sender, wclWiFiDirectDevice Device,
        out short GroupOwnerIntent, out wclWiFiDirectConfigurationMethod ConfigurationMethods,
        out wclWiFiDirectPairingProcedure PairingProcedure)
    {
        // Synchronous: read UI state and compute out params before returning.
        string goiText = edGroupOwnerIntent.text;
        bool displayPin  = cbDisplayPIN.isOn;
        bool providePin  = cbProvidePIN.isOn;
        bool pushButton  = cbPushButton.isOn;
        int pairingIdx   = cbPairingProcedure.value;

        GroupOwnerIntent = Convert.ToInt16(goiText);

        ConfigurationMethods = 0;
        if (displayPin)  ConfigurationMethods |= wclWiFiDirectConfigurationMethod.cmDisplayPin;
        if (providePin)  ConfigurationMethods |= wclWiFiDirectConfigurationMethod.cmProvidePin;
        if (pushButton)  ConfigurationMethods |= wclWiFiDirectConfigurationMethod.cmPushButton;

        if (pairingIdx == 0)
            PairingProcedure = wclWiFiDirectPairingProcedure.ppInvitation;
        else
            PairingProcedure = wclWiFiDirectPairingProcedure.ppGroupOwnerNegotiation;
    }

    private void Client_OnPairDisplayPin(object Sender, wclWiFiDirectDevice Device, string Pin)
    {
        // Synchronous blocking dialog (same as WinForms).
        MessageBox.Show("Use PIN: " + Pin);
    }

    private void Client_OnPairConfirm(object Sender, wclWiFiDirectDevice Device, out bool Confirm)
    {
        // Synchronous out: blocking MessageBox matches WinForms.
        Confirm = MessageBox.Show("Confirm pairing", "Pairing",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private void Client_OnPairCompleted(object Sender, wclWiFiDirectDevice Device, int Result)
    {
        string result = "0x" + Result.ToString("X8");
        Dispatch(() => lbLog.Items.Add("Pairing completed with result: " + result));
    }
}