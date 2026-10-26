#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class WiFiClientSceneBuilder
{
    private const float FormWidth  = 879f;
    private const float FormHeight = 619f;
    private const string ScenePath = "Assets/Scenes/WiFiClient.unity";

    [MenuItem("Tools/WiFiClient/Build Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.94f, 0.94f, 0.94f);
        camGO.transform.position = new Vector3(0, 0, -10);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(FormWidth, FormHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        CreatePanel(canvasGO.transform, "FormBackground", 0, 0, FormWidth, FormHeight,
            new Color(0.94f, 0.94f, 0.94f));

        var root = canvasGO.transform;

        // Row 1
        var btClientOpen              = CreateButton(root, "btClientOpen",              "Open",               12,  12, 75,  23);
        var btClientClose             = CreateButton(root, "btClientClose",             "Close",              93,  12, 75,  23);
        var btClientEnumInterfaces    = CreateButton(root, "btClientEnumInterfaces",    "Enum interfaces",    190, 12, 103, 23);
        var btClientDisconnect        = CreateButton(root, "btClientDisconnect",        "Disconnect select interface from WLAN network", 310, 12, 257, 23);
        var btClientGetInterfaceState = CreateButton(root, "btClientGetInterfaceState", "Get interface state",585, 12, 114, 23);
        var btTurnOn                  = CreateButton(root, "btTurnOn",                  "Turn ON",            713, 12, 75,  23);
        var btTurnOff                 = CreateButton(root, "btTurnOff",                 "Turn OFF",           794, 12, 75,  23);

        // Row 2
        var btCurrentIp              = CreateButton(root, "btCurrentIp",              "Current IP",         12,  41, 75,  23);
        var btGetIpSettings          = CreateButton(root, "btGetIpSettings",          "Get IP settings",    93,  41, 105, 23);
        var btEnableDhcp             = CreateButton(root, "btEnableDhcp",             "Enable DHCP",        204, 41, 104, 23);
        var btEnableStaticIp         = CreateButton(root, "btEnableStaticIp",         "Enable static IP",   314, 41, 104, 23);
        var btGetDualStaState        = CreateButton(root, "btGetDualStaState",        "Get dual STA state", 429, 41, 110, 23);
        var btEnableDualSta          = CreateButton(root, "btEnableDualSta",          "Enable dual STA",    545, 41, 97,  23);
        var btDisableDualSta         = CreateButton(root, "btDisableDualSta",         "Disable dual STA",   648, 41, 97,  23);
        var btEnumSecondaryInterfaces = CreateButton(root, "btEnumSecondaryInterfaces","Enum sec. interfaces",751,41, 116, 23);

        // lvClientInterfaces
        var lvClientInterfacesContent = CreateListView(root, "lvClientInterfaces", 12, 70, 855, 86,
            new[] { "Interface ID", "Description", "Primary" },
            new float[] { 350f, 350f, 60f });

        // Row 4
        var btClientEnumNetworks   = CreateButton(root, "btClientEnumNetworks",   "Enum networks",         12,  162, 102, 23);
        var cbClientAdHocProfiles  = CreateCheckBox(root, "cbClientAdHocProfiles", "Include Ad Hoc profiles", 120, 166, 136, 17, false);
        var cbClientHiddenProfiles = CreateCheckBox(root, "cbClientHiddenProfiles","Include hidden profiles", 262, 166, 132, 17, false);
        var btClientScan           = CreateButton(root, "btClientScan",           "Scan",                  400, 162, 75,  23);
        CreateLabel(root, "laClientScanSsid", "SSID", 507, 167, 32, 13);
        var edClientScanSsid       = CreateInputField(root, "edClientScanSsid", "", 545, 162, 184, 20);

        // lvClientNetworks
        var lvClientNetworksContent = CreateListView(root, "lvClientNetworks", 12, 189, 855, 76,
            new[] { "Profile name", "SSID", "BSS type", "Number of BSS IDs", "Connectable",
                    "Not connectable reason", "PHY types", "More PHY types", "Signal quality",
                    "Security enabled", "Def. auth. algorithm", "Def. cypher algorithm", "Flags" },
            new float[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 });

        // Row 6
        var btClientEnumBSS = CreateButton(root, "btClientEnumBSS", "Enum BSS", 12, 271, 75, 23);
        var cbClientUseSsid = CreateCheckBox(root, "cbClientUseSsid", "Use SSID", 93, 275, 73, 17, false);
        CreateLabel(root, "laClientBssType", "BSS type", 205, 276, 51, 13);
        var cbClientBssType = CreateDropdown(root, "cbClientBssType",
            new[] { "bssInfrastructure", "bssIndependent", "bssAny" }, 262, 273, 121, 21, 2);

        // lvClientBss
        var lvClientBssContent = CreateListView(root, "lvClientBss", 12, 300, 855, 76,
            new[] { "SSID", "PHY ID", "MAC", "BSS type", "PHY type", "RSSI", "Link quality",
                    "In reg domain", "Beacon period", "Timestamp", "Host timestamp",
                    "Capability", "Center frequency", "IEs" },
            new float[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 60 });

        // Row 8
        var btClientEnumProfiles   = CreateButton(root, "btClientEnumProfiles",   "Enum profiles",   12,  382, 87,  23);
        var btClientDeleteProfile  = CreateButton(root, "btClientDeleteProfile",  "Delete profile",  105, 382, 84,  23);
        var btClientRenameProfile  = CreateButton(root, "btClientRenameProfile",  "Rename profile",  195, 382, 87,  23);
        var btClientGetProfileXml  = CreateButton(root, "btClientGetProfileXml",  "Get profile XML", 288, 382, 106, 23);
        var cbPlainText            = CreateCheckBox(root, "cbPlainText", "Get passphrase as plain text", 400, 386, 159, 17, true);
        var btClientSetProfileXml  = CreateButton(root, "btClientSetProfileXml",  "Set profile XML", 565, 382, 87,  23);
        var btSetProfileXmlUserData= CreateButton(root, "btSetProfileXmlUserData","Set XML user data",658,382, 118, 23);
        var btEditUi               = CreateButton(root, "btEditUi",               "Show Edit UI",    782, 382, 85,  23);

        // lvClientProfiles
        var lvClientProfilesContent = CreateListView(root, "lvClientProfiles", 12, 411, 855, 76,
            new[] { "Name", "Flags" },
            new float[] { 200, 200 });

        // Row 10
        var btClientConnect = CreateButton(root, "btClientConnect", "Connect", 12, 493, 75, 23);
        var cbClientConnect = CreateDropdown(root, "cbClientConnect",
            new[] { "using network profile", "using selected profile", "using selected network", "using temporary profile" },
            94, 495, 214, 21, 0);
        CreateLabel(root, "laClientPassKey", "Passkey (for temporary profile)", 314, 498, 148, 13);
        var edClientPasskey = CreateInputField(root, "edClientPasskey", "12345678910", 468, 495, 170, 20);
        var cbClientDiscoverSecure = CreateCheckBox(root, "cbClientDiscoverSecure", "Discover secure", 649, 497, 103, 17, true);
        var cbUseBssMac = CreateCheckBox(root, "cbUseBssMac", "Use BSS MAC", 758, 497, 95, 17, false);

        // ACM settings
        CreateLabel(root, "laClientAcmSettings", "ACM Settings", 12, 532, 83, 13, true);

        var btClientGetShowDeniedNetworks           = CreateButton(root, "btClientGetShowDeniedNetworks",           "Get \"Show denied networks\"",        12,  560, 166, 23);
        var btClientGetPowerSettings                = CreateButton(root, "btClientGetPowerSettings",                "Get power settings",                 184, 560, 154, 23);
        var btClientGetAllowExplicitCreds           = CreateButton(root, "btClientGetAllowExplicitCreds",           "Get \"Allow explicit creds\"",        344, 560, 149, 23);
        var btClientGetBlockPeriod                  = CreateButton(root, "btClientGetBlockPeriod",                  "Get block period",                   499, 560, 103, 23);
        var btClientGetVirtualStationExtensibility  = CreateButton(root, "btClientGetVirtualStationExtensibility",  "Get \"Virtual station extensibility\"",608,560, 180, 23);
        var btClientSetShowDeniedNetworks           = CreateButton(root, "btClientSetShowDeniedNetworks",           "Set \"Show denied networks\"",        12,  589, 166, 23);
        var btClientGetOnlyUseGpProfiles            = CreateButton(root, "btClientGetOnlyUseGpProfiles",            "Get \"Only use GP profiles\"",        184, 589, 154, 23);
        var btClientSetAllowExplicitCreds           = CreateButton(root, "btClientSetAllowExplicitCreds",           "Set \"Allow explicit creds\"",        344, 589, 149, 23);
        var btClientSetBlockPeriod                  = CreateButton(root, "btClientSetBlockPeriod",                  "Set block period",                   499, 589, 103, 23);
        var btClientSetVirtualStationExtensibility  = CreateButton(root, "btClientSetVirtualStationExtensibility",  "Set \"Virtual station extensibility\"",608,589, 180, 23);

        // Controller
        var ctrlGO = new GameObject("WiFiClientController");
        var ctrl = ctrlGO.AddComponent<WiFiClientController>();

        ctrl.btClientOpen              = btClientOpen.GetComponent<Button>();
        ctrl.btClientClose             = btClientClose.GetComponent<Button>();
        ctrl.btClientEnumInterfaces    = btClientEnumInterfaces.GetComponent<Button>();
        ctrl.btClientDisconnect        = btClientDisconnect.GetComponent<Button>();
        ctrl.btClientGetInterfaceState = btClientGetInterfaceState.GetComponent<Button>();
        ctrl.btTurnOn                  = btTurnOn.GetComponent<Button>();
        ctrl.btTurnOff                 = btTurnOff.GetComponent<Button>();
        ctrl.btCurrentIp               = btCurrentIp.GetComponent<Button>();
        ctrl.btGetIpSettings           = btGetIpSettings.GetComponent<Button>();
        ctrl.btEnableDhcp              = btEnableDhcp.GetComponent<Button>();
        ctrl.btEnableStaticIp          = btEnableStaticIp.GetComponent<Button>();
        ctrl.btGetDualStaState         = btGetDualStaState.GetComponent<Button>();
        ctrl.btEnableDualSta           = btEnableDualSta.GetComponent<Button>();
        ctrl.btDisableDualSta          = btDisableDualSta.GetComponent<Button>();
        ctrl.btEnumSecondaryInterfaces = btEnumSecondaryInterfaces.GetComponent<Button>();
        ctrl.btClientEnumNetworks      = btClientEnumNetworks.GetComponent<Button>();
        ctrl.btClientScan              = btClientScan.GetComponent<Button>();
        ctrl.btClientEnumBSS           = btClientEnumBSS.GetComponent<Button>();
        ctrl.btClientEnumProfiles      = btClientEnumProfiles.GetComponent<Button>();
        ctrl.btClientDeleteProfile     = btClientDeleteProfile.GetComponent<Button>();
        ctrl.btClientRenameProfile     = btClientRenameProfile.GetComponent<Button>();
        ctrl.btClientGetProfileXml     = btClientGetProfileXml.GetComponent<Button>();
        ctrl.btClientSetProfileXml     = btClientSetProfileXml.GetComponent<Button>();
        ctrl.btSetProfileXmlUserData   = btSetProfileXmlUserData.GetComponent<Button>();
        ctrl.btEditUi                  = btEditUi.GetComponent<Button>();
        ctrl.btClientConnect           = btClientConnect.GetComponent<Button>();
        ctrl.btClientGetShowDeniedNetworks          = btClientGetShowDeniedNetworks.GetComponent<Button>();
        ctrl.btClientGetPowerSettings               = btClientGetPowerSettings.GetComponent<Button>();
        ctrl.btClientGetAllowExplicitCreds          = btClientGetAllowExplicitCreds.GetComponent<Button>();
        ctrl.btClientGetBlockPeriod                 = btClientGetBlockPeriod.GetComponent<Button>();
        ctrl.btClientGetVirtualStationExtensibility = btClientGetVirtualStationExtensibility.GetComponent<Button>();
        ctrl.btClientSetShowDeniedNetworks          = btClientSetShowDeniedNetworks.GetComponent<Button>();
        ctrl.btClientGetOnlyUseGpProfiles           = btClientGetOnlyUseGpProfiles.GetComponent<Button>();
        ctrl.btClientSetAllowExplicitCreds          = btClientSetAllowExplicitCreds.GetComponent<Button>();
        ctrl.btClientSetBlockPeriod                 = btClientSetBlockPeriod.GetComponent<Button>();
        ctrl.btClientSetVirtualStationExtensibility = btClientSetVirtualStationExtensibility.GetComponent<Button>();

        ctrl.cbClientAdHocProfiles   = cbClientAdHocProfiles;
        ctrl.cbClientHiddenProfiles  = cbClientHiddenProfiles;
        ctrl.cbClientUseSsid         = cbClientUseSsid;
        ctrl.cbClientBssType         = cbClientBssType;
        ctrl.cbClientConnect         = cbClientConnect;
        ctrl.cbClientDiscoverSecure  = cbClientDiscoverSecure;
        ctrl.cbUseBssMac             = cbUseBssMac;
        ctrl.cbPlainText             = cbPlainText;

        ctrl.edClientScanSsid = edClientScanSsid;
        ctrl.edClientPasskey  = edClientPasskey;

        ctrl.lvClientInterfacesContent = lvClientInterfacesContent;
        ctrl.lvClientNetworksContent   = lvClientNetworksContent;
        ctrl.lvClientBssContent        = lvClientBssContent;
        ctrl.lvClientProfilesContent   = lvClientProfilesContent;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError("[WiFiClient] Failed to save scene: " + ScenePath);
            return;
        }

        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Selection.activeObject = sceneAsset;
        EditorGUIUtility.PingObject(sceneAsset);

        Debug.Log("[WiFiClient] Scene created and saved: " + ScenePath);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject CreatePanel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject CreateLabel(Transform parent, string name, string text, float x, float y, float w, float h, bool bold = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 11;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.TopLeft; tmp.color = Color.black; tmp.raycastTarget = false;
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(0.87f, 0.87f, 0.87f);

        var btn = go.GetComponent<Button>();
        var c = btn.colors;
        c.normalColor = Color.white; c.highlightedColor = new Color(0.93f, 0.93f, 0.93f); c.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = c;

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 11; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.black; tmp.raycastTarget = false;
        return go;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string text, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = Color.white;

        var textAreaGO = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var textAreaRT = textAreaGO.GetComponent<RectTransform>();
        textAreaRT.SetParent(go.transform, false);
        textAreaRT.anchorMin = Vector2.zero; textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(4, 2); textAreaRT.offsetMax = new Vector2(-4, -2);

        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
        var placeholderRT = placeholderGO.GetComponent<RectTransform>();
        placeholderRT.SetParent(textAreaGO.transform, false);
        placeholderRT.anchorMin = Vector2.zero; placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = Vector2.zero; placeholderRT.offsetMax = Vector2.zero;
        var placeholderTmp = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = string.Empty; placeholderTmp.fontSize = 11;
        placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f); placeholderTmp.alignment = TextAlignmentOptions.Left;
        placeholderTmp.enableWordWrapping = false; placeholderTmp.raycastTarget = false;

        var textGO = new GameObject("Text", typeof(RectTransform));
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(textAreaGO.transform, false);
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        var textTmp = textGO.AddComponent<TextMeshProUGUI>();
        textTmp.text = text; textTmp.fontSize = 11; textTmp.color = Color.black;
        textTmp.alignment = TextAlignmentOptions.Left; textTmp.enableWordWrapping = false;

        var inputField = go.GetComponent<TMP_InputField>();
        inputField.targetGraphic = go.GetComponent<Image>();
        inputField.textViewport = textAreaRT; inputField.textComponent = textTmp; inputField.placeholder = placeholderTmp;
        inputField.lineType = TMP_InputField.LineType.SingleLine; inputField.text = text;
        return inputField;
    }

    private static Toggle CreateCheckBox(Transform parent, string name, string label, float x, float y, float w, float h, bool isOn)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);

        var boxGO = new GameObject("Box", typeof(RectTransform), typeof(Image));
        var boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.SetParent(go.transform, false);
        boxRT.anchorMin = new Vector2(0, 0.5f); boxRT.anchorMax = new Vector2(0, 0.5f); boxRT.pivot = new Vector2(0, 0.5f);
        boxRT.anchoredPosition = Vector2.zero; boxRT.sizeDelta = new Vector2(13, 13);
        var boxImg = boxGO.GetComponent<Image>(); boxImg.color = Color.white;

        var checkGO = new GameObject("Check", typeof(RectTransform), typeof(Image));
        var checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.SetParent(boxGO.transform, false);
        checkRT.anchorMin = Vector2.zero; checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = new Vector2(2, 2); checkRT.offsetMax = new Vector2(-2, -2);
        var checkImg = checkGO.GetComponent<Image>(); checkImg.color = new Color(0.2f, 0.2f, 0.2f); checkImg.raycastTarget = false;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(17, 0); labelRT.offsetMax = Vector2.zero;
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label; labelTmp.fontSize = 11; labelTmp.color = Color.black;
        labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.enableWordWrapping = false; labelTmp.raycastTarget = false;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = boxImg; toggle.graphic = checkImg; toggle.isOn = isOn;
        return toggle;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name, string[] options,
        float x, float y, float w, float h, int selectedIndex)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = Color.white;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(4, 0); labelRT.offsetMax = new Vector2(-18, 0);
        var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.fontSize = 11; labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.color = Color.black; labelTmp.raycastTarget = false;

        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f); arrowRT.anchorMax = new Vector2(1, 0.5f); arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-4, 0); arrowRT.sizeDelta = new Vector2(10, 10);
        arrowGO.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);

        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Canvas), typeof(GraphicRaycaster));
        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.SetParent(go.transform, false);
        templateRT.anchorMin = new Vector2(0, 0); templateRT.anchorMax = new Vector2(1, 0); templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, options.Length * 20f + 2f);
        templateGO.GetComponent<Image>().color = Color.white;
        var tc = templateGO.GetComponent<Canvas>(); tc.overrideSorting = true; tc.sortingOrder = 30000;
        templateGO.SetActive(false);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1); contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 1); itemRT.anchorMax = new Vector2(1, 1); itemRT.pivot = new Vector2(0.5f, 1);
        itemRT.anchoredPosition = Vector2.zero; itemRT.sizeDelta = new Vector2(0, 20);
        var itemImg = itemGO.GetComponent<Image>(); itemImg.color = Color.white;
        var itemToggle = itemGO.GetComponent<Toggle>(); itemToggle.targetGraphic = itemImg;
        var itemLe = itemGO.AddComponent<LayoutElement>(); itemLe.preferredHeight = 20;

        var itemBgGO = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBgGO.transform.SetParent(itemGO.transform, false);
        var itemBgRT = itemBgGO.GetComponent<RectTransform>();
        itemBgRT.anchorMin = Vector2.zero; itemBgRT.anchorMax = Vector2.one;
        itemBgRT.offsetMin = Vector2.zero; itemBgRT.offsetMax = Vector2.zero;
        itemBgGO.GetComponent<Image>().color = Color.white;

        var itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero; itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(6, 0); itemLabelRT.offsetMax = new Vector2(-6, 0);
        var itemLabelTmp = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelTmp.fontSize = 11; itemLabelTmp.alignment = TextAlignmentOptions.Left; itemLabelTmp.color = Color.black;

        var sr = templateGO.GetComponent<ScrollRect>();
        sr.content = contentRT; sr.viewport = viewportRT; sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = go.GetComponent<Image>();
        dropdown.template = templateRT;
        dropdown.captionText = labelTmp;
        dropdown.itemText = itemLabelTmp;
        dropdown.itemImage = itemBgGO.GetComponent<Image>();

        dropdown.options.Clear();
        foreach (var opt in options) dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
        dropdown.value = selectedIndex;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static RectTransform CreateListView(Transform parent, string name,
        float x, float y, float w, float h, string[] headers, float[] widths)
    {
        const float RowH = 18f;
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
        root.GetComponent<Image>().color = Color.white;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.SetParent(root.transform, false);
        viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(1, 1); viewportRT.offsetMax = new Vector2(-1, -1);
        viewportGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        float totalW = 0;
        for (int i = 0; i < widths.Length; i++) totalW += widths[i];

        var contentGO = new GameObject("Content", typeof(RectTransform));
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.SetParent(viewportGO.transform, false);
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(0, 1); contentRT.pivot = new Vector2(0, 1);
        contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = new Vector2(totalW, 0);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        vlg.spacing = 0; vlg.padding = new RectOffset(0, 0, 0, 0);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(contentGO.transform, false);
        headerGO.GetComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        var hlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.spacing = 0; hlg.padding = new RectOffset(0, 0, 0, 0);
        var hle = headerGO.GetComponent<LayoutElement>();
        hle.preferredHeight = RowH; hle.minHeight = RowH;

        for (int i = 0; i < headers.Length; i++)
        {
            var cellGO = new GameObject("h" + i, typeof(RectTransform), typeof(LayoutElement));
            cellGO.transform.SetParent(headerGO.transform, false);
            var cellLe = cellGO.GetComponent<LayoutElement>();
            cellLe.preferredWidth = widths[i]; cellLe.minWidth = widths[i];
            var tmp = cellGO.AddComponent<TextMeshProUGUI>();
            tmp.text = headers[i]; tmp.fontSize = 11; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left; tmp.color = Color.black;
            tmp.enableWordWrapping = false; tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.margin = new Vector4(4, 0, 2, 0); tmp.raycastTarget = false;
        }

        var sr = root.GetComponent<ScrollRect>();
        sr.content = contentRT; sr.viewport = viewportRT;
        sr.horizontal = true; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return contentRT;
    }
}
#endif