#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class BleSnifferSceneBuilder
{
    private const float FormWidth  = 524f;
    private const float FormHeight = 432f;
    private const string ScenePath = "Assets/Scenes/BleSniffer.unity";

    [MenuItem("Tools/BleSniffer/Build Scene")]
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

        // Buttons
        var btStart = CreateButton(canvasGO.transform, "btStart", "Start", 12, 12, 75, 23).GetComponent<Button>();
        var btStop  = CreateButton(canvasGO.transform, "btStop",  "Stop",  292, 12, 75, 23).GetComponent<Button>();
        var btClear = CreateButton(canvasGO.transform, "btClear", "Clear", 434, 12, 75, 23).GetComponent<Button>();

        // Label + combobox
        CreateLabel(canvasGO.transform, "laChannel", "Channel", 93, 17, 46, 13);

        var cbChannel = CreateDropdown(canvasGO.transform, "cbChannel",
            new[] { "37", "38", "39" }, 155, 12, 121, 21, 0);

        // lbPackets
        var lbPacketsScroll = CreatePacketListBox(canvasGO.transform, "lbPackets", 12, 41, 497, 368);
        var lbPacketsText = lbPacketsScroll.content.GetComponent<TextMeshProUGUI>();

        // Runtime controller
        var ctrlGO = new GameObject("BleSnifferController");
        var ctrl = ctrlGO.AddComponent<BleSnifferController>();
        ctrl.btStart       = btStart;
        ctrl.btStop        = btStop;
        ctrl.btClear       = btClear;
        ctrl.cbChannel     = cbChannel;
        ctrl.lbPacketsScroll = lbPacketsScroll;
        ctrl.lbPacketsText   = lbPacketsText;

        // Save scene
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[BleSniffer] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[BleSniffer] Scene created and saved: " + ScenePath);
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
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;

        return go;
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
    //  Packet list box (monospace-style log)
    // ==========================================================

    private static ScrollRect CreatePacketListBox(Transform parent, string name,
        float x, float y, float w, float h)
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
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
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