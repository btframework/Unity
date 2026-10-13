using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;
using wclCommunication;

public class WiiRemoteDemo : MonoBehaviour
{
    // ---------------- Top bar ----------------
    public Button discoverButton;
    public Button connectButton;
    public Button disconnectButton;

    public TMP_Text battLevelLabel;

    public Button getLedsButton;
    public Button getStatusButton;
    public Button setLedsButton;
    public Button rumbleOnButton;
    public Button rumbleOffButton;

    public Toggle led1Toggle;
    public Toggle led2Toggle;
    public Toggle led3Toggle;
    public Toggle led4Toggle;

    // ---------------- Devices ----------------
    public Transform  devicesContent;
    public GameObject deviceRowPrefab;

    // ---------------- Buttons group ----------------
    public Toggle aToggle;
    public Toggle bToggle;
    public Toggle plusToggle;
    public Toggle homeToggle;
    public Toggle minusToggle;
    public Toggle oneToggle;
    public Toggle twoToggle;
    public Toggle upToggle;
    public Toggle downToggle;
    public Toggle leftToggle;
    public Toggle rightToggle;

    // ---------------- Accelerometer ----------------
    public TMP_Text accelXLabel;
    public TMP_Text accelYLabel;
    public TMP_Text accelZLabel;
    public Button   enableAccelButton;
    public Button   disableAccelButton;

    // ---------------- IR ----------------
    public TMP_Text irModeLabel;
    public TMP_Text irSensitivityLabel;

    public TMP_Text irSensor0PositionLabel;
    public TMP_Text irSensor0SizeLabel;
    public TMP_Text irSensor0FoundLabel;

    public TMP_Text irSensor1PositionLabel;
    public TMP_Text irSensor1SizeLabel;
    public TMP_Text irSensor1FoundLabel;

    public TMP_Text irSensor2PositionLabel;
    public TMP_Text irSensor2SizeLabel;
    public TMP_Text irSensor2FoundLabel;

    public TMP_Text irSensor3PositionLabel;
    public TMP_Text irSensor3SizeLabel;
    public TMP_Text irSensor3FoundLabel;

    public Button enableIrButton;
    public Button disableIrButton;

    // ---------------- Nunchuk ----------------
    public TMP_Text nunchukStateLabel;
    public TMP_Text nunchukXLabel;
    public TMP_Text nunchukYLabel;
    public TMP_Text nunchukZLabel;
    public TMP_Text nunchukJoyXLabel;
    public TMP_Text nunchukJoyYLabel;
    public Toggle   nunchukCToggle;
    public Toggle   nunchukZToggle;

    // ---------------- Balance board ----------------
    public TMP_Text balanceBoardStateLabel;
    public TMP_Text bbTopRightLabel;
    public TMP_Text bbTopLeftLabel;
    public TMP_Text bbBottomRightLabel;
    public TMP_Text bbBottomLeftLabel;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;
    public Button     clearLogButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _manager;
    private wclWiiRemoteClient  _client;

    private TableRowUI _selectedDevice;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        discoverButton    .onClick.AddListener(OnDiscoverClicked);
        connectButton     .onClick.AddListener(OnConnectClicked);
        disconnectButton  .onClick.AddListener(OnDisconnectClicked);
        getLedsButton     .onClick.AddListener(OnGetLedsClicked);
        getStatusButton   .onClick.AddListener(OnGetStatusClicked);
        setLedsButton     .onClick.AddListener(OnSetLedsClicked);
        rumbleOnButton    .onClick.AddListener(OnRumbleOnClicked);
        rumbleOffButton   .onClick.AddListener(OnRumbleOffClicked);
        enableAccelButton .onClick.AddListener(OnEnableAccelClicked);
        disableAccelButton.onClick.AddListener(OnDisableAccelClicked);
        enableIrButton    .onClick.AddListener(OnEnableIrClicked);
        disableIrButton   .onClick.AddListener(OnDisableIrClicked);
        clearLogButton    .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();
        _manager.OnDiscoveringStarted   += ManagerOnDiscoveringStarted;
        _manager.OnDeviceFound          += ManagerOnDeviceFound;
        _manager.OnDiscoveringCompleted += ManagerOnDiscoveringCompleted;

        _client = new wclWiiRemoteClient();
        _client.OnConnect                += ClientOnConnect;
        _client.OnDisconnect             += ClientOnDisconnect;
        _client.OnStatusChanged          += ClientOnStatusChanged;
        _client.OnButtonsChanged         += ClientOnButtonsChanged;
        _client.OnAccelChanged           += ClientOnAccelChanged;
        _client.OnIrChanged              += ClientOnIrChanged;
        _client.OnExtensionAttached      += ClientOnExtensionAttached;
        _client.OnExtensionDetached      += ClientOnExtensionDetached;
        _client.OnNunchukChanged         += ClientOnNunchukChanged;
        _client.OnBalanceBoardChanged    += ClientOnBalanceBoardChanged;

        _manager.Open();

        ClearAccel();
        ClearIr();
        ClearExtensions();
    }

    private void OnDestroy()
    {
        try { _client?.Disconnect(); } catch (Exception e) { Debug.LogException(e); }
        try { _manager?.Close(); } catch (Exception e) { Debug.LogException(e); }
        _client = null;
        _manager = null;

        if (_manager != null)
        {
            _manager.OnDiscoveringStarted   -= ManagerOnDiscoveringStarted;
            _manager.OnDeviceFound          -= ManagerOnDeviceFound;
            _manager.OnDiscoveringCompleted -= ManagerOnDiscoveringCompleted;
        }

        if (_client != null)
        {
            _client.OnConnect                -= ClientOnConnect;
            _client.OnDisconnect             -= ClientOnDisconnect;
            _client.OnStatusChanged          -= ClientOnStatusChanged;
            _client.OnButtonsChanged         -= ClientOnButtonsChanged;
            _client.OnAccelChanged           -= ClientOnAccelChanged;
            _client.OnIrChanged              -= ClientOnIrChanged;
            _client.OnExtensionAttached      -= ClientOnExtensionAttached;
            _client.OnExtensionDetached      -= ClientOnExtensionDetached;
            _client.OnNunchukChanged         -= ClientOnNunchukChanged;
            _client.OnBalanceBoardChanged    -= ClientOnBalanceBoardChanged;
        }
    }

    // ==================================================================
    //  Manager events
    // ==================================================================
    private void ManagerOnDiscoveringStarted(object s, wclBluetoothRadio r)
    {
        ClearChildren(devicesContent);
        _selectedDevice = null;
        Log("Discovering started");
    }

    private void ManagerOnDeviceFound(object s, wclBluetoothRadio r, long addr)
    {
        var rowGO = Instantiate(deviceRowPrefab, devicesContent);
        var row = rowGO.GetComponent<TableRowUI>();
        row.Init(OnDeviceRowClicked);
        row.Tag = r;
        row.SetCell(0, addr.ToString("X12"));
        row.SetCell(1, "");
    }

    private void ManagerOnDiscoveringCompleted(object s, wclBluetoothRadio r, int error)
    {
        foreach (Transform child in devicesContent)
        {
            var row = child.GetComponent<TableRowUI>();
            if (row == null) continue;
            long addr;
            try { addr = Convert.ToInt64(row.GetCell(0), 16); } catch { continue; }
            string name;
            Int32 res = r.GetRemoteName(addr, out name);
            row.SetCell(1, res == wclErrors.WCL_E_SUCCESS ? name : "Error: 0x" + res.ToString("X9"));
        }
        Log("Discovering completed with result: 0x" + error.ToString("X8"));
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientOnConnect(object s, int error)
    {
        if (error != wclErrors.WCL_E_SUCCESS)
            Log("Connection failed: 0x" + error.ToString("X8"));
        else
        {
            Log("Connected");
            SetLeds();
        }
    }

    private void ClientOnDisconnect(object s, int reason)
    {
        Log("Disconnected. Reason: 0x" + reason.ToString("X8"));

        battLevelLabel.text = "0.00";
        SetAllToggles(false, led1Toggle, led2Toggle, led3Toggle, led4Toggle);
        SetAllToggles(false, aToggle, bToggle, plusToggle, homeToggle, minusToggle,
            oneToggle, twoToggle, upToggle, downToggle, leftToggle, rightToggle);

        ClearAccel();
        ClearIr();
        ClearExtensions();
    }

    private void ClientOnStatusChanged(object s, double batt, wclWiiRemoteLeds leds)
    {
        Log("Status changed");
        battLevelLabel.text = batt.ToString("N2");

        SetToggleSilent(led1Toggle, leds.Led1);
        SetToggleSilent(led2Toggle, leds.Led2);
        SetToggleSilent(led3Toggle, leds.Led3);
        SetToggleSilent(led4Toggle, leds.Led4);
    }

    private void ClientOnButtonsChanged(object s, wclWiiRemoteButtons buttons)
    {
        SetToggleSilent(aToggle,     buttons.A);
        SetToggleSilent(bToggle,     buttons.B);
        SetToggleSilent(plusToggle,  buttons.Plus);
        SetToggleSilent(homeToggle,  buttons.Home);
        SetToggleSilent(minusToggle, buttons.Minus);
        SetToggleSilent(oneToggle,   buttons.One);
        SetToggleSilent(twoToggle,   buttons.Two);
        SetToggleSilent(upToggle,    buttons.Up);
        SetToggleSilent(downToggle,  buttons.Down);
        SetToggleSilent(leftToggle,  buttons.Left);
        SetToggleSilent(rightToggle, buttons.Right);
    }

    private void ClientOnAccelChanged(object s, wclWiiRemoteAccel accel)
    {
        accelXLabel.text = accel.Values.X.ToString();
        accelYLabel.text = accel.Values.Y.ToString();
        accelZLabel.text = accel.Values.Z.ToString();
    }

    private void ClientOnIrChanged(object s, wclWiiRemoteIrSensors ir)
    {
        switch (ir.Mode)
        {
            case wclWiiRemoteIrMode.wiiIrOff:          ClearIr(); return;
            case wclWiiRemoteIrMode.wiiIrBasicMode:    irModeLabel.text = "Basic";    break;
            case wclWiiRemoteIrMode.wiiIrExtendedMode: irModeLabel.text = "Extended"; break;
            case wclWiiRemoteIrMode.wiiIrFullMode:     irModeLabel.text = "Full";     break;
            default:                                    irModeLabel.text = "Unknown";  break;
        }

        switch (ir.Sensitivity)
        {
            case wclWiiRemoteIrSensitivity.wiiIrLevelOff: irSensitivityLabel.text = "Off";     break;
            case wclWiiRemoteIrSensitivity.wiiIrLevel1:   irSensitivityLabel.text = "Level 1"; break;
            case wclWiiRemoteIrSensitivity.wiiIrLevel2:   irSensitivityLabel.text = "Level 2"; break;
            case wclWiiRemoteIrSensitivity.wiiIrLevel3:   irSensitivityLabel.text = "Level 3"; break;
            case wclWiiRemoteIrSensitivity.wiiIrLevel4:   irSensitivityLabel.text = "Level 4"; break;
            case wclWiiRemoteIrSensitivity.wiiIrLevel5:   irSensitivityLabel.text = "Level 5"; break;
            case wclWiiRemoteIrSensitivity.wiiIrLevelMax: irSensitivityLabel.text = "Maximum"; break;
            default:                                       irSensitivityLabel.text = "Unknown"; break;
        }

        UpdateSensor(0, ir.Sensors[0], irSensor0PositionLabel, irSensor0SizeLabel, irSensor0FoundLabel);
        UpdateSensor(1, ir.Sensors[1], irSensor1PositionLabel, irSensor1SizeLabel, irSensor1FoundLabel);
        UpdateSensor(2, ir.Sensors[2], irSensor2PositionLabel, irSensor2SizeLabel, irSensor2FoundLabel);
        UpdateSensor(3, ir.Sensors[3], irSensor3PositionLabel, irSensor3SizeLabel, irSensor3FoundLabel);
    }

    private static void UpdateSensor(int idx, wclWiiRemoteIrSensor sensor,
        TMP_Text posLabel, TMP_Text sizeLabel, TMP_Text foundLabel)
    {
        if (sensor.Found)
        {
            posLabel.text   = sensor.Position.X + "; " + sensor.Position.Y;
            sizeLabel.text  = sensor.Size.ToString();
            foundLabel.text = "True";
        }
        else
        {
            posLabel.text   = "0; 0";
            sizeLabel.text  = "0";
            foundLabel.text = "False";
        }
    }

    private void ClientOnExtensionAttached(object s, EventArgs e)
    {
        string ext;
        switch (_client.Extension)
        {
            case wclWiiRemoteExtension.wiiNoExtension:        ext = "wiiNoExtension"; break;
            case wclWiiRemoteExtension.wiiNunchuk:
                ext = "wiiNunchuk";
                nunchukStateLabel.text = "Attached";
                break;
            case wclWiiRemoteExtension.wiiClassicController:  ext = "wiiClassicController"; break;
            case wclWiiRemoteExtension.wiiGuitar:             ext = "wiiGuitar"; break;
            case wclWiiRemoteExtension.wiiDrums:              ext = "wiiDrums"; break;
            case wclWiiRemoteExtension.wiiBalanceBoard:
                ext = "wiiBalanceBoard";
                balanceBoardStateLabel.text = "Attached";
                break;
            default:                                           ext = "Unknown"; break;
        }
        Log("Extension attached: " + ext);
    }

    private void ClientOnExtensionDetached(object s, EventArgs e)
    {
        Log("Extension detached");
        ClearExtensions();
    }

    private void ClientOnNunchukChanged(object s, wclWiiRemoteNunchuk n)
    {
        nunchukXLabel.text = n.Accel.Values.X.ToString();
        nunchukYLabel.text = n.Accel.Values.Y.ToString();
        nunchukZLabel.text = n.Accel.Values.Z.ToString();

        nunchukJoyXLabel.text = n.Joystick.X.ToString();
        nunchukJoyYLabel.text = n.Joystick.Y.ToString();

        SetToggleSilent(nunchukCToggle, n.C);
        SetToggleSilent(nunchukZToggle, n.Z);
    }

    private void ClientOnBalanceBoardChanged(object s, wclWiiRemoteBalanceBoard board)
    {
        bbTopRightLabel.text    = board.SensorsKg.TopRight.ToString("N2");
        bbTopLeftLabel.text     = board.SensorsKg.TopLeft.ToString("N2");
        bbBottomRightLabel.text = board.SensorsKg.BottomRight.ToString("N2");
        bbBottomLeftLabel.text  = board.SensorsKg.BottomLeft.ToString("N2");
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnDiscoverClicked()
    {
        var radio = GetRadio();
        if (radio == null) return;
        Int32 res = radio.Discover(10, wclBluetoothDiscoverKind.dkClassic);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Discover failed: 0x" + res.ToString("X8"));
    }

    private void OnConnectClicked()
    {
        if (_client.State != wclClientState.csDisconnected)
        {
            Log("Wii Remote is connected");
            return;
        }

        var radio = GetRadio();
        if (radio == null) return;
        if (_selectedDevice == null) { Log("Select device"); return; }

        try
        {
            _client.Address = Convert.ToInt64(_selectedDevice.GetCell(0), 16);
            Int32 res = _client.Connect(radio);
            if (res != wclErrors.WCL_E_SUCCESS)
                Log("Unable connect: 0x" + res.ToString("X8"));
        }
        catch (Exception e) { Log("Connect exception: " + e.Message); }
    }

    private void OnDisconnectClicked()
    {
        Int32 res = _client.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Unable disconnect: 0x" + res.ToString("X8"));
    }

    private void OnGetStatusClicked()
    {
        Int32 res = _client.GetStatus();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Get status failed: 0x" + res.ToString("X8"));
    }

    private void OnGetLedsClicked()
    {
        if (_client.State != wclClientState.csConnected)
        {
            Log("Not connected");
            return;
        }
        SetToggleSilent(led1Toggle, _client.Leds.Led1);
        SetToggleSilent(led2Toggle, _client.Leds.Led2);
        SetToggleSilent(led3Toggle, _client.Leds.Led3);
        SetToggleSilent(led4Toggle, _client.Leds.Led4);
    }

    private void OnSetLedsClicked() => SetLeds();

    private void SetLeds()
    {
        Int32 res = _client.SetLeds(led1Toggle.isOn, led2Toggle.isOn,
            led3Toggle.isOn, led4Toggle.isOn);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set LEDs failed: 0x" + res.ToString("X8"));
    }

    private void OnRumbleOnClicked()
    {
        Int32 res = _client.SetRumble(true);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set Rumble ON failed: 0x" + res.ToString("X8"));
    }

    private void OnRumbleOffClicked()
    {
        Int32 res = _client.SetRumble(false);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Set Rumble OFF failed: 0x" + res.ToString("X8"));
    }

    private void OnEnableAccelClicked()
    {
        Int32 res = _client.EnableAccel();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Enable accelerometer failed: 0x" + res.ToString("X8"));
    }

    private void OnDisableAccelClicked()
    {
        Int32 res = _client.DisableAccel();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Disable accelerometer failed: 0x" + res.ToString("X8"));
        else
            ClearAccel();
    }

    private void OnEnableIrClicked()
    {
        Int32 res = _client.SetIrSensitivity(wclWiiRemoteIrSensitivity.wiiIrLevelMax);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Enable IR failed: 0x" + res.ToString("X8"));
    }

    private void OnDisableIrClicked()
    {
        Int32 res = _client.SetIrSensitivity(wclWiiRemoteIrSensitivity.wiiIrLevelOff);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Disable IR failed: 0x" + res.ToString("X8"));
        else
            ClearIr();
    }

    // ==================================================================
    //  Row handler
    // ==================================================================
    private void OnDeviceRowClicked(TableRowUI row)
    {
        if (_selectedDevice != null) _selectedDevice.SetSelected(false);
        _selectedDevice = row;
        if (_selectedDevice != null) _selectedDevice.SetSelected(true);
    }

    // ==================================================================
    //  Clear helpers
    // ==================================================================
    private void ClearAccel()
    {
        accelXLabel.text = "0";
        accelYLabel.text = "0";
        accelZLabel.text = "0";
    }

    private void ClearIr()
    {
        irModeLabel.text        = "Off";
        irSensitivityLabel.text = "Off";

        SetSensorNone(irSensor0PositionLabel, irSensor0SizeLabel, irSensor0FoundLabel);
        SetSensorNone(irSensor1PositionLabel, irSensor1SizeLabel, irSensor1FoundLabel);
        SetSensorNone(irSensor2PositionLabel, irSensor2SizeLabel, irSensor2FoundLabel);
        SetSensorNone(irSensor3PositionLabel, irSensor3SizeLabel, irSensor3FoundLabel);
    }

    private static void SetSensorNone(TMP_Text pos, TMP_Text size, TMP_Text found)
    {
        pos.text   = "0; 0";
        size.text  = "0";
        found.text = "False";
    }

    private void ClearExtensions()
    {
        ClearNunchuk();
        ClearBalanceBoard();
    }

    private void ClearNunchuk()
    {
        nunchukStateLabel.text = "Detached";
        nunchukXLabel.text = "0";
        nunchukYLabel.text = "0";
        nunchukZLabel.text = "0";
        nunchukJoyXLabel.text = "0";
        nunchukJoyYLabel.text = "0";
        SetToggleSilent(nunchukCToggle, false);
        SetToggleSilent(nunchukZToggle, false);
    }

    private void ClearBalanceBoard()
    {
        balanceBoardStateLabel.text = "Detached";
        bbTopRightLabel.text    = "0";
        bbTopLeftLabel.text     = "0";
        bbBottomRightLabel.text = "0";
        bbBottomLeftLabel.text  = "0";
    }

    // ==================================================================
    //  Misc
    // ==================================================================
    private wclBluetoothRadio GetRadio()
    {
        wclBluetoothRadio radio;
        Int32 res = _manager.GetClassicRadio(out radio);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Get working radio failed: 0x" + res.ToString("X8"));
            return null;
        }
        return radio;
    }

    private static void SetToggleSilent(Toggle t, bool value)
    {
        if (t != null) t.SetIsOnWithoutNotify(value);
    }

    private static void SetAllToggles(bool value, params Toggle[] toggles)
    {
        foreach (var t in toggles) SetToggleSilent(t, value);
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void Log(string msg)
    {
        Debug.Log("[WiiRemote] " + msg);
        if (logContent == null || logRowPrefab == null) return;

        var go = Instantiate(logRowPrefab, logContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, msg);

        var sr = logContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }
}