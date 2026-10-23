using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclBluetooth;

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

    public static DialogResult Show(string text) =>
        Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);

    public static DialogResult Show(string text, string caption) =>
        Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);

    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) =>
        Show(text, caption, buttons, MessageBoxIcon.None);

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
//  WinForms-style ListView (plain C#, no MonoBehaviour)
// ==========================================================
public class ListView
{
    public RectTransform Content;
    public float[]       ColumnWidths;

    internal readonly List<ListViewItem> _items    = new List<ListViewItem>();
    internal readonly List<ListViewItem> _selected = new List<ListViewItem>();

    private static readonly Color SelectedColor = new Color(0.70f, 0.85f, 0.98f, 1f);
    private static readonly Color NormalColor   = Color.white;

    public ListViewItems         Items         { get; private set; }
    public ListViewSelectedItems SelectedItems { get; private set; }
    public event Action          SelectedIndexChanged;

    public ListView(RectTransform content, float[] widths)
    {
        Content = content;
        ColumnWidths = widths;
        Items = new ListViewItems(this);
        SelectedItems = new ListViewSelectedItems(this);
    }

    internal ListViewItem AddItem(string text)
    {
        return InsertItem(_items.Count, text);
    }

    internal ListViewItem InsertItem(int index, string text)
    {
        if (index < 0) index = 0;
        if (index > _items.Count) index = _items.Count;

        var item = new ListViewItem(this);
        item.Values.Add(text ?? string.Empty);

        if (Content != null && ColumnWidths != null && ColumnWidths.Length > 0)
            item.CreateRow(index);

        _items.Insert(index, item);

        // Header is child 0; rows occupy indices 1..N in sibling order.
        if (item.RowGO != null)
            item.RowGO.transform.SetSiblingIndex(index + 1);

        Reindex();
        return item;
    }

    internal bool RemoveItem(ListViewItem item)
    {
        if (item == null) return false;
        int index = _items.IndexOf(item);
        if (index < 0) return false;

        _items.RemoveAt(index);
        if (_selected.Contains(item)) _selected.Remove(item);
        if (item.RowGO != null) UnityEngine.Object.Destroy(item.RowGO);
        Reindex();

        if (_selected.Count == 0) SelectedIndexChanged?.Invoke();
        return true;
    }

    internal void ClearItems()
    {
        foreach (var it in _items)
            if (it.RowGO != null) UnityEngine.Object.Destroy(it.RowGO);
        _items.Clear();
        _selected.Clear();
        SelectedIndexChanged?.Invoke();
    }

    internal void OnRowClicked(ListViewItem item)
    {
        foreach (var s in _selected)
            if (s.Background != null) s.Background.color = NormalColor;
        _selected.Clear();
        _selected.Add(item);
        if (item.Background != null) item.Background.color = SelectedColor;

        SelectedIndexChanged?.Invoke();
    }

    private void Reindex()
    {
        for (int i = 0; i < _items.Count; i++) _items[i].Index = i;
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
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        var le = rowGO.GetComponent<LayoutElement>();
        le.preferredHeight = 18f;
        le.minHeight = 18f;

        Cells = new TextMeshProUGUI[Owner.ColumnWidths.Length];
        for (int i = 0; i < Owner.ColumnWidths.Length; i++)
        {
            var cellGO = new GameObject("c" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(rowGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = Owner.ColumnWidths[i];
            cellLe.minWidth = Owner.ColumnWidths[i];
            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.black;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(4, 0, 2, 0);
            tmp.raycastTarget = false;
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
    public ListViewItem Insert(int index, string text) => _owner.InsertItem(index, text);
    public bool Remove(ListViewItem item) => _owner.RemoveItem(item);
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
//  WinForms-style ListBox (with TopIndex -> ScrollToBottom)
// ==========================================================
public class ListBox
{
    private readonly List<string> _lines = new List<string>();
    private readonly TextMeshProUGUI _text;
    private readonly ScrollRect _scroll;

    public ListBox(TextMeshProUGUI text, ScrollRect scroll)
    {
        _text = text;
        _scroll = scroll;
        Items = new ListBoxItems(this);
    }

    public ListBoxItems Items { get; private set; }

    internal void Add(string s) { _lines.Add(s ?? ""); Refresh(true); }
    internal void Clear() { _lines.Clear(); Refresh(false); }
    internal int Count => _lines.Count;
    internal string Get(int i) => _lines[i];

    internal void ScrollToBottom()
    {
        if (_scroll == null) return;
        Canvas.ForceUpdateCanvases();
        _scroll.verticalNormalizedPosition = 0f;
    }

    private void Refresh(bool scrollToBottom)
    {
        if (_text != null) _text.text = string.Join("\n", _lines);
        if (scrollToBottom) ScrollToBottom();
    }
}

public class ListBoxItems
{
    private readonly ListBox _owner;
    internal ListBoxItems(ListBox owner) { _owner = owner; }

    public int Count => _owner.Count;
    public void Add(string s) => _owner.Add(s);
    public void Clear() => _owner.Clear();
    public string this[int i] => _owner.Get(i);

    public int TopIndex
    {
        get => 0;
        set => _owner.ScrollToBottom();
    }
}

// ==========================================================
//  Controller (fmMain)
// ==========================================================
public class GattServerController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btStart;
    public Button btStop;
    public Button btDisconnect;
    public Button btNotify;
    public Button btNotifyAll;
    public Button btSetParameters;
    public Button btClear;

    [Header("Inputs")]
    public TMP_InputField edData;
    public TMP_InputField edRepeats;

    [Header("Dropdowns")]
    public TMP_Dropdown cbParams;

    [Header("ListView")]
    public RectTransform lvDevicesContent;

    [Header("ListBox")]
    public ScrollRect lbLogScroll;
    public TextMeshProUGUI lbLogText;

    private ListView lvDevices;
    private ListBox lbLog;

    private wclBluetoothManager FManager;
    private wclGattServer FServer;

    private const UInt16 SHORT_DATA_LEN = 5;
    private const UInt16 LONG_DATA_LEN = 1024;
    private Byte[] ShortData = new Byte[SHORT_DATA_LEN];
    private Byte[] LongData = new Byte[LONG_DATA_LEN];

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvDevices = new ListView(lvDevicesContent, new float[] {
            100, 140, 60, 50, 50, 50, 60, 50, 50, 60, 50, 50, 90
        });
        lbLog = new ListBox(lbLogText, lbLogScroll);

        lvDevices.SelectedIndexChanged += lvDevices_SelectedIndexChanged;

        btStart.onClick.AddListener(() => Dispatch(btStart_Click));
        btStop.onClick.AddListener(() => Dispatch(btStop_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btNotify.onClick.AddListener(() => Dispatch(btNotify_Click));
        btNotifyAll.onClick.AddListener(() => Dispatch(btNotifyAll_Click));
        btSetParameters.onClick.AddListener(() => Dispatch(btSetParameters_Click));
        btClear.onClick.AddListener(btClear_Click);

        // KeyPress filters: hex/digits only (backspace not routed through onValidateInput).
        edData.onValidateInput += (text, charIndex, addedChar) =>
        {
            if ((addedChar >= '0' && addedChar <= '9') ||
                (addedChar >= 'a' && addedChar <= 'f') ||
                (addedChar >= 'A' && addedChar <= 'F'))
                return addedChar;
            return '\0';
        };
        edRepeats.onValidateInput += (text, charIndex, addedChar) =>
        {
            if (addedChar >= '0' && addedChar <= '9') return addedChar;
            return '\0';
        };

        // Initial enabled state matches WinForms Load
        btStop.interactable = false;
        btDisconnect.interactable = false;
        btNotify.interactable = false;
        btNotifyAll.interactable = false;
        btSetParameters.interactable = false;
    }

    private void Start()
    {
        Dispatch(DoStart);
    }

    private void DoStart()
    {
        cbParams.value = 0;

        FManager = new wclBluetoothManager();
        FManager.AfterOpen += new EventHandler(BluetoothManagerAfterOpen);
        FManager.BeforeClose += new EventHandler(BluetoothManagerBeforeClose);
        FManager.OnClosed += new EventHandler(BluetoothManagerClosed);

        FServer = new wclGattServer();
        FServer.OnStarted += new EventHandler(GattServeStarted);
        FServer.OnStopped += new EventHandler(GattServerStopped);
        FServer.OnClientConnected += new wclGattServerClientEvent(GettServerClientConnected);
        FServer.OnMaxPduSizeChanged += new wclGattServerClientEvent(GattServerMaxPduSizeChanged);
        FServer.OnConnectionParamsChanged += new wclGattServerClientEvent(GattServerConnectionParamsChanged);
        FServer.OnConnectionPhyChanged += new wclGattServerClientEvent(GattServerConnectionPhyChanged);
        FServer.OnNotificationSizeChanged += new wclGattServerClientEvent(GattServerNotificationSizeChanged);
        FServer.OnClientDisconnected += new wclGattServerClientEvent(GattServerClientDisconnected);
        FServer.OnWrite += new wclGattLocalCharacteristicWriteEvent(GattServerWrite);
        FServer.OnSubscribed += new wclGattLocalCharacteristicSubscribedClientsChangedEvent(GattServerSubscribed);
        FServer.OnUnsubscribed += new wclGattLocalCharacteristicSubscribedClientsChangedEvent(GattServerUnsubscribed);
        FServer.OnRead += new wclGattLocalCharacteristicReadEvent(GattServerRead);

        for (UInt32 i = 0; i < SHORT_DATA_LEN; i++)
            ShortData[i] = (Byte)(i + 1);
        for (UInt32 i = 0; i < LONG_DATA_LEN; i++)
            LongData[i] = (Byte)i;
    }

    private void Update()
    {
        FitOpenDropdown(cbParams);

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
        try { Stop(); } catch { }
    }

    // ==========================================================
    //  Dispatcher / helpers
    // ==========================================================
    private void Dispatch(Action a)
    {
        lock (_pendingActions) { _pendingActions.Enqueue(a); }
    }

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

    private void Trace(String Msg)
    {
        Dispatch(() => lbLog.Items.Add(Msg));
    }

    private void Trace(Byte Spaces, String Msg)
    {
        String Str = "";
        for (int i = 0; i < Spaces; i++)
            Str += "";
        Str = Str + Msg;
        Trace(Str);
    }

    private void Trace(String Msg, Int32 Error)
    {
        Trace(Msg + " failed: 0x" + Error.ToString("X8"));
    }

    private void Trace(Byte Spaces, String Msg, Int32 Error)
    {
        Trace(Spaces, Msg + " failed: 0x" + Error.ToString("X8"));
    }

    // ==========================================================
    //  GATT server events
    // ==========================================================
    private void GattServerRead(object Sender, wclGattServerClient Client, wclGattLocalCharacteristic Characteristic,
        wclGattLocalCharacteristicReadRequest Request)
    {
        Trace("Read request received from: " + Client.Address.ToString("X12"));
        Trace(4, "Characteristic: " + Characteristic.Uuid.ShortUuid.ToString("X4"));
        Trace(4, "Offset: " + Request.Offset.ToString());
        Trace(4, "Buffer size: " + Request.Size.ToString());

        Int32 Res;
        if (Characteristic.Uuid.ShortUuid == 0xFFF2)
            Res = Request.Respond(ShortData);
        else
        {
            Byte[] ToSend = new Byte[LONG_DATA_LEN - Request.Offset];
            Array.Copy(LongData, Request.Offset, ToSend, 0, LONG_DATA_LEN - Request.Offset);
            Res = Request.Respond(ToSend);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(8, "Set data", Res);
            Trace(8, "Respond with error");
            Res = Request.RespondWithError(wclBluetoothErrors.WCL_E_BLUETOOTH_LE_UNLIKELY);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Trace(12, "Respond", Res);
        }
        else
            Trace(8, "Data sent");
    }

    private void GattServerUnsubscribed(object Sender, wclGattServerClient Client, wclGattLocalCharacteristic Characteristic)
    {
        Dispatch(() =>
        {
            Trace("Client " + Client.Address.ToString("X12") + " unsubscribed from");
            Trace(4, "Characteristic: " + Characteristic.Uuid.ShortUuid.ToString("X4"));

            ListViewItem Item = FindItem(Client);
            if (Item != null)
            {
                if (Item.Index < lvDevices.Items.Count - 1)
                {
                    for (Int32 i = Item.Index + 1; i < lvDevices.Items.Count; i++)
                    {
                        Item = lvDevices.Items[i];
                        if (Item.Text != "")
                            break;

                        if (Item.Tag == Characteristic)
                        {
                            lvDevices.Items.Remove(Item);

                            UpdateNotificationSize(Client);
                            UpdateNotifyAllStatus();

                            break;
                        }
                    }
                }
            }
        });
    }

    private void GattServerSubscribed(object Sender, wclGattServerClient Client, wclGattLocalCharacteristic Characteristic)
    {
        Dispatch(() =>
        {
            Trace("Client " + Client.Address.ToString("X12") + " subscribed to");
            Trace(4, "Characteristic: " + Characteristic.Uuid.ShortUuid.ToString("X4"));

            ListViewItem Item = FindItem(Client);
            if (Item != null)
            {
                Item = lvDevices.Items.Insert(Item.Index + 1, "");
                Item.Tag = Characteristic;
                Item.SubItems.Add(Characteristic.Uuid.ShortUuid.ToString("X4"));
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");
                Item.SubItems.Add("");

                UpdateNotificationSize(Client);
                UpdateNotifyAllStatus();
            }
        });
    }

    private void GattServerWrite(object Sender, wclGattServerClient Client, wclGattLocalCharacteristic Characteristic,
        wclGattLocalCharacteristicWriteRequest Request)
    {
        Dispatch(() =>
        {
            Trace("Write request received from: " + Client.Address.ToString("X12"));
            Trace(4, "Characteristic: " + Characteristic.Uuid.ShortUuid.ToString("X4"));
            if (Request.Data == null)
                Trace(4, "Size: 0");
            else
                Trace(4, "Size: " + Request.Data.Length.ToString());
            Trace(4, "Offset: " + Request.Offset.ToString());

            String s = "";
            if (Request.Data != null && Request.Data.Length > 0)
            {
                for (int i = 0; i < Request.Data.Length; i++)
                    s = s + Request.Data[i].ToString("X2");
            }
            Trace(8, s);

            if (Request.WithResponse)
            {
                Trace(4, "Sending response");
                Int32 Res = Request.Respond();
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Trace(8, "Send response", Res);
                else
                    Trace(8, "Response sent");
            }
        });
    }

    private void GattServerClientDisconnected(object Sender, wclGattServerClient Client)
    {
        Dispatch(() =>
        {
            Trace("Client disconnected: " + Client.Address.ToString("X12"));

            ListViewItem Item = FindItem(Client);
            if (Item != null)
            {
                lvDevices.Items.Remove(Item);
                UpdateNotifyAllStatus();
            }
        });
    }

    private void GattServerNotificationSizeChanged(object Sender, wclGattServerClient Client)
    {
        Dispatch(() =>
        {
            UpdateNotificationSize(Client);
            Trace("Notification size changed: " + Client.Address.ToString("X12"));
        });
    }

    private void GattServerConnectionPhyChanged(object Sender, wclGattServerClient Client)
    {
        Dispatch(() =>
        {
            UpdatePhy(Client);
            Trace("PHY changed: " + Client.Address.ToString("X12"));
        });
    }

    private void GattServerConnectionParamsChanged(object Sender, wclGattServerClient Client)
    {
        Dispatch(() =>
        {
            UpdateConnectionParams(Client);
            Trace("Connection parameters changed: " + Client.Address.ToString("X12"));
        });
    }

    private void GattServerMaxPduSizeChanged(object Sender, wclGattServerClient Client)
    {
        Dispatch(() =>
        {
            UpdateMaxPduSize(Client);
            Trace("Max PDU size changed: " + Client.Address.ToString("X12"));
        });
    }

    private void GettServerClientConnected(object Sender, wclGattServerClient Client)
    {
        Dispatch(() =>
        {
            Trace("Client connected: " + Client.Address.ToString("X12"));

            ListViewItem Item = lvDevices.Items.Add(Client.Address.ToString("X12"));
            Item.Tag = Client;
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");
            Item.SubItems.Add("");

            UpdateMaxPduSize(Client);
            UpdateConnectionParams(Client);
            UpdatePhy(Client);
            UpdateNotificationSize(Client);
        });
    }

    private void GattServerStopped(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            btStop.interactable = false;
            btStart.interactable = true;
            Trace("Server stopped");
        });
    }

    private void GattServeStarted(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            btStop.interactable = true;
            btStart.interactable = false;
            Trace("Server started");
        });
    }

    private void BluetoothManagerClosed(object sender, EventArgs e)
    {
        Trace("Bluetooth Manager closed");
    }

    private void BluetoothManagerBeforeClose(object sender, EventArgs e)
    {
        Trace("Bluetooth Manager is closing");
    }

    private void BluetoothManagerAfterOpen(object sender, EventArgs e)
    {
        Trace("Bluetooth Manager opened");
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClear_Click()
    {
        lbLog.Items.Clear();
    }

    private Boolean InitializeBluetooth(out wclBluetoothRadio Radio)
    {
        Radio = null;

        Trace("Opening Bluetooth Manager");
        Int32 Res = FManager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Open Bluetooth Manager", Res);
            return false;
        }

        Trace("Get working LE Radio");
        Res = FManager.GetLeRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get working LE Radio", Res);
            FManager.Close();
            return false;
        }

        Trace("Working radio found. Use " + Radio.ApiName + " radio");
        return true;
    }

    private void UninitializeBluetooth()
    {
        if (FManager.Active)
        {
            Trace("Closing Bluetooth Manager");
            FManager.Close();
        }
    }

    private Boolean InitializeServer(wclBluetoothRadio Radio)
    {
        Trace("Initializing GATT server");
        Int32 Res = FServer.Initialize(Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("GATT server initialization", Res);
            return false;
        }

        Trace("GATT server initialized");
        return true;
    }

    private void UninitializeServer()
    {
        if (FServer.Initialized)
        {
            Trace("Uninitializing GATT server");
            Int32 Res = FServer.Uninitialize();
            if (Res != wclErrors.WCL_E_SUCCESS)
                Trace("GATT server uninitialization", Res);
        }
    }

    private Boolean AddCharacteristics(wclGattLocalService Service)
    {
        Trace(4, "Adding GATT characteristics");

        wclGattLocalCharacteristic Char;

        wclGattUuid Uuid = new wclGattUuid();
        Uuid.IsShortUuid = true;

        wclGattLocalCharacteristicParameters Params = new wclGattLocalCharacteristicParameters();
        Params.Descriptors = null;
        Params.PresentationFormats = null;

        Trace(8, "Add empty characteristic");
        Uuid.ShortUuid = 0xFFF1;
        Params.Props = 0;
        Params.UserDescription = "Empty characteristic";
        Int32 Res = Service.AddCharacteristic(Uuid, Params, out Char);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(12, "Add empty characteristic", Res);
            return false;
        }
        Trace(12, "Empty characteristic added");

        Trace(8, "Add short readable characteristic");
        Uuid.ShortUuid = 0xFFF2;
        Params.Props = wclGattLocalCharacteristicProperty.cpReadable;
        Params.UserDescription = "Short readable characteristic";
        Res = Service.AddCharacteristic(Uuid, Params, out Char);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(12, "Add short readable characteristic", Res);
            return false;
        }
        Trace(12, "Short readable characteristic added");

        Trace(8, "Add long readable characteristic");
        Uuid.ShortUuid = 0xFFF3;
        Params.Props = wclGattLocalCharacteristicProperty.cpReadable;
        Params.UserDescription = "Long readable characteristic";
        Res = Service.AddCharacteristic(Uuid, Params, out Char);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(12, "Add long readable characteristic: 0x", Res);
            return false;
        }
        Trace(12, "Long readable characteristic added");

        Trace(8, "Add writable characteristic");
        Uuid.ShortUuid = 0xFFF4;
        Params.Props = wclGattLocalCharacteristicProperty.cpWritable |
            wclGattLocalCharacteristicProperty.cpWritableWithoutResponse;
        Params.UserDescription = "Writable characteristic";
        Res = Service.AddCharacteristic(Uuid, Params, out Char);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(12, "Add writable characteristic", Res);
            return false;
        }
        Trace(12, "Writable characteristic added");

        Trace(8, "Add notifiable characteristic");
        Uuid.ShortUuid = 0xFFF5;
        Params.Props = wclGattLocalCharacteristicProperty.cpNotifiable;
        Params.UserDescription = "Notifiable characteristic";
        Res = Service.AddCharacteristic(Uuid, Params, out Char);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(12, "Add notifiable characteristic", Res);
            return false;
        }
        Trace(12, "Notifiable characteristic added");

        Trace(8, "Add indicatable characteristic");
        Uuid.ShortUuid = 0xFFF6;
        Params.Props = wclGattLocalCharacteristicProperty.cpIndicatable;
        Params.UserDescription = "Indicatable characteristic";
        Res = Service.AddCharacteristic(Uuid, Params, out Char);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            Trace(12, "Add indicatable characteristic", Res);
            return false;
        }
        Trace(12, "Indicatable characteristic added");

        Trace(4, "All GATT charactertistics added");
        return true;
    }

    private wclGattLocalService AddService()
    {
        Trace("Adding GATT service");

        wclGattUuid Uuid = new wclGattUuid();
        Uuid.IsShortUuid = true;
        Uuid.ShortUuid = 0xFFF0;
        wclGattLocalService Result;
        Int32 Res = FServer.AddService(Uuid, out Result);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace(4, "Add GATT service", Res);
        else
            Trace("GATT service added");
        return Result;
    }

    private void StartServer()
    {
        wclBluetoothRadio Radio = null;
        if (InitializeBluetooth(out Radio))
        {
            Boolean BoolRes = InitializeServer(Radio);
            if (BoolRes)
            {
                wclGattLocalService Service = AddService();
                if (Service == null)
                    BoolRes = false;
                else
                {
                    BoolRes = AddCharacteristics(Service);
                    if (BoolRes)
                    {
                        Trace("Starting GATT server");
                        Int32 Res = FServer.Start();
                        if (Res != wclErrors.WCL_E_SUCCESS)
                        {
                            Trace("GATT server start", Res);
                            BoolRes = false;
                        }
                    }
                }
                if (!BoolRes)
                    UninitializeServer();
            }
            if (!BoolRes)
                UninitializeBluetooth();
        }
    }

    private void Stop()
    {
        if (FServer != null)
        {
            Trace("Stopping GATT server");
            Int32 Res = FServer.Stop();
            if (Res != wclErrors.WCL_E_SUCCESS)
                Trace("Stop GATT server", Res);

            UninitializeServer();
        }

        if (FManager != null)
            UninitializeBluetooth();
    }

    private void btStart_Click()
    {
        StartServer();
    }

    private void btStop_Click()
    {
        Stop();
    }

    private ListViewItem FindItem(wclGattServerClient Client)
    {
        if (Client != null)
        {
            if (lvDevices.Items.Count > 0)
            {
                foreach (ListViewItem Item in lvDevices.Items)
                {
                    if (Item.Tag == Client)
                        return Item;
                }
            }
        }
        return null;
    }

    private void UpdateMaxPduSize(wclGattServerClient Client)
    {
        ListViewItem Item = FindItem(Client);
        if (Item != null)
        {
            UInt16 Size;
            if (Client.GetMaxPduSize(out Size) == wclErrors.WCL_E_SUCCESS)
                Item.SubItems[2].Text = Size.ToString();
            else
                Item.SubItems[2].Text = "-----";
        }
    }

    private void UpdateConnectionParams(wclGattServerClient Client)
    {
        ListViewItem Item = FindItem(Client);
        if (Item != null)
        {
            wclBluetoothLeConnectionParameters Params;
            if (Client.GetConnectionParams(out Params) == wclErrors.WCL_E_SUCCESS)
            {
                Item.SubItems[3].Text = Params.Interval.ToString();
                Item.SubItems[4].Text = Params.Latency.ToString();
                Item.SubItems[5].Text = Params.LinkTimeout.ToString();
            }
            else
            {
                Item.SubItems[3].Text = "-----";
                Item.SubItems[4].Text = "-----";
                Item.SubItems[5].Text = "-----";
            }
        }
    }

    private void UpdatePhy(wclGattServerClient Client)
    {
        ListViewItem Item = FindItem(Client);
        if (Item != null)
        {
            wclBluetoothLeConnectionPhy Phy;
            if (Client.GetConnectionPhyInfo(out Phy) == wclErrors.WCL_E_SUCCESS)
            {
                Item.SubItems[6].Text = Phy.Receive.IsCoded.ToString();
                Item.SubItems[7].Text = Phy.Receive.IsUncoded1MPhy.ToString();
                Item.SubItems[8].Text = Phy.Receive.IsUncoded2MPhy.ToString();

                Item.SubItems[9].Text  = Phy.Transmit.IsCoded.ToString();
                Item.SubItems[10].Text = Phy.Transmit.IsUncoded1MPhy.ToString();
                Item.SubItems[11].Text = Phy.Transmit.IsUncoded2MPhy.ToString();
            }
            else
            {
                Item.SubItems[6].Text  = "-----";
                Item.SubItems[7].Text  = "-----";
                Item.SubItems[8].Text  = "-----";
                Item.SubItems[9].Text  = "-----";
                Item.SubItems[10].Text = "-----";
                Item.SubItems[11].Text = "-----";
            }
        }
    }

    private void UpdateNotificationSize(wclGattServerClient Client)
    {
        ListViewItem Item = FindItem(Client);
        if (Item != null)
        {
            UInt16 Size;
            if (Client.GetMaxNotificationSize(out Size) == wclErrors.WCL_E_SUCCESS)
                Item.SubItems[12].Text = Size.ToString();
            else
                Item.SubItems[12].Text = "-----";
        }
    }

    private void lvDevices_SelectedIndexChanged()
    {
        if (lvDevices.SelectedItems.Count > 0)
        {
            btDisconnect.interactable    = (lvDevices.SelectedItems[0].Text != "");
            btNotify.interactable        = (lvDevices.SelectedItems[0].Text == "");
            btSetParameters.interactable = (lvDevices.SelectedItems[0].Text != "");
        }
        else
        {
            btDisconnect.interactable    = false;
            btNotify.interactable        = false;
            btSetParameters.interactable = false;
        }

        UpdateNotifyAllStatus();
    }

    private void UpdateNotifyAllStatus()
    {
        Boolean Found = false;

        if (lvDevices.Items.Count > 0)
        {
            foreach (ListViewItem Item in lvDevices.Items)
            {
                if (Item.Text == "")
                {
                    Found = true;
                    break;
                }
            }
        }

        btNotifyAll.interactable = (Found && lvDevices.SelectedItems.Count > 0 &&
            lvDevices.SelectedItems[0].Text == "");
    }

    private void btDisconnect_Click()
    {
        Int32 Res = ((wclGattServerClient)lvDevices.SelectedItems[0].Tag).Disconnect();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Disconnect error: 0x" + Res.ToString("X8"));
    }

    private void Notify(Int64 Address)
    {
        Int32 Len = edData.text.Length;
        if (Len == 0)
            MessageBox.Show("Data is empty");
        else
        {
            Int32 Repeats;
            if (edRepeats.text == "")
                Repeats = 1;
            else
                Repeats = Convert.ToInt32(edRepeats.text);

            if (Repeats > 20)
                MessageBox.Show("Too many notification repeats");
            else
            {
                wclGattLocalCharacteristic Characteristic = (wclGattLocalCharacteristic)lvDevices.SelectedItems[0].Tag;

                if (Address == 0)
                    Trace("Sending notification to all subscribed clients");
                else
                    Trace("Sending notification to " + Address.ToString("X12"));
                Trace(4, "Characteristic: " + Characteristic.Uuid.ShortUuid.ToString("X4"));

                String Str = edData.text;
                if (Len % 2 != 0)
                {
                    Str = "0" + Str;
                    Len++;
                }

                Int32 DataLen = Len / 2;
                Byte[] Data = new Byte[DataLen];
                for (Int32 i = 0; i < DataLen; i++)
                    Data[i] = Convert.ToByte(Str.Substring(i * 2, 2), 16);

                for (Int32 i = 0; i < Repeats; i++)
                {
                    Int32 Start;
                    Int32 End;
                    Int32 Res;
                    if (Address == 0)
                    {
                        Start = Environment.TickCount;
                        Res = Characteristic.Notify(Data);
                        End = Environment.TickCount;
                    }
                    else
                    {
                        Start = Environment.TickCount;
                        Res = Characteristic.Notify(Address, Data);
                        End = Environment.TickCount;
                    }

                    if (Res != wclErrors.WCL_E_SUCCESS)
                        Trace(8, "Attempt [" + (i + 1).ToString() + "]", Res);
                    else
                    {
                        Trace(8, "Attempt [" + (i + 1).ToString() + "] took " +
                            (End - Start).ToString() + " ms");
                    }
                }

                Trace("Notification procedure completed");
            }
        }
    }

    private void btNotifyAll_Click()
    {
        Notify(0);
    }

    private void btNotify_Click()
    {
        for (Int32 i = lvDevices.SelectedItems[0].Index; i >= 0; i--)
        {
            if (lvDevices.Items[i].Text != "")
            {
                Notify(((wclGattServerClient)lvDevices.Items[i].Tag).Address);
                break;
            }
        }
    }

    private void btSetParameters_Click()
    {
        wclGattServerClient Client = (wclGattServerClient)lvDevices.SelectedItems[0].Tag;

        Trace("Set connection parameters for " + Client.Address.ToString("X12"));

        Int32 Res;
        wclBluetoothLeConnectionParametersValue ParamsValue = new wclBluetoothLeConnectionParametersValue();
        wclBluetoothLeConnectionParametersType Params;
        if (cbParams.value == 3)
        {
            ParamsValue.MinInterval = 84;
            ParamsValue.MaxInterval = 84;
            ParamsValue.Latency = 0;
            ParamsValue.LinkTimeout = 800;
            Res = Client.SetConnectionParams(ParamsValue);
        }
        else
        {
            switch (cbParams.value)
            {
                case 0:  Params = wclBluetoothLeConnectionParametersType.ppBalanced;            break;
                case 1:  Params = wclBluetoothLeConnectionParametersType.ppPowerOptimized;      break;
                case 2:  Params = wclBluetoothLeConnectionParametersType.ppThroughputOptimized; break;
                default: throw new Exception("Invalid connection parameters.");
            }
            Res = Client.SetConnectionParams(Params);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            Trace(4, "Set connection parameters", Res);
        else
            Trace(4, "Connection parameters set");
    }
}