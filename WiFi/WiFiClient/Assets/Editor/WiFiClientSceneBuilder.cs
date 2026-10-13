#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WiFiClientSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 879 x 619. We add the log below, so the canvas is taller.
    private const float CanvasW = 899f;
    private const float CanvasH = 760f;

    [MenuItem("Tools/WiFi Client/Build Scene")]
    public static void BuildScene()
    {
        DestroyIfExists("WiFiClientUI");
        DestroyIfExists("WiFiClientDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        var canvasGO = CreateUI("WiFiClientUI", null);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var root = canvasGO.transform;

        // ============================================================
        //  Row 1 - client / interfaces / IP
        // ============================================================
        var btOpen          = MakeButton("BtOpen",          "Open",          root, 12,  12, 75,  23);
        var btClose         = MakeButton("BtClose",         "Close",         root, 93,  12, 75,  23);
        var btEnumInterfaces= MakeButton("BtEnumInterfaces","Enum interfaces",root, 190, 12, 103, 23);
        var btDisconnect    = MakeButton("BtDisconnect",    "Disconnect select interface from WLAN network", root, 310, 12, 257, 23);
        var btGetIfState    = MakeButton("BtGetIfState",    "Get interface state", root, 585, 12, 114, 23);
        var btTurnOn        = MakeButton("BtTurnOn",        "Turn ON",       root, 713, 12, 75,  23);
        var btTurnOff       = MakeButton("BtTurnOff",       "Turn OFF",      root, 794, 12, 75,  23);

        var btCurrentIp        = MakeButton("BtCurrentIp",        "Current IP",          root, 12,  41, 75,  23);
        var btGetIpSettings    = MakeButton("BtGetIpSettings",    "Get IP settings",     root, 93,  41, 105, 23);
        var btEnableDhcp       = MakeButton("BtEnableDhcp",       "Enable DHCP",         root, 204, 41, 104, 23);
        var btEnableStaticIp   = MakeButton("BtEnableStaticIp",   "Enable static IP",    root, 314, 41, 104, 23);
        var btGetDualSta       = MakeButton("BtGetDualSta",       "Get dual STA state",  root, 429, 41, 110, 23);
        var btEnableDualSta    = MakeButton("BtEnableDualSta",    "Enable dual STA",     root, 545, 41, 97,  23);
        var btDisableDualSta   = MakeButton("BtDisableDualSta",   "Disable dual STA",    root, 648, 41, 97,  23);
        var btEnumSecondaryIfs = MakeButton("BtEnumSecondaryIfs", "Enum sec. interfaces",root, 751, 41, 116, 23);

        // ============================================================
        //  Interfaces table (12, 70) 855x86, columns 350 | 350 | 100
        // ============================================================
        var ifaceWidths  = new[] { 350f, 350f, 100f };
        var ifaceHeaders = new[] { "Interface ID", "Description", "Primary" };

        MakeHeaderBar("InterfacesHeader", root, 12, 70, 855, 20, ifaceWidths, ifaceHeaders);

        var interfacesScroll  = MakeScrollView("InterfacesScroll", root, 12, 90, 855, 66, true);
        var interfacesContent = interfacesScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(interfacesContent, false);

        // ============================================================
        //  Networks row (12, 162)
        // ============================================================
        var btEnumNetworks = MakeButton("BtEnumNetworks", "Enum networks", root, 12, 162, 102, 23);
        var tgAdHoc        = MakeToggle("TgAdHoc", "Include Ad Hoc profiles", root, 120, 164, 136, 17);
        var tgHidden       = MakeToggle("TgHidden", "Include hidden profiles", root, 262, 164, 132, 17);
        var btScan         = MakeButton("BtScan", "Scan", root, 400, 162, 75, 23);
        MakeLabel("LaScanSsid", "SSID", root, 507, 165, 32, 13);
        var edScanSsid     = MakeInputField("EdScanSsid", root, 545, 162, 184, 20, "");

        // ============================================================
        //  Networks table (12, 189) 855x76, 13 columns x 100
        // ============================================================
        var netWidths  = new float[13];
        for (int i = 0; i < netWidths.Length; i++) netWidths[i] = 100f;
        var netHeaders = new[]
        {
            "Profile name", "SSID", "BSS type", "Number of BSS IDs", "Connectable",
            "Not connectable reason", "PHY types", "More PHY types", "Signal quality",
            "Security enabled", "Def. auth. algorithm", "Def. cypher algorithm", "Flags"
        };

        MakeHeaderBar("NetworksHeader", root, 12, 189, 855, 20, netWidths, netHeaders);

        var networksScroll  = MakeScrollView("NetworksScroll", root, 12, 209, 855, 56, true);
        var networksContent = networksScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(networksContent, false);

        // ============================================================
        //  BSS row (12, 271)
        // ============================================================
        var btEnumBss     = MakeButton("BtEnumBss", "Enum BSS", root, 12, 271, 75, 23);
        var tgUseSsid     = MakeToggle("TgUseSsid", "Use SSID", root, 93, 273, 73, 17);
        MakeLabel("LaBssType", "BSS type", root, 205, 274, 51, 13);
        var ddBssType     = MakeDropdown("DdBssType", root, 262, 273, 121, 21,
            new[] { "bssInfrastructure", "bssIndependent", "bssAny" });

        // ============================================================
        //  BSS table (12, 300) 855x76, 14 columns x 100
        // ============================================================
        var bssWidths  = new float[14];
        for (int i = 0; i < bssWidths.Length; i++) bssWidths[i] = 100f;
        var bssHeaders = new[]
        {
            "SSID", "PHY ID", "MAC", "BSS type", "PHY type", "RSSI", "Link quality",
            "In reg domain", "Beacon period", "Timestamp", "Host timestamp",
            "Capability", "Center frequency", "IEs"
        };

        MakeHeaderBar("BssHeader", root, 12, 300, 855, 20, bssWidths, bssHeaders);

        var bssScroll  = MakeScrollView("BssScroll", root, 12, 320, 855, 56, true);
        var bssContent = bssScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(bssContent, false);

        // ============================================================
        //  Profiles row (12, 382)
        // ============================================================
        var btEnumProfiles    = MakeButton("BtEnumProfiles",   "Enum profiles",   root, 12,  382, 87,  23);
        var btDeleteProfile   = MakeButton("BtDeleteProfile",  "Delete profile",  root, 105, 382, 84,  23);
        var btRenameProfile   = MakeButton("BtRenameProfile",  "Rename profile",  root, 195, 382, 87,  23);
        var btGetProfileXml   = MakeButton("BtGetProfileXml",  "Get profile XML", root, 288, 382, 106, 23);
        var tgPlainText       = MakeToggle("TgPlainText", "Get passphrase as plain text", root, 400, 384, 159, 17);
        var btSetProfileXml   = MakeButton("BtSetProfileXml",  "Set profile XML", root, 565, 382, 87,  23);
        var btSetXmlUserData  = MakeButton("BtSetXmlUserData", "Set XML user data", root, 658, 382, 118, 23);
        var btEditUi          = MakeButton("BtEditUi",         "Show Edit UI",    root, 782, 382, 85,  23);

        // ============================================================
        //  Profiles table (12, 411) 855x76, columns 200 | 200
        // ============================================================
        var profWidths  = new[] { 200f, 200f };
        var profHeaders = new[] { "Name", "Flags" };

        MakeHeaderBar("ProfilesHeader", root, 12, 411, 855, 20, profWidths, profHeaders);

        var profilesScroll  = MakeScrollView("ProfilesScroll", root, 12, 431, 855, 56, true);
        var profilesContent = profilesScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(profilesContent, false);

        // ============================================================
        //  Connect row (12, 493)
        // ============================================================
        var btConnect        = MakeButton("BtConnect", "Connect", root, 12, 493, 75, 23);
        var ddConnectMode    = MakeDropdown("DdConnectMode", root, 94, 495, 214, 21,
            new[] { "using network profile", "using selected profile",
                    "using selected network", "using temporary profile" });
        MakeLabel("LaPassKey", "Passkey (for temporary profile)", root, 314, 496, 148, 13);
        var edPasskey        = MakeInputField("EdPasskey", root, 468, 495, 170, 20, "12345678910");
        var tgDiscoverSecure = MakeToggle("TgDiscoverSecure", "Discover secure", root, 649, 495, 103, 17);
        var tgUseBssMac      = MakeToggle("TgUseBssMac", "Use BSS MAC", root, 758, 495, 95, 17);

        // ============================================================
        //  ACM row 1 (12, 560)
        // ============================================================
        MakeBoldLabel("LaAcmSettings", "ACM Settings", root, 12, 530, 100, 15);

        var btGetShowDenied    = MakeButton("BtGetShowDenied",   "Get \"Show denied networks\"",  root, 12,  560, 166, 23);
        var btGetPowerSettings = MakeButton("BtGetPowerSettings","Get power settings",           root, 184, 560, 154, 23);
        var btGetAllowExplicit = MakeButton("BtGetAllowExplicit","Get \"Allow explicit creds\"", root, 344, 560, 149, 23);
        var btGetBlockPeriod   = MakeButton("BtGetBlockPeriod",  "Get block period",             root, 499, 560, 103, 23);
        var btGetVirtual       = MakeButton("BtGetVirtual",      "Get \"Virtual station extensibility\"", root, 608, 560, 180, 23);

        // ============================================================
        //  ACM row 2 (12, 589)
        // ============================================================
        var btSetShowDenied    = MakeButton("BtSetShowDenied",   "Set \"Show denied networks\"",  root, 12,  589, 166, 23);
        var btGetOnlyGp        = MakeButton("BtGetOnlyGp",       "Get \"Only use GP profiles\"",  root, 184, 589, 154, 23);
        var btSetAllowExplicit = MakeButton("BtSetAllowExplicit","Set \"Allow explicit creds\"", root, 344, 589, 149, 23);
        var btSetBlockPeriod   = MakeButton("BtSetBlockPeriod",  "Set block period",             root, 499, 589, 103, 23);
        var btSetVirtual       = MakeButton("BtSetVirtual",      "Set \"Virtual station extensibility\"", root, 608, 589, 180, 23);

        // ============================================================
        //  Log (12, 620) 855x130 - added below the original
        // ============================================================
        var logScroll  = MakeScrollView("LogScroll", root, 12, 620, 855, 130, false);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent, false);

        // ============================================================
        //  Confirm panel
        // ============================================================
        var (confirmPanel, confirmText, confirmYes, confirmNo, confirmCancel) =
            MakeConfirmDialog(root);

        // ============================================================
        //  Prefabs
        // ============================================================
        var interfaceRow = MakeTableRowPrefab("WiFiClientInterfaceRow", ifaceWidths,  22f);
        var networkRow   = MakeTableRowPrefab("WiFiClientNetworkRow",   netWidths,    22f);
        var bssRow       = MakeTableRowPrefab("WiFiClientBssRow",       bssWidths,    22f);
        var profileRow   = MakeTableRowPrefab("WiFiClientProfileRow",   profWidths,   22f);
        var logRow       = MakeTableRowPrefab("WiFiClientLogRow",       new[] { 850f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("WiFiClientDemo");
        var demo = demoGO.AddComponent<WiFiClientDemo>();

        demo.openButton                = btOpen;
        demo.closeButton               = btClose;
        demo.enumInterfacesButton      = btEnumInterfaces;
        demo.disconnectButton          = btDisconnect;
        demo.getInterfaceStateButton   = btGetIfState;
        demo.turnOnButton              = btTurnOn;
        demo.turnOffButton             = btTurnOff;

        demo.currentIpButton           = btCurrentIp;
        demo.getIpSettingsButton       = btGetIpSettings;
        demo.enableDhcpButton          = btEnableDhcp;
        demo.enableStaticIpButton      = btEnableStaticIp;
        demo.getDualStaStateButton     = btGetDualSta;
        demo.enableDualStaButton       = btEnableDualSta;
        demo.disableDualStaButton      = btDisableDualSta;
        demo.enumSecondaryInterfacesButton = btEnumSecondaryIfs;

        demo.interfacesContent  = interfacesContent;
        demo.interfaceRowPrefab = interfaceRow;

        demo.enumNetworksButton = btEnumNetworks;
        demo.adHocToggle        = tgAdHoc;
        demo.hiddenToggle       = tgHidden;
        demo.scanButton         = btScan;
        demo.scanSsidInput      = edScanSsid;

        demo.networksContent  = networksContent;
        demo.networkRowPrefab = networkRow;

        demo.enumBssButton    = btEnumBss;
        demo.useSsidToggle    = tgUseSsid;
        demo.bssTypeDropdown  = ddBssType;

        demo.bssContent  = bssContent;
        demo.bssRowPrefab = bssRow;

        demo.enumProfilesButton    = btEnumProfiles;
        demo.deleteProfileButton   = btDeleteProfile;
        demo.renameProfileButton   = btRenameProfile;
        demo.getProfileXmlButton   = btGetProfileXml;
        demo.plainTextToggle       = tgPlainText;
        demo.setProfileXmlButton   = btSetProfileXml;
        demo.setXmlUserDataButton  = btSetXmlUserData;
        demo.editUiButton          = btEditUi;

        demo.profilesContent  = profilesContent;
        demo.profileRowPrefab = profileRow;

        demo.connectButton        = btConnect;
        demo.connectModeDropdown  = ddConnectMode;
        demo.passkeyInput         = edPasskey;
        demo.discoverSecureToggle = tgDiscoverSecure;
        demo.useBssMacToggle      = tgUseBssMac;

        demo.getShowDeniedButton     = btGetShowDenied;
        demo.getPowerSettingsButton  = btGetPowerSettings;
        demo.getAllowExplicitButton  = btGetAllowExplicit;
        demo.getBlockPeriodButton    = btGetBlockPeriod;
        demo.getVirtualStationButton = btGetVirtual;

        demo.setShowDeniedButton     = btSetShowDenied;
        demo.getOnlyGpButton         = btGetOnlyGp;
        demo.setAllowExplicitButton  = btSetAllowExplicit;
        demo.setBlockPeriodButton    = btSetBlockPeriod;
        demo.setVirtualStationButton = btSetVirtual;

        demo.confirmPanel        = confirmPanel;
        demo.confirmText         = confirmText;
        demo.confirmYesButton    = confirmYes;
        demo.confirmNoButton     = confirmNo;
        demo.confirmCancelButton = confirmCancel;

        demo.logContent   = logContent;
        demo.logRowPrefab = logRow;

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Wi-Fi Client Demo UI built using the original layout.");
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
        text.enableWordWrapping = true;
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
        lblText.enableWordWrapping = true;

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

    private static TMP_Text MakeBoldLabel(string name, string text, Transform parent,
        float x, float y, float w, float h)
    {
        var t = MakeLabel(name, text, parent, x, y, w, h);
        t.fontStyle = FontStyles.Bold;
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
        tRect.sizeDelta = new Vector2(0, 160);

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
            tmp.fontSize = 10;
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
        if (horizontal)
        {
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(0, 1);
            cRect.pivot     = new Vector2(0, 1);
        }
        else
        {
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot     = new Vector2(0.5f, 1);
        }
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = Vector2.zero;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content  = cRect;

        return root;
    }

    private static void AddVerticalListGroup(Transform content, bool forceWidth)
    {
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childForceExpandWidth  = !forceWidth;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = !forceWidth;
        vlg.childControlHeight = true;

        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        if (!forceWidth) csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
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

        float rowW = 8;
        for (int i = 0; i < widths.Length; i++) rowW += widths[i];

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        le.preferredWidth = rowW;
        le.minWidth = rowW;

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
            t.fontSize = 10;
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
    //  Confirm dialog
    // ================================================================
    private static (GameObject panel, TMP_Text text, Button yes, Button no, Button cancel)
        MakeConfirmDialog(Transform parent)
    {
        var panel = CreateUI("ConfirmPanel", parent);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600, 200);

        var img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var txtGO = CreateUI("Text", panel.transform);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 20;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = true;
        var trt = txt.rectTransform;
        trt.anchorMin = new Vector2(0, 0.4f); trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(20, 0); trt.offsetMax = new Vector2(-20, -10);

        var bar = CreateUI("Buttons", panel.transform);
        var br = bar.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0.4f);
        br.offsetMin = new Vector2(20, 20); br.offsetMax = new Vector2(-20, 0);
        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false; hlg.childControlHeight = false;

        var yes    = MakeButton("Yes",    "YES",    bar.transform, 0, 0, 140, 44);
        var no     = MakeButton("No",     "NO",     bar.transform, 0, 0, 140, 44);
        var cancel = MakeButton("Cancel", "CANCEL", bar.transform, 0, 0, 140, 44);

        panel.SetActive(false);
        return (panel, txt, yes, no, cancel);
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