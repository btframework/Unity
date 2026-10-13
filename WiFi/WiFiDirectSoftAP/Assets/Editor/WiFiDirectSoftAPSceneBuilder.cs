#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WiFiDirectSoftAPSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 512 x 406
    private const float CanvasW = 532f;
    private const float CanvasH = 430f;

    [MenuItem("Tools/WiFi Direct SoftAP/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("WiFiDirectSoftAPUI");
        DestroyIfExists("WiFiDirectSoftAPDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("WiFiDirectSoftAPUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  SSID row
        //  laSsid (12, 11)  32x13
        //  edSsid (77, 8)   137x20
        //  btGetIpSettings (220, 8) 102x23
        // ============================================================
        MakeLabel("LaSsid", "SSID", root, 12, 9, 40, 13);
        var edSsid = MakeInputField("EdSsid", root, 77, 8, 137, 20, "");
        var btGetIpSettings = MakeButton("BtGetIpSettings", "Get IP Settings", root, 220, 8, 102, 23);

        // ============================================================
        //  Passphrase row
        //  laPssphrase (9, 37)  62x13
        //  edPassphrase (77, 34) 137x20
        //  btSetIpSettings (220, 37) 102x23
        // ============================================================
        MakeLabel("LaPassphrase", "Passphrase", root, 9, 35, 70, 13);
        var edPassphrase = MakeInputField("EdPassphrase", root, 77, 34, 137, 20, "");
        var btSetIpSettings = MakeButton("BtSetIpSettings", "Set IP Settings", root, 220, 37, 102, 23);

        // ============================================================
        //  Start / Stop row
        //  btStart (40, 60)  75x23
        //  btStop  (121, 60) 75x23
        //  btResetIpSettings (220, 66) 102x23
        // ============================================================
        var btStart = MakeButton("BtStart", "Start", root, 40, 60, 75, 23);
        var btStop  = MakeButton("BtStop",  "Stop",  root, 121, 60, 75, 23);
        var btResetIpSettings = MakeButton("BtResetIpSettings", "Reset IP Settings", root, 220, 66, 102, 23);

        // ============================================================
        //  Log (12, 93) 488x303
        // ============================================================
        var logScroll  = MakeScrollView("LogScroll", root, 12, 93, 488, 303, false);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent);

        // ============================================================
        //  Confirm panel
        // ============================================================
        var (confirmPanel, confirmText, confirmYes, confirmNo) = MakeConfirmDialog(root);

        // ============================================================
        //  Prefabs
        // ============================================================
        var logRow = MakeTableRowPrefab("WiFiDirectSoftAPLogRow", new[] { 480f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("WiFiDirectSoftAPDemo");
        var demo = demoGO.AddComponent<WiFiDirectSoftAPDemo>();

        demo.ssidInput       = edSsid;
        demo.passphraseInput = edPassphrase;

        demo.startButton          = btStart;
        demo.stopButton           = btStop;
        demo.getIpSettingsButton  = btGetIpSettings;
        demo.setIpSettingsButton  = btSetIpSettings;
        demo.resetIpSettingsButton = btResetIpSettings;

        demo.logContent   = logContent;
        demo.logRowPrefab = logRow;

        demo.confirmPanel     = confirmPanel;
        demo.confirmText      = confirmText;
        demo.confirmYesButton = confirmYes;
        demo.confirmNoButton  = confirmNo;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Wi-Fi Direct SoftAP Demo UI assembled according to the original layout.");
    }

    // ================================================================
    //  Confirm dialog
    // ================================================================
    private static (GameObject panel, TMP_Text text, Button yes, Button no)
        MakeConfirmDialog(Transform parent)
    {
        var panel = CreateUI("ConfirmPanel", parent);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480, 180);

        var img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var txtGO = CreateUI("Text", panel.transform);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = true;
        txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = new Vector2(0, 0.4f); trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(20, 0); trt.offsetMax = new Vector2(-20, -10);

        var bar = CreateUI("Buttons", panel.transform);
        var br = bar.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0.4f);
        br.offsetMin = new Vector2(20, 15); br.offsetMax = new Vector2(-20, 0);
        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false; hlg.childControlHeight = false;

        var yes = MakeButton("Yes", "YES", bar.transform, 0, 0, 140, 40);
        var no  = MakeButton("No",  "NO",  bar.transform, 0, 0, 140, 40);

        panel.SetActive(false);
        return (panel, txt, yes, no);
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