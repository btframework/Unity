#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GattClientSceneBuilder
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

    private static readonly string[] ProtectionOptions = new[]
    {
        "None", "Authentication", "Encryption", "Encryption And Authentication"
    };

    private static readonly string[] OpFlagOptions = new[]
    {
        "None", "Force Read From Device", "Force Read From Cache"
    };

    private static readonly string[] ParamsOptions = new[]
    {
        "Balanced", "PowerOptimized", "ThroughputOptimized", "Custom"
    };

    private static readonly string[] SubscribeOptions = new[]
    {
        "Notification", "Indication", "Manual"
    };

    private static readonly string[] WriteOptions = new[]
    {
        "With Response", "Without Response", "Auto Split", "Auto"
    };

    [MenuItem("Tools/GATT Client/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("GattClientUI");
        DestroyIfExists("GattClientDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // ---------- Canvas ----------
        var canvasGO = CreateUI("GattClientUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Row 1: Discover | Terminate | Pair | Unpair | EnumPaired | MITM | IO Cap
        // ============================================================
        var r1 = MakeHBar("Row1", canvasGO.transform, 20, 40);
        var btDiscover     = MakeButton("BtDiscover",     "Discover",    r1.transform, 140, 40);
        var btTerminate    = MakeButton("BtTerminate",    "Terminate",   r1.transform, 140, 40);
        var btPair         = MakeButton("BtPair",         "Pair",        r1.transform, 120, 40);
        var btUnpair       = MakeButton("BtUnpair",       "Unpair",      r1.transform, 120, 40);
        var btEnumPaired   = MakeButton("BtEnumPaired",   "Enum paired", r1.transform, 160, 40);
        MakeLabel("MITM", r1.transform, 80, 40);
        var ddMitm         = MakeDropdown("DdMitm", r1.transform, 320, 40, MitmOptions);
        MakeLabel("IO Cap", r1.transform, 100, 40);
        var ddIoCap        = MakeDropdown("DdIoCap", r1.transform, 220, 40, IoCapOptions);

        // ============================================================
        //  Row 2: Protection | OpFlag
        // ============================================================
        var r2 = MakeHBar("Row2", canvasGO.transform, 70, 40);
        MakeLabel("Protection", r2.transform, 130, 40);
        var ddProtection = MakeDropdown("DdProtection", r2.transform, 320, 40, ProtectionOptions);
        MakeLabel("Operation flag", r2.transform, 160, 40);
        var ddOpFlag = MakeDropdown("DdOpFlag", r2.transform, 320, 40, OpFlagOptions);

        // ============================================================
        //  Devices table (Address | Name | Device type | Paired)
        // ============================================================
        var devWidths = new[] { 300f, 300f, 200f, 120f };
        var devHeaders = new[] { "Address", "Name", "Device type", "Paired" };
        var (devScroll, devContent) = MakeTable("DevicesScroll", canvasGO.transform,
            new Vector2(20, -120), 170, devWidths, devHeaders);

        // ============================================================
        //  Row 3: Connect | Disconnect | ConnectOnRead | ForceIndications | GetServices | GetIncludedServices | GetParams | ParamsDd | SetParams
        // ============================================================
        var r3 = MakeHBar("Row3", canvasGO.transform, 320, 40);
        var btConnect      = MakeButton("BtConnect", "Connect", r3.transform, 130, 40);
        var btDisconnect   = MakeButton("BtDisconnect", "Disconnect", r3.transform, 140, 40);
        var tgConnectOnRead= MakeToggle("TgConnectOnRead", "Connect on read", r3.transform, 220, 40);
        var tgForceInd     = MakeToggle("TgForceIndications", "Force indications", r3.transform, 240, 40);
        var btGetServices  = MakeButton("BtGetServices", "Get services", r3.transform, 160, 40);
        var btGetInc       = MakeButton("BtGetIncludedServices", "Get included", r3.transform, 180, 40);
        var btGetParams    = MakeButton("BtGetParams", "Get params", r3.transform, 150, 40);
        var ddParams       = MakeDropdown("DdParams", r3.transform, 220, 40, ParamsOptions);
        var btSetParams    = MakeButton("BtSetParams", "Set params", r3.transform, 150, 40);

        // ============================================================
        //  Services table (UUID | Is short UUID | Handle)
        // ============================================================
        var svWidths = new[] { 600f, 200f, 200f };
        var svHeaders = new[] { "UUID", "Is short UUID", "Handle" };
        var (svScroll, svContent) = MakeTable("ServicesScroll", canvasGO.transform,
            new Vector2(20, -370), 140, svWidths, svHeaders);

        // ============================================================
        //  Row 4: GetCharacteristics | GetCharValue | EdCharValue | SetValue | WriteKind
        // ============================================================
        var r4 = MakeHBar("Row4", canvasGO.transform, 540, 40);
        var btGetChars     = MakeButton("BtGetCharacteristics", "Get characteristics", r4.transform, 200, 40);
        var btGetCharVal   = MakeButton("BtGetCharValue", "Get value", r4.transform, 140, 40);
        MakeLabel("Char value (hex)", r4.transform, 200, 40);
        var edCharVal      = MakeInputField("EdCharValue", r4.transform, 220, 40, "00FFAA00");
        var btSetValue     = MakeButton("BtSetValue", "Set value", r4.transform, 140, 40);
        MakeLabel("Write kind", r4.transform, 130, 40);
        var ddWriteKind    = MakeDropdown("DdWriteKind", r4.transform, 220, 40, WriteOptions);

        // ============================================================
        //  Characteristics table (13 cols)
        // ============================================================
        var chWidths = new[] { 220f, 100f, 120f, 100f, 120f, 120f, 110f, 110f, 160f, 140f, 110f, 110f, 200f };
        var chHeaders = new[] { "UUID", "Is short UUID", "Service handle", "Handle", "Value handle",
                                 "Is broadcastable", "Is readable", "Is writable", "Is writable w/o resp",
                                 "Is signed writable", "Is notifiable", "Is indicatable", "Has extended props" };
        var (chScroll, chContent) = MakeTable("CharacteristicsScroll", canvasGO.transform,
            new Vector2(20, -610), 160, chWidths, chHeaders);

        // ============================================================
        //  Row 5: Subscribe panel
        // ============================================================
        var r5 = MakeHBar("Row5", canvasGO.transform, 800, 40);
        var btGetDesc      = MakeButton("BtGetDescriptors", "Get descriptors", r5.transform, 180, 40);
        var btGetDescVal   = MakeButton("BtGetDescValue", "Get desc value", r5.transform, 180, 40);
        var tgFastSub      = MakeToggle("TgFastSubscribe", "Fast subscribe", r5.transform, 220, 40);
        MakeLabel("Subscribe kind", r5.transform, 150, 40);
        var ddSubscribeKind= MakeDropdown("DdSubscribeKind", r5.transform, 200, 40, SubscribeOptions);
        var btSubscribe    = MakeButton("BtSubscribe", "Subscribe", r5.transform, 140, 40);
        var btUnsubscribe  = MakeButton("BtUnsubscribe", "Unsubscribe", r5.transform, 160, 40);
        var btWriteSub     = MakeButton("BtWriteSubscribe", "Write CCCD sub", r5.transform, 200, 40);
        var btWriteUnsub   = MakeButton("BtWriteUnsubscribe", "Write CCCD unsub", r5.transform, 220, 40);
        var btGetMaxPdu    = MakeButton("BtGetMaxPdu", "Max PDU size", r5.transform, 160, 40);
        var btGetPhy       = MakeButton("BtGetPhy", "Get PHY", r5.transform, 150, 40);

        // ============================================================
        //  Descriptors table (6 cols)
        // ============================================================
        var dcWidths = new[] { 400f, 130f, 150f, 200f, 120f, 400f };
        var dcHeaders = new[] { "UUID", "Is short UUID", "Service handle", "Characteristic handle", "Handle", "Type" };
        var (dcScroll, dcContent) = MakeTable("DescriptorsScroll", canvasGO.transform,
            new Vector2(20, -870), 130, dcWidths, dcHeaders);

        // ============================================================
        //  Descriptor value table (Property name | Property value)
        // ============================================================
        var dvWidths = new[] { 500f, 1200f };
        var dvHeaders = new[] { "Property name", "Property value" };
        var (dvScroll, dvContent) = MakeTable("DescriptorValueScroll", canvasGO.transform,
            new Vector2(20, -1030), 110, dvWidths, dvHeaders);

        // ============================================================
        //  Events table (Address | Event | Param | Value)
        // ============================================================
        var evWidths = new[] { 250f, 350f, 350f, 800f };
        var evHeaders = new[] { "Address", "Event", "Param", "Value" };
        var (evScroll, evContent) = MakeTable("EventsScroll", canvasGO.transform,
            new Vector2(20, -1190), 110, evWidths, evHeaders);

        var clearEvents = MakeButton("BtClearEvents", "Clear", canvasGO.transform, 120, 40);
        var ceRect = clearEvents.GetComponent<RectTransform>();
        ceRect.anchorMin = new Vector2(1, 0); ceRect.anchorMax = new Vector2(1, 0);
        ceRect.pivot = new Vector2(1, 0);
        ceRect.anchoredPosition = new Vector2(-20, 20);

        // ============================================================
        //  Prefabs
        // ============================================================
        var devRow = MakeTableRowPrefab("GattDeviceRow", devWidths);
        var svRow  = MakeTableRowPrefab("GattServiceRow", svWidths);
        var chRow  = MakeTableRowPrefab("GattCharacteristicRow", chWidths);
        var dcRow  = MakeTableRowPrefab("GattDescriptorRow", dcWidths);
        var dvRow  = MakeTableRowPrefab("GattDescriptorValueRow", dvWidths);
        var evRow  = MakeTableRowPrefab("GattEventRow", evWidths);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("GattClientDemo");
        var demo = demoGO.AddComponent<GattClientDemo>();

        demo.discoverButton     = btDiscover;
        demo.terminateButton    = btTerminate;
        demo.pairButton         = btPair;
        demo.unpairButton       = btUnpair;
        demo.enumPairedButton   = btEnumPaired;
        demo.mitmDropdown       = ddMitm;
        demo.ioCapDropdown      = ddIoCap;
        demo.protectionDropdown = ddProtection;
        demo.opFlagDropdown     = ddOpFlag;

        demo.devicesContent  = devContent;
        demo.deviceRowPrefab = devRow;

        demo.connectButton           = btConnect;
        demo.disconnectButton        = btDisconnect;
        demo.connectOnReadToggle     = tgConnectOnRead;
        demo.forceIndicationsToggle  = tgForceInd;
        demo.getServicesButton       = btGetServices;
        demo.getIncludedServicesButton = btGetInc;

        demo.getParamsButton = btGetParams;
        demo.paramsDropdown  = ddParams;
        demo.setParamsButton = btSetParams;

        demo.servicesContent  = svContent;
        demo.serviceRowPrefab = svRow;

        demo.characteristicsContent   = chContent;
        demo.characteristicRowPrefab  = chRow;
        demo.getCharacteristicsButton = btGetChars;
        demo.getCharValueButton       = btGetCharVal;
        demo.charValueInput           = edCharVal;
        demo.setValueButton           = btSetValue;
        demo.writeKindDropdown        = ddWriteKind;

        demo.descriptorsContent   = dcContent;
        demo.descriptorRowPrefab  = dcRow;
        demo.getDescriptorsButton = btGetDesc;
        demo.getDescValueButton   = btGetDescVal;

        demo.descriptorValueContent  = dvContent;
        demo.descriptorValueRowPrefab = dvRow;

        demo.fastSubscribeToggle      = tgFastSub;
        demo.subscribeKindDropdown    = ddSubscribeKind;
        demo.subscribeButton          = btSubscribe;
        demo.unsubscribeButton        = btUnsubscribe;
        demo.writeSubscribeButton     = btWriteSub;
        demo.writeUnsubscribeButton   = btWriteUnsub;

        demo.getMaxPduSizeButton = btGetMaxPdu;
        demo.getPhyButton        = btGetPhy;

        demo.eventsContent     = evContent;
        demo.eventRowPrefab    = evRow;
        demo.clearEventsButton = clearEvents;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? GATT Client Demo UI собрана. Нажми Play.");
    }

    // ================================================================
    //  Table
    // ================================================================
    private static (GameObject scroll, Transform content) MakeTable(
        string name, Transform parent, Vector2 anchoredPos, float height,
        float[] widths, string[] headers)
    {
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
            t.fontSize = 15;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = widths[i]; le.minWidth = widths[i];
        }

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
        rt.sizeDelta = new Vector2(0, 26);

        var img = root.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);
        root.AddComponent<Button>();

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 26; le.preferredHeight = 26;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;

        var cells = new TMP_Text[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            var t = CreateText("Cell" + i, "", root.transform);
            t.fontSize = 14;
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
        t.text = label; t.color = Color.white; t.fontSize = 16;
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
        lbl.fontSize = 16;
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
        t.fontSize = 16;
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
        text.fontSize = 16;
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
        txt.color = Color.white; txt.fontSize = 16;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        var ph = CreateUI("Placeholder", ta.transform);
        var phRect = ph.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
        var phT = ph.AddComponent<TextMeshProUGUI>();
        phT.text = defaultText; phT.color = new Color(1, 1, 1, 0.4f); phT.fontSize = 16;
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
        label.color = Color.white; label.fontSize = 14;
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
        ilText.text = "Option"; ilText.color = Color.white; ilText.fontSize = 14;
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
        scroll.horizontal = true;
        scroll.vertical = true;
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
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(0, 1);
        cRect.pivot = new Vector2(0, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(3000, 0);

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