#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WiiRemoteSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 650 x 590 (+ 20 margin)
    private const float CanvasW = 680f;
    private const float CanvasH = 620f;

    [MenuItem("Tools/Wii Remote/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("WiiRemoteUI");
        DestroyIfExists("WiiRemoteDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("WiiRemoteUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Top bar (Discovery / Connection / Battery)
        // ============================================================
        var btDiscover   = MakeButton("BtDiscover",   "Discover",    root, 12,  12,  75, 23);
        var btConnect    = MakeButton("BtConnect",    "Connect",     root, 183, 12,  75, 23);
        var btDisconnect = MakeButton("BtDisconnect", "Disconnect",  root, 264, 12,  75, 23);

        MakeLabel("LaBattLevelCaption", "Batt level:", root, 361, 15, 54, 13);
        var laBattLevel = MakeLabel("LaBattLevel", "0.00", root, 421, 15, 40, 13);

        var btGetLeds   = MakeButton("BtGetLeds",  "Get LEDs",   root, 364, 41, 75, 23);
        var btGetStatus = MakeButton("BtGetStatus","Get status", root, 473, 12, 75, 23);
        var btSetLeds   = MakeButton("BtSetLeds",  "Set LEDs",   root, 473, 41, 75, 23);
        var btRumbleOn  = MakeButton("BtRumbleOn", "Rumble ON",  root, 565, 12, 75, 23);
        var btRumbleOff = MakeButton("BtRumbleOff","Rumble OFF", root, 565, 41, 75, 23);

        // ============================================================
        //  Devices table (12,41) 327x97, columns 150 | 150
        // ============================================================
        MakeHeaderBar("DevicesHeader", root, 12, 41, 327, 20,
            new[] { 150f, 150f }, new[] { "Address", "Name" });

        var devicesScroll  = MakeScrollView("DevicesScroll", root, 12, 61, 327, 77);
        var devicesContent = devicesScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(devicesContent);

        // ============================================================
        //  LEDs (4 toggles, 2x2)
        // ============================================================
        var tgLed1 = MakeToggle("CbLed1", "LED1", root, 376, 70, 53, 17);
        var tgLed2 = MakeToggle("CbLed2", "LED2", root, 376, 93, 53, 17);
        var tgLed3 = MakeToggle("CbLed3", "LED3", root, 435, 70, 53, 17);
        var tgLed4 = MakeToggle("CbLed4", "LED4", root, 435, 93, 53, 17);

        // ============================================================
        //  Buttons group (the Table: A..Right)
        // ============================================================
        MakeBoldLabel("LaButtons", "Buttons", root, 12, 154, 60, 15);

        var tgA     = MakeToggle("CbA",     "A",     root, 25,  180, 71, 17);
        var tgB     = MakeToggle("CbB",     "B",     root, 25,  203, 71, 17);
        var tgPlus  = MakeToggle("CbPlus",  "Plus",  root, 25,  226, 71, 17);
        var tgHome  = MakeToggle("CbHome",  "Home",  root, 25,  249, 71, 17);
        var tgMinus = MakeToggle("CbMinus", "Minus", root, 25,  272, 71, 17);
        var tgOne   = MakeToggle("CbOne",   "One",   root, 102, 180, 68, 17);
        var tgTwo   = MakeToggle("CbTwo",   "Two",   root, 102, 203, 68, 17);
        var tgUp    = MakeToggle("CbUp",    "Up",    root, 102, 226, 68, 17);
        var tgDown  = MakeToggle("CbDown",  "Down",  root, 102, 249, 68, 17);
        var tgLeft  = MakeToggle("CbLeft",  "Left",  root, 102, 272, 68, 17);
        var tgRight = MakeToggle("CbRight", "Right", root, 40,  295, 56, 17);

        // ============================================================
        //  Accelerometer
        // ============================================================
        MakeBoldLabel("LaAccel", "Accel", root, 12, 339, 40, 15);

        MakeLabel("LaXCaption", "X:", root, 22, 363, 20, 13);
        MakeLabel("LaYCaption", "Y:", root, 22, 388, 20, 13);
        MakeLabel("LaZCaption", "Z:", root, 22, 412, 20, 13);
        var laX = MakeLabel("LaX", "0", root, 45, 363, 30, 13);
        var laY = MakeLabel("LaY", "0", root, 45, 388, 30, 13);
        var laZ = MakeLabel("LaZ", "0", root, 45, 412, 30, 13);

        var btEnableAccel  = MakeButton("BtEnableAccel", "Enable",  root, 102, 360, 75, 23);
        var btDisableAccel = MakeButton("BtDisableAccel","Disable", root, 102, 390, 75, 23);

        // ============================================================
        //  IR
        // ============================================================
        MakeBoldLabel("LaIrCaption", "IR", root, 223, 154, 25, 15);
        MakeLabel("LaIrModeCaption", "Mode:", root, 223, 181, 45, 13);
        var laIrMode = MakeLabel("LaIrMode", "Off", root, 293, 183, 40, 13);
        MakeLabel("LaIrSensitivityCaption", "Sensitivity:", root, 350, 180, 80, 13);
        var laIrSensitivity = MakeLabel("LaIrSensitivity", "Off", root, 414, 180, 60, 13);

        // Sensors
        MakeLabel("LaSensor0Caption", "Sensor 0", root, 223, 203, 60, 13);
        MakeLabel("LaSensor0PositionCaption", "Position: ", root, 237, 226, 60, 13);
        MakeLabel("LaSensor0SizeCaption", "Size:", root, 237, 249, 40, 13);
        MakeLabel("LaSensor0FoundCaption", "Found:", root, 237, 272, 50, 13);
        var laSensor0Position = MakeLabel("LaSensor0Position", "0; 0", root, 293, 226, 60, 13);
        var laSensor0Size     = MakeLabel("LaSensor0Size",     "0",    root, 293, 249, 40, 13);
        var laSensor0Found    = MakeLabel("LaSensor0Found",    "False",root, 293, 272, 50, 13);

        MakeLabel("LaSensor1Caption", "Sensor 1", root, 350, 203, 60, 13);
        MakeLabel("LaSensor1PositionCaption", "Position: ", root, 364, 226, 60, 13);
        MakeLabel("LaSensor1SizeCaption", "Size:", root, 364, 249, 40, 13);
        MakeLabel("LaSensor1FoundCaption", "Found:", root, 364, 272, 50, 13);
        var laSensor1Position = MakeLabel("LaSensor1Position", "0; 0", root, 420, 226, 60, 13);
        var laSensor1Size     = MakeLabel("LaSensor1Size",     "0",    root, 420, 249, 40, 13);
        var laSensor1Found    = MakeLabel("LaSensor1Found",    "False",root, 420, 272, 50, 13);

        MakeLabel("LaSensor2Caption", "Sensor 2", root, 223, 295, 60, 13);
        MakeLabel("LaSensor2PositionCaption", "Position: ", root, 237, 319, 60, 13);
        MakeLabel("LaSensor2SizeCaption", "Size:", root, 237, 342, 40, 13);
        MakeLabel("LaSensor2FoundCaption", "Found:", root, 237, 365, 50, 13);
        var laSensor2Position = MakeLabel("LaSensor2Position", "0; 0", root, 293, 319, 60, 13);
        var laSensor2Size     = MakeLabel("LaSensor2Size",     "0",    root, 293, 342, 40, 13);
        var laSensor2Found    = MakeLabel("LaSensor2Found",    "False",root, 293, 365, 50, 13);

        MakeLabel("LaSensor3Caption", "Sensor 3", root, 350, 295, 60, 13);
        MakeLabel("LaSensor3PositionCaption", "Position: ", root, 364, 319, 60, 13);
        MakeLabel("LaSensor3SizeCaption", "Size:", root, 364, 342, 40, 13);
        MakeLabel("LaSensor3FoundCaption", "Found:", root, 364, 365, 50, 13);
        var laSensor3Position = MakeLabel("LaSensor3Position", "0; 0", root, 420, 319, 60, 13);
        var laSensor3Size     = MakeLabel("LaSensor3Size",     "0",    root, 420, 342, 40, 13);
        var laSensor3Found    = MakeLabel("LaSensor3Found",    "False",root, 420, 365, 50, 13);

        var btEnableIr  = MakeButton("BtEnableIr", "Enable",  root, 226, 390, 75, 23);
        var btDisableIr = MakeButton("BtDisableIr","Disable", root, 307, 390, 75, 23);

        // ============================================================
        //  Nunchuk
        // ============================================================
        MakeBoldLabel("LaNunchukCaption", "Nunchuk", root, 492, 154, 100, 15);
        var laNunchukState = MakeLabel("LaNunchukState", "Detached", root, 584, 154, 70, 13);

        MakeLabel("LaNunchukXCaption", "X:", root, 513, 180, 20, 13);
        MakeLabel("LaNunchukYCaption", "Y:", root, 513, 204, 20, 13);
        MakeLabel("LaNunchukZCaption", "Z:", root, 513, 226, 20, 13);
        MakeLabel("LaNunchukJoyXCaption", "Joy X:", root, 513, 249, 45, 13);
        MakeLabel("LaNunchukJoyYCaption", "Joy Y:", root, 513, 272, 45, 13);

        var laNunchukX    = MakeLabel("LaNunchukX",    "0", root, 536, 181, 40, 13);
        var laNunchukY    = MakeLabel("LaNunchukY",    "0", root, 536, 203, 40, 13);
        var laNunchukZ    = MakeLabel("LaNunchukZ",    "0", root, 536, 226, 40, 13);
        var laNunchukJoyX = MakeLabel("LaNunchukJoyX", "0", root, 555, 249, 40, 13);
        var laNunchukJoyY = MakeLabel("LaNunchukJoyY", "0", root, 555, 272, 40, 13);

        var tgNunchukC = MakeToggle("CbNunchukC", "C", root, 598, 180, 33, 17);
        var tgNunchukZ = MakeToggle("CbNunchukZ", "Z", root, 598, 202, 33, 17);

        // ============================================================
        //  Balance board
        // ============================================================
        MakeBoldLabel("LaBalanceBoardCaption", "Balance board", root, 492, 299, 100, 15);
        var laBalanceBoardState = MakeLabel("LaBalanceBoardState", "Detached", root, 587, 299, 70, 13);

        MakeLabel("LaBbTopRightCaption", "Top right:", root, 513, 319, 80, 13);
        MakeLabel("LaBbTopLeftCaption", "Top left:", root, 515, 342, 80, 13);
        MakeLabel("LaBbBottomRightCaption", "Bottom right:", root, 515, 365, 90, 13);
        MakeLabel("LaBbBottomLeftCaption", "Bottom left:", root, 515, 388, 85, 13);

        var laBbTopRight    = MakeLabel("LaBbTopRight",    "0", root, 596, 319, 40, 13);
        var laBbTopLeft     = MakeLabel("LaBbTopLeft",     "0", root, 595, 342, 40, 13);
        var laBbBottomRight = MakeLabel("LaBbBottomRight", "0", root, 595, 365, 40, 13);
        var laBbBottomLeft  = MakeLabel("LaBbBottomLeft",  "0", root, 595, 388, 40, 13);

        // ============================================================
        //  Log + Clear
        // ============================================================
        var logScroll  = MakeScrollView("LogScroll", root, 12, 482, 626, 95);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent);

        var btClear = MakeButton("BtClear", "Clear", root, 12, 453, 75, 23);

        // ============================================================
        //  Prefabs
        // ============================================================
        var deviceRow = MakeTableRowPrefab("WiiDeviceRow", new[] { 150f, 150f }, 22f);
        var logRow    = MakeTableRowPrefab("WiiLogRow",    new[] { 620f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("WiiRemoteDemo");
        var demo = demoGO.AddComponent<WiiRemoteDemo>();

        demo.discoverButton   = btDiscover;
        demo.connectButton    = btConnect;
        demo.disconnectButton = btDisconnect;
        demo.battLevelLabel   = laBattLevel;

        demo.getLedsButton   = btGetLeds;
        demo.getStatusButton = btGetStatus;
        demo.setLedsButton   = btSetLeds;
        demo.rumbleOnButton  = btRumbleOn;
        demo.rumbleOffButton = btRumbleOff;

        demo.led1Toggle = tgLed1;
        demo.led2Toggle = tgLed2;
        demo.led3Toggle = tgLed3;
        demo.led4Toggle = tgLed4;

        demo.devicesContent  = devicesContent;
        demo.deviceRowPrefab = deviceRow;

        demo.aToggle     = tgA;
        demo.bToggle     = tgB;
        demo.plusToggle  = tgPlus;
        demo.homeToggle  = tgHome;
        demo.minusToggle = tgMinus;
        demo.oneToggle   = tgOne;
        demo.twoToggle   = tgTwo;
        demo.upToggle    = tgUp;
        demo.downToggle  = tgDown;
        demo.leftToggle  = tgLeft;
        demo.rightToggle = tgRight;

        demo.accelXLabel       = laX;
        demo.accelYLabel       = laY;
        demo.accelZLabel       = laZ;
        demo.enableAccelButton = btEnableAccel;
        demo.disableAccelButton= btDisableAccel;

        demo.irModeLabel        = laIrMode;
        demo.irSensitivityLabel = laIrSensitivity;

        demo.irSensor0PositionLabel = laSensor0Position;
        demo.irSensor0SizeLabel     = laSensor0Size;
        demo.irSensor0FoundLabel    = laSensor0Found;

        demo.irSensor1PositionLabel = laSensor1Position;
        demo.irSensor1SizeLabel     = laSensor1Size;
        demo.irSensor1FoundLabel    = laSensor1Found;

        demo.irSensor2PositionLabel = laSensor2Position;
        demo.irSensor2SizeLabel     = laSensor2Size;
        demo.irSensor2FoundLabel    = laSensor2Found;

        demo.irSensor3PositionLabel = laSensor3Position;
        demo.irSensor3SizeLabel     = laSensor3Size;
        demo.irSensor3FoundLabel    = laSensor3Found;

        demo.enableIrButton  = btEnableIr;
        demo.disableIrButton = btDisableIr;

        demo.nunchukStateLabel = laNunchukState;
        demo.nunchukXLabel     = laNunchukX;
        demo.nunchukYLabel     = laNunchukY;
        demo.nunchukZLabel     = laNunchukZ;
        demo.nunchukJoyXLabel  = laNunchukJoyX;
        demo.nunchukJoyYLabel  = laNunchukJoyY;
        demo.nunchukCToggle    = tgNunchukC;
        demo.nunchukZToggle    = tgNunchukZ;

        demo.balanceBoardStateLabel = laBalanceBoardState;
        demo.bbTopRightLabel        = laBbTopRight;
        demo.bbTopLeftLabel         = laBbTopLeft;
        demo.bbBottomRightLabel     = laBbBottomRight;
        demo.bbBottomLeftLabel      = laBbBottomLeft;

        demo.logContent     = logContent;
        demo.logRowPrefab   = logRow;
        demo.clearLogButton = btClear;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("Wii Remote Demo UI built using the original layout.");
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

    private static TMP_Text MakeBoldLabel(string name, string text, Transform parent,
        float x, float y, float w, float h)
    {
        var t = MakeLabel(name, text, parent, x, y, w, h);
        t.fontStyle = FontStyles.Bold;
        return t;
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