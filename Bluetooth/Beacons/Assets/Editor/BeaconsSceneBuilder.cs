#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BeaconsSceneBuilder
{
    private const string PrefabsFolder        = "Assets/Prefabs";
    private const string RowPrefabPath        = PrefabsFolder + "/BeaconRow.prefab";
    private const string DetailRowPrefabPath  = PrefabsFolder + "/BeaconDetailRow.prefab";
    private const string DeviceNodePrefabPath = PrefabsFolder + "/BeaconDeviceNode.prefab";

    [MenuItem("Tools/Beacons/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("BeaconsUI");
        DestroyIfExists("BeaconsDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("BeaconsUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Watcher panel
        // ============================================================
        var watcherBar = MakeHBar("WatcherBar", canvasGO.transform, 20, 44);
        MakeBoldLabel("Beacon Watcher", watcherBar.transform, 220, 44);
        MakeLabel("Scan mode:", watcherBar.transform, 140, 44);
        var ddScanMode = MakeDropdown("DdScanMode", watcherBar.transform, 160, 44,
            new[] { "Active", "Passive" });
        MakeLabel("Scan param type:", watcherBar.transform, 200, 44);
        var ddScanParams = MakeDropdown("DdScanParams", watcherBar.transform, 280, 44,
            new[] { "Default", "Coexistence Optimized", "Low Latency", "Custom" });
        MakeLabel("Scan interval", watcherBar.transform, 140, 44);
        var edScanInterval = MakeInputField("EdScanInterval", watcherBar.transform, 120, 44);
        MakeLabel("Scan window", watcherBar.transform, 130, 44);
        var edScanWindow = MakeInputField("EdScanWindow", watcherBar.transform, 120, 44);
        var tgWatcherExt = MakeToggle("TgWatcherExt", "Extended", watcherBar.transform, 160, 44);

        var watcherButtons = MakeHBar("WatcherButtons", canvasGO.transform, 74, 40);
        var btWatcherStart = MakeButton("BtWatcherStart", "Start", watcherButtons.transform, 120, 40);
        var btWatcherStop  = MakeButton("BtWatcherStop",  "Stop",  watcherButtons.transform, 120, 40);

        // ============================================================
        //  Devices + Frame details
        // ============================================================
        var devicesScroll = MakeScrollView("DevicesScroll", canvasGO.transform);
        SetRect(devicesScroll, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -130), new Vector2(520, 420));
        var devicesContent = devicesScroll.transform.Find("Viewport/Content");
        var devVlg = devicesContent.gameObject.AddComponent<VerticalLayoutGroup>();
        devVlg.spacing = 2; devVlg.padding = new RectOffset(4, 4, 4, 4);
        devVlg.childForceExpandWidth = true; devVlg.childForceExpandHeight = false;
        devVlg.childControlWidth = true; devVlg.childControlHeight = true;
        var devCsf = devicesContent.gameObject.AddComponent<ContentSizeFitter>();
        devCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var detailsScroll = MakeScrollView("DetailsScroll", canvasGO.transform);
        SetRect(detailsScroll, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(560, -130), new Vector2(1340, 420));
        var detailsContent = detailsScroll.transform.Find("Viewport/Content");
        var detVlg = detailsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        detVlg.spacing = 2; detVlg.padding = new RectOffset(4, 4, 4, 4);
        detVlg.childForceExpandWidth = true; detVlg.childForceExpandHeight = false;
        detVlg.childControlWidth = true; detVlg.childControlHeight = true;
        var detCsf = detailsContent.gameObject.AddComponent<ContentSizeFitter>();
        detCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ============================================================
        //  Advertiser panel (набор checkbox'ов)
        // ============================================================
        var advBar = MakeHBar("AdvBar", canvasGO.transform, 570, 40);
        MakeBoldLabel("BLE Advertiser", advBar.transform, 180, 40);
        MakeLabel("Interval", advBar.transform, 90, 40);
        var edAdvInterval = MakeInputField("EdAdvInterval", advBar.transform, 120, 40);
        var btAdvDefault = MakeButton("BtAdvDefault", "Default", advBar.transform, 120, 40);
        var btAdvStart   = MakeButton("BtAdvStart",   "Start",   advBar.transform, 120, 40);
        var btAdvStop    = MakeButton("BtAdvStop",    "Stop",    advBar.transform, 120, 40);
        MakeLabel("Primary PHY", advBar.transform, 140, 40);
        var ddPrimaryPhy = MakeDropdown("DdPrimaryPhy", advBar.transform, 180, 40,
            new[] { "Unspecified", "Uncoded 1M", "Uncoded 2M", "Coded" });
        MakeLabel("Secondary PHY", advBar.transform, 160, 40);
        var ddSecondaryPhy = MakeDropdown("DdSecondaryPhy", advBar.transform, 180, 40,
            new[] { "Unspecified", "Uncoded 1M", "Uncoded 2M", "Coded" });

        // 4 grid-строки из toggles
        var advToggles = MakeVBar("AdvToggles", canvasGO.transform, 620, 4, 34);

        var row1 = MakeHBarRaw("AdvRow1", advToggles.transform, 34);
        var advBeacon       = MakeToggle("AdvBeacon",       "iBeacon",        row1.transform, 150, 34);
        var advEddUid       = MakeToggle("AdvEddUid",       "Eddystone UID",  row1.transform, 200, 34);
        var adv16Uuid       = MakeToggle("Adv16Uuid",       "16 UUID service",row1.transform, 220, 34);
        var adv16UuidData   = MakeToggle("Adv16UuidData",   "16 UUID data",   row1.transform, 200, 34);
        var adv128SolUuid   = MakeToggle("Adv128SolUuid",   "128 SOL UUID",   row1.transform, 200, 34);
        var advExtended     = MakeToggle("AdvExtended",     "Extended advertisement", row1.transform, 280, 34);

        var row2 = MakeHBarRaw("AdvRow2", advToggles.transform, 34);
        var advProximity    = MakeToggle("AdvProximity",    "Proximity Beacon", row2.transform, 220, 34);
        var advEddUrl       = MakeToggle("AdvEddUrl",       "Eddystone URL",  row2.transform, 200, 34);
        var adv32Uuid       = MakeToggle("Adv32Uuid",       "32 UUID service",row2.transform, 220, 34);
        var adv32UuidData   = MakeToggle("Adv32UuidData",   "32 UUID data",   row2.transform, 200, 34);
        var advCustom       = MakeToggle("AdvCustom",       "Custom RAW",     row2.transform, 200, 34);
        var advAnonymous    = MakeToggle("AdvAnonymous",    "Anonymous",      row2.transform, 200, 34);

        var row3 = MakeHBarRaw("AdvRow3", advToggles.transform, 34);
        var advAltBeacon    = MakeToggle("AdvAltBeacon",    "ALT Beacon",     row3.transform, 180, 34);
        var advManufacturer = MakeToggle("AdvManufacturer", "Manufacturer",   row3.transform, 200, 34);
        var adv128Uuid      = MakeToggle("Adv128Uuid",      "128 UUID service",row3.transform, 220, 34);
        var adv128UuidData  = MakeToggle("Adv128UuidData",  "128 UUID data",  row3.transform, 200, 34);
        var advTxRssi       = MakeToggle("AdvTxRssi",       "Include TX RSSI",row3.transform, 220, 34);

        // ============================================================
        //  Log
        // ============================================================
        var logBar = MakeHBar("LogBar", canvasGO.transform, 780, 40);
        MakeBoldLabel("Log", logBar.transform, 100, 40);
        var tgAutoScroll = MakeToggle("TgAutoScroll", "Autoscroll", logBar.transform, 180, 40);
        var btClearLog   = MakeButton("BtClearLog", "Clear", logBar.transform, 120, 40);

        var logScroll = MakeScrollView("LogScroll", canvasGO.transform);
        SetRect(logScroll, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(-40, 220));
        var logContent = logScroll.transform.Find("Viewport/Content");
        var logCsf = logContent.gameObject.AddComponent<ContentSizeFitter>();
        logCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var logText = CreateText("LogText", "", logContent);
        logText.enableWordWrapping = true;
        logText.margin = new Vector4(8, 8, 8, 8);
        var ltRect = logText.rectTransform;
        ltRect.anchorMin = new Vector2(0, 1); ltRect.anchorMax = new Vector2(1, 1);
        ltRect.pivot     = new Vector2(0.5f, 1);
        ltRect.anchoredPosition = Vector2.zero;
        ltRect.sizeDelta = Vector2.zero;
        var ltCsf = logText.gameObject.AddComponent<ContentSizeFitter>();
        ltCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ============================================================
        //  Prefabs
        // ============================================================
        var rowPrefab       = CreateBeaconRowPrefab();
        var detailPrefab    = CreateDetailRowPrefab();
        var deviceNodePrefab= CreateDeviceNodePrefab();

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("BeaconsDemo");
        var demo = demoGO.AddComponent<BeaconsDemo>();

        demo.scanModeDropdown         = ddScanMode;
        demo.scanParamsDropdown       = ddScanParams;
        demo.scanIntervalInput        = edScanInterval;
        demo.scanWindowInput          = edScanWindow;
        demo.enableExtendedToggle     = tgWatcherExt;
        demo.watcherStartButton       = btWatcherStart;
        demo.watcherStopButton        = btWatcherStop;

        demo.devicesContent           = devicesContent;
        demo.deviceNodePrefab         = deviceNodePrefab;
        demo.rowPrefab                = rowPrefab;

        demo.detailsContent           = detailsContent;
        demo.detailRowPrefab          = detailPrefab;

        demo.advertiserIntervalInput  = edAdvInterval;
        demo.advertiserDefaultButton  = btAdvDefault;
        demo.advertiserStartButton    = btAdvStart;
        demo.advertiserStopButton     = btAdvStop;

        demo.advBeaconToggle          = advBeacon;
        demo.advProximityToggle       = advProximity;
        demo.advAltBeaconToggle       = advAltBeacon;
        demo.advEddystoneUidToggle    = advEddUid;
        demo.advEddystoneUrlToggle    = advEddUrl;
        demo.advManufacturerToggle    = advManufacturer;
        demo.adv16UuidToggle          = adv16Uuid;
        demo.adv32UuidToggle          = adv32Uuid;
        demo.adv128UuidToggle         = adv128Uuid;
        demo.adv16UuidDataToggle      = adv16UuidData;
        demo.adv32UuidDataToggle      = adv32UuidData;
        demo.adv128UuidDataToggle     = adv128UuidData;
        demo.adv128SolUuidToggle      = adv128SolUuid;
        demo.advCustomToggle          = advCustom;

        demo.advExtendedToggle        = advExtended;
        demo.advAnonymousToggle       = advAnonymous;
        demo.advTxRssiToggle          = advTxRssi;

        demo.primaryPhyDropdown       = ddPrimaryPhy;
        demo.secondaryPhyDropdown     = ddSecondaryPhy;

        demo.logText                  = logText;
        demo.logScrollRect            = logScroll.GetComponent<ScrollRect>();
        demo.clearLogButton           = btClearLog;
        demo.autoScrollToggle         = tgAutoScroll;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? Beacons Demo UI собрана. Нажми Play.");
    }

    // ================================================================
    //  Prefabs
    // ================================================================
    private static GameObject CreateBeaconRowPrefab()
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI("BeaconRow", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.03f);
        root.AddComponent<Button>();

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 30; le.preferredHeight = 30;

        // Общий контейнер с отступом
        var indent = CreateUI("Indent", root.transform);
        var iRect = indent.GetComponent<RectTransform>();
        iRect.anchorMin = new Vector2(0, 0);
        iRect.anchorMax = new Vector2(1, 1);
        iRect.offsetMin = Vector2.zero;
        iRect.offsetMax = Vector2.zero;

        var hlg = indent.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var devText = MakeLayoutText("DeviceText", indent.transform, 220, 30);
        var frameText = MakeLayoutText("FrameTypeText", indent.transform, 220, 30);

        var ui = root.AddComponent<BeaconRowUI>();
        ui.indentRect = iRect;
        ui.deviceText = devText;
        ui.frameTypeText = frameText;
        ui.background = bg;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    private static GameObject CreateDetailRowPrefab()
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI("BeaconDetailRow", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 26);

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 26; le.preferredHeight = 26;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var fText = MakeLayoutText("FieldText", root.transform, 240, 26);
        var vText = MakeLayoutText("ValueText", root.transform, 900, 26);

        var ui = root.AddComponent<BeaconDetailRowUI>();
        ui.fieldText = fText;
        ui.valueText = vText;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, DetailRowPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    private static GameObject CreateDeviceNodePrefab()
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI("BeaconDeviceNode", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 0);

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = root.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, DeviceNodePrefabPath);
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

    private static GameObject MakeHBarRaw(string name, Transform parent, float h)
    {
        var bar = CreateUI(name, parent);
        var rt = bar.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, h);

        var le = bar.AddComponent<LayoutElement>();
        le.preferredHeight = h; le.minHeight = h;

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        return bar;
    }

    private static GameObject MakeVBar(string name, Transform parent, float y, int _, float rowHeight)
    {
        var bar = CreateUI(name, parent);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -y);
        rt.sizeDelta = new Vector2(-40, rowHeight * 3);

        var vlg = bar.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;

        var csf = bar.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return bar;
    }

    private static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    private static TMP_Text CreateText(string name, string content, Transform parent)
    {
        var go = CreateUI(name, parent);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.color = Color.white;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private static TMP_Text MakeLayoutText(string name, Transform parent, float width, float height)
    {
        var t = CreateText(name, "", parent);
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width; le.minWidth = width;
        le.preferredHeight = height; le.minHeight = height;
        return t;
    }

    private static TMP_Text MakeLabel(string text, Transform parent, float w, float h)
    {
        var t = CreateText(text, text, parent);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = w; le.minWidth  = w;
        le.preferredHeight = h; le.minHeight = h;
        return t;
    }

    private static TMP_Text MakeBoldLabel(string text, Transform parent, float w, float h)
    {
        var t = MakeLabel(text, parent, w, h);
        t.fontStyle = FontStyles.Bold;
        return t;
    }

    private static Button MakeButton(string name, string label, Transform parent, float w, float h)
    {
        var go = CreateUI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var txt = CreateUI("Text", go.transform);
        var t = txt.AddComponent<TextMeshProUGUI>();
        t.text = label; t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white; t.fontSize = 18;
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

    private static TMP_InputField MakeInputField(string name, Transform parent, float w, float h)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f);

        var textArea = CreateUI("Text Area", go.transform);
        var taRect = textArea.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(6, 4); taRect.offsetMax = new Vector2(-6, -4);

        var txt = textArea.AddComponent<TextMeshProUGUI>();
        txt.color = Color.white;
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        var placeholder = CreateUI("Placeholder", textArea.transform);
        var phRect = placeholder.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
        var phText = placeholder.AddComponent<TextMeshProUGUI>();
        phText.text = "0"; phText.color = new Color(1, 1, 1, 0.4f);
        phText.fontSize = 18;

        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = txt;
        inputField.placeholder = phText;
        inputField.targetGraphic = bg;
        return inputField;
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
        label.color = Color.white; label.fontSize = 18;
        var lRect = label.rectTransform;
        lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
        lRect.offsetMin = new Vector2(10, 0); lRect.offsetMax = new Vector2(-25, 0);

        var arrow = CreateUI("Arrow", go.transform);
        var arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = new Color(0.7f, 0.7f, 0.7f);
        var arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f); arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-8, 0);
        arrowRect.sizeDelta = new Vector2(12, 12);

        var template = CreateUI("Template", go.transform);
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0); templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 200);

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
        ilText.text = "Option"; ilText.color = Color.white; ilText.fontSize = 18;
        ilText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = ibImg;
        itemToggle.graphic = icImg;

        tScroll.viewport = vpRect;
        tScroll.content = cRect;

        dd.template = templateRect;
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