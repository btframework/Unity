#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GattServerSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    private static readonly string[] ParamsOptions = new[]
    {
        "Balanced", "PowerOptimized", "ThroughputOptimized", "Custom"
    };

    [MenuItem("Tools/GATT Server/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("GattServerUI");
        DestroyIfExists("GattServerDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("GattServerUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Row 1: Start | Stop | Disconnect | Params + SetParameters
        // ============================================================
        var r1 = MakeHBar("Row1", canvasGO.transform, 20, 40);
        var btStart     = MakeButton("BtStart", "Start", r1.transform, 120, 40);
        var btStop      = MakeButton("BtStop",  "Stop",  r1.transform, 120, 40);
        var btDisconnect = MakeButton("BtDisconnect", "Disconnect", r1.transform, 160, 40);
        MakeLabel("Params", r1.transform, 100, 40);
        var ddParams    = MakeDropdown("DdParams", r1.transform, 260, 40, ParamsOptions);
        var btSetParams = MakeButton("BtSetParams", "Set parameters", r1.transform, 200, 40);

        // ============================================================
        //  Clients tree (13 columns)
        // ============================================================
        var widths = new[]
        {
            200f, 240f, 120f, 100f, 100f, 100f,
            110f, 100f, 100f, 110f, 100f, 100f,
            140f
        };
        var headers = new[]
        {
            "Client", "Subscribed characteristics", "Max PDU", "Interval", "Latency", "Timeout",
            "RX coded", "RX 1M", "RX 2M", "TX coded", "TX 1M", "TX 2M",
            "Max notification"
        };

        // Headers
        var headerBar = CreateUI("ClientsHeader", canvasGO.transform);
        var hRect = headerBar.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0, 1); hRect.anchorMax = new Vector2(1, 1);
        hRect.pivot = new Vector2(0.5f, 1);
        hRect.anchoredPosition = new Vector2(0, -96);
        hRect.sizeDelta = new Vector2(-40, 24);

        var hHlg = headerBar.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing = 4;
        hHlg.childForceExpandWidth = false; hHlg.childForceExpandHeight = true;
        hHlg.childControlWidth = true; hHlg.childControlHeight = true;

        float totalWidth = 0;
        for (int i = 0; i < headers.Length; i++)
        {
            var t = CreateText("H_" + headers[i], headers[i], headerBar.transform);
            t.fontStyle = FontStyles.Bold;
            t.fontSize = 14;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = widths[i]; le.minWidth = widths[i];
            totalWidth += widths[i] + 4;
        }

        // Scroll
        var clientsScroll = MakeScrollView("ClientsScroll", canvasGO.transform);
        var csr = clientsScroll.GetComponent<RectTransform>();
        csr.anchorMin = new Vector2(0, 1); csr.anchorMax = new Vector2(1, 1);
        csr.pivot = new Vector2(0.5f, 1);
        csr.anchoredPosition = new Vector2(0, -124);
        csr.sizeDelta = new Vector2(-40, 200);

        // Горизонтальный скролл
        var clientsSr = clientsScroll.GetComponent<ScrollRect>();
        clientsSr.horizontal = true;

        var clientsContent = clientsScroll.transform.Find("Viewport/Content");
        var ccRect = clientsContent.GetComponent<RectTransform>();
        ccRect.anchorMin = new Vector2(0, 1);
        ccRect.anchorMax = new Vector2(0, 1);
        ccRect.pivot = new Vector2(0, 1);
        ccRect.anchoredPosition = Vector2.zero;
        ccRect.sizeDelta = new Vector2(totalWidth, 0);

        var ccVlg = clientsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        ccVlg.spacing = 2;
        ccVlg.padding = new RectOffset(4, 4, 4, 4);
        ccVlg.childForceExpandWidth = false;
        ccVlg.childForceExpandHeight = false;
        ccVlg.childControlWidth = false;
        ccVlg.childControlHeight = true;
        var ccCsf = clientsContent.gameObject.AddComponent<ContentSizeFitter>();
        ccCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ============================================================
        //  Notify row
        // ============================================================
        var r2 = MakeHBar("Row2", canvasGO.transform, 340, 40);
        MakeLabel("Data", r2.transform, 80, 40);
        var edData = MakeInputField("EdData", r2.transform, 600, 40, "10FF20FF");
        MakeLabel("Repeats", r2.transform, 100, 40);
        var edRepeats = MakeInputField("EdRepeats", r2.transform, 120, 40, "10");
        var btNotify    = MakeButton("BtNotify", "Notify", r2.transform, 140, 40);
        var btNotifyAll = MakeButton("BtNotifyAll", "Notify All", r2.transform, 160, 40);

        // ============================================================
        //  Log
        // ============================================================
        var logScroll = MakeScrollView("LogScroll", canvasGO.transform);
        var lr = logScroll.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0);
        lr.anchorMax = new Vector2(1, 1);
        lr.offsetMin = new Vector2(20, 70);
        lr.offsetMax = new Vector2(-20, -410);

        var logContent = logScroll.transform.Find("Viewport/Content");
        var logVlg = logContent.gameObject.AddComponent<VerticalLayoutGroup>();
        logVlg.spacing = 1;
        logVlg.childForceExpandWidth = true; logVlg.childForceExpandHeight = false;
        logVlg.childControlWidth = true; logVlg.childControlHeight = true;
        var logCsf = logContent.gameObject.AddComponent<ContentSizeFitter>();
        logCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var clearLog = MakeButton("BtClearLog", "Clear", canvasGO.transform, 120, 40);
        var clRect = clearLog.GetComponent<RectTransform>();
        clRect.anchorMin = new Vector2(1, 0); clRect.anchorMax = new Vector2(1, 0);
        clRect.pivot = new Vector2(1, 0);
        clRect.anchoredPosition = new Vector2(-20, 20);

        // ============================================================
        //  Prefabs
        // ============================================================
        var clientNodePrefab = MakeClientNodePrefab(widths);
        var clientRowPrefab  = MakeTableRowPrefab("GattServerClientRow", widths);
        var subRowPrefab     = MakeTableRowPrefab("GattServerSubRow", widths);
        var logRowPrefab     = MakeTableRowPrefab("GattServerLogRow", new[] { 1800f });

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("GattServerDemo");
        var demo = demoGO.AddComponent<GattServerDemo>();

        demo.startButton       = btStart;
        demo.stopButton        = btStop;
        demo.disconnectButton  = btDisconnect;

        demo.clientsContent    = clientsContent;
        demo.clientNodePrefab  = clientNodePrefab;
        demo.clientRowPrefab   = clientRowPrefab;
        demo.subscribedRowPrefab = subRowPrefab;

        demo.dataInput         = edData;
        demo.repeatsInput      = edRepeats;
        demo.notifyButton      = btNotify;
        demo.notifyAllButton   = btNotifyAll;

        demo.paramsDropdown       = ddParams;
        demo.setParametersButton  = btSetParams;

        demo.logContent     = logContent;
        demo.logRowPrefab   = logRowPrefab;
        demo.clearLogButton = clearLog;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? GATT Server Demo UI собрана. Нажми Play.");
    }

    // ================================================================
    //  Prefabs
    // ================================================================
    private static GameObject MakeClientNodePrefab(float[] widths)
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI("GattServerClientNode", null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 0);

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = false;
        vlg.childControlHeight = true;

        var csf = root.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        float totalWidth = 0;
        for (int i = 0; i < widths.Length; i++) totalWidth += widths[i] + 4;

        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth = totalWidth;
        le.minWidth = totalWidth;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabsFolder + "/GattServerClientNode.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    private static GameObject MakeTableRowPrefab(string name, float[] widths)
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 26);

        var img = root.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);
        root.AddComponent<Button>();

        float totalWidth = 0;
        for (int i = 0; i < widths.Length; i++) totalWidth += widths[i] + 4;

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 26; le.preferredHeight = 26;
        le.preferredWidth = totalWidth; le.minWidth = totalWidth;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;

        var cells = new TMP_Text[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            var t = CreateText("Cell" + i, "", root.transform);
            t.fontSize = 14;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var l = t.gameObject.AddComponent<LayoutElement>();
            l.preferredWidth = widths[i]; l.minWidth = widths[i];
            cells[i] = t;
        }

        var ui = root.AddComponent<TableRowUI>();
        ui.cells = cells;
        ui.background = img;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabsFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ================================================================
    //  UI helpers
    // ================================================================
    private static GameObject MakeHBar(string name, Transform parent, float y, float h)
    {
        var bar = CreateUI(name, parent);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -y);
        rt.sizeDelta = new Vector2(-40, h);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        return bar;
    }

    private static Button MakeButton(string name, string label, Transform parent, float w, float h)
    {
        var go = CreateUI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f);
        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        c.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = c;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var tGO = CreateUI("Text", go.transform);
        var t = tGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.color = Color.white; t.fontSize = 18;
        t.alignment = TextAlignmentOptions.Center;
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return btn;
    }

    private static TMP_Text MakeLabel(string text, Transform parent, float w, float h)
    {
        var t = CreateText(text, text, parent);
        t.fontSize = 18;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;
        return t;
    }

    private static TextMeshProUGUI CreateText(string name, string content, Transform parent)
    {
        var go = CreateUI(name, parent);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.color = Color.white;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private static TMP_InputField MakeInputField(string name, Transform parent, float w, float h, string defaultText)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f);

        var ta = CreateUI("Text Area", go.transform);
        var taRect = ta.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero; taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(6, 4); taRect.offsetMax = new Vector2(-6, -4);

        var txt = ta.AddComponent<TextMeshProUGUI>();
        txt.color = Color.white; txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        var ph = CreateUI("Placeholder", ta.transform);
        var phRect = ph.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero; phRect.offsetMax = Vector2.zero;
        var phT = ph.AddComponent<TextMeshProUGUI>();
        phT.text = defaultText; phT.color = new Color(1, 1, 1, 0.4f); phT.fontSize = 18;
        phT.alignment = TextAlignmentOptions.MidlineLeft;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = txt;
        input.placeholder = phT;
        input.targetGraphic = bg;
        input.text = defaultText;
        return input;
    }

    private static TMP_Dropdown MakeDropdown(string name, Transform parent, float w, float h, string[] opts)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.22f, 0.22f);
        var dd = go.AddComponent<TMP_Dropdown>();

        var labelGO = CreateUI("Label", go.transform);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = opts.Length > 0 ? opts[0] : "";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white; label.fontSize = 16;
        var lRect = label.rectTransform;
        lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
        lRect.offsetMin = new Vector2(10, 0); lRect.offsetMax = new Vector2(-25, 0);

        var arrow = CreateUI("Arrow", go.transform);
        var arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = new Color(0.7f, 0.7f, 0.7f);
        var aRect = arrow.GetComponent<RectTransform>();
        aRect.anchorMin = new Vector2(1, 0.5f); aRect.anchorMax = new Vector2(1, 0.5f);
        aRect.pivot = new Vector2(1, 0.5f);
        aRect.anchoredPosition = new Vector2(-8, 0);
        aRect.sizeDelta = new Vector2(12, 12);

        var template = CreateUI("Template", go.transform);
        var tRect = template.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0, 0); tRect.anchorMax = new Vector2(1, 0);
        tRect.pivot = new Vector2(0.5f, 1);
        tRect.anchoredPosition = new Vector2(0, 2);
        tRect.sizeDelta = new Vector2(0, 300);

        var tImg = template.AddComponent<Image>();
        tImg.color = new Color(0.15f, 0.15f, 0.15f);
        var tScroll = template.AddComponent<ScrollRect>();
        tScroll.horizontal = false;
        var tMask = template.AddComponent<Mask>();
        tMask.showMaskGraphic = true;

        var viewport = CreateUI("Viewport", template.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 28);

        var item = CreateUI("Item", content.transform);
        var iRect = item.GetComponent<RectTransform>();
        iRect.anchorMin = new Vector2(0, 0.5f); iRect.anchorMax = new Vector2(1, 0.5f);
        iRect.pivot = new Vector2(0.5f, 0.5f);
        iRect.anchoredPosition = Vector2.zero;
        iRect.sizeDelta = new Vector2(0, 28);

        var itemToggle = item.AddComponent<Toggle>();

        var itemBg = CreateUI("Item Background", item.transform);
        var ibRect = itemBg.GetComponent<RectTransform>();
        ibRect.anchorMin = Vector2.zero; ibRect.anchorMax = Vector2.one;
        ibRect.offsetMin = Vector2.zero; ibRect.offsetMax = Vector2.zero;
        var ibImg = itemBg.AddComponent<Image>();
        ibImg.color = new Color(0.2f, 0.2f, 0.2f);

        var itemCheck = CreateUI("Item Checkmark", item.transform);
        var icRect = itemCheck.GetComponent<RectTransform>();
        icRect.anchorMin = new Vector2(0, 0.5f); icRect.anchorMax = new Vector2(0, 0.5f);
        icRect.pivot = new Vector2(0, 0.5f);
        icRect.anchoredPosition = new Vector2(10, 0);
        icRect.sizeDelta = new Vector2(14, 14);
        var icImg = itemCheck.AddComponent<Image>();
        icImg.color = new Color(0.4f, 0.7f, 1f);

        var itemLabel = CreateUI("Item Label", item.transform);
        var ilRect = itemLabel.GetComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero; ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(28, 0); ilRect.offsetMax = new Vector2(-10, 0);
        var ilText = itemLabel.AddComponent<TextMeshProUGUI>();
        ilText.text = "Option"; ilText.color = Color.white; ilText.fontSize = 16;
        ilText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = ibImg;
        itemToggle.graphic = icImg;

        tScroll.viewport = vpRect;
        tScroll.content = cRect;

        dd.template = tRect;
        dd.captionText = label;
        dd.itemText = ilText;

        dd.options.Clear();
        foreach (var o in opts) dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();

        template.SetActive(false);
        return dd;
    }

    private static GameObject MakeScrollView(string name, Transform parent)
    {
        var root = CreateUI(name, parent);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        var scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = Vector2.zero;

        scroll.viewport = vpRect;
        scroll.content = cRect;
        return root;
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