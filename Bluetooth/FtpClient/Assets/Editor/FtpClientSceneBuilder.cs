#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class FtpClientSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    [MenuItem("Tools/FTP Client/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("FtpClientUI");
        DestroyIfExists("FtpClientDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // ---------- Canvas ----------
        var canvasGO = CreateUI("FtpClientUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Row 1: Discover | Connect | Disconnect
        // ============================================================
        var r1 = MakeHBar("Row1", canvasGO.transform, 20, 40);
        var btDiscover   = MakeButton("BtDiscover",   "Discover",   r1.transform, 140, 40);
        var btConnect    = MakeButton("BtConnect",    "Connect",    r1.transform, 140, 40);
        var btDisconnect = MakeButton("BtDisconnect", "Disconnect", r1.transform, 140, 40);

        // ============================================================
        //  Devices table (Address | Name)
        // ============================================================
        var devicesWidths = new[] { 300f, 600f };
        var devicesHeaders = new[] { "Address", "Name" };
        var (devicesScroll, devicesContent) = MakeTable(
            "DevicesScroll", canvasGO.transform,
            new Vector2(20, -80), 180, devicesWidths, devicesHeaders);

        // ============================================================
        //  Row 2: OpenSession | CloseSession
        // ============================================================
        var r2 = MakeHBar("Row2", canvasGO.transform, 280, 40);
        var btOpenSession  = MakeButton("BtOpenSession",  "Open Session",  r2.transform, 180, 40);
        var btCloseSession = MakeButton("BtCloseSession", "Close Session", r2.transform, 180, 40);

        // ============================================================
        //  ProgressBar
        // ============================================================
        var pbGO = CreateUI("ProgressBar", canvasGO.transform);
        var pbRect = pbGO.GetComponent<RectTransform>();
        pbRect.anchorMin = new Vector2(0, 1); pbRect.anchorMax = new Vector2(1, 1);
        pbRect.pivot = new Vector2(0.5f, 1);
        pbRect.anchoredPosition = new Vector2(0, -330);
        pbRect.sizeDelta = new Vector2(-40, 30);

        var pbBg = pbGO.AddComponent<Image>();
        pbBg.color = new Color(0.12f, 0.12f, 0.12f);
        pbBg.raycastTarget = false;

        var fillGO = CreateUI("Fill", pbGO.transform);
        StretchFull(fillGO);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.7f, 1f);
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
        progressLabel.fontSize = 18;
        progressLabel.raycastTarget = false;

        // ============================================================
        //  Row 3: SelectFile | EdFileName | SendFile | Abort
        // ============================================================
        var r3 = MakeHBar("Row3", canvasGO.transform, 370, 40);
        var btSelectFile = MakeButton("BtSelectFile", "Select File", r3.transform, 150, 40);
        var edFileName   = MakeInputField("EdFileName", r3.transform, 700, 40, "");
        var btSendFile   = MakeButton("BtSendFile", "Send file", r3.transform, 150, 40);
        var btAbort      = MakeButton("BtAbort",    "Abort",     r3.transform, 120, 40);

        // ============================================================
        //  Row 4: Label | EdNewDirName | MkDir | Dir | Delete | Root
        // ============================================================
        var r4 = MakeHBar("Row4", canvasGO.transform, 420, 40);
        MakeLabel("New dir name", r4.transform, 160, 40);
        var edNewDirName = MakeInputField("EdNewDirName", r4.transform, 500, 40, "");
        var btMkDir   = MakeButton("BtMkDir",   "MkDir",   r4.transform, 140, 40);
        var btDir     = MakeButton("BtDir",     "Dir",     r4.transform, 120, 40);
        var btDelete  = MakeButton("BtDelete",  "Delete",  r4.transform, 140, 40);
        var btRoot    = MakeButton("BtRoot",    "Root",    r4.transform, 120, 40);

        // ============================================================
        //  Files table (8 columns)
        // ============================================================
        var filesWidths = new[] { 240f, 90f, 100f, 100f, 160f, 160f, 160f, 260f };
        var filesHeaders = new[] { "Object name", "Type", "Size", "Permission",
                                    "Modified", "Created", "Accessed", "Description" };
        var (filesScroll, filesContent) = MakeTable(
            "FilesScroll", canvasGO.transform,
            new Vector2(20, -470), 230, filesWidths, filesHeaders);

        // ============================================================
        //  Log table (одна колонка)
        // ============================================================
        var logWidths = new[] { 1800f };
        var logHeaders = new[] { "Log" };
        var (logScroll, logContent) = MakeTable(
            "LogScroll", canvasGO.transform,
            new Vector2(20, -720), 280, logWidths, logHeaders);

        var clearLog = MakeButton("BtClearLog", "Clear", canvasGO.transform, 120, 40);
        var ceRect = clearLog.GetComponent<RectTransform>();
        ceRect.anchorMin = new Vector2(1, 0); ceRect.anchorMax = new Vector2(1, 0);
        ceRect.pivot = new Vector2(1, 0);
        ceRect.anchoredPosition = new Vector2(-20, 20);

        // ============================================================
        //  Prefabs для строк
        // ============================================================
        var deviceRow = MakeTableRowPrefab("FtpDeviceRow", devicesWidths);
        var fileRow   = MakeTableRowPrefab("FtpFileRow",   filesWidths);
        var logRow    = MakeTableRowPrefab("FtpLogRow",    logWidths);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("FtpClientDemo");
        var demo = demoGO.AddComponent<FtpClientDemo>();

        demo.discoverButton   = btDiscover;
        demo.connectButton    = btConnect;
        demo.disconnectButton = btDisconnect;

        demo.devicesContent  = devicesContent;
        demo.deviceRowPrefab = deviceRow;

        demo.openSessionButton  = btOpenSession;
        demo.closeSessionButton = btCloseSession;
        demo.progressFill       = fillImg;
        demo.progressLabel      = progressLabel;

        demo.selectFileButton = btSelectFile;
        demo.fileNameInput    = edFileName;
        demo.sendFileButton   = btSendFile;
        demo.abortButton      = btAbort;

        demo.newDirNameInput = edNewDirName;
        demo.mkDirButton     = btMkDir;
        demo.dirButton       = btDir;
        demo.deleteButton    = btDelete;
        demo.rootButton      = btRoot;

        demo.filesContent  = filesContent;
        demo.fileRowPrefab = fileRow;

        demo.logContent     = logContent;
        demo.logRowPrefab   = logRow;
        demo.clearLogButton = clearLog;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? FTP Client Demo UI собрана. Нажми Play.");
    }

    // ================================================================
    //  Table helpers
    // ================================================================
    private static (GameObject scroll, Transform content) MakeTable(
        string name, Transform parent, Vector2 anchoredPos, float height,
        float[] widths, string[] headers)
    {
        // header
        var header = CreateUI(name + "_Header", parent);
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.pivot = new Vector2(0.5f, 1);
        hr.anchoredPosition = new Vector2(0, anchoredPos.y + 26);
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

        // scroll
        var scroll = MakeScrollView(name, parent);
        var sr = scroll.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 1); sr.anchorMax = new Vector2(1, 1);
        sr.pivot = new Vector2(0.5f, 1);
        sr.anchoredPosition = anchoredPos;
        sr.sizeDelta = new Vector2(-40, height);

        var content = scroll.transform.Find("Viewport/Content");
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return (scroll, content);
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

    // ================================================================
    //  UI helpers
    // ================================================================
    private static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

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