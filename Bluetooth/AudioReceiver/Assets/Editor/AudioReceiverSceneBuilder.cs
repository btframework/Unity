#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class AudioReceiverSceneBuilder
{
    private const float FormWidth  = 551f;
    private const float FormHeight = 422f;
    private const string ScenePath = "Assets/Scenes/AudioReceiver.unity";

    [MenuItem("Tools/AudioReceiver/Build Scene")]
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

        // Buttons
        var btStart   = CreateButton(canvasGO.transform, "btStart",   "Start",   12,  12, 75, 23);
        var btStop    = CreateButton(canvasGO.transform, "btStop",    "Stop",    93,  12, 75, 23);
        var btOpen    = CreateButton(canvasGO.transform, "btOpen",    "Open",    302, 13, 75, 23);
        var btClose   = CreateButton(canvasGO.transform, "btClose",   "Close",   383, 13, 75, 23);
        var btConnect = CreateButton(canvasGO.transform, "btConnect", "Connect", 464, 12, 75, 23);
        var btClear   = CreateButton(canvasGO.transform, "btClear",   "Clear",   464, 154, 75, 23);

        // ListView
        var lvDevices = CreateListView(canvasGO.transform, "lvDevices", 12, 41, 527, 107);

        // ListBox (log)
        var lbLog = CreateListBox(canvasGO.transform, "lbLog", 12, 183, 527, 225);

        // Runtime controller
        var ctrlGO = new GameObject("AudioReceiverController");
        var ctrl = ctrlGO.AddComponent<AudioReceiverController>();
        ctrl.btStart          = btStart.GetComponent<Button>();
        ctrl.btStop           = btStop.GetComponent<Button>();
        ctrl.btOpen           = btOpen.GetComponent<Button>();
        ctrl.btClose          = btClose.GetComponent<Button>();
        ctrl.btConnect        = btConnect.GetComponent<Button>();
        ctrl.btClear          = btClear.GetComponent<Button>();
        ctrl.lvDevicesScroll  = lvDevices.GetComponent<ScrollRect>();
        ctrl.lvDevicesContent = ctrl.lvDevicesScroll.content;
        ctrl.lbLogText        = lbLog.GetComponent<ScrollRect>().content.GetComponent<TextMeshProUGUI>();

        // Save scene to disk
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[AudioReceiver] Failed to save scene: " + ScenePath);
            return;
        }

        // Add scene to build settings if not already there
        AddSceneToBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the scene asset so user sees it in Project window
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[AudioReceiver] Scene created and saved: " + ScenePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
            if (s.path == scenePath) return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

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
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f);
        colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = colors;

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

    private static GameObject CreateListView(Transform parent, string name, float x, float y, float w, float h)
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

        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(root.transform, false);
        var headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot     = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, 20);
        headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);

        CreateHeaderText(headerGO.transform, "chDeviceId", "Device ID", 2f,   246f);
        CreateHeaderText(headerGO.transform, "chName",     "Name",      250f, 148f);

        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.SetParent(root.transform, false);
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(1, 1);
        scrollRT.offsetMax = new Vector2(-1, -21);
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
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight     = false;
        vlg.childControlWidth      = true;
        vlg.spacing = 1;
        vlg.padding = new RectOffset(1, 1, 1, 1);

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content  = contentRT;
        sr.viewport = viewportRT;

        return root;
    }

    private static void CreateHeaderText(Transform parent, string name, string text, float x, float w)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot     = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(w, 18);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 11;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
    }

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
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content  = contentRT;
        sr.viewport = viewportRT;

        return root;
    }
}
#endif