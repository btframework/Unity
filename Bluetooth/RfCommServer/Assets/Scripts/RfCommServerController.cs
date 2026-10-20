using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclBluetooth;
using wclCommunication;

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

    internal void RemoveItemAt(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        var item = _items[index];
        _items.RemoveAt(index);
        _selected.Remove(item);
        if (item.RowGO != null) UnityEngine.Object.Destroy(item.RowGO);
        for (int i = 0; i < _items.Count; i++) _items[i].Index = i;
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
    public void Clear() => _owner.ClearItems();
    public bool Remove(ListViewItem item) => _owner.RemoveItem(item);
    public void RemoveAt(int index) => _owner.RemoveItemAt(index);

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
//  Controller (fmMain)
// ==========================================================
public class RfCommServerController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btListen;
    public Button btClose;
    public Button btDisconnect;
    public Button btSend;
    public Button btGetBuffers;
    public Button btSetBuffers;
    public Button btClear;

    [Header("Checkboxes")]
    public Toggle cbAuthentication;
    public Toggle cbEncryption;

    [Header("Inputs")]
    public TMP_InputField edServiceName;
    public TMP_InputField edText;
    public TMP_InputField edReadBuffer;
    public TMP_InputField edWriteBuffer;

    [Header("ListView")]
    public RectTransform lvClientsContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbEventsText;

    private ListView lvClients;
    private ListBox  lbEvents;

    private wclRfCommServer Server;
    private wclBluetoothManager Manager;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvClients = new ListView(lvClientsContent, new float[] { 150, 150 });
        lbEvents  = new ListBox(lbEventsText);

        btListen.onClick.AddListener(() => Dispatch(btListen_Click));
        btClose.onClick.AddListener(() => Dispatch(btClose_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btSend.onClick.AddListener(() => Dispatch(btSend_Click));
        btGetBuffers.onClick.AddListener(() => Dispatch(btGetBuffers_Click));
        btSetBuffers.onClick.AddListener(() => Dispatch(btSetBuffers_Click));
        btClear.onClick.AddListener(btClear_Click);
    }

    private void Start()
    {
        Dispatch(DoStart);
    }

    private void DoStart()
    {
        Manager = new wclBluetoothManager();
        Server = new wclRfCommServer();

        Manager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(Manager_OnNumericComparison);
        Manager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(Manager_OnPasskeyNotification);
        Manager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(Manager_OnPasskeyRequest);
        Manager.OnPinRequest += new wclBluetoothPinRequestEvent(Manager_OnPinRequest);

        Server.OnClosed += new wclClientConnectionDisconnectEvent(Server_OnClosed);
        Server.OnConnect += new wclRfCommServerConnectEvent(Server_OnConnect);
        Server.OnData += new wclRfCommServerDataEvent(Server_OnData);
        Server.OnDisconnect += new wclRfCommServerDisconnectEvent(Server_OnDisconnect);
        Server.OnListen += new EventHandler(Server_OnListen);

        // In real application you should always analize the result code.
        // In this demo we assume that all is always OK.
        Manager.Open();

        cbAuthentication.isOn = Server.Authentication;
        cbEncryption.isOn = Server.Encryption;
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
        if (Server != null) { try { Server.Close(); } catch { } Server = null; }
        if (Manager != null) { try { Manager.Close(); } catch { } Manager = null; }
    }

    // ==========================================================
    //  Dispatcher / helpers
    // ==========================================================
    private void Dispatch(Action a)
    {
        lock (_pendingActions) { _pendingActions.Enqueue(a); }
    }

    private void ShowMessageBox(string msg)
    {
        Dispatch(() => MessageBox.Show(msg));
    }

    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio Radio;
        Int32 Res = Manager.GetClassicRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            ShowMessageBox("Get working radio failed: 0x" + Res.ToString("X8"));
            return null;
        }
        return Radio;
    }

    // ==========================================================
    //  Server events (capture strings sync, dispatch UI updates)
    // ==========================================================
    private void Server_OnListen(object sender, EventArgs e)
    {
        wclRfCommServer srv = (wclRfCommServer)sender;
        string radioName = srv.Radio.ApiName;
        int channel = srv.AssignedChannel;

        Dispatch(() =>
        {
            lbEvents.Items.Add("Server listening on radio " +
                radioName + " on channel: " + channel.ToString());
        });
    }

    private void Server_OnDisconnect(object Sender, wclRfCommServerClientConnection Client, int Reason)
    {
        wclRfCommServerConnection srv = (wclRfCommServerConnection)Client.Server;
        string radioName = srv.Radio.ApiName;
        Int64 address = Client.Address;
        int reason = Reason;

        Dispatch(() =>
        {
            lbEvents.Items.Add("Client disconnected: " + address.ToString("X12") +
                " on radio " + radioName + " reason: 0x" + reason.ToString("X8"));

            foreach (ListViewItem Item in lvClients.Items)
            {
                if (Item.Tag == Client)
                {
                    lvClients.Items.Remove(Item);
                    break;
                }
            }
        });
    }

    private void Server_OnData(object Sender, wclRfCommServerClientConnection Client, byte[] Data)
    {
        if (Data != null && Data.Length > 0)
        {
            String Str = Encoding.ASCII.GetString(Data);
            wclRfCommServerConnection srv = (wclRfCommServerConnection)Client.Server;
            string radioName = srv.Radio.ApiName;
            Int64 address = Client.Address;
            int len = Data.Length;

            Dispatch(() =>
            {
                lbEvents.Items.Add("Received " + len.ToString() + " bytes: " + Str + " from " +
                    address.ToString("X12") + " on radio " + radioName);
            });
        }
    }

    private void Server_OnConnect(object Sender, wclRfCommServerClientConnection Client, int Error)
    {
        wclRfCommServerConnection srv = (wclRfCommServerConnection)Client.Server;
        string radioName = srv.Radio.ApiName;
        Int64 address = Client.Address;
        int err = Error;

        Dispatch(() =>
        {
            lbEvents.Items.Add("Client connected: " + address.ToString("X12") +
                " on Radio " + radioName + " error: 0x" + err.ToString("X8"));

            ListViewItem Item = lvClients.Items.Add(address.ToString("X12"));
            Item.SubItems.Add(GetDeviceName(address));
            Item.Tag = Client;
        });
    }

    private void Server_OnClosed(object Sender, int Reason)
    {
        wclRfCommServer srv = (wclRfCommServer)Sender;
        string radioName = srv.Radio.ApiName;
        int reason = Reason;

        Dispatch(() =>
        {
            lbEvents.Items.Add("Server closed on radio " +
                radioName + "with reason: 0x" + reason.ToString("X8"));

            lvClients.Items.Clear();

            edReadBuffer.text = "";
            edWriteBuffer.text = "";
        });
    }

    // ==========================================================
    //  Manager events
    // ==========================================================
    private void Manager_OnPinRequest(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
    {
        // Use '0000' as PIN.
        Pin = "0000";
        string pin = Pin;
        Dispatch(() => lbEvents.Items.Add("Pin request: " + pin));
    }

    private void Manager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
    {
        // Use 123456 as passkey.
        Passkey = 123456;
        uint pk = Passkey;
        Dispatch(() => lbEvents.Items.Add("Passkey request: " + pk.ToString()));
    }

    private void Manager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, long Address, uint Passkey)
    {
        Dispatch(() => lbEvents.Items.Add("Passkey notification: " + Passkey.ToString()));
    }

    private void Manager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
    {
        // Accept any pairing.
        Confirm = true;
        uint n = Number;
        Dispatch(() => lbEvents.Items.Add("Numeric comparison: " + n.ToString()));
    }

    // ==========================================================
    //  Button handlers (run deferred in Update)
    // ==========================================================
    private void btClear_Click()
    {
        lbEvents.Items.Clear();
    }

    private void btListen_Click()
    {
        wclBluetoothRadio Radio = GetRadio();
        if (Radio != null)
        {
            // Switch radio in discoverable and connectable mode.
            // Ignore any errors. In real application you need to check the result.
            Radio.SetDiscoverable(true);
            Radio.SetConnectable(true);

            Server.Authentication = cbAuthentication.isOn;
            Server.Encryption = cbEncryption.isOn;

            Server.Service = wclUUIDs.SerialPortServiceClass_UUID;
            Server.ServiceName = edServiceName.text;

            Int32 Res = Server.Listen(Radio);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btClose_Click()
    {
        Int32 Res = Server.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btDisconnect_Click()
    {
        if (lvClients.SelectedItems.Count == 0)
            MessageBox.Show("Select client", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            Int32 Res = ((wclRfCommServerClientConnection)lvClients.SelectedItems[0].Tag).Disconnect();
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btSend_Click()
    {
        if (lvClients.SelectedItems.Count == 0)
            MessageBox.Show("Select client", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            Byte[] Ansi = Encoding.ASCII.GetBytes(edText.text);
            UInt32 Sent = 0;
            Int32 Res = ((wclRfCommServerClientConnection)lvClients.SelectedItems[0].Tag).Write(Ansi, out Sent);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Error: 0x" + Res.ToString("X8"));
            else
                MessageBox.Show("Sent: " + Sent.ToString() + " from " + Ansi.Length.ToString());
        }
    }

    private void btGetBuffers_Click()
    {
        if (lvClients.SelectedItems.Count == 0)
            MessageBox.Show("Select client", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            wclRfCommServerClientConnection Client = (wclRfCommServerClientConnection)lvClients.SelectedItems[0].Tag;

            // Get read buffer.
            UInt32 Size = 0;
            Int32 Res = Client.GetReadBufferSize(out Size);
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbEvents.Items.Add("Get read buffer error: 0x" + Res.ToString("X8"));
            else
                edReadBuffer.text = Size.ToString();

            // Get write buffer.
            Size = 0;
            Res = Client.GetWriteBufferSize(out Size);
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbEvents.Items.Add("Get write buffer error: 0x" + Res.ToString("X8"));
            else
                edWriteBuffer.text = Size.ToString();
        }
    }

    private void btSetBuffers_Click()
    {
        if (lvClients.SelectedItems.Count == 0)
            MessageBox.Show("Select client", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            wclRfCommServerClientConnection Client = (wclRfCommServerClientConnection)lvClients.SelectedItems[0].Tag;

            // Set read buffer.
            UInt32 Size = Convert.ToUInt32(edReadBuffer.text);
            Int32 Res = Client.SetReadBufferSize(Size);
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbEvents.Items.Add("Set read buffer error: 0x" + Res.ToString("X8"));

            // Set write buffer.
            Size = Convert.ToUInt32(edWriteBuffer.text);
            Res = Client.SetWriteBufferSize(Size);
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbEvents.Items.Add("Set write buffer error: 0x" + Res.ToString("X8"));
        }
    }

    // ==========================================================
    //  Helpers
    // ==========================================================
    private String GetDeviceName(Int64 Address)
    {
        wclBluetoothRadio Radio = GetRadio();
        if (Radio != null)
        {
            String DevName;
            Int32 Res = Radio.GetRemoteName(Address, out DevName);
            if (Res == wclErrors.WCL_E_SUCCESS)
                return DevName;

            return "Error: 0x" + Res.ToString("X8");
        }

        return "";
    }
}