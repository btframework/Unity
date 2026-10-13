#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OppServerSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    [MenuItem("Tools/OPP Server/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("OppServerUI");
        DestroyIfExists("OppServerDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("OppServerUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Row 1: Listen | Close | Disconnect | Auto-accept
        // ============================================================
        var r1 = MakeHBar("Row1", canvasGO.transform, 20, 40);
        var btListen     = MakeButton("BtListen", "Listen", r1.transform, 140, 40);
        var btClose      = MakeButton("BtClose",  "Close",  r1.transform, 140, 40);
        var btDisconnect = MakeButton("BtDisconnect", "Disconnect", r1.transform, 160, 40);
        var tgAutoAccept = MakeToggle("TgAutoAccept", "Auto-accept", r1.transform, 240, 40);
        tgAutoAccept.isOn = true;

        // ============================================================
        //  Clients table (1 column: Address)
        // ============================================================
        var widths  = new[] { 1200f };
        var headers = new[] { "Address" };

        var header = CreateUI("ClientsHeader", canvasGO.transform);
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.pivot = new Vector2(0.5f, 1);
        hr.anchoredPosition = new Vector2(0, -96);
        hr.sizeDelta = new Vector2(-40, 24);

        var hHlg = header.AddComponent<HorizontalLayoutGroup>();
        hHlg.spacing = 4;
        hHlg.childForceExpandWidth = false; hHlg.childForceExpandHeight = true;
        hHlg.childControlWidth = true; hHlg.childControlHeight = true;

        for (int i = 0; i < headers.Length; i++)
        {
            var t = CreateText("H_" + headers[i], headers[i], header.transform);
            t.fontStyle = FontStyles.Bold;
            t.fontSize = 16;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = widths[i]; le.minWidth = widths[i];
        }

        var clientsScroll = MakeScrollView("ClientsScroll", canvasGO.transform);
        var csr = clientsScroll.GetComponent<RectTransform>();
        csr.anchorMin = new Vector2(0, 1); csr.anchorMax = new Vector2(1, 1);
        csr.pivot = new Vector2(0.5f, 1);
        csr.anchoredPosition = new Vector2(0, -124);
        csr.sizeDelta = new Vector2(-40, 180);

        var clientsContent = clientsScroll.transform.Find("Viewport/Content");
        var ccVlg = clientsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        ccVlg.spacing = 2;
        ccVlg.padding = new RectOffset(4, 4, 4, 4);
        ccVlg.childForceExpandWidth = true; ccVlg.childForceExpandHeight = false;
        ccVlg.childControlWidth = true; ccVlg.childControlHeight = true;
        var ccCsf = clientsContent.gameObject.AddComponent<ContentSizeFitter>();
        ccCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ============================================================
        //  Log
        // ============================================================
        var logScroll = MakeScrollView("LogScroll", canvasGO.transform);
        var lr = logScroll.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0);
        lr.anchorMax = new Vector2(1, 1);
        lr.offsetMin = new Vector2(20, 70);
        lr.offsetMax = new Vector2(-20, -320);

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
        var clientRowPrefab = MakeTableRowPrefab("OppServerClientRow", widths);
        var logRowPrefab    = MakeTableRowPrefab("OppServerLogRow", new[] { 1800f });

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("OppServerDemo");
        var demo = demoGO.AddComponent<OppServerDemo>();

        demo.listenButton      = btListen;
        demo.closeButton       = btClose;
        demo.disconnectButton  = btDisconnect;
        demo.autoAcceptToggle  = tgAutoAccept;

        demo.clientsContent  = clientsContent;
        demo.clientRowPrefab = clientRowPrefab;

        demo.logContent     = logContent;
        demo.logRowPrefab   = logRowPrefab;
        demo.clearLogButton = clearLog;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("OPP Server Demo UI built. Press Play.");
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

    private static Toggle MakeToggle(string name, string label, Transform parent, float w, float h)
    {
        var go = CreateUI(name, parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.minWidth = w;
        le.preferredHeight = h; le.minHeight = h;

        var toggle = go.AddComponent<Toggle>();

        var bg = CreateUI("Background", go.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f); bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(4, 0);
        bgRect.sizeDelta = new Vector2(20, 20);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        var cm = CreateUI("Checkmark", bg.transform);
        var cmRect = cm.GetComponent<RectTransform>();
        cmRect.anchorMin = new Vector2(0.5f, 0.5f); cmRect.anchorMax = new Vector2(0.5f, 0.5f);
        cmRect.pivot = new Vector2(0.5f, 0.5f); cmRect.anchoredPosition = Vector2.zero;
        cmRect.sizeDelta = new Vector2(12, 12);
        var cmImg = cm.AddComponent<Image>();
        cmImg.color = new Color(0.4f, 0.7f, 1f);

        var lbl = CreateText("Label", label, go.transform);
        lbl.fontSize = 18;
        var lRect = lbl.rectTransform;
        lRect.anchorMin = new Vector2(0, 0); lRect.anchorMax = new Vector2(1, 1);
        lRect.offsetMin = new Vector2(30, 0); lRect.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic = cmImg;
        return toggle;
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

    private static GameObject MakeTableRowPrefab(string name, float[] widths)
    {
        if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);

        var root = CreateUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 28);

        var img = root.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);
        root.AddComponent<Button>();

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 28; le.preferredHeight = 28;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;

        var cells = new TMP_Text[widths.Length];
        for (int i = 0; i < widths.Length; i++)
        {
            var t = CreateText("Cell" + i, "", root.transform);
            t.fontSize = 15;
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