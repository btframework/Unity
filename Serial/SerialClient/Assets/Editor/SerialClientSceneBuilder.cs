#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SerialClientSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 864 x 563
    private const float CanvasW = 884f;
    private const float CanvasH = 583f;

    [MenuItem("Tools/Serial Client/Build Scene")]
    public static void BuildScene()
    {
        // --- 0. Make sure Assets/Prefabs exists as a real asset folder ---
        EnsureAssetFolder(PrefabsFolder);

        // --- 1. Clean up previous build ---
        DestroyIfExists("SerialClientUI");
        DestroyIfExists("SerialClientDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // --- 2. Canvas ---
        var canvasGO = CreateUI("SerialClientUI", null);
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
        var btEnum           = MakeButton("BtEnum", "Enum", root, 12,  12, 75, 23);
        var ddPorts          = MakeDropdown("DdPorts", root, 93,  14, 112, 21, new string[0]);
        var btConnect        = MakeButton("BtConnect", "Connect",    root, 211, 12, 75, 23);
        var btDisconnect     = MakeButton("BtDisconnect", "Disconnect", root, 292, 12, 75, 23);
        var laWriteTimeout   = MakeLabel("LaWriteTimeout", "Write timeout", root, 373, 15, 69, 13);
        var edWriteTimeout   = MakeInputField("EdWriteTimeout", root, 448, 14, 100, 20, "");
        var btSetWriteTimeout= MakeButton("BtSetWriteTimeout", "Set write timeout", root, 554, 12, 104, 23);

        // ============================================================
        //  Config / buffers / comm-break buttons
        // ============================================================
        var btGetConfig      = MakeButton("BtGetConfig", "Get config", root, 12,  54, 75, 23);
        var btSetConfig      = MakeButton("BtSetConfig", "Set config", root, 93,  54, 75, 23);
        var btGetBuffers     = MakeButton("BtGetBuffers", "Get buffers", root, 427, 54, 75, 23);
        var btSetBuffers     = MakeButton("BtSetBuffers", "Set buffers", root, 508, 54, 75, 23);
        var btClearCommBreak = MakeButton("BtClearCommBreak", "Clear comm BREAK", root, 613, 54, 116, 23);
        var btSetCommBreak   = MakeButton("BtSetCommBreak",   "Set comm BREAK",   root, 735, 54, 116, 23);

        // ============================================================
        //  Config column 1
        // ============================================================
        var laBaudRate     = MakeLabel("LaBaudRate", "Baudrate", root, 12, 84, 60, 13);
        var edBaudRate     = MakeInputField("EdBaudRate", root, 82, 83, 121, 20, "");
        var laRtsControl   = MakeLabel("LaRtsControl", "RTS control", root, 12, 110, 70, 13);
        var ddRtsControl   = MakeDropdown("DdRtsControl", root, 82, 109, 121, 21,
            new[] { "rtsControlDisable", "rtsControlEnable", "rtsControlHandshake", "rtsControlToggle" });
        var laParity       = MakeLabel("LaParity", "Parity", root, 12, 137, 60, 13);
        var ddParity       = MakeDropdown("DdParity", root, 82, 136, 121, 21,
            new[] { "spNo", "spOdd", "spEven", "spMark", "spSpace" });
        var laXonLim       = MakeLabel("LaXonLim", "XON lim", root, 12, 164, 60, 13);
        var edXonLim       = MakeInputField("EdXonLim", root, 82, 163, 121, 20, "");

        // ============================================================
        //  Config column 2
        // ============================================================
        var laDtrControl   = MakeLabel("LaDtrControl", "DTR control", root, 209, 84, 70, 13);
        var ddDtrControl   = MakeDropdown("DdDtrControl", root, 280, 83, 121, 21,
            new[] { "dtrControlDisable", "dtrControlEnable", "dtrControlHandshake" });
        var laByteSize     = MakeLabel("LaByteSize", "Byte size", root, 209, 110, 60, 13);
        var ddByteSize     = MakeDropdown("DdByteSize", root, 280, 109, 121, 21,
            new[] { "4", "5", "6", "7", "8" });
        var laStopBits     = MakeLabel("LaStopBits", "Stop bits", root, 209, 137, 60, 13);
        var ddStopBits     = MakeDropdown("DdStopBits", root, 280, 136, 121, 21,
            new[] { "sbOne", "sbOne5", "sbTwo" });
        var laXoffLim      = MakeLabel("LaXoffLim", "XOFF lim", root, 209, 164, 60, 13);
        var edXoffLim      = MakeInputField("EdXoffLim", root, 280, 163, 121, 20, "");

        // ============================================================
        //  Char fields row (y=189)
        // ============================================================
        var laXonChar    = MakeLabel("LaXonChar",   "XON",   root, 12,  190, 30, 13);
        var edXonChar    = MakeInputField("EdXonChar", root, 48, 189, 29, 20, "");
        var laXoffChar   = MakeLabel("LaXoffChar",  "XOFF",  root, 89,  190, 35, 13);
        var edXoffChar   = MakeInputField("EdXoffChar", root, 129, 189, 29, 20, "");
        var laErrorChar  = MakeLabel("LaErrorChar", "Error", root, 175, 190, 30, 13);
        var edErrorChar  = MakeInputField("EdErrorChar", root, 210, 189, 29, 20, "");
        var laEofChar    = MakeLabel("LaEofChar",   "EOF",   root, 257, 190, 30, 13);
        var edEofChar    = MakeInputField("EdEofChar", root, 291, 189, 29, 20, "");
        var laEvtChar    = MakeLabel("LaEvtChar",   "EVT",   root, 338, 190, 30, 13);
        var edEvtChar    = MakeInputField("EdEvtChar", root, 372, 189, 29, 20, "");

        // ============================================================
        //  Checkboxes
        // ============================================================
        var tgParityCheck       = MakeToggle("TgParityCheck",       "Parity check",       root, 92,  213, 130, 17);
        var tgOutxDsrFlow       = MakeToggle("TgOutxDsrFlow",       "OUTX DSR flow",      root, 92,  236, 130, 17);
        var tgTXContinueOnXoff  = MakeToggle("TgTXContinueOnXoff",  "TX continue on XOFF",root, 92,  259, 150, 17);
        var tgErrorCharReplace  = MakeToggle("TgErrorCharReplace",  "Error char replace", root, 92,  282, 150, 17);
        var tgNullStrip         = MakeToggle("TgNullStrip",         "NULL strip",         root, 92,  305, 130, 17);

        var tgOutxCtsFlow       = MakeToggle("TgOutxCtsFlow",       "OUTX CTS flow",      root, 254, 213, 130, 17);
        var tgDsrSensitivity    = MakeToggle("TgDsrSensitivity",    "DSR sensitivity",    root, 254, 236, 130, 17);
        var tgOutX              = MakeToggle("TgOutX",              "OUTX",               root, 254, 259, 90,  17);
        var tgInX               = MakeToggle("TgInX",               "INX",                root, 254, 282, 90,  17);
        var tgAbortOnError      = MakeToggle("TgAbortOnError",      "Abort on error",     root, 254, 305, 120, 17);

        // ============================================================
        //  Buffers column
        // ============================================================
        var laReadBufferSize  = MakeLabel("LaReadBufferSize", "Read buffer size", root, 418, 84, 90, 13);
        var edReadBufferSize  = MakeInputField("EdReadBufferSize", root, 508, 83, 83, 20, "");
        var laWriteBufferSize = MakeLabel("LaWriteBufferSize", "Write buffer size", root, 418, 110, 90, 13);
        var edWriteBufferSize = MakeInputField("EdWriteBufferSize", root, 507, 109, 83, 20, "");

        // ============================================================
        //  Timeouts column
        // ============================================================
        var btGetTimeouts    = MakeButton("BtGetTimeouts", "Get timeouts", root, 427, 156, 75, 23);
        var btSetTimeouts    = MakeButton("BtSetTimeouts", "Set timeouts", root, 508, 156, 75, 23);
        var laReadInterval   = MakeLabel("LaReadInterval",   "Read interval",   root, 418, 186, 90, 13);
        var edReadInterval   = MakeInputField("EdReadInterval",   root, 507, 185, 84, 20, "");
        var laReadMultiplier = MakeLabel("LaReadMultiplier", "Read multiplier", root, 418, 212, 90, 13);
        var edReadMultiplier = MakeInputField("EdReadMultiplier", root, 508, 211, 83, 20, "");
        var laReadConstant   = MakeLabel("LaReadConstant",   "Read constant",   root, 418, 238, 90, 13);
        var edReadConstant   = MakeInputField("EdReadConstant",   root, 508, 237, 82, 20, "");
        var laWriteMultiplier= MakeLabel("LaWriteMultiplier","Write multiplier",root, 418, 264, 90, 13);
        var edWriteMultiplier= MakeInputField("EdWriteMultiplier",root, 508, 263, 82, 20, "");
        var laWriteConstant  = MakeLabel("LaWriteConstant",  "Write constant",  root, 418, 290, 90, 13);
        var edWriteConstant  = MakeInputField("EdWriteConstant",  root, 508, 289, 83, 20, "");

        // ============================================================
        //  Func / Purge / Transmit / Flush
        // ============================================================
        var laFunc           = MakeLabel("LaFunc", "Function", root, 610, 84, 50, 13);
        var ddFunc           = MakeDropdown("DdFunc", root, 664, 83, 108, 21,
            new[]
            {
                "escClrBreak", "escClrDtr", "escClrRts", "escSetBreak",
                "escSetDtr",   "escSetRts", "escSetXoff", "escSetXon"
            });
        var btFunc           = MakeButton("BtFunc", "Exec func", root, 778, 83, 75, 23);

        var tgPurgeRxAbort   = MakeToggle("TgPurgeRxAbort", "RX abort", root, 613, 109, 70, 17);
        var tgPurgeRxClear   = MakeToggle("TgPurgeRxClear", "RX clear", root, 613, 132, 70, 17);
        var tgPurgeTxAbort   = MakeToggle("TgPurgeTxAbort", "TX abort", root, 687, 109, 70, 17);
        var tgPurgeTxClear   = MakeToggle("TgPurgeTxClear", "TX clear", root, 687, 132, 70, 17);
        var btPurge          = MakeButton("BtPurge", "Purge", root, 776, 120, 75, 23);

        var laCharCode       = MakeLabel("LaCharCode", "Char code (ASCII)", root, 610, 157, 95, 13);
        var edChar           = MakeInputField("EdChar", root, 708, 156, 62, 20, "0");
        var btTransmit       = MakeButton("BtTransmit", "Transmit", root, 776, 155, 75, 23);

        var btFlushBuffers   = MakeButton("BtFlushBuffers", "Flush buffers", root, 697, 192, 75, 23);

        // ============================================================
        //  Send row
        // ============================================================
        var edText      = MakeInputField("EdText", root, 15, 341, 473, 20, "Something to send to serial");
        var btSend      = MakeButton("BtSend", "Send", root, 494, 339, 75, 23);
        var laLineFeed  = MakeLabel("LaLineFeed", "Line feed", root, 575, 342, 55, 13);
        var ddLineFeed  = MakeDropdown("DdLineFeed", root, 632, 341, 121, 21,
            new[] { "None", "CR", "LF", "CR & LF" });
        var btClear     = MakeButton("BtClear", "Clear", root, 776, 339, 75, 23);

        // ============================================================
        //  Events log
        // ============================================================
        var eventsScroll  = MakeScrollView("EventsScroll", root, 15, 367, 836, 186);
        var eventsContent = eventsScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(eventsContent);

        // ============================================================
        //  Prefabs
        // ============================================================
        var eventRow = MakeTableRowPrefab("SerialClientEventRow", new[] { 830f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("SerialClientDemo");
        var demo = demoGO.AddComponent<SerialClientDemo>();

        demo.enumButton           = btEnum;
        demo.portsDropdown        = ddPorts;
        demo.connectButton        = btConnect;
        demo.disconnectButton     = btDisconnect;
        demo.writeTimeoutLabel    = laWriteTimeout;
        demo.writeTimeoutInput    = edWriteTimeout;
        demo.setWriteTimeoutButton= btSetWriteTimeout;

        demo.getConfigButton      = btGetConfig;
        demo.setConfigButton      = btSetConfig;
        demo.getBuffersButton     = btGetBuffers;
        demo.setBuffersButton     = btSetBuffers;
        demo.clearCommBreakButton = btClearCommBreak;
        demo.setCommBreakButton   = btSetCommBreak;

        demo.baudRateLabel   = laBaudRate;
        demo.baudRateInput   = edBaudRate;
        demo.rtsControlLabel = laRtsControl;
        demo.rtsControlDropdown = ddRtsControl;
        demo.parityLabel     = laParity;
        demo.parityDropdown  = ddParity;
        demo.xonLimLabel     = laXonLim;
        demo.xonLimInput     = edXonLim;

        demo.dtrControlLabel = laDtrControl;
        demo.dtrControlDropdown = ddDtrControl;
        demo.byteSizeLabel   = laByteSize;
        demo.byteSizeDropdown= ddByteSize;
        demo.stopBitsLabel   = laStopBits;
        demo.stopBitsDropdown= ddStopBits;
        demo.xoffLimLabel    = laXoffLim;
        demo.xoffLimInput    = edXoffLim;

        demo.xonCharLabel    = laXonChar;
        demo.xonCharInput    = edXonChar;
        demo.xoffCharLabel   = laXoffChar;
        demo.xoffCharInput   = edXoffChar;
        demo.errorCharLabel  = laErrorChar;
        demo.errorCharInput  = edErrorChar;
        demo.eofCharLabel    = laEofChar;
        demo.eofCharInput    = edEofChar;
        demo.evtCharLabel    = laEvtChar;
        demo.evtCharInput    = edEvtChar;

        demo.parityCheckToggle      = tgParityCheck;
        demo.outxDsrFlowToggle      = tgOutxDsrFlow;
        demo.txContinueOnXoffToggle = tgTXContinueOnXoff;
        demo.errorCharReplaceToggle = tgErrorCharReplace;
        demo.nullStripToggle        = tgNullStrip;

        demo.outxCtsFlowToggle      = tgOutxCtsFlow;
        demo.dsrSensitivityToggle   = tgDsrSensitivity;
        demo.outXToggle             = tgOutX;
        demo.inXToggle              = tgInX;
        demo.abortOnErrorToggle     = tgAbortOnError;

        demo.readBufferSizeLabel  = laReadBufferSize;
        demo.readBufferSizeInput  = edReadBufferSize;
        demo.writeBufferSizeLabel = laWriteBufferSize;
        demo.writeBufferSizeInput = edWriteBufferSize;

        demo.getTimeoutsButton = btGetTimeouts;
        demo.setTimeoutsButton = btSetTimeouts;
        demo.readIntervalLabel    = laReadInterval;
        demo.readIntervalInput    = edReadInterval;
        demo.readMultiplierLabel  = laReadMultiplier;
        demo.readMultiplierInput  = edReadMultiplier;
        demo.readConstantLabel    = laReadConstant;
        demo.readConstantInput    = edReadConstant;
        demo.writeMultiplierLabel = laWriteMultiplier;
        demo.writeMultiplierInput = edWriteMultiplier;
        demo.writeConstantLabel   = laWriteConstant;
        demo.writeConstantInput   = edWriteConstant;

        demo.funcLabel = laFunc;
        demo.funcDropdown = ddFunc;
        demo.funcButton = btFunc;
        demo.purgeRxAbortToggle = tgPurgeRxAbort;
        demo.purgeRxClearToggle = tgPurgeRxClear;
        demo.purgeTxAbortToggle = tgPurgeTxAbort;
        demo.purgeTxClearToggle = tgPurgeTxClear;
        demo.purgeButton = btPurge;
        demo.charCodeLabel = laCharCode;
        demo.charInput = edChar;
        demo.transmitButton = btTransmit;
        demo.flushBuffersButton = btFlushBuffers;

        demo.textInput = edText;
        demo.sendButton = btSend;
        demo.lineFeedLabel = laLineFeed;
        demo.lineFeedDropdown = ddLineFeed;
        demo.clearButton = btClear;

        demo.eventsContent = eventsContent;
        demo.eventRowPrefab = eventRow;

        // ============================================================
        //  Save — critical, otherwise the build will ship an empty scene
        // ============================================================
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            Debug.LogWarning("Serial Client: active scene is invalid. " +
                             "Please save the scene manually (Ctrl+S), otherwise the build will be empty.");
        }

        AssetDatabase.Refresh();

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("Serial Client Demo UI built using the original layout. Scene saved.");
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
        label.fontSize = 11;
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
        tRect.sizeDelta = new Vector2(0, 180);

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
        // FIX: alpha must be 1 so Mask does not multiply child alpha to 0.
        vpImg.color = new Color(1, 1, 1, 1f);
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
        ilText.text = "Option"; ilText.color = Color.white; ilText.fontSize = 11;
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

        EditorUtility.SetDirty(dd);
        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(ilText);
        EditorUtility.SetDirty(go);

        return dd;
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
        // FIX: alpha must be 1 so Mask does not multiply child alpha to 0.
        vpImg.color = new Color(1, 1, 1, 1f);
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
        // FIX: match SerialMonitorSceneBuilder — VLG does not control the child
        // width, so the row's own sizeDelta.x defines how wide it is. Without
        // this the rows render with zero width under IL2CPP.
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = false;
        vlg.childControlHeight     = false;

        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static GameObject MakeTableRowPrefab(string name, float[] widths, float rowHeight)
    {
        EnsureAssetFolder(PrefabsFolder);

        var root = CreateUI(name, null);
        var rt = root.GetComponent<RectTransform>();
        // FIX: top-left anchor and a real width matching SerialMonitor's row
        // prefab. Without explicit anchors and sizeDelta the row has zero width.
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);

        float totalWidth = 8;
        for (int i = 0; i < widths.Length; i++) totalWidth += widths[i];

        rt.sizeDelta = new Vector2(totalWidth, rowHeight);

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

        string assetPath = PrefabsFolder + "/" + name + ".prefab";

        // Delete the old prefab (if any) so SaveAsPrefabAsset doesn't complain
        if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (prefab == null)
            Debug.LogError($"Serial Client: failed to create prefab at {assetPath}");

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

    /// <summary>
    /// Ensures the folder exists AND is registered in the AssetDatabase.
    /// Directory.CreateDirectory alone is not enough — Unity will not see the new folder.
    /// </summary>
    private static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;

        // assetFolder is expected in the form "Assets/Foo" or "Assets/Foo/Bar"
        string parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
        string leaf   = Path.GetFileName(assetFolder);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

        // Recursively ensure the parent exists first
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureAssetFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
        AssetDatabase.Refresh();
    }
}
#endif