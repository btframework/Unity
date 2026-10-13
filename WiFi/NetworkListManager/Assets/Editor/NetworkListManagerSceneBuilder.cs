#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class NetworkListManagerSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 892 ? 540
    private const float CanvasW = 912f;
    private const float CanvasH = 560f;

    [MenuItem("Tools/Network List Manager/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("NetworkListManagerUI");
        DestroyIfExists("NetworkListManagerDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("NetworkListManagerUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Row 1: Open | Close | Is Connected | Is Internet | Connectivity
        //         Network type label + dropdown | Enum networks | Refresh
        // ============================================================
        var btOpen             = MakeButton("BtOpen",             "Open",             root, 12,  12, 75, 23);
        var btClose            = MakeButton("BtClose",            "Close",            root, 93,  12, 75, 23);
        var btIsConnected      = MakeButton("BtIsConnected",      "Is Connected",     root, 187, 12, 89, 23);
        var btIsConnectedNet   = MakeButton("BtIsInternet",       "Is Internet",      root, 282, 12, 75, 23);
        var btConnectivity     = MakeButton("BtConnectivity",     "Connectivity",     root, 363, 12, 75, 23);

        MakeLabel("LaNetworkType", "Network type", root, 458, 15, 70, 13);
        var ddNetworkType = MakeDropdown("DdNetworkType", root, 534, 14, 121, 21,
            new[] { "Connected", "Disconnected", "All" });

        var btEnumNetworks = MakeButton("BtEnumNetworks", "Enum networks", root, 661, 12, 94, 23);
        var btRefresh      = MakeButton("BtRefresh",      "Refresh",       root, 805, 12, 75, 23);

        // ============================================================
        //  Networks table (12, 41) 868?118
        //  Колонки: 250 | 100 | 100 | 100 | 100 | 100 | 100 | 100 | 80 | 100
        // ============================================================
        var networksWidths = new[] { 250f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 80f, 100f };
        var networksHeaders = new[]
        {
            "Id", "Category", "Connectivity", "Description", "Domain type",
            "Name", "Creation Date Time", "Connected Date Time",
            "Connected", "Connected To Internet"
        };

        MakeHeaderBar("NetworksHeader", root, 12, 41, 868, 20,
            networksWidths, networksHeaders);

        var networksScroll  = MakeScrollView("NetworksScroll", root, 12, 61, 868, 98);
        var networksContent = networksScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(networksContent);

        // ============================================================
        //  Row 2: Edit name / description / category / delete
        // ============================================================
        MakeLabel("LaNewNameOrDescription", "New name or description", root, 9, 166, 130, 13);
        var edNewNameOrDesc = MakeInputField("EdNewNameOrDesc", root, 139, 165, 147, 20,
            "NewNameOrDescription");

        var btSetName        = MakeButton("BtSetName",        "Set name",         root, 292, 163, 75, 23);
        var btSetDescription = MakeButton("BtSetDescription", "Set description",  root, 373, 163, 87, 23);

        MakeLabel("LaCategory", "Category", root, 484, 166, 50, 13);
        var ddCategory = MakeDropdown("DdCategory", root, 534, 165, 146, 21,
            new[] { "Public", "Private", "Domain Authenticated" });

        var btSetCategory = MakeButton("BtSetCategory", "Set category", root, 686, 163, 75, 23);
        var btDelete      = MakeButton("BtDelete",      "Delete",       root, 767, 163, 75, 23);

        // ============================================================
        //  Row 3: connections
        // ============================================================
        var btMachineConn = MakeButton("BtMachineConn", "Get machine connections", root, 12,  203, 145, 23);
        var btNetworkConn = MakeButton("BtNetworkConn", "Get network connections", root, 163, 203, 140, 23);
        var btEnumAdapters= MakeButton("BtEnumAdapters","Enum adapters",           root, 373, 203, 87,  23);

        // ============================================================
        //  Connections table (12, 232) 868?106 — 9 колонок (максимум для адаптеров)
        //  150 | 150 | 150 | 100 | 200 | 100 | 80 | 150 | 150
        // ============================================================
        var connWidths = new[] { 150f, 150f, 150f, 100f, 200f, 100f, 80f, 150f, 150f };
        var connHeaders = new[]
        {
            "Id / Adapter id", "Friendly name / Connected", "Description / Connected to internet",
            "MAC / Connectivity", "IPs / Domain type", "DNS suffix / Id",
            "MTU", "Flags", "Status"
        };

        MakeHeaderBar("ConnectionsHeader", root, 12, 232, 868, 20, connWidths, connHeaders);

        var connScroll  = MakeScrollView("ConnectionsScroll", root, 12, 252, 868, 86);
        var connContent = connScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(connContent);

        // ============================================================
        //  Events log (12, 373) 868?160 + Clear (805, 344)
        // ============================================================
        var eventsScroll  = MakeScrollView("EventsScroll", root, 12, 373, 868, 160);
        var eventsContent = eventsScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(eventsContent);

        var btClear = MakeButton("BtClear", "Clear", root, 805, 344, 75, 23);

        // ============================================================
        //  Prefabs
        // ============================================================
        var networkRow    = MakeTableRowPrefab("NlmNetworkRow",    networksWidths, 22f);
        var connectionRow = MakeTableRowPrefab("NlmConnectionRow", connWidths,     22f);
        var eventRow      = MakeTableRowPrefab("NlmEventRow",      new[] { 860f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("NetworkListManagerDemo");
        var demo = demoGO.AddComponent<NetworkListManagerDemo>();

        demo.openButton                  = btOpen;
        demo.closeButton                 = btClose;
        demo.isConnectedButton           = btIsConnected;
        demo.isConnectedToInternetButton = btIsConnectedNet;
        demo.connectivityButton          = btConnectivity;
        demo.networkTypeDropdown         = ddNetworkType;
        demo.enumNetworksButton          = btEnumNetworks;
        demo.refreshButton               = btRefresh;

        demo.networksContent  = networksContent;
        demo.networkRowPrefab = networkRow;

        demo.newNameOrDescriptionInput = edNewNameOrDesc;
        demo.setNameButton             = btSetName;
        demo.setDescriptionButton      = btSetDescription;
        demo.categoryDropdown          = ddCategory;
        demo.setCategoryButton         = btSetCategory;
        demo.deleteButton              = btDelete;

        demo.getMachineConnectionsButton = btMachineConn;
        demo.getNetworkConnectionsButton = btNetworkConn;
        demo.enumAdaptersButton          = btEnumAdapters;

        demo.connectionsContent  = connContent;
        demo.connectionRowPrefab = connectionRow;

        demo.eventsContent     = eventsContent;
        demo.eventRowPrefab    = eventRow;
        demo.clearEventsButton = btClear;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? Network List Manager Demo UI собрана по оригинальному layout'у.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
        var tMask = template.AddComponent<Mask>();
        tMask.showMaskGraphic = true;

        var viewport = CreateUI("Viewport", template.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.001f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

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
        var ibRect = itemBg.GetComponent<RectTransform>();
        ibRect.anchorMin = Vector2.zero; ibRect.anchorMax = Vector2.one;
        ibRect.offsetMin = Vector2.zero; ibRect.offsetMax = Vector2.zero;
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

        tScroll.viewport = vpRect;
        tScroll.content = cRect;

        dd.template = tRect;
        dd.captionText = label;
        dd.itemText = ilText;

        dd.options.Clear();
        foreach (var o in opts) dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();

        EditorUtility.SetDirty(dd);
        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(ilText);
        EditorUtility.SetDirty(go);

        template.SetActive(false);
        return dd;
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