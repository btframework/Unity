using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using TMPro;

using wclWiFi;
using wclCommon;

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
//  Controller
// ==========================================================
[Preserve]
public class WiFiHostedNetworkController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btHnOpen;
    public Button btHnClose;
    public Button btHnClear;
    public Button btHnGetConnectionSettings;
    public Button btHnGetKey;
    public Button btHnGetProfile;
    public Button btHnGetSecuritySettings;
    public Button btHnGetStatus;
    public Button btHnGetState;
    public Button btHnRefreshSecuritySettings;
    public Button btHnRestart;
    public Button btHnSetSettings;
    public Button btHnSetKey;
    public Button btHnEnable;
    public Button btHnDisable;

    [Header("Inputs")]
    public TMP_InputField edSsHnid;
    public TMP_InputField edHnNumberOfPeers;
    public TMP_InputField edHnKey;

    [Header("Checkboxes")]
    public Toggle cbHnPersistent;

    [Header("ListView Content")]
    public RectTransform lvHnDataContent;

    private ListView lvHnData;

    private wclWiFiHostedNetwork Network;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvHnData = new ListView(lvHnDataContent, new float[] { 280, 280 });

        btHnOpen.onClick.AddListener(() => Dispatch(btHnOpen_Click));
        btHnClose.onClick.AddListener(() => Dispatch(btHnClose_Click));
        btHnClear.onClick.AddListener(btHnClear_Click);
        btHnGetConnectionSettings.onClick.AddListener(() => Dispatch(btHnGetConnectionSettings_Click));
        btHnGetKey.onClick.AddListener(() => Dispatch(btHnGetKey_Click));
        btHnGetProfile.onClick.AddListener(() => Dispatch(btHnGetProfile_Click));
        btHnGetSecuritySettings.onClick.AddListener(() => Dispatch(btHnGetSecuritySettings_Click));
        btHnGetStatus.onClick.AddListener(() => Dispatch(btHnGetStatus_Click));
        btHnGetState.onClick.AddListener(() => Dispatch(btHnGetState_Click));
        btHnRefreshSecuritySettings.onClick.AddListener(() => Dispatch(btHnRefreshSecuritySettings_Click));
        btHnRestart.onClick.AddListener(() => Dispatch(btHnRestart_Click));
        btHnSetSettings.onClick.AddListener(() => Dispatch(btHnSetSettings_Click));
        btHnSetKey.onClick.AddListener(() => Dispatch(btHnSetKey_Click));
        btHnEnable.onClick.AddListener(() => Dispatch(btHnEnable_Click));
        btHnDisable.onClick.AddListener(() => Dispatch(btHnDisable_Click));
    }

    private void Start() { Dispatch(DoStart); }

    private void DoStart()
    {
        Network = new wclWiFiHostedNetwork();
        Network.AfterOpen   += new EventHandler(Network_AfterOpen);
        Network.BeforeClose += new EventHandler(Network_BeforeClose);
    }

    private void Update()
    {
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
        if (Network != null) { try { Network.Close(); } catch { } }
        Network = null;
    }

    // ==========================================================
    //  Dispatcher / helpers
    // ==========================================================
    private void Dispatch(Action a) { lock (_pendingActions) { _pendingActions.Enqueue(a); } }

    private Boolean ShowResult(Int32 Res)
    {
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Res.ToString("X8"));
            return false;
        }
        return true;
    }

    private void HnClear() { lvHnData.Items.Clear(); }

    private void HnAddData(String ParamName, String ParamValue)
    {
        ListViewItem Item = lvHnData.Items.Add(ParamName);
        Item.SubItems.Add(ParamValue);
    }

    // ==========================================================
    //  Network events
    // ==========================================================
    private void Network_AfterOpen(object sender, EventArgs e)
    {
        Dispatch(() => MessageBox.Show("WiFi Hosted Network opened"));
    }

    private void Network_BeforeClose(object sender, EventArgs e)
    {
        Dispatch(() => MessageBox.Show("WiFi Hosted Network closing"));
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btHnOpen_Click()  { ShowResult(Network.Open()); }
    private void btHnClose_Click() { ShowResult(Network.Close()); }

    private void btHnClear_Click() { HnClear(); }

    private void btHnGetConnectionSettings_Click()
    {
        wclWiFiHostedNetworkConnectionSettings Settings;

        HnClear();

        if (ShowResult(Network.GetConnectionSettings(out Settings)))
        {
            HnAddData("SSID", Settings.Ssid);
            HnAddData("Max. number of peers", Settings.MaxNumberOfPeers.ToString());
        }
    }

    private void btHnGetKey_Click()
    {
        HnClear();

        Byte[] KeyData;
        Boolean IsPassPhrase;
        Boolean Persistent;
        if (ShowResult(Network.GetKey(out KeyData, out IsPassPhrase, out Persistent)))
        {
            String Str;
            if (IsPassPhrase)
                Str = Encoding.ASCII.GetString(KeyData);
            else
            {
                Str = "";
                if (KeyData != null)
                    for (Int32 i = 0; i < KeyData.Length; i++)
                        Str = Str + KeyData[i].ToString("X2");
            }

            if (KeyData != null)
                HnAddData("Key length", KeyData.Length.ToString());
            else
                HnAddData("Key length", "0");
            HnAddData("Key data", Str);
            HnAddData("Is pass phrase", IsPassPhrase.ToString());
            HnAddData("Persistent", Persistent.ToString());
        }
    }

    private void btHnGetProfile_Click()
    {
        HnClear();

        String Xml;
        if (ShowResult(Network.GetProfile(out Xml)))
            HnAddData("Profile XML", Xml);
    }

    private void btHnGetSecuritySettings_Click()
    {
        HnClear();

        wclWiFiAuthCipherPair Settings;
        if (ShowResult(Network.GetSecuritySettings(out Settings)))
        {
            HnAddData("Auth algorithm", Settings.AuthAlgorithm.ToString());
            HnAddData("Cipher algorithm", Settings.CipherAlgorithm.ToString());
        }
    }

    private void btHnGetStatus_Click()
    {
        HnClear();

        wclWiFiHostedNetworkStatus Status;
        if (ShowResult(Network.GetStatus(out Status)))
        {
            HnAddData("State", Status.State.ToString());
            HnAddData("Interface ID", Status.Id.ToString());
            HnAddData("BSS ID", Status.BssId);
            HnAddData("Phy", Status.Phy.ToString());
            HnAddData("ChannelFrequency", Status.ChannelFrequency.ToString());

            String Address;
            Int32 Res = Network.GetLocalIp(out Address);
            if (Res == wclErrors.WCL_E_SUCCESS)
                HnAddData("Local IP", Address);
            else
                HnAddData("Local IP", "Get failed: 0x" + Res.ToString("X8"));

            if (Status.Peers != null)
            {
                for (Int32 i = 0; i < Status.Peers.Length; i++)
                {
                    HnAddData("Peer[" + i.ToString() + "] MAC", Status.Peers[i].Mac);
                    Res = Network.GetRemoteIp(Status.Peers[i].Mac, out Address);
                    if (Res == wclErrors.WCL_E_SUCCESS)
                        HnAddData("Peer[" + i.ToString() + "] IP", Address);
                    else
                        HnAddData("Peer[" + i.ToString() + "] IP", "Get failed: 0x" + Res.ToString("X8"));
                    HnAddData("Peer[" + i.ToString() + "] auth state", Status.Peers[i].AuthState.ToString());
                }
            }
        }
    }

    private void btHnGetState_Click()
    {
        HnClear();

        Boolean State;
        if (ShowResult(Network.GetState(out State)))
            HnAddData("Enabled", State.ToString());
    }

    private void btHnRefreshSecuritySettings_Click()
    {
        ShowResult(Network.RefreshSecuritySettings());
    }

    private void btHnRestart_Click()
    {
        ShowResult(Network.Restart());
    }

    private void btHnSetSettings_Click()
    {
        wclWiFiHostedNetworkConnectionSettings Settings = new wclWiFiHostedNetworkConnectionSettings();
        Settings.Ssid = edSsHnid.text;
        Settings.MaxNumberOfPeers = Convert.ToUInt16(edHnNumberOfPeers.text);
        ShowResult(Network.SetConnectionSettings(Settings));
    }

    private void btHnSetKey_Click()
    {
        Byte[] Tmp = Encoding.ASCII.GetBytes(edHnKey.text);
        Byte[] KeyData = new Byte[Tmp.Length + 1];
        Tmp.CopyTo(KeyData, 0);
        ShowResult(Network.SetKey(KeyData, true, cbHnPersistent.isOn));
    }

    private void btHnEnable_Click()
    {
        ShowResult(Network.SetState(true));
    }

    private void btHnDisable_Click()
    {
        ShowResult(Network.SetState(false));
    }
}