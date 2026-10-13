using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclSerialFramework;

public class SerialClientDemo : MonoBehaviour
{
    // ---------------- Top row ----------------
    public Button         enumButton;
    public TMP_Dropdown   portsDropdown;
    public Button         connectButton;
    public Button         disconnectButton;
    public TMP_Text       writeTimeoutLabel;
    public TMP_InputField writeTimeoutInput;
    public Button         setWriteTimeoutButton;

    // ---------------- Config buttons ----------------
    public Button getConfigButton;
    public Button setConfigButton;
    public Button getBuffersButton;
    public Button setBuffersButton;
    public Button clearCommBreakButton;
    public Button setCommBreakButton;

    // ---------------- Config fields (col 1) ----------------
    public TMP_Text       baudRateLabel;
    public TMP_InputField baudRateInput;
    public TMP_Text       rtsControlLabel;
    public TMP_Dropdown   rtsControlDropdown;
    public TMP_Text       parityLabel;
    public TMP_Dropdown   parityDropdown;
    public TMP_Text       xonLimLabel;
    public TMP_InputField xonLimInput;

    // ---------------- Config fields (col 2) ----------------
    public TMP_Text       dtrControlLabel;
    public TMP_Dropdown   dtrControlDropdown;
    public TMP_Text       byteSizeLabel;
    public TMP_Dropdown   byteSizeDropdown;
    public TMP_Text       stopBitsLabel;
    public TMP_Dropdown   stopBitsDropdown;
    public TMP_Text       xoffLimLabel;
    public TMP_InputField xoffLimInput;

    // ---------------- Char fields ----------------
    public TMP_Text       xonCharLabel;
    public TMP_InputField xonCharInput;
    public TMP_Text       xoffCharLabel;
    public TMP_InputField xoffCharInput;
    public TMP_Text       errorCharLabel;
    public TMP_InputField errorCharInput;
    public TMP_Text       eofCharLabel;
    public TMP_InputField eofCharInput;
    public TMP_Text       evtCharLabel;
    public TMP_InputField evtCharInput;

    // ---------------- Config checkboxes (col 1) ----------------
    public Toggle parityCheckToggle;
    public Toggle outxDsrFlowToggle;
    public Toggle txContinueOnXoffToggle;
    public Toggle errorCharReplaceToggle;
    public Toggle nullStripToggle;

    // ---------------- Config checkboxes (col 2) ----------------
    public Toggle outxCtsFlowToggle;
    public Toggle dsrSensitivityToggle;
    public Toggle outXToggle;
    public Toggle inXToggle;
    public Toggle abortOnErrorToggle;

    // ---------------- Buffers ----------------
    public TMP_Text       readBufferSizeLabel;
    public TMP_InputField readBufferSizeInput;
    public TMP_Text       writeBufferSizeLabel;
    public TMP_InputField writeBufferSizeInput;

    // ---------------- Timeouts ----------------
    public Button         getTimeoutsButton;
    public Button         setTimeoutsButton;
    public TMP_Text       readIntervalLabel;
    public TMP_InputField readIntervalInput;
    public TMP_Text       readMultiplierLabel;
    public TMP_InputField readMultiplierInput;
    public TMP_Text       readConstantLabel;
    public TMP_InputField readConstantInput;
    public TMP_Text       writeMultiplierLabel;
    public TMP_InputField writeMultiplierInput;
    public TMP_Text       writeConstantLabel;
    public TMP_InputField writeConstantInput;

    // ---------------- Function / Purge ----------------
    public TMP_Text     funcLabel;
    public TMP_Dropdown funcDropdown;
    public Button       funcButton;
    public Toggle       purgeRxAbortToggle;
    public Toggle       purgeRxClearToggle;
    public Toggle       purgeTxAbortToggle;
    public Toggle       purgeTxClearToggle;
    public Button       purgeButton;
    public TMP_Text       charCodeLabel;
    public TMP_InputField charInput;
    public Button         transmitButton;
    public Button         flushBuffersButton;

    // ---------------- Send ----------------
    public TMP_InputField textInput;
    public Button         sendButton;
    public TMP_Text       lineFeedLabel;
    public TMP_Dropdown   lineFeedDropdown;
    public Button         clearButton;

    // ---------------- Events ----------------
    public Transform  eventsContent;
    public GameObject eventRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclSerialClient  _client;
    private wclSerialMonitor _monitor;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        enumButton           .onClick.AddListener(OnEnumClicked);
        connectButton        .onClick.AddListener(OnConnectClicked);
        disconnectButton     .onClick.AddListener(OnDisconnectClicked);
        setWriteTimeoutButton.onClick.AddListener(OnSetWriteTimeoutClicked);
        getConfigButton      .onClick.AddListener(OnGetConfigClicked);
        setConfigButton      .onClick.AddListener(OnSetConfigClicked);
        getBuffersButton     .onClick.AddListener(OnGetBuffersClicked);
        setBuffersButton     .onClick.AddListener(OnSetBuffersClicked);
        clearCommBreakButton .onClick.AddListener(OnClearCommBreakClicked);
        setCommBreakButton   .onClick.AddListener(OnSetCommBreakClicked);
        getTimeoutsButton    .onClick.AddListener(OnGetTimeoutsClicked);
        setTimeoutsButton    .onClick.AddListener(OnSetTimeoutsClicked);
        funcButton           .onClick.AddListener(OnFuncClicked);
        purgeButton          .onClick.AddListener(OnPurgeClicked);
        transmitButton       .onClick.AddListener(OnTransmitClicked);
        flushBuffersButton   .onClick.AddListener(OnFlushBuffersClicked);
        sendButton           .onClick.AddListener(OnSendClicked);
        clearButton          .onClick.AddListener(() => ClearChildren(eventsContent));
    }

    private void Start()
    {
        EnsureDropdown(rtsControlDropdown,  new[] { "rtsControlDisable", "rtsControlEnable", "rtsControlHandshake", "rtsControlToggle" }, -1);
        EnsureDropdown(dtrControlDropdown,  new[] { "dtrControlDisable", "dtrControlEnable", "dtrControlHandshake" }, -1);
        EnsureDropdown(parityDropdown,      new[] { "spNo", "spOdd", "spEven", "spMark", "spSpace" }, -1);
        EnsureDropdown(stopBitsDropdown,    new[] { "sbOne", "sbOne5", "sbTwo" }, -1);
        EnsureDropdown(byteSizeDropdown,    new[] { "4", "5", "6", "7", "8" }, -1);
        EnsureDropdown(funcDropdown,        new[]
        {
            "escClrBreak", "escClrDtr", "escClrRts", "escSetBreak",
            "escSetDtr", "escSetRts", "escSetXoff", "escSetXon"
        }, 0);
        EnsureDropdown(lineFeedDropdown, new[] { "None", "CR", "LF", "CR & LF" }, 0);

        _client = new wclSerialClient();
        _client.OnConnect    += ClientConnect;
        _client.OnData       += ClientData;
        _client.OnDisconnect += ClientDisconnect;
        _client.OnError      += ClientError;
        _client.OnReadError  += ClientReadError;
        _client.OnEvents     += ClientEvents;

        _monitor = new wclSerialMonitor();

        EnumComPorts();

        ClearConfig();
        ClearTimeouts();
        ClearBuffers();

        writeTimeoutInput.text = _client.WriteTimeout.ToString();
    }

    private void OnDestroy()
    {
        if (_client != null)
        {
            try { _client.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
            _client.OnConnect    -= ClientConnect;
            _client.OnData       -= ClientData;
            _client.OnDisconnect -= ClientDisconnect;
            _client.OnError      -= ClientError;
            _client.OnReadError  -= ClientReadError;
            _client.OnEvents     -= ClientEvents;
            _client = null;
        }

        if (_monitor != null)
        {
            try { _monitor.Stop(); } catch (Exception e) { Debug.LogException(e); }
            _monitor = null;
        }
    }

    // ==================================================================
    //  Enum <-> index mappers (as in original)
    // ==================================================================
    private static int DtrControlToIndex(wclSerialDtrControl c)
    {
        switch (c)
        {
            case wclSerialDtrControl.dtrControlDisable:   return 0;
            case wclSerialDtrControl.dtrControlEnable:    return 1;
            case wclSerialDtrControl.dtrControlHandshake: return 2;
            default:                                       return -1;
        }
    }

    private static wclSerialDtrControl IndexToDtrControl(int index)
    {
        switch (index)
        {
            case 0:  return wclSerialDtrControl.dtrControlDisable;
            case 1:  return wclSerialDtrControl.dtrControlEnable;
            case 2:  return wclSerialDtrControl.dtrControlHandshake;
            default: return wclSerialDtrControl.dtrControlDisable;
        }
    }

    private static int RtsControlToIndex(wclSerialRtsControl c)
    {
        switch (c)
        {
            case wclSerialRtsControl.rtsControlDisable:   return 0;
            case wclSerialRtsControl.rtsControlEnable:    return 1;
            case wclSerialRtsControl.rtsControlHandshake: return 2;
            case wclSerialRtsControl.rtsControlToggle:    return 3;
            default:                                       return -1;
        }
    }

    private static wclSerialRtsControl IndexToRtsControl(int index)
    {
        switch (index)
        {
            case 0:  return wclSerialRtsControl.rtsControlDisable;
            case 1:  return wclSerialRtsControl.rtsControlEnable;
            case 2:  return wclSerialRtsControl.rtsControlHandshake;
            case 3:  return wclSerialRtsControl.rtsControlToggle;
            default: return wclSerialRtsControl.rtsControlDisable;
        }
    }

    private static int ParityToIndex(wclSerialParity p)
    {
        switch (p)
        {
            case wclSerialParity.spNo:    return 0;
            case wclSerialParity.spOdd:   return 1;
            case wclSerialParity.spEven:  return 2;
            case wclSerialParity.spMark:  return 3;
            case wclSerialParity.spSpace: return 4;
            default:                       return -1;
        }
    }

    private static wclSerialParity IndexToParity(int index)
    {
        switch (index)
        {
            case 0:  return wclSerialParity.spNo;
            case 1:  return wclSerialParity.spOdd;
            case 2:  return wclSerialParity.spEven;
            case 3:  return wclSerialParity.spMark;
            case 4:  return wclSerialParity.spSpace;
            default: return wclSerialParity.spNo;
        }
    }

    private static int StopBitsToIndex(wclSerialStopBits s)
    {
        switch (s)
        {
            case wclSerialStopBits.sbOne:  return 0;
            case wclSerialStopBits.sbOne5: return 1;
            case wclSerialStopBits.sbTwo:  return 2;
            default:                        return -1;
        }
    }

    private static wclSerialStopBits IndexToStopBits(int index)
    {
        switch (index)
        {
            case 0:  return wclSerialStopBits.sbOne;
            case 1:  return wclSerialStopBits.sbOne5;
            case 2:  return wclSerialStopBits.sbTwo;
            default: return wclSerialStopBits.sbOne;
        }
    }

    // ==================================================================
    //  Config / timeouts / buffers helpers
    // ==================================================================
    private void ReadConfiguration()
    {
        wclSerialConfig config;
        Int32 res = _client.GetConfig(out config);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Read configuration error: 0x" + res.ToString("X8"));
            return;
        }

        baudRateInput.text  = config.BaudRate.ToString();
        xonLimInput.text    = config.XonLim.ToString();
        xoffLimInput.text   = config.XoffLim.ToString();
        xonCharInput.text   = config.XonChar.ToString();
        xoffCharInput.text  = config.XoffChar.ToString();
        errorCharInput.text = config.ErrorChar.ToString();
        eofCharInput.text   = config.EofChar.ToString();
        evtCharInput.text   = config.EvtChar.ToString();

        parityCheckToggle      .isOn = config.ParityCheck;
        outxCtsFlowToggle      .isOn = config.OutxCtsFlow;
        outxDsrFlowToggle      .isOn = config.OutxDsrFlow;
        dsrSensitivityToggle   .isOn = config.DsrSensitivity;
        txContinueOnXoffToggle .isOn = config.TxContinueOnXoff;
        outXToggle             .isOn = config.OutX;
        inXToggle              .isOn = config.InX;
        errorCharReplaceToggle .isOn = config.ErrorCharReplace;
        nullStripToggle        .isOn = config.NullStrip;
        abortOnErrorToggle     .isOn = config.AbortOnError;

        SetDropdownValue(rtsControlDropdown, RtsControlToIndex(config.RtsControl));
        SetDropdownValue(dtrControlDropdown, DtrControlToIndex(config.DtrControl));
        SetDropdownValue(parityDropdown,     ParityToIndex(config.Parity));
        SetDropdownValue(stopBitsDropdown,   StopBitsToIndex(config.StopBits));
        SetDropdownValue(byteSizeDropdown,   config.ByteSize - 4);
    }

    private void ReadTimeouts()
    {
        wclSerialTimeouts times;
        Int32 res = _client.GetTimeouts(out times);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get timeouts error: 0x" + res.ToString("X8"));
            return;
        }

        readIntervalInput   .text = times.ReadInterval.ToString();
        readMultiplierInput .text = times.ReadMultiplier.ToString();
        readConstantInput   .text = times.ReadConstant.ToString();
        writeMultiplierInput.text = times.WriteMultiplier.ToString();
        writeConstantInput  .text = times.WriteConstant.ToString();
    }

    private void ClearConfig()
    {
        baudRateInput .text = "";
        xonLimInput   .text = "";
        xoffLimInput  .text = "";
        xonCharInput  .text = "";
        xoffCharInput .text = "";
        errorCharInput.text = "";
        eofCharInput  .text = "";
        evtCharInput  .text = "";

        parityCheckToggle     .isOn = false;
        outxCtsFlowToggle     .isOn = false;
        outxDsrFlowToggle     .isOn = false;
        dsrSensitivityToggle  .isOn = false;
        txContinueOnXoffToggle.isOn = false;
        outXToggle            .isOn = false;
        inXToggle             .isOn = false;
        errorCharReplaceToggle.isOn = false;
        nullStripToggle       .isOn = false;
        abortOnErrorToggle    .isOn = false;

        SetDropdownValue(rtsControlDropdown, -1);
        SetDropdownValue(dtrControlDropdown, -1);
        SetDropdownValue(byteSizeDropdown,   -1);
        SetDropdownValue(parityDropdown,     -1);
        SetDropdownValue(stopBitsDropdown,   -1);
    }

    private void ClearTimeouts()
    {
        readIntervalInput   .text = "";
        readMultiplierInput .text = "";
        readConstantInput   .text = "";
        writeMultiplierInput.text = "";
        writeConstantInput  .text = "";
    }

    private void ClearBuffers()
    {
        readBufferSizeInput .text = "";
        writeBufferSizeInput.text = "";
    }

    private void ReadBuffers()
    {
        UInt32 size;
        Int32 res = _client.GetReadBufferSize(out size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get read buffer size error: 0x" + res.ToString("X8"));
        else
            readBufferSizeInput.text = size.ToString();

        res = _client.GetWriteBufferSize(out size);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get write buffer size error: 0x" + res.ToString("X8"));
        else
            writeBufferSizeInput.text = size.ToString();
    }

    private void EnumComPorts()
    {
        portsDropdown.ClearOptions();

        List<wclSerialDevice> ports;
        Int32 res = _monitor.EnumSerialDevices(out ports);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Error enumerating COM ports: 0x" + res.ToString("X8"));
            return;
        }

        if (ports != null && ports.Count > 0)
        {
            var names = new List<string>();
            foreach (var port in ports) names.Add(port.DeviceName);
            portsDropdown.AddOptions(names);
            portsDropdown.SetValueWithoutNotify(0);
            portsDropdown.RefreshShownValue();
        }
    }

    // ==================================================================
    //  Buttons - top row
    // ==================================================================
    private void OnEnumClicked() => EnumComPorts();

    private void OnConnectClicked()
    {
        if (portsDropdown.options.Count == 0) { Log("Select COM port"); return; }
        Int32 res = _client.Connect(portsDropdown.options[portsDropdown.value].text);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnDisconnectClicked()
    {
        Int32 res = _client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnSetWriteTimeoutClicked()
    {
        _client.WriteTimeout = ParseUInt(writeTimeoutInput.text, 0);
    }

    // ==================================================================
    //  Config / Buffers / Timeouts
    // ==================================================================
    private void OnGetConfigClicked()  => ReadConfiguration();
    private void OnSetConfigClicked()
    {
        var config = new wclSerialConfig
        {
            BaudRate      = ParseUInt(baudRateInput.text, 0),
            XonLim        = ParseUShort(xonLimInput.text, 0),
            XoffLim       = ParseUShort(xoffLimInput.text, 0),
            XonChar       = ParseByte(xonCharInput.text, 0),
            XoffChar      = ParseByte(xoffCharInput.text, 0),
            ErrorChar     = ParseByte(errorCharInput.text, 0),
            EofChar       = ParseByte(eofCharInput.text, 0),
            EvtChar       = ParseByte(evtCharInput.text, 0),

            ParityCheck      = parityCheckToggle.isOn,
            OutxCtsFlow      = outxCtsFlowToggle.isOn,
            OutxDsrFlow      = outxDsrFlowToggle.isOn,
            DsrSensitivity   = dsrSensitivityToggle.isOn,
            TxContinueOnXoff = txContinueOnXoffToggle.isOn,
            OutX             = outXToggle.isOn,
            InX              = inXToggle.isOn,
            ErrorCharReplace = errorCharReplaceToggle.isOn,
            NullStrip        = nullStripToggle.isOn,
            AbortOnError     = abortOnErrorToggle.isOn,

            RtsControl = IndexToRtsControl(rtsControlDropdown.value),
            DtrControl = IndexToDtrControl(dtrControlDropdown.value),
            Parity     = IndexToParity(parityDropdown.value),
            StopBits   = IndexToStopBits(stopBitsDropdown.value),
            ByteSize   = (byte)(byteSizeDropdown.value + 4),
        };

        Int32 res = _client.SetConfig(config);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnGetBuffersClicked() => ReadBuffers();

    private void OnSetBuffersClicked()
    {
        Int32 res = _client.SetReadBufferSize(ParseUInt(readBufferSizeInput.text, 0));
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set read buffer size error: 0x" + res.ToString("X8"));

        res = _client.SetWriteBufferSize(ParseUInt(writeBufferSizeInput.text, 0));
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set write buffer size error: 0x" + res.ToString("X8"));
    }

    private void OnGetTimeoutsClicked() => ReadTimeouts();

    private void OnSetTimeoutsClicked()
    {
        var times = new wclSerialTimeouts
        {
            ReadInterval    = ParseUInt(readIntervalInput.text, 0),
            ReadMultiplier  = ParseUInt(readMultiplierInput.text, 0),
            ReadConstant    = ParseUInt(readConstantInput.text, 0),
            WriteMultiplier = ParseUInt(writeMultiplierInput.text, 0),
            WriteConstant   = ParseUInt(writeConstantInput.text, 0),
        };
        Int32 res = _client.SetTimeouts(times);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set timeouts error: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Comm / Func / Purge / Transmit
    // ==================================================================
    private void OnClearCommBreakClicked()
    {
        Int32 res = _client.ClearCommBreak();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnSetCommBreakClicked()
    {
        Int32 res = _client.SetCommBreak();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnFlushBuffersClicked()
    {
        Int32 res = _client.FlushBuffers();
        if (res != wclErrors.WCL_E_SUCCESS) Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnFuncClicked()
    {
        Int32 res = _client.EscapeCommFunction((wclSerialEscapeFunction)funcDropdown.value);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnPurgeClicked()
    {
        wclSerialPurgeFlag flags = 0;
        if (purgeRxAbortToggle.isOn) flags |= wclSerialPurgeFlag.purgeRxAbort;
        if (purgeRxClearToggle.isOn) flags |= wclSerialPurgeFlag.purgeRxClear;
        if (purgeTxAbortToggle.isOn) flags |= wclSerialPurgeFlag.purgeTxAbort;
        if (purgeTxClearToggle.isOn) flags |= wclSerialPurgeFlag.purgeTxClear;

        Int32 res = _client.PurgeComm(flags);
        if (res != wclErrors.WCL_E_SUCCESS) Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnTransmitClicked()
    {
        Int32 res = _client.TransmitCommChar(ParseByte(charInput.text, 0));
        if (res != wclErrors.WCL_E_SUCCESS) Log("Error: 0x" + res.ToString("X8"));
    }

    private void OnSendClicked()
    {
        string str = textInput.text;
        switch (lineFeedDropdown.value)
        {
            case 1: str += "\r";     break;
            case 2: str += "\n";     break;
            case 3: str += "\r\n";   break;
        }

        byte[] ansi = Encoding.ASCII.GetBytes(str);
        UInt32 sent;
        Int32 res = _client.Write(ansi, out sent);

        Log("Sent: " + sent + " bytes from " + ansi.Length);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Write error: 0x" + res.ToString("X8"));
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientConnect(object s, Int32 error)
    {
        if (error == wclErrors.WCL_E_SUCCESS)
        {
            Log("Connected to Serial Device: " + _client.DeviceName);
            ReadConfiguration();
            ReadTimeouts();
            ReadBuffers();
        }
        else
        {
            Log("Connect error: 0x" + error.ToString("X8"));
        }
    }

    private void ClientData(object s, byte[] data)
    {
        if (data != null && data.Length > 0)
            Log("Received: " + Encoding.ASCII.GetString(data));
        else
            Log("Empty data received");
    }

    private void ClientDisconnect(object s, Int32 reason)
    {
        Log("Disconnected: 0x" + reason.ToString("X8"));
        ClearConfig();
        ClearTimeouts();
        ClearBuffers();
    }

    private void ClientError(object s, wclSerialError errors, wclSerialCommunicationState states)
    {
        string str = "";
        if ((errors & wclSerialError.erBreak)    != 0) str += "erBreak ";
        if ((errors & wclSerialError.erFrame)    != 0) str += "erFrame ";
        if ((errors & wclSerialError.erOverrun)  != 0) str += "erOverrun ";
        if ((errors & wclSerialError.erRxOver)   != 0) str += "erRxOver ";
        if ((errors & wclSerialError.erRxParity) != 0) str += "erRxParity ";
        Log("Error: " + str);

        str = "";
        if ((states & wclSerialCommunicationState.csCtsHold)   != 0) str += "csCtsHold ";
        if ((states & wclSerialCommunicationState.csDsrHold)   != 0) str += "csDsrHold ";
        if ((states & wclSerialCommunicationState.csRlsdHold)  != 0) str += "csRlsdHold ";
        if ((states & wclSerialCommunicationState.csXoffHold)  != 0) str += "csXoffHold ";
        if ((states & wclSerialCommunicationState.csXoffSent)  != 0) str += "csXoffSent ";
        if ((states & wclSerialCommunicationState.csEof)       != 0) str += "csEof ";
        if ((states & wclSerialCommunicationState.csTxim)      != 0) str += "csTxim ";
        Log("States: " + str);
    }

    private void ClientReadError(object s, Int32 error)
        => Log("Read error: 0x" + error.ToString("X8"));

    private void ClientEvents(object s, wclSerialEvent events)
    {
        string str = "";
        if ((events & wclSerialEvent.evBreak) != 0) str += "evBreak ";
        if ((events & wclSerialEvent.evCts)   != 0) str += "evCts ";
        if ((events & wclSerialEvent.evDsr)   != 0) str += "evDsr ";
        if ((events & wclSerialEvent.evRing)  != 0) str += "evRing ";
        if ((events & wclSerialEvent.evRlsd)  != 0) str += "evRlsd ";
        if ((events & wclSerialEvent.evChar)  != 0) str += "evChar ";
        Log("Event: " + str);

        if (events == 0) return;

        wclModemStatus status;
        Int32 res = _client.GetModemStatus(out status);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("GetModemStatus error: 0x" + res.ToString("X8"));
            return;
        }

        str = "";
        if ((status & wclModemStatus.msCtsOn)  != 0) str += "msCtsOn ";
        if ((status & wclModemStatus.msDsrOn)  != 0) str += "msDsrOn ";
        if ((status & wclModemStatus.msRingOn) != 0) str += "msRingOn ";
        if ((status & wclModemStatus.msRlsdOn) != 0) str += "msRlsdOn ";
        if (str != "") Log("Modem status: " + str);
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private static uint   ParseUInt  (string s, uint   fb) => uint  .TryParse(s, out var v) ? v : fb;
    private static ushort ParseUShort(string s, ushort fb) => ushort.TryParse(s, out var v) ? v : fb;
    private static byte   ParseByte  (string s, byte   fb) => byte  .TryParse(s, out var v) ? v : fb;

    private static void SetDropdownValue(TMP_Dropdown dd, int index)
    {
        if (dd == null) return;
        if (index < 0 || index >= dd.options.Count)
        {
            dd.SetValueWithoutNotify(-1);
            dd.RefreshShownValue();
            return;
        }
        dd.SetValueWithoutNotify(index);
        dd.RefreshShownValue();
    }

    private static void EnsureDropdown(TMP_Dropdown dd, string[] options, int defaultValue)
    {
        if (dd == null) return;
        if (dd.options == null || dd.options.Count != options.Length)
        {
            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
        }
        int v = defaultValue;
        if (v < 0) v = 0;                                    // TMP does not support -1
        dd.value = Mathf.Clamp(v, 0, options.Length - 1);
        dd.RefreshShownValue();
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void Log(string msg)
    {
        Debug.Log("[SerialClient] " + msg);
        if (eventsContent == null || eventRowPrefab == null) return;

        var go = Instantiate(eventRowPrefab, eventsContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, msg);

        var sr = eventsContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }
}