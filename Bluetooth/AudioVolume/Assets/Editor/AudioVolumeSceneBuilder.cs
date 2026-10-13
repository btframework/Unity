#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class AudioVolumeSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string PrefabPath    = PrefabsFolder + "/ChannelBar.prefab";

    [MenuItem("Tools/Audio Volume/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("AudioVolumeUI");
        DestroyIfExists("AudioVolumeDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // ============================================================
        //  Canvas
        // ============================================================
        var canvasGO = CreateUI("AudioVolumeUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Верхняя панель: Endpoints + dropdown + Open/Close
        // ============================================================
        var top = CreateUI("TopBar", canvasGO.transform);
        var topRect = top.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot     = new Vector2(0.5f, 1);
        topRect.anchoredPosition = new Vector2(0, -20);
        topRect.sizeDelta = new Vector2(-40, 44);

        var topHlg = top.AddComponent<HorizontalLayoutGroup>();
        topHlg.spacing = 12;
        topHlg.childAlignment = TextAnchor.MiddleLeft;
        topHlg.childForceExpandWidth  = false;
        topHlg.childForceExpandHeight = true;
        topHlg.childControlWidth  = true;
        topHlg.childControlHeight = true;

        TMP_Text lblEndpoints = CreateLabel("LblEndpoints", "Endpoints", top.transform, 130, 44);
        TMP_Dropdown ddEndpoints = CreateDropdown("DdEndpoints", top.transform,
            700, 44, new string[0]);
        Button btnOpen  = CreateButton("BtnOpen",  "Open",  top.transform, 120, 44);
        Button btnClose = CreateButton("BtnClose", "Close", top.transform, 120, 44);

        // ============================================================
        //  Master bar (слева)
        // ============================================================
        var masterBarGO = CreateUI("MasterBar", canvasGO.transform);
        var masterBarRect = masterBarGO.GetComponent<RectTransform>();
        masterBarRect.anchorMin = new Vector2(0, 1);
        masterBarRect.anchorMax = new Vector2(0, 1);
        masterBarRect.pivot     = new Vector2(0, 1);
        masterBarRect.anchoredPosition = new Vector2(30, -100);
        masterBarRect.sizeDelta = new Vector2(30, 300);

        var masterBarBg = masterBarGO.AddComponent<Image>();
        masterBarBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var masterBarFillGO = CreateUI("Fill", masterBarGO.transform);
        StretchFull(masterBarFillGO);
        var masterBarFill = masterBarFillGO.AddComponent<Image>();
        SetupVerticalFill(masterBarFill, new Color(0.3f, 0.8f, 0.4f));

        var masterBarUI = masterBarGO.AddComponent<VerticalBarUI>();
        masterBarUI.fill = masterBarFill;

        // ============================================================
        //  Channels container (по центру)
        // ============================================================
        var channelsContainer = CreateUI("ChannelsContainer", canvasGO.transform);
        var chRect = channelsContainer.GetComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0, 1);
        chRect.anchorMax = new Vector2(0, 1);
        chRect.pivot     = new Vector2(0, 1);
        chRect.anchoredPosition = new Vector2(80, -100);
        chRect.sizeDelta = new Vector2(1400, 300);

        var chHlg = channelsContainer.AddComponent<HorizontalLayoutGroup>();
        chHlg.spacing = 6;
        chHlg.childAlignment = TextAnchor.LowerLeft;
        chHlg.childForceExpandWidth  = false;
        chHlg.childForceExpandHeight = true;
        chHlg.childControlWidth  = true;
        chHlg.childControlHeight = true;

        // ============================================================
        //  Mute + вертикальный слайдер (справа)
        // ============================================================
        var muteGO = CreateUI("MuteToggle", canvasGO.transform);
        var muteRect = muteGO.GetComponent<RectTransform>();
        muteRect.anchorMin = new Vector2(1, 1);
        muteRect.anchorMax = new Vector2(1, 1);
        muteRect.pivot     = new Vector2(1, 1);
        muteRect.anchoredPosition = new Vector2(-30, -100);
        muteRect.sizeDelta = new Vector2(160, 40);

        var mute = BuildToggle(muteGO, "Mute");

        var sliderGO = CreateUI("MasterSlider", canvasGO.transform);
        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(1, 1);
        sliderRect.anchorMax = new Vector2(1, 1);
        sliderRect.pivot     = new Vector2(1, 1);
        sliderRect.anchoredPosition = new Vector2(-60, -150);
        sliderRect.sizeDelta = new Vector2(60, 250);

        var masterSlider = BuildVerticalSlider(sliderGO);

        // ============================================================
        //  Log
        // ============================================================
        var logScroll = CreateScrollView("LogScroll", canvasGO.transform);
        var logRect = logScroll.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.offsetMin = new Vector2(30, 90);
        logRect.offsetMax = new Vector2(-30, -470);

        var logContent = logScroll.transform.Find("Viewport/Content");
        var logCsf = logContent.gameObject.AddComponent<ContentSizeFitter>();
        logCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var logText = CreateText("LogText", "", logContent);
        logText.enableWordWrapping = true;
        logText.margin = new Vector4(8, 8, 8, 8);
        var ltRect = logText.rectTransform;
        ltRect.anchorMin = new Vector2(0, 1);
        ltRect.anchorMax = new Vector2(1, 1);
        ltRect.pivot     = new Vector2(0.5f, 1);
        ltRect.anchoredPosition = Vector2.zero;
        ltRect.sizeDelta = Vector2.zero;
        var ltCsf = logText.gameObject.AddComponent<ContentSizeFitter>();
        ltCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ============================================================
        //  Clear
        // ============================================================
        var clearBtn = CreateButton("BtnClear", "Clear", canvasGO.transform, 120, 40);
        var clearRect = clearBtn.GetComponent<RectTransform>();
        clearRect.anchorMin = new Vector2(1, 0);
        clearRect.anchorMax = new Vector2(1, 0);
        clearRect.pivot     = new Vector2(1, 0);
        clearRect.anchoredPosition = new Vector2(-30, 30);

        // ============================================================
        //  Префаб канала
        // ============================================================
        var channelBarPrefab = CreateChannelBarPrefab();

        // ============================================================
        //  Main GameObject + связи
        // ============================================================
        var demoGO = new GameObject("AudioVolumeDemo");
        var demo = demoGO.AddComponent<AudioVolumeDemo>();

        demo.endpointsDropdown  = ddEndpoints;
        demo.openButton         = btnOpen;
        demo.closeButton        = btnClose;

        demo.masterBar          = masterBarUI;
        demo.masterSlider       = masterSlider;
        demo.muteToggle         = mute;

        demo.channelsContainer  = channelsContainer.transform;
        demo.channelBarPrefab   = channelBarPrefab;

        demo.logText            = logText;
        demo.logScrollRect      = logScroll.GetComponent<ScrollRect>();
        demo.clearButton        = clearBtn;

        // Скрытые по умолчанию элементы — как в Form.Designer
        masterBarGO.SetActive(false);
        sliderGO   .SetActive(false);
        muteGO     .SetActive(false);

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? Audio Volume Demo UI собрана. Нажми Play.");
    }

    // ================================================================
    //  Channel bar prefab
    // ================================================================
    private static GameObject CreateChannelBarPrefab()
    {
        if (!Directory.Exists(PrefabsFolder))
            Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI("ChannelBar", null);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(30, 300);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth = 30;
        le.minWidth       = 30;

        var fillGO = CreateUI("Fill", root.transform);
        StretchFull(fillGO);

        var fill = fillGO.AddComponent<Image>();
        SetupVerticalFill(fill, new Color(0.3f, 0.7f, 1f));

        var ui = root.AddComponent<VerticalBarUI>();
        ui.fill = fill;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ================================================================
    //  UI builders
    // ================================================================
    private static Slider BuildVerticalSlider(GameObject root)
    {
        var slider = root.AddComponent<Slider>();
        slider.direction      = Slider.Direction.BottomToTop;
        slider.wholeNumbers   = true;
        slider.minValue       = 0;
        slider.maxValue       = 100;
        slider.value          = 0;

        // Background
        var bgGO = CreateUI("Background", root.transform);
        StretchFull(bgGO);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Fill Area
        var faGO = CreateUI("Fill Area", root.transform);
        var faRect = faGO.GetComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.offsetMin = new Vector2(15, 15);
        faRect.offsetMax = new Vector2(-15, -15);

        var fillGO = CreateUI("Fill", faGO.transform);
        StretchFull(fillGO);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 1f);

        // Handle Slide Area
        var haGO = CreateUI("Handle Slide Area", root.transform);
        var haRect = haGO.GetComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10, 10);
        haRect.offsetMax = new Vector2(-10, -10);

        var handleGO = CreateUI("Handle", haGO.transform);
        var hRect = handleGO.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0, 0);
        hRect.anchorMax = new Vector2(1, 0);
        hRect.pivot     = new Vector2(0.5f, 0.5f);
        hRect.sizeDelta = new Vector2(0, 20);
        var hImg = handleGO.AddComponent<Image>();
        hImg.color = new Color(0.9f, 0.9f, 0.9f);

        slider.fillRect      = fillGO.GetComponent<RectTransform>();
        slider.handleRect    = hRect;
        slider.targetGraphic = hImg;

        return slider;
    }

    private static Toggle BuildToggle(GameObject root, string label)
    {
        var toggle = root.AddComponent<Toggle>();

        var bgGO = CreateUI("Background", root.transform);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot     = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(4, 0);
        bgRect.sizeDelta = new Vector2(24, 24);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        var cmGO = CreateUI("Checkmark", bgGO.transform);
        var cmRect = cmGO.GetComponent<RectTransform>();
        cmRect.anchorMin = new Vector2(0.5f, 0.5f);
        cmRect.anchorMax = new Vector2(0.5f, 0.5f);
        cmRect.pivot     = new Vector2(0.5f, 0.5f);
        cmRect.anchoredPosition = Vector2.zero;
        cmRect.sizeDelta = new Vector2(14, 14);
        var cmImg = cmGO.AddComponent<Image>();
        cmImg.color = new Color(0.4f, 0.7f, 1f);

        var lblGO = CreateUI("Label", root.transform);
        var lblRect = lblGO.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(1, 1);
        lblRect.offsetMin = new Vector2(34, 0);
        lblRect.offsetMax = Vector2.zero;
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.color = Color.white;
        lbl.fontSize = 20;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        toggle.targetGraphic = bgImg;
        toggle.graphic       = cmImg;
        toggle.isOn = false;

        return toggle;
    }

    private static Button CreateButton(string name, string label, Transform parent,
        float width, float height)
    {
        var go = CreateUI(name, parent);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.minWidth        = width;
        le.preferredHeight = height;
        le.minHeight       = height;

        var textGO = CreateUI("Text", go.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 20;

        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    private static TMP_Text CreateLabel(string name, string text, Transform parent,
        float width, float height)
    {
        var t = CreateText(name, text, parent);
        t.fontSize = 20;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.minWidth        = width;
        le.preferredHeight = height;
        le.minHeight       = height;
        return t;
    }

    private static TextMeshProUGUI CreateText(string name, string content, Transform parent)
    {
        var go = CreateUI(name, parent);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.color = Color.white;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent,
        float width, float height, string[] options)
    {
        var go = CreateUI(name, parent);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.minWidth        = width;
        le.preferredHeight = height;
        le.minHeight       = height;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var dd = go.AddComponent<TMP_Dropdown>();

        // Caption
        var labelGO = CreateUI("Label", go.transform);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = options.Length > 0 ? options[0] : "";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.fontSize = 18;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = new Vector2(-25, 0);

        // Arrow
        var arrowGO = CreateUI("Arrow", go.transform);
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        var arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot     = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-8, 0);
        arrowRect.sizeDelta = new Vector2(12, 12);

        // Template
        var template = CreateUI("Template", go.transform);
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot     = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 200);

        var templateImg = template.AddComponent<Image>();
        templateImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var templateScroll = template.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;
        var templateMask = template.AddComponent<Mask>();
        templateMask.showMaskGraphic = true;

        var viewport = CreateUI("Viewport", template.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot     = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 28);

        var item = CreateUI("Item", content.transform);
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.pivot     = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0, 28);

        var itemToggle = item.AddComponent<Toggle>();

        var itemBg = CreateUI("Item Background", item.transform);
        StretchFull(itemBg);
        var ibImg = itemBg.AddComponent<Image>();
        ibImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var itemCheck = CreateUI("Item Checkmark", item.transform);
        var icRect = itemCheck.GetComponent<RectTransform>();
        icRect.anchorMin = new Vector2(0, 0.5f);
        icRect.anchorMax = new Vector2(0, 0.5f);
        icRect.pivot     = new Vector2(0, 0.5f);
        icRect.anchoredPosition = new Vector2(10, 0);
        icRect.sizeDelta = new Vector2(14, 14);
        var icImg = itemCheck.AddComponent<Image>();
        icImg.color = new Color(0.4f, 0.7f, 1f);

        var itemLabel = CreateUI("Item Label", item.transform);
        var ilRect = itemLabel.GetComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero;
        ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(28, 0);
        ilRect.offsetMax = new Vector2(-10, 0);
        var ilText = itemLabel.AddComponent<TextMeshProUGUI>();
        ilText.text = "Option";
        ilText.color = Color.white;
        ilText.fontSize = 18;
        ilText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = ibImg;
        itemToggle.graphic       = icImg;

        templateScroll.viewport = vpRect;
        templateScroll.content  = cRect;

        dd.template    = templateRect;
        dd.captionText = label;
        dd.itemText    = ilText;

        dd.options.Clear();
        foreach (var o in options) dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();

        template.SetActive(false);

        return dd;
    }

    private static GameObject CreateScrollView(string name, Transform parent)
    {
        var root = CreateUI(name, parent);

        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        var scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot     = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = vpRect;
        scrollRect.content  = cRect;

        return root;
    }

    // ================================================================
    //  Helpers
    // ================================================================
    private static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetupVerticalFill(Image img, Color color)
    {
        img.color         = color;
        img.type          = Image.Type.Filled;
        img.fillMethod    = Image.FillMethod.Vertical;
        img.fillOrigin    = (int)Image.OriginVertical.Bottom;
        img.fillAmount    = 0f;
        img.raycastTarget = false;
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