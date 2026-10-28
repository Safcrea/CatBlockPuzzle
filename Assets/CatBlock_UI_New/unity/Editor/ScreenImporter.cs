using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace CatBlocksUI.Editor {
public static class ScreenImporter {
    public const string DefaultOutputFolder = "Assets/UILayoutGenerated";
    public static string PackageRoot {
        get {
            var paths = AssetDatabase.FindAssets("ScreenImporter t:MonoScript").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith("/Editor/ScreenImporter.cs", StringComparison.Ordinal)).ToArray();
            if (paths.Length != 1) throw new InvalidOperationException("Choose a UI folder in Tools > UI Layout Importer. Install only one copy of the importer scripts.");
            return paths[0].Substring(0, paths[0].Length - "/Editor/ScreenImporter.cs".Length);
        }
    }
    public static string[] LayoutPaths() { string[] warnings; return LayoutAssetResolver.Discover(PackageRoot, out warnings); }
    public static LayoutDocument Read(string path) {
        var d = JsonUtility.FromJson<LayoutDocument>(File.ReadAllText(path)); Validate(d); return d;
    }
    static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
    static bool Finite(Vector2 v) { return Finite(v.x) && Finite(v.y); }
    static bool ValidRect(LayoutRect r) { return r != null && Finite(r.x) && Finite(r.y) && Finite(r.width) && Finite(r.height) && r.width > 0 && r.height > 0; }
    public static void Validate(LayoutDocument d) {
        if (d == null || d.schemaVersion != 1 || d.coordinateSystem != "top-left-pixels") throw new InvalidDataException("Expected layout schemaVersion 1 with top-left-pixels coordinates.");
        if (string.IsNullOrWhiteSpace(d.screenId)) throw new InvalidDataException("Missing screenId.");
        if (!Finite(d.referenceResolution) || d.referenceResolution.x <= 0 || d.referenceResolution.y <= 0) throw new InvalidDataException("referenceResolution must contain positive finite x/y values.");
        if (!Finite(d.canvasMatch) || d.canvasMatch < 0 || d.canvasMatch > 1) throw new InvalidDataException("canvasMatch must be between 0 and 1.");
        if (d.assets == null || d.elements == null || d.elements.Length == 0) throw new InvalidDataException("Supply assets and a non-empty elements array.");
        var assets = new HashSet<string>();
        foreach (var a in d.assets) if (a == null || string.IsNullOrWhiteSpace(a.id) || !assets.Add(a.id)) throw new InvalidDataException("Duplicate or empty asset ID.");
        var map = new Dictionary<string, ElementRecord>();
        foreach (var e in d.elements) {
            if (e == null || string.IsNullOrWhiteSpace(e.id) || map.ContainsKey(e.id)) throw new InvalidDataException("Duplicate or empty element ID.");
            map.Add(e.id, e);
            if (!ValidRect(e.rect)) throw new InvalidDataException("Invalid rectangle: " + e.id);
            if (!new[] { "image", "button", "text", "group", "overlay", "toggle" }.Contains(e.kind)) throw new InvalidDataException("Unknown kind: " + e.kind);
            if (!Finite(e.anchorMin) || !Finite(e.anchorMax) || !Finite(e.pivot) || !Finite(e.rotation) || e.anchorMin.x < 0 || e.anchorMin.y < 0 || e.anchorMax.x > 1 || e.anchorMax.y > 1 || e.anchorMin.x > e.anchorMax.x || e.anchorMin.y > e.anchorMax.y) throw new InvalidDataException("Invalid anchors, pivot or rotation: " + e.id);
            if (!string.IsNullOrEmpty(e.assetId) && !assets.Contains(e.assetId)) throw new InvalidDataException("Unknown asset: " + e.assetId);
            if ((e.kind == "image" || e.kind == "button") && string.IsNullOrEmpty(e.assetId)) throw new InvalidDataException("Missing assetId: " + e.id);
            if (e.kind == "text" && (!Finite(e.fontSize) || e.fontSize <= 0 || !Finite(e.outlineWidth) || e.outlineWidth < 0 || e.outlineWidth > 1)) throw new InvalidDataException("Invalid text size or outline: " + e.id);
            if (e.imageType == "filled" && (!Finite(e.fillAmount) || e.fillAmount < 0 || e.fillAmount > 1)) throw new InvalidDataException("Invalid fillAmount: " + e.id);
            if (e.kind == "toggle" && (!assets.Contains(e.onAssetId ?? "") || !assets.Contains(e.offAssetId ?? ""))) throw new InvalidDataException("Toggle state assets missing: " + e.id);
        }
        foreach (var e in d.elements) {
            var chain = new HashSet<string> { e.id }; var parent = e.parentId;
            while (!string.IsNullOrEmpty(parent)) {
                if (!map.ContainsKey(parent) || !chain.Add(parent)) throw new InvalidDataException("Missing parent or hierarchy cycle: " + e.id);
                parent = map[parent].parentId;
            }
            if (e.kind == "toggle" && !string.IsNullOrEmpty(e.knobElementId)) {
                ElementRecord knob;
                if (!map.TryGetValue(e.knobElementId, out knob) || knob.kind != "image" || knob.parentId != e.id || !assets.Contains(e.onKnobAssetId ?? "") || !assets.Contains(e.offKnobAssetId ?? "") || !ValidRect(e.knobEnabledRect) || !ValidRect(e.knobDisabledRect))
                    throw new InvalidDataException("Toggle needs an image child knob, state assets and both knob rectangles: " + e.id);
            }
        }
    }
    public static LayoutAssetResolver.ScanReport Inspect(string layoutPath, string uiFolder, bool convertTextures = true) {
        return new LayoutAssetResolver(uiFolder, layoutPath).Inspect(Read(layoutPath), layoutPath, convertTextures);
    }
    // Existing single/sliced Sprite import settings are preserved. Conversion only affects ordinary textures.
    static void ImportSprites(LayoutDocument d, LayoutAssetResolver.ScanReport report, bool convertTextures) {
        foreach (var item in report.assets) {
            var ti = AssetImporter.GetAtPath(item.resolvedPath) as TextureImporter;
            if (!ti) continue;
            if (ti.textureType == TextureImporterType.Sprite) {
                var sliced = d.assets.First(v => v.id == item.id);
                if (d.elements.Any(e => e.assetId == sliced.id && e.imageType == "sliced") && ti.spriteImportMode == SpriteImportMode.Single && ti.spriteBorder != sliced.border) {
                    ti.spriteBorder = sliced.border; ti.SaveAndReimport();
                }
                continue;
            }
            if (!convertTextures) throw new InvalidOperationException("Import as Sprite first: " + item.resolvedPath);
            var a = d.assets.First(v => v.id == item.id);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false; ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Clamp; ti.spritePixelsPerUnit = 100; ti.spriteBorder = a.border;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = Mathf.Clamp(Mathf.Max(ti.maxTextureSize, Mathf.NextPowerOfTwo(Mathf.Max(a.width, a.height))), 32, 16384);
            var settings = new TextureImporterSettings(); ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteExtrude = 0; ti.SetTextureSettings(settings); ti.SaveAndReimport();
        }
    }
    public static void ImportSprites(LayoutDocument d) {
        var resolver = new LayoutAssetResolver(PackageRoot, PackageRoot + "/layout.json"); var report = resolver.Inspect(d, "", true);
        if (!report.IsValid) throw new InvalidDataException(string.Join("\n", report.errors));
        ImportSprites(d, report, true);
    }
    static string OutputFolder(string path) {
        path = string.IsNullOrEmpty(path) ? DefaultOutputFolder : path.Replace('\\', '/').TrimEnd('/');
        if (!LayoutAssetResolver.Inside(path, "Assets")) throw new InvalidDataException("Output must be a folder under Assets.");
        Directory.CreateDirectory(path); AssetDatabase.Refresh(); return LayoutAssetResolver.RequireFolder(path);
    }
    public static TMP_FontAsset EnsureFont(string uiFolder = null, string outputFolder = null) {
        uiFolder = string.IsNullOrEmpty(uiFolder) ? PackageRoot : LayoutAssetResolver.RequireFolder(uiFolder);
        var fonts = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { uiFolder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        foreach (var path in fonts) { var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if (asset) return asset; }
        if (TMP_Settings.defaultFontAsset) return TMP_Settings.defaultFontAsset;
        var fontPaths = AssetDatabase.FindAssets("t:Font", new[] { uiFolder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var source = fontPaths.FirstOrDefault(p => p.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".otf", StringComparison.OrdinalIgnoreCase));
        if (source == null) throw new InvalidOperationException("Choose a TextMeshPro font, import TMP Essential Resources, or include a TTF/OTF in the UI folder.");
        string folder = OutputFolder(outputFolder);
        string dest = folder + "/" + Path.GetFileNameWithoutExtension(source) + "-" + AssetDatabase.AssetPathToGUID(source) + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(dest); if (existing) return existing;
        if (!Shader.Find("TextMeshPro/Mobile/Distance Field")) foreach (var p in AssetDatabase.FindAssets("TMP_SDF-Mobile t:Shader").Select(AssetDatabase.GUIDToAssetPath)) AssetDatabase.ImportAsset(p);
        var fa = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source), 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (!fa || !fa.material) throw new InvalidOperationException("TMP font creation failed. Import TMP Essential Resources and check its shaders.");
        fa.name = Path.GetFileNameWithoutExtension(source) + " SDF";
        AssetDatabase.CreateAsset(fa, dest); AssetDatabase.AddObjectToAsset(fa.material, fa);
        foreach (var tex in fa.atlasTextures) AssetDatabase.AddObjectToAsset(tex, fa);
        fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:$%+-/()!? ");
        EditorUtility.SetDirty(fa); AssetDatabase.SaveAssets(); return fa;
    }
    public static GameObject Build(string layoutPath, bool includePreview, TMP_FontAsset font = null, bool sceneEvents = true, string uiFolder = null, bool convertTextures = true, string outputFolder = null) {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Assemble UI in Edit Mode.");
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) throw new InvalidOperationException("Close Prefab Mode and open the game scene before assembling UI.");
        uiFolder = string.IsNullOrEmpty(uiFolder) ? PackageRoot : LayoutAssetResolver.RequireFolder(uiFolder);
        var d = Read(layoutPath); var report = new LayoutAssetResolver(uiFolder, layoutPath).Inspect(d, layoutPath, convertTextures);
        if (!report.IsValid) throw new InvalidDataException(string.Join("\n", report.errors));
        if (!font && d.elements.Any(e => e.kind == "text")) font = EnsureFont(uiFolder, outputFolder);
        ImportSprites(d, report, convertTextures);
        var sprites = report.assets.ToDictionary(a => a.id, a => LayoutAssetResolver.LoadSprite(d.assets.First(v => v.id == a.id), a.resolvedPath));
        var records = d.elements.ToDictionary(e => e.id); var excluded = new HashSet<string>();
        if (!includePreview) foreach (var e in d.elements) { var p = e; while (p != null) { if (p.previewOnly) { excluded.Add(e.id); break; } p = string.IsNullOrEmpty(p.parentId) ? null : records[p.parentId]; } }
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Assemble UI layout");
        GameObject root = null;
        try {
            root = new GameObject(d.screenId, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(ScreenLayout));
            root.SetActive(false);
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open a game scene before assembling UI.");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = d.referenceResolution; scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = d.canvasMatch; scaler.referencePixelsPerUnit = 100;
            var rootRect = (RectTransform)root.transform; rootRect.sizeDelta = d.referenceResolution;
            var info = root.GetComponent<ScreenLayout>(); info.screenId = d.screenId; info.sourceLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(layoutPath); info.sourceAssetRoot = uiFolder;
            var objects = new Dictionary<string, RectTransform>(); var texts = new List<TextMeshProUGUI>();
            foreach (var e in d.elements.OrderBy(e => e.order)) {
                if (excluded.Contains(e.id)) continue;
                var go = new GameObject(e.id, typeof(RectTransform), typeof(LayoutElement)); var rt = (RectTransform)go.transform; rt.SetParent(rootRect, false); objects[e.id] = rt;
                var tag = go.GetComponent<LayoutElement>(); tag.elementId = e.id; tag.binding = e.binding; tag.previewOnly = e.previewOnly;
                if (e.kind == "text") {
                    var t = go.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = e.text ?? ""; t.fontSize = e.fontSize; t.alignment = TextAlignmentOptions.Center;
#if UNITY_6000_0_OR_NEWER
                    t.textWrappingMode = TextWrappingModes.NoWrap;
#else
                    t.enableWordWrapping = false;
#endif
                    t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false;
                    Color c; if (ColorUtility.TryParseHtmlString(e.color, out c)) t.color = c;
                    if (e.outlineWidth > 0) {
                        string key = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font)) + "-" + (e.outlineColor ?? "FFFFFF").Replace("#", "") + "-" + Mathf.RoundToInt(e.outlineWidth * 1000);
                        string mp = OutputFolder(outputFolder) + "/Outline-" + key + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
                        if (!mat) { mat = new Material(font.material); if (ColorUtility.TryParseHtmlString(e.outlineColor, out c)) mat.SetColor(ShaderUtilities.ID_OutlineColor, c); mat.SetFloat(ShaderUtilities.ID_OutlineWidth, e.outlineWidth); AssetDatabase.CreateAsset(mat, mp); }
                        t.fontSharedMaterial = mat;
                    }
                    texts.Add(t);
                } else if (e.kind != "group") {
                    var im = go.AddComponent<UnityEngine.UI.Image>();
                    if (e.kind == "overlay") { Color c; if (ColorUtility.TryParseHtmlString(e.color, out c)) im.color = c; }
                    else im.sprite = sprites[e.kind == "toggle" ? (e.isOn ? e.onAssetId : e.offAssetId) : e.assetId];
                    im.raycastTarget = e.kind == "overlay"; im.preserveAspect = e.preserveAspect;
                    if (e.imageType == "filled") { im.type = UnityEngine.UI.Image.Type.Filled; im.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; im.fillOrigin = 0; im.fillAmount = e.fillAmount; }
                    else if (e.imageType == "sliced") {
                        im.type = UnityEngine.UI.Image.Type.Sliced;
                        // Preserve the artwork's natural width scale in all fixed slice regions.
                        // Otherwise Unity keeps borders at native UI units and squashes the header.
                        var record = d.assets.First(a => a.id == e.assetId);
                        im.pixelsPerUnitMultiplier = (im.sprite ? 100f / im.sprite.pixelsPerUnit : 1f) * record.width / e.rect.width;
                    }
                    if (e.kind == "button") { var button = go.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = im; button.transition = UnityEngine.UI.Selectable.Transition.ColorTint; }
                    if (e.kind == "toggle") { var toggle = go.AddComponent<UnityEngine.UI.Toggle>(); toggle.targetGraphic = im; toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None; toggle.isOn = e.isOn; go.AddComponent<ToggleSpriteState>().Configure(sprites[e.onAssetId], sprites[e.offAssetId]); }
                    if (e.kind == "button" || e.kind == "toggle") {
                        var hit = new GameObject("Hitbox", typeof(RectTransform), typeof(UnityEngine.UI.Image)); var hr = (RectTransform)hit.transform; hr.SetParent(rt, false);
                        hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one; hr.pivot = new Vector2(.5f, .5f);
                        float left = e.hitbox != null && e.screenRect != null ? e.hitbox.x - e.screenRect.x : 0;
                        float top = e.hitbox != null && e.screenRect != null ? e.hitbox.y - e.screenRect.y : 0;
                        float right = e.hitbox != null && e.screenRect != null ? e.screenRect.width - left - e.hitbox.width : 0;
                        float bottom = e.hitbox != null && e.screenRect != null ? e.screenRect.height - top - e.hitbox.height : 0;
                        hr.offsetMin = new Vector2(left, bottom); hr.offsetMax = new Vector2(-right, -top);
                        var graphic = hit.GetComponent<UnityEngine.UI.Image>(); graphic.color = Color.clear; graphic.raycastTarget = true;
                    }
                }
            }
            var placed = new HashSet<string>(); Action<ElementRecord> place = null;
            place = e => {
                if (excluded.Contains(e.id) || placed.Contains(e.id)) return;
                if (!string.IsNullOrEmpty(e.parentId)) place(records[e.parentId]);
                var rt = objects[e.id]; var parent = string.IsNullOrEmpty(e.parentId) ? rootRect : objects[e.parentId];
                rt.SetParent(parent, false); rt.anchorMin = e.anchorMin; rt.anchorMax = e.anchorMax; rt.pivot = e.pivot; rt.localRotation = Quaternion.Euler(0, 0, e.rotation);
                ApplyRect(rt, e.rect, string.IsNullOrEmpty(e.parentId) ? d.referenceResolution : records[e.parentId].rect.size); placed.Add(e.id);
            };
            foreach (var e in d.elements.OrderBy(e => e.order)) place(e);
            foreach (var group in d.elements.Where(e => !excluded.Contains(e.id)).GroupBy(e => e.parentId ?? "")) foreach (var e in group.OrderBy(e => e.order)) objects[e.id].SetAsLastSibling();
            foreach (var e in d.elements) {
                RectTransform tr, kr;
                if (e.kind == "toggle" && !string.IsNullOrEmpty(e.knobElementId) && objects.TryGetValue(e.id, out tr) && objects.TryGetValue(e.knobElementId, out kr))
                    tr.GetComponent<ToggleSpriteState>().ConfigureKnob(kr.GetComponent<UnityEngine.UI.Image>(), sprites[e.onKnobAssetId], sprites[e.offKnobAssetId], e.knobEnabledRect, e.knobDisabledRect);
            }
            foreach (var e in d.elements) if (objects.ContainsKey(e.id)) objects[e.id].gameObject.SetActive(e.active);
            info.dynamicText = texts.ToArray(); root.SetActive(true); Canvas.ForceUpdateCanvases();
            Undo.RegisterCreatedObjectUndo(root, "Assemble UI layout");
            if (sceneEvents) EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(undo); return root;
        } catch { Undo.RevertAllDownToGroup(undo); if (root) UnityEngine.Object.DestroyImmediate(root); throw; }
    }
    static void EnsureEventSystem(UnityEngine.SceneManagement.Scene scene) {
        var systems = Resources.FindObjectsOfTypeAll<EventSystem>();
        var active = systems.FirstOrDefault(e => e.gameObject.scene.IsValid() && e.gameObject.scene.isLoaded && e.enabled && e.gameObject.activeInHierarchy);
        if (active) {
            if (!active.GetComponents<BaseInputModule>().Any(m => m.enabled)) Debug.LogWarning("The existing EventSystem needs an enabled input module for UI interaction.", active);
            return;
        }
        var existing = systems.FirstOrDefault(e => e.gameObject.scene == scene);
        if (existing) {
            if (!existing.gameObject.activeInHierarchy || !existing.enabled || !existing.GetComponents<BaseInputModule>().Any(m => m.enabled)) Debug.LogWarning("An inactive or unconfigured EventSystem already exists in this scene. Enable it and its input module for UI interaction.", existing);
            return;
        }
        var ev = new GameObject("EventSystem", typeof(EventSystem));
        try {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ev, scene);
#if ENABLE_INPUT_SYSTEM
            var module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module == null) throw new InvalidOperationException("Input System is enabled but its UI input module is unavailable.");
            var component = ev.AddComponent(module); var assign = module.GetMethod("AssignDefaultActions", Type.EmptyTypes); if (assign != null) assign.Invoke(component, null);
#else
            ev.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(ev, "Create UI EventSystem");
        } catch { UnityEngine.Object.DestroyImmediate(ev); throw; }
    }
    public static void ApplyRect(RectTransform rt, Rect topLeft, Vector2 parentSize) {
        Vector2 size = topLeft.size; rt.sizeDelta = size - Vector2.Scale(parentSize, rt.anchorMax - rt.anchorMin);
        Vector2 pivotPosition = new Vector2(topLeft.x + size.x * rt.pivot.x, parentSize.y - topLeft.y - size.y * (1 - rt.pivot.y));
        Vector2 anchorPosition = Vector2.Scale(parentSize, rt.anchorMin + Vector2.Scale(rt.anchorMax - rt.anchorMin, rt.pivot)); rt.anchoredPosition = pivotPosition - anchorPosition;
    }
    public static void ChangeAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, bool preserve) {
        if (!rt || !(rt.parent is RectTransform)) return;
        var parent = (RectTransform)rt.parent;
        Undo.RecordObject(rt, "Change UI anchors"); Vector3 local = rt.localPosition; Vector2 size = rt.rect.size; Vector2 oldPivot = rt.pivot;
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
        if (preserve) { rt.sizeDelta = size - Vector2.Scale(parent.rect.size, max - min); rt.localPosition = local + rt.localRotation * Vector3.Scale(Vector2.Scale(size, pivot - oldPivot), rt.localScale); }
        PrefabUtility.RecordPrefabInstancePropertyModifications(rt); EditorUtility.SetDirty(rt);
    }
    public static void SaveOverrides(ScreenLayout screen, string path) {
        if (!screen || !screen.sourceLayout) throw new InvalidOperationException("Select an imported screen.");
        if (!LayoutAssetResolver.Inside(path, "Assets")) throw new InvalidOperationException("Save JSON inside Assets.");
        var sourcePath = AssetDatabase.GetAssetPath(screen.sourceLayout); var d = Read(sourcePath); var root = (RectTransform)screen.transform;
        var uiFolder = string.IsNullOrEmpty(screen.sourceAssetRoot) ? PackageRoot : screen.sourceAssetRoot; var resolver = new LayoutAssetResolver(uiFolder, sourcePath);
        foreach (var a in d.assets) { string method; var resolved = resolver.Resolve(a, out method); a.path = resolved.Substring(resolver.Root.Length + 1); a.filename = a.path; a.guid = AssetDatabase.AssetPathToGUID(resolved); }
        var tags = screen.GetComponentsInChildren<LayoutElement>(true).ToDictionary(t => t.elementId); var records = d.elements.ToDictionary(e => e.id); var saved = new HashSet<string>();
        Action<ElementRecord> save = null;
        save = e => {
            if (saved.Contains(e.id)) return;
            if (!string.IsNullOrEmpty(e.parentId)) save(records[e.parentId]); saved.Add(e.id);
            LayoutElement tag; if (!tags.TryGetValue(e.id, out tag)) return;
            var rt = (RectTransform)tag.transform;
            var expectedParent = string.IsNullOrEmpty(e.parentId) ? root : (tags.ContainsKey(e.parentId) ? (RectTransform)tags[e.parentId].transform : null);
            if (rt.parent != expectedParent) throw new InvalidOperationException("Hierarchy changed for " + e.id + ". Restore its JSON parent before saving overrides.");
            if ((rt.localScale - Vector3.one).sqrMagnitude > .00001f) throw new InvalidOperationException("Use RectTransform width/height instead of local scale for " + e.id + " before saving JSON.");
            e.anchorMin = rt.anchorMin; e.anchorMax = rt.anchorMax; e.pivot = rt.pivot; e.rotation = rt.localEulerAngles.z;
            // Save reference-resolution geometry, independent of the Game view's current aspect ratio.
            Vector2 parentSize = string.IsNullOrEmpty(e.parentId) ? d.referenceResolution : records[e.parentId].rect.size;
            Vector2 size = rt.sizeDelta + Vector2.Scale(parentSize, rt.anchorMax - rt.anchorMin);
            Vector2 anchor = Vector2.Scale(parentSize, rt.anchorMin + Vector2.Scale(rt.anchorMax - rt.anchorMin, rt.pivot)); Vector2 pos = rt.anchoredPosition + anchor;
            e.rect = new Rect(pos.x - size.x * rt.pivot.x, parentSize.y - pos.y - size.y * (1 - rt.pivot.y), size.x, size.y);
            var tmp = tag.GetComponent<TextMeshProUGUI>(); if (tmp) { e.text = tmp.text; e.fontSize = tmp.fontSize; e.color = "#" + ColorUtility.ToHtmlStringRGBA(tmp.color); }
            var toggle = tag.GetComponent<UnityEngine.UI.Toggle>(); if (toggle) e.isOn = toggle.isOn;
            e.active = tag.gameObject.activeSelf;
        };
        foreach (var e in d.elements) save(e);
        d.canvasMatch = screen.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight;
        foreach (var e in d.elements) if (e.kind == "toggle" && !string.IsNullOrEmpty(e.knobElementId)) { var knob = records[e.knobElementId]; if (e.isOn) e.knobEnabledRect = knob.rect; else e.knobDisabledRect = knob.rect; }
        Validate(d); LayoutMetadata.Refresh(d); File.WriteAllText(path, JsonUtility.ToJson(d, true)); AssetDatabase.ImportAsset(path.Replace('\\', '/'));
        Undo.RecordObject(screen, "Assign saved layout JSON"); screen.sourceLayout = AssetDatabase.LoadAssetAtPath<TextAsset>(path.Replace('\\', '/')); screen.sourceAssetRoot = uiFolder;
        PrefabUtility.RecordPrefabInstancePropertyModifications(screen); EditorUtility.SetDirty(screen);
    }
    public static string SavePrefab(GameObject root, string target = null) {
        if (!root) throw new ArgumentNullException("root");
        if (string.IsNullOrEmpty(target)) {
            string dir = OutputFolder(DefaultOutputFolder + "/Prefabs"); string safeName = string.Concat(root.name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            target = AssetDatabase.GenerateUniqueAssetPath(dir + "/" + safeName + ".prefab");
        }
        if (!LayoutAssetResolver.Inside(target, "Assets") || !target.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Save a .prefab inside Assets.");
        var clone = UnityEngine.Object.Instantiate(root); clone.name = root.name; clone.SetActive(false);
        try {
            foreach (var tag in clone.GetComponentsInChildren<LayoutElement>(true)) if (tag && tag.previewOnly) UnityEngine.Object.DestroyImmediate(tag.gameObject);
            clone.SetActive(root.activeSelf); if (!PrefabUtility.SaveAsPrefabAsset(clone, target)) throw new IOException("Could not save prefab: " + target);
        } finally { UnityEngine.Object.DestroyImmediate(clone); }
        AssetDatabase.SaveAssets(); return target;
    }
    public static void Render(GameObject root, string target, int width = 1080, int height = 1920) {
        if (width <= 0 || height <= 0) throw new ArgumentException("Preview dimensions must be positive.");
        var canvas = root.GetComponent<Canvas>(); var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); var rt = (RectTransform)root.transform;
        var previousMode = canvas.renderMode; bool scalerEnabled = scaler.enabled; Vector2 oldSize = rt.sizeDelta; Vector3 oldPosition = rt.position, oldScale = rt.localScale; Quaternion oldRotation = rt.rotation;
        var reference = scaler.referenceResolution;
        float scale = Mathf.Pow(width / reference.x, 1 - scaler.matchWidthOrHeight) * Mathf.Pow(height / reference.y, scaler.matchWidthOrHeight); Vector2 viewSize = new Vector2(width / scale, height / scale);
        var cameraObject = new GameObject("UI Preview Camera", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = viewSize.y / 2; camera.aspect = (float)width / height; camera.transform.position = new Vector3(0, 0, -100); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 30;
        var layers = root.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => t.gameObject.layer); foreach (var pair in layers) pair.Key.gameObject.layer = 30;
        var targetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); var oldActive = RenderTexture.active;
        try {
            scaler.enabled = false; canvas.renderMode = RenderMode.WorldSpace; rt.sizeDelta = viewSize; root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity; root.transform.localScale = Vector3.one;
            camera.targetTexture = targetTexture; Canvas.ForceUpdateCanvases(); foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>()) t.ForceMeshUpdate(); camera.Render(); RenderTexture.active = targetTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try { texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); string dir = Path.GetDirectoryName(target); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir); File.WriteAllBytes(target, texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        } finally {
            camera.targetTexture = null; RenderTexture.active = oldActive; targetTexture.Release(); UnityEngine.Object.DestroyImmediate(targetTexture); UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach (var pair in layers) if (pair.Key) pair.Key.gameObject.layer = pair.Value;
            canvas.renderMode = previousMode; rt.sizeDelta = oldSize; rt.position = oldPosition; rt.rotation = oldRotation; rt.localScale = oldScale; scaler.enabled = scalerEnabled;
        }
    }
}
}
