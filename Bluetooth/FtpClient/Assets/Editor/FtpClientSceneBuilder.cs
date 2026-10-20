#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class FtpClientSceneBuilder
{
    private const float FormWidth  = 435f;
    private const float FormHeight = 581f;
    private const string ScenePath = "Assets/Scenes/FtpClient.unity";

    [MenuItem("Tools/FtpClient/Build Scene")]
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

        // Row 1 (top buttons)
        var btDiscover   = CreateButton(root, "btDiscover",   "Discover",   12,  7, 75, 23);
        var btConnect    = CreateButton(root, "btConnect",    "Connect",    268, 7, 75, 23);
        var btDisconnect = CreateButton(root, "btDisconnect", "Disconnect", 349, 7, 75, 23);

        // lvDevices
        var lvDevicesContent = CreateListView(root, "lvDevices", 12, 36, 412, 88,
            new[] { "Address", "Name" },
            new float[] { 150, 150 });

        // Session buttons
        var btOpenSession  = CreateButton(root, "btOpenSession",  "Open Session",  12,  130, 85, 23);
        var btCloseSession = CreateButton(root, "btCloseSession", "Close Session", 103, 130, 84, 23);

        // ProgressBar
        var progressFill = CreateProgressBar(root, "ProgressBar", 11, 159, 412, 23);

        // File row
        var btSelectFile = CreateButton(root, "btSelectFile", "Select File", 11,  188, 75, 23);
        var edFileName   = CreateInputField(root, "edFileName", "", 92, 190, 250, 20);
        var btSendFile   = CreateButton(root, "btSendFile", "Send file", 348, 187, 75, 23);

        // New dir row
        var laNewDirName = CreateLabel(root, "laNewDirName", "New dir name", 12, 219, 72, 13);
        var edNewDirName = CreateInputField(root, "edNewDirName", "", 92, 216, 250, 20);
        var btMkDir      = CreateButton(root, "btMkDir", "MkDir", 349, 214, 75, 23);

        // Dir/Delete/Root/Abort row
        var btDir    = CreateButton(root, "btDir",    "Dir",    15,  242, 75, 23);
        var btDelete = CreateButton(root, "btDelete", "Delete", 96,  242, 75, 23);
        var btRoot   = CreateButton(root, "btRoot",   "Root",   199, 242, 75, 23);
        var btAbort  = CreateButton(root, "btAbort",  "Abort",  349, 242, 75, 23);

        // laDesc
        var laDesc = CreateLabel(root, "laDesc",
            "Double click to change dir or download file",
            12, 268, 300, 13);

        // lvFiles
        var lvFilesContent = CreateListView(root, "lvFiles", 12, 284, 411, 116,
            new[] { "Object name", "Type", "Size", "Permission", "Modified", "Created", "Accessed", "Description" },
            new float[] { 150, 80, 100, 80, 80, 80, 80, 100 });

        // btClear
        var btClear = CreateButton(root, "btClear", "Clear", 348, 406, 75, 23);

        // lbLog
        var lbLogText = CreateListBox(root, "lbLog", 12, 435, 412, 134);

        // Controller
        var ctrlGO = new GameObject("FtpClientController");
        var ctrl = ctrlGO.AddComponent<FtpClientController>();

        ctrl.btDiscover      = btDiscover.GetComponent<Button>();
        ctrl.btConnect       = btConnect.GetComponent<Button>();
        ctrl.btDisconnect    = btDisconnect.GetComponent<Button>();
        ctrl.btOpenSession   = btOpenSession.GetComponent<Button>();
        ctrl.btCloseSession  = btCloseSession.GetComponent<Button>();
        ctrl.btSelectFile    = btSelectFile.GetComponent<Button>();
        ctrl.btSendFile      = btSendFile.GetComponent<Button>();
        ctrl.btMkDir         = btMkDir.GetComponent<Button>();
        ctrl.btDir           = btDir.GetComponent<Button>();
        ctrl.btDelete        = btDelete.GetComponent<Button>();
        ctrl.btRoot          = btRoot.GetComponent<Button>();
        ctrl.btAbort         = btAbort.GetComponent<Button>();
        ctrl.btClear         = btClear.GetComponent<Button>();

        ctrl.edFileName   = edFileName;
        ctrl.edNewDirName = edNewDirName;

        ctrl.lvDevicesContent = lvDevicesContent;
        ctrl.lvFilesContent   = lvFilesContent;
        ctrl.lbLogText        = lbLogText;
        ctrl.ProgressBarFill  = progressFill;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[FtpClient] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[FtpClient] Scene created and saved: " + ScenePath);
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
        tmp.alignment = TextAlignmentOptions.TopLeft;
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

    private static TMP_InputField CreateInputField(Transform parent, string name, string text,
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
        go.GetComponent<Image>().color = Color.white;

        var textAreaGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var textAreaRT = textAreaGO.GetComponent<RectTransform>();
        textAreaRT.SetParent(go.transform, false);
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(4, 2);
        textAreaRT.offsetMax = new Vector2(-4, -2);

        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
        var placeholderRT = placeholderGO.GetComponent<RectTransform>();
        placeholderRT.SetParent(textAreaGO.transform, false);
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = Vector2.zero;
        placeholderRT.offsetMax = Vector2.zero;
        var placeholderTmp = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = string.Empty;
        placeholderTmp.fontSize = 11;
        placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f);
        placeholderTmp.alignment = TextAlignmentOptions.Left;
        placeholderTmp.enableWordWrapping = false;
        placeholderTmp.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform));
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(textAreaGO.transform, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var textTmp = textGO.AddComponent<TextMeshProUGUI>();
        textTmp.text = text;
        textTmp.fontSize = 11;
        textTmp.color = Color.black;
        textTmp.alignment = TextAlignmentOptions.Left;
        textTmp.enableWordWrapping = false;

        var inputField = go.GetComponent<TMP_InputField>();
        inputField.targetGraphic = go.GetComponent<Image>();
        inputField.textViewport  = textAreaRT;
        inputField.textComponent = textTmp;
        inputField.placeholder   = placeholderTmp;
        inputField.lineType      = TMP_InputField.LineType.SingleLine;
        inputField.text          = text;

        return inputField;
    }

    // ==========================================================
    //  ListView: returns Content RectTransform
    // ==========================================================
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

    // ==========================================================
    //  ListBox: returns TextMeshProUGUI that holds log lines
    // ==========================================================
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

    // ==========================================================
    //  ProgressBar: returns fill Image
    // ==========================================================
    private static Image CreateProgressBar(Transform parent, string name,
        float x, float y, float w, float h)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f);

        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.SetParent(root.transform, false);
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2, 2);
        fillRT.offsetMax = new Vector2(-2, -2);

        var fillImg = fillGO.GetComponent<Image>();
        fillImg.color = new Color(0.3f, 0.7f, 0.3f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 0f;

        return fillImg;
    }
}
#endif