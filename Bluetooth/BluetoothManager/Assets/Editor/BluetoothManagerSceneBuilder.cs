#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class BluetoothManagerSceneBuilder
{
    private const float FormWidth  = 844f;
    private const float FormHeight = 750f;
    private const string ScenePath = "Assets/Scenes/BluetoothManager.unity";

    [MenuItem("Tools/BluetoothManager/Build Scene")]
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

        // Row 1 (y=12..36)
        var btOpen            = CreateButton(root, "btOpen", "Open", 12, 12, 48, 23);
        var btClose           = CreateButton(root, "btClose", "Close", 66, 12, 48, 23);
        var cbHandlePairing   = CreateCheckBox(root, "cbHandlePairing", "Handle pairing", 120, 16, 94, 17, true);
        var btRefreshRadio    = CreateButton(root, "btRefreshRadio", "Refresh", 220, 12, 52, 23);
        var btSetConnectable  = CreateButton(root, "btSetConnectable", "Connectable", 278, 12, 74, 23);
        var btSetDiscoverable = CreateButton(root, "btSetDiscoverable", "Discoverable", 358, 12, 74, 23);
        var edName            = CreateInputField(root, "edName", "NewRadioName", 438, 14, 118, 20);
        var btSetName         = CreateButton(root, "btSetName", "Set name", 562, 12, 65, 23);
        var btTurnOn          = CreateButton(root, "btTurnOn", "ON", 633, 12, 42, 23);
        var ntTurnOff         = CreateButton(root, "ntTurnOff", "OFF", 681, 12, 42, 23);
        var cbDontTurnOff     = CreateCheckBox(root, "cbDontTurnOff", "Don't turn OFF", 729, 16, 95, 17, false);

        // lvRadios
        var lvRadiosContent = CreateListView(root, "lvRadios", 11, 41, 814, 85,
            new[] { "Driver", "Available", "Address", "Connectable", "Discoverable",
                    "HCI version", "LMP version", "Manufacturer", "Name", "COD" },
            new float[] { 100, 70, 100, 80, 80, 70, 80, 80, 100, 100 });

        // Row 2 (y=132..155)
        var btDiscover          = CreateButton(root, "btDiscover", "Discover", 12, 132, 75, 23);
        var laTimeout           = CreateLabel(root, "laTimeout", "Timeout", 93, 137, 45, 13);
        var edTimeout           = CreateInputField(root, "edTimeout", "10", 144, 134, 38, 20);
        var laDiscoveringMode   = CreateLabel(root, "laDiscoveringMode", "Mode", 188, 137, 34, 13);
        var cbDiscoveringMode   = CreateDropdown(root, "cbDiscoveringMode",
            new[] { "Classic", "BLE" }, 228, 134, 95, 21, 0);
        var btTerminate         = CreateButton(root, "btTerminate", "Terminate", 329, 132, 75, 23);
        var btEnumConnected     = CreateButton(root, "btEnumConnected", "Enum connected", 410, 132, 99, 23);
        var btEnumPaired        = CreateButton(root, "btEnumPaired", "Enum paired", 515, 132, 75, 23);
        var btRssi              = CreateButton(root, "btRssi", "RSSI", 596, 132, 68, 23);
        var btIsInRange         = CreateButton(root, "btIsInRange", "Is in range", 670, 131, 75, 23);
        var btRefreshDevice     = CreateButton(root, "btRefreshDevice", "Refresh", 751, 132, 75, 23);

        // lvDevices
        var lvDevicesContent = CreateListView(root, "lvDevices", 12, 161, 814, 109,
            new[] { "Driver", "Address", "Class Of Device", "Device type",
                    "Name", "Paired", "Connected", "Address type" },
            new float[] { 100, 100, 90, 80, 100, 50, 80, 80 });

        // Row 3 labels/buttons (y=276..321)
        var btEnumServices     = CreateButton(root, "btEnumServices", "Enum services", 12, 276, 106, 23);
        var btPair             = CreateButton(root, "btPair", "Pair", 124, 276, 75, 23);
        var laPin              = CreateLabel(root, "laPin", "PIN", 205, 278, 25, 13);
        var laMitmProtection   = CreateLabel(root, "laMitmProtection", "MITM", 260, 278, 35, 13);
        var laIoCap            = CreateLabel(root, "laIoCap", "IO Cap", 462, 278, 40, 13);
        var laBleProtection    = CreateLabel(root, "laBleProtection", "BLE Protection", 611, 278, 78, 13);
        var laPairingMethod    = CreateLabel(root, "laPairingMethod", "Pairing method", 748, 278, 77, 13);

        // Row 4 inputs (y=296..318)
        var edPIN              = CreateInputField(root, "edPIN", "0000", 208, 297, 49, 20);
        var cbMitmProtection   = CreateDropdown(root, "cbMitmProtection", new[] {
            "mitmProtectionNotRequired", "mitmProtectionRequired",
            "mitmProtectionNotRequiredBonding", "mitmProtectionRequiredBonding",
            "mitmProtectionNotRequiredGeneralBonding", "mitmProtectionRequiredGeneralBonding",
            "mitmProtectionNotDefined"
        }, 263, 296, 195, 21, 3);
        var cbIoCap            = CreateDropdown(root, "cbIoCap", new[] {
            "iocapDisplayOnly", "iocapDisplayYesNo", "iocapKeyboardOnly",
            "iocapNoInputNoOutput", "iocapDisplayKeyboard", "iocapNotDefined"
        }, 464, 297, 144, 21, 1);
        var cbBleProtection    = CreateDropdown(root, "cbBleProtection", new[] {
            "pplDefault", "pplNone", "pplEncryption", "pplEncryptionAndAuthentication"
        }, 614, 297, 131, 21, 0);
        var cbPairingMethod    = CreateDropdown(root, "cbPairingMethod", new[] {
            "pmAuto", "pmClassic", "pmLe"
        }, 751, 297, 75, 21, 0);

        // Row 5 (y=305..328)
        var btUnpair                 = CreateButton(root, "btUnpair", "Unpair", 126, 305, 73, 23);
        var btEnumInstalledServices  = CreateButton(root, "btEnumInstalledServices", "Installed services", 12, 305, 106, 23);

        // lvServices
        var lvServicesContent = CreateListView(root, "lvServices", 12, 334, 814, 109,
            new[] { "Driver", "Address", "Handle", "UUID", "Channel", "Name", "Comment" },
            new float[] { 80, 90, 80, 230, 60, 200, 100 });

        // Row 6 (y=449..472)
        var btCreateVCOM        = CreateButton(root, "btCreateVCOM", "Create vCOM", 12, 449, 86, 23);
        var btDestroyVCOM       = CreateButton(root, "btDestroyVCOM", "Destroy vCOM", 104, 449, 86, 23);
        var btEnumVCOMs         = CreateButton(root, "btEnumVCOMs", "Enum vCOMs", 196, 449, 86, 23);
        var cbEnumPreinstalled  = CreateCheckBox(root, "cbEnumPreinstalled", "Enum preinstalled", 288, 455, 109, 17, false);
        var btIntsallDevice     = CreateButton(root, "btIntsallDevice", "Install device", 423, 449, 93, 23);
        var btUninstallDevice   = CreateButton(root, "btUninstallDevice", "Uninstall device", 522, 449, 98, 23);
        var btDisconnect        = CreateButton(root, "btDisconnect", "Disconnect", 660, 449, 75, 23);

        // lvComPorts
        var lvComPortsContent = CreateListView(root, "lvComPorts", 12, 478, 814, 78,
            new[] { "Driver", "Address", "UUID", "Number" },
            new float[] { 80, 90, 230, 60 });

        // lvEvents
        var lvEventsContent = CreateListView(root, "lvEvents", 12, 572, 814, 137,
            new[] { "Driver", "Event", "Parameter", "Value" },
            new float[] { 100, 220, 220, 220 });

        var btClear = CreateButton(root, "btClear", "Clear", 751, 715, 75, 23);

        var ctrlGO = new GameObject("BluetoothManagerController");
        var ctrl = ctrlGO.AddComponent<BluetoothManagerController>();

        ctrl.btOpen                  = btOpen.GetComponent<Button>();
        ctrl.btClose                 = btClose.GetComponent<Button>();
        ctrl.btRefreshRadio          = btRefreshRadio.GetComponent<Button>();
        ctrl.btSetConnectable        = btSetConnectable.GetComponent<Button>();
        ctrl.btSetDiscoverable       = btSetDiscoverable.GetComponent<Button>();
        ctrl.btSetName               = btSetName.GetComponent<Button>();
        ctrl.ntTurnOff               = ntTurnOff.GetComponent<Button>();
        ctrl.btTurnOn                = btTurnOn.GetComponent<Button>();
        ctrl.btDiscover              = btDiscover.GetComponent<Button>();
        ctrl.btTerminate             = btTerminate.GetComponent<Button>();
        ctrl.btEnumPaired            = btEnumPaired.GetComponent<Button>();
        ctrl.btPair                  = btPair.GetComponent<Button>();
        ctrl.btUnpair                = btUnpair.GetComponent<Button>();
        ctrl.btRssi                  = btRssi.GetComponent<Button>();
        ctrl.btRefreshDevice         = btRefreshDevice.GetComponent<Button>();
        ctrl.btEnumServices          = btEnumServices.GetComponent<Button>();
        ctrl.btClear                 = btClear.GetComponent<Button>();
        ctrl.btCreateVCOM            = btCreateVCOM.GetComponent<Button>();
        ctrl.btDestroyVCOM           = btDestroyVCOM.GetComponent<Button>();
        ctrl.btEnumVCOMs             = btEnumVCOMs.GetComponent<Button>();
        ctrl.btIntsallDevice         = btIntsallDevice.GetComponent<Button>();
        ctrl.btUninstallDevice       = btUninstallDevice.GetComponent<Button>();
        ctrl.btIsInRange             = btIsInRange.GetComponent<Button>();
        ctrl.btDisconnect            = btDisconnect.GetComponent<Button>();
        ctrl.btEnumConnected         = btEnumConnected.GetComponent<Button>();
        ctrl.btEnumInstalledServices = btEnumInstalledServices.GetComponent<Button>();

        ctrl.edName    = edName;
        ctrl.edTimeout = edTimeout;
        ctrl.edPIN     = edPIN;

        ctrl.cbDiscoveringMode = cbDiscoveringMode;
        ctrl.cbMitmProtection  = cbMitmProtection;
        ctrl.cbIoCap           = cbIoCap;
        ctrl.cbBleProtection   = cbBleProtection;
        ctrl.cbPairingMethod   = cbPairingMethod;

        ctrl.cbEnumPreinstalled = cbEnumPreinstalled;
        ctrl.cbHandlePairing    = cbHandlePairing;
        ctrl.cbDontTurnOff      = cbDontTurnOff;

        ctrl.lvRadiosContent   = lvRadiosContent;
        ctrl.lvDevicesContent  = lvDevicesContent;
        ctrl.lvServicesContent = lvServicesContent;
        ctrl.lvEventsContent   = lvEventsContent;
        ctrl.lvComPortsContent = lvComPortsContent;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[BluetoothManager] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[BluetoothManager] Scene created and saved: " + ScenePath);
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
        templateRT.sizeDelta = new Vector2(0, options.Length * 20f + 2f);
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

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(root.transform, false);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1);
        viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        // Content
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

        // Header row
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
}
#endif