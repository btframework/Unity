using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
//  WinForms-style OpenFileDialog / SaveFileDialog
// ==========================================================
public class OpenFileDialog
{
    public string FileName = "";
    public string Title    = "";
    public string Filter   = "XML file (*.xml)|*.xml";

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
            public int lStructSize; public IntPtr hwndOwner; public IntPtr hInstance;
            public string lpstrFilter; public string lpstrCustomFilter; public int nMaxCustFilter; public int nFilterIndex;
            public IntPtr lpstrFile; public int nMaxFile; public string lpstrFileTitle; public int nMaxFileTitle;
            public string lpstrInitialDir; public string lpstrTitle; public int Flags;
            public short nFileOffset; public short nFileExtension; public string lpstrDefExt;
            public IntPtr lCustData; public IntPtr lpfnHook; public string lpTemplateName;
            public IntPtr pvReserved; public int dwReserved; public int flagsEx;
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
            ofn.lpstrFilter = (filter ?? "").Replace("|", "\0") + "\0\0";
            ofn.lpstrTitle = title ?? "";
            ofn.lpstrInitialDir = initial ?? "";
            ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
            try
            {
                if (GetOpenFileNameW(ref ofn)) return Marshal.PtrToStringUni(ofn.lpstrFile);
                return null;
            }
            finally { Marshal.FreeHGlobal(ofn.lpstrFile); }
        }
    }
}

public class SaveFileDialog
{
    public string FileName = "";
    public string Title    = "";
    public string Filter   = "XML file (*.xml)|*.xml";

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
            public int lStructSize; public IntPtr hwndOwner; public IntPtr hInstance;
            public string lpstrFilter; public string lpstrCustomFilter; public int nMaxCustFilter; public int nFilterIndex;
            public IntPtr lpstrFile; public int nMaxFile; public string lpstrFileTitle; public int nMaxFileTitle;
            public string lpstrInitialDir; public string lpstrTitle; public int Flags;
            public short nFileOffset; public short nFileExtension; public string lpstrDefExt;
            public IntPtr lCustData; public IntPtr lpfnHook; public string lpTemplateName;
            public IntPtr pvReserved; public int dwReserved; public int flagsEx;
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
            ofn.lpstrFilter = (filter ?? "").Replace("|", "\0") + "\0\0";
            ofn.lpstrTitle = title ?? "";
            ofn.Flags = OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
            try
            {
                if (GetSaveFileNameW(ref ofn)) return Marshal.PtrToStringUni(ofn.lpstrFile);
                return null;
            }
            finally { Marshal.FreeHGlobal(ofn.lpstrFile); }
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
//  Controller
// ==========================================================
[Preserve]
public class WiFiClientController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btClientOpen;
    public Button btClientClose;
    public Button btClientEnumInterfaces;
    public Button btClientDisconnect;
    public Button btClientGetInterfaceState;
    public Button btTurnOn;
    public Button btTurnOff;
    public Button btCurrentIp;
    public Button btGetIpSettings;
    public Button btEnableDhcp;
    public Button btEnableStaticIp;
    public Button btGetDualStaState;
    public Button btEnableDualSta;
    public Button btDisableDualSta;
    public Button btEnumSecondaryInterfaces;
    public Button btClientEnumNetworks;
    public Button btClientScan;
    public Button btClientEnumBSS;
    public Button btClientEnumProfiles;
    public Button btClientDeleteProfile;
    public Button btClientRenameProfile;
    public Button btClientGetProfileXml;
    public Button btClientSetProfileXml;
    public Button btSetProfileXmlUserData;
    public Button btEditUi;
    public Button btClientConnect;
    public Button btClientGetShowDeniedNetworks;
    public Button btClientGetPowerSettings;
    public Button btClientGetAllowExplicitCreds;
    public Button btClientGetBlockPeriod;
    public Button btClientGetVirtualStationExtensibility;
    public Button btClientSetShowDeniedNetworks;
    public Button btClientGetOnlyUseGpProfiles;
    public Button btClientSetAllowExplicitCreds;
    public Button btClientSetBlockPeriod;
    public Button btClientSetVirtualStationExtensibility;

    [Header("Checkboxes")]
    public Toggle cbClientAdHocProfiles;
    public Toggle cbClientHiddenProfiles;
    public Toggle cbClientUseSsid;
    public Toggle cbClientDiscoverSecure;
    public Toggle cbUseBssMac;
    public Toggle cbPlainText;

    [Header("Dropdowns")]
    public TMP_Dropdown cbClientBssType;
    public TMP_Dropdown cbClientConnect;

    [Header("Inputs")]
    public TMP_InputField edClientScanSsid;
    public TMP_InputField edClientPasskey;

    [Header("ListView Contents")]
    public RectTransform lvClientInterfacesContent;
    public RectTransform lvClientNetworksContent;
    public RectTransform lvClientBssContent;
    public RectTransform lvClientProfilesContent;

    private ListView lvClientInterfaces;
    private ListView lvClientNetworks;
    private ListView lvClientBss;
    private ListView lvClientProfiles;

    private wclWiFiClient Client;
    private wclWiFiProfilesManager ProfilesManager;

    private OpenFileDialog openFileDialog;
    private SaveFileDialog saveFileDialog;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvClientInterfaces = new ListView(lvClientInterfacesContent, new float[] { 350, 350, 60 });
        lvClientNetworks   = new ListView(lvClientNetworksContent, new float[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 });
        lvClientBss        = new ListView(lvClientBssContent, new float[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 60 });
        lvClientProfiles   = new ListView(lvClientProfilesContent, new float[] { 200, 200 });

        openFileDialog = new OpenFileDialog();
        saveFileDialog = new SaveFileDialog();

        btClientOpen.onClick.AddListener(() => Dispatch(btClientOpen_Click));
        btClientClose.onClick.AddListener(() => Dispatch(btClientClose_Click));
        btClientEnumInterfaces.onClick.AddListener(() => Dispatch(btClientEnumInterfaces_Click));
        btClientDisconnect.onClick.AddListener(() => Dispatch(btClientDisconnect_Click));
        btClientGetInterfaceState.onClick.AddListener(() => Dispatch(btClientGetInterfaceState_Click));
        btTurnOn.onClick.AddListener(() => Dispatch(btTurnOn_Click));
        btTurnOff.onClick.AddListener(() => Dispatch(btTurnOff_Click));
        btCurrentIp.onClick.AddListener(() => Dispatch(btCurrentIp_Click));
        btGetIpSettings.onClick.AddListener(() => Dispatch(btGetIpSettings_Click));
        btEnableDhcp.onClick.AddListener(() => Dispatch(btEnableDhcp_Click));
        btEnableStaticIp.onClick.AddListener(() => Dispatch(btEnableStaticIp_Click));
        btGetDualStaState.onClick.AddListener(() => Dispatch(btGetDualStaState_Click));
        btEnableDualSta.onClick.AddListener(() => Dispatch(btEnableDualSta_Click));
        btDisableDualSta.onClick.AddListener(() => Dispatch(btDisableDualSta_Click));
        btEnumSecondaryInterfaces.onClick.AddListener(() => Dispatch(btEnumSecondaryInterfaces_Click));
        btClientEnumNetworks.onClick.AddListener(() => Dispatch(btClientEnumNetworks_Click));
        btClientScan.onClick.AddListener(() => Dispatch(btClientScan_Click));
        btClientEnumBSS.onClick.AddListener(() => Dispatch(btClientEnumBss_Click));
        btClientEnumProfiles.onClick.AddListener(() => Dispatch(btClientEnumProfiles_Click));
        btClientDeleteProfile.onClick.AddListener(() => Dispatch(btClientDeleteProfile_Click));
        btClientRenameProfile.onClick.AddListener(() => Dispatch(btClientRenameProfile_Click));
        btClientGetProfileXml.onClick.AddListener(() => Dispatch(btClientGetprofileXml_Click));
        btClientSetProfileXml.onClick.AddListener(() => Dispatch(btClientSetProfileXml_Click));
        btSetProfileXmlUserData.onClick.AddListener(() => Dispatch(btSetXmlUserData_Click));
        btEditUi.onClick.AddListener(() => Dispatch(btShowEditUi_Click));
        btClientConnect.onClick.AddListener(() => Dispatch(btClientConnect_Click));
        btClientGetShowDeniedNetworks.onClick.AddListener(() => Dispatch(btClientGetShowDeniedNetworks_Click));
        btClientGetPowerSettings.onClick.AddListener(() => Dispatch(btClientGetPowerSettings_Click));
        btClientGetAllowExplicitCreds.onClick.AddListener(() => Dispatch(btClientGetAllowExplicitCreds_Click));
        btClientGetBlockPeriod.onClick.AddListener(() => Dispatch(btClientGetBlockPeriod_Click));
        btClientGetVirtualStationExtensibility.onClick.AddListener(() => Dispatch(btClientGetVirtualStationExtensibility_Click));
        btClientSetShowDeniedNetworks.onClick.AddListener(() => Dispatch(btClientSetShowDeniedNetworks_Click));
        btClientGetOnlyUseGpProfiles.onClick.AddListener(() => Dispatch(btClientGetOnlyUseGpProfiles_Click));
        btClientSetAllowExplicitCreds.onClick.AddListener(() => Dispatch(btClientSetAllowExplicitCreds_Click));
        btClientSetBlockPeriod.onClick.AddListener(() => Dispatch(btClientSetBlockPeriod_Click));
        btClientSetVirtualStationExtensibility.onClick.AddListener(() => Dispatch(btClientSetVirtualStationExtensibility_Click));
    }

    private void Start() { Dispatch(DoStart); }

    private void DoStart()
    {
        cbClientBssType.value = 2;
        cbClientConnect.value = 0;

        Client = new wclWiFiClient();
        Client.AfterOpen  += new EventHandler(Client_AfterOpen);
        Client.BeforeClose += new EventHandler(Client_BeforeClose);

        ProfilesManager = new wclWiFiProfilesManager();

        UpdateUI(false);
    }

    private void Update()
    {
        FitOpenDropdown(cbClientBssType);
        FitOpenDropdown(cbClientConnect);

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
        if (Client != null) { try { Client.Close(); } catch { } }
    }

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

    // ==========================================================
    //  Helpers
    // ==========================================================
    private Boolean ShowResult(Int32 Res)
    {
        if (Res == wclErrors.WCL_E_SUCCESS) return true;
        MessageBox.Show("Error: 0x" + Res.ToString("X8"));
        return false;
    }

    private void ClientClearInterfaces() { lvClientInterfaces.Items.Clear(); }
    private void ClientClearNetworks()   { lvClientNetworks.Items.Clear(); }
    private void ClientClearBss()        { lvClientBss.Items.Clear(); }
    private void ClientClearProfiles()   { lvClientProfiles.Items.Clear(); }

    private void Client_AfterOpen(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            ProfilesManager.Open();
            UpdateUI(true);
        });
    }

    private void Client_BeforeClose(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            ProfilesManager.Close();
            ClientClearInterfaces();
            ClientClearNetworks();
            ClientClearBss();
            ClientClearProfiles();
            UpdateUI(false);
        });
    }

    private Boolean ClientGetInterfaceId(out Guid Id)
    {
        Id = Guid.Empty;
        if (lvClientInterfaces.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select interface");
            return false;
        }
        Id = new Guid(lvClientInterfaces.SelectedItems[0].Text);
        return true;
    }

    private Boolean ClientGetProfile(out String Name)
    {
        Name = "";
        if (lvClientProfiles.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select profile:");
            return false;
        }
        Name = lvClientProfiles.SelectedItems[0].Text;
        return true;
    }

    private wclWiFiBssType ClientGetNetworkBss()
    {
        if (lvClientNetworks.SelectedItems.Count == 0)
            return (wclWiFiBssType)cbClientBssType.value;

        if (lvClientNetworks.SelectedItems[0].SubItems[2].Text == "bssInfrastructure")
            return wclWiFiBssType.bssInfrastructure;

        if (lvClientNetworks.SelectedItems[0].SubItems[2].Text == "bssIndependent")
            return wclWiFiBssType.bssIndependent;

        throw new Exception("Inavlid BSS type");
    }

    private void UpdateUI(Boolean Enable)
    {
        btClientOpen.interactable = !Enable;
        btClientClose.interactable = Enable;
        btClientEnumInterfaces.interactable = Enable;
        btClientEnumNetworks.interactable = Enable;
        btClientScan.interactable = Enable;
        btClientEnumBSS.interactable = Enable;
        btClientDisconnect.interactable = Enable;
        btClientGetShowDeniedNetworks.interactable = Enable;
        btClientSetShowDeniedNetworks.interactable = Enable;
        btClientGetPowerSettings.interactable = Enable;
        btClientGetOnlyUseGpProfiles.interactable = Enable;
        btClientGetAllowExplicitCreds.interactable = Enable;
        btClientSetAllowExplicitCreds.interactable = Enable;
        btClientGetBlockPeriod.interactable = Enable;
        btClientSetBlockPeriod.interactable = Enable;
        btClientGetVirtualStationExtensibility.interactable = Enable;
        btClientSetVirtualStationExtensibility.interactable = Enable;
        btClientGetInterfaceState.interactable = Enable;
        btClientEnumProfiles.interactable = Enable;
        btClientDeleteProfile.interactable = Enable;
        btClientRenameProfile.interactable = Enable;
        btClientGetProfileXml.interactable = Enable;
        btClientSetProfileXml.interactable = Enable;
        btClientConnect.interactable = Enable;
        btTurnOn.interactable = Enable;
        btTurnOff.interactable = Enable;
        btCurrentIp.interactable = Enable;
        btGetIpSettings.interactable = Enable;
        btEnableDhcp.interactable = Enable;
        btEnableStaticIp.interactable = Enable;
        btEditUi.interactable = Enable;
        btSetProfileXmlUserData.interactable = Enable;
        btGetDualStaState.interactable = Enable;
        btEnableDualSta.interactable = Enable;
        btDisableDualSta.interactable = Enable;
        btEnumSecondaryInterfaces.interactable = Enable;
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClientOpen_Click()  { ShowResult(Client.Open()); }
    private void btClientClose_Click() { ShowResult(Client.Close()); }

    private void btClientEnumInterfaces_Click()
    {
        wclWiFiInterfaceData[] Interfaces = null;
        ClientClearInterfaces();
        if (ShowResult(Client.EnumInterfaces(out Interfaces)))
        {
            if (Interfaces != null)
            {
                for (Int32 i = 0; i < Interfaces.Length; i++)
                {
                    ListViewItem Item = lvClientInterfaces.Items.Add(Interfaces[i].Id.ToString());
                    Item.SubItems.Add(Interfaces[i].Description);
                    Item.SubItems.Add(Interfaces[i].Primary.ToString());
                }
            }
        }
    }

    private void btClientEnumNetworks_Click()
    {
        Guid Id = Guid.Empty;
        ClientClearNetworks();
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiAvailableNetworkFilter Filters = 0;
            if (cbClientAdHocProfiles.isOn) Filters |= wclWiFiAvailableNetworkFilter.ffIncludeAllAdhocProfiles;
            if (cbClientHiddenProfiles.isOn) Filters |= wclWiFiAvailableNetworkFilter.ffIncludeAllManualHiddenProfiles;

            wclWiFiAvailableNetwork[] Networks = null;
            if (ShowResult(Client.EnumAvailableNetworks(Id, Filters, out Networks)))
            {
                if (Networks != null)
                {
                    for (Int32 i = 0; i < Networks.Length; i++)
                    {
                        ListViewItem Item = lvClientNetworks.Items.Add(Networks[i].ProfileName);
                        Item.SubItems.Add(Networks[i].Ssid);
                        Item.SubItems.Add(Networks[i].BssType.ToString());
                        Item.SubItems.Add(Networks[i].NumberOfBssids.ToString());
                        Item.SubItems.Add(Networks[i].NetworkConnectable.ToString());
                        Item.SubItems.Add(Networks[i].NotConnectableReason.ToString());
                        Item.SubItems.Add(Networks[i].PhyTypes.ToString());
                        Item.SubItems.Add(Networks[i].MorePhyTypes.ToString());
                        Item.SubItems.Add(Networks[i].SignalQuality.ToString());
                        Item.SubItems.Add(Networks[i].SecurityEnabled.ToString());
                        Item.SubItems.Add(Networks[i].DefaultAuthAlgorithm.ToString());
                        Item.SubItems.Add(Networks[i].DefaultCipherAlgorithm.ToString());
                        Item.SubItems.Add(Networks[i].Flags.ToString());
                    }
                }
            }
        }
    }

    private void btClientScan_Click()
    {
        Guid Id = Guid.Empty;
        if (ClientGetInterfaceId(out Id))
        {
            if (ShowResult(Client.Scan(Id, edClientScanSsid.text)))
                MessageBox.Show("Scan started. Use EventsDemo to see scan events.");
        }
    }

    private void btClientEnumBss_Click()
    {
        ClientClearBss();
        Guid Id = Guid.Empty;

        if (ClientGetInterfaceId(out Id))
        {
            String Ssid = "";
            Boolean SecurityEnabled = false;
            wclWiFiBssType BssType = wclWiFiBssType.bssAny;

            if (cbClientUseSsid.isOn)
            {
                if (lvClientNetworks.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Select network");
                    return;
                }
                Ssid = lvClientNetworks.SelectedItems[0].SubItems[1].Text;
                SecurityEnabled = Convert.ToBoolean(lvClientNetworks.SelectedItems[0].SubItems[9].Text);
                BssType = ClientGetNetworkBss();
            }

            wclWiFiBss[] BssList = null;
            if (ShowResult(Client.EnumBss(Id, Ssid, BssType, SecurityEnabled, out BssList)))
            {
                if (BssList != null)
                {
                    for (Int32 i = 0; i < BssList.Length; i++)
                    {
                        ListViewItem Item = lvClientBss.Items.Add(BssList[i].Ssid);
                        Item.SubItems.Add(BssList[i].PhyId.ToString());
                        Item.SubItems.Add(BssList[i].Mac);
                        Item.SubItems.Add(BssList[i].BssType.ToString());
                        Item.SubItems.Add(BssList[i].PhyType.ToString());
                        Item.SubItems.Add(BssList[i].Rssi.ToString());
                        Item.SubItems.Add(BssList[i].LinkQuality.ToString());
                        Item.SubItems.Add(BssList[i].InRegDomain.ToString());
                        Item.SubItems.Add(BssList[i].BeaconPeriod.ToString());
                        Item.SubItems.Add(BssList[i].Timestamp.ToString());
                        Item.SubItems.Add(BssList[i].HostTimestamp.ToString());
                        Item.SubItems.Add(BssList[i].Capability.ToString());
                        Item.SubItems.Add(BssList[i].ChCenterFrequency.ToString());

                        String s = "";
                        if (BssList[i].IeRaw != null && BssList[i].IeRaw.Length > 0)
                        {
                            for (Int32 x = 0; x < BssList[i].IeRaw.Length; x++)
                                s = s + BssList[i].IeRaw[x].ToString("X2");
                        }
                        Item.SubItems.Add(s);
                    }
                }
            }
        }
    }

    private void btClientDisconnect_Click()
    {
        Guid Id = Guid.Empty;
        if (ClientGetInterfaceId(out Id))
            ShowResult(Client.Disconnect(Id));
    }

    private void btClientGetInterfaceState_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open()))
            {
                try
                {
                    wclWiFiInterfaceState State;
                    if (ShowResult(Iface.GetState(out State)))
                        MessageBox.Show("Interface state: " + State.ToString());
                }
                finally { Iface.Close(); }
            }
        }
    }

    private void btClientEnumProfiles_Click() { ClientEnumProfiles(); }

    private void ClientEnumProfiles()
    {
        ClientClearProfiles();
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiProfileData[] Profiles;
            if (ShowResult(ProfilesManager.GetProfileList(Id, out Profiles)))
            {
                if (Profiles != null)
                {
                    for (Int32 i = 0; i < Profiles.Length; i++)
                    {
                        ListViewItem Item = lvClientProfiles.Items.Add(Profiles[i].Name);
                        Item.SubItems.Add(Profiles[i].Flags.ToString());
                    }
                }
            }
        }
    }

    private void btClientDeleteProfile_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            String Profile;
            if (ClientGetProfile(out Profile))
            {
                if (ShowResult(ProfilesManager.DeleteProfile(Id, Profile)))
                    ClientEnumProfiles();
            }
        }
    }

    private void btClientRenameProfile_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            String Profile;
            if (ClientGetProfile(out Profile))
            {
                if (ShowResult(ProfilesManager.RenameProfile(Id, Profile, "NEW_" + Profile)))
                    ClientEnumProfiles();
            }
        }
    }

    private void btClientGetprofileXml_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            String Profile;
            if (ClientGetProfile(out Profile))
            {
                wclWiFiProfileFlag Flags = 0;
                if (cbPlainText.isOn) Flags |= wclWiFiProfileFlag.pfGetPlaintextKey;
                String Xml;
                if (ShowResult(ProfilesManager.GetProfile(Id, Profile, ref Flags, out Xml)))
                {
                    saveFileDialog.FileName = Profile + ".xml";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        File.WriteAllText(saveFileDialog.FileName, Xml);
                }
            }
        }
    }

    private void btClientSetProfileXml_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            openFileDialog.FileName = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                String Xml = File.ReadAllText(openFileDialog.FileName);
                ShowResult(ProfilesManager.SetProfile(Id, 0, Xml, true));
            }
        }
    }

    private void btSetXmlUserData_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            String Profile;
            if (ClientGetProfile(out Profile))
            {
                openFileDialog.FileName = "";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    String Xml = File.ReadAllText(openFileDialog.FileName);
                    ShowResult(ProfilesManager.SetProfileEapXmlUserData(Id, Profile, false, Xml));
                }
            }
        }
    }

    private void btShowEditUi_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            String Profile;
            if (ClientGetProfile(out Profile))
                ShowResult(ProfilesManager.ShowUIEdit(Id, Profile));
        }
    }

    private void btClientConnect_Click()
    {
        switch (cbClientConnect.value)
        {
            case 0: ClientConnectUsingNetworkProfile(); break;
            case 1: ClientConnectUsingSelectedprofile(); break;
            case 2: ClientConnectUsingSelectedNetwork(); break;
            case 3: ClientConenctUsingTemporaryProfile(); break;
        }
    }

    private void ClientConnectUsingNetworkProfile()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            if (lvClientNetworks.SelectedItems.Count == 0) MessageBox.Show("Select network");
            else
            {
                if (cbUseBssMac.isOn && lvClientBss.SelectedItems.Count == 0) MessageBox.Show("Select Bss");
                else
                {
                    String Mac = "";
                    if (cbUseBssMac.isOn) Mac = lvClientBss.SelectedItems[0].SubItems[2].Text;

                    ShowResult(Client.Connect(Id, wclWiFiConnectionMode.cmProfile,
                        lvClientNetworks.SelectedItems[0].Text, "", ClientGetNetworkBss(), 0, Mac));
                }
            }
        }
    }

    private void ClientConnectUsingSelectedprofile()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            if (lvClientNetworks.SelectedItems.Count == 0) MessageBox.Show("Select network");
            else
            {
                if (lvClientProfiles.SelectedItems.Count == 0) MessageBox.Show("Select profile");
                else
                {
                    if (cbUseBssMac.isOn && lvClientBss.SelectedItems.Count == 0) MessageBox.Show("Select Bss");
                    else
                    {
                        String Mac = "";
                        if (cbUseBssMac.isOn) Mac = lvClientBss.SelectedItems[0].SubItems[2].Text;

                        ShowResult(Client.Connect(Id, wclWiFiConnectionMode.cmProfile,
                            lvClientProfiles.SelectedItems[0].Text, "", ClientGetNetworkBss(), 0, Mac));
                    }
                }
            }
        }
    }

    private void ClientConnectUsingSelectedNetwork()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            if (lvClientNetworks.SelectedItems.Count == 0) MessageBox.Show("Select network");
            else
            {
                if (cbUseBssMac.isOn && lvClientBss.SelectedItems.Count == 0) MessageBox.Show("Select Bss");
                else
                {
                    String Mac = "";
                    if (cbUseBssMac.isOn) Mac = lvClientBss.SelectedItems[0].SubItems[2].Text;

                    wclWiFiConnectionMode Mode;
                    if (cbClientDiscoverSecure.isOn) Mode = wclWiFiConnectionMode.cmDiscoverySecure;
                    else Mode = wclWiFiConnectionMode.cmDiscoveryUnsecure;

                    ShowResult(Client.Connect(Id, Mode, "", lvClientNetworks.SelectedItems[0].SubItems[1].Text,
                        ClientGetNetworkBss(), 0, Mac));
                }
            }
        }
    }

    private void ClientConenctUsingTemporaryProfile()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            if (lvClientNetworks.SelectedItems.Count == 0) MessageBox.Show("Select network");
            else
            {
                if (cbUseBssMac.isOn && lvClientBss.SelectedItems.Count == 0) MessageBox.Show("Select Bss");
                else
                {
                    String Mac = "";
                    if (cbUseBssMac.isOn) Mac = lvClientBss.SelectedItems[0].SubItems[2].Text;

                    String Ssid = lvClientNetworks.SelectedItems[0].SubItems[1].Text;
                    String Hex = ProfilesManager.SsidToHex(Ssid);

                    String AuthAlg;
                    if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auOpen") AuthAlg = "open";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auSharedKey") AuthAlg = "shared";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auWpa") AuthAlg = "WPA";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auWpaPsk") AuthAlg = "WPAPSK";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auRsna") AuthAlg = "WPA2";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auWpa3") AuthAlg = "WPA3ENT192";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auWpa3Sae") AuthAlg = "WPA3SAE";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auWpa3Ent") AuthAlg = "WPA3ENT";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[11].Text == "auOwe") AuthAlg = "OWE";
                    else AuthAlg = "WPA2PSK";

                    String CipherAlg;
                    if (lvClientNetworks.SelectedItems[0].SubItems[12].Text == "caNone") CipherAlg = "none";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[12].Text == "caTkip") CipherAlg = "TKIP";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[12].Text == "caWep40" ||
                             lvClientNetworks.SelectedItems[0].SubItems[12].Text == "caWep104" ||
                             lvClientNetworks.SelectedItems[0].SubItems[12].Text == "caWep") CipherAlg = "WEP";
                    else if (lvClientNetworks.SelectedItems[0].SubItems[12].Text == "caGcmp256") CipherAlg = "GCMP256";
                    else CipherAlg = "AES";

                    String Xml = "<?xml version=\"1.0\"?>" +
                        "<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
                        "<name>" + Ssid + "</name>" +
                        "<SSIDConfig><SSID><hex>" + Hex + "</hex><name>" + Ssid + "</name></SSID></SSIDConfig>" +
                        "<connectionType>ESS</connectionType>" +
                        "<connectionMode>manual</connectionMode>" +
                        "<autoSwitch>false</autoSwitch>" +
                        "<MSM><security><authEncryption>" +
                        "<authentication>" + AuthAlg + "</authentication>" +
                        "<encryption>" + CipherAlg + "</encryption>" +
                        "<useOneX>false</useOneX>" +
                        "</authEncryption>" +
                        "<sharedKey><keyType>passPhrase</keyType><protected>false</protected>" +
                        "<keyMaterial>" + edClientPasskey.text + "</keyMaterial></sharedKey>" +
                        "</security></MSM></WLANProfile>";

                    ShowResult(Client.Connect(Id, wclWiFiConnectionMode.cmTemporaryProfile, Xml,
                        Ssid, ClientGetNetworkBss(), 0, Mac));
                }
            }
        }
    }

    private void btTurnOn_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
            ClientSwitchState(Id, false);
    }

    private void btTurnOff_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
            ClientSwitchState(Id, true);
    }

    private void ClientSwitchState(Guid Id, Boolean Off)
    {
        wclWiFiInterface Iface = new wclWiFiInterface(Id);
        try
        {
            if (ShowResult(Iface.Open()))
            {
                try
                {
                    if (Off) ShowResult(Iface.TurnOff());
                    else     ShowResult(Iface.TurnOn());
                }
                finally { Iface.Close(); }
            }
        }
        finally { Iface = null; }
    }

    private void btCurrentIp_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open()))
            {
                Boolean Static; String Dns1, Dns2, Address, Gateway, Mask;
                Int32 Res = Iface.GetCurrentIp(out Static, out Address, out Mask, out Gateway, out Dns1, out Dns2);
                if (ShowResult(Res))
                {
                    String Msg;
                    if (Static) Msg = "Static IP\n"; else Msg = "DHCP\n";
                    Msg = Msg + "Address: " + Address + "\n" +
                        "Subnet mask: " + Mask + "\n" +
                        "Default gateway: " + Gateway + "\n" +
                        "Name server 1: " + Dns1 + "\n" +
                        "Name server 2: " + Dns2;
                    MessageBox.Show(Msg);
                }
                Iface.Close();
            }
            Iface = null;
        }
    }

    private void btGetIpSettings_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open()))
            {
                Boolean Static; String Dns1, Dns2, Address, Gateway, Mask;
                Int32 Res = Iface.GetIpSettings(out Static, out Address, out Mask, out Gateway, out Dns1, out Dns2);
                if (ShowResult(Res))
                {
                    String Msg;
                    if (Static) Msg = "Static IP\n"; else Msg = "DHCP\n";
                    Msg = Msg + "Address: " + Address + "\n" +
                        "Subnet mask: " + Mask + "\n" +
                        "Default gateway: " + Gateway + "\n" +
                        "Name server 1: " + Dns1 + "\n" +
                        "Name server 2: " + Dns2;
                    MessageBox.Show(Msg);
                }
                Iface.Close();
            }
            Iface = null;
        }
    }

    private void btEnableDhcp_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open())) { ShowResult(Iface.EnableDhcp()); Iface.Close(); }
            Iface = null;
        }
    }

    private void btEnableStaticIp_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open()))
            {
                ShowResult(Iface.SetStaticIp("192.168.1.210", "255.255.255.0", "192.168.1.1", "192.168.1.1", ""));
                Iface.Close();
            }
            Iface = null;
        }
    }

    private void btGetDualStaState_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open()))
            {
                Boolean State;
                if (ShowResult(Iface.GetSecondarySta(out State)))
                    MessageBox.Show("Dual-STA state: " + State.ToString());
                Iface.Close();
            }
        }
    }

    private void ClientSetDualState(Boolean Enable)
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open())) { ShowResult(Iface.SetSecondarySta(Enable)); Iface.Close(); }
        }
    }

    private void btEnableDualSta_Click()  { ClientSetDualState(true); }
    private void btDisableDualSta_Click() { ClientSetDualState(false); }

    private void btEnumSecondaryInterfaces_Click()
    {
        Guid Id;
        if (ClientGetInterfaceId(out Id))
        {
            wclWiFiInterface Iface = new wclWiFiInterface(Id);
            if (ShowResult(Iface.Open()))
            {
                wclWiFiInterfaceData[] Ifaces;
                if (ShowResult(Iface.EnumInterfaces(out Ifaces)))
                {
                    ClientClearInterfaces();
                    if (Ifaces != null)
                    {
                        for (Int32 i = 0; i < Ifaces.Length; i++)
                        {
                            ListViewItem Item = lvClientInterfaces.Items.Add(Ifaces[i].Id.ToString());
                            Item.SubItems.Add(Ifaces[i].Description);
                            Item.SubItems.Add(Ifaces[i].Primary.ToString());
                        }
                    }
                }
                Iface.Close();
            }
        }
    }

    private void btClientGetShowDeniedNetworks_Click()
    {
        Boolean Enabled = false;
        if (ShowResult(Client.AcmGetShowDeniedNetworks(out Enabled)))
        {
            if (Enabled) MessageBox.Show("Show denied networks");
            else MessageBox.Show("Do not show denied networks");
        }
    }

    private void btClientSetShowDeniedNetworks_Click()
    {
        DialogResult Res = MessageBox.Show("Show denied networks?", "Show denied networks",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (Res == DialogResult.Cancel) return;
        ShowResult(Client.AcmSetShowDeniedNetworks(Res == DialogResult.Yes));
    }

    private void btClientGetPowerSettings_Click()
    {
        wclWiFiPowerSetting Setting;
        if (ShowResult(Client.AcmGetPowerSetting(out Setting)))
            MessageBox.Show(Setting.ToString());
    }

    private void btClientGetOnlyUseGpProfiles_Click()
    {
        Boolean Enabled;
        if (ShowResult(Client.AcmGetOnlyUseGpProfiles(out Enabled)))
        {
            if (Enabled) MessageBox.Show("Only use GP profiles ENABLED");
            else MessageBox.Show("Only use GP profiles DISABLED");
        }
    }

    private void btClientGetAllowExplicitCreds_Click()
    {
        Boolean Enabled;
        if (ShowResult(Client.AcmGetAllowExplicitCreds(out Enabled)))
        {
            if (Enabled) MessageBox.Show("Explicit creds ALLOWED");
            else MessageBox.Show("Explicit creds NOT ALLOWED");
        }
    }

    private void btClientSetAllowExplicitCreds_Click()
    {
        DialogResult Res = MessageBox.Show("Allow explicit creds?", "Allow explicit creds?",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (Res == DialogResult.Cancel) return;
        ShowResult(Client.AcmSetAllowExplicitCreds(Res == DialogResult.Yes));
    }

    private void btClientGetBlockPeriod_Click()
    {
        UInt32 Period;
        if (ShowResult(Client.AcmGetBlockPeriod(out Period)))
            MessageBox.Show("Block period: " + Period.ToString());
    }

    private void btClientSetBlockPeriod_Click()
    {
        UInt32 Period = 10;
        ShowResult(Client.AcmSetBlockPeriod(Period));
    }

    private void btClientGetVirtualStationExtensibility_Click()
    {
        Boolean Allowed;
        if (ShowResult(Client.AcmGetVirtualStationExtensibility(out Allowed)))
        {
            if (Allowed) MessageBox.Show("Virtual station extensibility ALLOWED");
            else MessageBox.Show("Virtual station extensibility NOT ALLOWED");
        }
    }

    private void btClientSetVirtualStationExtensibility_Click()
    {
        DialogResult Res = MessageBox.Show("Allow wirtual station extensibility?",
            "Allow wirtual station extensibility?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (Res == DialogResult.Cancel) return;
        ShowResult(Client.AcmSetVirtualStationExtensibility(Res == DialogResult.Yes));
    }
}