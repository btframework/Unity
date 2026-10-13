#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RfCommServerSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // WinForms ClientSize = 641 x 592 + 20 margin on all sides
    private const float CanvasW = 680f;
    private const float CanvasH = 620f;

    [MenuItem("Tools/RFCOMM Server/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("RfCommServerUI");
        DestroyIfExists("RfCommServerDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // ---------- Canvas ----------
        var canvasGO = CreateUI("RfCommServerUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  cbAuthentication (12, 16)
        //  cbEncryption     (12, 39)
        // ============================================================
        Toggle tgAuth = MakeToggle("TgAuth", "Authentication", canvasGO.transform, 12, 14, 94, 17);
        Toggle tgEnc  = MakeToggle("TgEnc",  "Encryption",     canvasGO.transform, 12, 37, 76, 17);

        // ============================================================
        //  laServiceName (122, 29) 72x13  "Service name"
        //  edServiceName (200, 26) 193x20 "Bluetooth Framework server"
        // ============================================================
        MakeLabel("LaServiceName", "Service name", canvasGO.transform, 122, 27, 72, 13);
        TMP_InputField edSvcName = MakeInputField("EdSvcName",
            canvasGO.transform, 200, 26, 193, 20, "Bluetooth Framework server");

        // ============================================================
        //  btListen (473, 4) 75x23
        //  btClose  (473, 33) 75x23
        // ============================================================
        Button btListen = MakeButton("BtListen", "Listen", canvasGO.transform, 473, 4, 75, 23);
        Button btClose  = MakeButton("BtClose",  "Close",  canvasGO.transform, 473, 33, 75, 23);

        // ============================================================
        //  lvClients (12, 62) 536x76  columns 150 | 150
        //  + btDisconnect (554, 62) 75x23
        // ============================================================
        MakeHeaderBar("ClientsHeader", canvasGO.transform, 12, 62, 536, 20,
            new[] { 150f, 150f }, new[] { "Address", "Name" });

        var clientsScroll  = MakeScrollView("ClientsScroll", canvasGO.transform, 12, 82, 536, 56);
        var clientsContent = clientsScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(clientsContent);

        Button btDisconnect = MakeButton("BtDisconnect", "Disconnect",
            canvasGO.transform, 554, 62, 75, 23);

        // ============================================================
        //  edText (12, 146) 536x20
        //  btSend (554, 144) 75x23
        // ============================================================
        TMP_InputField edText = MakeInputField("EdText",
            canvasGO.transform, 12, 146, 536, 20, "Send this text to selected client");
        Button btSend = MakeButton("BtSend", "Send", canvasGO.transform, 554, 144, 75, 23);

        // ============================================================
        //  btGetBuffers   (12, 172)  75x23
        //  laReadBuffer   (93, 177)  63x13
        //  edReadBuffer   (162, 174) 100x20
        //  laWriteBuffer  (268, 177) 62x13
        //  edWriteBuffer  (336, 174) 100x20
        //  btSetBuffers   (442, 172) 75x23
        // ============================================================
        Button btGetBuffers = MakeButton("BtGetBuffers", "Get buffers",
            canvasGO.transform, 12, 172, 75, 23);
        MakeLabel("LaReadBuffer", "Read buffer", canvasGO.transform, 93, 175, 63, 13);
        TMP_InputField edReadBuffer = MakeInputField("EdReadBuffer",
            canvasGO.transform, 162, 174, 100, 20, "");
        MakeLabel("LaWriteBuffer", "Write buffer", canvasGO.transform, 268, 175, 62, 13);
        TMP_InputField edWriteBuffer = MakeInputField("EdWriteBuffer",
            canvasGO.transform, 336, 174, 100, 20, "");
        Button btSetBuffers = MakeButton("BtSetBuffers", "Set buffers",
            canvasGO.transform, 442, 172, 75, 23);

        // ============================================================
        //  lbEvents (12, 201) 617x342
        //  btClear  (554, 557) 75x23
        // ============================================================
        var eventsScroll  = MakeScrollView("EventsScroll", canvasGO.transform, 12, 201, 617, 342);
        var eventsContent = eventsScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(eventsContent);

        Button btClear = MakeButton("BtClear", "Clear", canvasGO.transform, 554, 557, 75, 23);

        // ============================================================
        //  Prefabs
        // ============================================================
        var clientRow = MakeTableRowPrefab("RfCommServerClientRow",
            new[] { 150f, 150f }, 22f);
        var eventRow  = MakeTableRowPrefab("RfCommServerEventRow",
            new[] { 610f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("RfCommServerDemo");
        var demo = demoGO.AddComponent<RfCommServerDemo>();

        demo.authenticationToggle = tgAuth;
        demo.encryptionToggle     = tgEnc;
        demo.listenButton         = btListen;
        demo.closeButton          = btClose;

        demo.serviceNameLabel = null;
        demo.serviceNameInput = edSvcName;

        demo.clientsContent  = clientsContent;
        demo.clientRowPrefab = clientRow;
        demo.disconnectButton = btDisconnect;

        demo.textInput  = edText;
        demo.sendButton = btSend;

        demo.getBuffersButton = btGetBuffers;
        demo.readBufferLabel  = null;
        demo.readBufferInput  = edReadBuffer;
        demo.writeBufferLabel = null;
        demo.writeBufferInput = edWriteBuffer;
        demo.setBuffersButton = btSetBuffers;

        demo.eventsContent     = eventsContent;
        demo.eventRowPrefab    = eventRow;
        demo.clearEventsButton = btClear;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("RFCOMM Server Demo UI built using the original layout.");
    }

    // ================================================================
    //  Position - absolute from the top-left corner
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
        StretchFull(textGO);

        return btn;
    }

    private static Toggle MakeToggle(string name, string label, Transform parent,
        float x, float y, float w, float h)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var toggle = go.AddComponent<Toggle>();

        var bg = CreateUI("Background", go.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot     = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(13, 13);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        var cm = CreateUI("Checkmark", bg.transform);
        var cmRect = cm.GetComponent<RectTransform>();
        cmRect.anchorMin = new Vector2(0.5f, 0.5f);
        cmRect.anchorMax = new Vector2(0.5f, 0.5f);
        cmRect.pivot     = new Vector2(0.5f, 0.5f);
        cmRect.anchoredPosition = Vector2.zero;
        cmRect.sizeDelta = new Vector2(9, 9);
        var cmImg = cm.AddComponent<Image>();
        cmImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);

        var lbl = CreateUI("Label", go.transform);
        var lblRect = lbl.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(1, 1);
        lblRect.offsetMin = new Vector2(17, 0);
        lblRect.offsetMax = Vector2.zero;
        var lblText = lbl.AddComponent<TextMeshProUGUI>();
        lblText.text = label;
        lblText.color = Color.white;
        lblText.fontSize = 12;
        lblText.alignment = TextAlignmentOptions.MidlineLeft;
        lblText.enableWordWrapping = false;
        lblText.overflowMode = TextOverflowModes.Overflow;

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
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    private static TMP_InputField MakeInputField(string name, Transform parent,
        float x, float y, float w, float h, string defaultText)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var bg = go.AddComponent<Image>();
        bg.color = Color.white;

        var ta = CreateUI("Text Area", go.transform);
        var taRect = ta.GetComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(3, 2);
        taRect.offsetMax = new Vector2(-3, -2);

        var txt = ta.AddComponent<TextMeshProUGUI>();
        txt.color = Color.black;
        txt.fontSize = 12;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Overflow;

        var ph = CreateUI("Placeholder", ta.transform);
        var phRect = ph.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        var phT = ph.AddComponent<TextMeshProUGUI>();
        phT.text = "";
        phT.color = new Color(0, 0, 0, 0.4f);
        phT.fontSize = 12;
        phT.alignment = TextAlignmentOptions.MidlineLeft;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport  = taRect;
        input.textComponent = txt;
        input.placeholder   = phT;
        input.targetGraphic = bg;
        input.text = defaultText;

        return input;
    }

    // ================================================================
    //  Header bar
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
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            offset += widths[i];
        }
    }

    // ================================================================
    //  ScrollView
    // ================================================================
    private static GameObject MakeScrollView(string name, Transform parent,
        float x, float y, float w, float h)
    {
        var root = CreateUI(name, parent);
        Place(root, x, y, w, h);

        var rootImg = root.AddComponent<Image>();
        rootImg.color = Color.white;

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
        img.color = new Color(0, 0, 0, 0.03f);
        root.AddComponent<Button>();

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;

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
    //  Helpers
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