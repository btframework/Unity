#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SerialOppClientSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 513 x 450
    private const float CanvasW = 533f;
    private const float CanvasH = 470f;

    [MenuItem("Tools/Serial OPP Client/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("SerialOppClientUI");
        DestroyIfExists("SerialOppClientDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("SerialOppClientUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Top row
        // ============================================================
        var laSelectComPort = MakeLabel("LaSelectComPort", "Select COM Port", root, 12, 13, 100, 13);
        var ddPorts         = MakeDropdown("DdPorts", root, 104, 12, 152, 21, new string[0]);
        var btRefresh       = MakeButton("BtRefresh",    "Refresh",    root, 262, 10, 75, 23);
        var btConnect       = MakeButton("BtConnect",    "Connect",    root, 343, 10, 75, 23);
        var btDisconnect    = MakeButton("BtDisconnect", "Disconnect", root, 424, 10, 75, 23);

        // ============================================================
        //  OBEX row
        // ============================================================
        var btObexConnect    = MakeButton("BtObexConnect",    "OBEX connect",    root, 15,  39, 106, 23);
        var btObexDisconnect = MakeButton("BtObexDisconnect", "OBEX disconnect", root, 127, 39, 106, 23);

        // ============================================================
        //  File row
        // ============================================================
        var laFilePath   = MakeLabel("LaFilePath", "File path", root, 12, 69, 55, 13);
        var edFilePath   = MakeInputField("EdFilePath", root, 65, 68, 272, 20, "");
        var btSelectFile = MakeButton("BtSelectFile", "Select file", root, 343, 66, 75, 23);
        var btSendFile   = MakeButton("BtSendFile",   "Send file",   root, 424, 66, 75, 23);

        // ============================================================
        //  ProgressBar (15, 94) 484x23
        // ============================================================
        var pbGO = CreateUI("ProgressBar", root);
        Place(pbGO, 15, 94, 484, 23);

        var pbBg = pbGO.AddComponent<Image>();
        pbBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        pbBg.raycastTarget = false;

        var fillGO = CreateUI("Fill", pbGO.transform);
        StretchFull(fillGO);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.7f, 1f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;
        fillImg.raycastTarget = false;

        var progressLabelGO = CreateUI("Label", pbGO.transform);
        StretchFull(progressLabelGO);
        var progressLabel = progressLabelGO.AddComponent<TextMeshProUGUI>();
        progressLabel.text = "";
        progressLabel.alignment = TextAlignmentOptions.Center;
        progressLabel.color = Color.white;
        progressLabel.fontSize = 12;
        progressLabel.raycastTarget = false;

        // ============================================================
        //  Clear (424, 123) 75x23
        // ============================================================
        var btClear = MakeButton("BtClear", "Clear", root, 424, 123, 75, 23);

        // ============================================================
        //  Log (15, 152) 484x290
        // ============================================================
        var logScroll  = MakeScrollView("LogScroll", root, 15, 152, 484, 290, false);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent);

        // ============================================================
        //  Prefabs
        // ============================================================
        var logRow = MakeTableRowPrefab("SerialOppClientLogRow", new[] { 478f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("SerialOppClientDemo");
        var demo = demoGO.AddComponent<SerialOppClientDemo>();

        demo.selectComPortLabel  = laSelectComPort;
        demo.portsDropdown       = ddPorts;
        demo.refreshButton       = btRefresh;
        demo.connectButton       = btConnect;
        demo.disconnectButton    = btDisconnect;

        demo.obexConnectButton    = btObexConnect;
        demo.obexDisconnectButton = btObexDisconnect;

        demo.filePathLabel  = laFilePath;
        demo.filePathInput  = edFilePath;
        demo.selectFileButton = btSelectFile;
        demo.sendFileButton   = btSendFile;

        demo.progressFill  = fillImg;
        demo.progressLabel = progressLabel;

        demo.clearButton  = btClear;
        demo.logContent   = logContent;
        demo.logRowPrefab = logRow;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Serial OPP Client Demo UI built.");
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

    private static TMP_Dropdown MakeDropdown(string name, Transform parent,
        float x, float y, float w, float h, string[] opts)
    {
        var go = CreateUI(name, parent);
        Place(go, x, y, w, h);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var dd = go.AddComponent<TMP_Dropdown>();

        var labelGO = CreateUI("Label", go.transform);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = opts.Length > 0 ? opts[0] : "";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.fontSize = 12;
        var lRect = label.rectTransform;
        lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
        lRect.offsetMin = new Vector2(8, 0); lRect.offsetMax = new Vector2(-20, 0);

        var arrow = CreateUI("Arrow", go.transform);
        var arrowImg = arrow.AddComponent<Image>();
        arrowImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        var aRect = arrow.GetComponent<RectTransform>();
        aRect.anchorMin = new Vector2(1, 0.5f); aRect.anchorMax = new Vector2(1, 0.5f);
        aRect.pivot = new Vector2(1, 0.5f);
        aRect.anchoredPosition = new Vector2(-6, 0);
        aRect.sizeDelta = new Vector2(10, 10);

        var template = CreateUI("Template", go.transform);
        var tRect = template.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0, 0); tRect.anchorMax = new Vector2(1, 0);
        tRect.pivot = new Vector2(0.5f, 1);
        tRect.anchoredPosition = new Vector2(0, 2);
        tRect.sizeDelta = new Vector2(0, 200);

        var tImg = template.AddComponent<Image>();
        tImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var tScroll = template.AddComponent<ScrollRect>();
        tScroll.horizontal = false;
        template.AddComponent<RectMask2D>();  // Use Mask

        var viewport = CreateUI("Viewport", template.transform);
        StretchFull(viewport);

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1); cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 22);

        var item = CreateUI("Item", content.transform);
        var iRect = item.GetComponent<RectTransform>();
        iRect.anchorMin = new Vector2(0, 0.5f); iRect.anchorMax = new Vector2(1, 0.5f);
        iRect.pivot = new Vector2(0.5f, 0.5f);
        iRect.anchoredPosition = Vector2.zero;
        iRect.sizeDelta = new Vector2(0, 22);

        var itemToggle = item.AddComponent<Toggle>();

        var itemBg = CreateUI("Item Background", item.transform);
        StretchFull(itemBg);
        var ibImg = itemBg.AddComponent<Image>();
        ibImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var itemCheck = CreateUI("Item Checkmark", item.transform);
        var icRect = itemCheck.GetComponent<RectTransform>();
        icRect.anchorMin = new Vector2(0, 0.5f); icRect.anchorMax = new Vector2(0, 0.5f);
        icRect.pivot = new Vector2(0, 0.5f);
        icRect.anchoredPosition = new Vector2(8, 0);
        icRect.sizeDelta = new Vector2(12, 12);
        var icImg = itemCheck.AddComponent<Image>();
        icImg.color = new Color(0.4f, 0.7f, 1f, 1f);

        var itemLabel = CreateUI("Item Label", item.transform);
        var ilRect = itemLabel.GetComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero; ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(24, 0); ilRect.offsetMax = new Vector2(-8, 0);
        var ilText = itemLabel.AddComponent<TextMeshProUGUI>();
        ilText.text = "Option"; ilText.color = Color.white; ilText.fontSize = 12;
        ilText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = ibImg;
        itemToggle.graphic = icImg;

        tScroll.viewport = viewport.GetComponent<RectTransform>();
        tScroll.content = cRect;

        dd.template = tRect;
        dd.captionText = label;
        dd.itemText = ilText;

        dd.options.Clear();
        foreach (var o in opts) dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();

        template.SetActive(false);

        EditorUtility.SetDirty(dd);
        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(ilText);
        EditorUtility.SetDirty(go);

        return dd;
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
        viewport.AddComponent<RectMask2D>();  // Use Mask + Image

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
        ui.normalColor   = new Color(1, 1, 1, 0.03f);
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