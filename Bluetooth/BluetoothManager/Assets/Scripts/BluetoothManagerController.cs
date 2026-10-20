using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclBluetooth;

// ==========================================================
//  WinForms-style dialog enums
// ==========================================================
public enum DialogResult { None, OK, Cancel, Yes, No }
public enum MessageBoxButtons { OK, OKCancel, YesNo, YesNoCancel }
public enum MessageBoxIcon { None, Error, Question, Warning, Information }

// ==========================================================
//  WinForms-style MessageBox (native Windows, blocking)
// ==========================================================
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
//  WinForms-style ListView (plain C#, NOT MonoBehaviour)
// ==========================================================
public class ListView
{
    public RectTransform Content;
    public float[]       ColumnWidths;

    private const float RowHeight = 18f;

    internal readonly List<ListViewItem> _items    = new List<ListViewItem>();
    internal readonly List<ListViewItem> _selected = new List<ListViewItem>();

    private static readonly Color SelectedColor    = new Color(0.70f, 0.85f, 0.98f, 1f);
    private static readonly Color NormalColor      = Color.white;

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
    }

    internal void Select(ListViewItem item)
    {
        foreach (var s in _selected)
            if (s.Background != null) s.Background.color = NormalColor;
        _selected.Clear();
        if (item != null)
        {
            _selected.Add(item);
            if (item.Background != null) item.Background.color = SelectedColor;
        }
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
        btn.onClick.AddListener(() => Owner.Select(captured));

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
//  Controller (fmMain)
// ==========================================================
public class BluetoothManagerController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btOpen;
    public Button btClose;
    public Button btRefreshRadio;
    public Button btSetConnectable;
    public Button btSetDiscoverable;
    public Button btSetName;
    public Button ntTurnOff;
    public Button btTurnOn;
    public Button btDiscover;
    public Button btTerminate;
    public Button btEnumPaired;
    public Button btPair;
    public Button btUnpair;
    public Button btRssi;
    public Button btRefreshDevice;
    public Button btEnumServices;
    public Button btClear;
    public Button btCreateVCOM;
    public Button btDestroyVCOM;
    public Button btEnumVCOMs;
    public Button btIntsallDevice;
    public Button btUninstallDevice;
    public Button btIsInRange;
    public Button btDisconnect;
    public Button btEnumConnected;
    public Button btEnumInstalledServices;

    [Header("Inputs")]
    public TMP_InputField edName;
    public TMP_InputField edTimeout;
    public TMP_InputField edPIN;

    [Header("Dropdowns")]
    public TMP_Dropdown cbDiscoveringMode;
    public TMP_Dropdown cbMitmProtection;
    public TMP_Dropdown cbIoCap;
    public TMP_Dropdown cbBleProtection;
    public TMP_Dropdown cbPairingMethod;

    [Header("Checkboxes")]
    public Toggle cbEnumPreinstalled;
    public Toggle cbHandlePairing;
    public Toggle cbDontTurnOff;

    [Header("ListViews (Content)")]
    public RectTransform lvRadiosContent;
    public RectTransform lvDevicesContent;
    public RectTransform lvServicesContent;
    public RectTransform lvEventsContent;
    public RectTransform lvComPortsContent;

    private ListView lvRadios;
    private ListView lvDevices;
    private ListView lvServices;
    private ListView lvEvents;
    private ListView lvComPorts;

    private wclBluetoothManager Manager;
    private wclPowerEventsMonitor FPowerMonitor;

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        btOpen.onClick.AddListener(btOpen_Click);
        btClose.onClick.AddListener(btClose_Click);
        btRefreshRadio.onClick.AddListener(btRefreshRadio_Click);
        btSetConnectable.onClick.AddListener(btSetConnectable_Click);
        btSetDiscoverable.onClick.AddListener(btSetDiscoverable_Click);
        btSetName.onClick.AddListener(ntSetName_Click);
        ntTurnOff.onClick.AddListener(ntTurnOff_Click);
        btTurnOn.onClick.AddListener(btTurnOn_Click);
        btDiscover.onClick.AddListener(btDiscover_Click);
        btTerminate.onClick.AddListener(btTerminate_Click);
        btEnumPaired.onClick.AddListener(btEnumPaired_Click);
        btPair.onClick.AddListener(btPair_Click);
        btUnpair.onClick.AddListener(btUnpair_Click);
        btRssi.onClick.AddListener(btRssi_Click);
        btRefreshDevice.onClick.AddListener(btRefreshDevice_Click);
        btEnumServices.onClick.AddListener(btEnumServices_Click);
        btClear.onClick.AddListener(btClear_Click);
        btCreateVCOM.onClick.AddListener(btCreateVCOM_Click);
        btDestroyVCOM.onClick.AddListener(btDestroyVCOM_Click);
        btEnumVCOMs.onClick.AddListener(btEnumVCOMs_Click);
        btIntsallDevice.onClick.AddListener(btIntsallDevice_Click);
        btUninstallDevice.onClick.AddListener(btUninstallDevice_Click);
        btIsInRange.onClick.AddListener(btIsInRange_Click);
        btDisconnect.onClick.AddListener(btDisconnect_Click);
        btEnumConnected.onClick.AddListener(btEnumConnected_Click);
        btEnumInstalledServices.onClick.AddListener(btEnumInstalledServices_Click);

        lvRadios   = new ListView(lvRadiosContent,   new float[] { 100, 70, 100, 80, 80, 70, 80, 80, 100, 100 });
        lvDevices  = new ListView(lvDevicesContent,  new float[] { 100, 100, 90, 80, 100, 50, 80, 80 });
        lvServices = new ListView(lvServicesContent, new float[] { 80, 90, 80, 230, 60, 200, 100 });
        lvEvents   = new ListView(lvEventsContent,   new float[] { 100, 220, 220, 220 });
        lvComPorts = new ListView(lvComPortsContent, new float[] { 80, 90, 230, 60 });
    }

    private void Start()
    {
        Manager = new wclBluetoothManager();

        Manager.AfterOpen += new EventHandler(Manager_AfterOpen);
        Manager.BeforeClose += new EventHandler(Manager_BeforeClose);
        Manager.OnAuthenticationCompleted += new wclBluetoothDeviceResultEvent(Manager_OnAuthenticationCompleted);
        Manager.OnDeviceFound += new wclBluetoothDeviceEvent(Manager_OnDeviceFound);
        Manager.OnDiscoveringCompleted += new wclBluetoothResultEvent(Manager_OnDiscoveringCompleted);
        Manager.OnDiscoveringStarted += new wclBluetoothEvent(Manager_OnDiscoveringStarted);
        Manager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(Manager_OnNumericComparison);
        Manager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(Manager_OnPasskeyNotification);
        Manager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(Manager_OnPasskeyRequest);
        Manager.OnPinRequest += new wclBluetoothPinRequestEvent(Manager_OnPinRequest);
        Manager.OnStatusChanged += new wclBluetoothEvent(Manager_OnStatusChanged);
        Manager.OnConfirm += new wclBluetoothConfirmEvent(Manager_OnConfirm);
        Manager.OnIoCapabilityRequest += new wclBluetoothIoCapabilityRequestEvent(Manager_OnIoCapabilityRequest);
        Manager.OnOobDataRequest += new wclBluetoothOobDataRequestEvent(Manager_OnOobDataRequest);
        Manager.OnProtectionLevelRequest += new wclBluetoothProtectionLevelRequestEvent(Manager_OnProtectionLevelRequest);
        Manager.OnClosed += new EventHandler(Manager_OnClosed);

        FPowerMonitor = new wclPowerEventsMonitor();
        FPowerMonitor.OnPowerStateChanged += new wclPowerStateChangedEvent(FPowerMonitor_OnPowerStateChanged);
        FPowerMonitor.Open();

        cbDiscoveringMode.value = 0;
        cbMitmProtection.value = 3;
        cbIoCap.value = 1;
        cbBleProtection.value = 0;
        cbPairingMethod.value = 0;
    }

    private void OnDestroy()
    {
        if (Manager != null)
        {
            Manager.Close();
            Manager = null;
        }

        if (FPowerMonitor != null)
        {
            FPowerMonitor.Close();
            FPowerMonitor = null;
        }
    }

    private void LateUpdate()
    {
        FitOpenDropdown(cbDiscoveringMode);
        FitOpenDropdown(cbMitmProtection);
        FitOpenDropdown(cbIoCap);
        FitOpenDropdown(cbBleProtection);
        FitOpenDropdown(cbPairingMethod);
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

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClear_Click()
    {
        lvEvents.Items.Clear();
    }

    private void btRefreshRadio_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
            RefreshRadio(lvRadios.SelectedItems[0]);
    }

    private void btOpen_Click()
    {
        Int32 Res = Manager.Open(cbHandlePairing.isOn);
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowError(Res);
    }

    private void btClose_Click()
    {
        Int32 Res = Manager.Close();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowError(Res);
    }

    private void ShowError(Int32 Error)
    {
        MessageBox.Show("Error: 0x" + Error.ToString("X8"), "Error", MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void btSetConnectable_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            DialogResult Mr = MessageBox.Show("[YES] - turn connectable ON, [NO] - turn connectable OFF",
                "Set Connectable", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (Mr != DialogResult.Cancel)
            {
                ListViewItem Item = lvRadios.SelectedItems[0];
                wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

                Int32 Res = Radio.SetConnectable(Mr == DialogResult.Yes);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowError(Res);

                RefreshRadio(Item);
            }
        }
    }

    private void btSetDiscoverable_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            DialogResult Mr = MessageBox.Show("[YES] - turn discoverable ON, [NO] - turn discoverable OFF",
              "Set discoverable", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (Mr != DialogResult.Cancel)
            {
                ListViewItem Item = lvRadios.SelectedItems[0];
                wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

                Int32 Res = Radio.SetDiscoverable(Mr == DialogResult.Yes);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowError(Res);

                RefreshRadio(Item);
            }
        }
    }

    private void ntSetName_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            Int32 Res = Radio.SetName(edName.text);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);

            RefreshRadio(Item);
        }
    }

    private void btEnumPaired_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ClearDevices();

            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            Int64[] Devices;
            Int32 Res = Radio.EnumPairedDevices(out Devices);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                if (Devices != null)
                {
                    for (Int32 i = 0; i < Devices.Length; i++)
                        AddDevice(Radio, Devices[i]);

                    RefreshDevices();
                }
            }
        }
    }

    private void btRefreshDevice_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            RefreshDevice(lvDevices.SelectedItems[0]);
    }

    private void btEnumServices_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ClearServices();

            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            wclBluetoothService[] Services;
            Guid g = Guid.Empty;
            Int32 Res = Radio.EnumRemoteServices(Address, g, out Services);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                if (Services != null)
                {
                    foreach (wclBluetoothService Service in Services)
                    {
                        Item = lvServices.Items.Add(Radio.ApiName);
                        Item.SubItems.Add(Address.ToString("X12"));
                        Item.SubItems.Add(Service.Handle.ToString("X8"));
                        Item.SubItems.Add(Service.Uuid.ToString());
                        Item.SubItems.Add(Service.Channel.ToString());
                        Item.SubItems.Add(Service.Name);
                        Item.SubItems.Add(Service.Comment);
                    }
                }
            }
        }
    }

    private void btDiscover_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            wclBluetoothDiscoverKind Kind;
            if (cbDiscoveringMode.value == 0)
                Kind = wclBluetoothDiscoverKind.dkClassic;
            else
                Kind = wclBluetoothDiscoverKind.dkBle;

            Int32 Res = Radio.Discover(Convert.ToByte(edTimeout.text), Kind);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
        }
    }

    private void btTerminate_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            Int32 Res = Radio.Terminate();
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
        }
    }

    private void btUnpair_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            wclBluetoothPairingMethod Method;
            switch (cbPairingMethod.value)
            {
                case 1:  Method = wclBluetoothPairingMethod.pmClassic; break;
                case 2:  Method = wclBluetoothPairingMethod.pmLe;      break;
                default: Method = wclBluetoothPairingMethod.pmAuto;    break;
            }

            Int32 Res = Radio.RemoteUnpair(Address, Method);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);

            RefreshDevice(Item);
        }
    }

    private void btPair_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            wclBluetoothPairingMethod Method;
            switch (cbPairingMethod.value)
            {
                case 1:  Method = wclBluetoothPairingMethod.pmClassic; break;
                case 2:  Method = wclBluetoothPairingMethod.pmLe;      break;
                default: Method = wclBluetoothPairingMethod.pmAuto;    break;
            }

            Int32 Res = Radio.RemotePair(Address, Method);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
        }
    }

    private void btRssi_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            SByte Rssi;
            Int32 Res = Radio.GetRemoteRssi(Address, out Rssi);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                MessageBox.Show("RSSI: " + Rssi.ToString() + "dB", "RSSI", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }

    private void btTurnOn_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            Int32 Res = Radio.TurnOn();
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);

            RefreshRadio(Item);
        }
    }

    private void ntTurnOff_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            Int32 Res = Radio.TurnOff();
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);

            RefreshRadio(Item);
        }
    }

    private void btEnumVCOMs_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            RefreshVComs();
    }

    private void btCreateVCOM_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            if (lvServices.SelectedItems.Count == 0)
                MessageBox.Show("Select service", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                ListViewItem Item = lvDevices.SelectedItems[0];
                wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
                Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);
                Guid Service = new Guid(lvServices.SelectedItems[0].SubItems[3].Text);

                UInt16 Number;
                Int32 Res = Radio.CreateComPort(Address, Service, out Number);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowError(Res);
                else
                {
                    TraceEvent(Radio, "Create vCOM", "Number", Number.ToString());

                    RefreshVComs();
                }
            }
        }
    }

    private void btDestroyVCOM_Click()
    {
        if (lvComPorts.SelectedItems.Count == 0)
            MessageBox.Show("Select vCOM", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvComPorts.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            UInt16 Number = Convert.ToUInt16(Item.SubItems[3].Text);

            Int32 Res = Radio.DestroyComPort(Number);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                TraceEvent(Radio, "Destroy vCOM", "Number", Number.ToString());

                RefreshVComs();
            }
        }
    }

    private void btIntsallDevice_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            if (lvServices.SelectedItems.Count == 0)
                MessageBox.Show("Select service", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                ListViewItem Item = lvDevices.SelectedItems[0];
                wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
                Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);
                Guid Service = new Guid(lvServices.SelectedItems[0].SubItems[3].Text);

                Int32 Res = Radio.InstallDevice(Address, Service);
                if (Res == wclBluetoothErrors.WCL_E_BLUETOOTH_DEVICE_ALREADY_INSTALLED)
                {
                    // Reinstall to reconnect!
                    Radio.UninstallDevice(Address, Service);
                    Res = Radio.InstallDevice(Address, Service);
                }

                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowError(Res);
                else
                    MessageBox.Show("Device installed");
            }
        }
    }

    private void btUninstallDevice_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            if (lvServices.SelectedItems.Count == 0)
                MessageBox.Show("Select service", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                ListViewItem Item = lvDevices.SelectedItems[0];
                wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
                Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);
                Guid Service = new Guid(lvServices.SelectedItems[0].SubItems[3].Text);

                Int32 Res = Radio.UninstallDevice(Address, Service);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    ShowError(Res);
                else
                    MessageBox.Show("Device uninstalled");
            }
        }
    }

    private void btIsInRange_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            Boolean InRange;
            Int32 Res = Radio.IsRemoteDeviceInRange(Address, out InRange);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                if (InRange)
                    MessageBox.Show("Device is in range");
                else
                    MessageBox.Show("Device is NOT in range");
            }
        }
    }

    private void btDisconnect_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            Int32 Res = Radio.RemoteDisconnect(Address);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
        }
    }

    private void btEnumConnected_Click()
    {
        if (lvRadios.SelectedItems.Count == 0)
            MessageBox.Show("Select radio", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ClearDevices();

            ListViewItem Item = lvRadios.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

            Int64[] Devices;
            Int32 Res = Radio.EnumConnectedDevices(out Devices);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                if (Devices != null)
                {
                    for (Int32 i = 0; i < Devices.Length; i++)
                        AddDevice(Radio, Devices[i]);

                    RefreshDevices();
                }
            }
        }
    }

    private void btEnumInstalledServices_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        else
        {
            ClearServices();

            ListViewItem Item = lvDevices.SelectedItems[0];
            wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
            Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

            Guid[] Services;
            Int32 Res = Radio.EnumInstalledServices(Address, out Services);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowError(Res);
            else
            {
                if (Services != null && Services.Length > 0)
                {
                    foreach (Guid Service in Services)
                    {
                        Item = lvServices.Items.Add(Radio.ApiName);
                        Item.SubItems.Add(Address.ToString("X12"));
                        Item.SubItems.Add("");
                        Item.SubItems.Add(Service.ToString());
                        Item.SubItems.Add("");
                        Item.SubItems.Add("");
                        Item.SubItems.Add("");
                    }
                }
            }
        }
    }

    // ==========================================================
    //  Manager events
    // ==========================================================
    private void TraceEvent(wclBluetoothRadio Radio, String Event,
        String Parameter, String Value)
    {
        String Str;
        if (Radio != null)
            Str = Radio.ApiName;
        else
            Str = "";

        ListViewItem Item = lvEvents.Items.Add(Str);

        Item.SubItems.Add(Event);
        Item.SubItems.Add(Parameter);
        Item.SubItems.Add(Value);
    }

    private void Manager_OnClosed(object sender, EventArgs e)
    {
        ClearRadios();

        TraceEvent(null, "Closed", "", "");
    }

    private Boolean WasOpened = false;

    private void FPowerMonitor_OnPowerStateChanged(object Sender, wclPowerState State)
    {
        switch (State)
        {
            case wclPowerState.psResumeAutomatic:
                TraceEvent(null, "Power", "State", "psResumeAutomatic");
                if (WasOpened)
                {
                    WasOpened = false;
                    Manager.Open();
                }
                break;

            case wclPowerState.psResume:
                TraceEvent(null, "Power", "State", "psResume");
                break;

            case wclPowerState.psSuspend:
                TraceEvent(null, "Power", "State", "psSuspend");
                if (Manager.Active)
                {
                    WasOpened = true;
                    Manager.Close();
                }
                break;

            case wclPowerState.psPowerStatusChanged:
                TraceEvent(null, "Power", "State", "psPowerStatusChanged");
                break;

            case wclPowerState.psUnknown:
                TraceEvent(null, "Power", "State", "psUnknown");
                break;
        }
    }

    private void Manager_OnProtectionLevelRequest(object Sender, wclBluetoothRadio Radio, long Address,
        out wclBluetoothLeProtectionLevel Protection)
    {
        switch (cbBleProtection.value)
        {
            case 0:  Protection = wclBluetoothLeProtectionLevel.pplDefault;  break;
            case 1:  Protection = wclBluetoothLeProtectionLevel.pplNone;     break;
            case 2:  Protection = wclBluetoothLeProtectionLevel.pplEncryption; break;
            case 3:  Protection = wclBluetoothLeProtectionLevel.pplEncryptionAndAuthentication; break;
            default: Protection = wclBluetoothLeProtectionLevel.pplDefault;  break;
        }
    }

    private void Manager_OnOobDataRequest(object Sender, wclBluetoothRadio Radio, long Address, out wclBluetoothOobData OobData)
    {
        // This event fires when a remote device requests OOB data.
        OobData = null;
    }

    private void Manager_OnIoCapabilityRequest(object Sender, wclBluetoothRadio Radio, long Address,
        out wclBluetoothMitmProtection Mitm, out wclBluetoothIoCapability IoCapability, out bool OobPresent)
    {
        switch (cbMitmProtection.value)
        {
            case 0:  Mitm = wclBluetoothMitmProtection.mitmProtectionNotRequired;                break;
            case 1:  Mitm = wclBluetoothMitmProtection.mitmProtectionRequired;                   break;
            case 2:  Mitm = wclBluetoothMitmProtection.mitmProtectionNotRequiredBonding;         break;
            case 3:  Mitm = wclBluetoothMitmProtection.mitmProtectionRequiredBonding;            break;
            case 4:  Mitm = wclBluetoothMitmProtection.mitmProtectionNotRequiredGeneralBonding;  break;
            case 5:  Mitm = wclBluetoothMitmProtection.mitmProtectionRequiredGeneralBonding;     break;
            default: Mitm = wclBluetoothMitmProtection.mitmProtectionNotDefined;                 break;
        }

        switch (cbIoCap.value)
        {
            case 0:  IoCapability = wclBluetoothIoCapability.iocapDisplayOnly;      break;
            case 1:  IoCapability = wclBluetoothIoCapability.iocapDisplayYesNo;     break;
            case 2:  IoCapability = wclBluetoothIoCapability.iocapKeyboardOnly;     break;
            case 3:  IoCapability = wclBluetoothIoCapability.iocapNoInputNoOutput;  break;
            case 4:  IoCapability = wclBluetoothIoCapability.iocapDisplayKeyboard;  break;
            default: IoCapability = wclBluetoothIoCapability.iocapNotDefined;       break;
        }

        // To accept OOB pairing set this parameter to True.
        OobPresent = false;
    }

    private void Manager_OnConfirm(object Sender, wclBluetoothRadio Radio, long Address, out bool Confirm)
    {
        Confirm = true;

        TraceEvent(Radio, "Just Works pairing", "Address", Address.ToString("X12"));
    }

    private void Manager_OnStatusChanged(object Sender, wclBluetoothRadio Radio)
    {
        String Status;
        if (Radio.Available)
            Status = "Available";
        else
        {
            if (Radio.Plugged)
                Status = "Not available, Plugged";
            else
                Status = "Unplugged";
        }
        TraceEvent(Radio, "StatusChanged", "New status", Status);

        Boolean Found = false;
        foreach (ListViewItem Item in lvRadios.Items)
        {
            if (((wclBluetoothRadio)Item.Tag) == Radio)
            {
                Found = true;
                RefreshRadio(Item);
                break;
            }
        }

        if (!Found)
        {
            ListViewItem Item = lvRadios.Items.Add(""); // Api
            Item.Tag = Radio;

            Item.SubItems.Add(""); // Available
            Item.SubItems.Add(""); // Address
            Item.SubItems.Add(""); // Connectable
            Item.SubItems.Add(""); // Discoverable
            Item.SubItems.Add(""); // HCI version
            Item.SubItems.Add(""); // LMP version
            Item.SubItems.Add(""); // Manufacturer
            Item.SubItems.Add(""); // Name
            Item.SubItems.Add(""); // COD

            RefreshRadio(Item);
        }

        if (cbDontTurnOff.isOn && Radio.Plugged && !Radio.Available)
        {
            Int32 Res = Radio.TurnOn();
            if (Res == wclBluetoothErrors.WCL_E_BLUETOOTH_DRIVER_BUSY)
            {
                Thread.Sleep(200);
                Res = Radio.TurnOn();
            }
            if (Res != wclErrors.WCL_E_SUCCESS)
                TraceEvent(Radio, "Turn ON", "Error", Res.ToString("X8"));
        }
    }

    private void Manager_OnPinRequest(object Sender, wclBluetoothRadio Radio, long Address, out string Pin)
    {
        Pin = edPIN.text;

        TraceEvent(Radio, "PinRequest", "Address", Address.ToString("X12"));
        TraceEvent(null, "", "PIN", Pin);
    }

    private void Manager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, long Address, out uint Passkey)
    {
        Passkey = 123456;

        TraceEvent(Radio, "PasskeyRequest", "Address", Address.ToString());
        TraceEvent(null, "", "Passkey", Passkey.ToString());
    }

    private void Manager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, long Address, uint Passkey)
    {
        TraceEvent(Radio, "PasskeyNotification", "Address", Address.ToString("X12"));
        TraceEvent(null, "", "Passkey", Passkey.ToString());
    }

    private void Manager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, long Address, uint Number, out bool Confirm)
    {
        Confirm = true;

        TraceEvent(Radio, "NumericComparison", "Address", Address.ToString("X12"));
        TraceEvent(null, "", "Number", Number.ToString());
        TraceEvent(null, "", "Confirm", Confirm.ToString());
    }

    private void Manager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
    {
        ClearDevices();

        TraceEvent(Radio, "DiscoveringStarted", "", "");
    }

    private void Manager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, int Error)
    {
        TraceEvent(Radio, "DiscoveringCompleted", "Error", "0x" + Error.ToString("X8"));

        RefreshDevices();
    }

    private void Manager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, long Address)
    {
        TraceEvent(Radio, "DeviceFound", "Address", Address.ToString("X12"));

        AddDevice(Radio, Address);
    }

    private void Manager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio, long Address, int Error)
    {
        TraceEvent(Radio, "AuthenticationCompleted", "Address", Address.ToString("X12"));
        TraceEvent(null, "", "Error", "0x" + Error.ToString("X8"));
    }

    private void Manager_BeforeClose(object sender, EventArgs e)
    {
        TraceEvent(null, "BeforeClose", "", "");
    }

    private void Manager_AfterOpen(object sender, EventArgs e)
    {
        TraceEvent(null, "AfterOpen", "Handle pairing", Manager.HandlePairing.ToString());
    }

    // ==========================================================
    //  Helpers
    // ==========================================================
    private void ClearRadios()
    {
        lvRadios.Items.Clear();
        ClearDevices();
    }

    private void RefreshRadio(ListViewItem Item)
    {
        wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

        Item.Text = Radio.ApiName;
        Item.SubItems[1].Text = Radio.Available.ToString();

        Int64 Address = 0;
        Int32 Res = Radio.GetAddress(out Address);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[2].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[2].Text = Address.ToString("X12");

        Boolean BoolVal;
        Res = Radio.GetConnectable(out BoolVal);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[3].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[3].Text = BoolVal.ToString();

        Res = Radio.GetDiscoverable(out BoolVal);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[4].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[4].Text = BoolVal.ToString();

        Byte ByteVal;
        UInt16 WordVal;
        Res = Radio.GetHciVersion(out ByteVal, out WordVal);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[5].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[5].Text = ByteVal.ToString() + "." + WordVal.ToString();

        Res = Radio.GetLmpVersion(out ByteVal, out WordVal);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[6].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[6].Text = ByteVal.ToString() + "." + WordVal.ToString();

        Res = Radio.GetManufacturer(out WordVal);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[7].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[7].Text = WordVal.ToString();

        String StrVal;
        Res = Radio.GetName(out StrVal);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[8].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[8].Text = StrVal;

        UInt32 Cod;
        Res = Radio.GetCod(out Cod);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[9].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[9].Text = "0x" + Cod.ToString("X8");
    }

    private void ClearDevices()
    {
        lvDevices.Items.Clear();
        ClearServices();
    }

    private void RefreshDevice(ListViewItem Item)
    {
        wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;
        Int64 Address = Convert.ToInt64(Item.SubItems[1].Text, 16);

        UInt32 Cod;
        Int32 Res = Radio.GetRemoteCod(Address, out Cod);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[2].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[2].Text = Cod.ToString("X8");

        wclBluetoothDeviceType DevType;
        Res = Radio.GetRemoteDeviceType(Address, out DevType);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[3].Text = "Error: 0x" + Res.ToString("X8");
        else
        {
            switch (DevType)
            {
                case wclBluetoothDeviceType.dtClassic: Item.SubItems[3].Text = "Classic"; break;
                case wclBluetoothDeviceType.dtBle:     Item.SubItems[3].Text = "BLE";     break;
                case wclBluetoothDeviceType.dtMixed:   Item.SubItems[3].Text = "Mixed";   break;
                default:                               Item.SubItems[3].Text = "Unknown"; break;
            }
        }

        String DevName;
        Res = Radio.GetRemoteName(Address, out DevName);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[4].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[4].Text = DevName;

        Boolean Paired;
        Res = Radio.GetRemotePaired(Address, out Paired);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[5].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[5].Text = Paired.ToString();

        Boolean Connected;
        Res = Radio.GetRemoteConnectedStatus(Address, out Connected);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[6].Text = "Error: 0x" + Res.ToString("X8");
        else
            Item.SubItems[6].Text = Connected.ToString();

        wclBluetoothAddressType AddrType;
        Res = Radio.GetRemoteAddressType(Address, out AddrType);
        if (Res != wclErrors.WCL_E_SUCCESS)
            Item.SubItems[7].Text = "Error: 0x" + Res.ToString("X8");
        else
        {
            switch (AddrType)
            {
                case wclBluetoothAddressType.atClassic:     Item.SubItems[7].Text = "Classic";     break;
                case wclBluetoothAddressType.atPublic:      Item.SubItems[7].Text = "LE Public";   break;
                case wclBluetoothAddressType.atRandom:      Item.SubItems[7].Text = "LE Random";   break;
                case wclBluetoothAddressType.atUnspecified: Item.SubItems[7].Text = "Unspecified"; break;
                default:                                    Item.SubItems[7].Text = "Unknown";     break;
            }
        }
    }

    private void ClearServices()
    {
        lvServices.Items.Clear();
        ClearComPorts();
    }

    private void AddDevice(wclBluetoothRadio Radio, Int64 Address)
    {
        ListViewItem Item = lvDevices.Items.Add(Radio.ApiName);
        Item.Tag = Radio;

        Item.SubItems.Add(Address.ToString("X12"));
        Item.SubItems.Add(""); // Class Of Device
        Item.SubItems.Add(""); // Device type
        Item.SubItems.Add(""); // Name
        Item.SubItems.Add(""); // Paired
        Item.SubItems.Add(""); // Connected
        Item.SubItems.Add(""); // Address type
    }

    private void RefreshDevices()
    {
        foreach (ListViewItem Item in lvDevices.Items)
            RefreshDevice(Item);
    }

    private void ClearComPorts()
    {
        lvComPorts.Items.Clear();
    }

    private void RefreshVComs()
    {
        ClearComPorts();

        ListViewItem Item = lvRadios.SelectedItems[0];
        wclBluetoothRadio Radio = (wclBluetoothRadio)Item.Tag;

        Int32 Res;
        wclVirtualComPort[] Ports;
        if (cbEnumPreinstalled.isOn)
            Res = Radio.EnumComPorts(out Ports);
        else
        {
            Ports = Radio.ComPorts;
            Res = wclErrors.WCL_E_SUCCESS;
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error enumerating vCOMs: 0x" + Res.ToString("X8"));
        else
        {
            if (Ports != null && Ports.Length > 0)
            {
                for (int i = 0; i < Ports.Length; i++)
                {
                    wclVirtualComPort Port = Ports[i];

                    Item = lvComPorts.Items.Add(Radio.ApiName);
                    Item.SubItems.Add(Port.Address.ToString("X12"));
                    Item.SubItems.Add(Port.Service.ToString());
                    Item.SubItems.Add(Port.Number.ToString());

                    Item.Tag = Radio;
                }
            }
        }
    }
}