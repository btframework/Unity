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
public class WiiRemoteController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btDiscover;
    public Button btConnect;
    public Button btDisconnect;
    public Button btGetLeds;
    public Button btGetStatus;
    public Button btSetLeds;
    public Button btRumbleOn;
    public Button btRumbleOff;
    public Button btEnableAccel;
    public Button btDisableAccel;
    public Button btEnableIr;
    public Button btDisableIr;
    public Button btClear;

    [Header("LEDs")]
    public Toggle cbLed1;
    public Toggle cbLed2;
    public Toggle cbLed3;
    public Toggle cbLed4;

    [Header("Buttons")]
    public Toggle cbA;
    public Toggle cbB;
    public Toggle cbPlus;
    public Toggle cbHome;
    public Toggle cbMinus;
    public Toggle cbOne;
    public Toggle cbTwo;
    public Toggle cbUp;
    public Toggle cbDown;
    public Toggle cbLeft;
    public Toggle cbRight;

    [Header("Nunchuk buttons")]
    public Toggle cbNunchukC;
    public Toggle cbNunchukZ;

    [Header("Battery / Accel")]
    public TextMeshProUGUI laBattLevel;
    public TextMeshProUGUI laX;
    public TextMeshProUGUI laY;
    public TextMeshProUGUI laZ;

    [Header("IR")]
    public TextMeshProUGUI laIrMode;
    public TextMeshProUGUI laIrSensitivity;

    public TextMeshProUGUI laSensor0Position;
    public TextMeshProUGUI laSensor0Size;
    public TextMeshProUGUI laSensor0Found;
    public TextMeshProUGUI laSensor1Position;
    public TextMeshProUGUI laSensor1Size;
    public TextMeshProUGUI laSensor1Found;
    public TextMeshProUGUI laSensor2Position;
    public TextMeshProUGUI laSensor2Size;
    public TextMeshProUGUI laSensor2Found;
    public TextMeshProUGUI laSensor3Position;
    public TextMeshProUGUI laSensor3Size;
    public TextMeshProUGUI laSensor3Found;

    [Header("Nunchuk values")]
    public TextMeshProUGUI laNunchukState;
    public TextMeshProUGUI laNunchukX;
    public TextMeshProUGUI laNunchukY;
    public TextMeshProUGUI laNunchukZ;
    public TextMeshProUGUI laNunchukJoyX;
    public TextMeshProUGUI laNunchukJoyY;

    [Header("Balance board")]
    public TextMeshProUGUI laBalanceBoardState;
    public TextMeshProUGUI laBbTopRight;
    public TextMeshProUGUI laBbTopLeft;
    public TextMeshProUGUI laBbBottomRight;
    public TextMeshProUGUI laBbBottomLeft;

    [Header("ListView / Log")]
    public RectTransform lvDevicesContent;
    public TextMeshProUGUI lbLogText;

    private ListView lvDevices;
    private ListBox  lbLog;

    private wclBluetoothManager FManager;
    private wclWiiRemoteClient FClient;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvDevices = new ListView(lvDevicesContent, new float[] { 150, 150 });
        lbLog     = new ListBox(lbLogText);

        btDiscover.onClick.AddListener(() => Dispatch(btDiscover_Click));
        btConnect.onClick.AddListener(() => Dispatch(btConnect_Click));
        btDisconnect.onClick.AddListener(() => Dispatch(btDisconnect_Click));
        btGetLeds.onClick.AddListener(() => Dispatch(btGetLeds_Click));
        btGetStatus.onClick.AddListener(() => Dispatch(btGetStatus_Click));
        btSetLeds.onClick.AddListener(() => Dispatch(btSetLeds_Click));
        btRumbleOn.onClick.AddListener(() => Dispatch(btRumbleOn_Click));
        btRumbleOff.onClick.AddListener(() => Dispatch(btRumbleOff_Click));
        btEnableAccel.onClick.AddListener(() => Dispatch(btEnableAccel_Click));
        btDisableAccel.onClick.AddListener(() => Dispatch(btDisableAccel_Click));
        btEnableIr.onClick.AddListener(() => Dispatch(btEnableIr_Click));
        btDisableIr.onClick.AddListener(() => Dispatch(btDisableIr_Click));
        btClear.onClick.AddListener(btClear_Click);
    }

    private void Start()
    {
        Dispatch(DoStart);
    }

    private void DoStart()
    {
        FManager = new wclBluetoothManager();
        FManager.OnDiscoveringStarted += new wclBluetoothEvent(FManager_OnDiscoveringStarted);
        FManager.OnDeviceFound += new wclBluetoothDeviceEvent(FManager_OnDeviceFound);
        FManager.OnDiscoveringCompleted += new wclBluetoothResultEvent(FManager_OnDiscoveringCompleted);

        FClient = new wclWiiRemoteClient();
        FClient.OnConnect += new wclClientConnectionConnectEvent(FClient_OnConnect);
        FClient.OnDisconnect += new wclClientConnectionDisconnectEvent(FClient_OnDisconnect);
        FClient.OnStatusChanged += new wclWiiRemoteStatusChangedEvent(FClient_OnStatusChanged);
        FClient.OnButtonsChanged += new wclWiiRemoteButtonsChangedEvent(FClient_OnButtonsChanged);
        FClient.OnAccelChanged += new wclWiiRemoteAccelChangedEvent(FClient_OnAccelChanged);
        FClient.OnIrChanged += new wclWiiRemoteIrChangedEvent(FClient_OnIrChanged);
        FClient.OnExtensionAttached += new EventHandler(FClient_OnExtensionAttached);
        FClient.OnExtensionDetached += new EventHandler(FClient_OnExtensionDetached);
        FClient.OnNunchukChanged += new wclWiiRemoteNunchukChangedEvent(FClient_OnNunchukChanged);
        FClient.OnBalanceBoardChanged += new wclWiiRemoteBalanceBoardChangedEvent(FClient_OnBalanceBoardChanged);

        FManager.Open();
        ClearAccel();
        ClearIr();
        ClearExtensions();
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
        if (FClient != null)  { try { FClient.Disconnect(); } catch { } }
        if (FManager != null) { try { FManager.Close(); } catch { } }
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
        Int32 Res = FManager.GetClassicRadio(out Radio);
        if (Res != wclErrors.WCL_E_SUCCESS)
        {
            ShowMessageBox("Get working radio failed: 0x" + Res.ToString("X8"));
            return null;
        }
        return Radio;
    }

    // ==========================================================
    //  Manager events
    // ==========================================================
    private void FManager_OnDiscoveringStarted(object Sender, wclBluetoothRadio Radio)
    {
        Dispatch(() =>
        {
            lvDevices.Items.Clear();
            Trace("Discovering started");
        });
    }

    private void FManager_OnDeviceFound(object Sender, wclBluetoothRadio Radio, long Address)
    {
        Dispatch(() =>
        {
            ListViewItem Item = lvDevices.Items.Add(Address.ToString("X12"));
            Item.SubItems.Add("");
        });
    }

    private void FManager_OnDiscoveringCompleted(object Sender, wclBluetoothRadio Radio, int Error)
    {
        Dispatch(() =>
        {
            for (int i = 0; i < lvDevices.Items.Count; i++)
            {
                ListViewItem Item = lvDevices.Items[i];
                Int64 Addr = Convert.ToInt64(Item.Text, 16);

                String Name;
                Int32 Res = Radio.GetRemoteName(Addr, out Name);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    Name = "Error: 0x" + Res.ToString("X9"); // 1:1 (X9 as in original)
                Item.SubItems[1].Text = Name;
            }

            Trace("Discovering completed with result", Error);
        });
    }

    // ==========================================================
    //  Wii Remote client events
    // ==========================================================
    private void FClient_OnConnect(object Sender, int Error)
    {
        Dispatch(() =>
        {
            if (Error != wclErrors.WCL_E_SUCCESS)
                Trace("Connection failed", Error);
            else
            {
                Trace("Connected");
                SetLeds();
            }
        });
    }

    private void FClient_OnDisconnect(object Sender, int Reason)
    {
        Dispatch(() =>
        {
            Trace("Disconnected. Reason", Reason);

            laBattLevel.text = "0.00";
            cbLed1.isOn = false;
            cbLed2.isOn = false;
            cbLed3.isOn = false;
            cbLed4.isOn = false;

            cbA.isOn = false;
            cbB.isOn = false;
            cbPlus.isOn = false;
            cbHome.isOn = false;
            cbMinus.isOn = false;
            cbOne.isOn = false;
            cbTwo.isOn = false;
            cbUp.isOn = false;
            cbDown.isOn = false;
            cbLeft.isOn = false;
            cbRight.isOn = false;

            ClearAccel();
            ClearIr();
            ClearExtensions();
        });
    }

    private void FClient_OnStatusChanged(object Sender, double Batt, wclWiiRemoteLeds Leds)
    {
        string battStr = Batt.ToString("N2");
        bool l1 = Leds.Led1, l2 = Leds.Led2, l3 = Leds.Led3, l4 = Leds.Led4;

        Dispatch(() =>
        {
            Trace("Status changed");
            laBattLevel.text = battStr;
            cbLed1.isOn = l1;
            cbLed2.isOn = l2;
            cbLed3.isOn = l3;
            cbLed4.isOn = l4;
        });
    }

    private void FClient_OnButtonsChanged(object Sender, wclWiiRemoteButtons Buttons)
    {
        bool a = Buttons.A;
        bool b = Buttons.B;
        bool plus = Buttons.Plus;
        bool home = Buttons.Home;
        bool minus = Buttons.Minus;
        bool one = Buttons.One;
        bool two = Buttons.Two;
        bool up = Buttons.Up;
        bool down = Buttons.Down;
        bool left = Buttons.Left;
        bool right = Buttons.Right;

        Dispatch(() =>
        {
            cbA.isOn = a;
            cbB.isOn = b;
            cbPlus.isOn = plus;
            cbHome.isOn = home;
            cbMinus.isOn = minus;
            cbOne.isOn = one;
            cbTwo.isOn = two;
            cbUp.isOn = up;
            cbDown.isOn = down;
            cbLeft.isOn = left;
            cbRight.isOn = right;
        });
    }

    private void FClient_OnAccelChanged(object Sender, wclWiiRemoteAccel Accel)
    {
        string xs = Accel.Values.X.ToString();
        string ys = Accel.Values.Y.ToString();
        string zs = Accel.Values.Z.ToString();

        Dispatch(() =>
        {
            laX.text = xs;
            laY.text = ys;
            laZ.text = zs;
        });
    }

    private void FClient_OnIrChanged(object Sender, wclWiiRemoteIrSensors Ir)
    {
        // Capture values into local strings synchronously.
        string modeStr = null;
        switch (Ir.Mode)
        {
            case wclWiiRemoteIrMode.wiiIrBasicMode:    modeStr = "Basic";    break;
            case wclWiiRemoteIrMode.wiiIrExtendedMode: modeStr = "Extended"; break;
            case wclWiiRemoteIrMode.wiiIrFullMode:     modeStr = "Full";     break;
            case wclWiiRemoteIrMode.wiiIrOff:          modeStr = null;       break;
            default:                                   modeStr = "Unknown";  break;
        }

        bool clearIr = (Ir.Mode == wclWiiRemoteIrMode.wiiIrOff);

        string sensStr = null;
        if (!clearIr)
        {
            switch (Ir.Sensitivity)
            {
                case wclWiiRemoteIrSensitivity.wiiIrLevelOff: sensStr = "Off";     break;
                case wclWiiRemoteIrSensitivity.wiiIrLevel1:   sensStr = "Level 1"; break;
                case wclWiiRemoteIrSensitivity.wiiIrLevel2:   sensStr = "Level 2"; break;
                case wclWiiRemoteIrSensitivity.wiiIrLevel3:   sensStr = "Level 3"; break;
                case wclWiiRemoteIrSensitivity.wiiIrLevel4:   sensStr = "Level 4"; break;
                case wclWiiRemoteIrSensitivity.wiiIrLevel5:   sensStr = "Level 5"; break;
                case wclWiiRemoteIrSensitivity.wiiIrLevelMax: sensStr = "Maximum"; break;
                default:                                      sensStr = "Unknown"; break;
            }
        }

        string[] positions = new string[4];
        string[] sizes     = new string[4];
        string[] founds    = new string[4];
        if (!clearIr)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Ir.Sensors[i].Found)
                {
                    positions[i] = Ir.Sensors[i].Position.X.ToString() + "; " + Ir.Sensors[i].Position.Y.ToString();
                    sizes[i]     = Ir.Sensors[i].Size.ToString();
                    founds[i]    = "True";
                }
                else
                {
                    positions[i] = "0; 0";
                    sizes[i]     = "0";
                    founds[i]    = "False";
                }
            }
        }

        Dispatch(() =>
        {
            if (clearIr)
            {
                ClearIr();
            }
            else
            {
                if (modeStr != null) laIrMode.text = modeStr;
                if (sensStr != null) laIrSensitivity.text = sensStr;

                laSensor0Position.text = positions[0];
                laSensor0Size.text     = sizes[0];
                laSensor0Found.text    = founds[0];
                laSensor1Position.text = positions[1];
                laSensor1Size.text     = sizes[1];
                laSensor1Found.text    = founds[1];
                laSensor2Position.text = positions[2];
                laSensor2Size.text     = sizes[2];
                laSensor2Found.text    = founds[2];
                laSensor3Position.text = positions[3];
                laSensor3Size.text     = sizes[3];
                laSensor3Found.text    = founds[3];
            }
        });
    }

    private void FClient_OnExtensionAttached(object sender, EventArgs e)
    {
        string ext = null;
        bool nunchukAttached = false;
        bool balanceAttached = false;

        switch (FClient.Extension)
        {
            case wclWiiRemoteExtension.wiiNoExtension:        ext = "wiiNoExtension";        break;
            case wclWiiRemoteExtension.wiiNunchuk:            ext = "wiiNunchuk";            nunchukAttached = true; break;
            case wclWiiRemoteExtension.wiiClassicController:  ext = "wiiClassicController";  break;
            case wclWiiRemoteExtension.wiiGuitar:             ext = "wiiGuitar";             break;
            case wclWiiRemoteExtension.wiiDrums:              ext = "wiiDrums";              break;
            case wclWiiRemoteExtension.wiiBalanceBoard:       ext = "wiiBalanceBoard";       balanceAttached = true; break;
            default:                                          ext = "Unknonw";               break;
        }

        Dispatch(() =>
        {
            if (nunchukAttached) laNunchukState.text = "Attached";
            if (balanceAttached) laBalanceBoardState.text = "Attached";
            Trace("Extension attached: " + ext);
        });
    }

    private void FClient_OnExtensionDetached(object sender, EventArgs e)
    {
        Dispatch(() =>
        {
            Trace("Extension detached");
            ClearExtensions();
        });
    }

    private void FClient_OnNunchukChanged(object Sender, wclWiiRemoteNunchuk Nunchuk)
    {
        string xs = Nunchuk.Accel.Values.X.ToString();
        string ys = Nunchuk.Accel.Values.Y.ToString();
        string zs = Nunchuk.Accel.Values.Z.ToString();
        string jxs = Nunchuk.Joystick.X.ToString();
        string jys = Nunchuk.Joystick.Y.ToString();
        bool cs = Nunchuk.C;
        bool zz = Nunchuk.Z;

        Dispatch(() =>
        {
            laNunchukX.text = xs;
            laNunchukY.text = ys;
            laNunchukZ.text = zs;
            laNunchukJoyX.text = jxs;
            laNunchukJoyY.text = jys;
            cbNunchukC.isOn = cs;
            cbNunchukZ.isOn = zz;
        });
    }

    private void FClient_OnBalanceBoardChanged(object Sender, wclWiiRemoteBalanceBoard Board)
    {
        string tr = Board.SensorsKg.TopRight.ToString("N2");
        string tl = Board.SensorsKg.TopLeft.ToString("N2");
        string br = Board.SensorsKg.BottomRight.ToString("N2");
        string bl = Board.SensorsKg.BottomLeft.ToString("N2");

        Dispatch(() =>
        {
            laBbTopRight.text = tr;
            laBbTopLeft.text = tl;
            laBbBottomRight.text = br;
            laBbBottomLeft.text = bl;
        });
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btClear_Click()
    {
        lbLog.Items.Clear();
    }

    private void btDiscover_Click()
    {
        wclBluetoothRadio Radio = GetRadio();
        if (Radio != null)
        {
            Int32 Res = Radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
            if (Res != wclErrors.WCL_E_SUCCESS)
                ShowMessageBox("Discover failed; 0x" + Res.ToString("X8"));
        }
    }

    private void btConnect_Click()
    {
        if (FClient.State != wclClientState.csDisconnected)
            ShowMessageBox("Wii Remote is connected");
        else
        {
            wclBluetoothRadio Radio = GetRadio();
            if (Radio != null)
            {
                if (lvDevices.SelectedItems.Count == 0)
                    ShowMessageBox("Select device");
                else
                {
                    FClient.Address = Convert.ToInt64(lvDevices.SelectedItems[0].Text, 16);
                    Int32 Res = FClient.Connect(Radio);
                    if (Res != wclErrors.WCL_E_SUCCESS)
                        ShowMessageBox("Unable connect: 0x" + Res.ToString("X8"));
                }
            }
        }
    }

    private void btDisconnect_Click()
    {
        Int32 Res = FClient.Disconnect();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Unable disconnect: 0x" + Res.ToString("X8"));
    }

    private void btGetStatus_Click()
    {
        Int32 Res = FClient.GetStatus();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Get status failed; 0x" + Res.ToString("X8"));
    }

    private void btGetLeds_Click()
    {
        if (FClient.State != wclClientState.csConnected)
            ShowMessageBox("Not connected");
        else
        {
            cbLed1.isOn = FClient.Leds.Led1;
            cbLed2.isOn = FClient.Leds.Led2;
            cbLed3.isOn = FClient.Leds.Led3;
            cbLed4.isOn = FClient.Leds.Led4;
        }
    }

    private void btSetLeds_Click()
    {
        SetLeds();
    }

    private void SetLeds()
    {
        Int32 Res = FClient.SetLeds(cbLed1.isOn, cbLed2.isOn,
            cbLed3.isOn, cbLed4.isOn);
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Set LEDs failed: 0x" + Res.ToString("X8"));
    }

    private void btEnableAccel_Click()
    {
        Int32 Res = FClient.EnableAccel();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Enable accelerometer failed: 0x" + Res.ToString("X8"));
    }

    private void btDisableAccel_Click()
    {
        Int32 Res = FClient.DisableAccel();
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Disable accelerometer failed: 0x" + Res.ToString("X8"));
        else
            ClearAccel();
    }

    private void btEnableIr_Click()
    {
        Int32 Res = FClient.SetIrSensitivity(wclWiiRemoteIrSensitivity.wiiIrLevelMax);
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Enable IR failed: 0x" + Res.ToString("X8"));
    }

    private void btDisableIr_Click()
    {
        Int32 Res = FClient.SetIrSensitivity(wclWiiRemoteIrSensitivity.wiiIrLevelOff);
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Disable IR failed: 0x" + Res.ToString("X8"));
        else
            ClearIr();
    }

    private void btRumbleOn_Click()
    {
        Int32 Res = FClient.SetRumble(true);
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Set Rumble ON failed; 0x" + Res.ToString("X8"));
    }

    private void btRumbleOff_Click()
    {
        Int32 Res = FClient.SetRumble(false);
        if (Res != wclErrors.WCL_E_SUCCESS)
            ShowMessageBox("Set Rumble OFF failed; 0x" + Res.ToString("X8"));
    }

    // ==========================================================
    //  Helpers
    // ==========================================================
    private void Trace(String Msg)
    {
        lbLog.Items.Add(Msg);
    }

    private void Trace(String Msg, Int32 Res)
    {
        Trace(Msg + ": 0x" + Res.ToString("X8"));
    }

    private void ClearNunchuk()
    {
        laNunchukState.text = "Detached";
        laNunchukX.text = "0";
        laNunchukY.text = "0";
        laNunchukZ.text = "0";
        laNunchukJoyX.text = "0";
        laNunchukJoyY.text = "0";
        cbNunchukC.isOn = false;
        cbNunchukZ.isOn = false;
    }

    private void ClearBalanceBoard()
    {
        laBalanceBoardState.text = "Detached";
        laBbTopRight.text = "0";
        laBbTopLeft.text = "0";
        laBbBottomRight.text = "0";
        laBbBottomLeft.text = "0";
    }

    private void ClearExtensions()
    {
        ClearNunchuk();
        ClearBalanceBoard();
    }

    private void ClearAccel()
    {
        laX.text = "0";
        laY.text = "0";
        laZ.text = "0";
    }

    private void ClearIr()
    {
        laIrMode.text = "Off";
        laIrSensitivity.text = "Off";

        laSensor0Position.text = "0; 0";
        laSensor0Size.text = "0";
        laSensor0Found.text = "False";

        laSensor1Position.text = "0; 0";
        laSensor1Size.text = "0";
        laSensor1Found.text = "False";

        laSensor2Position.text = "0; 0";
        laSensor2Size.text = "0";
        laSensor2Found.text = "False";

        laSensor3Position.text = "0; 0";
        laSensor3Size.text = "0";
        laSensor3Found.text = "False";
    }
}