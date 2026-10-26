#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class WiFiHostedNetworkSceneBuilder
{
    private const float FormWidth  = 644f;
    private const float FormHeight = 468f;
    private const string ScenePath = "Assets/Scenes/WiFiHostedNetwork.unity";

    [MenuItem("Tools/WiFiHostedNetwork/Build Scene")]
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

        // Row 1
        var btHnOpen  = CreateButton(root, "btHnOpen",  "Open",  12,  12, 75, 23);
        var btHnClose = CreateButton(root, "btHnClose", "Close", 93,  12, 75, 23);
        var btHnClear = CreateButton(root, "btHnClear", "Clear", 202, 12, 75, 23);

        // Row 2
        var btHnGetConnectionSettings   = CreateButton(root, "btHnGetConnectionSettings",   "Get Connection Settings", 12,  41, 156, 23);
        var btHnGetKey                  = CreateButton(root, "btHnGetKey",                  "Get Key",                 174, 41, 75,  23);
        var btHnGetProfile              = CreateButton(root, "btHnGetProfile",              "Get profile",             255, 41, 75,  23);
        var btHnGetSecuritySettings     = CreateButton(root, "btHnGetSecuritySettings",     "Get security settings",   336, 41, 116, 23);
        var btHnGetStatus               = CreateButton(root, "btHnGetStatus",               "Get status",              458, 41, 75,  23);
        var btHnGetState                = CreateButton(root, "btHnGetState",                "Get state",               539, 41, 75,  23);

        // lvHnData
        var lvHnDataContent = CreateListView(root, "lvHnData", 12, 70, 620, 192,
            new[] { "Parameter name", "Parameter value" },
            new float[] { 280f, 280f });

        // Row 3
        var btHnRefreshSecuritySettings = CreateButton(root, "btHnRefreshSecuritySettings", "Refresh security settings", 12,  277, 143, 23);
        var btHnRestart                 = CreateButton(root, "btHnRestart",                 "Restart",                  161, 277, 75,  23);

        // Row 4 - SSID / Number of peers
        CreateLabel(root, "laHnSsid", "SSID", 12, 327, 32, 13);
        var edSsHnid = CreateInputField(root, "edSsHnid", "MyHostedNetwork", 111, 324, 125, 20);

        CreateLabel(root, "laHnNumberOfPeers", "Number of peers", 12, 355, 85, 13);
        var edHnNumberOfPeers = CreateInputField(root, "edHnNumberOfPeers", "5", 111, 352, 100, 20);

        var btHnSetSettings = CreateButton(root, "btHnSetSettings", "Set settings", 161, 378, 75, 23);

        // Row 4 - Key / Persistent
        CreateLabel(root, "laHnKey", "Key", 333, 327, 25, 13);
        var edHnKey = CreateInputField(root, "edHnKey", "123456789", 374, 324, 131, 20);

        var cbHnPersistent = CreateCheckBox(root, "cbHnPersistent", "Persistent", 336, 354, 72, 17, false);

        var btHnSetKey = CreateButton(root, "btHnSetKey", "Set key", 430, 378, 75, 23);

        // Row 5 - Enable / Disable
        var btHnEnable  = CreateButton(root, "btHnEnable",  "Enable",  12, 433, 75, 23);
        var btHnDisable = CreateButton(root, "btHnDisable", "Disable", 93, 433, 75, 23);

        // Controller
        var ctrlGO = new GameObject("WiFiHostedNetworkController");
        var ctrl = ctrlGO.AddComponent<WiFiHostedNetworkController>();

        ctrl.btHnOpen                    = btHnOpen.GetComponent<Button>();
        ctrl.btHnClose                   = btHnClose.GetComponent<Button>();
        ctrl.btHnClear                   = btHnClear.GetComponent<Button>();
        ctrl.btHnGetConnectionSettings   = btHnGetConnectionSettings.GetComponent<Button>();
        ctrl.btHnGetKey                  = btHnGetKey.GetComponent<Button>();
        ctrl.btHnGetProfile              = btHnGetProfile.GetComponent<Button>();
        ctrl.btHnGetSecuritySettings     = btHnGetSecuritySettings.GetComponent<Button>();
        ctrl.btHnGetStatus               = btHnGetStatus.GetComponent<Button>();
        ctrl.btHnGetState                = btHnGetState.GetComponent<Button>();
        ctrl.btHnRefreshSecuritySettings = btHnRefreshSecuritySettings.GetComponent<Button>();
        ctrl.btHnRestart                 = btHnRestart.GetComponent<Button>();
        ctrl.btHnSetSettings             = btHnSetSettings.GetComponent<Button>();
        ctrl.btHnSetKey                  = btHnSetKey.GetComponent<Button>();
        ctrl.btHnEnable                  = btHnEnable.GetComponent<Button>();
        ctrl.btHnDisable                 = btHnDisable.GetComponent<Button>();

        ctrl.edSsHnid          = edSsHnid;
        ctrl.edHnNumberOfPeers = edHnNumberOfPeers;
        ctrl.edHnKey           = edHnKey;

        ctrl.cbHnPersistent = cbHnPersistent;

        ctrl.lvHnDataContent = lvHnDataContent;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[WiFiHostedNetwork] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[WiFiHostedNetwork] Scene created and saved: " + ScenePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject CreatePanel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateLabel(Transform parent, string name, string text, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 11; tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.black; tmp.raycastTarget = false;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(0.87f, 0.87f, 0.87f);

        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(0.93f, 0.93f, 0.93f);
        c.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = c;

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 11; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black; tmp.raycastTarget = false;
        return go;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string text, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = Color.white;

        var textAreaGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var textAreaRT = textAreaGO.GetComponent<RectTransform>();
        textAreaRT.SetParent(go.transform, false);
        textAreaRT.anchorMin = Vector2.zero; textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(4, 2); textAreaRT.offsetMax = new Vector2(-4, -2);

        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
        var placeholderRT = placeholderGO.GetComponent<RectTransform>();
        placeholderRT.SetParent(textAreaGO.transform, false);
        placeholderRT.anchorMin = Vector2.zero; placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = Vector2.zero; placeholderRT.offsetMax = Vector2.zero;
        var placeholderTmp = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = string.Empty; placeholderTmp.fontSize = 11;
        placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f); placeholderTmp.alignment = TextAlignmentOptions.Left;
        placeholderTmp.enableWordWrapping = false; placeholderTmp.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform));
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(textAreaGO.transform, false);
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        var textTmp = textGO.AddComponent<TextMeshProUGUI>();
        textTmp.text = text; textTmp.fontSize = 11; textTmp.color = Color.black;
        textTmp.alignment = TextAlignmentOptions.Left; textTmp.enableWordWrapping = false;

        var inputField = go.GetComponent<TMP_InputField>();
        inputField.targetGraphic = go.GetComponent<Image>();
        inputField.textViewport = textAreaRT; inputField.textComponent = textTmp;
        inputField.placeholder = placeholderTmp;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.text = text;
        return inputField;
    }

    private static Toggle CreateCheckBox(Transform parent, string name, string label, float x, float y, float w, float h, bool isOn)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);

        var boxGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
        var boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.SetParent(go.transform, false);
        boxRT.anchorMin = new Vector2(0, 0.5f); boxRT.anchorMax = new Vector2(0, 0.5f); boxRT.pivot = new Vector2(0, 0.5f);
        boxRT.anchoredPosition = Vector2.zero; boxRT.sizeDelta = new Vector2(13, 13);
        var boxImg = boxGO.GetComponent<Image>(); boxImg.color = Color.white;

        var checkGO = new GameObject("Check", typeof(RectTransform), typeof(Image));
        var checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.SetParent(boxGO.transform, false);
        checkRT.anchorMin = Vector2.zero; checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = new Vector2(2, 2); checkRT.offsetMax = new Vector2(-2, -2);
        var checkImg = checkGO.GetComponent<Image>(); checkImg.color = new Color(0.2f, 0.2f, 0.2f); checkImg.raycastTarget = false;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(17, 0); labelRT.offsetMax = Vector2.zero;
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label; labelTmp.fontSize = 11; labelTmp.color = Color.black;
        labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.enableWordWrapping = false;
        labelTmp.raycastTarget = false;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = boxImg; toggle.graphic = checkImg; toggle.isOn = isOn;
        return toggle;
    }

    private static RectTransform CreateListView(Transform parent, string name,
        float x, float y, float w, float h, string[] headers, float[] widths)
    {
        const float RowH = 18f;
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(root.transform, false);
        viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1); viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        float totalW = 0;
        for (int i = 0; i < widths.Length; i++) totalW += widths[i];

        var contentGO = new GameObject("Content", typeof(RectTransform));
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.SetParent(viewportGO.transform, false);
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(0, 1); contentRT.pivot = new Vector2(0, 1);
        contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = new Vector2(totalW, 0);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        vlg.spacing = 0; vlg.padding = new RectOffset(0, 0, 0, 0);

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(contentGO.transform, false);
        headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        var hlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.spacing = 0; hlg.padding = new RectOffset(0, 0, 0, 0);
        var hle = headerGO.GetComponent<LayoutElement>();
        hle.preferredHeight = RowH; hle.minHeight = RowH;

        for (int i = 0; i < headers.Length; i++)
        {
            var cellGO = new GameObject("h" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(headerGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = widths[i]; cellLe.minWidth = widths[i];
            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.text = headers[i]; tmp.fontSize = 11; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left; tmp.color = Color.black;
            tmp.enableWordWrapping = false; tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(4, 0, 2, 0); tmp.raycastTarget = false;
        }

        var sr = root.GetComponent<ScrollRect>();
        sr.content = contentRT; sr.viewport = viewportRT;
        sr.horizontal = true; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return contentRT;
    }
}
#endif