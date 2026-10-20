using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclCommunication;
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
//  WinForms-style ListView
// ==========================================================
public class ListView
{
    public RectTransform Content;
    public float[]       ColumnWidths;

    private const float RowHeight = 18f;
    private const float DoubleClickInterval = 0.4f;

    internal readonly List<ListViewItem> _items    = new List<ListViewItem>();
    internal readonly List<ListViewItem> _selected = new List<ListViewItem>();

    private static readonly Color SelectedColor = new Color(0.70f, 0.85f, 0.98f, 1f);
    private static readonly Color NormalColor   = Color.white;

    private ListViewItem _lastClickedItem;
    private float        _lastClickTime = -1f;

    public event Action DoubleClick;

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
        item.Values.Add(text ?? string.Empty);
        if (Content != null && ColumnWidths != null && ColumnWidths.Length > 0)
            item.CreateRow(_items.Count);
        _items.Add(item);
        return item;
    }

    internal void ClearItems()
    {
        foreach (var it in _items)
            if (it.RowGO != null) UnityEngine.Object.Destroy(it.RowGO);
        _items.Clear();
        _selected.Clear();
        _lastClickedItem = null;
    }

    internal void OnRowClicked(ListViewItem item)
    {
        if (_lastClickedItem == item && Time.unscaledTime - _lastClickTime < DoubleClickInterval)
        {
            _lastClickedItem = null;
            _lastClickTime = -1f;
            DoubleClick?.Invoke();
        }
        else
        {
            _lastClickedItem = item;
            _lastClickTime = Time.unscaledTime;
        }

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
//  WinForms-style ProgressBar
// ==========================================================
public class ProgressBar
{
    private readonly Image _fill;
    private int _value;
    private int _maximum;

    public ProgressBar(Image fill) { _fill = fill; UpdateFill(); }

    public int Value
    {
        get => _value;
        set { _value = value; UpdateFill(); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = value; UpdateFill(); }
    }

    private void UpdateFill()
    {
        if (_fill == null) return;
        float pct = _maximum <= 0 ? 0f : Mathf.Clamp01((float)_value / _maximum);
        _fill.fillAmount = pct;
    }
}

// ==========================================================
//  WinForms-style OpenFileDialog
// ==========================================================
public class OpenFileDialog
{
    public string FileName = "";
    public string Title    = "";
    public string Filter   = "All files (*.*)|*.*";

    public DialogResult ShowDialog()
    {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            string.IsNullOrEmpty(Title) ? "Select file" : Title, "", "");
#else
        string path = Win32OpenFileDialog.Show(FileName, Title, Filter);
#endif
        if (string.IsNullOrEmpty(path)) return DialogResult.Cancel;
        FileName = path;
        return DialogResult.OK;
    }

#if !UNITY_EDITOR
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
#endif
}

// ==========================================================
//  Controller (fmMain)
// ==========================================================
public class FtpClientController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btDiscover;
    public Button btConnect;
    public Button btDisconnect;
    public Button btOpenSession;
    public Button btCloseSession;
    public Button btSelectFile;
    public Button btSendFile;
    public Button btMkDir;
    public Button btDir;
    public Button btDelete;
    public Button btRoot;
    public Button btAbort;
    public Button btClear;

    [Header("Inputs")]
    public TMP_InputField edFileName;
    public TMP_InputField edNewDirName;

    [Header("ListView Contents")]
    public RectTransform lvDevicesContent;
    public RectTransform lvFilesContent;

    [Header("ListBox")]
    public TextMeshProUGUI lbLogText;

    [Header("ProgressBar")]
    public Image ProgressBarFill;

    private ListView        lvDevices;
    private ListView        lvFiles;
    private ListBox         lbLog;
    private ProgressBar     ProgressBar;
    private OpenFileDialog  openFileDialog;

    private wclBluetoothManager wclBluetoothManager;
    private wclRfCommClient     wclRfCommClient;
    private wclObexFtpClient    FFtp;

    // Deferred actions queue: run native API / MessageBox from Update, not from button click.
    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        btDiscover.onClick.AddListener(btDiscover_Click);
        btConnect.onClick.AddListener(btConnect_Click);
        btDisconnect.onClick.AddListener(btDisconnect_Click);
        btOpenSession.onClick.AddListener(btOpenSession_Click);
        btCloseSession.onClick.AddListener(btCloseSession_Click);
        btSelectFile.onClick.AddListener(btSelectFile_Click);
        btSendFile.onClick.AddListener(btSendFile_Click);
        btMkDir.onClick.AddListener(btMkDir_Click);
        btDir.onClick.AddListener(btDir_Click);
        btDelete.onClick.AddListener(btDelete_Click);
        btRoot.onClick.AddListener(btRoot_Click);
        btAbort.onClick.AddListener(btAbort_Click);
        btClear.onClick.AddListener(btClear_Click);

        lvDevices = new ListView(lvDevicesContent, new float[] { 150, 150 });
        lvFiles   = new ListView(lvFilesContent,   new float[] { 150, 80, 100, 80, 80, 80, 80, 100 });
        lbLog     = new ListBox(lbLogText);
        ProgressBar = new ProgressBar(ProgressBarFill);
        openFileDialog = new OpenFileDialog();

        lvFiles.DoubleClick += lvFiles_DoubleClick;
    }

    private void Start()
    {
        wclBluetoothManager = new wclBluetoothManager();
        wclBluetoothManager.AfterOpen += new EventHandler(wclBluetoothManager_AfterOpen);
        wclBluetoothManager.OnAuthenticationCompleted += new wclBluetoothDeviceResultEvent(wclBluetoothManager_OnAuthenticationCompleted);
        wclBluetoothManager.OnDiscoveringCompleted += new wclBluetoothResultEvent(wclBluetoothManager_OnDiscoveringCompleted);
        wclBluetoothManager.OnDiscoveringStarted += new wclBluetoothEvent(wclBluetoothManager_OnDiscoveringStarted);
        wclBluetoothManager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(wclBluetoothManager_OnNumericComparison);
        wclBluetoothManager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(wclBluetoothManager_OnPasskeyNotification);
        wclBluetoothManager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(wclBluetoothManager_OnPasskeyRequest);
        wclBluetoothManager.OnPinRequest += new wclBluetoothPinRequestEvent(wclBluetoothManager_OnPinRequest);
        wclBluetoothManager.OnDeviceFound += new wclBluetoothDeviceEvent(wclBluetoothManager_OnDeviceFound);

        wclRfCommClient = new wclRfCommClient();
        wclRfCommClient.OnDisconnect += new wclClientConnectionDisconnectEvent(wclRfCommClient_OnDisconnect);
        wclRfCommClient.OnConnect += new wclClientConnectionConnectEvent(wclRfCommClient_OnConnect);
        wclRfCommClient.OnDestroyProcessor += new wclClientConnectionProcessorEvent(wclRfCommClient_OnDestroyProcessor);
        wclRfCommClient.OnCreateProcessor += new wclClientConnectionProcessorEvent(wclRfCommClient_OnCreateProcessor);

        // We use ObjectPush Profile so must set the profile's UUID.
        wclRfCommClient.Service = wclUUIDs.OBEXFileTransferServiceClass_UUID;

        FFtp = null;

        // Try to open Bluetooth Manager.
        Int32 Res = wclBluetoothManager.Open();
        if (Res != wclErrors.WCL_E_SUCCESS)
            lbLog.Items.Add("Bluetooth Manager Open failed: 0x" + Res.ToString("X8"));
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
        if (wclRfCommClient != null) wclRfCommClient.Disconnect();
        if (wclBluetoothManager != null) wclBluetoothManager.Close();
    }

    // ==========================================================
    //  Deferral helpers
    // ==========================================================
    private void Dispatch(Action a)
    {
        lock (_pendingActions) { _pendingActions.Enqueue(a); }
    }

    private void ShowMessageBox(string msg)
    {
        Dispatch(() => MessageBox.Show(msg));
    }

    // ==========================================================
    //  Helpers
    // ==========================================================
    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio Radio;
        Int32 Res = wclBluetoothManager.GetClassicRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            ShowMessageBox("Get working radio failed: 0x" + Res.ToString("X8"));
            return null;
        }
        return Radio;
    }

    // ==========================================================
    //  RfComm client events
    // ==========================================================
    private void wclRfCommClient_OnCreateProcessor(object Sender, wclClientDataConnection Connection)
    {
        FFtp = new wclObexFtpClient(Connection);
        FFtp.OnConnect += new wclObexOperationResultEvent(FFtp_OnConnect);
        FFtp.OnDisconnect += new wclObexOperationResultEvent(FFtp_OnDisconnect);
        FFtp.OnGetComplete += new wclObexObjectOperationResultEvent(FFtp_OnGetComplete);
        FFtp.OnPutComplete += new wclObexObjectOperationResultEvent(FFtp_OnPutComplete);
        FFtp.OnProgress += new wclObexOperationProgressEvent(FFtp_OnProgress);
        FFtp.OnDirComplete += new wclObexFtpDirCompleteEvent(FFtp_OnDirComplete);
        FFtp.OnChangeDirComplete += new wclObexOperationResultEvent(FFtp_OnChangeDirComplete);
        FFtp.OnDeleteComplete += new wclObexOperationResultEvent(FFtp_OnDeleteComplete);
        FFtp.OnMakeDirComplete += new wclObexOperationResultEvent(FFtp_OnMakeDirComplete);
    }

    private void wclRfCommClient_OnDestroyProcessor(object Sender, wclClientDataConnection Connection)
    {
        if (FFtp != null)
        {
            if (Connection.Processor == FFtp)
            {
                FFtp.Dispose();
                FFtp = null;
            }
        }
    }

    private void wclRfCommClient_OnConnect(object Sender, int Error)
    {
        Dispatch(() => lbLog.Items.Add("Connect. Operation result: 0x" + Error.ToString("X8")));
    }

    private void wclRfCommClient_OnDisconnect(object Sender, int Reason)
    {
        Dispatch(() => lbLog.Items.Add("Disconnected with reason: 0x" + Reason.ToString("X8")));
    }

    // ==========================================================
    //  OBEX FTP events
    // ==========================================================
    private void FFtp_OnMakeDirComplete(object Sender, int Error, string Description)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("Make dir completed with result: 0x" + Error.ToString("X8") + ". Description: " + Description);

            ProgressBar.Value = 0;
            ProgressBar.Maximum = 0;

            lvFiles.Items.Clear();
            FFtp.Dir();
        });
    }

    private void FFtp_OnDeleteComplete(object Sender, int Error, string Description)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("Delete completed with result: 0x" + Error.ToString("X8") + ". Description: " + Description);

            ProgressBar.Value = 0;
            ProgressBar.Maximum = 0;

            lvFiles.Items.Clear();
            FFtp.Dir();
        });
    }

    private void FFtp_OnChangeDirComplete(object Sender, int Error, string Description)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("Change dir completed with result: 0x" + Error.ToString("X8") + ". Description: " + Description);

            ProgressBar.Value = 0;
            ProgressBar.Maximum = 0;

            lvFiles.Items.Clear();
            FFtp.Dir();
        });
    }

    private void FFtp_OnDirComplete(object Sender, int Error, string Description, wclObexFileObject[] Dirs)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("Dir completed with result: 0x" + Error.ToString("X8") + ". Description: " + Description);

            ProgressBar.Value = 0;
            ProgressBar.Maximum = 0;

            if (Dirs != null)
            {
                foreach (wclObexFileObject Obj in Dirs)
                {
                    ListViewItem Item = lvFiles.Items.Add(Obj.Name);
                    if (Obj.IsDirectory)
                        Item.SubItems.Add("Folder");
                    else
                        Item.SubItems.Add("File");
                    Item.SubItems.Add(Obj.Size.ToString());

                    String Perm = "";
                    if ((Obj.Permissions & wclObexFilePermission.opRead) != 0)
                        Perm = Perm + "R";
                    if ((Obj.Permissions & wclObexFilePermission.opWrite) != 0)
                        Perm = Perm + "W";
                    if ((Obj.Permissions & wclObexFilePermission.opDelete) != 0)
                        Perm = Perm + "D";
                    Item.SubItems.Add(Perm);
                    if (!Obj.Modified.Equals(DateTime.MinValue))
                        Item.SubItems.Add(Obj.Modified.ToString());
                    else
                        Item.SubItems.Add("");
                    if (!Obj.Created.Equals(DateTime.MinValue))
                        Item.SubItems.Add(Obj.Created.ToString());
                    else
                        Item.SubItems.Add("");
                    if (!Obj.Accessed.Equals(DateTime.MinValue))
                        Item.SubItems.Add(Obj.Accessed.ToString());
                    else
                        Item.SubItems.Add("");

                    Item.SubItems.Add(Obj.Description);
                }
            }
        });
    }

    private void FFtp_OnGetComplete(object Sender, int Error, string Description, Stream Stream)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("File receiving completed with result: 0x" + Error.ToString("X8") + ". Description: " + Description);

            ProgressBar.Value = 0;
            ProgressBar.Maximum = 0;

            // We must destroy the stream object here.
            Stream.Close();
            Stream.Dispose();
        });
    }

    private void FFtp_OnProgress(object Sender, uint Size, uint Position)
    {
        Dispatch(() =>
        {
            ProgressBar.Maximum = (int)Size;
            ProgressBar.Value = (int)Position;
        });
    }

    private void FFtp_OnPutComplete(object Sender, int Error, string Description, Stream Stream)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("File sending completed with result: 0x" + Error.ToString("X8") + ". Description: " + Description);

            ProgressBar.Value = 0;
            ProgressBar.Maximum = 0;

            // We must destroy the stream object here.
            Stream.Close();
            Stream.Dispose();
        });
    }

    private void FFtp_OnDisconnect(object Sender, int Error, string Description)
    {
        Dispatch(() => lbLog.Items.Add("OBEX session closed with result: 0x" + Error.ToString("X8") + ". Description: " + Description));
    }

    private void FFtp_OnConnect(object Sender, int Error, string Description)
    {
        Dispatch(() => lbLog.Items.Add("OBEX session opened with result: 0x" + Error.ToString("X8") + ". Description: " + Description));
    }

    // ==========================================================
    //  BluetoothManager events
    // ==========================================================
    private void wclBluetoothManager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, long Address)
    {
        Dispatch(() =>
        {
            ListViewItem Item = lvDevices.Items.Add(Address.ToString("X12"));
            Item.SubItems.Add("");
        });
    }

    private void wclBluetoothManager_OnPinRequest(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
    {
        // out parameter MUST be set synchronously (on caller's thread).
        Pin = "0000";
        Dispatch(() => lbLog.Items.Add("PIN code request. PIN: 0000"));
    }

    private void wclBluetoothManager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
    {
        Passkey = 12345678;
        Dispatch(() => lbLog.Items.Add("Passkey request. Passkey: 12345678"));
    }

    private void wclBluetoothManager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, long Address, uint Passkey)
    {
        Dispatch(() => lbLog.Items.Add("Passkey notification. Passkey: " + Passkey.ToString()));
    }

    private void wclBluetoothManager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
    {
        Confirm = true;
        Dispatch(() => lbLog.Items.Add("Numeric comparison. Accept"));
    }

    private void wclBluetoothManager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("Discovering started");
            lvDevices.Items.Clear();
        });
    }

    private void wclBluetoothManager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, int Error)
    {
        Dispatch(() =>
        {
            lbLog.Items.Add("Discovering completed with result: 0x" + Error.ToString("X8"));

            for (Int32 i = 0; i < lvDevices.Items.Count; i++)
            {
                ListViewItem Item = lvDevices.Items[i];
                Int64 Addr = Convert.ToInt64(Item.Text, 16);
                String Name;
                Int32 Res = Radio.GetRemoteName(Addr, out Name);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Name = "Error: 0x" + Res.ToString("X8");
                Item.SubItems[1].Text = Name;
            }
        });
    }

    private void wclBluetoothManager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio, long Address, int Error)
    {
        Dispatch(() => lbLog.Items.Add("Authentication completed with result: 0x" + Error.ToString("X8")));
    }

    private void wclBluetoothManager_AfterOpen(object sender, EventArgs e)
    {
        Dispatch(() => lbLog.Items.Add("Bluetooth Manager has been opened"));
    }

    // ==========================================================
    //  Button handlers (all heavy work deferred to Update)
    // ==========================================================
    private void btClear_Click()
    {
        lbLog.Items.Clear();
    }

    private void btDiscover_Click()
    {
        Dispatch(DoDiscover);
    }

    private void DoDiscover()
    {
        Debug.Log("[FtpClient] Discover: enter");

        if (wclBluetoothManager == null) { Debug.LogError("[FtpClient] manager is null"); return; }
        Debug.Log("[FtpClient] Discover: Manager.Active = " + wclBluetoothManager.Active);
        if (!wclBluetoothManager.Active)
        {
            Debug.LogError("[FtpClient] Discover: manager is not active. Open was not successful.");
            ShowMessageBox("Bluetooth Manager is not active");
            return;
        }

        wclBluetoothRadio Radio = GetRadio();
        Debug.Log("[FtpClient] Discover: GetRadio => " + (Radio == null ? "null" : "ok"));
        if (Radio != null)
        {
            Debug.Log("[FtpClient] Discover: calling Radio.Discover...");
            Int32 Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
            Debug.Log("[FtpClient] Discover: Radio.Discover returned 0x" + Res.ToString("X8"));
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowMessageBox("Failed to start discovering: 0x" + Res.ToString("X8"));
        }
    }

    private void btDisconnect_Click()
    {
        Dispatch(() =>
        {
            Int32 Res = wclRfCommClient.Disconnect();
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowMessageBox("Disconnect failed: 0x" + Res.ToString("X8"));
        });
    }

    private void btCloseSession_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                Int32 Res = FFtp.Disconnect("");
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Close session failed: 0x" + Res.ToString("X8"));
            }
        });
    }

    private void btOpenSession_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                Int32 Res = FFtp.Connect();
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Open session failed: 0x" + Res.ToString("X8"));
            }
        });
    }

    private void btConnect_Click()
    {
        Dispatch(() =>
        {
            if (wclRfCommClient.State != wclClientState.csDisconnected)
                ShowMessageBox("Client is connected");
            else
            {
                if (lvDevices.SelectedItems.Count == 0)
                    ShowMessageBox("Select device");
                else
                {
                    wclBluetoothRadio Radio = GetRadio();
                    if (Radio != null)
                    {
                        Int64 Addr = Convert.ToInt64(lvDevices.SelectedItems[0].Text, 16);
                        wclRfCommClient.Address = Addr;
                        Int32 Res = wclRfCommClient.Connect(Radio);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                            ShowMessageBox("Failed to connect: 0x" + Res.ToString("X8"));
                    }
                }
            }
        });
    }

    private void btSelectFile_Click()
    {
        Dispatch(() =>
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                edFileName.text = openFileDialog.FileName;
        });
    }

    private void btAbort_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                Int32 Res = FFtp.Abort("");
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Abort failed: 0x" + Res.ToString("X8"));
            }
        });
    }

    private void btSendFile_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                if (edFileName.text == "")
                    ShowMessageBox("Select file");
                else
                {
                    FileStream Stream = new FileStream(edFileName.text, FileMode.Open);
                    Int32 Res = FFtp.Put(Path.GetFileName(edFileName.text), "", Stream);
                    if (Res != wclErrors.WCL_E_SUCCESS)
                    {
                        ShowMessageBox("Send file failed: 0x" + Res.ToString("X8"));
                        Stream.Close();
                        Stream.Dispose();
                    }
                }
            }
        });
    }

    private void btDir_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                Int32 Res = FFtp.Dir();
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Dir failed: 0x" + Res.ToString("X8"));
                else
                    lvFiles.Items.Clear();
            }
        });
    }

    private void lvFiles_DoubleClick()
    {
        Dispatch(() =>
        {
            if (lvFiles.SelectedItems.Count > 0)
            {
                if (FFtp == null)
                    ShowMessageBox("Not connected");
                else
                {
                    if (lvFiles.SelectedItems[0].SubItems[1].Text == "Folder")
                    {
                        Int32 Res = FFtp.ChangeDir(lvFiles.SelectedItems[0].Text);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                            ShowMessageBox("Change path failed 0x" + Res.ToString("X8"));
                    }
                    else
                    {
                        FileStream Stream = new FileStream(".\\" + lvFiles.SelectedItems[0].Text, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        Int32 Res = FFtp.Get(lvFiles.SelectedItems[0].Text, Stream);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                        {
                            ShowMessageBox("Get failed 0x" + Res.ToString("X8"));
                            Stream.Close();
                            Stream.Dispose();
                        }
                    }
                }
            }
        });
    }

    private void btRoot_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                Int32 Res = FFtp.ChangeDir("");
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Root failed 0x" + Res.ToString("X8"));
            }
        });
    }

    private void btDelete_Click()
    {
        Dispatch(() =>
        {
            if (lvFiles.SelectedItems.Count > 0)
            {
                if (FFtp == null)
                    ShowMessageBox("Not connected");
                else
                {
                    Int32 Res = FFtp.Delete(lvFiles.SelectedItems[0].Text);
                    if (Res != wclErrors.WCL_E_SUCCESS)
                        ShowMessageBox("Delete failed 0x" + Res.ToString("X8"));
                }
            }
        });
    }

    private void btMkDir_Click()
    {
        Dispatch(() =>
        {
            if (FFtp == null)
                ShowMessageBox("Not connected");
            else
            {
                Int32 Res = FFtp.MkDir(edNewDirName.text);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowMessageBox("Make sire failed 0x" + Res.ToString("X8"));
            }
        });
    }
}