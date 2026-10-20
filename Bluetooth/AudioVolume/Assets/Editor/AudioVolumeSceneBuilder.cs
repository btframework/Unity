#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class AudioVolumeSceneBuilder
{
    private const float FormWidth  = 460f;
    private const float FormHeight = 426f;
    private const string ScenePath = "Assets/Scenes/AudioVolume.unity";

    [MenuItem("Tools/AudioVolume/Build Scene")]
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

        // laEndpoints
        CreateLabel(canvasGO.transform, "laEndpoints", "Endpoints", 9, 15, 60, 16);

        // cbEndpoints
        var cbEndpoints = CreateDropdown(canvasGO.transform, "cbEndpoints",
            new string[0], 72, 12, 376, 21, 0);

        // Buttons
        var btOpen  = CreateButton(canvasGO.transform, "btOpen",  "Open",  12, 39, 75, 23);
        var btClose = CreateButton(canvasGO.transform, "btClose", "Close", 93, 39, 75, 23);
        var btClear = CreateButton(canvasGO.transform, "btClear", "Clear", 373, 391, 75, 23);

        // pbMaster (vertical progress bar) - at (12, 68) size 15x97, hidden initially
        var pbMasterFill = CreateVerticalProgressBar(canvasGO.transform, "pbMaster", 12, 68, 15, 97);

        // tbMaster (vertical slider) - at (403, 68) size 45x97, hidden initially
        var tbMaster = CreateVerticalSlider(canvasGO.transform, "tbMaster", 403, 68, 45, 97);

        // cbMute - at (403, 43) size 50x17, hidden initially
        var cbMute = CreateCheckBox(canvasGO.transform, "cbMute", "Mute", 403, 43, 50, 17, false);

        // lbLog
        var lbLog = CreateListBox(canvasGO.transform, "lbLog", 12, 171, 436, 212);

        // Dynamic bars container - starts at pbMaster.Right + 5 = 12 + 15 + 5 = 32, same top as pbMaster
        var barsContainerGO = new GameObject("DynamicBars", typeof(RectTransform));
        var barsContainerRT = barsContainerGO.GetComponent<RectTransform>();
        barsContainerRT.SetParent(canvasGO.transform, false);
        barsContainerRT.anchorMin = new Vector2(0, 1);
        barsContainerRT.anchorMax = new Vector2(0, 1);
        barsContainerRT.pivot     = new Vector2(0, 1);
        barsContainerRT.anchoredPosition = new Vector2(32, -68);
        barsContainerRT.sizeDelta = new Vector2(420, 97);

        // Hide the initially-hidden controls
        pbMasterFill.transform.parent.gameObject.SetActive(false);
        tbMaster.gameObject.SetActive(false);
        cbMute.gameObject.SetActive(false);

        // Runtime controller
        var ctrlGO = new GameObject("AudioVolumeController");
        var ctrl = ctrlGO.AddComponent<AudioVolumeController>();
        ctrl.btOpen           = btOpen.GetComponent<Button>();
        ctrl.btClose          = btClose.GetComponent<Button>();
        ctrl.btClear          = btClear.GetComponent<Button>();
        ctrl.cbEndpoints      = cbEndpoints;
        ctrl.cbMute           = cbMute;
        ctrl.tbMaster         = tbMaster.GetComponent<Slider>();
        ctrl.pbMasterRoot     = pbMasterFill.transform.parent.gameObject;
        ctrl.pbMasterFill     = pbMasterFill;
        ctrl.lbLogText        = lbLog.GetComponent<ScrollRect>().content.GetComponent<TextMeshProUGUI>();
        ctrl.barsParent       = barsContainerRT;

        // Save scene to disk
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[AudioVolume] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[AudioVolume] Scene created and saved: " + ScenePath);
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
    //  Vertical progress bar (returns the Fill Image)
    // ==========================================================

    private static Image CreateVerticalProgressBar(Transform parent, string name,
        float x, float y, float w, float h)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var bg = root.GetComponent<Image>();
        bg.color = new Color(0.95f, 0.95f, 0.95f);
        var outline = root.GetComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.5f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(root.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1, 1);
        fillRT.offsetMax = new Vector2(-1, -1);
        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.75f, 0.2f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = 0; // Bottom
        fillImg.fillAmount = 0f;

        return fillImg;
    }

    // ==========================================================
    //  Vertical slider (WinForms TrackBar equivalent)
    // ==========================================================

    private static GameObject CreateVerticalSlider(Transform parent, string name,
        float x, float y, float w, float h)
    {
        var sliderGO = new GameObject(name, typeof(RectTransform), typeof(Slider));
        var rt = sliderGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        // Background
        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);

        // Fill Area
        var faGO = new GameObject("Fill Area", typeof(RectTransform));
        faGO.transform.SetParent(sliderGO.transform, false);
        var faRT = faGO.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(18, 4);
        faRT.offsetMax = new Vector2(-18, -4);

        // Fill
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(faGO.transform, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = 0; // Bottom

        // Handle Slide Area
        var hsaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
        hsaGO.transform.SetParent(sliderGO.transform, false);
        var hsaRT = hsaGO.GetComponent<RectTransform>();
        hsaRT.anchorMin = Vector2.zero;
        hsaRT.anchorMax = Vector2.one;
        hsaRT.offsetMin = new Vector2(4, 4);
        hsaRT.offsetMax = new Vector2(-4, -4);

        // Handle
        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(hsaGO.transform, false);
        var handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0.5f, 0.5f);
        handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.anchoredPosition = Vector2.zero;
        handleRT.sizeDelta = new Vector2(25, 10);
        handleGO.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f);

        var slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleGO.GetComponent<Image>();
        slider.direction = Slider.Direction.BottomToTop;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.value = 0f;

        return sliderGO;
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