#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class WiFiDirectClientSceneBuilder
{
    private const float FormWidth  = 413f;
    private const float FormHeight = 472f;
    private const string ScenePath = "Assets/Scenes/WiFiDirectClient.unity";

    [MenuItem("Tools/WiFiDirectClient/Build Scene")]
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
        var btDiscover   = CreateButton(root, "btDiscover",   "Discover",   12,  12, 58, 23);
        var btTerminate  = CreateButton(root, "btTerminate",  "Terminate",  76,  12, 62, 23);
        var btEnumPaired = CreateButton(root, "btEnumPaired", "Enum paired",144, 12, 75, 23);
        var btIsPaired   = CreateButton(root, "btIsPaired",   "Is Paired",  225, 12, 56, 23);
        var btUnpair     = CreateButton(root, "btUnpair",     "Unpair",     287, 12, 47, 23);

        // lvDevices
        var lvDevicesContent = CreateListView(root, "lvDevices", 12, 41, 390, 115,
            new[] { "Name", "ID" },
            new float[] { 150f, 200f });

        // Row 3 - Group owner intent / pairing params
        CreateLabel(root, "laGroupOwnerIntent", "Group owner intent", 12, 165, 97, 13);
        var edGroupOwnerIntent = CreateInputField(root, "edGroupOwnerIntent", "14", 115, 162, 53, 20);

        CreateLabel(root, "laPairingProcedure", "Pairing procedure", 12, 191, 90, 13);
        var cbPairingProcedure = CreateDropdown(root, "cbPairingProcedure",
            new[] { "ppInvitation", "ppGroupOwnerNegotiation" }, 115, 188, 132, 21, 0);

        var cbDisplayPIN  = CreateCheckBox(root, "cbDisplayPIN",  "Display PIN",  321, 161, 81, 17, false, true);
        var cbProvidePIN  = CreateCheckBox(root, "cbProvidePIN",  "Provide PIN",  319, 184, 83, 17, false, true);
        var cbPushButton  = CreateCheckBox(root, "cbPushButton",  "Push Button",  318, 207, 84, 17, true,  true);

        // Row 4 - Connect / Disconnect / Clear
        var btConnect    = CreateButton(root, "btConnect",    "Connect",    15,  230, 75, 23);
        var btDisconnect = CreateButton(root, "btDisconnect", "Disconnect", 96,  230, 75, 23);
        var btClear      = CreateButton(root, "btClear",      "Clear",      327, 230, 75, 23);

        // lbLog
        var lbLogText = CreateListBox(root, "lbLog", 12, 259, 390, 199);

        // Controller
        var ctrlGO = new GameObject("WiFiDirectClientController");
        var ctrl = ctrlGO.AddComponent<WiFiDirectClientController>();

        ctrl.btDiscover   = btDiscover.GetComponent<Button>();
        ctrl.btTerminate  = btTerminate.GetComponent<Button>();
        ctrl.btEnumPaired = btEnumPaired.GetComponent<Button>();
        ctrl.btIsPaired   = btIsPaired.GetComponent<Button>();
        ctrl.btUnpair     = btUnpair.GetComponent<Button>();
        ctrl.btConnect    = btConnect.GetComponent<Button>();
        ctrl.btDisconnect = btDisconnect.GetComponent<Button>();
        ctrl.btClear      = btClear.GetComponent<Button>();

        ctrl.cbDisplayPIN     = cbDisplayPIN;
        ctrl.cbProvidePIN     = cbProvidePIN;
        ctrl.cbPushButton     = cbPushButton;
        ctrl.cbPairingProcedure = cbPairingProcedure;

        ctrl.edGroupOwnerIntent = edGroupOwnerIntent;

        ctrl.lvDevicesContent = lvDevicesContent;
        ctrl.lbLogText        = lbLogText;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[WiFiDirectClient] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[WiFiDirectClient] Scene created and saved: " + ScenePath);
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

    private static Toggle CreateCheckBox(Transform parent, string name, string label,
        float x, float y, float w, float h, bool isOn, bool boxOnRight = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);

        var boxGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
        var boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.SetParent(go.transform, false);
        if (boxOnRight)
        {
            boxRT.anchorMin = new Vector2(1, 0.5f); boxRT.anchorMax = new Vector2(1, 0.5f); boxRT.pivot = new Vector2(1, 0.5f);
        }
        else
        {
            boxRT.anchorMin = new Vector2(0, 0.5f); boxRT.anchorMax = new Vector2(0, 0.5f); boxRT.pivot = new Vector2(0, 0.5f);
        }
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
        if (boxOnRight)
        {
            labelRT.offsetMin = new Vector2(0, 0); labelRT.offsetMax = new Vector2(-17, 0);
        }
        else
        {
            labelRT.offsetMin = new Vector2(17, 0); labelRT.offsetMax = Vector2.zero;
        }
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label; labelTmp.fontSize = 11; labelTmp.color = Color.black;
        labelTmp.alignment = boxOnRight ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        labelTmp.enableWordWrapping = false; labelTmp.raycastTarget = false;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = boxImg; toggle.graphic = checkImg; toggle.isOn = isOn;
        return toggle;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, string[] options,
        float x, float y, float w, float h, int selectedIndex)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = Color.white;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(4, 0); labelRT.offsetMax = new Vector2(-18, 0);
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.fontSize = 11; labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color = Color.black; labelTmp.raycastTarget = false;

        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f); arrowRT.anchorMax = new Vector2(1, 0.5f); arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-4, 0); arrowRT.sizeDelta = new Vector2(10, 10);
        arrowGO.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);

        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Canvas), typeof(GraphicRaycaster));
        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.SetParent(go.transform, false);
        templateRT.anchorMin = new Vector2(0, 0); templateRT.anchorMax = new Vector2(1, 0); templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, options.Length * 20f + 2f);
        templateGO.GetComponent<Image>().color = Color.white;
        var tc = templateGO.GetComponent<Canvas>(); tc.overrideSorting = true; tc.sortingOrder = 30000;
        templateGO.SetActive(false);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1); contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 1); itemRT.anchorMax = new Vector2(1, 1); itemRT.pivot = new Vector2(0.5f, 1);
        itemRT.anchoredPosition = Vector2.zero; itemRT.sizeDelta = new Vector2(0, 20);
        var itemImg = itemGO.GetComponent<Image>(); itemImg.color = Color.white;
        var itemToggle = itemGO.GetComponent<Toggle>(); itemToggle.targetGraphic = itemImg;
        var itemLe = itemGO.AddComponent<LayoutElement>(); itemLe.preferredHeight = 20;

        var itemBgGO = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBgGO.transform.SetParent(itemGO.transform, false);
        var itemBgRT = itemBgGO.GetComponent<RectTransform>();
        itemBgRT.anchorMin = Vector2.zero; itemBgRT.anchorMax = Vector2.one;
        itemBgRT.offsetMin = Vector2.zero; itemBgRT.offsetMax = Vector2.zero;
        itemBgGO.GetComponent<Image>().color = Color.white;

        var itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero; itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(6, 0); itemLabelRT.offsetMax = new Vector2(-6, 0);
        var itemLabelTmp = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelTmp.fontSize = 11; itemLabelTmp.alignment = TextAlignmentOptions.Left; itemLabelTmp.color = Color.black;

        var sr = templateGO.GetComponent<ScrollRect>();
        sr.content = contentRT; sr.viewport = viewportRT; sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = go.GetComponent<Image>();
        dropdown.template = templateRT;
        dropdown.captionText = labelTmp;
        dropdown.itemText = itemLabelTmp;
        dropdown.itemImage = itemBgGO.GetComponent<Image>();

        dropdown.options.Clear();
        foreach (var opt in options) dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
        dropdown.value = selectedIndex;
        dropdown.RefreshShownValue();
        return dropdown;
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

    private static TextMeshProUGUI CreateListBox(Transform parent, string name, float x, float y, float w, float h)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        var sr = root.GetComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(root.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1); viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1); contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = Vector2.zero;

        var tmp = contentGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 11; tmp.color = Color.black; tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false; tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(2, 2, 2, 2);

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = contentRT; sr.viewport = viewportRT;

        return tmp;
    }
}
#endif