#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WiFiDirectSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 484 x 582
    private const float CanvasW = 504f;
    private const float CanvasH = 620f;

    [MenuItem("Tools/WiFi Direct/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("WiFiDirectUI");
        DestroyIfExists("WiFiDirectDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("WiFiDirectUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Row 1: Discoverability | Pairing procedure
        // ============================================================
        MakeLabel("LaDiscoverability", "Discoverability", root, 12, 13, 100, 13);
        var ddDiscoverability = MakeDropdown("DdDiscoverability", root, 93, 12, 101, 21,
            new[] { "Intensive", "None", "Normal" });

        MakeLabel("LaPairingProcedure", "Pairing procedure", root, 214, 13, 100, 13);
        var ddPairingProcedure = MakeDropdown("DdPairingProcedure", root, 310, 12, 162, 21,
            new[] { "ppInvitation", "ppGroupOwnerNegotiation" });

        // ============================================================
        //  Row 2: Autonomous Group Owner
        // ============================================================
        var tgAutonomous = MakeToggle("TgAutonomous", "Autonomous Group Owner", root, 43, 37, 151, 17);

        // ============================================================
        //  Row 3: Group owner intent
        // ============================================================
        MakeLabel("LaGroupOwnerIntent", "Group owner intent", root, 12, 63, 100, 13);
        var edGroupOwnerIntent = MakeInputField("EdGroupOwnerIntent", root, 115, 62, 79, 20, "14");

        // ============================================================
        //  Row 4: Configuration methods
        // ============================================================
        MakeLabel("LaConfigurationMethods", "Configuration methods", root, 214, 41, 130, 13);

        var tgDisplayPin = MakeToggle("TgDisplayPin", "Display PIN", root, 245, 62, 100, 17);
        var tgProvidePin = MakeToggle("TgProvidePin", "Provide PIN", root, 243, 85, 100, 17);
        var tgPushButton = MakeToggle("TgPushButton", "Push Button", root, 242, 108, 100, 17);

        // ============================================================
        //  Row 5: Start | Stop
        // ============================================================
        var btStart = MakeButton("BtStart", "Start", root, 12, 141, 75, 23);
        var btStop  = MakeButton("BtStop",  "Stop",  root, 93, 141, 75, 23);

        // ============================================================
        //  Devices table (12, 170) 379x171
        //  135 | 135 | 80
        // ============================================================
        var devWidths  = new[] { 135f, 135f, 80f };
        var devHeaders = new[] { "Device name", "ID", "Status" };

        MakeHeaderBar("DevicesHeader", root, 12, 170, 379, 20, devWidths, devHeaders);

        var devicesScroll  = MakeScrollView("DevicesScroll", root, 12, 190, 379, 151, false);
        var devicesContent = devicesScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(devicesContent);

        var btDisconnect = MakeButton("BtDisconnect", "Disconnect", root, 397, 170, 75, 23);

        // ============================================================
        //  Clear + Log
        //  btClear (397, 318) 75x23
        //  lbLog   (15, 347) 457x225
        // ============================================================
        var btClear = MakeButton("BtClear", "Clear", root, 397, 318, 75, 23);

        var logScroll  = MakeScrollView("LogScroll", root, 15, 347, 457, 225, false);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent);

        // ============================================================
        //  Confirm panel (Yes / No)
        // ============================================================
        var (confirmPanel, confirmText, confirmYes, confirmNo) =
            MakeConfirmDialog(root, 500, 180,
                new[] { "YES", "NO" },
                out Button[] confirmButtons);

        // ============================================================
        //  PIN input panel
        // ============================================================
        var (pinPanel, pinText, pinInput, pinOk, pinCancel) =
            MakePinDialog(root);

        // ============================================================
        //  Prefabs
        // ============================================================
        var deviceRow = MakeTableRowPrefab("WiFiDirectDeviceRow", devWidths, 22f);
        var logRow    = MakeTableRowPrefab("WiFiDirectLogRow",    new[] { 450f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("WiFiDirectDemo");
        var demo = demoGO.AddComponent<WiFiDirectDemo>();

        demo.discoverabilityDropdown     = ddDiscoverability;
        demo.pairingProcedureDropdown    = ddPairingProcedure;
        demo.autonomousGroupOwnerToggle  = tgAutonomous;
        demo.groupOwnerIntentInput       = edGroupOwnerIntent;

        demo.displayPinToggle = tgDisplayPin;
        demo.providePinToggle = tgProvidePin;
        demo.pushButtonToggle = tgPushButton;

        demo.startButton = btStart;
        demo.stopButton  = btStop;

        demo.devicesContent   = devicesContent;
        demo.deviceRowPrefab  = deviceRow;
        demo.disconnectButton = btDisconnect;

        demo.logContent     = logContent;
        demo.logRowPrefab   = logRow;
        demo.clearLogButton = btClear;

        demo.confirmPanel     = confirmPanel;
        demo.confirmText      = confirmText;
        demo.confirmYesButton = confirmYes;
        demo.confirmNoButton  = confirmNo;

        demo.pinPanel      = pinPanel;
        demo.pinText       = pinText;
        demo.pinInput      = pinInput;
        demo.pinOkButton   = pinOk;
        demo.pinCancelButton = pinCancel;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Wi-Fi Direct Demo UI built using the original layout.");
    }

    // ================================================================
    //  Confirm dialog (Yes/No)
    // ================================================================
    private static (GameObject panel, TMP_Text text, Button yes, Button no)
        MakeConfirmDialog(Transform parent, float w, float h,
            string[] labels, out Button[] buttons)
    {
        var panel = CreateUI("ConfirmPanel", parent);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);

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

        var list = new System.Collections.Generic.List<Button>();
        for (int i = 0; i < labels.Length; i++)
            list.Add(MakeButton("Btn" + labels[i], labels[i], bar.transform, 0, 0, 140, 40));

        panel.SetActive(false);
        buttons = list.ToArray();

        return (panel, txt, list[0], list.Count > 1 ? list[1] : null);
    }

    // ================================================================
    //  PIN dialog
    // ================================================================
    private static (GameObject panel, TMP_Text text, TMP_InputField input, Button ok, Button cancel)
        MakePinDialog(Transform parent)
    {
        var panel = CreateUI("PinPanel", parent);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 200);

        var img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var txtGO = CreateUI("Text", panel.transform);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = new Vector2(0, 0.7f); trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(20, 0); trt.offsetMax = new Vector2(-20, -10);

        var edPin = MakeInputField("EdPin", panel.transform, 100, 80, 300, 30, "");

        var bar = CreateUI("Buttons", panel.transform);
        var br = bar.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0.3f);
        br.offsetMin = new Vector2(20, 15); br.offsetMax = new Vector2(-20, 0);
        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false; hlg.childControlHeight = false;

        var ok     = MakeButton("BtnOk",     "OK",     bar.transform, 0, 0, 140, 40);
        var cancel = MakeButton("BtnCancel", "CANCEL", bar.transform, 0, 0, 140, 40);

        panel.SetActive(false);
        return (panel, txt, edPin, ok, cancel);
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

        template.SetActive(false);

        EditorUtility.SetDirty(dd);
        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(ilText);
        EditorUtility.SetDirty(go);

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