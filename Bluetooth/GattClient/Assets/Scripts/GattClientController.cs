using System;
using System.Collections;
using System.Collections.Generic;
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
//  WinForms-style ListView (plain C#, no MonoBehaviour)
// ==========================================================
public class ListView
{
    public RectTransform Content;
    public float[]       ColumnWidths;

    private const float RowHeight = 18f;

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
//  Controller (fmMain)
// ==========================================================
public class GattClientController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btDiscover;
    public Button btTerminate;
    public Button btEnumPaired;
    public Button btPair;
    public Button btUnpair;
    public Button btConnect;
    public Button btDisconnect;
    public Button btGetParams;
    public Button btSetParams;
    public Button btGetServices;
    public Button btGetIncludedServices;
    public Button btGetCharacteristics;
    public Button btGetCharValue;
    public Button btSetValue;
    public Button btGetDesc;
    public Button btGetDescVal;
    public Button btSubscribe;
    public Button btUnsubscribe;
    public Button btWriteSubsc;
    public Button btWriteUnsubsc;
    public Button btGetMaxPduSize;
    public Button btPhy;
    public Button btClear;

    [Header("Dropdowns")]
    public TMP_Dropdown cbMitmProtection;
    public TMP_Dropdown cbIoCap;
    public TMP_Dropdown cbProtection;
    public TMP_Dropdown cbOpFlag;
    public TMP_Dropdown cbParams;
    public TMP_Dropdown cbWriteKind;
    public TMP_Dropdown cbSubscribeKind;

    [Header("Checkboxes")]
    public Toggle cbForceIndications;
    public Toggle cbConnectOnRead;
    public Toggle cbFastSubscribe;

    [Header("Inputs")]
    public TMP_InputField edCharVal;

    [Header("ListView Contents")]
    public RectTransform lvDevicesContent;
    public RectTransform lvServicesContent;
    public RectTransform lvCharacteristicsContent;
    public RectTransform lvDescriptorsContent;
    public RectTransform lvDescriptorValueContent;
    public RectTransform lvEventsContent;

    private ListView lvDevices;
    private ListView lvServices;
    private ListView lvCharacteristics;
    private ListView lvDescriptors;
    private ListView lvDescriptorValue;
    private ListView lvEvents;

    private wclBluetoothManager Manager;
    private wclGattClient Client;

    private wclGattCharacteristic[] FCharacteristics;
    private wclGattDescriptor[] FDescriptors;
    private wclGattService[] FServices;

    // Deferred actions queue: native wcl calls / MessageBox run in Update, not from click handlers.
    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        btDiscover.onClick.AddListener(() => Dispatch(btDiscover_Click));
        btTerminate.onClick.AddListener(() => Dispatch(btTerminate_Click));
        btEnumPaired.onClick.AddListener(() => Dispatch(btEnumPaired_Click));
        btPair.onClick.AddListener(() => Dispatch(btPair_Click));
        btUnpair.onClick.AddListener(() => Dispatch(btUnpair_Click));
        btConnect.onClick.AddListener(() => Dispatch(btConnect_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btGetParams.onClick.AddListener(() => Dispatch(btGetParams_Click));
        btSetParams.onClick.AddListener(() => Dispatch(btSetParams_Click));
        btGetServices.onClick.AddListener(() => Dispatch(btGetServices_Click));
        btGetIncludedServices.onClick.AddListener(() => Dispatch(btGetIncludedServices_Click));
        btGetCharacteristics.onClick.AddListener(() => Dispatch(btGetCharacteristics_Click));
        btGetCharValue.onClick.AddListener(() => Dispatch(btGetCharValue_Click));
        btSetValue.onClick.AddListener(() => Dispatch(btSetValue_Click));
        btGetDesc.onClick.AddListener(() => Dispatch(btGetDesc_Click));
        btGetDescVal.onClick.AddListener(() => Dispatch(btGetDescVal_Click));
        btSubscribe.onClick.AddListener(() => Dispatch(btSubscribe_Click));
        btUnsubscribe.onClick.AddListener(() => Dispatch(btUnsubscribe_Click));
        btWriteSubsc.onClick.AddListener(() => Dispatch(btWriteSubsc_Click));
        btWriteUnsubsc.onClick.AddListener(() => Dispatch(btWriteUnsubsc_Click));
        btGetMaxPduSize.onClick.AddListener(() => Dispatch(btGetMaxPduSize_Click));
        btPhy.onClick.AddListener(() => Dispatch(btPhy_Click));
        btClear.onClick.AddListener(() => Dispatch(btClear_Click));

        cbFastSubscribe.onValueChanged.AddListener(_ => cbFastSubscribe_CheckedChanged());

        lvDevices         = new ListView(lvDevicesContent,         new float[] { 150, 150, 150, 80 });
        lvServices        = new ListView(lvServicesContent,        new float[] { 200, 80, 100 });
        lvCharacteristics = new ListView(lvCharacteristicsContent, new float[] { 200, 80, 90, 100, 100, 80, 80, 80, 80, 80, 80, 80, 80 });
        lvDescriptors     = new ListView(lvDescriptorsContent,     new float[] { 200, 80, 80, 80, 100, 100 });
        lvDescriptorValue = new ListView(lvDescriptorValueContent, new float[] { 200, 200 });
        lvEvents          = new ListView(lvEventsContent,          new float[] { 100, 150, 150, 150 });
    }

    private void Start()
    {
        Dispatch(DoStart);
    }

    private void DoStart()
    {
        Manager = new wclBluetoothManager();
        Client = new wclGattClient();

        Manager.OnNumericComparison += new wclBluetoothNumericComparisonEvent(Manager_OnNumericComparison);
        Manager.OnPasskeyNotification += new wclBluetoothPasskeyNotificationEvent(Manager_OnPasskeyNotification);
        Manager.OnPasskeyRequest += new wclBluetoothPasskeyRequestEvent(Manager_OnPasskeyRequest);
        Manager.OnPinRequest += new wclBluetoothPinRequestEvent(Manager_OnPinRequest);
        Manager.OnAuthenticationCompleted += new wclBluetoothDeviceResultEvent(Manager_OnAuthenticationCompleted);
        Manager.OnConfirm += new wclBluetoothConfirmEvent(Manager_OnConfirm);
        Manager.OnProtectionLevelRequest += new wclBluetoothProtectionLevelRequestEvent(Manager_OnProtectionLevelRequest);
        Manager.OnDeviceFound += new wclBluetoothDeviceEvent(Manager_OnDeviceFound);
        Manager.OnDiscoveringCompleted += new wclBluetoothResultEvent(Manager_OnDiscoveringCompleted);
        Manager.OnDiscoveringStarted += new wclBluetoothEvent(Manager_OnDiscoveringStarted);
        Manager.OnIoCapabilityRequest += new wclBluetoothIoCapabilityRequestEvent(Manager_OnIoCapabilityRequest);

        Client.OnCharacteristicChanged += new wclGattCharacteristicChangedEvent(Client_OnCharacteristicChanged);
        Client.OnConnect += new wclCommunication.wclClientConnectionConnectEvent(Client_OnConnect);
        Client.OnDisconnect += new wclCommunication.wclClientConnectionDisconnectEvent(Client_OnDisconnect);
        Client.OnConnectionParamsChanged += new EventHandler(Client_OnConnectionParamsChanged);
        Client.OnMaxPduSizeChanged += new EventHandler(Client_OnMaxPduSizeChanged);
        Client.OnConnectionPhyChanged += new EventHandler(Client_OnConnectionPhyChanged);

        cbOpFlag.value = 0;
        cbProtection.value = 0;
        cbParams.value = 0;
        cbWriteKind.value = 3;
        cbSubscribeKind.value = 2;
        cbMitmProtection.value = 0;
        cbIoCap.value = 0;

        Manager.Open();
        Cleanup();
    }

    private void Update()
    {
        FitOpenDropdown(cbMitmProtection);
        FitOpenDropdown(cbIoCap);
        FitOpenDropdown(cbProtection);
        FitOpenDropdown(cbOpFlag);
        FitOpenDropdown(cbParams);
        FitOpenDropdown(cbWriteKind);
        FitOpenDropdown(cbSubscribeKind);

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
        if (Client != null) { try { Client.Disconnect(); } catch { } Client = null; }
        if (Manager != null) { try { Manager.Close(); } catch { } Manager = null; }
        Cleanup();
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

    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio Radio;
        Int32 Res = Manager.GetLeRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Get working radio failed: 0x" + Res.ToString("X8"));
            return null;
        }
        return Radio;
    }

    // ==========================================================
    //  Button handlers (run deferred in Update)
    // ==========================================================
    private void btDiscover_Click()
    {
        wclBluetoothRadio Radio = GetRadio();
        if (Radio != null)
        {
            Int32 Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkBle);
            if (Res != wclErrors.WCL_E_SUCCESS)
            {
                MessageBox.Show("Error starting discovering: 0x" + Res.ToString("X8"),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void btTerminate_Click()
    {
        wclBluetoothRadio Radio = GetRadio();
        if (Radio != null)
        {
            Int32 Res = Radio.Terminate();
            if (Res != wclErrors.WCL_E_SUCCESS)
            {
                MessageBox.Show("Error terminating discovering: 0x" + Res.ToString("X8"),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void btEnumPaired_Click()
    {
        wclBluetoothRadio Radio = GetRadio();
        if (Radio != null)
        {
            lvDevices.Items.Clear();

            Int64[] Devices;
            Int32 Res = Radio.EnumPairedDevices(wclBluetoothDiscoverKind.dkBle, out Devices);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Enum paired failed: 0x" + Res.ToString("X8"));
            else
            {
                if (Devices != null && Devices.Length > 0)
                {
                    foreach (Int64 Device in Devices)
                    {
                        ListViewItem Item = lvDevices.Items.Add(Device.ToString("X12"));
                        Item.Tag = Radio;

                        String Name;
                        Res = Radio.GetRemoteName(Device, out Name);
                        if (Res == wclErrors.WCL_E_SUCCESS)
                            Item.SubItems.Add(Name);
                        else
                            Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));

                        wclBluetoothDeviceType DevType;
                        Res = Radio.GetRemoteDeviceType(Device, out DevType);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                            Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
                        else
                        {
                            switch (DevType)
                            {
                                case wclBluetoothDeviceType.dtClassic: Item.SubItems.Add("Classic"); break;
                                case wclBluetoothDeviceType.dtBle:     Item.SubItems.Add("BLE");     break;
                                case wclBluetoothDeviceType.dtMixed:   Item.SubItems.Add("Mixed");   break;
                                default:                               Item.SubItems.Add("Unknown"); break;
                            }
                        }

                        Boolean Paired;
                        Res = Radio.GetRemotePaired(Device, out Paired);
                        if (Res != wclErrors.WCL_E_SUCCESS)
                            Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
                        else
                            Item.SubItems.Add(Paired ? "True" : "False");
                    }
                }
            }
        }
    }

    private void btPair_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            Int32 Res = ((wclBluetoothRadio)Item.Tag).RemotePair(Convert.ToInt64(Item.Text, 16),
                wclBluetoothPairingMethod.pmLe);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btUnpair_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device");
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            Int32 Res = ((wclBluetoothRadio)Item.Tag).RemoteUnpair(Convert.ToInt64(Item.Text, 16),
                wclBluetoothPairingMethod.pmLe);
            if (Res != wclErrors.WCL_E_SUCCESS)
                MessageBox.Show("Error: 0x" + Res.ToString("X8"));
        }
    }

    private void btConnect_Click()
    {
        if (lvDevices.SelectedItems.Count == 0)
            MessageBox.Show("Select device", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
        {
            ListViewItem Item = lvDevices.SelectedItems[0];
            try
            {
                Client.Address = Convert.ToInt64(Item.Text, 16);
                Client.ConnectOnRead = cbConnectOnRead.isOn;
                Client.ForceNotifications = cbForceIndications.isOn;
                Int32 Res = Client.Connect((wclBluetoothRadio)Item.Tag);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void btDisconnect_Click()
    {
        Int32 Res = Client.Disconnect();
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btGetServices_Click()
    {
        lvServices.Items.Clear();
        FServices = null;

        Int32 Res = Client.ReadServices(OpFlag(), out FServices);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (FServices == null) return;

        foreach (wclGattService Service in FServices)
        {
            String s;
            if (Service.Uuid.IsShortUuid)
                s = Service.Uuid.ShortUuid.ToString("X4");
            else
                s = Service.Uuid.LongUuid.ToString();
            ListViewItem Item = lvServices.Items.Add(s);
            Item.SubItems.Add(Service.Uuid.IsShortUuid.ToString());
            Item.SubItems.Add(Service.Handle.ToString("X4"));
        }
    }

    private void btGetIncludedServices_Click()
    {
        if (lvServices.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select service", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattService Service = FServices[lvServices.SelectedItems[0].Index];

        lvServices.Items.Clear();
        FServices = null;

        Int32 Res = Client.ReadIncludedServices(Service, OpFlag(), out FServices);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (FServices == null) return;

        foreach (wclGattService IncService in FServices)
        {
            String s;
            if (IncService.Uuid.IsShortUuid)
                s = IncService.Uuid.ShortUuid.ToString("X4");
            else
                s = IncService.Uuid.LongUuid.ToString();

            ListViewItem Item = lvServices.Items.Add(s);
            Item.SubItems.Add(IncService.Uuid.IsShortUuid.ToString());
            Item.SubItems.Add(IncService.Handle.ToString("X4"));
        }
    }

    private void btGetCharacteristics_Click()
    {
        FCharacteristics = null;
        lvCharacteristics.Items.Clear();

        if (lvServices.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select service", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattService Service = FServices[lvServices.SelectedItems[0].Index];
        Int32 Res = Client.ReadCharacteristics(Service, OpFlag(), out FCharacteristics);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (FCharacteristics == null) return;

        foreach (wclGattCharacteristic Character in FCharacteristics)
        {
            String s;
            if (Character.Uuid.IsShortUuid)
                s = Character.Uuid.ShortUuid.ToString("X4");
            else
                s = Character.Uuid.LongUuid.ToString();
            ListViewItem Item = lvCharacteristics.Items.Add(s);

            Item.SubItems.Add(Character.Uuid.IsShortUuid.ToString());
            Item.SubItems.Add(Character.ServiceHandle.ToString("X4"));
            Item.SubItems.Add(Character.Handle.ToString("X4"));
            Item.SubItems.Add(Character.ValueHandle.ToString("X4"));
            Item.SubItems.Add(Character.IsBroadcastable.ToString());
            Item.SubItems.Add(Character.IsReadable.ToString());
            Item.SubItems.Add(Character.IsWritable.ToString());
            Item.SubItems.Add(Character.IsWritableWithoutResponse.ToString());
            Item.SubItems.Add(Character.IsSignedWritable.ToString());
            Item.SubItems.Add(Character.IsNotifiable.ToString());
            Item.SubItems.Add(Character.IsIndicatable.ToString());
            Item.SubItems.Add(Character.HasExtendedProperties.ToString());
        }
    }

    private void btGetCharValue_Click()
    {
        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];
        Byte[] Value;
        Int32 Res = Client.ReadCharacteristicValue(Characteristic, OpFlag(), out Value, Protection());
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (Value == null) return;

        if (Value.Length == 0)
            MessageBox.Show("Value is empty", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
        {
            String Str = "";
            for (Int32 i = 0; i < Value.Length; i++)
                Str = Str + Value[i].ToString("X2");

            MessageBox.Show("Value: " + Str, "Characterist value", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void btSetValue_Click()
    {
        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];

        String Str = edCharVal.text;
        if (Str.Length % 2 != 0)
            Str = "0" + Str;

        Byte[] Val = new Byte[Str.Length / 2];
        for (Int32 i = 0; i < Val.Length; i++)
        {
            String b = Str.Substring(i * 2, 2);
            Val[i] = Convert.ToByte(b, 16);
        }

        Int32 Res;
        wclGattWriteKind WriteKind;
        switch (cbWriteKind.value)
        {
            case 0:  WriteKind = wclGattWriteKind.wkWithResponse;    break;
            case 1:  WriteKind = wclGattWriteKind.wkWithoutResponse; break;
            case 2:  WriteKind = wclGattWriteKind.wkAutoSplit;       break;
            default: WriteKind = wclGattWriteKind.wkAuto;            break;
        }

        Res = Client.WriteCharacteristicValue(Characteristic, Val, Protection(), WriteKind);
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btGetDesc_Click()
    {
        FDescriptors = null;
        lvDescriptors.Items.Clear();

        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];
        Int32 Res = Client.ReadDescriptors(Characteristic, OpFlag(), out FDescriptors);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (FDescriptors == null) return;

        foreach (wclGattDescriptor Descriptor in FDescriptors)
        {
            String s;
            if (Descriptor.Uuid.IsShortUuid)
                s = Descriptor.Uuid.ShortUuid.ToString("X4");
            else
                s = Descriptor.Uuid.LongUuid.ToString();
            ListViewItem Item = lvDescriptors.Items.Add(s);

            Item.SubItems.Add(Descriptor.Uuid.IsShortUuid.ToString());
            Item.SubItems.Add(Descriptor.ServiceHandle.ToString("X4"));
            Item.SubItems.Add(Descriptor.CharacteristicHandle.ToString("X4"));
            Item.SubItems.Add(Descriptor.Handle.ToString("X4"));
            switch (Descriptor.DescriptorType)
            {
                case wclGattDescriptorType.dtCharacteristicExtendedProperties:
                    Item.SubItems.Add("dtCharacteristicExtendedProperties"); break;
                case wclGattDescriptorType.dtCharacteristicUserDescription:
                    Item.SubItems.Add("dtCharacteristicUserDescription"); break;
                case wclGattDescriptorType.dtClientCharacteristicConfiguration:
                    Item.SubItems.Add("dtClientCharacteristicConfiguration"); break;
                case wclGattDescriptorType.dtServerCharacteristicConfiguration:
                    Item.SubItems.Add("dtServerCharacteristicConfiguration"); break;
                case wclGattDescriptorType.dtCharacteristicFormat:
                    Item.SubItems.Add("dtCharacteristicFormat"); break;
                case wclGattDescriptorType.dtCharacteristicAggregateFormat:
                    Item.SubItems.Add("dtCharacteristicAggregateFormat"); break;
                default:
                    Item.SubItems.Add("dtCustomDescriptor"); break;
            }
        }
    }

    private void btGetDescVal_Click()
    {
        lvDescriptorValue.Items.Clear();

        if (lvDescriptors.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select descriptor", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattDescriptor Descriptor = FDescriptors[lvDescriptors.SelectedItems[0].Index];
        wclGattDescriptorValue Value;
        Int32 Res = Client.ReadDescriptorValue(Descriptor, OpFlag(), out Value, Protection());
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else
        {
            switch (Value.AType)
            {
                case wclGattDescriptorType.dtCharacteristicExtendedProperties:
                    ListProp("Type", "dtCharacteristicExtendedProperties");
                    ListProp("IsReliableWriteEnabled", Value.CharacteristicExtendedProperties.IsReliableWriteEnabled.ToString());
                    ListProp("IsAuxiliariesWritable", Value.CharacteristicExtendedProperties.IsAuxiliariesWritable.ToString());
                    break;

                case wclGattDescriptorType.dtCharacteristicUserDescription:
                    ListProp("Type", "dtCharacteristicUserDescription");
                    ListProp("Descriptipon", Value.UserDescription.Description);
                    break;

                case wclGattDescriptorType.dtClientCharacteristicConfiguration:
                    ListProp("Type", "dtClientCharacteristicConfiguration");
                    ListProp("IsSubscribeToNotification", Value.ClientCharacteristicConfiguration.IsSubscribeToNotification.ToString());
                    ListProp("IsSubscribeToIndication", Value.ClientCharacteristicConfiguration.IsSubscribeToIndication.ToString());
                    break;

                case wclGattDescriptorType.dtServerCharacteristicConfiguration:
                    ListProp("Type", "dtServerCharacteristicConfiguration");
                    ListProp("IsBroadcast", Value.ServerCharacteristicConfiguration.IsBroadcast.ToString());
                    break;

                case wclGattDescriptorType.dtCharacteristicFormat:
                    ListProp("Type", "dtCharacteristicFormat");
                    ListProp("Format", Value.CharacteristicFormat.Format.ToString());
                    ListProp("Exponent", Value.CharacteristicFormat.Exponent.ToString("X2"));
                    ListProp("Unit", Value.CharacteristicFormat.AUnit.ToString("X4"));
                    ListProp("NameSpace", Value.CharacteristicFormat.NameSpace.ToString("X2"));
                    ListProp("Description", Value.CharacteristicFormat.Description.ToString("X4"));
                    break;

                case wclGattDescriptorType.dtCharacteristicAggregateFormat:
                    ListProp("Type", "dtCharacteristicAggregateFormat");
                    if (Value.CharacteristicAggregateFormat.Handles != null && Value.CharacteristicAggregateFormat.Handles.Length > 0)
                    {
                        for (Int32 i = 0; i < Value.CharacteristicAggregateFormat.Handles.Length; i++)
                        {
                            ListProp("Handle [" + i.ToString() + "]", Value.CharacteristicAggregateFormat.Handles[i].ToString("X4"));
                        }
                    }
                    break;

                default:
                    ListProp("Type", "UNKNOWN");
                    break;
            }

            if (Value.Data != null)
            {
                String Str = "";
                for (Int32 i = 0; i < Value.Data.Length; i++)
                    Str = Str + Value.Data[i].ToString("X2");
                ListProp("Data", Str);
            }
        }
    }

    private void ListProp(String Name, String Value)
    {
        ListViewItem Item = lvDescriptorValue.Items.Add(Name);
        Item.SubItems.Add(Value);
    }

    private void btSubscribe_Click()
    {
        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];

        wclGattSubscribeKind SubscribeKind;
        switch (cbSubscribeKind.value)
        {
            case 0:  SubscribeKind = wclGattSubscribeKind.skNotification; break;
            case 1:  SubscribeKind = wclGattSubscribeKind.skIndication;   break;
            default: SubscribeKind = wclGattSubscribeKind.skManual;       break;
        }

        if (SubscribeKind == wclGattSubscribeKind.skManual)
        {
            if (Characteristic.IsNotifiable && Characteristic.IsIndicatable)
                Characteristic.IsIndicatable = false;
        }

        Int32 Res;
        if (cbFastSubscribe.isOn)
        {
            if (SubscribeKind == wclGattSubscribeKind.skManual)
                Res = Client.SubscribeForNotifications(Characteristic);
            else
                Res = Client.SubscribeForNotifications(Characteristic, OpFlag(), Protection(), SubscribeKind);
        }
        else
        {
            if (SubscribeKind == wclGattSubscribeKind.skManual)
                Res = Client.Subscribe(Characteristic);
            else
                Res = Client.Subscribe(Characteristic, SubscribeKind);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btUnsubscribe_Click()
    {
        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];

        wclGattSubscribeKind SubscribeKind;
        switch (cbSubscribeKind.value)
        {
            case 0:  SubscribeKind = wclGattSubscribeKind.skNotification; break;
            case 1:  SubscribeKind = wclGattSubscribeKind.skIndication;   break;
            default: SubscribeKind = wclGattSubscribeKind.skManual;       break;
        }

        if (SubscribeKind == wclGattSubscribeKind.skManual)
        {
            if (Characteristic.IsNotifiable && Characteristic.IsIndicatable)
                Characteristic.IsIndicatable = false;
        }

        Int32 Res;
        if (cbFastSubscribe.isOn)
        {
            if (SubscribeKind == wclGattSubscribeKind.skManual)
                Res = Client.UnsubscribeFromNotifications(Characteristic);
            else
                Res = Client.UnsubscribeFromNotifications(Characteristic, OpFlag(), Protection(), SubscribeKind);
        }
        else
        {
            if (SubscribeKind == wclGattSubscribeKind.skManual)
                Res = Client.Unsubscribe(Characteristic);
            else
                Res = Client.Unsubscribe(Characteristic, SubscribeKind);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btWriteSubsc_Click()
    {
        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];

        wclGattSubscribeKind SubscribeKind;
        switch (cbSubscribeKind.value)
        {
            case 0:  SubscribeKind = wclGattSubscribeKind.skNotification; break;
            case 1:  SubscribeKind = wclGattSubscribeKind.skIndication;   break;
            default: SubscribeKind = wclGattSubscribeKind.skManual;       break;
        }

        Int32 Res;
        if (SubscribeKind == wclGattSubscribeKind.skManual)
        {
            if (Characteristic.IsNotifiable && Characteristic.IsIndicatable)
                Characteristic.IsIndicatable = false;
            Res = Client.WriteClientConfiguration(Characteristic, true, OpFlag(), Protection());
        }
        else
        {
            Res = Client.WriteClientConfiguration(Characteristic, true, OpFlag(), Protection(), SubscribeKind);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btWriteUnsubsc_Click()
    {
        if (lvCharacteristics.SelectedItems.Count == 0)
        {
            MessageBox.Show("Select characteristic", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        wclGattCharacteristic Characteristic = FCharacteristics[lvCharacteristics.SelectedItems[0].Index];

        wclGattSubscribeKind SubscribeKind;
        switch (cbSubscribeKind.value)
        {
            case 0:  SubscribeKind = wclGattSubscribeKind.skNotification; break;
            case 1:  SubscribeKind = wclGattSubscribeKind.skIndication;   break;
            default: SubscribeKind = wclGattSubscribeKind.skManual;       break;
        }

        Int32 Res;
        if (SubscribeKind == wclGattSubscribeKind.skManual)
        {
            if (Characteristic.IsNotifiable && Characteristic.IsIndicatable)
                Characteristic.IsIndicatable = false;
            Res = Client.WriteClientConfiguration(Characteristic, false, OpFlag(), Protection());
        }
        else
        {
            Res = Client.WriteClientConfiguration(Characteristic, false, OpFlag(), Protection(), SubscribeKind);
        }

        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btGetParams_Click()
    {
        TraceEvent(Client.Address, "Connection params", "", "");
        GetConnectionParams();
    }

    private void btSetParams_Click()
    {
        Int32 Res;
        if (cbParams.value == 3)
        {
            wclBluetoothLeConnectionParametersValue ParamsValue = new wclBluetoothLeConnectionParametersValue();
            ParamsValue.MinInterval = 84;
            ParamsValue.MaxInterval = 84;
            ParamsValue.Latency = 0;
            ParamsValue.LinkTimeout = 800;
            Res = Client.SetConnectionParams(ParamsValue);
        }
        else
        {
            wclBluetoothLeConnectionParametersType Params;
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
            MessageBox.Show("Error: 0x" + Res.ToString("X8"));
    }

    private void btGetMaxPduSize_Click()
    {
        TraceEvent(Client.Address, "Max PDU", "", "");
        GetMaxPduSize();
    }

    private void btPhy_Click()
    {
        TraceEvent(Client.Address, "Connection PHY", "", "");
        GetConnectionPhy();
    }

    private void btClear_Click()
    {
        lvEvents.Items.Clear();
    }

    private void cbFastSubscribe_CheckedChanged()
    {
        btWriteSubsc.interactable = !cbFastSubscribe.isOn;
        btWriteUnsubsc.interactable = !cbFastSubscribe.isOn;
    }

    // ==========================================================
    //  Helpers (called from deferred handlers)
    // ==========================================================
    private void Cleanup()
    {
        FCharacteristics = null;
        FDescriptors = null;
        FServices = null;

        lvServices.Items.Clear();
        lvCharacteristics.Items.Clear();
        lvDescriptors.Items.Clear();
        lvDescriptorValue.Items.Clear();
    }

    private void TraceEvent(Int64 Address, String Event, String Param, String Value)
    {
        String s = "";
        if (Address != 0)
            s = Address.ToString("X12");
        ListViewItem Item = lvEvents.Items.Add(s);
        Item.SubItems.Add(Event);
        Item.SubItems.Add(Param);
        Item.SubItems.Add(Value);
    }

    private wclGattOperationFlag OpFlag()
    {
        switch (cbOpFlag.value)
        {
            case 1:  return wclGattOperationFlag.goReadFromDevice;
            case 2:  return wclGattOperationFlag.goReadFromCache;
            default: return wclGattOperationFlag.goNone;
        }
    }

    private wclGattProtectionLevel Protection()
    {
        switch (cbProtection.value)
        {
            case 0:  return wclGattProtectionLevel.plNone;
            case 1:  return wclGattProtectionLevel.plAuthentication;
            case 2:  return wclGattProtectionLevel.plEncryption;
            case 3:  return wclGattProtectionLevel.plEncryptionAndAuthentication;
            default: return wclGattProtectionLevel.plNone;
        }
    }

    private void GetMaxPduSize()
    {
        UInt16 Size;
        Int32 Res = Client.GetMaxPduSize(out Size);
        if (Res != wclErrors.WCL_E_SUCCESS)
            TraceEvent(Client.Address, "", "Error", Res.ToString("X8"));
        else
            TraceEvent(Client.Address, "", "Size", Size.ToString());
    }

    private void GetConnectionParams()
    {
        wclBluetoothLeConnectionParameters Params;
        Int32 Res = Client.GetConnectionParams(out Params);
        if (Res != wclErrors.WCL_E_SUCCESS)
            TraceEvent(0, "", "Error", Res.ToString("X8"));
        else
        {
            TraceEvent(0, "", "Connection interval", Params.Interval.ToString());
            TraceEvent(0, "", "Connection latency", Params.Latency.ToString());
            TraceEvent(0, "", "Link timeout", Params.LinkTimeout.ToString());
        }
    }

    private void GetConnectionPhy()
    {
        wclBluetoothLeConnectionPhy Phy;
        Int32 Res = Client.GetConnectionPhyInfo(out Phy);
        if (Res != wclErrors.WCL_E_SUCCESS)
            TraceEvent(0, "", "Error", Res.ToString("X8"));
        else
        {
            TraceEvent(0, "Transmit PHY", "", "");
            TraceEvent(0, "", "Coded", Phy.Transmit.IsCoded.ToString());
            TraceEvent(0, "", "1M", Phy.Transmit.IsUncoded1MPhy.ToString());
            TraceEvent(0, "", "2M", Phy.Transmit.IsUncoded2MPhy.ToString());

            TraceEvent(0, "Receive PHY", "", "");
            TraceEvent(0, "", "Coded", Phy.Receive.IsCoded.ToString());
            TraceEvent(0, "", "1M", Phy.Receive.IsUncoded1MPhy.ToString());
            TraceEvent(0, "", "2M", Phy.Receive.IsUncoded2MPhy.ToString());
        }
    }

    // ==========================================================
    //  wcl Bluetooth manager events (dispatch UI side effects)
    // ==========================================================
    private void Manager_OnIoCapabilityRequest(object Sender, wclBluetoothRadio Radio, Int64 Address,
        out wclBluetoothMitmProtection Mitm, out wclBluetoothIoCapability IoCapability, out Boolean OobPresent)
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

        OobPresent = false;
    }

    private void Manager_OnNumericComparison(object Sender, wclBluetoothRadio Radio, Int64 Address,
        UInt32 Number, out Boolean Confirm)
    {
        Confirm = true;
        Int64 addr = Address; uint n = Number;
        Dispatch(() => TraceEvent(addr, "Numeric comparison", "Number", n.ToString()));
    }

    private void Manager_OnPasskeyNotification(object Sender, wclBluetoothRadio Radio, Int64 Address, UInt32 Passkey)
    {
        Int64 addr = Address; uint p = Passkey;
        Dispatch(() => TraceEvent(addr, "Passkey notification", "Passkey", p.ToString()));
    }

    private void Manager_OnPasskeyRequest(object Sender, wclBluetoothRadio Radio, Int64 Address, out UInt32 Passkey)
    {
        Passkey = 123456;
        Int64 addr = Address; uint p = Passkey;
        Dispatch(() => TraceEvent(addr, "Passkey request", "Passkey", p.ToString()));
    }

    private void Manager_OnPinRequest(object Sender, wclBluetoothRadio Radio, Int64 Address, out String Pin)
    {
        Pin = "123456";
        Int64 addr = Address; string pin = Pin;
        Dispatch(() => TraceEvent(addr, "Pin request", "PIN", pin));
    }

    private void Manager_OnConfirm(object Sender, wclBluetoothRadio Radio, Int64 Address, out Boolean Confirm)
    {
        Confirm = true;
        Int64 addr = Address;
        Dispatch(() => TraceEvent(addr, "Just works pairing", "Accept", "True"));
    }

    private void Manager_OnAuthenticationCompleted(object Sender, wclBluetoothRadio Radio, Int64 Address, Int32 Error)
    {
        Int64 addr = Address; int err = Error;
        Dispatch(() => TraceEvent(addr, "Authentication completed", "Error", err.ToString("X8")));
    }

    private void Manager_OnProtectionLevelRequest(object Sender, wclBluetoothRadio Radio, Int64 Address,
        out wclBluetoothLeProtectionLevel Protection)
    {
        switch (cbProtection.value)
        {
            case 0:  Protection = wclBluetoothLeProtectionLevel.pplNone;                          break;
            case 1:  Protection = wclBluetoothLeProtectionLevel.pplDefault;                       break;
            case 2:  Protection = wclBluetoothLeProtectionLevel.pplEncryption;                    break;
            case 3:  Protection = wclBluetoothLeProtectionLevel.pplEncryptionAndAuthentication;   break;
            default: Protection = wclBluetoothLeProtectionLevel.pplDefault;                       break;
        }
    }

    private void Manager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, Int64 Address)
    {
        Dispatch(() =>
        {
            ListViewItem Item = lvDevices.Items.Add(Address.ToString("X12"));
            Item.SubItems.Add("");
            Item.Tag = Radio;

            wclBluetoothDeviceType DevType;
            Int32 Res = Radio.GetRemoteDeviceType(Address, out DevType);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
            else
            {
                switch (DevType)
                {
                    case wclBluetoothDeviceType.dtClassic: Item.SubItems.Add("Classic"); break;
                    case wclBluetoothDeviceType.dtBle:     Item.SubItems.Add("BLE");     break;
                    case wclBluetoothDeviceType.dtMixed:   Item.SubItems.Add("Mixed");   break;
                    default:                               Item.SubItems.Add("Unknown"); break;
                }
            }

            Boolean Paired;
            Res = Radio.GetRemotePaired(Address, out Paired);
            if (Res != wclErrors.WCL_E_SUCCESS)
                Item.SubItems.Add("Error: 0x" + Res.ToString("X8"));
            else
                Item.SubItems.Add(Paired ? "True" : "False");

            TraceEvent(Address, "Device found", "", "");
        });
    }

    private void Manager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, Int32 Error)
    {
        Dispatch(() =>
        {
            if (lvDevices.Items.Count == 0)
                MessageBox.Show("No BLE devices were found.", "Discovering for BLE devices", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                for (Int32 i = 0; i < lvDevices.Items.Count; i++)
                {
                    ListViewItem Item = lvDevices.Items[i];

                    Int64 Address = Convert.ToInt64(Item.Text, 16);
                    String DevName;
                    Int32 Res = Radio.GetRemoteName(Address, out DevName);
                    if (Res != wclErrors.WCL_E_SUCCESS)
                        Item.SubItems[1].Text = "Error: 0x" + Res.ToString("X8");
                    else
                        Item.SubItems[1].Text = DevName;
                }
            }

            TraceEvent(0, "Discovering completed", "", "");
        });
    }

    private void Manager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
    {
        Dispatch(() =>
        {
            lvDevices.Items.Clear();
            TraceEvent(0, "Discovering started", "", "");
        });
    }

    // ==========================================================
    //  wcl GATT client events (dispatch UI side effects)
    // ==========================================================
    private void Client_OnCharacteristicChanged(object Sender, UInt16 Handle, Byte[] Value)
    {
        Int64 addr = ((wclGattClient)Sender).Address;
        Dispatch(() =>
        {
            TraceEvent(addr, "ValueChanged", "Handle", Handle.ToString("X4"));
            if (Value == null)
                TraceEvent(0, "", "Value", "");
            else
            {
                if (Value.Length == 0)
                    TraceEvent(0, "", "Value", "");
                else
                {
                    String Str = "";
                    for (Int32 i = 0; i < Value.Length; i++)
                        Str = Str + Value[i].ToString("X2");
                    TraceEvent(0, "", "Value", Str);
                }
            }
        });
    }

    private void Client_OnConnect(object Sender, Int32 Error)
    {
        Int64 addr = ((wclGattClient)Sender).Address;
        Dispatch(() =>
        {
            TraceEvent(addr, "Connected", "Error", "0x" + Error.ToString("X8"));

            if (Error == wclErrors.WCL_E_SUCCESS)
            {
                TraceEvent(Client.Address, "Max PDU", "", "");
                GetMaxPduSize();

                TraceEvent(Client.Address, "Connection params", "", "");
                GetConnectionParams();

                TraceEvent(Client.Address, "Connection PHY", "", "");
                GetConnectionPhy();
            }
        });
    }

    private void Client_OnDisconnect(object Sender, Int32 Reason)
    {
        Int64 addr = ((wclGattClient)Sender).Address;
        Dispatch(() =>
        {
            TraceEvent(addr, "Disconnected", "Reason", "0x" + Reason.ToString("X8"));
            Cleanup();
        });
    }

    private void Client_OnConnectionParamsChanged(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            TraceEvent(Client.Address, "Connection params changed", "", "");
            GetConnectionParams();
        });
    }

    private void Client_OnMaxPduSizeChanged(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            TraceEvent(Client.Address, "Max PDU size changed", "", "");
            GetMaxPduSize();
        });
    }

    private void Client_OnConnectionPhyChanged(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            TraceEvent(Client.Address, "Connection PHY changed", "", "");
            GetConnectionPhy();
        });
    }
}