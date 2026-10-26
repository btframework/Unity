#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MobileHotspotSceneBuilder
{
    private const float FormWidth = 461f;
    private const float FormHeight = 316f;
    private const string ScenePath = "Assets/Scenes/MobileHotspot.unity";

    [MenuItem("Tools/MobileHotspot/Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.94f, 0.94f, 0.94f);
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.tag = "MainCamera";

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(FormWidth, FormHeight);
        scaler.matchWidthOrHeight = 1;
        canvasGo.AddComponent<GraphicRaycaster>();

        var font = GetDefaultFont();
        if (font == null)
        {
            Debug.LogError("MobileHotspotSceneBuilder: TMP default font is null. Import TMP Essential Resources and re-run Build Scene.");
            return;
        }

        var formBg = CreateFormBackground(canvasGo.transform, "FormBackground", new Color(0.94f, 0.94f, 0.94f));

        var ctrlGo = new GameObject("MobileHotspotController");
        var ctrl = ctrlGo.AddComponent<MobileHotspotController>();

        ctrl.btOpen = CreateButton(formBg, "btOpen", 12, 12, 75, 23, "Open", font);
        ctrl.btClose = CreateButton(formBg, "btClose", 93, 12, 75, 23, "Close", font);
        ctrl.btStart = CreateButton(formBg, "btStart", 200, 12, 75, 23, "Start", font);
        ctrl.btStop = CreateButton(formBg, "btStop", 281, 12, 75, 23, "Stop", font);
        ctrl.btGetState = CreateButton(formBg, "btGetState", 377, 12, 75, 23, "Get state", font);

        CreateLabel(formBg, "laSssid", 12, 55, 60, 13, "SSID", font);
        CreateLabel(formBg, "laPwd", 12, 81, 60, 13, "Password", font);
        ctrl.edSsid = CreateInputField(formBg, "edSsid", 79, 52, 115, 20, "", font);
        ctrl.edPassword = CreateInputField(formBg, "edPassword", 79, 78, 115, 20, "", font);

        ctrl.btGetSsid = CreateButton(formBg, "btGetSsid", 200, 50, 75, 23, "Get SSID", font);
        ctrl.btGetPwd = CreateButton(formBg, "btGetPwd", 200, 76, 75, 23, "Get pwd", font);
        ctrl.btSetSsid = CreateButton(formBg, "btSetSsid", 281, 50, 75, 23, "Set SSID", font);
        ctrl.btSetPwd = CreateButton(formBg, "btSetPwd", 281, 76, 75, 23, "Set pwd", font);
        ctrl.btMaxClients = CreateButton(formBg, "btMaxClients", 377, 50, 75, 23, "Max clients", font);
        ctrl.btConnected = CreateButton(formBg, "btConnected", 377, 76, 75, 23, "Connected", font);

        CreateLabel(formBg, "laBand", 12, 107, 60, 13, "Band", font);
        ctrl.cbBand = CreateDropdown(formBg, "cbBand", 79, 104, 115, 21, new[] { "Auto", "2.4", "5.8" }, font);

        ctrl.btIsSupported = CreateButton(formBg, "btIsSupported", 200, 102, 75, 23, "Is supported", font);
        ctrl.btSetBand = CreateButton(formBg, "btSetBand", 281, 102, 75, 23, "Set band", font);
        ctrl.btGetBand = CreateButton(formBg, "btGetBand", 362, 102, 75, 23, "Get band", font);

        ctrl.btConnectedClients = CreateButton(formBg, "btConnectedClients", 12, 131, 109, 23, "Connected clients", font);
        ctrl.btCheckTimeout = CreateButton(formBg, "btCheckTimeout", 139, 131, 91, 23, "Check timeout", font);
        ctrl.btEnableTimeout = CreateButton(formBg, "btEnableTimeout", 236, 131, 91, 23, "Enable timeout", font);
        ctrl.btDisableTimeout = CreateButton(formBg, "btDisableTimeout", 333, 131, 92, 23, "Disable timeout", font);

        ctrl.lvConnectedClientsContent = CreateListView(
            formBg, "lvConnectedClients", 15, 160, 434, 144,
            new[] { "Mac", "Host name" }, new[] { 150f, 250f }, font);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("Failed to save scene " + ScenePath);
            return;
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("Build Settings scenes replaced with single entry: " + ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);
        Debug.Log("MobileHotspotSceneBuilder: scene built and saved with font " + font.name);
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        TMP_FontAsset font = null;
        try { font = TMP_Settings.defaultFontAsset; } catch { }
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
            Debug.LogWarning("MobileHotspotSceneBuilder: TMP default font asset not found. Import TMP Essential Resources first.");
        return font;
    }

    private static RectTransform CreateFormBackground(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private static Button CreateButton(Transform parent, string name, float x, float y, float w, float h, string text, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.94f, 0.94f, 0.94f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(0.94f, 0.94f, 0.94f);
        colors.highlightedColor = new Color(0.85f, 0.92f, 1f);
        colors.pressedColor = new Color(0.75f, 0.82f, 0.95f);
        btn.colors = colors;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        var txtGo = new GameObject("Text");
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 12;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        return btn;
    }

    private static void CreateLabel(Transform parent, string name, float x, float y, float w, float h, string text, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 12;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, float x, float y, float w, float h, string text, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var bg = go.AddComponent<Image>();
        bg.color = Color.white;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        var viewportGo = new GameObject("Text Area");
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(rt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(4, 2);
        viewportRt.offsetMax = new Vector2(-4, -2);
        viewportGo.AddComponent<RectMask2D>();

        var textGo = new GameObject("Text");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(viewportRt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmpText = textGo.AddComponent<TextMeshProUGUI>();
        tmpText.font = font;
        tmpText.text = text;
        tmpText.fontSize = 12;
        tmpText.color = Color.black;
        tmpText.alignment = TextAlignmentOptions.Left;
        tmpText.enableWordWrapping = false;

        var inputField = go.AddComponent<TMP_InputField>();
        inputField.textViewport = viewportRt;
        inputField.textComponent = tmpText;
        inputField.text = text;
        inputField.lineType = TMP_InputField.LineType.SingleLine;

        return inputField;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, float x, float y, float w, float h, string[] options, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var bg = go.AddComponent<Image>();
        bg.color = Color.white;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        var dropdown = go.AddComponent<TMP_Dropdown>();

        var labelGo = new GameObject("Label");
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(4, 0);
        labelRt.offsetMax = new Vector2(-20, 0);
        var labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.font = font;
        labelTxt.fontSize = 12;
        labelTxt.color = Color.black;
        labelTxt.alignment = TextAlignmentOptions.Left;
        labelTxt.enableWordWrapping = false;

        var arrowGo = new GameObject("Arrow");
        var arrowRt = arrowGo.AddComponent<RectTransform>();
        arrowRt.SetParent(rt, false);
        arrowRt.anchorMin = new Vector2(1, 0.5f);
        arrowRt.anchorMax = new Vector2(1, 0.5f);
        arrowRt.pivot = new Vector2(1, 0.5f);
        arrowRt.anchoredPosition = new Vector2(-4, 0);
        arrowRt.sizeDelta = new Vector2(12, 12);
        var arrowTxt = arrowGo.AddComponent<TextMeshProUGUI>();
        arrowTxt.font = font;
        arrowTxt.text = "v";
        arrowTxt.fontSize = 10;
        arrowTxt.color = Color.black;
        arrowTxt.alignment = TextAlignmentOptions.Center;

        var templateGo = new GameObject("Template");
        var templateRt = templateGo.AddComponent<RectTransform>();
        templateRt.SetParent(rt, false);
        templateRt.anchorMin = new Vector2(0, 0);
        templateRt.anchorMax = new Vector2(1, 0);
        templateRt.pivot = new Vector2(0.5f, 1);
        templateRt.anchoredPosition = new Vector2(0, 2);
        templateRt.sizeDelta = new Vector2(0, options.Length * 20f + 2f);
        var templateImg = templateGo.AddComponent<Image>();
        templateImg.color = Color.white;
        var scroll = templateGo.AddComponent<ScrollRect>();
        templateGo.SetActive(false);

        var viewportGo = new GameObject("Viewport");
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(templateRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();

        var contentGo = new GameObject("Content");
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, options.Length * 20f);
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var itemGo = new GameObject("Item");
        var itemRt = itemGo.AddComponent<RectTransform>();
        itemRt.SetParent(contentRt, false);
        var itemLe = itemGo.AddComponent<LayoutElement>();
        itemLe.preferredHeight = 20;
        var itemBg = itemGo.AddComponent<Image>();
        itemBg.color = Color.white;
        var toggle = itemGo.AddComponent<Toggle>();
        toggle.targetGraphic = itemBg;

        var checkGo = new GameObject("Item Check");
        var checkRt = checkGo.AddComponent<RectTransform>();
        checkRt.SetParent(itemRt, false);
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = Vector2.zero;
        checkRt.offsetMax = Vector2.zero;
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.4f, 0.7f, 1f, 0.4f);
        toggle.graphic = checkImg;

        var itemLabelGo = new GameObject("Item Label");
        var itemLabelRt = itemLabelGo.AddComponent<RectTransform>();
        itemLabelRt.SetParent(itemRt, false);
        itemLabelRt.anchorMin = Vector2.zero;
        itemLabelRt.anchorMax = Vector2.one;
        itemLabelRt.offsetMin = new Vector2(4, 0);
        itemLabelRt.offsetMax = new Vector2(-4, 0);
        var itemTxt = itemLabelGo.AddComponent<TextMeshProUGUI>();
        itemTxt.font = font;
        itemTxt.fontSize = 12;
        itemTxt.color = Color.black;
        itemTxt.alignment = TextAlignmentOptions.Left;

        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;

        dropdown.template = templateRt;
        dropdown.captionText = labelTxt;
        dropdown.itemText = itemTxt;

        dropdown.options.Clear();
        foreach (var o in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(o));

        return dropdown;
    }

    private static RectTransform CreateListView(Transform parent, string name, float x, float y, float w, float h, string[] columnNames, float[] columnWidths, TMP_FontAsset font)
    {
        var rootGo = new GameObject(name);
        var rootRt = rootGo.AddComponent<RectTransform>();
        rootRt.SetParent(parent, false);
        rootRt.anchorMin = new Vector2(0, 1);
        rootRt.anchorMax = new Vector2(0, 1);
        rootRt.pivot = new Vector2(0, 1);
        rootRt.anchoredPosition = new Vector2(x, -y);
        rootRt.sizeDelta = new Vector2(w, h);

        var bgImg = rootGo.AddComponent<Image>();
        bgImg.color = Color.white;
        var rootOutline = rootGo.AddComponent<Outline>();
        rootOutline.effectColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        rootOutline.effectDistance = new Vector2(1, -1);

        var scrollRect = rootGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var headerGo = new GameObject("Header");
        var headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.SetParent(rootRt, false);
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0, 1);
        headerRt.anchoredPosition = new Vector2(0, 0);
        headerRt.sizeDelta = new Vector2(0, 20);
        var headerBg = headerGo.AddComponent<Image>();
        headerBg.color = new Color(0.9f, 0.9f, 0.9f);
        var headerHlg = headerGo.AddComponent<HorizontalLayoutGroup>();
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = false;
        headerHlg.childControlWidth = false;
        headerHlg.childControlHeight = false;
        headerHlg.childAlignment = TextAnchor.MiddleLeft;
        headerHlg.padding = new RectOffset(2, 2, 2, 2);

        for (int i = 0; i < columnNames.Length; i++)
        {
            var cellGo = new GameObject("Col_" + columnNames[i]);
            var cellRt = cellGo.AddComponent<RectTransform>();
            cellRt.SetParent(headerRt, false);
            var le = cellGo.AddComponent<LayoutElement>();
            le.preferredWidth = columnWidths[i];
            le.preferredHeight = 20;

            var cellTxtGo = new GameObject("Text");
            var cellTxtRt = cellTxtGo.AddComponent<RectTransform>();
            cellTxtRt.SetParent(cellRt, false);
            cellTxtRt.anchorMin = Vector2.zero;
            cellTxtRt.anchorMax = Vector2.one;
            cellTxtRt.offsetMin = new Vector2(4, 0);
            cellTxtRt.offsetMax = new Vector2(-4, 0);
            var cellTmp = cellTxtGo.AddComponent<TextMeshProUGUI>();
            cellTmp.font = font;
            cellTmp.text = columnNames[i];
            cellTmp.fontSize = 12;
            cellTmp.fontStyle = FontStyles.Bold;
            cellTmp.color = Color.black;
            cellTmp.alignment = TextAlignmentOptions.Left;
            cellTmp.enableWordWrapping = false;
        }

        var viewportGo = new GameObject("Viewport");
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(rootRt, false);
        viewportRt.anchorMin = new Vector2(0, 0);
        viewportRt.anchorMax = new Vector2(1, 1);
        viewportRt.offsetMin = new Vector2(0, 0);
        viewportRt.offsetMax = new Vector2(0, -20);
        var vpImg = viewportGo.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.001f);
        viewportGo.AddComponent<RectMask2D>();

        var contentGo = new GameObject("Content");
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.UpperLeft;
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        return contentRt;
    }
}
#endif