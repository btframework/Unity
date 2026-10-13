#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BluetoothDemoSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string PrefabPath    = PrefabsFolder + "/DeviceRow.prefab";

    [MenuItem("Tools/Bluetooth Demo/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("BluetoothDemoUI");
        DestroyIfExists("BluetoothDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("BluetoothDemoUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var topBar = CreateUI("TopBar", canvasGO.transform);
        var topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot     = new Vector2(0.5f, 1);
        topRect.anchoredPosition = new Vector2(0, -10);
        topRect.sizeDelta = new Vector2(-20, 50);

        var hlg = topBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth  = false;
        hlg.childControlHeight = true;

        Button btnStart   = CreateButton("BtnStart",   "Start",   topBar.transform);
        Button btnStop    = CreateButton("BtnStop",    "Stop",    topBar.transform);
        Button btnOpen    = CreateButton("BtnOpen",    "Open",    topBar.transform);
        Button btnClose   = CreateButton("BtnClose",   "Close",   topBar.transform);
        Button btnConnect = CreateButton("BtnConnect", "Connect", topBar.transform);
        Button btnClear   = CreateButton("BtnClear",   "Clear",   topBar.transform);

        var selectedLabel = CreateText("SelectedDeviceLabel", "Selected: (none)", canvasGO.transform);
        var selRect = selectedLabel.rectTransform;
        selRect.anchorMin = new Vector2(0, 1);
        selRect.anchorMax = new Vector2(1, 1);
        selRect.pivot     = new Vector2(0.5f, 1);
        selRect.anchoredPosition = new Vector2(0, -70);
        selRect.sizeDelta = new Vector2(-20, 28);

        var devicesScroll = CreateScrollView("DevicesScroll", canvasGO.transform);
        var devRect = devicesScroll.GetComponent<RectTransform>();
        devRect.anchorMin = new Vector2(0, 1);
        devRect.anchorMax = new Vector2(1, 1);
        devRect.pivot     = new Vector2(0.5f, 1);
        devRect.anchoredPosition = new Vector2(0, -105);
        devRect.sizeDelta = new Vector2(-20, 250);

        var devicesContent = devicesScroll.transform.Find("Viewport/Content");

        var devVlg = devicesContent.gameObject.AddComponent<VerticalLayoutGroup>();
        devVlg.spacing = 2;
        devVlg.padding = new RectOffset(4, 4, 4, 4);
        devVlg.childForceExpandWidth  = true;
        devVlg.childForceExpandHeight = false;
        devVlg.childControlWidth  = true;
        devVlg.childControlHeight = true;

        var devCsf = devicesContent.gameObject.AddComponent<ContentSizeFitter>();
        devCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var logScroll = CreateScrollView("LogScroll", canvasGO.transform);
        var logRect = logScroll.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.pivot     = new Vector2(0.5f, 0.5f);
        logRect.offsetMin = new Vector2(10, 10);
        logRect.offsetMax = new Vector2(-10, -370);

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
        ltRect.sizeDelta = new Vector2(0, 0);

        var ltCsf = logText.gameObject.AddComponent<ContentSizeFitter>();
        ltCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var deviceRowPrefab = CreateDeviceRowPrefab();

        var demoGO = new GameObject("BluetoothDemo");
        var demo = demoGO.AddComponent<BluetoothAudioReceiverDemo>();

        demo.startButton        = btnStart;
        demo.stopButton         = btnStop;
        demo.openButton         = btnOpen;
        demo.closeButton        = btnClose;
        demo.connectButton      = btnConnect;
        demo.clearButton        = btnClear;
        demo.devicesContent     = devicesContent;
        demo.deviceRowPrefab    = deviceRowPrefab;
        demo.selectedDeviceLabel = selectedLabel;
        demo.logText            = logText;
        demo.logScrollRect      = logScroll.GetComponent<ScrollRect>();

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);
    }

    // ================================================================
    //  Helpers
    // ================================================================

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

    private static Button CreateButton(string name, string label, Transform parent)
    {
        var go = CreateUI(name, parent);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(110, 40);

        var textGO = CreateUI("Text", go.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 22;

        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
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

    private static GameObject CreateScrollView(string name, Transform parent)
    {
        var root = CreateUI(name, parent);

        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        var scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        var viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(1, 1, 1, 0.01f);
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

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

    private static GameObject CreateDeviceRowPrefab()
    {
        if (!Directory.Exists(PrefabsFolder))
            Directory.CreateDirectory(PrefabsFolder);

        var row = CreateUI("DeviceRow", null);

        var img = row.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.05f);

        row.AddComponent<Button>();

        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);

        var le = row.AddComponent<LayoutElement>();
        le.minHeight      = 36;
        le.preferredHeight = 36;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;

        var idText = CreateText("IdText", "", row.transform);
        idText.fontSize = 18;
        var idLe = idText.gameObject.AddComponent<LayoutElement>();
        idLe.preferredWidth = 500;
        idLe.flexibleWidth  = 2;

        var nameText = CreateText("NameText", "", row.transform);
        nameText.fontSize = 18;
        var nameLe = nameText.gameObject.AddComponent<LayoutElement>();
        nameLe.preferredWidth = 300;
        nameLe.flexibleWidth  = 1;

        var rowUI = row.AddComponent<DeviceRowUI>();
        rowUI.idText     = idText;
        rowUI.nameText   = nameText;
        rowUI.background = img;

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, PrefabPath);
        Object.DestroyImmediate(row);

        AssetDatabase.Refresh();
        return prefab;
    }
}
#endif