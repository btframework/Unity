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
//  WinForms-style ListView (dynamic columns)
// ==========================================================

public class ListViewColumn
{
    public string Name;
    public float  Width;
}

public class ListViewColumnCollection
{
    private readonly ListView _owner;
    internal readonly List<ListViewColumn> _columns = new List<ListViewColumn>();

    internal ListViewColumnCollection(ListView owner) { _owner = owner; }

    public int Count => _columns.Count;
    public ListViewColumn this[int i] => _columns[i];

    public ListViewColumn Add(string name, float width)
    {
        var col = new ListViewColumn { Name = name ?? string.Empty, Width = width };
        _columns.Add(col);
        _owner.EnsureHeader();
        _owner.AppendHeaderCell(col, _columns.Count - 1);
        _owner.ResizeContent();
        return col;
    }

    public void Clear()
    {
        _columns.Clear();
        _owner.DestroyHeader();
        _owner.ResizeContent();
    }
}

public class ListView
{
    public RectTransform Content;
    public ListViewColumnCollection Columns { get; private set; }

    internal readonly List<ListViewItem> _items    = new List<ListViewItem>();
    internal readonly List<ListViewItem> _selected = new List<ListViewItem>();

    internal GameObject _headerGO;

    private static readonly Color SelectedColor = new Color(0.70f, 0.85f, 0.98f, 1f);
    private static readonly Color NormalColor   = Color.white;

    private ListViewItems _itemsColl;
    private ListViewSelectedItems _selectedColl;

    public ListViewItems         Items         => _itemsColl ?? (_itemsColl = new ListViewItems(this));
    public ListViewSelectedItems SelectedItems => _selectedColl ?? (_selectedColl = new ListViewSelectedItems(this));

    public ListView(RectTransform content, string[] headers, float[] widths)
    {
        Content = content;
        Columns = new ListViewColumnCollection(this);

        if (headers != null && widths != null)
        {
            for (int i = 0; i < headers.Length && i < widths.Length; i++)
                Columns.Add(headers[i], widths[i]);
        }
    }

    internal void EnsureHeader()
    {
        if (_headerGO != null) return;

        _headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        _headerGO.transform.SetParent(Content, false);
        _headerGO.transform.SetSiblingIndex(0);
        _headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        var hlg = _headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        var hle = _headerGO.GetComponent<LayoutElement>();
        hle.preferredHeight = 18f;
        hle.minHeight = 18f;
    }

    internal void AppendHeaderCell(ListViewColumn col, int index)
    {
        var cellGO = new GameObject("h" + index, typeof(RectTransform), typeof(LayoutElement));
        cellGO.transform.SetParent(_headerGO.transform, false);
        var cellLe = cellGO.GetComponent<LayoutElement>();
        cellLe.preferredWidth = col.Width;
        cellLe.minWidth = col.Width;
        var tmp = cellGO.AddComponent<TextMeshProUGUI>();
        tmp.text = col.Name;
        tmp.fontSize = 11;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.margin = new Vector4(4, 0, 2, 0);
        tmp.raycastTarget = false;
    }

    internal void DestroyHeader()
    {
        if (_headerGO != null)
        {
            _headerGO.SetActive(false);
            UnityEngine.Object.Destroy(_headerGO);
            _headerGO = null;
        }
    }

    internal void ResizeContent()
    {
        float totalW = 0;
        for (int i = 0; i < Columns.Count; i++) totalW += Columns[i].Width;
        if (Content != null)
            Content.sizeDelta = new Vector2(totalW, Content.sizeDelta.y);
    }

    internal ListViewItem AddItem(string text)
    {
        var item = new ListViewItem(this);
        item.Index = _items.Count;
        item.Values.Add(text ?? string.Empty);
        if (Content != null && Columns.Count > 0)
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
        if (item.RowGO != null)
        {
            item.RowGO.SetActive(false);
            UnityEngine.Object.Destroy(item.RowGO);
        }
        for (int i = 0; i < _items.Count; i++) _items[i].Index = i;
        return true;
    }

    internal void ClearItems()
    {
        foreach (var it in _items)
        {
            if (it.RowGO != null)
            {
                it.RowGO.SetActive(false);
                UnityEngine.Object.Destroy(it.RowGO);
            }
        }
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
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        var le = rowGO.GetComponent<LayoutElement>();
        le.preferredHeight = 18f;
        le.minHeight = 18f;

        Cells = new TextMeshProUGUI[Owner.Columns.Count];
        for (int i = 0; i < Owner.Columns.Count; i++)
        {
            var col = Owner.Columns[i];
            var cellGO = new GameObject("c" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(rowGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = col.Width;
            cellLe.minWidth = col.Width;
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
    internal string Get(int i) => _lines[i];

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
    public string this[int i] => _owner.Get(i);
}

// ==========================================================
//  Controller (main)
// ==========================================================
[Preserve]
public class NetworkListManagerController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btOpen;
    public Button btClose;
    public Button btIsConnected;
    public Button btIsConnectedToInternet;
    public Button btConnectivity;
    public Button btEnumNetworks;
    public Button btRefresh;
    public Button btSetName;
    public Button btSetDescription;
    public Button btSetCategory;
    public Button btDelete;
    public Button btGetMachineConnections;
    public Button btGetNetworkConnections;
    public Button btEnumAdapters;
    public Button btClear;

    [Header("Dropdowns")]
    public TMP_Dropdown cbNetworkType;
    public TMP_Dropdown cbCategory;

    [Header("Inputs")]
    public TMP_InputField edNewNameOrDescription;

    [Header("ListView Contents")]
    public RectTransform lvNetworksContent;
    public RectTransform lvConnectionsContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbEventsText;

    private ListView lvNetworks;
    private ListView lvConnections;
    private ListBox  lbEvents;

    private wclNetworkListManager wclNetworkListManager;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvNetworks = new ListView(lvNetworksContent, new[] {
            "Id", "Category", "Connectivity", "Description", "Domain type",
            "Name", "Creation Date Time", "Connected Date Time",
            "Connected", "Connected To Internet"
        }, new float[] { 250, 100, 100, 100, 100, 100, 100, 100, 80, 100 });

        lvConnections = new ListView(lvConnectionsContent, new string[0], new float[0]);

        lbEvents = new ListBox(lbEventsText);

        btOpen.onClick.AddListener(() => Dispatch(btOpen_Click));
        btClose.onClick.AddListener(() => Dispatch(btClose_Click));
        btIsConnected.onClick.AddListener(() => Dispatch(btIsConnected_Click));
        btIsConnectedToInternet.onClick.AddListener(() => Dispatch(btIsConnectedToInternet_Click));
        btConnectivity.onClick.AddListener(() => Dispatch(btConnectivity_Click));
        btEnumNetworks.onClick.AddListener(() => Dispatch(btEnumNetworks_Click));
        btRefresh.onClick.AddListener(() => Dispatch(btRefresh_Click));
        btSetName.onClick.AddListener(() => Dispatch(btSetName_Click));
        btSetDescription.onClick.AddListener(() => Dispatch(btSetDescription_Click));
        btSetCategory.onClick.AddListener(() => Dispatch(btSetCategory_Click));
        btDelete.onClick.AddListener(() => Dispatch(btDelete_Click));
        btGetMachineConnections.onClick.AddListener(() => Dispatch(btGetMachineConnections_Click));
        btGetNetworkConnections.onClick.AddListener(() => Dispatch(btGetNetworkConnections_Click));
        btEnumAdapters.onClick.AddListener(() => Dispatch(btEnumAdapters_Click));
        btClear.onClick.AddListener(() => Dispatch(btClear_Click));
    }

    private void Start()
    {
        Dispatch(DoStart);
    }

    private void DoStart()
    {
        cbNetworkType.value = 2;
        cbCategory.value = 0;

        wclNetworkListManager = new wclNetworkListManager();
        wclNetworkListManager.AfterOpen                    += new EventHandler(wclNetworkListManager_AfterOpen);
        wclNetworkListManager.BeforeClose                  += new EventHandler(wclNetworkListManager_BeforeClose);
        wclNetworkListManager.OnConnectivityChanged        += new wclNlmConnectivityChangeEvent(wclNetworkListManager_OnConnectivityChanged);
        wclNetworkListManager.OnNetworkAdded               += new wclNlmNetworkEvent(wclNetworkListManager_OnNetworkAdded);
        wclNetworkListManager.OnNetworkConnectivityChanged += new wclNlmNetworkConnectivityChangedEvent(wclNetworkListManager_OnNetworkConnectivityChanged);
        wclNetworkListManager.OnNetworkDeleted             += new wclNlmNetworkEvent(wclNetworkListManager_OnNetworkDeleted);
        wclNetworkListManager.OnNetworkPropertyChanged     += new wclNlmNetworkPropertyChangedEvent(wclNetworkListManager_OnNetworkPropertyChanged);
        wclNetworkListManager.OnConnectionConnectivityChanged += new wclNlmConnectionConnectivityChangedEvent(wclNetworkListManager_OnConnectionConnectivityChanged);
        wclNetworkListManager.OnConnectionPropertyChanged  += new wclNlmConnectionPropertyChangedEvent(wclNetworkListManager_OnConnectionPropertyChanged);
    }

    private void Update()
    {
        FitOpenDropdown(cbNetworkType);
        FitOpenDropdown(cbCategory);

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
        if (wclNetworkListManager != null)
        {
            try { wclNetworkListManager.Close(); } catch { }
            wclNetworkListManager = null;
        }
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

    private void AddFlag(ref String Str, String Flag)
    {
        if (Str.Length > 0)
            Str += ", ";
        Str += Flag;
    }

    private Boolean ShowError(Int32 Error)
    {
        if (Error != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Error.ToString("X8"));
            return true;
        }
        return false;
    }

    // ==========================================================
    //  Manager events
    // ==========================================================
    private void wclNetworkListManager_OnConnectionPropertyChanged(object Sender, Guid ConnectionId, wclNlmConnectionProperty Prop)
    {
        string id = ConnectionId.ToString(); string prop = Prop.ToString();
        Dispatch(() => lbEvents.Items.Add("Connection: " + id + " property changed: " + prop));
    }

    private void wclNetworkListManager_OnConnectionConnectivityChanged(object Sender, Guid ConnectionId, wclNlmConnectivity Connectivity)
    {
        string id = ConnectionId.ToString(); string conn = Connectivity.ToString();
        Dispatch(() => lbEvents.Items.Add("Connection: " + id + " connectivity changed: " + conn));
    }

    private void wclNetworkListManager_OnNetworkPropertyChanged(object Sender, Guid NetworkId, wclNlmNetworkPropertyChange Change)
    {
        string id = NetworkId.ToString(); string ch = Change.ToString();
        Dispatch(() => lbEvents.Items.Add("Network " + id + " property changed: " + ch));
    }

    private void wclNetworkListManager_OnNetworkDeleted(object Sender, Guid NetworkId)
    {
        string id = NetworkId.ToString();
        Dispatch(() => lbEvents.Items.Add("Network deleted: " + id));
    }

    private void wclNetworkListManager_OnNetworkConnectivityChanged(object Sender, Guid NetworkId, wclNlmConnectivity Connectivity)
    {
        string id = NetworkId.ToString(); string conn = Connectivity.ToString();
        Dispatch(() => lbEvents.Items.Add("Network " + id + " connectivity changed: " + conn));
    }

    private void wclNetworkListManager_OnNetworkAdded(object Sender, Guid NetworkId)
    {
        string id = NetworkId.ToString();
        Dispatch(() => lbEvents.Items.Add("Network added: " + id));
    }

    private void wclNetworkListManager_OnConnectivityChanged(object Sender, wclNlmConnectivity Connectivity)
    {
        string conn = Connectivity.ToString();
        Dispatch(() => lbEvents.Items.Add("Connectivity changed: " + conn));
    }

    private void wclNetworkListManager_BeforeClose(object sender, EventArgs e)
    {
        Dispatch(() => lbEvents.Items.Add("BeforeClose"));
    }

    private void wclNetworkListManager_AfterOpen(object sender, EventArgs e)
    {
        Dispatch(() => lbEvents.Items.Add("AfterOpen"));
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClear_Click()
    {
        lbEvents.Items.Clear();
    }

    private void btOpen_Click()
    {
        ShowError(wclNetworkListManager.Open());
    }

    private void btClose_Click()
    {
        ShowError(wclNetworkListManager.Close());
    }

    private void btIsConnected_Click()
    {
        bool Connected;
        if (!ShowError(wclNetworkListManager.GetConnected(out Connected)))
        {
            if (Connected)
                MessageBox.Show("Connected");
            else
                MessageBox.Show("NOT Connected");
        }
    }

    private void btIsConnectedToInternet_Click()
    {
        bool Connected;
        if (!ShowError(wclNetworkListManager.GetConnectedToInternet(out Connected)))
        {
            if (Connected)
                MessageBox.Show("Connected");
            else
                MessageBox.Show("NOT Connected");
        }
    }

    private void btConnectivity_Click()
    {
        wclNlmConnectivity Connectivity;
        if (!ShowError(wclNetworkListManager.GetConnectivity(out Connectivity)))
            MessageBox.Show("Connectivity: " + Connectivity.ToString());
    }

    private void btEnumNetworks_Click()
    {
        lvNetworks.Items.Clear();

        wclNlmEnumNetwork Flag = (wclNlmEnumNetwork)cbNetworkType.value;
        wclNlmNetwork[] Networks;
        Int32 Res = wclNetworkListManager.GetNetworks(Flag, out Networks);
        try
        {
            if (!ShowError(Res))
            {
                for (int i = 0; i < Networks.Length; i++)
                {
                    wclNlmNetwork Network = Networks[i];
                    ListViewItem Item = lvNetworks.Items.Add("");

                    Guid Id;
                    Res = Network.GetNetworkId(out Id);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.Text = Id.ToString();
                    else
                        Item.Text = "Error: 0x" + Res.ToString("X8");

                    wclNlmNetworkCategory Category;
                    Res = Network.GetCategory(out Category);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(Category.ToString());
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                    wclNlmConnectivity Connectivity;
                    Res = Network.GetConnectivity(out Connectivity);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(Connectivity.ToString());
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                    String Description;
                    Res = Network.GetDescription(out Description);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(Description);
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                    wclNlmDomainType Domain;
                    Res = Network.GetDomainType(out Domain);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(Domain.ToString());
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                    String AName;
                    Res = Network.GetName(out AName);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(AName);
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                    DateTime CreationDateTime;
                    DateTime ConnectedDateTime;
                    Res = Network.GetTimeCreatedAndConnected(out CreationDateTime, out ConnectedDateTime);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                    {
                        Item.SubItems.Add(CreationDateTime.ToString());
                        Item.SubItems.Add(ConnectedDateTime.ToString());
                    }
                    else
                    {
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
                    }

                    bool Connected;
                    Res = Network.GetConnected(out Connected);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(Connected.ToString());
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                    Res = Network.GetConnectedToInternet(out Connected);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        Item.SubItems.Add(Connected.ToString());
                    else
                        Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
                }
            }
        }
        finally
        {
            Networks = null;
        }
    }

    private void btRefresh_Click()
    {
        if (lvNetworks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select network");
            return;
        }

        wclNlmNetwork Network;
        ListViewItem Item = lvNetworks.SelectedItems[0];
        Guid Id = new Guid(Item.SubItems[0].Text);
        if (!ShowError(wclNetworkListManager.GetNetwork(Id, out Network)))
        {
            try
            {
                wclNlmNetworkCategory Category;
                int Res = Network.GetCategory(out Category);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[1].Text = Category.ToString();
                else
                    Item.SubItems[1].Text = "Error: 0x" + Res.ToString("X8");

                wclNlmConnectivity Connectivity;
                Res = Network.GetConnectivity(out Connectivity);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[2].Text = Connectivity.ToString();
                else
                    Item.SubItems[2].Text = "Error: 0x" + Res.ToString("X8");

                String Description;
                Res = Network.GetDescription(out Description);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[3].Text = Description;
                else
                    Item.SubItems[3].Text = "Error: 0x" + Res.ToString("X8");

                wclNlmDomainType Domain;
                Res = Network.GetDomainType(out Domain);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[4].Text = Domain.ToString();
                else
                    Item.SubItems[4].Text = "Error: 0x" + Res.ToString("X8");

                String AName;
                Res = Network.GetName(out AName);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[5].Text = AName;
                else
                    Item.SubItems[5].Text = "Error: 0x" + Res.ToString("X8");

                DateTime CreationDateTime;
                DateTime ConnectedDateTime;
                Res = Network.GetTimeCreatedAndConnected(out CreationDateTime, out ConnectedDateTime);
                if (Res == wclErrors.WCL_E_SUCCESS)
                {
                    Item.SubItems[6].Text = CreationDateTime.ToString();
                    Item.SubItems[7].Text = ConnectedDateTime.ToString();
                }
                else
                {
                    Item.SubItems[6].Text = "Error: 0x" + Res.ToString("X8");
                    Item.SubItems[7].Text = "Error: 0x" + Res.ToString("X8");
                }

                bool Connected;
                Res = Network.GetConnected(out Connected);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[8].Text = Connected.ToString();
                else
                    Item.SubItems[8].Text = "Error: 0x" + Res.ToString("X8");

                Res = Network.GetConnectedToInternet(out Connected);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems[9].Text = Connected.ToString();
                else
                    Item.SubItems[9].Text = "Error: 0x" + Res.ToString("X8");
            }
            finally
            {
                Network = null;
            }
        }
    }

    private void btSetName_Click()
    {
        if (lvNetworks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select network");
            return;
        }

        wclNlmNetwork Network;
        int Res = wclNetworkListManager.GetNetwork(new Guid(lvNetworks.SelectedItems[0].SubItems[0].Text), out Network);
        if (!ShowError(Res))
        {
            if (ShowError(Network.SetName(edNewNameOrDescription.text)))
                MessageBox.Show("Admin privilegies required");

            Network = null;
        }
    }

    private void btSetDescription_Click()
    {
        if (lvNetworks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select network");
            return;
        }

        wclNlmNetwork Network;
        int Res = wclNetworkListManager.GetNetwork(new Guid(lvNetworks.SelectedItems[0].SubItems[0].Text), out Network);
        if (!ShowError(Res))
        {
            if (ShowError(Network.SetDescription(edNewNameOrDescription.text)))
                MessageBox.Show("Admin privilegies required");

            Network = null;
        }
    }

    private void btSetCategory_Click()
    {
        if (lvNetworks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select network");
            return;
        }

        wclNlmNetwork Network;
        int Res = wclNetworkListManager.GetNetwork(new Guid(lvNetworks.SelectedItems[0].SubItems[0].Text), out Network);
        if (!ShowError(Res))
        {
            if (ShowError(Network.SetCategory((wclNlmNetworkCategory)cbCategory.value)))
                MessageBox.Show("Admin privilegies required");

            Network = null;
        }
    }

    private void ShowConnections(wclNlmConnection[] Connections)
    {
        lvConnections.Items.Clear();

        lvConnections.Columns.Clear();
        lvConnections.Columns.Add("Adapter Id", 200);
        lvConnections.Columns.Add("Connected", 80);
        lvConnections.Columns.Add("Connected to Internet", 100);
        lvConnections.Columns.Add("Connectivity", 100);
        lvConnections.Columns.Add("Domain type", 100);
        lvConnections.Columns.Add("Id", 200);

        if (Connections == null)
            return;

        try
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                wclNlmConnection Connection = Connections[i];
                ListViewItem Item = lvConnections.Items.Add("");

                Guid Id;
                int Res = Connection.GetAdapterId(out Id);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.Text = Id.ToString();
                else
                    Item.Text = "Error: 0x" + Res.ToString("X8");

                bool Connected;
                Res = Connection.GetConnected(out Connected);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems.Add(Connected.ToString());
                else
                    Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                Res = Connection.GetConnectedToInternet(out Connected);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems.Add(Connected.ToString());
                else
                    Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                wclNlmConnectivity Connectivity;
                Res = Connection.GetConnectivity(out Connectivity);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems.Add(Connectivity.ToString());
                else
                    Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                wclNlmDomainType Domain;
                Res = Connection.GetDomainType(out Domain);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems.Add(Domain.ToString());
                else
                    Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                Res = Connection.GetId(out Id);
                if (Res == wclErrors.WCL_E_SUCCESS)
                    Item.SubItems.Add(Id.ToString());
                else
                    Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
            }
        }
        finally
        {
            Connections = null;
        }
    }

    private void btGetMachineConnections_Click()
    {
        wclNlmConnection[] Connections;
        if (!ShowError(wclNetworkListManager.GetConnections(out Connections)))
            ShowConnections(Connections);
    }

    private void btGetNetworkConnections_Click()
    {
        if (lvNetworks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select network");
            return;
        }

        wclNlmNetwork Network;
        int Res = wclNetworkListManager.GetNetwork(new Guid(lvNetworks.SelectedItems[0].SubItems[0].Text), out Network);
        if (!ShowError(Res))
        {
            try
            {
                wclNlmConnection[] Connections;
                if (!ShowError(Network.GetConnections(out Connections)))
                    ShowConnections(Connections);
            }
            finally
            {
                Network = null;
            }
        }
    }

    private void btDelete_Click()
    {
        if (lvNetworks.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select network");
            return;
        }

        int Res = wclNetworkListManager.DeleteNetwork(new Guid(lvNetworks.SelectedItems[0].SubItems[0].Text));
        if (!ShowError(Res))
            lvNetworks.Items.Remove(lvNetworks.SelectedItems[0]);
    }

    private void btEnumAdapters_Click()
    {
        wclNetworkAdapter[] Adapters;
        if (!ShowError(wclNetworkListManager.EnumAdapters(out Adapters)))
        {
            lvConnections.Items.Clear();

            lvConnections.Columns.Clear();
            lvConnections.Columns.Add("Id", 150);
            lvConnections.Columns.Add("Friendly name", 150);
            lvConnections.Columns.Add("Description", 150);
            lvConnections.Columns.Add("MAC", 100);
            lvConnections.Columns.Add("IPs", 200);
            lvConnections.Columns.Add("DNS suffix", 100);
            lvConnections.Columns.Add("MTU", 80);
            lvConnections.Columns.Add("Flags", 150);
            lvConnections.Columns.Add("Status", 150);

            if (Adapters != null && Adapters.Length > 0)
            {
                foreach (wclNetworkAdapter Adapter in Adapters)
                {
                    ListViewItem Item = lvConnections.Items.Add(Adapter.Id);
                    Item.SubItems.Add(Adapter.FriendlyName);
                    Item.SubItems.Add(Adapter.Description);
                    Item.SubItems.Add(Adapter.Mac);

                    String Str = "";
                    if (Adapter.IPs != null && Adapter.IPs.Length > 0)
                    {
                        foreach (String IP in Adapter.IPs)
                            AddFlag(ref Str, IP);
                    }
                    Str = "[" + Str + "]";
                    Item.SubItems.Add(Str);

                    Item.SubItems.Add(Adapter.DnsSuffix);
                    Item.SubItems.Add(Adapter.Mtu.ToString());

                    Str = "";
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afDdnsEnabled) != 0)
                        AddFlag(ref Str, "afDdnsEnabled");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afRegisterSuffix) != 0)
                        AddFlag(ref Str, "afRegisterSuffix");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afDhcpEnabled) != 0)
                        AddFlag(ref Str, "afDhcpEnabled");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afReceiveOnly) != 0)
                        AddFlag(ref Str, "afReceiveOnly");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afNoMulticast) != 0)
                        AddFlag(ref Str, "afNoMulticast");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afIpv6OtherStatefulConfig) != 0)
                        AddFlag(ref Str, "afIpv6OtherStatefulConfig");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afNetbiosOverTcpIpEnabled) != 0)
                        AddFlag(ref Str, "afNetbiosOverTcpIpEnabled");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afIpv4Enabled) != 0)
                        AddFlag(ref Str, "afIpv4Enabled");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afIpv6Enabled) != 0)
                        AddFlag(ref Str, "afIpv6Enabled");
                    if ((Adapter.Flags & wclNetworkAdapterFlag.afIpv6ManageAddressConfig) != 0)
                        AddFlag(ref Str, "afIpv6ManageAddressConfig");
                    Str = "[" + Str + "]";
                    Item.SubItems.Add(Str);

                    switch (Adapter.Status)
                    {
                        case wclNetworkInterfaceStatus.isUp:
                            Item.SubItems.Add("Up");
                            break;
                        case wclNetworkInterfaceStatus.isDown:
                            Item.SubItems.Add("Down");
                            break;
                        case wclNetworkInterfaceStatus.isTesting:
                            Item.SubItems.Add("Testring");
                            break;
                        case wclNetworkInterfaceStatus.isDormant:
                            Item.SubItems.Add("Dormant");
                            break;
                        case wclNetworkInterfaceStatus.isNotPresent:
                            Item.SubItems.Add("Not present");
                            break;
                        case wclNetworkInterfaceStatus.isLowerLayerDown:
                            Item.SubItems.Add("Low Layer Down");
                            break;
                        default:
                            Item.SubItems.Add("Unknown");
                            break;
                    }
                }
            }
        }
    }
}