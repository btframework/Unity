#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SerialMonitorSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 559 x 496
    private const float CanvasW = 579f;
    private const float CanvasH = 516f;

    [MenuItem("Tools/Serial Monitor/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("SerialMonitorUI");
        DestroyIfExists("SerialMonitorDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("SerialMonitorUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Top row: Start | Stop | Enum serial | Enum USB | Disable | Enable
        // ============================================================
        var btStart      = MakeButton("BtStart",      "Start",       root, 12,  12, 75, 23);
        var btStop       = MakeButton("BtStop",       "Stop",        root, 93,  12, 75, 23);
        var btEnumSerial = MakeButton("BtEnumSerial", "Enum serial", root, 185, 12, 75, 23);
        var btEnumUsb    = MakeButton("BtEnumUsb",    "Enum USB",    root, 266, 12, 75, 23);
        var btDisable    = MakeButton("BtDisable",    "Disable",     root, 391, 12, 75, 23);
        var btEnable     = MakeButton("BtEnable",     "Enable",      root, 472, 12, 75, 23);

        // ============================================================
        //  Devices table (12, 41) 535x182 with two alternate header bars
        //  Serial: 3 columns 80 | 350 | 70
        //  USB:    7 columns 250 | 250 | 50 | 50 | 250 | 200 | 70
        // ============================================================
        var serialWidths = new[] { 80f, 350f, 70f };
        var serialHeaders = new[] { "Device name", "Friendly name", "IsModem" };

        var usbWidths = new[] { 250f, 250f, 50f, 50f, 250f, 200f, 70f };
        var usbHeaders = new[] { "Instance", "Friendly name", "VID", "PID", "Class", "Manufacturer", "Enabled" };

        // --- Serial header ---
        var serialHeader = CreateUI("SerialHeaderBar", root);
        Place(serialHeader, 12, 41, 535, 20);
        var shBg = serialHeader.AddComponent<Image>();
        shBg.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        BuildHeaderChildren(serialHeader.transform, serialWidths, serialHeaders);

        // --- USB header ---
        var usbHeader = CreateUI("UsbHeaderBar", root);
        Place(usbHeader, 12, 41, 535, 20);
        var uhBg = usbHeader.AddComponent<Image>();
        uhBg.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        BuildHeaderChildren(usbHeader.transform, usbWidths, usbHeaders);

        // --- Scroll view (horizontal = true, USB has 7 columns x 1120 px) ---
        var devicesScroll  = MakeScrollView("DevicesScroll", root, 12, 61, 535, 162, true);
        var devicesContent = devicesScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(devicesContent);

        // ============================================================
        //  Clear (472, 229) 75x23
        // ============================================================
        var btClear = MakeButton("BtClear", "Clear", root, 472, 229, 75, 23);

        // ============================================================
        //  Log (12, 258) 535x225
        // ============================================================
        var logScroll  = MakeScrollView("LogScroll", root, 12, 258, 535, 225, false);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent);

        // ============================================================
        //  Prefabs
        // ============================================================
        var serialRow = MakeTableRowPrefab("SerialMonitorSerialRow", serialWidths, 22f);
        var usbRow    = MakeTableRowPrefab("SerialMonitorUsbRow",    usbWidths,    22f);
        var logRow    = MakeTableRowPrefab("SerialMonitorLogRow",    new[] { 525f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("SerialMonitorDemo");
        var demo = demoGO.AddComponent<SerialMonitorDemo>();

        demo.startButton      = btStart;
        demo.stopButton       = btStop;
        demo.enumSerialButton = btEnumSerial;
        demo.enumUsbButton    = btEnumUsb;
        demo.disableButton    = btDisable;
        demo.enableButton     = btEnable;

        demo.serialHeaderBar = serialHeader;
        demo.usbHeaderBar    = usbHeader;

        demo.devicesContent  = devicesContent;
        demo.serialRowPrefab = serialRow;
        demo.usbRowPrefab    = usbRow;

        demo.clearButton = btClear;

        demo.logContent   = logContent;
        demo.logRowPrefab = logRow;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Serial Monitor Demo UI built using the original layout.");
    }

    // ================================================================
    //  Header children builder
    // ================================================================
    private static void BuildHeaderChildren(Transform bar, float[] widths, string[] headers)
    {
        float offset = 4;
        for (int i = 0; i < headers.Length; i++)
        {
            var t = CreateUI("H_" + headers[i], bar);
            var tRect = t.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0);
            tRect.anchorMax = new Vector2(0, 1);
            tRect.pivot     = new Vector2(0, 0.5f);
            tRect.anchoredPosition = new Vector2(offset, 0);
            tRect.sizeDelta = new Vector2(widths[i], 0);

            var tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.text = headers[i];
            tmp.color = Color.white;
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            offset += widths[i];
        }
    }

    // ================================================================
    //  Position
    // ================================================================
    private static void Place(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // ================================================================
    //  Controls
    // ================================================================
    private static Button MakeButton(string name, string label, Transform parent,
        float x, float y, float w, float h)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        c.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = c;

        var textGO = CreateUI("Text", go.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 12;
        text.enableWordWrapping = false;
        StretchFull(textGO);
        return btn;
    }

    private static GameObject MakeScrollView(string name, Transform parent,
        float x, float y, float w, float h, bool horizontal)
    {
        var root = CreateUI(name, parent);
        Place(root, x, y, w, h);

        var rootImg = root.AddComponent<Image>();
        rootImg.color = Color.white;

        var scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = horizontal;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        StretchFull(viewport);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 1);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        if (horizontal)
        {
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(0, 1);
            cRect.pivot     = new Vector2(0, 1);
        }
        else
        {
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot     = new Vector2(0.5f, 1);
        }
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = Vector2.zero;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content  = cRect;

        return root;
    }

    private static void AddVerticalListGroup(Transform content)
    {
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = false;   // VLG does not touch width
        vlg.childControlHeight = false;   // VLG does not touch height
        vlg.childAlignment = TextAnchor.UpperLeft;

        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static GameObject MakeTableRowPrefab(string name, float[] widths, float rowHeight)
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);

        float totalWidth = 8;
        for (int i = 0; i < widths.Length; i++) totalWidth += widths[i];

        rt.sizeDelta = new Vector2(totalWidth, rowHeight);   // <-- set the actual width here

        var img = root.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.03f);
        root.AddComponent<Button>();

        float offset = 4;
        var cells = new TMP_Text[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            var cellGO = CreateUI("Cell" + i, root.transform);
            var cr = cellGO.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0, 0);
            cr.anchorMax = new Vector2(0, 1);
            cr.pivot     = new Vector2(0, 0.5f);
            cr.anchoredPosition = new Vector2(offset, 0);
            cr.sizeDelta = new Vector2(widths[i], 0);

            var t = cellGO.AddComponent<TextMeshProUGUI>();
            t.text = "";
            t.color = Color.black;
            t.fontSize = 11;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;

            cells[i] = t;
            offset += widths[i];
        }

        var ui = root.AddComponent<TableRowUI>();
        ui.cells = cells;
        ui.background = img;
        ui.normalColor   = new Color(0, 0, 0, 0.03f);
        ui.selectedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabsFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ================================================================
    //  Misc
    // ================================================================
    private static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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