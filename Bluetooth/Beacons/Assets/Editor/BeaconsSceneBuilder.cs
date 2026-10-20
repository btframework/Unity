#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class BeaconsSceneBuilder
{
    private const float FormWidth  = 796f;
    private const float FormHeight = 789f;
    private const string ScenePath = "Assets/Scenes/Beacons.unity";

    // Builder outputs (filled in Build, consumed by controller wiring)
    public class Refs
    {
        public TMP_Dropdown cbWatcherScanMode, cbScanParametersType, cbPrimaryPhy, cbSecondaryPhy;
        public TMP_InputField edWatcherScanInterval, edWatcherScanWindow, edAdvertiserInterval;
        public Toggle cbWatcherEnableExtended;
        public Toggle cbAdvertiserBeacon, cbAdvertiserProximityBeacon, cbAdvertiserAltBeacon;
        public Toggle cbAdvertiserEddystoneUid, cbAdvertiserEddystoneUrl, cbAdvertiserManufacturer;
        public Toggle cbAdvertiser16Uuid, cbAdvertiser32Uuid, cbAdvertiser128Uuid;
        public Toggle cbAdvertiser16UuidData, cbAdvertiser32UuidData, cbAdvertiser128UuidData;
        public Toggle cbAdvertiser128SolUuid, cbAdvertiserCustom, cbAdvertiserExtended;
        public Toggle cbAdvertiserAnonymous, cbAdvertiserTxRssi;
        public Toggle cbAutoScroll;
        public Button btWatcherStart, btWatcherStop;
        public Button btAdvertiserDefault, btAdvertiserStart, btAdvertiserStop;
        public Button btClearLog;
        public RectTransform lvDevicesContent;
        public RectTransform lvFrameDetailsContent;
        public ScrollRect lvDevicesScroll, lvFrameDetailsScroll, lbLogScroll;
        public TextMeshProUGUI lbLogText;
    }

    [MenuItem("Tools/Beacons/Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.94f, 0.94f, 0.94f);
        camGO.transform.position = new Vector3(0, 0, -10);

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(FormWidth, FormHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        CreatePanel(canvasGO.transform, "FormBackground", 0, 0, FormWidth, FormHeight,
            new Color(0.94f, 0.94f, 0.94f));

        var refs = new Refs();

        // =================== Section: Beacon Watcher ===================
        CreateLabel(canvasGO.transform, "laBeaconWatcher", "Beacon Watcher", 12, 9, 130, 16, true);
        CreateLabel(canvasGO.transform, "laWatcherScanMode", "Scan mode:", 15, 28, 64, 14, false);

        refs.cbWatcherScanMode = CreateDropdown(canvasGO.transform, "cbWatcherScanMode",
            new[] { "Active", "Passive" }, 85, 25, 73, 21, 0);

        CreateLabel(canvasGO.transform, "laScanParametersType",
            "Scan paramaters type:", 164, 28, 116, 14, false);

        refs.cbScanParametersType = CreateDropdown(canvasGO.transform, "cbScanParametersType",
            new[] { "Default", "Coexistence Optimized", "Low Latency", "Custom" },
            283, 25, 121, 21, 0);

        CreateLabel(canvasGO.transform, "laWatcherScanInterval", "Scan interval", 412, 28, 69, 14, false);
        refs.edWatcherScanInterval = CreateNumericInputField(canvasGO.transform,
            "edWatcherScanInterval", 487, 25, 65, 20);

        CreateLabel(canvasGO.transform, "laWatcherScanWindow", "Scan window", 558, 28, 71, 14, false);
        refs.edWatcherScanWindow = CreateNumericInputField(canvasGO.transform,
            "edWatcherScanWindow", 635, 25, 65, 20);

        refs.cbWatcherEnableExtended = CreateCheckBox(canvasGO.transform,
            "cbWatcherEnableExtended", "Extended", 706, 27, 71, 17, false);

        refs.btWatcherStart = CreateButton(canvasGO.transform, "btWatcherStart", "Start", 18, 52, 75, 23).GetComponent<Button>();
        refs.btWatcherStop  = CreateButton(canvasGO.transform, "btWatcherStop",  "Stop",  99, 52, 75, 23).GetComponent<Button>();
        refs.btWatcherStop.interactable = false;

        // lvDevices (2 columns, scrollable)
        refs.lvDevicesScroll = CreateTwoColumnListView(canvasGO.transform, "lvDevices",
            18, 81, 225, 292, 100, "Device", 100, "Frame type", out refs.lvDevicesContent);

        // lvFrameDetails (2 columns, scrollable)
        refs.lvFrameDetailsScroll = CreateTwoColumnListView(canvasGO.transform, "lvFrameDetails",
            249, 81, 528, 292, 150, "Field", 350, "Value", out refs.lvFrameDetailsContent);

        // =================== Section: BLE Advertiser ===================
        CreateLabel(canvasGO.transform, "laLeAdvertiser", "BLE Advertiser", 12, 382, 120, 16, true);
        CreateLabel(canvasGO.transform, "laAdvertiserInterval", "Interval", 15, 401, 42, 14, false);
        refs.edAdvertiserInterval = CreateNumericInputField(canvasGO.transform,
            "edAdvertiserInterval", 63, 398, 65, 20);

        refs.btAdvertiserDefault = CreateButton(canvasGO.transform, "btAdvertiserDefault", "Default", 134, 396, 75, 23).GetComponent<Button>();
        refs.btAdvertiserStart   = CreateButton(canvasGO.transform, "btAdvertiserStart",   "Start",   229, 396, 75, 23).GetComponent<Button>();
        refs.btAdvertiserStop    = CreateButton(canvasGO.transform, "btAdvertiserStop",    "Stop",    310, 396, 75, 23).GetComponent<Button>();
        refs.btAdvertiserStop.interactable = false;

        CreateLabel(canvasGO.transform, "laPrimaryPhy",   "Primary PHY",   391, 401, 66, 14, false);
        refs.cbPrimaryPhy = CreateDropdown(canvasGO.transform, "cbPrimaryPhy",
            new[] { "Unspecified", "Uncoded 1M", "Uncoded 2M", "Coded" },
            467, 398, 106, 21, 0);

        CreateLabel(canvasGO.transform, "laSecondaryPhy", "Secondary PHY", 579, 401, 83, 14, false);
        refs.cbSecondaryPhy = CreateDropdown(canvasGO.transform, "cbSecondaryPhy",
            new[] { "Unspecified", "Uncoded 1M", "Uncoded 2M", "Coded" },
            668, 398, 105, 21, 0);

        refs.cbAdvertiserBeacon           = CreateCheckBox(canvasGO.transform, "cbAdvertiserBeacon",           "iBeacon",                  34, 424,  65, 17, false);
        refs.cbAdvertiserProximityBeacon  = CreateCheckBox(canvasGO.transform, "cbAdvertiserProximityBeacon",  "Proximity Beacon",         34, 447, 107, 17, false);
        refs.cbAdvertiserAltBeacon        = CreateCheckBox(canvasGO.transform, "cbAdvertiserAltBeacon",        "ALT Beacon",               34, 470,  86, 17, false);
        refs.cbAdvertiserEddystoneUid     = CreateCheckBox(canvasGO.transform, "cbAdvertiserEddystoneUid",     "Eddystone UID",           147, 425,  98, 17, false);
        refs.cbAdvertiserEddystoneUrl     = CreateCheckBox(canvasGO.transform, "cbAdvertiserEddystoneUrl",     "Eddystone URL",           147, 447, 101, 17, false);
        refs.cbAdvertiserManufacturer     = CreateCheckBox(canvasGO.transform, "cbAdvertiserManufacturer",     "Manufacturer",            147, 470,  89, 17, false);
        refs.cbAdvertiser16Uuid           = CreateCheckBox(canvasGO.transform, "cbAdvertiser16Uuid",           "16 UUID service",         254, 425, 105, 17, false);
        refs.cbAdvertiser32Uuid           = CreateCheckBox(canvasGO.transform, "cbAdvertiser32Uuid",           "32 UUID service",         254, 447, 105, 17, false);
        refs.cbAdvertiser128Uuid          = CreateCheckBox(canvasGO.transform, "cbAdvertiser128Uuid",          "128 UUID service",        254, 470, 111, 17, false);
        refs.cbAdvertiser16UuidData       = CreateCheckBox(canvasGO.transform, "cbAdvertiser16UuidData",       "16 UUID data",            371, 425,  92, 17, false);
        refs.cbAdvertiser32UuidData       = CreateCheckBox(canvasGO.transform, "cbAdvertiser32UuidData",       "32 UUID data",            371, 447,  92, 17, false);
        refs.cbAdvertiser128UuidData      = CreateCheckBox(canvasGO.transform, "cbAdvertiser128UuidData",      "128 UUID data",           371, 470,  98, 17, false);
        refs.cbAdvertiser128SolUuid       = CreateCheckBox(canvasGO.transform, "cbAdvertiser128SolUuid",       "128 SOL UUID",            475, 425,  98, 17, false);
        refs.cbAdvertiserCustom           = CreateCheckBox(canvasGO.transform, "cbAdvertiserCustom",           "Custom RAW",              475, 447,  90, 17, false);
        refs.cbAdvertiserExtended         = CreateCheckBox(canvasGO.transform, "cbAdvertiserExtended",         "Extended advertisement",  621, 425, 140, 17, false);
        refs.cbAdvertiserAnonymous        = CreateCheckBox(canvasGO.transform, "cbAdvertiserAnonymous",        "Anonymous",               646, 447,  81, 17, false);
        refs.cbAdvertiserTxRssi           = CreateCheckBox(canvasGO.transform, "cbAdvertiserTxRssi",           "Include TX RSSI",         646, 470, 106, 17, false);

        refs.cbAdvertiserAnonymous.interactable = false;
        refs.cbAdvertiserTxRssi.interactable    = false;

        // =================== Section: Log ===================
        CreateLabel(canvasGO.transform, "laLog", "Log", 12, 514, 28, 16, true);

        refs.btClearLog = CreateButton(canvasGO.transform, "btClearLog", "Clear", 702, 538, 75, 23).GetComponent<Button>();

        refs.cbAutoScroll = CreateCheckBox(canvasGO.transform, "cbAutoScroll", "Autoscroll", 18, 542, 72, 17, false);

        refs.lbLogScroll = CreateListBox(canvasGO.transform, "lbLog", 18, 571, 759, 212);
        refs.lbLogText = refs.lbLogScroll.content.GetComponent<TextMeshProUGUI>();

        // =================== Runtime controller ===================
        var ctrlGO = new GameObject("BeaconsController");
        var ctrl = ctrlGO.AddComponent<BeaconsController>();

        ctrl.btWatcherStart  = refs.btWatcherStart;
        ctrl.btWatcherStop   = refs.btWatcherStop;
        ctrl.btAdvertiserDefault = refs.btAdvertiserDefault;
        ctrl.btAdvertiserStart   = refs.btAdvertiserStart;
        ctrl.btAdvertiserStop    = refs.btAdvertiserStop;
        ctrl.btClearLog          = refs.btClearLog;

        ctrl.cbWatcherScanMode         = refs.cbWatcherScanMode;
        ctrl.cbScanParametersType      = refs.cbScanParametersType;
        ctrl.cbPrimaryPhy              = refs.cbPrimaryPhy;
        ctrl.cbSecondaryPhy            = refs.cbSecondaryPhy;
        ctrl.edWatcherScanInterval     = refs.edWatcherScanInterval;
        ctrl.edWatcherScanWindow       = refs.edWatcherScanWindow;
        ctrl.edAdvertiserInterval      = refs.edAdvertiserInterval;
        ctrl.cbWatcherEnableExtended   = refs.cbWatcherEnableExtended;

        ctrl.cbAdvertiserBeacon          = refs.cbAdvertiserBeacon;
        ctrl.cbAdvertiserProximityBeacon = refs.cbAdvertiserProximityBeacon;
        ctrl.cbAdvertiserAltBeacon       = refs.cbAdvertiserAltBeacon;
        ctrl.cbAdvertiserEddystoneUid    = refs.cbAdvertiserEddystoneUid;
        ctrl.cbAdvertiserEddystoneUrl    = refs.cbAdvertiserEddystoneUrl;
        ctrl.cbAdvertiserManufacturer    = refs.cbAdvertiserManufacturer;
        ctrl.cbAdvertiser16Uuid          = refs.cbAdvertiser16Uuid;
        ctrl.cbAdvertiser32Uuid          = refs.cbAdvertiser32Uuid;
        ctrl.cbAdvertiser128Uuid         = refs.cbAdvertiser128Uuid;
        ctrl.cbAdvertiser16UuidData      = refs.cbAdvertiser16UuidData;
        ctrl.cbAdvertiser32UuidData      = refs.cbAdvertiser32UuidData;
        ctrl.cbAdvertiser128UuidData     = refs.cbAdvertiser128UuidData;
        ctrl.cbAdvertiser128SolUuid      = refs.cbAdvertiser128SolUuid;
        ctrl.cbAdvertiserCustom          = refs.cbAdvertiserCustom;
        ctrl.cbAdvertiserExtended        = refs.cbAdvertiserExtended;
        ctrl.cbAdvertiserAnonymous       = refs.cbAdvertiserAnonymous;
        ctrl.cbAdvertiserTxRssi          = refs.cbAdvertiserTxRssi;
        ctrl.cbAutoScroll                = refs.cbAutoScroll;

        ctrl.lvDevicesContent      = refs.lvDevicesContent;
        ctrl.lvFrameDetailsContent = refs.lvFrameDetailsContent;
        ctrl.lvDevicesScroll       = refs.lvDevicesScroll;
        ctrl.lvFrameDetailsScroll  = refs.lvFrameDetailsScroll;
        ctrl.lbLogScroll           = refs.lbLogScroll;
        ctrl.lbLogText             = refs.lbLogText;

        // Save scene
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[Beacons] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[Beacons] Scene created and saved: " + ScenePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == scenePath) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ==========================================================
    //  Primitives
    // ==========================================================

    private static GameObject CreatePanel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateLabel(Transform parent, string name, string text,
        float x, float y, float w, float h, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = bold ? 12 : 11;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(0.87f, 0.87f, 0.87f);

        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(0.93f, 0.93f, 0.93f);
        c.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        c.disabledColor    = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        btn.colors = c;

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;

        return go;
    }

    private static Toggle CreateCheckBox(Transform parent, string name, string label,
        float x, float y, float w, float h, bool isOn)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var boxGO = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(Outline));
        boxGO.transform.SetParent(go.transform, false);
        var boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0, 0.5f);
        boxRT.anchorMax = new Vector2(0, 0.5f);
        boxRT.pivot     = new Vector2(0, 0.5f);
        boxRT.anchoredPosition = Vector2.zero;
        boxRT.sizeDelta = new Vector2(13, 13);
        var boxImg = boxGO.GetComponent<Image>();
        boxImg.color = Color.white;
        var outline = boxGO.GetComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.5f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(boxGO.transform, false);
        var checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = new Vector2(2, 2);
        checkRT.offsetMax = new Vector2(-2, -2);
        checkGO.GetComponent<Image>().color = Color.black;

        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(go.transform, false);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0);
        lblRT.anchorMax = new Vector2(1, 1);
        lblRT.offsetMin = new Vector2(18, 0);
        lblRT.offsetMax = Vector2.zero;
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 11;
        lblTmp.alignment = TextAlignmentOptions.Left;
        lblTmp.color = Color.black;
        lblTmp.enableWordWrapping = false;
        lblTmp.overflowMode = TextOverflowModes.Ellipsis;
        lblTmp.raycastTarget = false;

        var t = go.GetComponent<Toggle>();
        t.targetGraphic = boxImg;
        t.graphic = checkGO.GetComponent<Image>();
        t.isOn = isOn;

        return t;
    }

    // ==========================================================
    //  TMP_Dropdown
    // ==========================================================

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, string[] options,
        float x, float y, float w, float h, int selectedIndex)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = Color.white;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(4, 0);
        labelRT.offsetMax = new Vector2(-18, 0);
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.fontSize = 11;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.black;
        labelTmp.raycastTarget = false;

        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.pivot     = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-4, 0);
        arrowRT.sizeDelta = new Vector2(10, 10);
        arrowGO.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);

        var templateGO = new GameObject("Template",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Canvas), typeof(GraphicRaycaster));
        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.SetParent(go.transform, false);
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot     = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, 150);
        templateGO.GetComponent<Image>().color = Color.white;
        var templateCanvas = templateGO.GetComponent<Canvas>();
        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = 30000;
        templateGO.SetActive(false);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 1);
        itemRT.anchorMax = new Vector2(1, 1);
        itemRT.pivot     = new Vector2(0.5f, 1);
        itemRT.anchoredPosition = Vector2.zero;
        itemRT.sizeDelta = new Vector2(0, 20);
        var itemImg = itemGO.GetComponent<Image>();
        itemImg.color = Color.white;
        var itemToggle = itemGO.GetComponent<Toggle>();
        itemToggle.targetGraphic = itemImg;
        var itemLe = itemGO.AddComponent<LayoutElement>();
        itemLe.preferredHeight = 20;

        var itemBgGO = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBgGO.transform.SetParent(itemGO.transform, false);
        var itemBgRT = itemBgGO.GetComponent<RectTransform>();
        itemBgRT.anchorMin = Vector2.zero;
        itemBgRT.anchorMax = Vector2.one;
        itemBgRT.offsetMin = Vector2.zero;
        itemBgRT.offsetMax = Vector2.zero;
        itemBgGO.GetComponent<Image>().color = Color.white;

        var itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(6, 0);
        itemLabelRT.offsetMax = new Vector2(-6, 0);
        var itemLabelTmp = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelTmp.fontSize = 11;
        itemLabelTmp.alignment = TextAlignmentOptions.Left;
        itemLabelTmp.color = Color.black;

        var sr = templateGO.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.viewport = viewportRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = go.GetComponent<Image>();
        dropdown.template = templateRT;
        dropdown.captionText = labelTmp;
        dropdown.itemText = itemLabelTmp;
        dropdown.itemImage = itemBgGO.GetComponent<Image>();

        dropdown.options.Clear();
        foreach (var opt in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
        dropdown.value = selectedIndex;
        dropdown.RefreshShownValue();

        return dropdown;
    }

    // ==========================================================
    //  Numeric-only input field
    // ==========================================================

    private static TMP_InputField CreateNumericInputField(Transform parent, string name,
        float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var bg = go.GetComponent<Image>();
        bg.color = Color.white;

        var textAreaGO = new GameObject("Text Area", typeof(RectTransform));
        textAreaGO.transform.SetParent(go.transform, false);
        var taRT = textAreaGO.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(4, 2);
        taRT.offsetMax = new Vector2(-4, -2);

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(textAreaGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.fontSize = 11;
        textTMP.color = Color.black;
        textTMP.alignment = TextAlignmentOptions.Left;
        textTMP.enableWordWrapping = false;

        var inputField = go.GetComponent<TMP_InputField>();
        inputField.targetGraphic = bg;
        inputField.textViewport = taRT;
        inputField.textComponent = textTMP;
        inputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            if (addedChar >= '0' && addedChar <= '9') return addedChar;
            return '\0';
        };

        return inputField;
    }

    // ==========================================================
    //  Two-column ListView (header + scrollable content)
    // ==========================================================

    private static ScrollRect CreateTwoColumnListView(Transform parent, string name,
        float x, float y, float w, float h,
        float col0W, string col0Header, float col1W, string col1Header,
        out RectTransform contentRT)
    {
        const float HeaderH  = 20f;
        const float ScrollW  = 15f;

        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        // Header
        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup));
        var headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.SetParent(root.transform, false);
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot     = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, HeaderH);
        headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        var hlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        CreateColumnText(headerGO.transform, col0Header, col0W, true);
        CreateColumnText(headerGO.transform, col1Header, col1W, true);

        // ScrollView
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.SetParent(root.transform, false);
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(1, 1);
        scrollRT.offsetMax = new Vector2(-1 - ScrollW, -HeaderH - 1);
        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 1;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = contentRT;
        sr.viewport = viewportRT;

        // Vertical scrollbar
        var sbGO = CreateVerticalScrollbar(root.transform, "Scrollbar Vertical", ScrollW);
        sr.verticalScrollbar = sbGO.GetComponent<Scrollbar>();
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return sr;
    }

    private static void CreateColumnText(Transform parent, string text, float width, bool bold)
    {
        var go = new GameObject("HeaderCell", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 11;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.margin = new Vector4(4, 0, 2, 0);
        tmp.raycastTarget = false;
    }

    private static GameObject CreateVerticalScrollbar(Transform parent, string name, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-1, 0);
        rt.sizeDelta = new Vector2(thickness, -2);
        go.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);

        var slideGO = new GameObject("Sliding Area", typeof(RectTransform));
        slideGO.transform.SetParent(go.transform, false);
        var slideRT = slideGO.GetComponent<RectTransform>();
        slideRT.anchorMin = Vector2.zero;
        slideRT.anchorMax = Vector2.one;
        slideRT.offsetMin = Vector2.zero;
        slideRT.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(slideGO.transform, false);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;
        handleGO.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);

        var sb = go.GetComponent<Scrollbar>();
        sb.handleRect = handleRT;
        sb.targetGraphic = handleGO.GetComponent<Image>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        return go;
    }

    // ==========================================================
    //  ListBox (log)
    // ==========================================================

    private static ScrollRect CreateListBox(Transform parent, string name, float x, float y, float w, float h)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        var sr = root.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(root.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1);
        viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        var tmp = contentGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 11;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.margin = new Vector4(2, 2, 2, 2);

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = contentRT;
        sr.viewport = viewportRT;

        return sr;
    }
}
#endif