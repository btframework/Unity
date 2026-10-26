using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using wclCommon;
using wclWiFi;

public class MobileHotspotController : MonoBehaviour
{
    public Button btOpen;
    public Button btClose;
    public Button btStart;
    public Button btStop;
    public Button btGetState;
    public Button btGetSsid;
    public Button btGetPwd;
    public Button btSetSsid;
    public Button btSetPwd;
    public Button btMaxClients;
    public Button btConnected;
    public Button btIsSupported;
    public Button btSetBand;
    public Button btGetBand;
    public Button btConnectedClients;
    public Button btCheckTimeout;
    public Button btEnableTimeout;
    public Button btDisableTimeout;

    public TMP_InputField edSsid;
    public TMP_InputField edPassword;
    public TMP_Dropdown cbBand;

    public RectTransform lvConnectedClientsContent;

    private ListView lvConnectedClients;
    private wclMobileHotspot _hotspot;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();
    private void Dispatch(Action a) { lock (_pendingActions) { _pendingActions.Enqueue(a); } }

    private bool _firstUpdateLogged;

    private void Awake()
    {
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "<invalid>";
        Debug.Log("MobileHotspotController.Awake scene=" + sceneName
                  + " activeScene=" + SceneManager.GetActiveScene().name
                  + " btOpen=" + (btOpen != null)
                  + " cbBand=" + (cbBand != null)
                  + " edSsid=" + (edSsid != null)
                  + " lvContent=" + (lvConnectedClientsContent != null));

        if (btOpen == null)
        {
            Debug.LogError("MobileHotspotController: btOpen is null. Serialized references missing. Abort Awake.");
            return;
        }

        btOpen.onClick.AddListener(() => Dispatch(btOpen_Click));
        btClose.onClick.AddListener(() => Dispatch(btClose_Click));
        btStart.onClick.AddListener(() => Dispatch(btStart_Click));
        btStop.onClick.AddListener(() => Dispatch(btStop_Click));
        btGetState.onClick.AddListener(() => Dispatch(btGetState_Click));
        btGetSsid.onClick.AddListener(() => Dispatch(btGetSsid_Click));
        btSetSsid.onClick.AddListener(() => Dispatch(btSetSsid_Click));
        btGetPwd.onClick.AddListener(() => Dispatch(btGetPwd_Click));
        btSetPwd.onClick.AddListener(() => Dispatch(btSetPwd_Click));
        btMaxClients.onClick.AddListener(() => Dispatch(btMaxClients_Click));
        btConnected.onClick.AddListener(() => Dispatch(btConnected_Click));
        btIsSupported.onClick.AddListener(() => Dispatch(btIsSupported_Click));
        btSetBand.onClick.AddListener(() => Dispatch(btSetBand_Click));
        btGetBand.onClick.AddListener(() => Dispatch(btGetBand_Click));
        btConnectedClients.onClick.AddListener(() => Dispatch(btConnectedClients_Click));
        btCheckTimeout.onClick.AddListener(() => Dispatch(btCheckTimeout_Click));
        btEnableTimeout.onClick.AddListener(() => Dispatch(btEnableTimeout_Click));
        btDisableTimeout.onClick.AddListener(() => Dispatch(btDisableTimeout_Click));

        if (lvConnectedClientsContent != null)
            lvConnectedClients = new ListView(lvConnectedClientsContent);
    }

    private void Start()
    {
        Debug.Log("MobileHotspotController.Start");
        Dispatch(DoStart);
    }

    private void DoStart()
    {
        Debug.Log("MobileHotspotController.DoStart: creating wclMobileHotspot");
        _hotspot = new wclMobileHotspot();
        _hotspot.OnClosed += (sender, e) => Dispatch(wclMobileHotspot_OnClosed);
        _hotspot.OnOpened += (sender, e) => Dispatch(wclMobileHotspot_OnOpened);

        cbBand.value = 0;
    }

    private void Update()
    {
        if (!_firstUpdateLogged)
        {
            _firstUpdateLogged = true;
            Debug.Log("MobileHotspotController.Update first call");
        }

        FitOpenDropdown(cbBand);

        while (true)
        {
            Action a;
            lock (_pendingActions) { if (_pendingActions.Count == 0) break; a = _pendingActions.Dequeue(); }
            try { a?.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    private void OnDestroy()
    {
        Debug.Log("MobileHotspotController.OnDestroy");
        try
        {
            if (_hotspot != null)
            {
                _hotspot.Close();
                _hotspot = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void Trace(string msg)
    {
        Debug.Log("Trace: " + msg);
        MessageBox.Show(msg, "Mobile Hotspot Demo");
    }

    private void Trace(string msg, int error)
    {
        Trace(msg + ". Error: 0x" + error.ToString("X8"));
    }

    private void wclMobileHotspot_OnOpened()
    {
        Debug.Log("wclMobileHotspot_OnOpened");
        Trace("Mobile hotspot opened.");
        string ssid;
        if (_hotspot.GetSsid(out ssid) == wclErrors.WCL_E_SUCCESS)
            edSsid.text = ssid;
        string passphrase;
        if (_hotspot.GetPassphrase(out passphrase) == wclErrors.WCL_E_SUCCESS)
            edPassword.text = passphrase;
    }

    private void wclMobileHotspot_OnClosed()
    {
        Debug.Log("wclMobileHotspot_OnClosed");
        Trace("Mobile hotspot closed.");
        edSsid.text = "";
        edPassword.text = "";
    }

    private void btOpen_Click()
    {
        Debug.Log("btOpen_Click");
        int res = _hotspot.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Open failed", res);
    }

    private void btClose_Click()
    {
        Debug.Log("btClose_Click");
        int res = _hotspot.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Close failed", res);
    }

    private void btStart_Click()
    {
        Debug.Log("btStart_Click");
        int res = _hotspot.Start();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Start failed", res);
    }

    private void btStop_Click()
    {
        Debug.Log("btStop_Click");
        int res = _hotspot.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Stop failed", res);
    }

    private void btGetState_Click()
    {
        Debug.Log("btGetState_Click");
        wclMobileHotspotState state;
        int res = _hotspot.GetState(out state);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get state failed", res);
        }
        else
        {
            switch (state)
            {
                case wclMobileHotspotState.mhsUnknown:
                    Trace("State: unknown");
                    break;
                case wclMobileHotspotState.mhsOn:
                    Trace("State: on");
                    break;
                case wclMobileHotspotState.mhsOff:
                    Trace("State: off");
                    break;
                case wclMobileHotspotState.mhsInTransition:
                    Trace("State: switching");
                    break;
            }
        }
    }

    private void btGetSsid_Click()
    {
        Debug.Log("btGetSsid_Click");
        string ssid;
        int res = _hotspot.GetSsid(out ssid);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get SSID failed", res);
        else
            edSsid.text = ssid;
    }

    private void btSetSsid_Click()
    {
        Debug.Log("btSetSsid_Click");
        int res = _hotspot.SetSsid(edSsid.text);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Set SSID failed", res);
    }

    private void btGetPwd_Click()
    {
        Debug.Log("btGetPwd_Click");
        string pwd;
        int res = _hotspot.GetPassphrase(out pwd);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get passphrase failed", res);
        else
            edPassword.text = pwd;
    }

    private void btSetPwd_Click()
    {
        Debug.Log("btSetPwd_Click");
        int res = _hotspot.SetPassphrase(edPassword.text);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Set passphrase failed", res);
    }

    private void btMaxClients_Click()
    {
        Debug.Log("btMaxClients_Click");
        uint count;
        int res = _hotspot.GetMaxClientCount(out count);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get max client connections failed", res);
        else
            Trace("Maximum allowed connections: " + count.ToString());
    }

    private void btConnected_Click()
    {
        Debug.Log("btConnected_Click");
        uint count;
        int res = _hotspot.GetClientCount(out count);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Get connectioned clients count failed", res);
        else
            Trace("Connected clients: " + count.ToString());
    }

    private wclMobileHotspotBand GetSelectedBand()
    {
        switch (cbBand.value)
        {
            case 0: return wclMobileHotspotBand.mhbAuto;
            case 1: return wclMobileHotspotBand.mhbTwoPointFourGigahertz;
            default: return wclMobileHotspotBand.mhbFiveGigahertz;
        }
    }

    private void btIsSupported_Click()
    {
        Debug.Log("btIsSupported_Click");
        var band = GetSelectedBand();
        bool supp;
        int res = _hotspot.IsBandSupported(band, out supp);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Check band failed", res);
        }
        else
        {
            if (supp)
                MessageBox.Show("Band is supported", "Mobile Hotspot Demo");
            else
                MessageBox.Show("Band is NOT supported", "Mobile Hotspot Demo");
        }
    }

    private void btSetBand_Click()
    {
        Debug.Log("btSetBand_Click");
        var band = GetSelectedBand();
        int res = _hotspot.SetBand(band);
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Set band failed", res);
    }

    private void btGetBand_Click()
    {
        Debug.Log("btGetBand_Click");
        wclMobileHotspotBand band;
        int res = _hotspot.GetBand(out band);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get band failed", res);
        }
        else
        {
            switch (band)
            {
                case wclMobileHotspotBand.mhbAuto:
                    MessageBox.Show("Band: Auto", "Mobile Hotspot Demo");
                    break;
                case wclMobileHotspotBand.mhbTwoPointFourGigahertz:
                    MessageBox.Show("Band: 2.4", "Mobile Hotspot Demo");
                    break;
                default:
                    MessageBox.Show("Band: 5.8", "Mobile Hotspot Demo");
                    break;
            }
        }
    }

    private void btConnectedClients_Click()
    {
        Debug.Log("btConnectedClients_Click");
        lvConnectedClients.Items.Clear();
        wclMobileHotspotClient[] clients;
        int res = _hotspot.GetClients(out clients);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get connected clients failed", res);
        }
        else
        {
            if (clients != null && clients.Length > 0)
            {
                foreach (var client in clients)
                {
                    var item = lvConnectedClients.Items.Add(client.Mac);
                    item.SubItems.Add(client.Name);
                }
            }
        }
    }

    private void btCheckTimeout_Click()
    {
        Debug.Log("btCheckTimeout_Click");
        bool enabled;
        int res = _hotspot.IsNoConnectionsTimeoutEnabled(out enabled);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get no connection timeout status failed", res);
        }
        else
        {
            if (enabled)
                MessageBox.Show("No connection timeout enabled", "Mobile Hotspot Demo");
            else
                MessageBox.Show("No connection timeout disabled", "Mobile Hotspot Demo");
        }
    }

    private void btEnableTimeout_Click()
    {
        Debug.Log("btEnableTimeout_Click");
        int res = _hotspot.EnableNoConnectionsTimeout();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Enable No Connection Timeout failed", res);
    }

    private void btDisableTimeout_Click()
    {
        Debug.Log("btDisableTimeout_Click");
        int res = _hotspot.DisableNoConnectionsTimeout();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Disable No Connection Timeout failed", res);
    }

    private static void FitOpenDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        var list = dropdown.transform.Find("Dropdown List");
        if (list == null) return;
        var viewport = list.Find("Viewport");
        if (viewport == null) return;
        var content = viewport.Find("Content");
        if (content == null) return;
        var viewportRt = viewport as RectTransform;
        var contentRt = content as RectTransform;
        if (viewportRt == null || contentRt == null) return;
        viewportRt.sizeDelta = new Vector2(viewportRt.sizeDelta.x, contentRt.rect.height + 10f);
    }
}

public static class MessageBox
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    private const uint MB_OK = 0x0;
    private const uint MB_ICONINFORMATION = 0x40;

    public static void Show(string text)
    {
        MessageBoxW(IntPtr.Zero, text, "Mobile Hotspot Demo", MB_OK | MB_ICONINFORMATION);
    }

    public static void Show(string text, string caption)
    {
        MessageBoxW(IntPtr.Zero, text, caption, MB_OK | MB_ICONINFORMATION);
    }
}

public class ListView
{
    public class SubItem
    {
        public string Text;
    }

    public class ListViewSubItems
    {
        private readonly List<SubItem> _items = new List<SubItem>();
        private readonly ListViewItem _owner;

        public ListViewSubItems(ListViewItem owner) { _owner = owner; }

        public int Count => _items.Count;
        public SubItem this[int i] => _items[i];

        public void Add(string text)
        {
            _items.Add(new SubItem { Text = text });
            _owner?.OnSubItemsChanged();
        }

        public void Add(SubItem item)
        {
            _items.Add(item);
            _owner?.OnSubItemsChanged();
        }

        public void Clear()
        {
            _items.Clear();
            _owner?.OnSubItemsChanged();
        }
    }

    public class ListViewItem
    {
        internal ListView Owner;
        internal GameObject RowGo;

        public string Text;
        public ListViewSubItems SubItems;
        public object Tag;
        public int Index;

        public ListViewItem() { SubItems = new ListViewSubItems(this); }

        internal void OnSubItemsChanged()
        {
            Owner?.RebuildRow(this);
        }
    }

    public class ListViewItemCollection
    {
        private readonly List<ListViewItem> _items = new List<ListViewItem>();
        private readonly ListView _listView;

        public ListViewItemCollection(ListView owner) { _listView = owner; }

        public int Count => _items.Count;
        public ListViewItem this[int i] => _items[i];

        public ListViewItem Add(string text)
        {
            var item = new ListViewItem { Text = text, Index = _items.Count, Owner = _listView };
            _items.Add(item);
            _listView.CreateRow(item);
            return item;
        }

        public ListViewItem Add(ListViewItem item)
        {
            item.Index = _items.Count;
            item.Owner = _listView;
            _items.Add(item);
            _listView.CreateRow(item);
            return item;
        }

        public void Clear()
        {
            _items.Clear();
            _listView.ClearRows();
        }

        public void RemoveAt(int i)
        {
            _items.RemoveAt(i);
            _listView.RebuildAllRows();
        }
    }

    private readonly RectTransform _content;
    private readonly RectTransform _header;
    private readonly List<float> _columnWidths = new List<float>();

    public readonly ListViewItemCollection Items;

    public ListViewItem SelectedItems(int index) => Items.Count > 0 ? Items[index] : null;
    public int SelectedIndex { get; set; } = -1;

    public ListView(RectTransform content)
    {
        _content = content;
        Items = new ListViewItemCollection(this);

        var scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            _header = scrollRect.transform.Find("Header") as RectTransform;

        if (_header != null)
        {
            for (int i = 0; i < _header.childCount; i++)
            {
                var cell = _header.GetChild(i) as RectTransform;
                if (cell == null) { _columnWidths.Add(100f); continue; }
                var le = cell.GetComponent<LayoutElement>();
                _columnWidths.Add(le != null ? le.preferredWidth : cell.rect.width);
            }
        }
    }

    internal void CreateRow(ListViewItem item)
    {
        var rowGo = new GameObject("Row_" + item.Index);
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.SetParent(_content, false);
        rowRt.anchorMin = new Vector2(0, 1);
        rowRt.anchorMax = new Vector2(1, 1);
        rowRt.pivot = new Vector2(0, 1);
        rowRt.anchoredPosition = Vector2.zero;
        rowRt.sizeDelta = new Vector2(0, 20);

        var le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 20;

        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.padding = new RectOffset(2, 2, 2, 2);

        item.RowGo = rowGo;
        RenderRowCells(item);
    }

    internal void RebuildRow(ListViewItem item)
    {
        if (item.RowGo == null) return;
        var t = item.RowGo.transform;
        for (int i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        RenderRowCells(item);
    }

    internal void RebuildAllRows()
    {
        for (int i = 0; i < Items.Count; i++)
            RebuildRow(Items[i]);
    }

    internal void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
    }

    private void RenderRowCells(ListViewItem item)
    {
        if (item.RowGo == null) return;
        var rowRt = item.RowGo.transform as RectTransform;
        AddCell(rowRt, item.Text, 0);
        for (int i = 0; i < item.SubItems.Count; i++)
            AddCell(rowRt, item.SubItems[i].Text, i + 1);
    }

    private void AddCell(RectTransform parent, string text, int column)
    {
        var cellGo = new GameObject("Cell_" + column);
        var cellRt = cellGo.AddComponent<RectTransform>();
        cellRt.SetParent(parent, false);

        var le = cellGo.AddComponent<LayoutElement>();
        le.preferredWidth = column < _columnWidths.Count ? _columnWidths[column] : 100f;
        le.preferredHeight = 20;

        var txtGo = new GameObject("Text");
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.SetParent(cellRt, false);
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4, 0);
        txtRt.offsetMax = new Vector2(-4, 0);

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 12;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }
}