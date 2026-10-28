using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace CatBlocksUI.Editor {
public sealed class ScreenImporterWindow : EditorWindow {
    [SerializeField] DefaultAsset uiFolder;
    [SerializeField] string selectedPath;
    [SerializeField] bool preview, convertTextures = true, createEvents = true;
    [SerializeField] TMP_FontAsset font;
    [SerializeField] GameObject built;
    [SerializeField] Vector2 scroll;
    string[] paths = Array.Empty<string>();
    string[] scanWarnings = Array.Empty<string>();
    string status;
    MessageType statusType;
    bool showAssets = true, showAnchors = true;
    LayoutAssetResolver.ScanReport report;
    UnityEditor.Editor rectEditor;
    UnityEngine.Object[] rectTargets = Array.Empty<UnityEngine.Object>();

    [MenuItem("Tools/UI Layout Importer")]
    [MenuItem("Tools/Cat Blocks/UI Asset Importer")]
    public static void Open() { GetWindow<ScreenImporterWindow>("UI Layout Importer").minSize = new Vector2(440, 620); }
    void OnEnable() {
        if (!uiFolder) {
            try { uiFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(ScreenImporter.PackageRoot); } catch { }
        }
        if (uiFolder) Try(Refresh);
        Selection.selectionChanged += SelectionChanged;
        Undo.undoRedoPerformed += Repaint;
    }
    void OnDisable() {
        Selection.selectionChanged -= SelectionChanged; Undo.undoRedoPerformed -= Repaint;
        if (rectEditor) DestroyImmediate(rectEditor);
    }
    void SelectionChanged() { Repaint(); }
    void OnProjectChange() { report = null; Repaint(); }
    string Folder { get { return uiFolder ? LayoutAssetResolver.RequireFolder(AssetDatabase.GetAssetPath(uiFolder)) : null; } }
    void Refresh() {
        paths = uiFolder ? LayoutAssetResolver.Discover(Folder, out scanWarnings) : Array.Empty<string>();
        if (!paths.Contains(selectedPath)) selectedPath = paths.FirstOrDefault();
        report = null; status = paths.Length + " layout document(s) found."; statusType = MessageType.Info;
    }
    void OnGUI() {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("UI Layout Importer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Choose a UI folder under Assets. Scan its layout JSON, check asset references, then assemble a screen in the active game scene.", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        var folder = (DefaultAsset)EditorGUILayout.ObjectField("UI folder", uiFolder, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck()) {
            paths = Array.Empty<string>(); selectedPath = null; report = null;
            if (folder && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder))) { status = "Choose a folder, rather than a file."; statusType = MessageType.Warning; }
            else { uiFolder = folder; Try(Refresh); }
        }
        using (new EditorGUILayout.HorizontalScope()) {
            using (new EditorGUI.DisabledScope(!uiFolder)) if (GUILayout.Button("Scan UI folder")) Try(Refresh);
            if (GUILayout.Button("Select folder...")) Try(() => {
                string path = EditorUtility.OpenFolderPanel("Choose a UI folder inside Assets", Application.dataPath, "");
                if (string.IsNullOrEmpty(path)) return;
                uiFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(LayoutAssetResolver.RequireFolder(path)); Refresh();
            });
        }
        foreach (var warning in scanWarnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
        if (paths.Length > 0) {
            int selected = Mathf.Max(0, Array.IndexOf(paths, selectedPath));
            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup("Layout", selected, paths.Select(p => p.Substring(AssetDatabase.GetAssetPath(uiFolder).Length + 1).Replace("/", " > ")).ToArray());
            if (EditorGUI.EndChangeCheck()) { selectedPath = paths[selected]; report = null; status = null; }
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(selectedPath, EditorStyles.miniLabel);
                if (GUILayout.Button("Show JSON", GUILayout.Width(85))) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(selectedPath));
            }
        } else EditorGUILayout.HelpBox("No supported layouts found. JSON must use schemaVersion 1, coordinateSystem top-left-pixels, and an elements array. See PORTABLE-IMPORTER.md.", MessageType.None);
        font = (TMP_FontAsset)EditorGUILayout.ObjectField("TextMeshPro font (optional)", font, typeof(TMP_FontAsset), false);
        preview = EditorGUILayout.Toggle("Include preview-only art", preview);
        createEvents = EditorGUILayout.Toggle("Ensure scene EventSystem", createEvents);
        EditorGUI.BeginChangeCheck();
        convertTextures = EditorGUILayout.Toggle(new GUIContent("Convert non-sprite textures", "Assembly imports ordinary textures as single Sprites. Existing Sprite and sliced-sheet settings are preserved. Scanning does not change assets."), convertTextures);
        if (EditorGUI.EndChangeCheck()) report = null;
        using (new EditorGUI.DisabledScope(paths.Length == 0 || EditorApplication.isPlayingOrWillChangePlaymode)) {
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Check selected references")) Try(() => { report = ScreenImporter.Inspect(selectedPath, Folder, convertTextures); status = report.IsValid ? "All " + report.assets.Count + " asset references resolved." : report.errors.Count + " asset issue(s). See the report below."; statusType = report.IsValid ? MessageType.Info : MessageType.Error; });
                if (GUILayout.Button("Check all layouts")) Try(CheckAll);
            }
            if (GUILayout.Button("Assemble selected screen", GUILayout.Height(30))) Try(() => {
                report = ScreenImporter.Inspect(selectedPath, Folder, convertTextures);
                if (!report.IsValid) throw new InvalidDataException(string.Join("\n", report.errors));
                built = ScreenImporter.Build(selectedPath, preview, font, createEvents, Folder, convertTextures);
                Selection.activeGameObject = built;
                status = "Created " + built.name + " in the active scene. Save your scene to keep it. Undo removes this assembly."; statusType = MessageType.Info;
            });
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
        DrawReport();
        EditorGUILayout.Space();
        showAnchors = EditorGUILayout.Foldout(showAnchors, "Selected UI: Unity RectTransform Inspector", true);
        if (showAnchors) DrawNativeRectInspector();
        EditorGUILayout.Space(); DrawSaveControls();
        EditorGUILayout.EndScrollView();
    }
    void CheckAll() {
        int assets = 0, errors = 0;
        foreach (var p in paths) {
            try {
                var r = ScreenImporter.Inspect(p, Folder, convertTextures); assets += r.assets.Count;
                foreach (var error in r.errors) { Debug.LogError(p + ": " + error); errors++; }
            } catch (Exception ex) { Debug.LogError(p + ": " + ex.Message); errors++; }
        }
        status = paths.Length + " layouts, " + assets + " asset references, " + errors + " issue(s)." + (errors > 0 ? " See Console for details." : " Ready to assemble.");
        statusType = errors > 0 ? MessageType.Error : MessageType.Info;
        report = ScreenImporter.Inspect(selectedPath, Folder, convertTextures);
    }
    void DrawReport() {
        if (report == null) return;
        showAssets = EditorGUILayout.Foldout(showAssets, "Asset references (" + report.assets.Count + ")", true);
        if (showAssets) foreach (var a in report.assets) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField(a.id, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Requested: " + a.requestedPath, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(a.error)) EditorGUILayout.HelpBox(a.error, MessageType.Error);
                else {
                    EditorGUILayout.LabelField(a.resolvedPath, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField(a.method, EditorStyles.miniLabel);
                    if (GUILayout.Button("Locate asset", GUILayout.Width(100))) EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(a.resolvedPath));
                }
                EditorGUILayout.LabelField("Referenced by: " + (a.elements.Length == 0 ? "alternative / unused in default layout" : string.Join(", ", a.elements)), EditorStyles.wordWrappedMiniLabel);
            }
        }
        if (GUILayout.Button("Export reference report...")) Try(() => {
            string p = EditorUtility.SaveFilePanel("Export UI asset references", "", "ui-reference-report", "json");
            if (!string.IsNullOrEmpty(p)) File.WriteAllText(p, JsonUtility.ToJson(report, true));
        });
    }
    void DrawNativeRectInspector() {
        var targets = Selection.transforms.OfType<RectTransform>().Where(t => t.gameObject.scene.IsValid()).Cast<UnityEngine.Object>().ToArray();
        if (!targets.SequenceEqual(rectTargets) || (!rectEditor && targets.Length > 0)) {
            if (rectEditor) DestroyImmediate(rectEditor);
            rectTargets = targets;
            if (targets.Length > 0) rectEditor = UnityEditor.Editor.CreateEditor(targets);
        }
        if (!rectEditor) { EditorGUILayout.HelpBox("Select one or more UI objects in the Hierarchy to edit anchors, pivot, position and size here.", MessageType.None); return; }
        EditorGUILayout.LabelField(targets.Length == 1 ? targets[0].name : targets.Length + " RectTransforms", EditorStyles.boldLabel);
        // Use Unity's own inspector, including its native anchor preset popup and modifier keys.
        rectEditor.OnInspectorGUI();
        EditorGUILayout.HelpBox("Use the anchor preset square above. Shift also sets pivot; Alt also sets position. The regular Inspector and Scene view Rect Tool work on these same objects.", MessageType.None);
    }
    void DrawSaveControls() {
        var screen = Selection.activeGameObject ? Selection.activeGameObject.GetComponentInParent<ScreenLayout>() : null;
        if (!screen && built) screen = built.GetComponent<ScreenLayout>();
        EditorGUILayout.LabelField("Save imported screen", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!screen || EditorApplication.isPlayingOrWillChangePlaymode)) {
            if (screen) {
                EditorGUILayout.ObjectField("Screen", screen, typeof(ScreenLayout), true);
                var scaler = screen.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler) {
                    EditorGUI.BeginChangeCheck(); float match = EditorGUILayout.Slider("Match width / height", scaler.matchWidthOrHeight, 0, 1);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(scaler, "Adjust canvas scaling"); scaler.matchWidthOrHeight = match; PrefabUtility.RecordPrefabInstancePropertyModifications(scaler); EditorUtility.SetDirty(scaler); }
                }
            }
            if (GUILayout.Button("Save layout overrides as JSON...")) Try(() => {
                string root = string.IsNullOrEmpty(screen.sourceAssetRoot) ? Folder : screen.sourceAssetRoot;
                string p = EditorUtility.SaveFilePanelInProject("Save layout overrides", SafeName(screen.screenId) + ".overrides", "json", "Save inside this screen's UI folder so it can be scanned again.", root);
                if (string.IsNullOrEmpty(p)) return;
                if (!LayoutAssetResolver.Inside(p, root)) throw new InvalidOperationException("Save the JSON inside the screen's UI folder: " + root);
                ScreenImporter.SaveOverrides(screen, p); Refresh(); status = "Saved " + p; statusType = MessageType.Info;
            });
            if (GUILayout.Button("Save reusable prefab...")) Try(() => {
                string p = EditorUtility.SaveFilePanelInProject("Save UI prefab", SafeName(screen.screenId), "prefab", "Save the assembled UI as a prefab. Preview-only art is excluded.");
                if (!string.IsNullOrEmpty(p)) { ScreenImporter.SavePrefab(screen.gameObject, p); status = "Saved " + p; statusType = MessageType.Info; }
            });
            if (GUILayout.Button("Export preview PNG...")) Try(() => {
                string p = EditorUtility.SaveFilePanel("Export UI preview", "", SafeName(screen.screenId), "png");
                if (!string.IsNullOrEmpty(p)) {
                    var scaler = screen.GetComponent<UnityEngine.UI.CanvasScaler>(); var res = scaler.referenceResolution;
                    ScreenImporter.Render(screen.gameObject, p, Mathf.RoundToInt(res.x), Mathf.RoundToInt(res.y)); status = "Exported " + p; statusType = MessageType.Info;
                }
            });
        }
    }
    static string SafeName(string name) { return string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)); }
    void Try(Action action) { try { action(); } catch (Exception ex) { status = ex.Message; statusType = MessageType.Error; Debug.LogException(ex); } }
}
}
