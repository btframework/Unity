#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class AudioSwitcherSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string PrefabPath    = PrefabsFolder + "/AudioSwitcherRow.prefab";

    // Ширины колонок в таблице устройств (px)
    private static readonly (string header, float width)[] Columns = new[]
    {
        ("ID",            180f),
        ("Friendly name", 140f),
        ("Description",   140f),
        ("Flow",           80f),
        ("Roles",         150f),
        ("States",         90f),
        ("MAC",           110f),
        ("Service",        80f),
    };

    [MenuItem("Tools/Audio Switcher/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("AudioSwitcherUI");
        DestroyIfExists("AudioSwitcherDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // ============================================================
        //  Canvas
        // ============================================================
        var canvasGO = CreateUI("AudioSwitcherUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ============================================================
        //  Row 1: Enum | Role+Dropdown | SetDefault | Refresh | Flow+Dropdown | GetDefault
        // ============================================================
        var row1 = CreateHorizontalBar("Row1", canvasGO.transform,
            new Vector2(20, -20), new Vector2(-20, -20), 44);

        Button btnEnum = CreateButton("BtnEnum", "Enum", row1.transform, 110, 40);
        CreateLabel("LblRole", "Role", row1.transform, 60, 40);
        TMP_Dropdown ddRole = CreateDropdown("DdRole", row1.transform, 150, 40,
            new[] { "Console", "Multimedia", "Communications" });
        Button btnSetDefault = CreateButton("BtnSetDefault", "Set default", row1.transform, 140, 40);
        Button btnRefresh    = CreateButton("BtnRefresh",    "Refresh",     row1.transform, 120, 40);
        CreateLabel("LblFlow", "Flow", row1.transform, 60, 40);
        TMP_Dropdown ddFlow = CreateDropdown("DdFlow", row1.transform, 150, 40,
            new[] { "Capture", "Render" });
        Button btnGetDefault = CreateButton("BtnGetDefault", "GetDefault", row1.transform, 140, 40);

        // ============================================================
        //  Row 2: Toggles Active/Disabled/Unplugged/NotPresent + Connect/Disconnect + UseMAC
        // ============================================================
        var row2 = CreateHorizontalBar("Row2", canvasGO.transform,
            new Vector2(20, -74), new Vector2(-20, -74), 34);

        Toggle tgActive     = CreateToggle("TgActive",     "Active",      row2.transform, 120);
        Toggle tgDisabled   = CreateToggle("TgDisabled",   "Disabled",    row2.transform, 130);
        Toggle tgUnplugged  = CreateToggle("TgUnplugged",  "Unplugged",   row2.transform, 140);
        Toggle tgNotPresent = CreateToggle("TgNotPresent", "Not present", row2.transform, 150);
        Button btnConnect    = CreateButton("BtnConnect",    "Connect",    row2.transform, 130, 30);
        Button btnDisconnect = CreateButton("BtnDisconnect", "Disconnect", row2.transform, 130, 30);
        Toggle tgUseMac      = CreateToggle("TgUseMac",      "Use MAC",    row2.transform, 120);

        // ============================================================
        //  Headers
        // ============================================================
        var headerRow = CreateHorizontalBar("Headers", canvasGO.transform,
            new Vector2(20, -116), new Vector2(-20, -116), 30);

        foreach (var col in Columns)
        {
            var h = CreateText(col.header, col.header, headerRow.transform);
            h.fontSize = 18;
            h.fontStyle = FontStyles.Bold;
            h.alignment = TextAlignmentOptions.MidlineLeft;
            var le = h.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = col.width;
            le.minWidth       = col.width;
        }

        // ============================================================
        //  DevicesScroll
        // ============================================================
        var devicesScroll = CreateScrollView("DevicesScroll", canvasGO.transform);
        var devRect = devicesScroll.GetComponent<RectTransform>();
        devRect.anchorMin = new Vector2(0, 1);
        devRect.anchorMax = new Vector2(1, 1);
        devRect.pivot     = new Vector2(0.5f, 1);
        devRect.anchoredPosition = new Vector2(0, -150);
        devRect.sizeDelta = new Vector2(-40, 260);

        var devicesContent = devicesScroll.transform.Find("Viewport/Content");
        var devVlg = devicesContent.gameObject.AddComponent<VerticalLayoutGroup>();
        devVlg.spacing = 2;
        devVlg.padding = new RectOffset(4, 4, 4, 4);
        devVlg.childForceExpandWidth  = true;
        devVlg.childForceExpandHeight = false;
        devVlg.childControlWidth  = true;
        devVlg.childControlHeight = true;

        var devCsf = devicesContent.gameObject.AddComponent<ContentSizeFitter>();
        devCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ============================================================
        //  LogScroll + Clear
        // ============================================================
        var logScroll = CreateScrollView("LogScroll", canvasGO.transform);
        var logRect = logScroll.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 1);
        logRect.offsetMin = new Vector2(20, 70);
        logRect.offsetMax = new Vector2(-20, -420);

        var logContent = logScroll.transform.Find("Viewport/Content");
        var logCsf = logContent.gameObject.AddComponent<ContentSizeFitter>();
        logCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var logText = CreateText("LogText", "", logContent);
        logText.enableWordWrapping = true;
        logText.margin = new Vector4(8, 8, 8, 8);
        var ltRect = logText.rectTransform;
        ltRect.anchorMin = new Vector2(0, 1);
        ltRect.anchorMax = new Vector2(1, 1);
        ltRect.pivot     = new Vector2(0.5f, 1);
        ltRect.anchoredPosition = Vector2.zero;
        ltRect.sizeDelta = Vector2.zero;
        ltTextCsf(logText);

        // Кнопка Clear в правом нижнем углу
        var clearBtn = CreateButton("BtnClear", "Clear", canvasGO.transform, 110, 40);
        var clearRect = clearBtn.GetComponent<RectTransform>();
        clearRect.anchorMin = new Vector2(1, 0);
        clearRect.anchorMax = new Vector2(1, 0);
        clearRect.pivot     = new Vector2(1, 0);
        clearRect.anchoredPosition = new Vector2(-20, 20);

        // ============================================================
        //  Префаб строки таблицы
        // ============================================================
        var rowPrefab = CreateDeviceRowPrefab();

        // ============================================================
        //  Главный GameObject + связи
        // ============================================================
        var demoGO = new GameObject("AudioSwitcherDemo");
        var demo = demoGO.AddComponent<AudioSwitcherDemo>();

        demo.enumButton       = btnEnum;
        demo.roleDropdown     = ddRole;
        demo.setDefaultButton = btnSetDefault;
        demo.refreshButton    = btnRefresh;
        demo.flowDropdown     = ddFlow;
        demo.getDefaultButton = btnGetDefault;

        demo.activeToggle     = tgActive;
        demo.disabledToggle   = tgDisabled;
        demo.unpluggedToggle  = tgUnplugged;
        demo.notPresentToggle = tgNotPresent;

        demo.connectButton    = btnConnect;
        demo.disconnectButton = btnDisconnect;
        demo.useMacToggle     = tgUseMac;

        demo.devicesContent   = devicesContent;
        demo.deviceRowPrefab  = rowPrefab;

        demo.logText          = logText;
        demo.logScrollRect    = logScroll.GetComponent<ScrollRect>();
        demo.clearButton      = clearBtn;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("? Audio Switcher Demo UI собрана. Нажми Play.");
    }

    // ================================================================
    //  Row prefab
    // ================================================================
    private static GameObject CreateDeviceRowPrefab()
    {
        if (!Directory.Exists(PrefabsFolder))
            Directory.CreateDirectory(PrefabsFolder);

        var row = CreateUI("AudioSwitcherRow", null);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 34);

        var img = row.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);

        row.AddComponent<Button>();

        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 34;
        le.preferredHeight = 34;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;

        TMP_Text MakeColumn(string name, float width)
        {
            var t = CreateText(name, "", row.transform);
            t.fontSize = 16;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            var l = t.gameObject.AddComponent<LayoutElement>();
            l.preferredWidth = width;
            l.minWidth       = width;
            return t;
        }

        var rowUI = row.AddComponent<AudioSwitcherRowUI>();
        rowUI.idText           = MakeColumn("IdText",           Columns[0].width);
        rowUI.friendlyNameText = MakeColumn("FriendlyNameText", Columns[1].width);
        rowUI.descriptionText  = MakeColumn("DescriptionText",  Columns[2].width);
        rowUI.flowText         = MakeColumn("FlowText",         Columns[3].width);
        rowUI.rolesText        = MakeColumn("RolesText",        Columns[4].width);
        rowUI.stateText        = MakeColumn("StateText",        Columns[5].width);
        rowUI.macText          = MakeColumn("MacText",          Columns[6].width);
        rowUI.serviceText      = MakeColumn("ServiceText",      Columns[7].width);
        rowUI.background       = img;

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, PrefabPath);
        Object.DestroyImmediate(row);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ================================================================
    //  Helpers
    // ================================================================
    private static void ltTextCsf(TMP_Text t)
    {
        var csf = t.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

    private static GameObject CreateHorizontalBar(string name, Transform parent,
        Vector2 topLeft, Vector2 topRight, float height)
    {
        var bar = CreateUI(name, parent);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, topLeft.y);
        rt.sizeDelta = new Vector2(-40, height);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;

        return bar;
    }

    private static TMP_Text CreateLabel(string name, string text, Transform parent,
        float width, float height)
    {
        var t = CreateText(name, text, parent);
        t.fontSize = 20;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth       = width;
        le.preferredHeight = height;
        return t;
    }

    private static TextMeshProUGUI CreateText(string name, string content, Transform parent)
    {
        var go = CreateUI(name, parent);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.color = Color.white;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private static Button CreateButton(string name, string label, Transform parent,
        float width, float height)
    {
        var go = CreateUI(name, parent);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor     = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.minWidth        = width;
        le.preferredHeight = height;
        le.minHeight       = height;

        var textGO = CreateUI("Text", go.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 20;

        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    private static Toggle CreateToggle(string name, string label, Transform parent, float width)
    {
        var go = CreateUI(name, parent);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth       = width;
        le.preferredHeight = 30;
        le.minHeight       = 30;

        var toggle = go.AddComponent<Toggle>();

        // Background
        var bg = CreateUI("Background", go.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.pivot     = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(4, 0);
        bgRect.sizeDelta = new Vector2(20, 20);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Checkmark
        var cm = CreateUI("Checkmark", bg.transform);
        var cmRect = cm.GetComponent<RectTransform>();
        cmRect.anchorMin = new Vector2(0.5f, 0.5f);
        cmRect.anchorMax = new Vector2(0.5f, 0.5f);
        cmRect.pivot     = new Vector2(0.5f, 0.5f);
        cmRect.anchoredPosition = Vector2.zero;
        cmRect.sizeDelta = new Vector2(12, 12);
        var cmImg = cm.AddComponent<Image>();
        cmImg.color = new Color(0.4f, 0.7f, 1f, 1f);

        // Label
        var lbl = CreateText("Label", label, go.transform);
        lbl.fontSize = 18;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        var lRect = lbl.rectTransform;
        lRect.anchorMin = new Vector2(0, 0);
        lRect.anchorMax = new Vector2(1, 1);
        lRect.offsetMin = new Vector2(30, 0);
        lRect.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic       = cmImg;
        toggle.isOn = false;

        return toggle;
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent,
        float width, float height, string[] options)
    {
        var go = CreateUI(name, parent);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.minWidth        = width;
        le.preferredHeight = height;
        le.minHeight       = height;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 1f);

        var dd = go.AddComponent<TMP_Dropdown>();

        // Caption label (показывает текущее значение)
        var labelGO = CreateUI("Label", go.transform);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = options.Length > 0 ? options[0] : "";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.fontSize = 18;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = new Vector2(-25, 0);

        // Arrow (простой квадратик)
        var arrowGO = CreateUI("Arrow", go.transform);
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        var arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot     = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-8, 0);
        arrowRect.sizeDelta = new Vector2(12, 12);

        // Template (всплывающий список)
        var template = CreateUI("Template", go.transform);
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot     = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 160);

        var templateImg = template.AddComponent<Image>();
        templateImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var templateScroll = template.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;
        var templateMask = template.AddComponent<Mask>();
        templateMask.showMaskGraphic = true;

        // Viewport
        var viewport = CreateUI("Viewport", template.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        // Content
        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot     = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 28);

        // Item
        var item = CreateUI("Item", content.transform);
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.pivot     = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0, 28);

        var itemToggle = item.AddComponent<Toggle>();

        var itemBg = CreateUI("Item Background", item.transform);
        var ibRect = itemBg.GetComponent<RectTransform>();
        ibRect.anchorMin = Vector2.zero;
        ibRect.anchorMax = Vector2.one;
        ibRect.offsetMin = Vector2.zero;
        ibRect.offsetMax = Vector2.zero;
        var ibImg = itemBg.AddComponent<Image>();
        ibImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var itemCheck = CreateUI("Item Checkmark", item.transform);
        var icRect = itemCheck.GetComponent<RectTransform>();
        icRect.anchorMin = new Vector2(0, 0.5f);
        icRect.anchorMax = new Vector2(0, 0.5f);
        icRect.pivot     = new Vector2(0, 0.5f);
        icRect.anchoredPosition = new Vector2(10, 0);
        icRect.sizeDelta = new Vector2(14, 14);
        var icImg = itemCheck.AddComponent<Image>();
        icImg.color = new Color(0.4f, 0.7f, 1f, 1f);

        var itemLabel = CreateUI("Item Label", item.transform);
        var ilRect = itemLabel.GetComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero;
        ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(28, 0);
        ilRect.offsetMax = new Vector2(-10, 0);
        var ilText = itemLabel.AddComponent<TextMeshProUGUI>();
        ilText.text = "Option";
        ilText.color = Color.white;
        ilText.fontSize = 18;
        ilText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = ibImg;
        itemToggle.graphic       = icImg;

        templateScroll.viewport = vpRect;
        templateScroll.content  = cRect;

        dd.template    = templateRect;
        dd.captionText = label;
        dd.itemText    = ilText;

        dd.options.Clear();
        foreach (var o in options)
            dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();

        template.SetActive(false);

        return dd;
    }

    private static GameObject CreateScrollView(string name, Transform parent)
    {
        var root = CreateUI(name, parent);

        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        var scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUI("Viewport", root.transform);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;

        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;

        var content = CreateUI("Content", viewport.transform);
        var cRect = content.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot     = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = Vector2.zero;

        scrollRect.viewport = vpRect;
        scrollRect.content  = cRect;

        return root;
    }
}
#endif