using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
public class WiFiEventsController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btEventsOpen;
    public Button btEventsClose;
    public Button btEventsClear;

    [Header("ListView Content")]
    public RectTransform lvEventsContent;

    private ListView lvEvents;

    private wclWiFiEvents WiFiEvents;
    private wclPowerEventsMonitor FPowerMonitor;

    private readonly Queue<Action> _pendingActions = new Queue<Action>();

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        lvEvents = new ListView(lvEventsContent, new float[] { 200, 200, 200, 200 });

        btEventsOpen.onClick.AddListener(() => Dispatch(btEventsOpen_Click));
        btEventsClose.onClick.AddListener(() => Dispatch(btEventsClose_Click));
        btEventsClear.onClick.AddListener(() => Dispatch(btEventsClear_Click));
    }

    private void Start() { Dispatch(DoStart); }

    private void DoStart()
    {
        WiFiEvents = new wclWiFiEvents();

        WiFiEvents.AfterOpen += new EventHandler(Events_AfterOpen);
        WiFiEvents.BeforeClose += new EventHandler(WiFiEvents_BeforeClose);

        WiFiEvents.OnAcmAdHocNetworkStateChange += new wclWiFiAcmAdHocNetworkStateChangeEvent(WiFiEvents_OnAcmAdHocNetworkStateChange);
        WiFiEvents.OnAcmAutoconfDisabled += new wclWiFiEvent(WiFiEvents_OnAcmAutoconfDisabled);
        WiFiEvents.OnAcmAutoconfEnabled += new wclWiFiEvent(WiFiEvents_OnAcmAutoconfEnabled);
        WiFiEvents.OnAcmBackgroundScanDisabled += new wclWiFiEvent(WiFiEvents_OnAcmBackgroundScanDisabled);
        WiFiEvents.OnAcmBackgroundScanEnabled += new wclWiFiEvent(WiFiEvents_OnAcmBackgroundScanEnabled);
        WiFiEvents.OnAcmBssTypeChange += new wclWiFiAcmBssTypeChangeEvent(WiFiEvents_OnAcmBssTypeChange);
        WiFiEvents.OnAcmConnectionAttemptFail += new wclWiFiAcmConnectionEvent(WiFiEvents_OnAcmConnectionAttemptFail);
        WiFiEvents.OnAcmConnectionComplete += new wclWiFiAcmConnectionEvent(WiFiEvents_OnAcmConnectionComplete);
        WiFiEvents.OnAcmConnectionStart += new wclWiFiAcmConnectionEvent(WiFiEvents_OnAcmConnectionStart);
        WiFiEvents.OnAcmDisconnected += new wclWiFiAcmConnectionEvent(WiFiEvents_OnAcmDisconnected);
        WiFiEvents.OnAcmDisconnecting += new wclWiFiAcmConnectionEvent(WiFiEvents_OnAcmDisconnecting);
        WiFiEvents.OnAcmFilterListChange += new wclWiFiEvent(WiFiEvents_OnAcmFilterListChange);
        WiFiEvents.OnAcmInterfaceArrival += new wclWiFiEvent(WiFiEvents_OnAcmInterfaceArrival);
        WiFiEvents.OnAcmInterfaceRemoval += new wclWiFiEvent(WiFiEvents_OnAcmInterfaceRemoval);
        WiFiEvents.OnAcmNetworkAvailable += new wclWiFiEvent(WiFiEvents_OnAcmNetworkAvailable);
        WiFiEvents.OnAcmNetworkNotAvailable += new wclWiFiEvent(WiFiEvents_OnAcmNetworkNotAvailable);
        WiFiEvents.OnAcmOperationalStateChange += new wclWiFiEvent(WiFiEvents_OnAcmOperationalStateChange);
        WiFiEvents.OnAcmPowerSettingChange += new wclWiFiAcmPowerSettingChangeEvent(WiFiEvents_OnAcmPowerSettingChange);
        WiFiEvents.OnAcmProfileBlocked += new wclWiFiEvent(WiFiEvents_OnAcmProfileBlocked);
        WiFiEvents.OnAcmProfileChange += new wclWiFiEvent(WiFiEvents_OnAcmProfileChange);
        WiFiEvents.OnAcmProfileNameChange += new wclWiFiAcmProfileNameChangeEvent(WiFiEvents_OnAcmProfileNameChange);
        WiFiEvents.OnAcmProfilesExhausted += new wclWiFiEvent(WiFiEvents_OnAcmProfilesExhausted);
        WiFiEvents.OnAcmProfileUnblocked += new wclWiFiEvent(WiFiEvents_OnAcmProfileUnblocked);
        WiFiEvents.OnAcmScanComplete += new wclWiFiEvent(WiFiEvents_OnAcmScanComplete);
        WiFiEvents.OnAcmScanFail += new wclWiFiAcmScanFailEvent(WiFiEvents_OnAcmScanFail);
        WiFiEvents.OnAcmScanListRefresh += new wclWiFiEvent(WiFiEvents_OnAcmScanListRefresh);
        WiFiEvents.OnAcmScreenPowerChange += new wclWiFiAcmScreenPowerChangeEvent(WiFiEvents_OnAcmScreenPowerChange);

        WiFiEvents.OnHostedNetworkPeerStateChange += new wclWiFiHostedNetworkPeerStateChangeEvent(WiFiEvents_OnHostedNetworkPeerStateChange);
        WiFiEvents.OnHostedNetworkRadioStateChange += new wclWiFiHostedNetworkRadioStateChangeEvent(WiFiEvents_OnHostedNetworkRadioStateChange);
        WiFiEvents.OnHostedNetworkStateChange += new wclWiFiHostedNetworkStateChangeEvent(WiFiEvents_OnHostedNetworkStateChange);

        WiFiEvents.OnMsmAdapterOperationModeChange += new wclWiFiMsmAdapterOperationModeChangeEvent(WiFiEvents_OnMsmAdapterOperationModeChange);
        WiFiEvents.OnMsmAdapterRemoval += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmAdapterRemoval);
        WiFiEvents.OnMsmAssociated += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmAssociated);
        WiFiEvents.OnMsmAssociating += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmAssociating);
        WiFiEvents.OnMsmAuthenticating += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmAuthenticating);
        WiFiEvents.OnMsmConnected += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmConnected);
        WiFiEvents.OnMsmDisassociating += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmDisassociating);
        WiFiEvents.OnMsmDisconnected += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmDisconnected);
        WiFiEvents.OnMsmPeerJoin += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmPeerJoin);
        WiFiEvents.OnMsmPeerLeave += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmPeerLeave);
        WiFiEvents.OnMsmRadioStateChange += new wclWiFiMsmRadioStateChangeEvent(WiFiEvents_OnMsmRadioStateChange);
        WiFiEvents.OnMsmRoamingEnd += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmRoamingEnd);
        WiFiEvents.OnMsmRoamingStart += new wclWiFiMsmConnectionEvent(WiFiEvents_OnMsmRoamingStart);
        WiFiEvents.OnMsmSignalQualityChange += new wclWiFiMsmSignalQualityChangeEvent(WiFiEvents_OnMsmSignalQualityChange);
        WiFiEvents.OnMsmLinkDegraded += new wclWiFiEvent(WiFiEvents_OnMsmLinkDegraded);
        WiFiEvents.OnMsmLinkImproved += new wclWiFiEvent(WiFiEvents_OnMsmLinkImproved);

        WiFiEvents.OnOneXAuthRestarted += new wclWiFiOneXAuthRestartedEvent(WiFiEvents_OnOneXAuthRestarted);
        WiFiEvents.OnOneXAuthUpdate += new wclWiFiOneXAuthUpdateEvent(WiFiEvents_OnOneXAuthUpdate);

        WiFiEvents.OnIpChanged += new wclWiFiIpChangedEvent(WiFiEvents_OnIpChanged);

        FPowerMonitor = new wclPowerEventsMonitor();
        FPowerMonitor.OnPowerStateChanged += new wclPowerStateChangedEvent(FPowerMonitor_OnPowerStateChanged);
        FPowerMonitor.Open();
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
        if (WiFiEvents != null)    { try { WiFiEvents.Close(); }    catch { } }
        if (FPowerMonitor != null) { try { FPowerMonitor.Close(); } catch { } }
        WiFiEvents = null;
        FPowerMonitor = null;
    }

    // ==========================================================
    //  Dispatcher / helpers
    // ==========================================================
    private void Dispatch(Action a) { lock (_pendingActions) { _pendingActions.Enqueue(a); } }

    private void ShowResult(Int32 Res)
    {
        if (Res != wclErrors.WCL_E_SUCCESS)
            MessageBox.Show("Error: 0x" + Res.ToString("X8"));
    }

    private void AddEventRow(string id, string evt, string param, string value)
    {
        ListViewItem Item = lvEvents.Items.Add(id);
        Item.SubItems.Add(evt);
        Item.SubItems.Add(param);
        Item.SubItems.Add(value);
    }

    private void TraceEvent(Guid Iface, String EventName, String ParamName, String ParamValue)
    {
        string Id = (Iface == Guid.Empty) ? "" : Iface.ToString();
        string evt = EventName ?? "";
        string pn  = ParamName ?? "";
        string pv  = ParamValue ?? "";
        Dispatch(() => AddEventRow(Id, evt, pn, pv));
    }

    private void TraceMsmConnectionEvent(Guid IfaceId, String EventName, wclWiFiMsmConnectionEventData Data)
    {
        string id = (IfaceId == Guid.Empty) ? "" : IfaceId.ToString();
        string evt = EventName ?? "";

        string connectionMode = Data.ConnectionMode.ToString();
        string profileName    = Data.ProfileName ?? "";
        string ssid           = Data.Ssid ?? "";
        string bssType        = Data.BssType.ToString();
        string mac            = Data.Mac ?? "";
        string secEnabled     = Data.SecurityEnabled.ToString();
        string firstPeer      = Data.FirstPeer.ToString();
        string lastPeer       = Data.LastPeer.ToString();
        string reason         = Data.Reason.ToString("X8");

        Dispatch(() =>
        {
            AddEventRow(id,  evt, "ConnectionMode",     connectionMode);
            AddEventRow("",  "",  "Profile name",       profileName);
            AddEventRow("",  "",  "SSID",               ssid);
            AddEventRow("",  "",  "BSS type",           bssType);
            AddEventRow("",  "",  "MAC",                mac);
            AddEventRow("",  "",  "Security enabled",   secEnabled);
            AddEventRow("",  "",  "First peer",         firstPeer);
            AddEventRow("",  "",  "Last peer",          lastPeer);
            AddEventRow("",  "",  "Reason 0x",          reason);
        });
    }

    private void TraceAcmConnectionEvent(Guid IfaceId, String EventName, wclWiFiAcmConnectionEventData Data)
    {
        string id = (IfaceId == Guid.Empty) ? "" : IfaceId.ToString();
        string evt = EventName ?? "";

        string connectionMode = Data.ConnectionMode.ToString();
        string profileName    = Data.ProfileName ?? "";
        string ssid           = Data.Ssid ?? "";
        string bssType        = Data.BssType.ToString();
        string secEnabled     = Data.SecurityEnabled.ToString();
        string reason         = Data.Reason.ToString("X8");
        string flags          = Data.Flags.ToString();
        string profileXml     = Data.ProfileXml ?? "";

        Dispatch(() =>
        {
            AddEventRow(id, evt, "ConnectionMode",   connectionMode);
            AddEventRow("", "",  "Profile name",     profileName);
            AddEventRow("", "",  "SSID",             ssid);
            AddEventRow("", "",  "BSS type",         bssType);
            AddEventRow("", "",  "Security enabled", secEnabled);
            AddEventRow("", "",  "Reason 0x",        reason);
            AddEventRow("", "",  "Flags",            flags);
            AddEventRow("", "",  "Profile XML",      profileXml);
        });
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void btEventsOpen_Click()  { ShowResult(WiFiEvents.Open()); }
    private void btEventsClose_Click() { ShowResult(WiFiEvents.Close()); }
    private void btEventsClear_Click() { lvEvents.Items.Clear(); }

    // ==========================================================
    //  Events
    // ==========================================================
    private void WiFiEvents_OnMsmLinkImproved(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "MsmLinkImproved", "", "");
    }

    private void WiFiEvents_OnMsmLinkDegraded(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "MsmLinkDegraded", "", "");
    }

    private void WiFiEvents_OnOneXAuthUpdate(object Sender, Guid IfaceId, wclWiFiOneXStatusUpdate State)
    {
        String Str;

        switch (State.Status.Status)
        {
            case wclWiFiOneXAuthStatus.oxAuthNotStarted:              Str = "oxAuthNotStarted"; break;
            case wclWiFiOneXAuthStatus.oxAuthInProgress:              Str = "oxAuthInProgress"; break;
            case wclWiFiOneXAuthStatus.oxAuthNoAuthenticatorFound:    Str = "oxAuthNoAuthenticatorFound"; break;
            case wclWiFiOneXAuthStatus.oxAuthSuccess:                 Str = "oxAuthSuccess"; break;
            case wclWiFiOneXAuthStatus.oxAuthFailure:                 Str = "oxAuthFailure"; break;
            case wclWiFiOneXAuthStatus.oxAuthInvalid:                 Str = "oxAuthInvalid"; break;
            default:                                                  Str = ""; break;
        }
        TraceEvent(IfaceId, "OneXAuthUpdate", "Status", Str);
        TraceEvent(Guid.Empty, "", "Status.Reason", "0x" + State.Status.Reason.ToString("X8"));
        TraceEvent(Guid.Empty, "", "Status.Error", "0x" + State.Status.Error.ToString("X8"));

        switch (State.BackendSupport)
        {
            case wclWiFiOneXEapMethodBackendSupport.oxEapMethodBackendSupportUnknown:      Str = "oxEapMethodBackendSupportUnknown"; break;
            case wclWiFiOneXEapMethodBackendSupport.oxEapMethodBackendSupported:          Str = "oxEapMethodBackendSupported"; break;
            case wclWiFiOneXEapMethodBackendSupport.oxEapMethodBackendUnsupported:        Str = "oxEapMethodBackendUnsupported"; break;
            default:                                                                      Str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "BackendSupport", Str);

        if (State.BackendEngaged) TraceEvent(Guid.Empty, "", "BackendEngaged", "True");
        else                      TraceEvent(Guid.Empty, "", "BackendEngaged", "False");

        if (State.AuthParams.UpdatePending) TraceEvent(Guid.Empty, "", "AuthParams.UpdatePending", "True");
        else                                TraceEvent(Guid.Empty, "", "AuthParams.UpdatePending", "False");

        switch (State.AuthParams.Profile.SupplicantMode)
        {
            case wclWiFiOneXSupplicantMode.oxSupplicantModeInhibitTransmission: Str = "oxSupplicantModeInhibitTransmission"; break;
            case wclWiFiOneXSupplicantMode.oxSupplicantModeLearn:               Str = "oxSupplicantModeLearn"; break;
            case wclWiFiOneXSupplicantMode.oxSupplicantModeCompliant:           Str = "oxSupplicantModeCompliant"; break;
            case wclWiFiOneXSupplicantMode.oxSupplicantModeInvalid:             Str = "oxSupplicantModeInvalid"; break;
            default:                                                            Str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.SupplicantMode", Str);

        switch (State.AuthParams.Profile.AuthMode)
        {
            case wclWiFiOnexXAuthMode.oxAuthModeMachineOrUser: Str = "oxAuthModeMachineOrUser"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeMachineOnly:   Str = "oxAuthModeMachineOnly"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeUserOnly:      Str = "oxAuthModeUserOnly"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeGuest:         Str = "oxAuthModeGuest"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeUnspecified:   Str = "oxAuthModeUnspecified"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeInvalid:       Str = "oxAuthModeInvalid"; break;
            default:                                           Str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.AuthMode", Str);

        TraceEvent(Guid.Empty, "", "AuthParams.Profile.HeldPeriod",               State.AuthParams.Profile.HeldPeriod.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.AuthPeriod",               State.AuthParams.Profile.AuthPeriod.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.StartPeriod",              State.AuthParams.Profile.StartPeriod.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.MaxStart",                 State.AuthParams.Profile.MaxStart.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.MaxAuthFailures",          State.AuthParams.Profile.MaxAuthFailures.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.NetworkAuthTimeout",       State.AuthParams.Profile.NetworkAuthTimeout.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.NetworkAuthWithUITimeout", State.AuthParams.Profile.NetworkAuthWithUITimeout.ToString());

        if (State.AuthParams.Profile.AllowLogonDialogs) TraceEvent(Guid.Empty, "", "AuthParams.Profile.AllowLogonDialogs", "True");
        else                                            TraceEvent(Guid.Empty, "", "AuthParams.Profile.AllowLogonDialogs", "False");

        if (State.AuthParams.Profile.UserBasedVLan) TraceEvent(Guid.Empty, "", "AuthParams.Profile.UserBasedVLan", "True");
        else                                        TraceEvent(Guid.Empty, "", "AuthParams.Profile.UserBasedVLan", "False");

        switch (State.AuthParams.AuthIdentity)
        {
            case wclWiFiOneXAuthIdentity.oxAuthIdentityNone:         Str = ""; break;
            case wclWiFiOneXAuthIdentity.oxAuthIdentityMachine:      Str = ""; break;
            case wclWiFiOneXAuthIdentity.oxAuthIdentityUser:         Str = ""; break;
            case wclWiFiOneXAuthIdentity.oxAuthIdentityExplicitUser: Str = ""; break;
            case wclWiFiOneXAuthIdentity.oxAuthIdentityGuest:        Str = ""; break;
            case wclWiFiOneXAuthIdentity.oxAuthIdentityInvalid:      Str = ""; break;
            case wclWiFiOneXAuthIdentity.oxAuthIdentityUnknown:      Str = ""; break;
            default:                                                 Str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.AuthIdentity", Str);

        switch (State.AuthParams.QuarantineState)
        {
            case wclWiFiIsolationState.isUnknonw:          Str = "isUnknonw"; break;
            case wclWiFiIsolationState.isNotRestricted:    Str = "isNotRestricted"; break;
            case wclWiFiIsolationState.isInProbation:      Str = "isInProbation"; break;
            case wclWiFiIsolationState.isRestrictedAccess: Str = "isRestrictedAccess"; break;
            case wclWiFiIsolationState.isInvalid:          Str = "isInvalid"; break;
            default:                                       Str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.QuarantineState", Str);

        TraceEvent(Guid.Empty, "", "AuthParams.SessiondId", State.AuthParams.SessiondId.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Identity",   State.AuthParams.Identity);
        TraceEvent(Guid.Empty, "", "AuthParams.UserName",   State.AuthParams.UserName);
        TraceEvent(Guid.Empty, "", "AuthParams.Domain",     State.AuthParams.Domain);

        TraceEvent(Guid.Empty, "", "EapError.Error", "0x" + State.EapError.Error.ToString("X8"));

        TraceEvent(Guid.Empty, "", "EapError.MethodType.EapType.EapType",    State.EapError.MethodType.EapType.EapType.ToString());
        TraceEvent(Guid.Empty, "", "EapError.MethodType.EapType.VendorId",   State.EapError.MethodType.EapType.VendorId.ToString());
        TraceEvent(Guid.Empty, "", "EapError.MethodType.EapType.VendorType", State.EapError.MethodType.EapType.VendorType.ToString());

        TraceEvent(Guid.Empty, "", "EapError.MethodType.AuthorId", State.EapError.MethodType.AuthorId.ToString());

        TraceEvent(Guid.Empty, "", "EapError.Reason",          "0x" + State.EapError.Reason.ToString("X8"));
        TraceEvent(Guid.Empty, "", "EapError.RootCauseGuid",   State.EapError.RootCauseGuid.ToString());
        TraceEvent(Guid.Empty, "", "EapError.RepairGuid",      State.EapError.RepairGuid.ToString());
        TraceEvent(Guid.Empty, "", "EapError.HelpLinkGuid",    State.EapError.HelpLinkGuid.ToString());
        TraceEvent(Guid.Empty, "", "EapError.RootCauseString", State.EapError.RootCauseString);
        TraceEvent(Guid.Empty, "", "EapError.RepairString",    State.EapError.RepairString);
    }

    private void WiFiEvents_OnIpChanged(object Sender, Guid IfaceId, wclWiFiIpSettings Old, wclWiFiIpSettings New)
    {
        string oldAddr = Old.Address ?? "";
        string newAddr = New.Address ?? "";
        TraceEvent(IfaceId, "IP changed", "Old address", oldAddr);
        TraceEvent(Guid.Empty, "IP changed", "New address", newAddr);
    }

    private void FPowerMonitor_OnPowerStateChanged(object Sender, wclPowerState State)
    {
        switch (State)
        {
            case wclPowerState.psResumeAutomatic:     TraceEvent(Guid.Empty, "Power", "State", "psResumeAutomatic"); break;
            case wclPowerState.psResume:              TraceEvent(Guid.Empty, "Power", "State", "psResume"); break;
            case wclPowerState.psSuspend:             TraceEvent(Guid.Empty, "Power", "State", "psSuspend"); break;
            case wclPowerState.psPowerStatusChanged:  TraceEvent(Guid.Empty, "Power", "State", "psPowerStatusChanged"); break;
            case wclPowerState.psUnknown:             TraceEvent(Guid.Empty, "Power", "State", "psUnknown"); break;
        }
    }

    private void WiFiEvents_OnOneXAuthRestarted(object Sender, Guid IfaceId, int Reason)
    {
        TraceEvent(IfaceId, "OneXAuthRestarted", "Reason", Reason.ToString());
    }

    private void WiFiEvents_OnMsmSignalQualityChange(object Sender, Guid IfaceId, uint Quality)
    {
        TraceEvent(IfaceId, "MsmSignalQualityChange", "Quality", Quality.ToString());
    }

    private void WiFiEvents_OnMsmRoamingStart(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmRoamingStart", Data);
    }

    private void WiFiEvents_OnMsmRoamingEnd(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmRoamingEnd", Data);
    }

    private void WiFiEvents_OnMsmRadioStateChange(object Sender, Guid IfaceId, wclWiFiPhyRadioState State)
    {
        string phy = State.Phy.ToString();
        string sw  = State.SoftwareState.ToString();
        string hw  = State.HardwareState.ToString();
        TraceEvent(IfaceId, "MsmRadioStateChange", "PHY", phy);
        TraceEvent(Guid.Empty, "", "Software state", sw);
        TraceEvent(Guid.Empty, "", "Hardware state", hw);
    }

    private void WiFiEvents_OnMsmPeerLeave(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmPeerLeave", Data);
    }

    private void WiFiEvents_OnMsmPeerJoin(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmPeerJoin", Data);
    }

    private void WiFiEvents_OnMsmDisconnected(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmDisconnected", Data);
    }

    private void WiFiEvents_OnMsmDisassociating(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmDisassociating", Data);
    }

    private void WiFiEvents_OnMsmConnected(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmConnected", Data);
    }

    private void WiFiEvents_OnMsmAuthenticating(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmAuthenticating", Data);
    }

    private void WiFiEvents_OnMsmAssociating(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmAssociating", Data);
    }

    private void WiFiEvents_OnMsmAssociated(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmAssociated", Data);
    }

    private void WiFiEvents_OnMsmAdapterRemoval(object Sender, Guid IfaceId, wclWiFiMsmConnectionEventData Data)
    {
        TraceMsmConnectionEvent(IfaceId, "MsmAdapterRemoval", Data);
    }

    private void WiFiEvents_OnMsmAdapterOperationModeChange(object Sender, Guid IfaceId, wclWiFiOperationMode Mode)
    {
        TraceEvent(IfaceId, "MsmAdapterOperationModeChange", "Mode", Mode.ToString());
    }

    private void WiFiEvents_OnHostedNetworkStateChange(object Sender, Guid IfaceId, wclWiFiHostedNetworkState OldState, wclWiFiHostedNetworkState NewState, int Reason)
    {
        string oldS = OldState.ToString();
        string newS = NewState.ToString();
        string reason = Reason.ToString("X8");
        TraceEvent(IfaceId, "HostedNetworkStateChange", "Old state", oldS);
        TraceEvent(Guid.Empty, "", "New state", newS);
        TraceEvent(Guid.Empty, "", "Reason 0x", reason);
    }

    private void WiFiEvents_OnHostedNetworkRadioStateChange(object Sender, Guid IfaceId, wclWiFiRadioState SoftwareState, wclWiFiRadioState HardwareState)
    {
        string sw = SoftwareState.ToString();
        string hw = HardwareState.ToString();
        TraceEvent(IfaceId, "HostedNetworkRadioStateChange", "Software state", sw);
        TraceEvent(Guid.Empty, "", "Hardware state", hw);
    }

    private void WiFiEvents_OnHostedNetworkPeerStateChange(object Sender, Guid IfaceId, wclWiFiHostedNetworkPeerState OldState, wclWiFiHostedNetworkPeerState NewState, int Reason)
    {
        string oldMac  = OldState.Mac ?? "";
        string oldAuth = OldState.AuthState.ToString();
        string newMac  = NewState.Mac ?? "";
        string newAuth = NewState.AuthState.ToString();
        string reason  = Reason.ToString("X8");
        TraceEvent(IfaceId, "HostedNetworkPeerStateChange", "Old MAC", oldMac);
        TraceEvent(Guid.Empty, "", "Old authentication state", oldAuth);
        TraceEvent(Guid.Empty, "", "New MAC", newMac);
        TraceEvent(Guid.Empty, "", "New authentication state", newAuth);
        TraceEvent(Guid.Empty, "", "Reason 0x", reason);
    }

    private void WiFiEvents_OnAcmScreenPowerChange(object Sender, Guid IfaceId, bool SwitchedOn)
    {
        TraceEvent(IfaceId, "AcmScreenPowerChange", "SwitchedOn", SwitchedOn.ToString());
    }

    private void WiFiEvents_OnAcmScanListRefresh(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmScanListRefresh", "", "");
    }

    private void WiFiEvents_OnAcmScanFail(object Sender, Guid IfaceId, int Reason)
    {
        TraceEvent(IfaceId, "AcmScanFail", "Reason", Reason.ToString());
    }

    private void WiFiEvents_OnAcmScanComplete(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmScanComplete", "", "");
    }

    private void WiFiEvents_OnAcmProfileUnblocked(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmProfileUnblocked", "", "");
    }

    private void WiFiEvents_OnAcmProfilesExhausted(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmProfilesExhausted", "", "");
    }

    private void WiFiEvents_OnAcmProfileNameChange(object Sender, Guid IfaceId, string OldName, string NewName)
    {
        string oldN = OldName ?? "";
        string newN = NewName ?? "";
        TraceEvent(IfaceId, "AcmProfileNameChange", "OldName", oldN);
        TraceEvent(Guid.Empty, "", "NewName", newN);
    }

    private void WiFiEvents_OnAcmProfileChange(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmProfileChange", "", "");
    }

    private void WiFiEvents_OnAcmProfileBlocked(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmProfileBlocked", "", "");
    }

    private void WiFiEvents_OnAcmPowerSettingChange(object Sender, Guid IfaceId, wclWiFiPowerSetting Setting)
    {
        TraceEvent(IfaceId, "AcmOperationalStateChange", "Setting", Setting.ToString());
    }

    private void WiFiEvents_OnAcmOperationalStateChange(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmOperationalStateChange", "", "");
    }

    private void WiFiEvents_OnAcmNetworkNotAvailable(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmNetworkNotAvailable", "", "");
    }

    private void WiFiEvents_OnAcmNetworkAvailable(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmNetworkAvailable", "", "");
    }

    private void WiFiEvents_OnAcmInterfaceRemoval(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmInterfaceRemoval", "", "");
    }

    private void WiFiEvents_OnAcmInterfaceArrival(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmInterfaceArrival", "", "");
    }

    private void WiFiEvents_OnAcmFilterListChange(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmFilterListChange", "", "");
    }

    private void WiFiEvents_OnAcmDisconnecting(object Sender, Guid IfaceId, wclWiFiAcmConnectionEventData Data)
    {
        TraceAcmConnectionEvent(IfaceId, "AcmDisconnecting", Data);
    }

    private void WiFiEvents_OnAcmDisconnected(object Sender, Guid IfaceId, wclWiFiAcmConnectionEventData Data)
    {
        TraceAcmConnectionEvent(IfaceId, "AcmDisconnected", Data);
    }

    private void WiFiEvents_OnAcmConnectionStart(object Sender, Guid IfaceId, wclWiFiAcmConnectionEventData Data)
    {
        TraceAcmConnectionEvent(IfaceId, "AcmConnectionStart", Data);
    }

    private void WiFiEvents_OnAcmConnectionComplete(object Sender, Guid IfaceId, wclWiFiAcmConnectionEventData Data)
    {
        TraceAcmConnectionEvent(IfaceId, "AcmConnectionComplete", Data);
    }

    private void WiFiEvents_OnAcmConnectionAttemptFail(object Sender, Guid IfaceId, wclWiFiAcmConnectionEventData Data)
    {
        TraceAcmConnectionEvent(IfaceId, "AcmConnectionAttemptFail", Data);
    }

    private void WiFiEvents_OnAcmBssTypeChange(object Sender, Guid IfaceId, wclWiFiBssType BssType)
    {
        TraceEvent(IfaceId, "AcmBssTypeChange", "BSS type", BssType.ToString());
    }

    private void WiFiEvents_OnAcmBackgroundScanEnabled(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmBackgroundScanEnabled", "", "");
    }

    private void WiFiEvents_OnAcmBackgroundScanDisabled(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmBackgroundScanDisabled", "", "");
    }

    private void WiFiEvents_OnAcmAutoconfEnabled(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmAutoconfEnabled", "", "");
    }

    private void WiFiEvents_OnAcmAutoconfDisabled(object Sender, Guid IfaceId)
    {
        TraceEvent(IfaceId, "AcmAutoconfDisabled", "", "");
    }

    private void WiFiEvents_OnAcmAdHocNetworkStateChange(object Sender, Guid IfaceId, wclWiFiAdHocNetworkState State)
    {
        TraceEvent(IfaceId, "AcmAdHocNetworkStateChange", "State", State.ToString());
    }

    private void WiFiEvents_BeforeClose(object sender, EventArgs e)
    {
        TraceEvent(Guid.Empty, "BeforeClose", "", "");
    }

    private void Events_AfterOpen(object sender, EventArgs e)
    {
        TraceEvent(Guid.Empty, "AfterOpen", "", "");
    }
}