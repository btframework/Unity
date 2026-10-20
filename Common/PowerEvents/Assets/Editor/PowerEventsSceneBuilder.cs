#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PowerEventsSceneBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs";

    // ClientSize = 553 x 401
    private const float CanvasW = 573f;
    private const float CanvasH = 421f;

    [MenuItem("Tools/Power Events/Build Scene")]
    public static void BuildScene()
    {
        // --- 0. Make sure Assets/Prefabs exists as a real asset folder ---
        EnsureAssetFolder(PrefabsFolder);

        // --- 1. Clean up previous build ---
        DestroyIfExists("PowerEventsUI");
        DestroyIfExists("PowerEventsDemo");
        DestroyIfExists("EventSystem");

        EnsureEventSystem();

        // --- 2. Canvas ---
        var canvasGO = CreateUI("PowerEventsUI", null);
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
        var btStart  = MakeButton("BtStart",  "Start",  root, 12,  12, 75, 23);
        var btStop   = MakeButton("BtStop",   "Stop",   root, 93,  12, 75, 23);
        var btStatus = MakeButton("BtStatus", "Status", root, 195, 12, 75, 23);

        // ============================================================
        //  Log (12, 41) 529x342
        // ============================================================
        var logScroll  = MakeScrollView("LogScroll", root, 12, 41, 529, 342);
        var logContent = logScroll.transform.Find("Viewport/Content");
        AddVerticalListGroup(logContent);

        // ============================================================
        //  Prefabs
        // ============================================================
        var logRow = MakeTableRowPrefab("PowerEventsLogRow", new[] { 523f }, 20f);

        // ============================================================
        //  Main GameObject + links
        // ============================================================
        var demoGO = new GameObject("PowerEventsDemo");
        var demo = demoGO.AddComponent<PowerEventsDemo>();

        demo.startButton  = btStart;
        demo.stopButton   = btStop;
        demo.statusButton = btStatus;

        demo.logContent   = logContent;
        demo.logRowPrefab = logRow;

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
            Debug.LogWarning("Power Events: active scene is invalid. " +
                             "Please save the scene manually (Ctrl+S), otherwise the build will be empty.");
        }

        AssetDatabase.Refresh();

        Selection.activeGameObject = demoGO;
        EditorGUIUtility.PingObject(demoGO);

        Debug.Log("Power Events Demo UI built. Scene saved.");
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
        text.enableWordWrapping = false;
        StretchFull(textGO);
        return btn;
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

        // RectMask2D does not need an Image — cleaner and lighter than Mask
        viewport.AddComponent<RectMask2D>();

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

        // The VLG must NOT control the child width, otherwise the row is
        // computed as zero-width (no LayoutElement.preferredWidth on prefab).
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

        // Explicit top-left anchors, top pivot, and a real width.
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);

        float totalWidth = 8;
        for (int i = 0; i < widths.Length; i++) totalWidth += widths[i];

        rt.sizeDelta = new Vector2(totalWidth, rowHeight);

        var img = root.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.03f);
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
            // The scroll view background is white; text must be dark.
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
        ui.normalColor   = new Color(1, 1, 1, 0.03f);
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
            Debug.LogError($"Power Events: failed to create prefab at {assetPath}");

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

        string parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
        string leaf   = Path.GetFileName(assetFolder);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureAssetFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
        AssetDatabase.Refresh();
    }
}
#endif