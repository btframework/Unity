#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class WiiRemoteSceneBuilder
{
    private const float FormWidth  = 650f;
    private const float FormHeight = 590f;
    private const string ScenePath = "Assets/Scenes/WiiRemote.unity";

    [MenuItem("Tools/WiiRemote/Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.94f, 0.94f, 0.94f);
        camGO.transform.position = new Vector3(0, 0, -10);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

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

        var root = canvasGO.transform;

        // ---- Row 1: Discover / Connect / Disconnect / Battery / LEDs / Rumble
        var btDiscover        = CreateButton(root, "btDiscover", "Discover", 12, 12, 75, 23);
        var btConnect         = CreateButton(root, "btConnect", "Connect", 183, 12, 75, 23);
        var btDisconnect      = CreateButton(root, "btDisconnect", "Disconnect", 264, 12, 75, 23);
        var laBattLevelCaption = CreateLabel(root, "laBattLevelCaption", "Batt level:", 361, 17, 54, 13);
        var laBattLevel       = CreateLabel(root, "laBattLevel", "0.00", 421, 17, 28, 13);
        var btGetLeds         = CreateButton(root, "btGetLeds", "Get LEDs", 364, 41, 75, 23);
        var btGetStatus       = CreateButton(root, "btGetStatus", "Get status", 473, 12, 75, 23);
        var btSetLeds         = CreateButton(root, "btSetLeds", "Set LEDs", 473, 41, 75, 23);
        var btRumbleOn        = CreateButton(root, "btRumbleOn", "Rumble ON", 565, 12, 75, 23);
        var btRumbleOff       = CreateButton(root, "btRumbleOff", "Rumble OFF", 565, 41, 75, 23);

        // ---- lvDevices
        var lvDevicesContent = CreateListView(root, "lvDevices", 12, 41, 327, 97,
            new[] { "Address", "Name" },
            new float[] { 150, 150 });

        // ---- LEDs
        var cbLed1 = CreateCheckBox(root, "cbLed1", "LED1", 376, 70, 53, 17, false);
        var cbLed2 = CreateCheckBox(root, "cbLed2", "LED2", 376, 93, 53, 17, false);
        var cbLed3 = CreateCheckBox(root, "cbLed3", "LED3", 435, 70, 53, 17, false);
        var cbLed4 = CreateCheckBox(root, "cbLed4", "LED4", 435, 93, 53, 17, false);

        // ---- Buttons
        CreateLabel(root, "laButtons", "Buttons", 12, 156, 50, 13, true);
        var cbA     = CreateCheckBox(root, "cbA",     "A",     25,  182, 71, 17, false);
        var cbB     = CreateCheckBox(root, "cbB",     "B",     25,  205, 71, 17, false);
        var cbPlus  = CreateCheckBox(root, "cbPlus",  "Plus",  25,  228, 71, 17, false);
        var cbHome  = CreateCheckBox(root, "cbHome",  "Home",  25,  251, 71, 17, false);
        var cbMinus = CreateCheckBox(root, "cbMinus", "Minus", 25,  274, 71, 17, false);
        var cbOne   = CreateCheckBox(root, "cbOne",   "One",   102, 182, 68, 17, false);
        var cbTwo   = CreateCheckBox(root, "cbTwo",   "Two",   102, 205, 68, 17, false);
        var cbUp    = CreateCheckBox(root, "cbUp",    "Up",    102, 228, 68, 17, false);
        var cbDown  = CreateCheckBox(root, "cbDown",  "Down",  102, 251, 68, 17, false);
        var cbLeft  = CreateCheckBox(root, "cbLeft",  "Left",  102, 274, 68, 17, false);
        var cbRight = CreateCheckBox(root, "cbRight", "Right", 40,  297, 56, 17, false);

        // ---- Accel
        CreateLabel(root, "laAccel", "Accel", 12, 341, 39, 13, true);
        CreateLabel(root, "laXCaption", "X:", 22, 365, 17, 13);
        CreateLabel(root, "laYCaption", "Y:", 22, 390, 17, 13);
        CreateLabel(root, "laZCaption", "Z:", 22, 414, 17, 13);
        var laX = CreateLabel(root, "laX", "0", 45, 365, 13, 13);
        var laY = CreateLabel(root, "laY", "0", 45, 390, 13, 13);
        var laZ = CreateLabel(root, "laZ", "0", 45, 414, 13, 13);
        var btEnableAccel  = CreateButton(root, "btEnableAccel",  "Enable",  102, 360, 75, 23);
        var btDisableAccel = CreateButton(root, "btDisableAccel", "Disable", 102, 390, 75, 23);

        // ---- IR
        CreateLabel(root, "laIrCaption", "IR", 223, 156, 20, 13, true);
        CreateLabel(root, "laIrModeCpation", "Mode:", 223, 183, 37, 13);
        var laIrMode = CreateLabel(root, "laIrMode", "Off", 293, 186, 21, 13);

        // Sensor 0
        CreateLabel(root, "laSensor0Caption", "Sensor 0", 223, 205, 49, 13);
        CreateLabel(root, "laSensor0PositionCaption", "Position: ", 237, 228, 50, 13);
        CreateLabel(root, "laSensor0SizeCaption", "Size:", 237, 251, 30, 13);
        CreateLabel(root, "laSensor0FoundCaption", "Found:", 237, 274, 40, 13);
        var laSensor0Position = CreateLabel(root, "laSensor0Position", "0; 0", 293, 228, 25, 13);
        var laSensor0Size     = CreateLabel(root, "laSensor0Size", "0", 293, 251, 13, 13);
        var laSensor0Found    = CreateLabel(root, "laSensor0Found", "False", 293, 274, 32, 13);

        // Sensor 1
        CreateLabel(root, "laSensor1Caption", "Sensor 1", 350, 205, 49, 13);
        CreateLabel(root, "laSensor1PositionCaption", "Position: ", 364, 228, 50, 13);
        CreateLabel(root, "laSensor1SizeCaption", "Size:", 364, 251, 30, 13);
        CreateLabel(root, "laSensor1FoundCaption", "Found:", 364, 274, 40, 13);
        var laSensor1Position = CreateLabel(root, "laSensor1Position", "0; 0", 420, 228, 25, 13);
        var laSensor1Size     = CreateLabel(root, "laSensor1Size", "0", 420, 251, 13, 13);
        var laSensor1Found    = CreateLabel(root, "laSensor1Found", "False", 420, 274, 32, 13);

        // Sensor 2
        CreateLabel(root, "laSensor2Caption", "Sensor 2", 223, 297, 49, 13);
        CreateLabel(root, "laSensor2PositionCaption", "Position: ", 237, 321, 50, 13);
        CreateLabel(root, "laSensor2SizeCaption", "Size:", 237, 344, 30, 13);
        CreateLabel(root, "laSensor2FoundCaption", "Found:", 237, 367, 40, 13);
        var laSensor2Position = CreateLabel(root, "laSensor2Position", "0; 0", 293, 321, 25, 13);
        var laSensor2Size     = CreateLabel(root, "laSensor2Size", "0", 293, 344, 13, 13);
        var laSensor2Found    = CreateLabel(root, "laSensor2Found", "False", 293, 367, 32, 13);

        // Sensor 3
        CreateLabel(root, "laSensor3Caption", "Sensor 3", 350, 297, 49, 13);
        CreateLabel(root, "laSensor3PositionCaption", "Position: ", 364, 321, 50, 13);
        CreateLabel(root, "laSensor3SizeCpation", "Size:", 364, 344, 30, 13);
        CreateLabel(root, "laSensor3FoundCaption", "Found:", 364, 367, 40, 13);
        var laSensor3Position = CreateLabel(root, "laSensor3Position", "0; 0", 420, 321, 25, 13);
        var laSensor3Size     = CreateLabel(root, "laSensor3Size", "0", 420, 344, 13, 13);
        var laSensor3Found    = CreateLabel(root, "laSensor3Found", "False", 420, 367, 32, 13);

        var btEnableIr  = CreateButton(root, "btEnableIr",  "Enable",  226, 390, 75, 23);
        var btDisableIr = CreateButton(root, "btDisableIr", "Disable", 307, 390, 75, 23);
        CreateLabel(root, "laIrSensitivityCaption", "Sensitivity:", 350, 182, 57, 13);
        var laIrSensitivity = CreateLabel(root, "laIrSensitivity", "Off", 414, 182, 21, 13);

        // ---- Nunchuk
        CreateLabel(root, "laNunchukCaption", "Nunchuk", 492, 156, 58, 13, true);
        var laNunchukState = CreateLabel(root, "laNunchukState", "Detached", 584, 156, 54, 13);
        CreateLabel(root, "laNunchukXCaption", "X:", 513, 182, 17, 13);
        CreateLabel(root, "laNunchukYCaption", "Y:", 513, 206, 17, 13);
        CreateLabel(root, "laNunchukZCaption", "Z:", 513, 228, 17, 13);
        CreateLabel(root, "laNunchukJoyXCaption", "Joy X:", 513, 251, 36, 13);
        CreateLabel(root, "laNunchukJoyYCaption", "Joy Y:", 513, 274, 36, 13);
        var laNunchukX    = CreateLabel(root, "laNunchukX", "0", 536, 183, 13, 13);
        var laNunchukY    = CreateLabel(root, "laNunchukY", "0", 536, 205, 13, 13);
        var laNunchukZ    = CreateLabel(root, "laNunchukZ", "0", 536, 228, 13, 13);
        var laNunchukJoyX = CreateLabel(root, "laNunchukJoyX", "0", 555, 251, 13, 13);
        var laNunchukJoyY = CreateLabel(root, "laNunchukJoyY", "0", 555, 274, 13, 13);
        var cbNunchukC    = CreateCheckBox(root, "cbNunchukC", "C", 598, 182, 33, 17, false);
        var cbNunchukZ    = CreateCheckBox(root, "cbNunchukZ", "Z", 598, 204, 33, 17, false);

        // ---- Balance board
        CreateLabel(root, "laBalanceBoardCaption", "Balance board", 492, 301, 89, 13, true);
        var laBalanceBoardState = CreateLabel(root, "laBalanceBoardState", "Detached", 587, 301, 54, 13);
        CreateLabel(root, "laBbTopRightCaption", "Top right:", 513, 321, 52, 13);
        CreateLabel(root, "laBbTopLeftCaption", "Top left:", 515, 344, 46, 13);
        CreateLabel(root, "laBbBottomRightCaption", "Bottom right:", 515, 367, 66, 13);
        CreateLabel(root, "laBbBottomLeftCaption", "Bottom left:", 515, 390, 60, 13);
        var laBbTopRight    = CreateLabel(root, "laBbTopRight", "0", 596, 321, 13, 13);
        var laBbTopLeft     = CreateLabel(root, "laBbTopLeft", "0", 595, 344, 13, 13);
        var laBbBottomRight = CreateLabel(root, "laBbBottomRight", "0", 595, 367, 13, 13);
        var laBbBottomLeft  = CreateLabel(root, "laBbBottomLeft", "0", 595, 390, 13, 13);

        // ---- Log
        var lbLogText = CreateListBox(root, "lbLog", 12, 482, 626, 95);

        // ---- btClear
        var btClear = CreateButton(root, "btClear", "Clear", 12, 453, 75, 23);

        // ---- Controller
        var ctrlGO = new GameObject("WiiRemoteController");
        var ctrl = ctrlGO.AddComponent<WiiRemoteController>();

        ctrl.btDiscover       = btDiscover.GetComponent<Button>();
        ctrl.btConnect        = btConnect.GetComponent<Button>();
        ctrl.btDisconnect     = btDisconnect.GetComponent<Button>();
        ctrl.btGetLeds        = btGetLeds.GetComponent<Button>();
        ctrl.btGetStatus      = btGetStatus.GetComponent<Button>();
        ctrl.btSetLeds        = btSetLeds.GetComponent<Button>();
        ctrl.btRumbleOn       = btRumbleOn.GetComponent<Button>();
        ctrl.btRumbleOff      = btRumbleOff.GetComponent<Button>();
        ctrl.btEnableAccel    = btEnableAccel.GetComponent<Button>();
        ctrl.btDisableAccel   = btDisableAccel.GetComponent<Button>();
        ctrl.btEnableIr       = btEnableIr.GetComponent<Button>();
        ctrl.btDisableIr      = btDisableIr.GetComponent<Button>();
        ctrl.btClear          = btClear.GetComponent<Button>();

        ctrl.cbLed1 = cbLed1;
        ctrl.cbLed2 = cbLed2;
        ctrl.cbLed3 = cbLed3;
        ctrl.cbLed4 = cbLed4;

        ctrl.cbA     = cbA;
        ctrl.cbB     = cbB;
        ctrl.cbPlus  = cbPlus;
        ctrl.cbHome  = cbHome;
        ctrl.cbMinus = cbMinus;
        ctrl.cbOne   = cbOne;
        ctrl.cbTwo   = cbTwo;
        ctrl.cbUp    = cbUp;
        ctrl.cbDown  = cbDown;
        ctrl.cbLeft  = cbLeft;
        ctrl.cbRight = cbRight;

        ctrl.cbNunchukC = cbNunchukC;
        ctrl.cbNunchukZ = cbNunchukZ;

        ctrl.laBattLevel        = laBattLevel;
        ctrl.laX                = laX;
        ctrl.laY                = laY;
        ctrl.laZ                = laZ;
        ctrl.laIrMode           = laIrMode;
        ctrl.laIrSensitivity    = laIrSensitivity;

        ctrl.laSensor0Position  = laSensor0Position;
        ctrl.laSensor0Size      = laSensor0Size;
        ctrl.laSensor0Found     = laSensor0Found;
        ctrl.laSensor1Position  = laSensor1Position;
        ctrl.laSensor1Size      = laSensor1Size;
        ctrl.laSensor1Found     = laSensor1Found;
        ctrl.laSensor2Position  = laSensor2Position;
        ctrl.laSensor2Size      = laSensor2Size;
        ctrl.laSensor2Found     = laSensor2Found;
        ctrl.laSensor3Position  = laSensor3Position;
        ctrl.laSensor3Size      = laSensor3Size;
        ctrl.laSensor3Found     = laSensor3Found;

        ctrl.laNunchukState = laNunchukState;
        ctrl.laNunchukX     = laNunchukX;
        ctrl.laNunchukY     = laNunchukY;
        ctrl.laNunchukZ     = laNunchukZ;
        ctrl.laNunchukJoyX  = laNunchukJoyX;
        ctrl.laNunchukJoyY  = laNunchukJoyY;

        ctrl.laBalanceBoardState = laBalanceBoardState;
        ctrl.laBbTopRight        = laBbTopRight;
        ctrl.laBbTopLeft         = laBbTopLeft;
        ctrl.laBbBottomRight     = laBbBottomRight;
        ctrl.laBbBottomLeft      = laBbBottomLeft;

        ctrl.lvDevicesContent = lvDevicesContent;
        ctrl.lbLogText        = lbLogText;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[WiiRemote] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[WiiRemote] Scene created and saved: " + ScenePath);
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

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
        float x, float y, float w, float h, bool bold = false)
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
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.black;
        tmp.raycastTarget = false;
        return tmp;
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

        var boxGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
        var boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.SetParent(go.transform, false);
        boxRT.anchorMin = new Vector2(0, 0.5f);
        boxRT.anchorMax = new Vector2(0, 0.5f);
        boxRT.pivot     = new Vector2(0, 0.5f);
        boxRT.anchoredPosition = Vector2.zero;
        boxRT.sizeDelta = new Vector2(13, 13);
        var boxImg = boxGO.GetComponent<Image>();
        boxImg.color = Color.white;

        var checkGO = new GameObject("Check", typeof(RectTransform), typeof(Image));
        var checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.SetParent(boxGO.transform, false);
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = new Vector2(2, 2);
        checkRT.offsetMax = new Vector2(-2, -2);
        var checkImg = checkGO.GetComponent<Image>();
        checkImg.color = new Color(0.2f, 0.2f, 0.2f);
        checkImg.raycastTarget = false;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(17, 0);
        labelRT.offsetMax = Vector2.zero;
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 11;
        labelTmp.color = Color.black;
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.enableWordWrapping = false;
        labelTmp.raycastTarget = false;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;
        toggle.isOn = isOn;

        return toggle;
    }

    private static RectTransform CreateListView(Transform parent, string name,
        float x, float y, float w, float h, string[] headers, float[] widths)
    {
        const float RowH = 18f;
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(root.transform, false);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1);
        viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        float contentW = 0;
        for (int i = 0; i < widths.Length; i++) contentW += widths[i];

        var contentGO = new GameObject("Content", typeof(RectTransform));
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.SetParent(viewportGO.transform, false);
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot     = new Vector2(0, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(contentW, 0);
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(contentGO.transform, false);
        headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        var hlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 0;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        var hle = headerGO.GetComponent<LayoutElement>();
        hle.preferredHeight = RowH;
        hle.minHeight = RowH;

        for (int i = 0; i < headers.Length; i++)
        {
            var cellGO = new GameObject("h" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(headerGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = widths[i];
            cellLe.minWidth = widths[i];
            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.text = headers[i];
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.black;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(4, 0, 2, 0);
            tmp.raycastTarget = false;
        }

        var sr = root.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.viewport = viewportRT;
        sr.horizontal = true;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return contentRT;
    }

    private static TextMeshProUGUI CreateListBox(Transform parent, string name,
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
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(root.transform, false);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1);
        viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.SetParent(viewportGO.transform, false);
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

        return tmp;
    }
}
#endif