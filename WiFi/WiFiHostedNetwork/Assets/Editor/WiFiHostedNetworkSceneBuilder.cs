#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WiFiHostedNetworkSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 644 ? 468
    private const float CanvasW = 664f;
    private const float CanvasH = 490f;

    [MenuItem("Tools/WiFi Hosted Network/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("WiFiHostedNetworkUI");
        DestroyIfExists("WiFiHostedNetworkDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("WiFiHostedNetworkUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Row 1: Open | Close | Clear
        // ============================================================
        var btOpen  = MakeButton("BtOpen",  "Open",  root, 12,  12, 75, 23);
        var btClose = MakeButton("BtClose", "Close", root, 93,  12, 75, 23);
        var btClear = MakeButton("BtClear", "Clear", root, 202, 12, 75, 23);

        // ============================================================
        //  Row 2: Get connection settings | Get key | Get profile |
        //         Get security settings | Get status | Get state
        // ============================================================
        var btGetConnSettings = MakeButton("BtGetConnSettings", "Get Connection Settings", root, 12,  41, 156, 23);
        var btGetKey          = MakeButton("BtGetKey",          "Get Key",                 root, 174, 41, 75,  23);
        var btGetProfile      = MakeButton("BtGetProfile",      "Get profile",             root, 255, 41, 75,  23);
        var btGetSecSettings  = MakeButton("BtGetSecSettings",  "Get security settings",   root, 336, 41, 116, 23);
        var btGetStatus       = MakeButton("BtGetStatus",       "Get status",              root, 458, 41, 75,  23);
        var btGetState        = MakeButton("BtGetState",        "Get state",               root, 539, 41, 75,  23);

        // ============================================================
        //  Data table (12, 70) 620?192, колонки 280 | 280
        // ============================================================
        var dataWidths  = new[] { 280f, 280f };
        var dataHeaders = new[] { "Parameter name", "Parameter value" };

        MakeHeaderBar("DataHeader", root, 12, 70, 620, 20, dataWidths, dataHeaders);

        var dataScroll  = MakeScrollView("DataScroll", root, 12, 90, 620, 172);
        var dataContent = dataScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(dataContent);

        // ============================================================
        //  Row 3: Refresh | Restart
        // ============================================================
        var btRefresh = MakeButton("BtRefresh", "Refresh security settings", root, 12,  277, 143, 23);
        var btRestart = MakeButton("BtRestart", "Restart",                   root, 161, 277, 75,  23);

        // ============================================================
        //  Settings: SSID / Number of peers / Set settings
        //  laHnSsid (12, 327)
        //  edSsHnid (111, 324) 125?20
        //  laHnNumberOfPeers (12, 355)
        //  edHnNumberOfPeers (111, 352) 100?20
        //  btHnSetSettings (161, 378) 75?23
        // ============================================================
        var laSsid = MakeLabel("LaSsid", "SSID", root, 12, 327, 40, 13);
        var edSsid = MakeInputField("EdSsid", root, 111, 324, 125, 20, "MyHostedNetwork");

        var laNumPeers = MakeLabel("LaNumPeers", "Number of peers", root, 12, 355, 100, 13);
        var edNumPeers = MakeInputField("EdNumPeers", root, 111, 352, 100, 20, "5");

        var btSetSettings = MakeButton("BtSetSettings", "Set settings", root, 161, 378, 75, 23);

        // ============================================================
        //  Key / Persistent / Set key
        //  laHnKey (333, 327)
        //  edHnKey (374, 324) 131?20
        //  cbHnPersistent (336, 354)
        //  btHnSetKey (430, 378) 75?23
        // ============================================================
        var laKey = MakeLabel("LaKey", "Key", root, 333, 327, 30, 13);
        var edKey = MakeInputField("EdKey", root, 374, 324, 131, 20, "123456789");
        var tgPersistent = MakeToggle("TgPersistent", "Persistent", root, 336, 352, 100, 17);
        var btSetKey = MakeButton("BtSetKey", "Set key", root, 430, 378, 75, 23);

        // ============================================================
        //  Row 4: Enable | Disable
        // ============================================================
        var btEnable  = MakeButton("BtEnable",  "Enable",  root, 12, 433, 75, 23);
        var btDisable = MakeButton("BtDisable", "Disable", root, 93, 433, 75, 23);

        // ============================================================
        //  Prefabs
        // ============================================================
        var dataRow = MakeTableRowPrefab("WiFiHostedNetworkDataRow", dataWidths, 22f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("WiFiHostedNetworkDemo");
        var demo = demoGO.AddComponent<WiFiHostedNetworkDemo>();

        demo.openButton  = btOpen;
        demo.closeButton = btClose;
        demo.clearButton = btClear;

        demo.getConnectionSettingsButton = btGetConnSettings;
        demo.getKeyButton                = btGetKey;
        demo.getProfileButton            = btGetProfile;
        demo.getSecuritySettingsButton   = btGetSecSettings;
        demo.getStatusButton             = btGetStatus;
        demo.getStateButton              = btGetState;

        demo.dataContent  = dataContent;
        demo.dataRowPrefab = dataRow;

        demo.refreshSecuritySettingsButton = btRefresh;
        demo.restartButton                 = btRestart;

        demo.ssidLabel          = laSsid;
        demo.ssidInput          = edSsid;
        demo.numberOfPeersLabel = laNumPeers;
        demo.numberOfPeersInput = edNumPeers;
        demo.setSettingsButton  = btSetSettings;

        demo.keyLabel        = laKey;
        demo.keyInput        = edKey;
        demo.persistentToggle = tgPersistent;
        demo.setKeyButton    = btSetKey;

        demo.enableButton  = btEnable;
        demo.disableButton = btDisable;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("? Wi-Fi Hosted Network Demo UI собрана по оригинальному layout'у.");
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
        text.fontSize = 11;
        text.enableWordWrapping = false;
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
        lblText.fontSize = 11;
        lblText.alignment = TextAlignmentOptions.MidlineLeft;
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
        t.fontSize = 11;
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
        txt.fontSize = 11;
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
        phT.fontSize = 11;
        phT.alignment = TextAlignmentOptions.MidlineLeft;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport  = taRect;
        input.textComponent = txt;
        input.placeholder   = phT;
        input.targetGraphic = bg;
        input.text = defaultText;

        return input;
    }

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