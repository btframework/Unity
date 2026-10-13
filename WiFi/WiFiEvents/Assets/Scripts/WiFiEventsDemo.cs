using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiEventsDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button openButton;
    public Button closeButton;
    public Button clearButton;

    // ---------------- Events table ----------------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiEvents         _wifiEvents;
    private wclPowerEventsMonitor _powerMonitor;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        openButton .onClick.AddListener(OnOpenClicked);
        closeButton.onClick.AddListener(OnCloseClicked);
        clearButton.onClick.AddListener(() => ClearChildren(eventsContent));
    }

    private void Start()
    {
        _wifiEvents = new wclWiFiEvents();

        _wifiEvents.AfterOpen   += EventsAfterOpen;
        _wifiEvents.BeforeClose += EventsBeforeClose;

        // ACM
        _wifiEvents.OnAcmAdHocNetworkStateChange += EventsOnAcmAdHocNetworkStateChange;
        _wifiEvents.OnAcmAutoconfDisabled         += EventsOnAcmAutoconfDisabled;
        _wifiEvents.OnAcmAutoconfEnabled          += EventsOnAcmAutoconfEnabled;
        _wifiEvents.OnAcmBackgroundScanDisabled   += EventsOnAcmBackgroundScanDisabled;
        _wifiEvents.OnAcmBackgroundScanEnabled    += EventsOnAcmBackgroundScanEnabled;
        _wifiEvents.OnAcmBssTypeChange            += EventsOnAcmBssTypeChange;
        _wifiEvents.OnAcmConnectionAttemptFail    += EventsOnAcmConnectionAttemptFail;
        _wifiEvents.OnAcmConnectionComplete       += EventsOnAcmConnectionComplete;
        _wifiEvents.OnAcmConnectionStart          += EventsOnAcmConnectionStart;
        _wifiEvents.OnAcmDisconnected             += EventsOnAcmDisconnected;
        _wifiEvents.OnAcmDisconnecting            += EventsOnAcmDisconnecting;
        _wifiEvents.OnAcmFilterListChange         += EventsOnAcmFilterListChange;
        _wifiEvents.OnAcmInterfaceArrival         += EventsOnAcmInterfaceArrival;
        _wifiEvents.OnAcmInterfaceRemoval         += EventsOnAcmInterfaceRemoval;
        _wifiEvents.OnAcmNetworkAvailable         += EventsOnAcmNetworkAvailable;
        _wifiEvents.OnAcmNetworkNotAvailable      += EventsOnAcmNetworkNotAvailable;
        _wifiEvents.OnAcmOperationalStateChange   += EventsOnAcmOperationalStateChange;
        _wifiEvents.OnAcmPowerSettingChange       += EventsOnAcmPowerSettingChange;
        _wifiEvents.OnAcmProfileBlocked           += EventsOnAcmProfileBlocked;
        _wifiEvents.OnAcmProfileChange            += EventsOnAcmProfileChange;
        _wifiEvents.OnAcmProfileNameChange        += EventsOnAcmProfileNameChange;
        _wifiEvents.OnAcmProfilesExhausted        += EventsOnAcmProfilesExhausted;
        _wifiEvents.OnAcmProfileUnblocked         += EventsOnAcmProfileUnblocked;
        _wifiEvents.OnAcmScanComplete             += EventsOnAcmScanComplete;
        _wifiEvents.OnAcmScanFail                 += EventsOnAcmScanFail;
        _wifiEvents.OnAcmScanListRefresh          += EventsOnAcmScanListRefresh;
        _wifiEvents.OnAcmScreenPowerChange        += EventsOnAcmScreenPowerChange;

        // Hosted network
        _wifiEvents.OnHostedNetworkPeerStateChange  += EventsOnHostedNetworkPeerStateChange;
        _wifiEvents.OnHostedNetworkRadioStateChange += EventsOnHostedNetworkRadioStateChange;
        _wifiEvents.OnHostedNetworkStateChange      += EventsOnHostedNetworkStateChange;

        // MSM
        _wifiEvents.OnMsmAdapterOperationModeChange += EventsOnMsmAdapterOperationModeChange;
        _wifiEvents.OnMsmAdapterRemoval             += EventsOnMsmAdapterRemoval;
        _wifiEvents.OnMsmAssociated                 += EventsOnMsmAssociated;
        _wifiEvents.OnMsmAssociating                += EventsOnMsmAssociating;
        _wifiEvents.OnMsmAuthenticating             += EventsOnMsmAuthenticating;
        _wifiEvents.OnMsmConnected                  += EventsOnMsmConnected;
        _wifiEvents.OnMsmDisassociating             += EventsOnMsmDisassociating;
        _wifiEvents.OnMsmDisconnected               += EventsOnMsmDisconnected;
        _wifiEvents.OnMsmPeerJoin                   += EventsOnMsmPeerJoin;
        _wifiEvents.OnMsmPeerLeave                  += EventsOnMsmPeerLeave;
        _wifiEvents.OnMsmRadioStateChange           += EventsOnMsmRadioStateChange;
        _wifiEvents.OnMsmRoamingEnd                 += EventsOnMsmRoamingEnd;
        _wifiEvents.OnMsmRoamingStart               += EventsOnMsmRoamingStart;
        _wifiEvents.OnMsmSignalQualityChange        += EventsOnMsmSignalQualityChange;
        _wifiEvents.OnMsmLinkDegraded               += EventsOnMsmLinkDegraded;
        _wifiEvents.OnMsmLinkImproved               += EventsOnMsmLinkImproved;

        // OneX
        _wifiEvents.OnOneXAuthRestarted += EventsOnOneXAuthRestarted;
        _wifiEvents.OnOneXAuthUpdate    += EventsOnOneXAuthUpdate;

        // IP
        _wifiEvents.OnIpChanged += EventsOnIpChanged;

        // Power monitor
        _powerMonitor = new wclPowerEventsMonitor();
        _powerMonitor.OnPowerStateChanged += PowerMonitorOnPowerStateChanged;
        _powerMonitor.Open();
    }

    private void OnDestroy()
    {
        if (_wifiEvents != null)
        {
            try { _wifiEvents.Close(); } catch (Exception e) { Debug.LogException(e); }

            _wifiEvents.AfterOpen   -= EventsAfterOpen;
            _wifiEvents.BeforeClose -= EventsBeforeClose;

            _wifiEvents.OnAcmAdHocNetworkStateChange -= EventsOnAcmAdHocNetworkStateChange;
            _wifiEvents.OnAcmAutoconfDisabled         -= EventsOnAcmAutoconfDisabled;
            _wifiEvents.OnAcmAutoconfEnabled          -= EventsOnAcmAutoconfEnabled;
            _wifiEvents.OnAcmBackgroundScanDisabled   -= EventsOnAcmBackgroundScanDisabled;
            _wifiEvents.OnAcmBackgroundScanEnabled    -= EventsOnAcmBackgroundScanEnabled;
            _wifiEvents.OnAcmBssTypeChange            -= EventsOnAcmBssTypeChange;
            _wifiEvents.OnAcmConnectionAttemptFail    -= EventsOnAcmConnectionAttemptFail;
            _wifiEvents.OnAcmConnectionComplete       -= EventsOnAcmConnectionComplete;
            _wifiEvents.OnAcmConnectionStart          -= EventsOnAcmConnectionStart;
            _wifiEvents.OnAcmDisconnected             -= EventsOnAcmDisconnected;
            _wifiEvents.OnAcmDisconnecting            -= EventsOnAcmDisconnecting;
            _wifiEvents.OnAcmFilterListChange         -= EventsOnAcmFilterListChange;
            _wifiEvents.OnAcmInterfaceArrival         -= EventsOnAcmInterfaceArrival;
            _wifiEvents.OnAcmInterfaceRemoval         -= EventsOnAcmInterfaceRemoval;
            _wifiEvents.OnAcmNetworkAvailable         -= EventsOnAcmNetworkAvailable;
            _wifiEvents.OnAcmNetworkNotAvailable      -= EventsOnAcmNetworkNotAvailable;
            _wifiEvents.OnAcmOperationalStateChange   -= EventsOnAcmOperationalStateChange;
            _wifiEvents.OnAcmPowerSettingChange       -= EventsOnAcmPowerSettingChange;
            _wifiEvents.OnAcmProfileBlocked           -= EventsOnAcmProfileBlocked;
            _wifiEvents.OnAcmProfileChange            -= EventsOnAcmProfileChange;
            _wifiEvents.OnAcmProfileNameChange        -= EventsOnAcmProfileNameChange;
            _wifiEvents.OnAcmProfilesExhausted        -= EventsOnAcmProfilesExhausted;
            _wifiEvents.OnAcmProfileUnblocked         -= EventsOnAcmProfileUnblocked;
            _wifiEvents.OnAcmScanComplete             -= EventsOnAcmScanComplete;
            _wifiEvents.OnAcmScanFail                 -= EventsOnAcmScanFail;
            _wifiEvents.OnAcmScanListRefresh          -= EventsOnAcmScanListRefresh;
            _wifiEvents.OnAcmScreenPowerChange        -= EventsOnAcmScreenPowerChange;

            _wifiEvents.OnHostedNetworkPeerStateChange  -= EventsOnHostedNetworkPeerStateChange;
            _wifiEvents.OnHostedNetworkRadioStateChange -= EventsOnHostedNetworkRadioStateChange;
            _wifiEvents.OnHostedNetworkStateChange      -= EventsOnHostedNetworkStateChange;

            _wifiEvents.OnMsmAdapterOperationModeChange -= EventsOnMsmAdapterOperationModeChange;
            _wifiEvents.OnMsmAdapterRemoval             -= EventsOnMsmAdapterRemoval;
            _wifiEvents.OnMsmAssociated                 -= EventsOnMsmAssociated;
            _wifiEvents.OnMsmAssociating                -= EventsOnMsmAssociating;
            _wifiEvents.OnMsmAuthenticating             -= EventsOnMsmAuthenticating;
            _wifiEvents.OnMsmConnected                  -= EventsOnMsmConnected;
            _wifiEvents.OnMsmDisassociating             -= EventsOnMsmDisassociating;
            _wifiEvents.OnMsmDisconnected               -= EventsOnMsmDisconnected;
            _wifiEvents.OnMsmPeerJoin                   -= EventsOnMsmPeerJoin;
            _wifiEvents.OnMsmPeerLeave                  -= EventsOnMsmPeerLeave;
            _wifiEvents.OnMsmRadioStateChange           -= EventsOnMsmRadioStateChange;
            _wifiEvents.OnMsmRoamingEnd                 -= EventsOnMsmRoamingEnd;
            _wifiEvents.OnMsmRoamingStart               -= EventsOnMsmRoamingStart;
            _wifiEvents.OnMsmSignalQualityChange        -= EventsOnMsmSignalQualityChange;
            _wifiEvents.OnMsmLinkDegraded               -= EventsOnMsmLinkDegraded;
            _wifiEvents.OnMsmLinkImproved               -= EventsOnMsmLinkImproved;

            _wifiEvents.OnOneXAuthRestarted -= EventsOnOneXAuthRestarted;
            _wifiEvents.OnOneXAuthUpdate    -= EventsOnOneXAuthUpdate;

            _wifiEvents.OnIpChanged -= EventsOnIpChanged;

            _wifiEvents = null;
        }

        if (_powerMonitor != null)
        {
            try { _powerMonitor.Close(); } catch (Exception e) { Debug.LogException(e); }
            _powerMonitor.OnPowerStateChanged -= PowerMonitorOnPowerStateChanged;
            _powerMonitor = null;
        }
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnOpenClicked()  => ShowResult(_wifiEvents.Open());
    private void OnCloseClicked() => ShowResult(_wifiEvents.Close());

    private void ShowResult(Int32 res)
    {
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Top-level events
    // ==================================================================
    private void EventsAfterOpen(object s, EventArgs e)
        => TraceEvent(Guid.Empty, "AfterOpen", "", "");

    private void EventsBeforeClose(object s, EventArgs e)
        => TraceEvent(Guid.Empty, "BeforeClose", "", "");

    // ==================================================================
    //  ACM events
    // ==================================================================
    private void EventsOnAcmAdHocNetworkStateChange(object s, Guid iface, wclWiFiAdHocNetworkState state)
        => TraceEvent(iface, "AcmAdHocNetworkStateChange", "State", state.ToString());

    private void EventsOnAcmAutoconfDisabled(object s, Guid iface)
        => TraceEvent(iface, "AcmAutoconfDisabled", "", "");

    private void EventsOnAcmAutoconfEnabled(object s, Guid iface)
        => TraceEvent(iface, "AcmAutoconfEnabled", "", "");

    private void EventsOnAcmBackgroundScanDisabled(object s, Guid iface)
        => TraceEvent(iface, "AcmBackgroundScanDisabled", "", "");

    private void EventsOnAcmBackgroundScanEnabled(object s, Guid iface)
        => TraceEvent(iface, "AcmBackgroundScanEnabled", "", "");

    private void EventsOnAcmBssTypeChange(object s, Guid iface, wclWiFiBssType bssType)
        => TraceEvent(iface, "AcmBssTypeChange", "BSS type", bssType.ToString());

    private void EventsOnAcmConnectionAttemptFail(object s, Guid iface, wclWiFiAcmConnectionEventData data)
        => TraceAcmConnectionEvent(iface, "AcmConnectionAttemptFail", data);

    private void EventsOnAcmConnectionComplete(object s, Guid iface, wclWiFiAcmConnectionEventData data)
        => TraceAcmConnectionEvent(iface, "AcmConnectionComplete", data);

    private void EventsOnAcmConnectionStart(object s, Guid iface, wclWiFiAcmConnectionEventData data)
        => TraceAcmConnectionEvent(iface, "AcmConnectionStart", data);

    private void EventsOnAcmDisconnected(object s, Guid iface, wclWiFiAcmConnectionEventData data)
        => TraceAcmConnectionEvent(iface, "AcmDisconnected", data);

    private void EventsOnAcmDisconnecting(object s, Guid iface, wclWiFiAcmConnectionEventData data)
        => TraceAcmConnectionEvent(iface, "AcmDisconnecting", data);

    private void EventsOnAcmFilterListChange(object s, Guid iface)
        => TraceEvent(iface, "AcmFilterListChange", "", "");

    private void EventsOnAcmInterfaceArrival(object s, Guid iface)
        => TraceEvent(iface, "AcmInterfaceArrival", "", "");

    private void EventsOnAcmInterfaceRemoval(object s, Guid iface)
        => TraceEvent(iface, "AcmInterfaceRemoval", "", "");

    private void EventsOnAcmNetworkAvailable(object s, Guid iface)
        => TraceEvent(iface, "AcmNetworkAvailable", "", "");

    private void EventsOnAcmNetworkNotAvailable(object s, Guid iface)
        => TraceEvent(iface, "AcmNetworkNotAvailable", "", "");

    private void EventsOnAcmOperationalStateChange(object s, Guid iface)
        => TraceEvent(iface, "AcmOperationalStateChange", "", "");

    private void EventsOnAcmPowerSettingChange(object s, Guid iface, wclWiFiPowerSetting setting)
        => TraceEvent(iface, "AcmOperationalStateChange", "Setting", setting.ToString());

    private void EventsOnAcmProfileBlocked(object s, Guid iface)
        => TraceEvent(iface, "AcmProfileBlocked", "", "");

    private void EventsOnAcmProfileChange(object s, Guid iface)
        => TraceEvent(iface, "AcmProfileChange", "", "");

    private void EventsOnAcmProfileNameChange(object s, Guid iface, string oldName, string newName)
    {
        TraceEvent(iface, "AcmProfileNameChange", "OldName", oldName);
        TraceEvent(Guid.Empty, "", "NewName", newName);
    }

    private void EventsOnAcmProfilesExhausted(object s, Guid iface)
        => TraceEvent(iface, "AcmProfilesExhausted", "", "");

    private void EventsOnAcmProfileUnblocked(object s, Guid iface)
        => TraceEvent(iface, "AcmProfileUnblocked", "", "");

    private void EventsOnAcmScanComplete(object s, Guid iface)
        => TraceEvent(iface, "AcmScanComplete", "", "");

    private void EventsOnAcmScanFail(object s, Guid iface, int reason)
        => TraceEvent(iface, "AcmScanFail", "Reason", reason.ToString());

    private void EventsOnAcmScanListRefresh(object s, Guid iface)
        => TraceEvent(iface, "AcmScanListRefresh", "", "");

    private void EventsOnAcmScreenPowerChange(object s, Guid iface, bool switchedOn)
        => TraceEvent(iface, "AcmScreenPowerChange", "SwitchedOn", switchedOn.ToString());

    // ==================================================================
    //  Hosted network
    // ==================================================================
    private void EventsOnHostedNetworkPeerStateChange(object s, Guid iface,
        wclWiFiHostedNetworkPeerState oldState, wclWiFiHostedNetworkPeerState newState, int reason)
    {
        TraceEvent(iface, "HostedNetworkPeerStateChange", "Old MAC", oldState.Mac);
        TraceEvent(Guid.Empty, "", "Old authentication state", oldState.AuthState.ToString());
        TraceEvent(Guid.Empty, "", "New MAC", newState.Mac);
        TraceEvent(Guid.Empty, "", "New authentication state", newState.AuthState.ToString());
        TraceEvent(Guid.Empty, "", "Reason 0x", reason.ToString("X8"));
    }

    private void EventsOnHostedNetworkRadioStateChange(object s, Guid iface,
        wclWiFiRadioState softwareState, wclWiFiRadioState hardwareState)
    {
        TraceEvent(iface, "HostedNetworkRadioStateChange", "Software state", softwareState.ToString());
        TraceEvent(Guid.Empty, "", "Hardware state", hardwareState.ToString());
    }

    private void EventsOnHostedNetworkStateChange(object s, Guid iface,
        wclWiFiHostedNetworkState oldState, wclWiFiHostedNetworkState newState, int reason)
    {
        TraceEvent(iface, "HostedNetworkStateChange", "Old state", oldState.ToString());
        TraceEvent(Guid.Empty, "", "New state", newState.ToString());
        TraceEvent(Guid.Empty, "", "Reason 0x", reason.ToString("X8"));
    }

    // ==================================================================
    //  MSM
    // ==================================================================
    private void EventsOnMsmAdapterOperationModeChange(object s, Guid iface, wclWiFiOperationMode mode)
        => TraceEvent(iface, "MsmAdapterOperationModeChange", "Mode", mode.ToString());

    private void EventsOnMsmAdapterRemoval(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmAdapterRemoval", data);

    private void EventsOnMsmAssociated(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmAssociated", data);

    private void EventsOnMsmAssociating(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmAssociating", data);

    private void EventsOnMsmAuthenticating(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmAuthenticating", data);

    private void EventsOnMsmConnected(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmConnected", data);

    private void EventsOnMsmDisassociating(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmDisassociating", data);

    private void EventsOnMsmDisconnected(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmDisconnected", data);

    private void EventsOnMsmPeerJoin(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmPeerJoin", data);

    private void EventsOnMsmPeerLeave(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmPeerLeave", data);

    private void EventsOnMsmRadioStateChange(object s, Guid iface, wclWiFiPhyRadioState state)
    {
        TraceEvent(iface, "MsmRadioStateChange", "PHY", state.Phy.ToString());
        TraceEvent(Guid.Empty, "", "Software state", state.SoftwareState.ToString());
        TraceEvent(Guid.Empty, "", "Hardware state", state.HardwareState.ToString());
    }

    private void EventsOnMsmRoamingEnd(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmRoamingEnd", data);

    private void EventsOnMsmRoamingStart(object s, Guid iface, wclWiFiMsmConnectionEventData data)
        => TraceMsmConnectionEvent(iface, "MsmRoamingStart", data);

    private void EventsOnMsmSignalQualityChange(object s, Guid iface, uint quality)
        => TraceEvent(iface, "MsmSignalQualityChange", "Quality", quality.ToString());

    private void EventsOnMsmLinkDegraded(object s, Guid iface)
        => TraceEvent(iface, "MsmLinkDegraded", "", "");

    private void EventsOnMsmLinkImproved(object s, Guid iface)
        => TraceEvent(iface, "MsmLinkImproved", "", "");

    private void TraceMsmConnectionEvent(Guid iface, string eventName, wclWiFiMsmConnectionEventData data)
    {
        TraceEvent(iface, eventName, "ConnectionMode", data.ConnectionMode.ToString());
        TraceEvent(Guid.Empty, "", "Profile name", data.ProfileName);
        TraceEvent(Guid.Empty, "", "SSID", data.Ssid);
        TraceEvent(Guid.Empty, "", "BSS type", data.BssType.ToString());
        TraceEvent(Guid.Empty, "", "MAC", data.Mac);
        TraceEvent(Guid.Empty, "", "Security enabled", data.SecurityEnabled.ToString());
        TraceEvent(Guid.Empty, "", "First peer", data.FirstPeer.ToString());
        TraceEvent(Guid.Empty, "", "Last peer", data.LastPeer.ToString());
        TraceEvent(Guid.Empty, "", "Reason 0x", data.Reason.ToString("X8"));
    }

    // ==================================================================
    //  OneX
    // ==================================================================
    private void EventsOnOneXAuthRestarted(object s, Guid iface, int reason)
        => TraceEvent(iface, "OneXAuthRestarted", "Reason", reason.ToString());

    private void EventsOnOneXAuthUpdate(object s, Guid iface, wclWiFiOneXStatusUpdate state)
    {
        string str;

        switch (state.Status.Status)
        {
            case wclWiFiOneXAuthStatus.oxAuthNotStarted:            str = "oxAuthNotStarted"; break;
            case wclWiFiOneXAuthStatus.oxAuthInProgress:            str = "oxAuthInProgress"; break;
            case wclWiFiOneXAuthStatus.oxAuthNoAuthenticatorFound:  str = "oxAuthNoAuthenticatorFound"; break;
            case wclWiFiOneXAuthStatus.oxAuthSuccess:               str = "oxAuthSuccess"; break;
            case wclWiFiOneXAuthStatus.oxAuthFailure:               str = "oxAuthFailure"; break;
            case wclWiFiOneXAuthStatus.oxAuthInvalid:               str = "oxAuthInvalid"; break;
            default:                                                 str = ""; break;
        }
        TraceEvent(iface, "OneXAuthUpdate", "Status", str);
        TraceEvent(Guid.Empty, "", "Status.Reason", "0x" + state.Status.Reason.ToString("X8"));
        TraceEvent(Guid.Empty, "", "Status.Error",  "0x" + state.Status.Error.ToString("X8"));

        switch (state.BackendSupport)
        {
            case wclWiFiOneXEapMethodBackendSupport.oxEapMethodBackendSupportUnknown:     str = "oxEapMethodBackendSupportUnknown"; break;
            case wclWiFiOneXEapMethodBackendSupport.oxEapMethodBackendSupported:         str = "oxEapMethodBackendSupported"; break;
            case wclWiFiOneXEapMethodBackendSupport.oxEapMethodBackendUnsupported:       str = "oxEapMethodBackendUnsupported"; break;
            default:                                                                     str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "BackendSupport", str);

        TraceEvent(Guid.Empty, "", "BackendEngaged", state.BackendEngaged ? "True" : "False");
        TraceEvent(Guid.Empty, "", "AuthParams.UpdatePending", state.AuthParams.UpdatePending ? "True" : "False");

        switch (state.AuthParams.Profile.SupplicantMode)
        {
            case wclWiFiOneXSupplicantMode.oxSupplicantModeInhibitTransmission: str = "oxSupplicantModeInhibitTransmission"; break;
            case wclWiFiOneXSupplicantMode.oxSupplicantModeLearn:               str = "oxSupplicantModeLearn"; break;
            case wclWiFiOneXSupplicantMode.oxSupplicantModeCompliant:           str = "oxSupplicantModeCompliant"; break;
            case wclWiFiOneXSupplicantMode.oxSupplicantModeInvalid:             str = "oxSupplicantModeInvalid"; break;
            default:                                                            str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.SupplicantMode", str);

        switch (state.AuthParams.Profile.AuthMode)
        {
            case wclWiFiOnexXAuthMode.oxAuthModeMachineOrUser: str = "oxAuthModeMachineOrUser"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeMachineOnly:   str = "oxAuthModeMachineOnly"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeUserOnly:      str = "oxAuthModeUserOnly"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeGuest:         str = "oxAuthModeGuest"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeUnspecified:   str = "oxAuthModeUnspecified"; break;
            case wclWiFiOnexXAuthMode.oxAuthModeInvalid:       str = "oxAuthModeInvalid"; break;
            default:                                           str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.AuthMode", str);

        TraceEvent(Guid.Empty, "", "AuthParams.Profile.HeldPeriod", state.AuthParams.Profile.HeldPeriod.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.AuthPeriod", state.AuthParams.Profile.AuthPeriod.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.StartPeriod", state.AuthParams.Profile.StartPeriod.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.MaxStart", state.AuthParams.Profile.MaxStart.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.MaxAuthFailures", state.AuthParams.Profile.MaxAuthFailures.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.NetworkAuthTimeout", state.AuthParams.Profile.NetworkAuthTimeout.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.NetworkAuthWithUITimeout", state.AuthParams.Profile.NetworkAuthWithUITimeout.ToString());

        TraceEvent(Guid.Empty, "", "AuthParams.Profile.AllowLogonDialogs", state.AuthParams.Profile.AllowLogonDialogs ? "True" : "False");
        TraceEvent(Guid.Empty, "", "AuthParams.Profile.UserBasedVLan", state.AuthParams.Profile.UserBasedVLan ? "True" : "False");

        // AuthIdentity Ч просто пишем им€ enum'а.
        TraceEvent(Guid.Empty, "", "AuthParams.AuthIdentity", state.AuthParams.AuthIdentity.ToString());

        switch (state.AuthParams.QuarantineState)
        {
            case wclWiFiIsolationState.isUnknonw:          str = "isUnknonw"; break;
            case wclWiFiIsolationState.isNotRestricted:    str = "isNotRestricted"; break;
            case wclWiFiIsolationState.isInProbation:      str = "isInProbation"; break;
            case wclWiFiIsolationState.isRestrictedAccess: str = "isRestrictedAccess"; break;
            case wclWiFiIsolationState.isInvalid:          str = "isInvalid"; break;
            default:                                       str = ""; break;
        }
        TraceEvent(Guid.Empty, "", "AuthParams.QuarantineState", str);

        TraceEvent(Guid.Empty, "", "AuthParams.SessiondId", state.AuthParams.SessiondId.ToString());
        TraceEvent(Guid.Empty, "", "AuthParams.Identity",   state.AuthParams.Identity);
        TraceEvent(Guid.Empty, "", "AuthParams.UserName",   state.AuthParams.UserName);
        TraceEvent(Guid.Empty, "", "AuthParams.Domain",     state.AuthParams.Domain);

        TraceEvent(Guid.Empty, "", "EapError.Error", "0x" + state.EapError.Error.ToString("X8"));
        TraceEvent(Guid.Empty, "", "EapError.MethodType.EapType.EapType",   state.EapError.MethodType.EapType.EapType.ToString());
        TraceEvent(Guid.Empty, "", "EapError.MethodType.EapType.VendorId",  state.EapError.MethodType.EapType.VendorId.ToString());
        TraceEvent(Guid.Empty, "", "EapError.MethodType.EapType.VendorType",state.EapError.MethodType.EapType.VendorType.ToString());
        TraceEvent(Guid.Empty, "", "EapError.MethodType.AuthorId",          state.EapError.MethodType.AuthorId.ToString());
        TraceEvent(Guid.Empty, "", "EapError.Reason", "0x" + state.EapError.Reason.ToString("X8"));
        TraceEvent(Guid.Empty, "", "EapError.RootCauseGuid", state.EapError.RootCauseGuid.ToString());
        TraceEvent(Guid.Empty, "", "EapError.RepairGuid",    state.EapError.RepairGuid.ToString());
        TraceEvent(Guid.Empty, "", "EapError.HelpLinkGuid",  state.EapError.HelpLinkGuid.ToString());
        TraceEvent(Guid.Empty, "", "EapError.RootCauseString", state.EapError.RootCauseString);
        TraceEvent(Guid.Empty, "", "EapError.RepairString",    state.EapError.RepairString);
    }

    // ==================================================================
    //  IP changed
    // ==================================================================
    private void EventsOnIpChanged(object s, Guid iface, wclWiFiIpSettings old, wclWiFiIpSettings @new)
    {
        TraceEvent(iface, "IP changed", "Old address", old.Address);
        TraceEvent(Guid.Empty, "IP changed", "New address", @new.Address);
    }

    // ==================================================================
    //  Power monitor
    // ==================================================================
    private void PowerMonitorOnPowerStateChanged(object s, wclPowerState state)
    {
        switch (state)
        {
            case wclPowerState.psResumeAutomatic:    TraceEvent(Guid.Empty, "Power", "State", "psResumeAutomatic"); break;
            case wclPowerState.psResume:             TraceEvent(Guid.Empty, "Power", "State", "psResume"); break;
            case wclPowerState.psSuspend:            TraceEvent(Guid.Empty, "Power", "State", "psSuspend"); break;
            case wclPowerState.psPowerStatusChanged: TraceEvent(Guid.Empty, "Power", "State", "psPowerStatusChanged"); break;
            case wclPowerState.psUnknown:            TraceEvent(Guid.Empty, "Power", "State", "psUnknown"); break;
        }
    }

    // ==================================================================
    //  ACM trace helper
    // ==================================================================
    private void TraceAcmConnectionEvent(Guid iface, string eventName, wclWiFiAcmConnectionEventData data)
    {
        TraceEvent(iface, eventName, "ConnectionMode", data.ConnectionMode.ToString());
        TraceEvent(Guid.Empty, "", "Profile name", data.ProfileName);
        TraceEvent(Guid.Empty, "", "SSID", data.Ssid);
        TraceEvent(Guid.Empty, "", "BSS type", data.BssType.ToString());
        TraceEvent(Guid.Empty, "", "Security enabled", data.SecurityEnabled.ToString());
        TraceEvent(Guid.Empty, "", "Reason 0x", data.Reason.ToString("X8"));
        TraceEvent(Guid.Empty, "", "Flags", data.Flags.ToString());
        TraceEvent(Guid.Empty, "", "Profile XML", data.ProfileXml);
    }

    // ==================================================================
    //  Table row / log
    // ==================================================================
    private void TraceEvent(Guid iface, string eventName, string paramName, string paramValue)
    {
        string id = (iface == Guid.Empty) ? "" : iface.ToString();

        if (eventsContent == null || eventRowPrefab == null) return;

        var go = Instantiate(eventRowPrefab, eventsContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, id);
        row.SetCell(1, eventName);
        row.SetCell(2, paramName);
        row.SetCell(3, paramValue);

        var sr = eventsContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }

    private void Log(string msg) => TraceEvent(Guid.Empty, "LOG", "", msg);

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}