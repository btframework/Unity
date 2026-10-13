#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BluetoothManagerSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    private static readonly string[] MitmOptions = new[]
    {
        "mitmProtectionNotRequired",
        "mitmProtectionRequired",
        "mitmProtectionNotRequiredBonding",
        "mitmProtectionRequiredBonding",
        "mitmProtectionNotRequiredGeneralBonding",
        "mitmProtectionRequiredGeneralBonding",
        "mitmProtectionNotDefined"
    };

    private static readonly string[] IoCapOptions = new[]
    {
        "iocapDisplayOnly", "iocapDisplayYesNo", "iocapKeyboardOnly",
        "iocapNoInputNoOutput", "iocapDisplayKeyboard", "iocapNotDefined"
    };

    private static readonly string[] BleProtOptions = new[]
    {
        "pplDefault", "pplNone", "pplEncryption", "pplEncryptionAndAuthentication"
    };

    private static readonly string[] PairingOptions = new[] { "pmAuto", "pmClassic", "pmLe" };

    [MenuItem("Tools/Bluetooth Manager/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("BluetoothManagerUI");
        DestroyIfExists("BluetoothManagerDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("BluetoothManagerUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Row 1: Open | Close | Handle pairing | Refresh | Connectable | Discoverable | Name | SetName | ON | OFF | DontTurnOff
        // ============================================================
        var r1 = MakeHBar("Row1", canvasGO.transform, 20, 40);
        var btOpen      = MakeButton("BtOpen",   "Open",   r1.transform, 100, 40);
        var btClose     = MakeButton("BtClose",  "Close",  r1.transform, 100, 40);
        var tgHandle    = MakeToggle("TgHandlePairing", "Handle pairing", r1.transform, 200, 40);
        var btRefreshR  = MakeButton("BtRefreshRadio", "Refresh", r1.transform, 110, 40);
        var btConnect   = MakeButton("BtConnectable",  "Connectable", r1.transform, 140, 40);
        var btDiscover  = MakeButton("BtDiscoverable", "Discoverable", r1.transform, 140, 40);
        var edName      = MakeInputField("EdName", r1.transform, 220, 40, "NewRadioName");
        var btSetName   = MakeButton("BtSetName", "Set name", r1.transform, 140, 40);
        var btTurnOn    = MakeButton("BtTurnOn",  "ON",  r1.transform, 70, 40);
        var btTurnOff   = MakeButton("BtTurnOff", "OFF", r1.transform, 70, 40);
        var tgDontOff   = MakeToggle("TgDontTurnOff", "Don't turn OFF", r1.transform, 220, 40);

        // ============================================================
        //  Row 2: Discover | Timeout | Mode | Terminate | Enum paired | Enum connected | RSSI | IsInRange | Refresh device
        // ============================================================
        var r2 = MakeHBar("Row2", canvasGO.transform, 70, 40);
        var btDiscoverBtn  = MakeButton("BtDiscover", "Discover", r2.transform, 130, 40);
        MakeLabel("Timeout", r2.transform, 100, 40);
        var edTimeout      = MakeInputField("EdTimeout", r2.transform, 100, 40, "10");
        MakeLabel("Mode", r2.transform, 80, 40);
        var ddDiscoverMode = MakeDropdown("DdDiscoverMode", r2.transform, 160, 40, new[] { "Classic", "BLE" });
        var btTerminate    = MakeButton("BtTerminate", "Terminate", r2.transform, 140, 40);
        var btEnumPaired   = MakeButton("BtEnumPaired", "Enum paired", r2.transform, 150, 40);
        var btEnumConn     = MakeButton("BtEnumConnected", "Enum connected", r2.transform, 170, 40);
        var btRssi         = MakeButton("BtRssi", "RSSI", r2.transform, 100, 40);
        var btIsInRange    = MakeButton("BtIsInRange", "Is in range", r2.transform, 150, 40);
        var btRefreshDev   = MakeButton("BtRefreshDevice", "Refresh", r2.transform, 120, 40);

        // ============================================================
        //  Row 3: Pair | PIN | Unpair | MITM | IO Cap | BLE Protection | Pairing Method
        // ============================================================
        var r3 = MakeHBar("Row3", canvasGO.transform, 120, 40);
        var btPair          = MakeButton("BtPair", "Pair", r3.transform, 100, 40);
        MakeLabel("PIN", r3.transform, 60, 40);
        var edPIN           = MakeInputField("EdPIN", r3.transform, 120, 40, "0000");
        var btUnpair        = MakeButton("BtUnpair", "Unpair", r3.transform, 120, 40);
        MakeLabel("MITM", r3.transform, 80, 40);
        var ddMitm          = MakeDropdown("DdMitm", r3.transform, 400, 40, MitmOptions);
        MakeLabel("IO Cap", r3.transform, 100, 40);
        var ddIoCap         = MakeDropdown("DdIoCap", r3.transform, 280, 40, IoCapOptions);
        MakeLabel("BLE Prot", r3.transform, 120, 40);
        var ddBleProt       = MakeDropdown("DdBleProt", r3.transform, 300, 40, BleProtOptions);
        MakeLabel("Pairing", r3.transform, 110, 40);
        var ddPairingMethod = MakeDropdown("DdPairing", r3.transform, 180, 40, PairingOptions);

        // ============================================================
        //  Row 4: Enum services | Installed services | Create vCOM | Destroy vCOM | Enum vCOMs | Preinstalled | Install | Uninstall | Disconnect
        // ============================================================
        var r4 = MakeHBar("Row4", canvasGO.transform, 170, 40);
        var btEnumServices = MakeButton("BtEnumServices", "Enum services", r4.transform, 170, 40);
        var btEnumInst     = MakeButton("BtEnumInstalled", "Installed services", r4.transform, 200, 40);
        var btCreateVcom   = MakeButton("BtCreateVcom", "Create vCOM", r4.transform, 160, 40);
        var btDestroyVcom  = MakeButton("BtDestroyVcom", "Destroy vCOM", r4.transform, 170, 40);
        var btEnumVcoms    = MakeButton("BtEnumVcoms", "Enum vCOMs", r4.transform, 160, 40);
        var tgEnumPre      = MakeToggle("TgEnumPreinstalled", "Enum preinstalled", r4.transform, 240, 40);
        var btInstallDev   = MakeButton("BtInstallDevice", "Install device", r4.transform, 170, 40);
        var btUninstallDev = MakeButton("BtUninstallDevice", "Uninstall device", r4.transform, 190, 40);
        var btDisconnect   = MakeButton("BtDisconnect", "Disconnect", r4.transform, 150, 40);

        // ============================================================
        //  Scroll tables
        // ============================================================
        // Radios (10 columns)
        var (radiosScroll, radiosContent) = MakeTable("RadiosScroll", canvasGO.transform,
            new Vector2(20, -220), new Vector2(-20, -100), 420,
            new[] { 100f, 70f, 110f, 90f, 90f, 80f, 90f, 90f, 120f, 120f },
            new[] { "Driver", "Available", "Address", "Connectable", "Discoverable",
                    "HCI ver.", "LMP ver.", "Manufacturer", "Name", "COD" });

        // Devices (8 columns)
        var (devicesScroll, devicesContent) = MakeTable("DevicesScroll", canvasGO.transform,
            new Vector2(20, -560), new Vector2(-20, -440), 170,
            new[] { 100f, 110f, 110f, 90f, 120f, 80f, 100f, 100f },
            new[] { "Driver", "Address", "COD", "Device type", "Name",
                    "Paired", "Connected", "Address type" });

        // Services (7 columns)
        var (servicesScroll, servicesContent) = MakeTable("ServicesScroll", canvasGO.transform,
            new Vector2(20, -740), new Vector2(-20, -620), 150,
            new[] { 100f, 110f, 90f, 400f, 90f, 260f, 140f },
            new[] { "Driver", "Address", "Handle", "UUID", "Channel", "Name", "Comment" });

        // vCOMs (4 columns)
        var (vcomsScroll, vcomsContent) = MakeTable("VcomsScroll", canvasGO.transform,
            new Vector2(20, -900), new Vector2(-20, -780), 120,
            new[] { 100f, 110f, 400f, 100f },
            new[] { "Driver", "Address", "UUID", "Number" });

        // Events (4 columns)
        var (eventsScroll, eventsContent) = MakeTable("EventsScroll", canvasGO.transform,
            new Vector2(20, -1030), new Vector2(-20, -920), 140,
            new[] { 100f, 260f, 260f, 500f },
            new[] { "Driver", "Event", "Parameter", "Value" });

        var clearEvents = MakeButton("BtClearEvents", "Clear", canvasGO.transform, 120, 40);
        var ceRect = clearEvents.GetComponent<RectTransform>();
        ceRect.anchorMin = new Vector2(1, 0); ceRect.anchorMax = new Vector2(1, 0);
        ceRect.pivot = new Vector2(1, 0);
        ceRect.anchoredPosition = new Vector2(-20, 20);

        // ============================================================
        //  Table row prefabs
        // ============================================================
        var radioRow    = MakeTableRowPrefab("RadioRow",   new[] { 100f, 70f, 110f, 90f, 90f, 80f, 90f, 90f, 120f, 120f });
        var deviceRow   = MakeTableRowPrefab("DeviceRow",  new[] { 100f, 110f, 110f, 90f, 120f, 80f, 100f, 100f });
        var serviceRow  = MakeTableRowPrefab("ServiceRow", new[] { 100f, 110f, 90f, 400f, 90f, 260f, 140f });
        var comPortRow  = MakeTableRowPrefab("ComPortRow", new[] { 100f, 110f, 400f, 100f });
        var eventRow    = MakeTableRowPrefab("EventRow",   new[] { 100f, 260f, 260f, 500f });

        // ============================================================
        //  Confirm dialog
        // ============================================================
        var (confirmPanel, confirmText, confirmYes, confirmNo, confirmCancel) =
            MakeConfirmDialog(canvasGO.transform);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("BluetoothManagerDemo");
        var demo = demoGO.AddComponent<BluetoothManagerDemo>();

        demo.openButton            = btOpen;
        demo.closeButton           = btClose;
        demo.handlePairingToggle   = tgHandle;
        demo.refreshRadioButton    = btRefreshR;
        demo.setConnectableButton  = btConnect;
        demo.setDiscoverableButton = btDiscover;
        demo.nameInput             = edName;
        demo.setNameButton         = btSetName;
        demo.turnOnButton          = btTurnOn;
        demo.turnOffButton         = btTurnOff;
        demo.dontTurnOffToggle     = tgDontOff;

        demo.radiosContent     = radiosContent;
        demo.radioRowPrefab    = radioRow;

        demo.discoverButton          = btDiscoverBtn;
        demo.timeoutInput            = edTimeout;
        demo.discoveringModeDropdown = ddDiscoverMode;
        demo.terminateButton         = btTerminate;
        demo.enumPairedButton        = btEnumPaired;
        demo.rssiButton              = btRssi;
        demo.isInRangeButton         = btIsInRange;
        demo.refreshDeviceButton     = btRefreshDev;
        demo.enumConnectedButton     = btEnumConn;

        demo.pairButton            = btPair;
        demo.pinInput              = edPIN;
        demo.unpairButton          = btUnpair;
        demo.mitmDropdown          = ddMitm;
        demo.ioCapDropdown         = ddIoCap;
        demo.bleProtectionDropdown = ddBleProt;
        demo.pairingMethodDropdown = ddPairingMethod;

        demo.devicesContent    = devicesContent;
        demo.deviceRowPrefab   = deviceRow;

        demo.enumServicesButton          = btEnumServices;
        demo.enumInstalledServicesButton = btEnumInst;
        demo.createVcomButton            = btCreateVcom;
        demo.destroyVcomButton           = btDestroyVcom;
        demo.enumVcomsButton             = btEnumVcoms;
        demo.enumPreinstalledToggle      = tgEnumPre;
        demo.installDeviceButton         = btInstallDev;
        demo.uninstallDeviceButton       = btUninstallDev;
        demo.disconnectButton            = btDisconnect;

        demo.servicesContent   = servicesContent;
        demo.serviceRowPrefab  = serviceRow;

        demo.comPortsContent   = vcomsContent;
        demo.comPortRowPrefab  = comPortRow;

        demo.eventsContent     = eventsContent;
        demo.eventRowPrefab    = eventRow;
        demo.clearEventsButton = clearEvents;

        demo.confirmPanel        = confirmPanel;
        demo.confirmText         = confirmText;
        demo.confirmYesButton    = confirmYes;
        demo.confirmNoButton     = confirmNo;
        demo.confirmCancelButton = confirmCancel;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("Bluetooth Manager Demo UI built. Press Play.");
    }

    // ================================================================
    //  Table
    // ================================================================
    private static (GameObject scroll, Transform content) MakeTable(
        string name, Transform parent,
        Vector2 anchoredPos, Vector2 topRight,
        float height, float[] columnWidths, string[] headers)
    {
        // header
        var header = CreateUI(name + "_Header", parent);
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.pivot = new Vector2(0.5f, 1);
        hr.anchoredPosition = new Vector2(0, anchoredPos.y + 26);
        hr.sizeDelta = new Vector2(-40, 24);

        var hHlg = header.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing = 4;
        hHlg.childForceExpandWidth = false; hHlg.childForceExpandHeight = true;
        hHlg.childControlWidth = true; hHlg.childControlHeight = true;

        for (int i = 0; i < headers.Length; i++)
        {
            var t = CreateText("H_" + headers[i], headers[i], header.transform);
            t.fontStyle = FontStyles.Bold;
            t.fontSize = 16;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = columnWidths[i]; le.minWidth = columnWidths[i];
        }

        // scroll
        var scroll = MakeScrollView(name, parent);
        var sr = scroll.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 1); sr.anchorMax = new Vector2(1, 1);
        sr.pivot = new Vector2(0.5f, 1);
        sr.anchoredPosition = anchoredPos;
        sr.sizeDelta = new Vector2(-40, height);

        var content = scroll.transform.Find("Viewport/Content");
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return (scroll, content);
    }

    private static GameObject MakeTableRowPrefab(string name, float[] widths)
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 28);

        var img = root.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);
        root.AddComponent<Button>();

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 28; le.preferredHeight = 28;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;

        var cells = new TMP_Text[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            var t = CreateText("Cell" + i, "", root.transform);
            t.fontSize = 15;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var l = t.gameObject.AddComponent<LayoutElement>();
            l.preferredWidth = widths[i]; l.minWidth = widths[i];
            cells[i] = t;
        }

        var ui = root.AddComponent<TableRowUI>();
        ui.cells = cells;
        ui.background = img;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabsFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ================================================================
    //  Confirm dialog
    // ================================================================
    private static (GameObject panel, TMP_Text text, Button yes, Button no, Button cancel)
        MakeConfirmDialog(Transform canvasParent)
    {
        var panel = CreateUI("ConfirmPanel", canvasParent);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700, 240);

        var img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var txtGO = CreateUI("Text", panel.transform);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 22;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = true;
        var trt = txt.rectTransform;
        trt.anchorMin = new Vector2(0, 0.4f); trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(20, 0); trt.offsetMax = new Vector2(-20, -10);

        var buttonBar = CreateUI("Buttons", panel.transform);
        var br = buttonBar.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0.4f);
        br.offsetMin = new Vector2(20, 20); br.offsetMax = new Vector2(-20, 0);
        var hlg = buttonBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false; hlg.childControlHeight = false;

        var yes    = MakeButton("Yes",    "YES",    buttonBar.transform, 150, 50);
        var no     = MakeButton("No",     "NO",     buttonBar.transform, 150, 50);
        var cancel = MakeButton("Cancel", "CANCEL", buttonBar.transform, 150, 50);

        panel.SetActive(false);
        return (panel, txt, yes, no, cancel);
    }

    // ================================================================
    //  UI helpers
    // ================================================================
    private static GameObject MakeHBar(string name, Transform parent, float y, float h)
    {
        var bar = CreateUI(name, parent);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -y);
        rt.sizeDelta = new Vector2(-40, h);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        return bar;
    }

    private static Button MakeButton(string name, string label, Transform parent, float w, float h)
    {
        var go = CreateUI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f);

        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        c.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = c;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var tGO = CreateUI("Text", go.transform);
        var t = tGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.color = Color.white; t.fontSize = 18;
        t.alignment = TextAlignmentOptions.Center;
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return btn;
    }

    private static Toggle MakeToggle(string name, string label, Transform parent, float w, float h)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var toggle = go.AddComponent<Toggle>();

        var bg = CreateUI("Background", go.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f); bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(4, 0);
        bgRect.sizeDelta = new Vector2(20, 20);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        var cm = CreateUI("Checkmark", bg.transform);
        var cmRect = cm.GetComponent<RectTransform>();
        cmRect.anchorMin = new Vector2(0.5f, 0.5f); cmRect.anchorMax = new Vector2(0.5f, 0.5f);
        cmRect.pivot = new Vector2(0.5f, 0.5f); cmRect.anchoredPosition = Vector2.zero;
        cmRect.sizeDelta = new Vector2(12, 12);
        var cmImg = cm.AddComponent<Image>();
        cmImg.color = new Color(0.4f, 0.7f, 1f);

        var lbl = CreateText("Label", label, go.transform);
        lbl.fontSize = 18;
        var lRect = lbl.rectTransform;
        lRect.anchorMin = new Vector2(0, 0); lRect.anchorMax = new Vector2(1, 1);
        lRect.offsetMin = new Vector2(30, 0); lRect.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic = cmImg;
        return toggle;
    }

    private static TMP_Text MakeLabel(string text, Transform parent, float w, float h)
    {
        var t = CreateText(text, text, parent);
        t.fontSize = 18;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;
        return t;
    }

    private static TextMeshProUGUI CreateText(string name, string content, Transform parent)
    {
        var go = CreateUI(name, parent);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.color = Color.white;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private static TMP_InputField MakeInputField(string name, Transform parent, float w, float h, string defaultText)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f);

        var ta = CreateUI("Text Area", go.transform);
        var taRect = ta.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(6, 4); taRect.offsetMax = new Vector2(-6, -4);

        var txt = ta.AddComponent<TextMeshProUGUI>();
        txt.color = Color.white; txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        var ph = CreateUI("Placeholder", ta.transform);
        var phRect = ph.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
        var phT = ph.AddComponent<TextMeshProUGUI>();
        phT.text = defaultText; phT.color = new Color(1, 1, 1, 0.4f); phT.fontSize = 18;
        phT.alignment = TextAlignmentOptions.MidlineLeft;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = txt;
        input.placeholder = phT;
        input.targetGraphic = bg;
        input.text = defaultText;
        return input;
    }

    private static TMP_Dropdown MakeDropdown(string name, Transform parent, float w, float h, string[] opts)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.22f, 0.22f);

        var dd = go.AddComponent<TMP_Dropdown>();

        var labelGO = CreateUI("Label", go.transform);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = opts.Length > 0 ? opts[0] : "";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white; label.fontSize = 16;
        var lRect = label.rectTransform;
        lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
        lRect.offsetMin = new Vector2(10, 0); lRect.offsetMax = new Vector2(-25, 0);

        var arrow = CreateUI("Arrow", go.transform);
        var arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = new Color(0.7f, 0.7f, 0.7f);
        var aRect = arrow.GetComponent<RectTransform>();
        aRect.anchorMin = new Vector2(1, 0.5f); aRect.anchorMax = new Vector2(1, 0.5f);
        aRect.pivot = new Vector2(1, 0.5f);
        aRect.anchoredPosition = new Vector2(-8, 0);
        aRect.sizeDelta = new Vector2(12, 12);

        var template = CreateUI("Template", go.transform);
        var tRect = template.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0, 0); tRect.anchorMax = new Vector2(1, 0);
        tRect.pivot = new Vector2(0.5f, 1);
        tRect.anchoredPosition = new Vector2(0, 2);
        tRect.sizeDelta = new Vector2(0, 300);

        var tImg = template.AddComponent<Image>();
        tImg.color = new Color(0.15f, 0.15f, 0.15f);
        var tScroll = template.AddComponent<ScrollRect>();
        tScroll.horizontal = false;
        var tMask = template.AddComponent<Mask>();
        tMask.showMaskGraphic = true;

        var viewport = CreateUI("Viewport", template.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 28);

        var item = CreateUI("Item", content.transform);
        var iRect = item.GetComponent<RectTransform>();
        iRect.anchorMin = new Vector2(0, 0.5f); iRect.anchorMax = new Vector2(1, 0.5f);
        iRect.pivot = new Vector2(0.5f, 0.5f);
        iRect.anchoredPosition = Vector2.zero;
        iRect.sizeDelta = new Vector2(0, 28);

        var itemToggle = item.AddComponent<Toggle>();

        var itemBg = CreateUI("Item Background", item.transform);
        var ibRect = itemBg.GetComponent<RectTransform>();
        ibRect.anchorMin = Vector2.zero; ibRect.anchorMax = Vector2.one;
        ibRect.offsetMin = Vector2.zero; ibRect.offsetMax = Vector2.zero;
        var ibImg = itemBg.AddComponent<Image>();
        ibImg.color = new Color(0.2f, 0.2f, 0.2f);

        var itemCheck = CreateUI("Item Checkmark", item.transform);
        var icRect = itemCheck.GetComponent<RectTransform>();
        icRect.anchorMin = new Vector2(0, 0.5f); icRect.anchorMax = new Vector2(0, 0.5f);
        icRect.pivot = new Vector2(0, 0.5f);
        icRect.anchoredPosition = new Vector2(10, 0);
        icRect.sizeDelta = new Vector2(14, 14);
        var icImg = itemCheck.AddComponent<Image>();
        icImg.color = new Color(0.4f, 0.7f, 1f);

        var itemLabel = CreateUI("Item Label", item.transform);
        var ilRect = itemLabel.GetComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero; ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(28, 0); ilRect.offsetMax = new Vector2(-10, 0);
        var ilText = itemLabel.AddComponent<TextMeshProUGUI>();
        ilText.text = "Option"; ilText.color = Color.white; ilText.fontSize = 16;
        ilText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = ibImg;
        itemToggle.graphic = icImg;

        tScroll.viewport = vpRect;
        tScroll.content = cRect;

        dd.template = tRect;
        dd.captionText = label;
        dd.itemText = ilText;

        dd.options.Clear();
        foreach (var o in opts) dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();

        template.SetActive(false);
        return dd;
    }

    private static GameObject MakeScrollView(string name, Transform parent)
    {
        var root = CreateUI(name, parent);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        var scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = Vector2.zero;

        scroll.viewport = vpRect;
        scroll.content = cRect;
        return root;
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }
}
#endif