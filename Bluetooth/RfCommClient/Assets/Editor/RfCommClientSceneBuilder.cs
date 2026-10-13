#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RfCommClientSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // WinForms ClientSize = 528 ? 592. Берём чуть больше — чтобы форма
    // выглядела как окно, с запасом 20 px со всех сторон.
    private const float CanvasW = 560f;
    private const float CanvasH = 620f;

    [MenuItem("Tools/RFCOMM Client/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("RfCommClientUI");
        DestroyIfExists("RfCommClientDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // ---------- Canvas ----------
        var canvasGO = CreateUI("RfCommClientUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Top row  (Designer.cs)
        //  btDiscover    (12, 12)  75?23
        //  cbEnumPaired  (93, 16)  85?17
        //  btPair        (248, 12) 75?23
        //  btUnpair      (329, 12) 75?23
        // ============================================================
        Button btDiscover   = MakeButton("BtDiscover",   "Discover",    canvasGO.transform, 12,  12,  75, 23);
        Toggle tgEnumPaired = MakeToggle("TgEnumPaired", "Enum paired", canvasGO.transform, 93,  14,  85, 17);
        Button btPair       = MakeButton("BtPair",       "Pair",        canvasGO.transform, 248, 12,  75, 23);
        Button btUnpair     = MakeButton("BtUnpair",     "Unpair",      canvasGO.transform, 329, 12,  75, 23);

        // ============================================================
        //  Devices table
        //  lvDevices (12, 41) 505?93, колонки 150 | 150 | 150
        //  Header: 20 px, content: 73 px
        // ============================================================
        MakeHeaderBar("DevicesHeader", canvasGO.transform, 12, 41, 505, 20,
            new[] { 150f, 150f, 150f },
            new[] { "Address", "Name", "Device type" });

        var devicesScroll  = MakeScrollView("DevicesScroll", canvasGO.transform, 12, 61, 505, 73);
        var devicesContent = devicesScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(devicesContent);

        // ============================================================
        //  Service name row
        //  cbServiceName (12, 142) 111?17
        //  edServiceName (129, 140) 388?20
        // ============================================================
        Toggle tgSvcName = MakeToggle("TgSvcName", "Use service name",
            canvasGO.transform, 12, 140, 111, 17);
        TMP_InputField edSvcName = MakeInputField("EdSvcName",
            canvasGO.transform, 129, 140, 388, 20, "Bluetooth Framework server");

        // ============================================================
        //  Auth / Encryption / Timeout / Connect / Disconnect
        //  cbAuthentication (12, 170)
        //  cbEncryption    (112, 170)
        //  laTimeout       (214, 169)  "Timeout"
        //  edTimeout       (265, 166)  58?20  "10000"
        //  laMs            (329, 169)  "(ms)"
        //  btConnect       (361, 164) 75?23
        //  btDisconnect    (442, 164) 75?23
        // ============================================================
        Toggle tgAuth = MakeToggle("TgAuth", "Authentication", canvasGO.transform, 12,  168, 94, 17);
        Toggle tgEnc  = MakeToggle("TgEnc",  "Encryption",     canvasGO.transform, 112, 168, 76, 17);

        MakeLabel("LaTimeout", "Timeout", canvasGO.transform, 214, 167, 45, 13);
        TMP_InputField edTimeout = MakeInputField("EdTimeout",
            canvasGO.transform, 265, 166, 58, 20, "10000");
        MakeLabel("LaMs", "(ms)", canvasGO.transform, 329, 167, 26, 13);

        Button btConnect    = MakeButton("BtConnect",    "Connect",    canvasGO.transform, 361, 164, 75, 23);
        Button btDisconnect = MakeButton("BtDisconnect", "Disconnect", canvasGO.transform, 442, 164, 75, 23);

        // ============================================================
        //  Text to send
        //  edText (12, 195) 424?20
        //  btSend (442, 193) 75?23
        // ============================================================
        TMP_InputField edText = MakeInputField("EdText",
            canvasGO.transform, 12, 195, 424, 20, "Send this text to selected client");
        Button btSend = MakeButton("BtSend", "Send", canvasGO.transform, 442, 193, 75, 23);

        // ============================================================
        //  Buffers
        //  btGetBuffers  (12, 222) 75?23
        //  laReadBuffer  (93, 227)  "Read buffer"
        //  edReadBuffer  (169, 222) 100?20
        //  laWriteBuffer (275, 227) "Write buffer"
        //  edWriteBuffer (336, 224) 100?20
        //  btWriteBuffers (442, 222) 75?23
        // ============================================================
        Button btGetBuffers = MakeButton("BtGetBuffers", "Get buffers",
            canvasGO.transform, 12, 222, 75, 23);
        MakeLabel("LaReadBuffer", "Read buffer", canvasGO.transform, 93, 225, 63, 13);
        TMP_InputField edReadBuffer = MakeInputField("EdReadBuffer",
            canvasGO.transform, 169, 222, 100, 20, "0");
        MakeLabel("LaWriteBuffer", "Write buffer", canvasGO.transform, 275, 225, 62, 13);
        TMP_InputField edWriteBuffer = MakeInputField("EdWriteBuffer",
            canvasGO.transform, 336, 224, 100, 20, "0");
        Button btWriteBuffers = MakeButton("BtWriteBuffers", "Set buffers",
            canvasGO.transform, 442, 222, 75, 23);

        // ============================================================
        //  Events log
        //  lbEvents (12, 252) 505?290
        //  btClear  (442, 557) 75?23
        // ============================================================
        var eventsScroll  = MakeScrollView("EventsScroll", canvasGO.transform, 12, 252, 505, 290);
        var eventsContent = eventsScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(eventsContent);

        Button btClear = MakeButton("BtClear", "Clear", canvasGO.transform, 442, 557, 75, 23);

        // ============================================================
        //  Prefabs
        // ============================================================
        var deviceRow = MakeTableRowPrefab("RfCommDeviceRow",
            new[] { 150f, 150f, 150f }, 22f);
        var eventRow  = MakeTableRowPrefab("RfCommEventRow",
            new[] { 500f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("RfCommClientDemo");
        var demo = demoGO.AddComponent<RfCommClientDemo>();

        demo.discoverButton   = btDiscover;
        demo.enumPairedToggle = tgEnumPaired;
        demo.pairButton       = btPair;
        demo.unpairButton     = btUnpair;

        demo.devicesContent  = devicesContent;
        demo.deviceRowPrefab = deviceRow;

        demo.authenticationToggle = tgAuth;
        demo.encryptionToggle     = tgEnc;
        demo.timeoutInput         = edTimeout;
        demo.serviceNameToggle    = tgSvcName;
        demo.serviceNameInput     = edSvcName;
        demo.connectButton        = btConnect;
        demo.disconnectButton     = btDisconnect;

        demo.textInput  = edText;
        demo.sendButton = btSend;

        demo.readBufferInput    = edReadBuffer;
        demo.writeBufferInput   = edWriteBuffer;
        demo.writeBuffersButton = btWriteBuffers;
        demo.getBuffersButton   = btGetBuffers;

        demo.eventsContent     = eventsContent;
        demo.eventRowPrefab    = eventRow;
        demo.clearEventsButton = btClear;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? RFCOMM Client Demo UI собрана по оригинальному layout'у.");
    }

    // ================================================================
    //  Positioning helper — все контролы абсолютно от левого верхнего угла
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
    //  Basic controls
    // ================================================================
    private static Button MakeButton(string name, string label, Transform parent,
        float x, float y, float w, float h)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;

        var textGO = CreateUI("Text", go.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 12;
        StretchFull(textGO);

        return btn;
    }

    private static Toggle MakeToggle(string name, string label, Transform parent,
        float x, float y, float w, float h)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var toggle = go.AddComponent<Toggle>();

        // Background (квадратик слева)
        var bg = CreateUI("Background", go.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot     = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(0, 0);
        bgRect.sizeDelta = new Vector2(13, 13);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Checkmark
        var cm = CreateUI("Checkmark", bg.transform);
        var cmRect = cm.GetComponent<RectTransform>();
        cmRect.anchorMin = new Vector2(0.5f, 0.5f);
        cmRect.anchorMax = new Vector2(0.5f, 0.5f);
        cmRect.pivot     = new Vector2(0.5f, 0.5f);
        cmRect.anchoredPosition = Vector2.zero;
        cmRect.sizeDelta = new Vector2(9, 9);
        var cmImg = cm.AddComponent<Image>();
        cmImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);

        // Label
        var lbl = CreateUI("Label", go.transform);
        var lblRect = lbl.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(1, 1);
        lblRect.pivot     = new Vector2(0.5f, 0.5f);
        lblRect.offsetMin = new Vector2(17, 0);
        lblRect.offsetMax = new Vector2(0, 0);
        var lblText = lbl.AddComponent<TextMeshProUGUI>();
        lblText.text = label;
        lblText.color = Color.white;
        lblText.fontSize = 12;
        lblText.alignment = TextAlignmentOptions.MidlineLeft;
        lblText.overflowMode = TextOverflowModes.Overflow;
        lblText.enableWordWrapping = false;

        toggle.targetGraphic = bgImg;
        toggle.graphic       = cmImg;

        return toggle;
    }

    private static TMP_Text MakeLabel(string name, string text, Transform parent,
        float x, float y, float w, float h)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.color = Color.white;
        t.fontSize = 12;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.overflowMode = TextOverflowModes.Overflow;
        t.enableWordWrapping = false;

        return t;
    }

    private static TMP_InputField MakeInputField(string name, Transform parent,
        float x, float y, float w, float h, string defaultText)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 1f);

        var textArea = CreateUI("Text Area", go.transform);
        var taRect = textArea.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(3, 2);
        taRect.offsetMax = new Vector2(-3, -2);

        var txt = textArea.AddComponent<TextMeshProUGUI>();
        txt.color = Color.black;
        txt.fontSize = 12;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.overflowMode = TextOverflowModes.Overflow;
        txt.enableWordWrapping = false;

        var phGO = CreateUI("Placeholder", textArea.transform);
        var phRect = phGO.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text = "";
        ph.color = new Color(0, 0, 0, 0.4f);
        ph.fontSize = 12;
        ph.alignment = TextAlignmentOptions.MidlineLeft;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport  = taRect;
        input.textComponent = txt;
        input.placeholder   = ph;
        input.targetGraphic = bg;
        input.text = defaultText;

        return input;
    }

    // ================================================================
    //  Header bar — одна строка с фиксированными колонками
    // ================================================================
    private static void MakeHeaderBar(string name, Transform parent,
        float x, float y, float w, float h, float[] widths, string[] headers)
    {
        var bar = CreateUI(name, parent);
        Place(bar, x, y, w, h);

        var bg = bar.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        float offset = 4;
        for (int i = 0; i < headers.Length; i++)
        {
            var t = CreateUI("H_" + headers[i], bar.transform);
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
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.enableWordWrapping = false;

            offset += widths[i];
        }
    }

    // ================================================================
    //  ScrollView с абсолютной позицией
    // ================================================================
    private static GameObject MakeScrollView(string name, Transform parent,
        float x, float y, float w, float h)
    {
        var root = CreateUI(name, parent);
        Place(root, x, y, w, h);

        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(1f, 1f, 1f, 1f);

        var scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        StretchFull(viewport);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.001f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot     = new Vector2(0.5f, 1);
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
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = true;

        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ================================================================
    //  Table row prefab
    // ================================================================
    private static GameObject MakeTableRowPrefab(string name, float[] widths, float rowHeight)
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, rowHeight);

        var img = root.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);
        root.AddComponent<Button>();

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;

        // Фон-подсветка (для выделения) — тот же Image, но перекрашиваемый TableRowUI.
        // Накладываем строки абсолютно.
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
            t.color = Color.white;
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

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabsFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ================================================================
    //  Misc helpers
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