using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclCommunication;
using wclBluetooth;

// ==========================================================
//  WinForms-style dialog enums + MessageBox (native, blocking)
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
//  WinForms-style OpenFileDialog (native, blocking; any thread)
// ==========================================================
public class OpenFileDialog
{
    public string FileName = "";
    public string Title    = "";
    public string Filter   = "All files (*.*)|*.*";

    public DialogResult ShowDialog()
    {
        string path = Win32OpenFileDialog.Show(FileName, Title, Filter);
        if (string.IsNullOrEmpty(path)) return DialogResult.Cancel;
        FileName = path;
        return DialogResult.OK;
    }

    private static class Win32OpenFileDialog
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public IntPtr lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetOpenFileNameW(ref OpenFileName ofn);

        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR   = 0x00000008;

        public static string Show(string initial, string title, string filter)
        {
            const int MAX_PATH = 2048;
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(typeof(OpenFileName));
            ofn.lpstrFile = Marshal.AllocHGlobal(MAX_PATH * 2);
            for (int i = 0; i < MAX_PATH * 2; i++) Marshal.WriteByte(ofn.lpstrFile, i, 0);
            ofn.nMaxFile = MAX_PATH;
            ofn.lpstrFilter     = (filter ?? "").Replace("|", "\0") + "\0\0";
            ofn.lpstrTitle      = title ?? "";
            ofn.lpstrInitialDir = initial ?? "";
            ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

            try
            {
                if (GetOpenFileNameW(ref ofn))
                    return Marshal.PtrToStringUni(ofn.lpstrFile);
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(ofn.lpstrFile);
            }
        }
    }
}

// ==========================================================
//  WinForms-style SaveFileDialog (native, blocking; any thread)
// ==========================================================
public class SaveFileDialog
{
    public string FileName = "";
    public string Title    = "";
    public string Filter   = "All files (*.*)|*.*";

    public DialogResult ShowDialog()
    {
        string path = Win32SaveFileDialog.Show(FileName, Title, Filter);
        if (string.IsNullOrEmpty(path)) return DialogResult.Cancel;
        FileName = path;
        return DialogResult.OK;
    }

    private static class Win32SaveFileDialog
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public IntPtr lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetSaveFileNameW(ref OpenFileName ofn);

        private const int OFN_OVERWRITEPROMPT = 0x00000002;
        private const int OFN_PATHMUSTEXIST   = 0x00000800;
        private const int OFN_NOCHANGEDIR     = 0x00000008;

        public static string Show(string initialName, string title, string filter)
        {
            const int MAX_PATH = 2048;
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(typeof(OpenFileName));
            ofn.lpstrFile = Marshal.AllocHGlobal(MAX_PATH * 2);

            for (int i = 0; i < MAX_PATH * 2; i++) Marshal.WriteByte(ofn.lpstrFile, i, 0);

            if (!string.IsNullOrEmpty(initialName))
            {
                char[] chars = (initialName + "\0").ToCharArray();
                byte[] bytes = Encoding.Unicode.GetBytes(chars);
                if (bytes.Length > MAX_PATH * 2) bytes = new byte[0];
                Marshal.Copy(bytes, 0, ofn.lpstrFile, bytes.Length);
            }

            ofn.nMaxFile = MAX_PATH;
            ofn.lpstrFilter     = (filter ?? "").Replace("|", "\0") + "\0\0";
            ofn.lpstrTitle      = title ?? "";
            ofn.Flags = OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

            try
            {
                if (GetSaveFileNameW(ref ofn))
                    return Marshal.PtrToStringUni(ofn.lpstrFile);
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(ofn.lpstrFile);
            }
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
public class OppServerController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btListen;
    public Button btClose;
    public Button btDisconnect;
    public Button btClear;

    [Header("ListView")]
    public RectTransform lvClientsContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbLogText;

    private ListView lvClients;
    private ListBox  lbLog;

    private wclBluetoothManager wclBluetoothManager;
    private wclRfCommServer     wclRfCommServer;
    private String FFileName;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    private OpenFileDialog openFileDialog;
    private SaveFileDialog saveFileDialog;

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvClients = new ListView(lvClientsContent, new float[] { 250 });
        lbLog     = new ListBox(lbLogText);
        openFileDialog = new OpenFileDialog();
        saveFileDialog = new SaveFileDialog();

        btListen.onClick.AddListener(() => Dispatch(btListen_Click));
        btClose.onClick.AddListener(() => Dispatch(btClose_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btClear.onClick.AddListener(btClear_Click);
    }

    private void Start()
    {
        wclBluetoothManager = new wclBluetoothManager();

        wclRfCommServer = new wclRfCommServer();
        wclRfCommServer.Authentication = false;
        wclRfCommServer.Service = wclUUIDs.OBEXObjectPushServiceClass_UUID;
        wclRfCommServer.ServiceName = "WCL OPP Server";
        wclRfCommServer.OnListen += new EventHandler(wclRfCommServer_OnListen);
        wclRfCommServer.OnClosed += new wclClientConnectionDisconnectEvent(wclRfCommServer_OnClosed);
        wclRfCommServer.OnDisconnect += new wclRfCommServerDisconnectEvent(wclRfCommServer_OnDisconnect);
        wclRfCommServer.OnDestroyProcessor += new wclServerConnectionProcessorEvent(wclRfCommServer_OnDestroyProcessor);
        wclRfCommServer.OnConnect += new wclRfCommServerConnectEvent(wclRfCommServer_OnConnect);
        wclRfCommServer.OnCreateProcessor += new wclServerConnectionProcessorEvent(wclRfCommServer_OnCreateProcessor);
        wclRfCommServer.GetSdpAttributes += new wclBluetoothGetSdpAttributesEvent(wclRfCommServer_GetSdpAttributes);
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
        if (wclRfCommServer != null) { try { wclRfCommServer.Close(); } catch { } }
        if (wclBluetoothManager != null) { try { wclBluetoothManager.Close(); } catch { } }
        wclRfCommServer = null;
        wclBluetoothManager = null;
    }

    // ==========================================================
    //  Dispatcher / helpers
    // ==========================================================
    private void Dispatch(Action a)
    {
        lock (_pendingActions) { _pendingActions.Enqueue(a); }
    }

    private void Trace(String Msg)
    {
        Dispatch(() => lbLog.Items.Add(Msg));
    }

    private void ShowMessageBox(string msg)
    {
        Dispatch(() => MessageBox.Show(msg));
    }

    // ==========================================================
    //  Server SDP attributes (sync out-only)
    // ==========================================================
    private void wclRfCommServer_GetSdpAttributes(object Sender, out ushort[] Protocols,
        out wclBluetoothSdpProfile[] Profiles, out byte[] Formats, out uint Cod)
    {
        Protocols = new ushort[1];
        Protocols[0] = wclUUIDs.OBEX_PROTOCOL_UUID16;
        Profiles = new wclBluetoothSdpProfile[1];
        Profiles[0].Uuid = wclUUIDs.OBEXObjectPushServiceClass_UUID16;
        Profiles[0].Version = 0x0100;
        Formats = new byte[1];
        Formats[0] = 0xFF; // All formats.
        Cod = wclCODs.COD_SERVICE_OBJECT_XFER;
    }

    // ==========================================================
    //  Server events
    // ==========================================================
    private void wclRfCommServer_OnCreateProcessor(object Sender, wclServerClientDataConnection Connection)
    {
        wclObexOppServer Proc = new wclObexOppServer(Connection);
        Proc.OnConnect += new wclObexOppServerConnectEvent(Proc_OnConnect);
        Proc.OnDisconnected += new wclObexServerClientDisconnectedEvent(Proc_OnDisconnected);
        Proc.OnPutBegin += new wclObexServerClientPutBeginEvent(Proc_OnPutBegin);
        Proc.OnPutCompleted += new wclObexServerClientPutCompletedEvent(Proc_OnPutCompleted);
        Proc.OnPutProgress += new wclObexServerClientOperationProgressEvent(Proc_OnPutProgress);
        Proc.OnGetRequest += new wclObexOppServerGetRequestEvent(Proc_OnGetRequest);
        Proc.OnProgress += new wclObexOperationProgressEvent(Proc_OnProgress);
        Proc.OnGetCompleted += new wclObexServerClientGetCompletedEvent(Proc_OnGetCompleted);
    }

    private void wclRfCommServer_OnDestroyProcessor(object Sender, wclServerClientDataConnection Connection)
    {
        Trace("Data processor for client " + ((wclRfCommServerClientConnection)Connection).Address.ToString("X12") + " is destroing");
        Connection.Processor.Dispose();
    }

    private void wclRfCommServer_OnConnect(object Sender, wclRfCommServerClientConnection Client, int Error)
    {
        if (Error == wclErrors.WCL_E_SUCCESS)
            Trace("Client " + Client.Address.ToString("X12") + " connected");
        else
            Trace("Client " + Client.Address.ToString("X12") + " connect failed: 0x" + Error.ToString("X8"));
    }

    private void wclRfCommServer_OnDisconnect(object Sender, wclRfCommServerClientConnection Client, int Reason)
    {
        Trace("Client " + Client.Address.ToString("X12") + " disconnected. Reason: 0x" + Reason.ToString("X8"));
    }

    private void wclRfCommServer_OnClosed(object Sender, int Reason)
    {
        Trace("Server closed. Reason:: 0x" + Reason.ToString("X8"));
    }

    private void wclRfCommServer_OnListen(object sender, EventArgs e)
    {
        Trace("Server listening");
    }

    // ==========================================================
    //  OBEX OPP server events
    // ==========================================================
    private void Proc_OnGetCompleted(object Sender, int Error, Stream Stream)
    {
        Trace("Get operation completed with result: 0x" + Error.ToString("X8"));
        if (Stream != null)
            Stream.Close();
    }

    private void Proc_OnProgress(object Sender, uint Length, uint Position)
    {
        Trace("Sending file: " + Position.ToString() + " from " + Length.ToString());
    }

    private void Proc_OnGetRequest(object Sender, string aType,
        out wclObexServerOperationResult Result, out Stream Stream)
    {
        // Synchronous: dialog and FileStream open from this thread.
        if (aType == "text/x-vCard" || aType == "x-obex/capability")
        {
            Trace("Requested file type: " + aType);

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                Result = wclObexServerOperationResult.orForbidden;
                Stream = null;
            }
            else
            {
                Stream = new FileStream(openFileDialog.FileName, FileMode.Open);
                Result = wclObexServerOperationResult.orSuccess;
            }
        }
        else
        {
            Result = wclObexServerOperationResult.orUnsupportedMedia;
            Stream = null;
        }
    }

    private void Proc_OnPutProgress(object Sender, uint Position, uint Length, out bool Continue)
    {
        Trace("Sending file: " + Position.ToString() + " from " + Length.ToString());
        // Do not forget to set Continue flag!
        Continue = true;
    }

    private void Proc_OnPutCompleted(object Sender, int Error, Stream Stream, out bool Accept)
    {
        // Synchronous: MessageBox + SaveFileDialog + file I/O from this thread.
        wclObexOppServer Proc = (wclObexOppServer)Sender;
        Int64 Address = ((wclRfCommServerClientConnection)Proc.Connection).Address;
        if (Error == wclErrors.WCL_E_SUCCESS)
        {
            String Str = "Client " + Address.ToString("X12") + " completed sent file. Accept?";
            Accept = MessageBox.Show(Str, "Accept?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

            if (Accept)
            {
                saveFileDialog.FileName = FFileName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Stream.Seek(0, SeekOrigin.Begin);
                    FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.CreateNew, FileAccess.Write);
                    Byte[] Bytes = new Byte[Stream.Length];
                    Stream.Read(Bytes, 0, (int)Stream.Length);
                    fs.Write(Bytes, 0, Bytes.Length);
                    fs.Close();
                }
            }
        }
        else
        {
            Trace("Send file error: 0x" + Error.ToString("X8"));
            Accept = false; // does not matter.
        }
    }

    private void Proc_OnPutBegin(object Sender, string Name, string Description, string Mime,
        uint Length, out bool Accept)
    {
        wclObexOppServer Proc = (wclObexOppServer)Sender;
        Int64 Address = ((wclRfCommServerClientConnection)Proc.Connection).Address;
        String Str = "Client " + Address.ToString("X12") + " send file " + Name +
            ". Accept?";
        Accept = MessageBox.Show(Str, "Accept?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        if (Accept)
            FFileName = Name;
    }

    private void Proc_OnDisconnected(object Sender, int Reason, string Description)
    {
        wclObexOppServer Proc = (wclObexOppServer)Sender;
        Int64 Address = ((wclRfCommServerClientConnection)Proc.Connection).Address;

        Trace("Client " + Address.ToString("X12") + " closed OBEX session with reason: 0x" + Reason.ToString("X8") +
            ". Description: " + Description);

        Dispatch(() =>
        {
            for (Int32 i = 0; i < lvClients.Items.Count; i++)
            {
                if (lvClients.Items[i].Tag == Sender)
                {
                    lvClients.Items.RemoveAt(i);
                    break;
                }
            }
        });
    }

    private void Proc_OnConnect(object Sender, string Description)
    {
        // No out parameters — safe to Dispatch whole callback. MessageBox stays sync (blocking).
        wclObexOppServer Proc = (wclObexOppServer)Sender;
        Int64 Address = ((wclRfCommServerClientConnection)Proc.Connection).Address;
        Boolean Accept = MessageBox.Show("Client " + Address.ToString("X12") +
            " requsts for connection: " + Description + ". Accept?", "Accept?",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        Int32 Res;
        if (Accept)
        {
            Res = Proc.Accept("Accepted by user");
            if (Res == wclErrors.WCL_E_SUCCESS)
            {
                Dispatch(() =>
                {
                    ListViewItem Item = lvClients.Items.Add(Address.ToString("X12"));
                    Item.Tag = Sender;
                });
                Trace("OBEX session opened for client: " + Address.ToString("X12"));
            }
        }
        else
            Res = Proc.Reject("Rejected by user");

        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            ShowMessageBox("Operation failed: 0x" + Res.ToString("X8"));
            // Disconnect client!
            Proc.Connection.Disconnect();
        }
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClear_Click()
    {
        lbLog.Items.Clear();
    }

    private void btListen_Click()
    {
        Int32 Res = wclBluetoothManager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Bluetooth manager open failed: 0x" + Res.ToString("X8"));
        else
        {
            wclBluetoothRadio Radio;
            Res = wclBluetoothManager.GetClassicRadio(out Radio);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowMessageBox("Get working radio failed: 0x" + Res.ToString("X8"));
            else
            {
                // Switch radio in discoverable and connectable mode.
                // Ignore any errors. In real application you need to check the result.
                Radio.SetDiscoverable(true);
                Radio.SetConnectable(true);

                Res = wclRfCommServer.Listen(Radio);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Failed to start listening: 0x" + Res.ToString("X8"));
            }

            if (Res != wclErrors.WCL_E_SUCCESS)
                // We must close Bluetooth Manager here!
                wclBluetoothManager.Close();
        }
    }

    private void btClose_Click()
    {
        Int32 Res = wclRfCommServer.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Close failed; 0x" + Res.ToString("X8"));
        else
            wclBluetoothManager.Close();
    }

    private void btDisconnect_Click()
    {
        if (lvClients.SelectedItems == null)
            ShowMessageBox("Select client");
        else
        {
            wclObexOppServer Proc = (wclObexOppServer)lvClients.SelectedItems[0].Tag;
            Int32 Res = Proc.Connection.Disconnect();
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowMessageBox("Disconnect failed: 0x" + Res.ToString("X8"));
        }
    }
}