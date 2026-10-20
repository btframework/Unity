#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class AudioSwitcherSceneBuilder
{
    private const float FormWidth  = 666f;
    private const float FormHeight = 452f;
    private const string ScenePath = "Assets/Scenes/AudioSwitcher.unity";

    [MenuItem("Tools/AudioSwitcher/Build Scene")]
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

        // Form background
        CreatePanel(canvasGO.transform, "FormBackground", 0, 0, FormWidth, FormHeight,
            new Color(0.94f, 0.94f, 0.94f));

        // Labels
        CreateLabel(canvasGO.transform, "laRole", "Role", 93, 18, 40, 13);
        CreateLabel(canvasGO.transform, "laFlow", "Flow", 419, 18, 40, 13);

        // Comboboxes (TMP_Dropdown)
        var cbRole = CreateDropdown(canvasGO.transform, "cbRole",
            new[] { "Console", "Multimedia", "Communications" }, 128, 15, 121, 21, 0);
        var cbFlow = CreateDropdown(canvasGO.transform, "cbFlow",
            new[] { "Capture", "Render" }, 452, 14, 121, 21, 0);

        // Buttons
        var btEnum       = CreateButton(canvasGO.transform, "btEnum",       "Enum",        12,  12, 75, 23);
        var btSetDefault = CreateButton(canvasGO.transform, "btSetDefault", "Set default", 255, 13, 75, 23);
        var btRefresh    = CreateButton(canvasGO.transform, "btRefresh",    "Refresh",     336, 13, 75, 23);
        var btConnect    = CreateButton(canvasGO.transform, "btConnect",    "Connect",     422, 42, 75, 23);
        var btDisconnect = CreateButton(canvasGO.transform, "btDisconnect", "Disconnect",  503, 41, 75, 23);
        var btGetDefault = CreateButton(canvasGO.transform, "btGetDefault", "GetDefault",  579, 12, 75, 23);
        var btClear      = CreateButton(canvasGO.transform, "btClear",      "Clear",       573, 417, 75, 23);

        // Checkboxes
        var cbActive     = CreateCheckBox(canvasGO.transform, "cbActive",     "Active",      12,  47, 56, 17, true);
        var cbDisabled   = CreateCheckBox(canvasGO.transform, "cbDisabled",   "Disabled",    74,  47, 67, 17, false);
        var cbUnplugged  = CreateCheckBox(canvasGO.transform, "cbUnplugged",  "Unplugged",   147, 47, 78, 17, false);
        var cbNotPresent = CreateCheckBox(canvasGO.transform, "cbNotPresent", "Not present", 231, 47, 81, 17, false);
        var cbUseMac     = CreateCheckBox(canvasGO.transform, "cbUseMac",     "Use MAC",     584, 43, 71, 17, false);

        // ListView
        var lvDevices = CreateListView(canvasGO.transform, "lvDevices", 12, 70, 642, 175);

        // ListBox (log)
        var lbLog = CreateListBox(canvasGO.transform, "lbLog", 12, 251, 642, 160);

        // Runtime controller
        var ctrlGO = new GameObject("AudioSwitcherController");
        var ctrl = ctrlGO.AddComponent<AudioSwitcherController>();
        ctrl.btEnum         = btEnum.GetComponent<Button>();
        ctrl.btSetDefault   = btSetDefault.GetComponent<Button>();
        ctrl.btRefresh      = btRefresh.GetComponent<Button>();
        ctrl.btConnect      = btConnect.GetComponent<Button>();
        ctrl.btDisconnect   = btDisconnect.GetComponent<Button>();
        ctrl.btGetDefault   = btGetDefault.GetComponent<Button>();
        ctrl.btClear        = btClear.GetComponent<Button>();
        ctrl.cbActive       = cbActive;
        ctrl.cbDisabled     = cbDisabled;
        ctrl.cbUnplugged    = cbUnplugged;
        ctrl.cbNotPresent   = cbNotPresent;
        ctrl.cbUseMac       = cbUseMac;
        ctrl.cbRole         = cbRole;
        ctrl.cbFlow         = cbFlow;
        ctrl.lvDevicesScroll  = lvDevices.GetComponent<ScrollRect>();
        ctrl.lvDevicesContent = ctrl.lvDevicesScroll.content;
        ctrl.lbLogText        = lbLog.GetComponent<ScrollRect>().content.GetComponent<TextMeshProUGUI>();

        // Save scene to disk
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[AudioSwitcher] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[AudioSwitcher] Scene created and saved: " + ScenePath);
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

    private static GameObject CreateLabel(Transform parent, string name, string text, float x, float y, float w, float h)
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
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, float x, float y, float w, float h)
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
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;

        return go;
    }

    private static Toggle CreateCheckBox(Transform parent, string name, string label, float x, float y, float w, float h, bool isOn)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        // Box
        var boxGO = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(Outline));
        boxGO.transform.SetParent(go.transform, false);
        var boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0, 0.5f);
        boxRT.anchorMax = new Vector2(0, 0.5f);
        boxRT.pivot     = new Vector2(0, 0.5f);
        boxRT.anchoredPosition = new Vector2(0, 0);
        boxRT.sizeDelta = new Vector2(13, 13);
        var boxImg = boxGO.GetComponent<Image>();
        boxImg.color = Color.white;
        var outline = boxGO.GetComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.5f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        // Checkmark
        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(boxGO.transform, false);
        var checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = new Vector2(2, 2);
        checkRT.offsetMax = new Vector2(-2, -2);
        var checkImg = checkGO.GetComponent<Image>();
        checkImg.color = Color.black;

        // Label
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
        lblTmp.raycastTarget = false;

        var t = go.GetComponent<Toggle>();
        t.targetGraphic = boxImg;
        t.graphic = checkImg;
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

        // Label
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

        // Arrow
        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.pivot     = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-4, 0);
        arrowRT.sizeDelta = new Vector2(10, 10);
        arrowGO.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);

        // Template
        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Canvas), typeof(GraphicRaycaster));
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

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        // Content
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

        // Item
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

        // Item Background
        var itemBgGO = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBgGO.transform.SetParent(itemGO.transform, false);
        var itemBgRT = itemBgGO.GetComponent<RectTransform>();
        itemBgRT.anchorMin = Vector2.zero;
        itemBgRT.anchorMax = Vector2.one;
        itemBgRT.offsetMin = Vector2.zero;
        itemBgRT.offsetMax = Vector2.zero;
        itemBgGO.GetComponent<Image>().color = Color.white;

        // Item Label
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

        // Wire dropdown
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
    //  ListView (8 columns, horizontal + vertical scroll)
    // ==========================================================

    private static GameObject CreateListView(Transform parent, string name, float x, float y, float w, float h)
    {
        const float HeaderH   = 20f;
        const float ScrollH   = 15f;
        const float ColumnW   = 100f;
        const int   ColumnCnt = 8;
        const float ContentW  = ColumnW * ColumnCnt;

        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(root.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1 + ScrollH);
        viewportRT.offsetMax = new Vector2(-1 - ScrollH, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot     = new Vector2(0, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(ContentW, 0);
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 1;
        vlg.padding = new RectOffset(1, 1, 1, 1);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Header row (part of content so it scrolls horizontally with data)
        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(contentGO.transform, false);
        headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        var headerHlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        headerHlg.childControlWidth = true;
        headerHlg.childControlHeight = true;
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = true;
        headerHlg.spacing = 0;
        headerHlg.padding = new RectOffset(0, 0, 0, 0);
        var headerLe = headerGO.GetComponent<LayoutElement>();
        headerLe.preferredHeight = HeaderH;
        headerLe.minHeight = HeaderH;

        string[] headers = { "ID", "Friendly name", "Description", "Flow", "Roles", "States", "MAC", "Service" };
        for (int i = 0; i < headers.Length; i++)
            CreateColumnCell(headerGO.transform, "ch_" + i, headers[i], ColumnW, true);

        // Horizontal scrollbar
        var hSbGO = CreateScrollbar(root.transform, "Scrollbar Horizontal", true, ScrollH);

        // Vertical scrollbar
        var vSbGO = CreateScrollbar(root.transform, "Scrollbar Vertical", false, ScrollH);

        // Wire ScrollRect
        var sr = root.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.viewport = viewportRT;
        sr.horizontal = true;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.horizontalScrollbar = hSbGO.GetComponent<Scrollbar>();
        sr.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        sr.verticalScrollbar = vSbGO.GetComponent<Scrollbar>();
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return root;
    }

    public static void CreateColumnCell(Transform parent, string name, string text, float width, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
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

    private static GameObject CreateScrollbar(Transform parent, string name, bool horizontal, float thickness)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        if (horizontal)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(0, thickness);
        }
        else
        {
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-1, thickness * 0.5f);
            rt.sizeDelta = new Vector2(thickness, -thickness);
        }
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
        sb.direction = horizontal ? Scrollbar.Direction.LeftToRight : Scrollbar.Direction.BottomToTop;

        return go;
    }

    // ==========================================================
    //  ListBox (log)
    // ==========================================================

    private static GameObject CreateListBox(Transform parent, string name, float x, float y, float w, float h)
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

        return root;
    }
}
#endif