using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PSD2UIPro.Editor
{
    public class Psd2UiProWindow : EditorWindow
    {
        #region Serialized Settings
        [SerializeField] private string jsonPath = "";
        [SerializeField] private string importSpritesFolder = "Assets/PSD2UI Imports";
        [SerializeField] private RectTransform targetCanvas;
        [SerializeField] private bool createTextObjects = true;
        [SerializeField] private bool respectVisibility = true;
        [SerializeField] private bool copyImagesIntoProject = true;
        [SerializeField] private PivotMode pivotMode = PivotMode.Center;
        #endregion

        #region UI State
        private Vector2 scrollPos;
        private bool isDragHovering;
        #endregion

        #region Preview Cache
        private string previewCachedPath;
        private bool previewValid;
        private string previewDocName;
        private int previewDocWidth, previewDocHeight;
        private int previewNodeCount, previewImageCount, previewTextCount, previewGroupCount;
        #endregion

        #region Import Results
        private bool showResults;
        private string resultDocName;
        private int resultNodeCount, resultImageCount, resultTextCount, resultGroupCount;
        private double resultDuration;
        #endregion

        #region Cached Styles
        private bool stylesReady;
        private GUIStyle headerTitle;
        private GUIStyle headerSub;
        private GUIStyle versionLabel;
        private GUIStyle sectionBox;
        private GUIStyle sectionTitle;
        private GUIStyle dropZoneLabel;
        private GUIStyle importBtn;
        private GUIStyle hintLabel;
        private GUIStyle richLabel;
        private GUIStyle overlayLabel;
        private GUIStyle statValue;

        private static readonly Color AccentBlue = new Color(0.33f, 0.60f, 0.87f);
        private static readonly Color SuccessGreen = new Color(0.33f, 0.78f, 0.47f);
        private static readonly Color ErrorRed = new Color(0.87f, 0.33f, 0.33f);
        private static readonly Color Subtle = new Color(0.55f, 0.55f, 0.55f);
        #endregion

        public enum PivotMode
        {
            Center,
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        private static Vector2 PivotModeToVector(PivotMode mode)
        {
            switch (mode)
            {
                case PivotMode.TopLeft:      return new Vector2(0f, 1f);
                case PivotMode.TopCenter:    return new Vector2(0.5f, 1f);
                case PivotMode.TopRight:     return new Vector2(1f, 1f);
                case PivotMode.MiddleLeft:   return new Vector2(0f, 0.5f);
                case PivotMode.MiddleRight:  return new Vector2(1f, 0.5f);
                case PivotMode.BottomLeft:   return new Vector2(0f, 0f);
                case PivotMode.BottomCenter: return new Vector2(0.5f, 0f);
                case PivotMode.BottomRight:  return new Vector2(1f, 0f);
                default:                     return new Vector2(0.5f, 0.5f);
            }
        }

        private class RuntimeNode
        {
            public PsExportNode Data;
            public GameObject GameObject;
            public RectTransform RectTransform;
        }

        [MenuItem("Window/PSD2UI Pro")]
        public static void ShowWindow()
        {
            var window = GetWindow<Psd2UiProWindow>("PSD2UI Pro");
            window.minSize = new Vector2(480f, 540f);
            window.Show();
        }


        #region Styles

        private void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            bool dk = EditorGUIUtility.isProSkin;

            headerTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                padding = new RectOffset(0, 0, 0, 0)
            };

            headerSub = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Subtle }
            };

            versionLabel = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Subtle }
            };

            sectionBox = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(4, 4, 2, 2)
            };

            sectionTitle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = dk ? new Color(0.50f, 0.58f, 0.70f) : new Color(0.30f, 0.38f, 0.55f) },
                padding = new RectOffset(0, 0, 0, 6)
            };

            dropZoneLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            importBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 38,
                richText = true
            };

            hintLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 10,
                padding = new RectOffset(0, 0, 4, 0)
            };

            richLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 11,
                padding = new RectOffset(4, 4, 1, 1)
            };

            overlayLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = AccentBlue }
            };

            statValue = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }


        #endregion

        #region Main Layout

        private void OnGUI()
        {
            EnsureStyles();
            UpdatePreviewCache();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            EditorGUILayout.Space(6);
            DrawSourceSection();
            EditorGUILayout.Space(2);
            DrawSettingsSection();
            EditorGUILayout.Space(2);
            DrawImportSection();

            if (showResults)
            {
                EditorGUILayout.Space(2);
                DrawResultsSection();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();

            HandleDragAndDrop();
            if (isDragHovering) DrawDragOverlay();
        }


        #endregion

        #region Header

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("PSD2UI Pro", headerTitle);
            GUILayout.Label("Import PSD designs as Unity UI", headerSub);
            EditorGUILayout.EndVertical();
            GUILayout.Label("v1.0", versionLabel, GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            Rect sep = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sep, EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.72f, 0.72f, 0.72f));
        }


        #endregion

        #region Source

        private void DrawSourceSection()
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("SOURCE", sectionTitle);

            // Drop zone
            bool dk = EditorGUIUtility.isProSkin;
            Rect dz = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            Color dzBg = dk ? new Color(0.17f, 0.19f, 0.22f) : new Color(0.90f, 0.92f, 0.95f);
            Color dzBorder = isDragHovering
                ? AccentBlue
                : (dk ? new Color(0.28f, 0.30f, 0.34f) : new Color(0.68f, 0.70f, 0.74f));
            EditorGUI.DrawRect(dz, dzBg);
            DrawBorder(dz, dzBorder, isDragHovering ? 2f : 1f);
            GUI.Label(dz, isDragHovering
                ? "Release to load"
                : "Drag & drop layout.json here", dropZoneLabel);

            EditorGUILayout.Space(8);

            // Path field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Layout JSON");
            jsonPath = EditorGUILayout.TextField(jsonPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string p = EditorUtility.OpenFilePanel("Select layout JSON", "", "json");
                if (!string.IsNullOrEmpty(p))
                {
                    jsonPath = p;
                    previewCachedPath = null;
                    showResults = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Preview / validation
            if (previewValid || !string.IsNullOrEmpty(jsonPath))
            {
                EditorGUILayout.Space(6);
                DrawPreviewInfo();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewInfo()
        {
            if (previewValid)
            {
                string gh = ColorUtility.ToHtmlStringRGB(SuccessGreen);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"<color=#{gh}>\u2713</color>  <b>{previewDocName}</b>",
                    richLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    $"{previewDocWidth} \u00d7 {previewDocHeight}",
                    richLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                DrawStatCards(previewImageCount, previewTextCount, previewGroupCount, previewNodeCount);
            }
            else
            {
                string rh = ColorUtility.ToHtmlStringRGB(ErrorRed);
                string msg = File.Exists(jsonPath) ? "Invalid layout JSON" : "File not found";
                GUILayout.Label($"<color=#{rh}>\u2715  {msg}</color>", richLabel);
            }
        }

        private void DrawStatCards(int images, int text, int groups, int total)
        {
            bool dk = EditorGUIUtility.isProSkin;
            Color cardBg = dk ? new Color(0.20f, 0.20f, 0.22f) : new Color(0.88f, 0.88f, 0.90f);

            EditorGUILayout.BeginHorizontal();
            DrawSingleStat(cardBg, images.ToString(), "images", dk);
            GUILayout.Space(4);
            DrawSingleStat(cardBg, text.ToString(), "text", dk);
            GUILayout.Space(4);
            DrawSingleStat(cardBg, groups.ToString(), "groups", dk);
            GUILayout.Space(4);
            DrawSingleStat(cardBg, total.ToString(), "total", dk);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSingleStat(Color bg, string value, string label, bool dk)
        {
            EditorGUILayout.BeginVertical();
            Rect card = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(card, bg);
            DrawBorder(card, dk ? new Color(0.26f, 0.26f, 0.28f) : new Color(0.76f, 0.76f, 0.78f));

            float half = card.height * 0.55f;
            Rect valRect = new Rect(card.x, card.y + 2, card.width, half);
            Rect lblRect = new Rect(card.x, card.y + half - 2, card.width, card.height - half);

            GUI.Label(valRect, value, statValue);
            var miniCentered = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.UpperCenter
            };
            GUI.Label(lblRect, label, miniCentered);
            EditorGUILayout.EndVertical();
        }


        #endregion

        #region Settings

        private void DrawSettingsSection()
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("IMPORT", sectionTitle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(
                new GUIContent("Destination", "Folder under Assets/ where imported sprites are stored"));
            importSpritesFolder = EditorGUILayout.TextField(importSpritesFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string p = EditorUtility.OpenFolderPanel(
                    "Select import destination under Assets", Application.dataPath, "");
                if (!string.IsNullOrEmpty(p)) importSpritesFolder = p;
            }
            EditorGUILayout.EndHorizontal();

            targetCanvas = (RectTransform)EditorGUILayout.ObjectField(
                new GUIContent("Target Canvas", "Parent canvas for imported UI. Auto-created if empty."),
                targetCanvas, typeof(RectTransform), true);

            pivotMode = (PivotMode)EditorGUILayout.EnumPopup(
                new GUIContent("Layer Pivot", "Pivot point for each imported RectTransform"),
                pivotMode);

            EditorGUILayout.Space(6);
            Rect line = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, new Color(0.5f, 0.5f, 0.5f, 0.12f));
            EditorGUILayout.Space(4);

            GUILayout.Label("CONTENT", sectionTitle);

            createTextObjects = EditorGUILayout.Toggle(
                new GUIContent("Create Text Objects",
                    "Text layers become editable TextMeshPro components.\n" +
                    "When enabled, TMP takes priority over rasterized PNGs for text layers."),
                createTextObjects);

            respectVisibility = EditorGUILayout.Toggle(
                new GUIContent("Respect Visibility",
                    "Layers hidden in Photoshop become inactive GameObjects in Unity."),
                respectVisibility);

            copyImagesIntoProject = EditorGUILayout.Toggle(
                new GUIContent("Copy Images Into Project",
                    "Copies PNGs from the export folder into Unity Assets.\n" +
                    "Recommended for project portability."),
                copyImagesIntoProject);

            EditorGUILayout.EndVertical();
        }


        #endregion

        #region Import Button

        private void DrawImportSection()
        {
            bool canImport = previewValid;

            EditorGUILayout.BeginVertical(sectionBox);

            Color prevBg = GUI.backgroundColor;
            if (canImport) GUI.backgroundColor = new Color(0.30f, 0.55f, 0.85f);
            GUI.enabled = canImport;

            string label = previewValid && !string.IsNullOrEmpty(previewDocName)
                ? $"Import \u201c{previewDocName}\u201d"
                : "Import Layout";

            if (GUILayout.Button(label, importBtn))
                ImportLayout();

            GUI.enabled = true;
            GUI.backgroundColor = prevBg;

            GUILayout.Label("The entire import is a single undo step \u2014 Ctrl+Z to revert", hintLabel);

            EditorGUILayout.EndVertical();
        }


        #endregion

        #region Results

        private void DrawResultsSection()
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("LAST IMPORT", sectionTitle);

            string gh = ColorUtility.ToHtmlStringRGB(SuccessGreen);
            GUILayout.Label(
                $"<color=#{gh}>\u2713</color>  Imported <b>{resultNodeCount}</b> nodes from " +
                $"<b>{resultDocName}</b> in {resultDuration:F1}s",
                richLabel);

            EditorGUILayout.Space(4);
            DrawStatCards(resultImageCount, resultTextCount, resultGroupCount, resultNodeCount);

            EditorGUILayout.EndVertical();
        }


        #endregion

        #region Drag and Drop

        private void HandleDragAndDrop()
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (HasJsonInDrag())
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        isDragHovering = true;
                        evt.Use();
                        Repaint();
                    }
                    break;

                case EventType.DragPerform:
                    string path = GetJsonFromDrag();
                    if (!string.IsNullOrEmpty(path))
                    {
                        DragAndDrop.AcceptDrag();
                        jsonPath = path;
                        previewCachedPath = null;
                        showResults = false;
                    }
                    isDragHovering = false;
                    evt.Use();
                    Repaint();
                    break;

                case EventType.DragExited:
                    isDragHovering = false;
                    Repaint();
                    break;
            }
        }

        private static bool HasJsonInDrag()
        {
            if (DragAndDrop.paths == null) return false;
            for (int i = 0; i < DragAndDrop.paths.Length; i++)
                if (DragAndDrop.paths[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetJsonFromDrag()
        {
            if (DragAndDrop.paths == null) return null;
            for (int i = 0; i < DragAndDrop.paths.Length; i++)
                if (DragAndDrop.paths[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return DragAndDrop.paths[i];
            return null;
        }

        private void DrawDragOverlay()
        {
            Rect wr = new Rect(0, 0, position.width, position.height);
            Color bg = EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.22f, 0.45f, 0.18f)
                : new Color(0.20f, 0.40f, 0.80f, 0.08f);
            EditorGUI.DrawRect(wr, bg);
            DrawBorder(wr, AccentBlue, 2f);
            GUI.Label(wr, "Drop layout.json", overlayLabel);
        }


        #endregion

        #region Drawing Helpers

        private static void DrawBorder(Rect r, Color c, float w = 1f)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
        }


        #endregion

        #region Preview Cache

        private void UpdatePreviewCache()
        {
            if (previewCachedPath == jsonPath) return;
            previewCachedPath = jsonPath;
            previewValid = false;

            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) return;

            try
            {
                string json = File.ReadAllText(jsonPath);
                PsExportLayout layout = JsonUtility.FromJson<PsExportLayout>(json);
                if (layout == null || layout.document == null) return;

                previewDocName = layout.document.name ?? Path.GetFileNameWithoutExtension(jsonPath);
                previewDocWidth = layout.document.width;
                previewDocHeight = layout.document.height;

                var nodes = BuildNodeList(layout);
                previewNodeCount = nodes.Count;
                previewImageCount = previewTextCount = previewGroupCount = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].isGroup) previewGroupCount++;
                    else if (nodes[i].isText) previewTextCount++;
                    else previewImageCount++;
                }
                previewValid = previewNodeCount > 0;
            }
            catch
            {
                previewValid = false;
            }
        }


        #endregion

        #region Import Logic

        private void ImportLayout()
        {
            showResults = false;
            double startTime = EditorApplication.timeSinceStartup;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("PSD2UI Import");

            try
            {
                string json = File.ReadAllText(jsonPath);
                PsExportLayout layout = JsonUtility.FromJson<PsExportLayout>(json);
                if (layout == null || layout.document == null)
                {
                    Debug.LogError("[PSD2UI] Invalid layout JSON.");
                    return;
                }

                List<PsExportNode> nodes = BuildNodeList(layout);
                if (nodes.Count == 0)
                {
                    Debug.LogError("[PSD2UI] Layout has no nodes/layers.");
                    return;
                }

                string sourceImagesFolder = ResolveSourceImagesFolder();
                Dictionary<string, string> fileToAssetPath = PrepareImageAssets(nodes, sourceImagesFolder);
                Dictionary<string, Sprite> spriteCache = LoadSprites(fileToAssetPath);

                RectTransform canvasRt = EnsureCanvas(layout.document.width, layout.document.height);
                RectTransform rootRt = CreateRoot(canvasRt, layout.document);
                BuildHierarchy(nodes, rootRt, spriteCache);

                Undo.CollapseUndoOperations(undoGroup);

                resultDocName = layout.document.name ?? Path.GetFileNameWithoutExtension(jsonPath);
                resultNodeCount = nodes.Count;
                resultImageCount = resultTextCount = resultGroupCount = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].isGroup) resultGroupCount++;
                    else if (nodes[i].isText) resultTextCount++;
                    else resultImageCount++;
                }
                resultDuration = EditorApplication.timeSinceStartup - startTime;
                showResults = true;

                Debug.Log($"[PSD2UI] Imported {resultNodeCount} nodes from '{resultDocName}' in {resultDuration:F1}s.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PSD2UI] Failed:\n{ex}");
            }
        }


        #endregion

        #region Node Tree

        private List<PsExportNode> BuildNodeList(PsExportLayout layout)
        {
            var nodes = new List<PsExportNode>();

            if (layout.nodes != null && layout.nodes.Length > 0)
            {
                for (int i = 0; i < layout.nodes.Length; i++)
                {
                    PsExportNode node = layout.nodes[i];
                    if (string.IsNullOrEmpty(node.id))
                        node.id = i.ToString();
                    if (node.order < 0)
                        node.order = i;
                    nodes.Add(node);
                }
                return nodes;
            }

            if (layout.layers == null) return nodes;

            for (int i = 0; i < layout.layers.Length; i++)
            {
                PsExportLayer layer = layout.layers[i];
                nodes.Add(new PsExportNode
                {
                    id = i.ToString(),
                    parentId = "",
                    name = layer.name,
                    file = layer.file,
                    x = layer.x,
                    y = layer.y,
                    width = layer.width,
                    height = layer.height,
                    opacity = layer.opacity,
                    visible = layer.visible,
                    isText = layer.isText,
                    text = layer.text,
                    isGroup = false,
                    order = layout.layers.Length - 1 - i
                });
            }

            return nodes;
        }


        #endregion

        #region Asset Preparation

        private string ResolveSourceImagesFolder()
        {
            string jsonDirectory = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(jsonDirectory) && Directory.Exists(jsonDirectory))
                return jsonDirectory;
            throw new DirectoryNotFoundException("Could not resolve images folder from layout JSON path.");
        }

        private Dictionary<string, string> PrepareImageAssets(List<PsExportNode> nodes, string sourceFolder)
        {
            var fileToAssetPath = new Dictionary<string, string>();
            var uniqueFiles = new HashSet<string>();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!string.IsNullOrEmpty(nodes[i].file))
                    uniqueFiles.Add(nodes[i].file);
            }

            if (uniqueFiles.Count == 0) return fileToAssetPath;

            string destinationAssetFolder = ResolveDestinationAssetFolder();
            string destinationAbsoluteFolder = AssetPathToAbsolutePath(destinationAssetFolder);
            Directory.CreateDirectory(destinationAbsoluteFolder);

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string fileName in uniqueFiles)
                {
                    string sourcePath = Path.Combine(sourceFolder, fileName);
                    if (!File.Exists(sourcePath))
                    {
                        Debug.LogWarning($"[PSD2UI] Missing source image: {sourcePath}");
                        continue;
                    }

                    string destinationAbsolutePath = Path.Combine(destinationAbsoluteFolder, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationAbsolutePath) ?? destinationAbsoluteFolder);

                    if (copyImagesIntoProject)
                    {
                        if (!PathsEqual(sourcePath, destinationAbsolutePath))
                            File.Copy(sourcePath, destinationAbsolutePath, true);
                    }
                    else
                    {
                        string sourceAssetPath = AbsolutePathToAssetPath(sourcePath);
                        if (!string.IsNullOrEmpty(sourceAssetPath))
                        {
                            fileToAssetPath[fileName] = sourceAssetPath;
                            continue;
                        }
                        File.Copy(sourcePath, destinationAbsolutePath, true);
                    }

                    fileToAssetPath[fileName] = AbsolutePathToAssetPath(destinationAbsolutePath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return fileToAssetPath;
        }

        private string ResolveDestinationAssetFolder()
        {
            string baseFolder = NormalizeToAssetPath(importSpritesFolder);
            if (string.IsNullOrEmpty(baseFolder))
                baseFolder = "Assets/PSD2UI Imports";

            string layoutFolder = SanitizePathPart(Path.GetFileNameWithoutExtension(jsonPath));
            if (string.IsNullOrEmpty(layoutFolder))
                layoutFolder = "LayoutImport";

            return $"{baseFolder}/{layoutFolder}";
        }

        private Dictionary<string, Sprite> LoadSprites(Dictionary<string, string> fileToAssetPath)
        {
            var spriteCache = new Dictionary<string, Sprite>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in fileToAssetPath)
                {
                    if (string.IsNullOrEmpty(kv.Value)) continue;

                    TextureImporter importer = AssetImporter.GetAtPath(kv.Value) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        importer.SaveAndReimport();
                    }
                    else
                    {
                        AssetDatabase.ImportAsset(kv.Value, ImportAssetOptions.ForceUpdate);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (var kv in fileToAssetPath)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(kv.Value);
                if (sprite != null)
                    spriteCache[kv.Key] = sprite;
                else
                    Debug.LogWarning($"[PSD2UI] Failed to load sprite at '{kv.Value}'.");
            }

            return spriteCache;
        }


        #endregion

        #region Hierarchy Construction

        private RectTransform EnsureCanvas(int width, int height)
        {
            if (targetCanvas != null)
            {
                CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1f;
                }
                return targetCanvas;
            }

            GameObject canvasGo = new GameObject("PS_Layout_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create PS Layout Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(width, height);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo.GetComponent<RectTransform>();
        }

        private RectTransform CreateRoot(RectTransform canvasRt, PsExportDocument document)
        {
            string rootName = string.IsNullOrEmpty(document.name)
                ? Path.GetFileNameWithoutExtension(jsonPath)
                : Path.GetFileNameWithoutExtension(document.name);

            GameObject rootGo = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(rootGo, "Create PS Layout Root");
            RectTransform rootRt = rootGo.AddComponent<RectTransform>();
            rootRt.SetParent(canvasRt, false);
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(document.width, document.height);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.localScale = Vector3.one;
            return rootRt;
        }

        private void BuildHierarchy(List<PsExportNode> nodes, RectTransform rootRt, Dictionary<string, Sprite> spriteCache)
        {
            var runtimeById = new Dictionary<string, RuntimeNode>();
            var childrenByParentId = new Dictionary<string, List<RuntimeNode>>();

            for (int i = 0; i < nodes.Count; i++)
            {
                PsExportNode node = nodes[i];
                GameObject go = new GameObject(SafeGameObjectName(node.name));
                Undo.RegisterCreatedObjectUndo(go, "Create PS Layer");
                RectTransform rt = go.AddComponent<RectTransform>();

                runtimeById[node.id] = new RuntimeNode
                {
                    Data = node,
                    GameObject = go,
                    RectTransform = rt
                };
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                RuntimeNode runtime = runtimeById[nodes[i].id];
                PsExportNode node = runtime.Data;

                string parentId = string.Empty;
                RectTransform parentRt = rootRt;
                float parentX = 0f;
                float parentY = 0f;

                if (!string.IsNullOrEmpty(node.parentId) &&
                    runtimeById.TryGetValue(node.parentId, out RuntimeNode parentRuntime))
                {
                    parentId = parentRuntime.Data.id;
                    parentRt = parentRuntime.RectTransform;
                    parentX = parentRuntime.Data.x;
                    parentY = parentRuntime.Data.y;
                }

                Vector2 pivot = PivotModeToVector(pivotMode);
                RectTransform rt = runtime.RectTransform;
                rt.SetParent(parentRt, false);
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = pivot;
                float localX = node.x - parentX;
                float localY = -(node.y - parentY);
                float w = Mathf.Max(0f, node.width);
                float h = Mathf.Max(0f, node.height);
                float pivotOffsetX = pivot.x * w;
                float pivotOffsetY = -(1f - pivot.y) * h;
                rt.anchoredPosition = new Vector2(localX + pivotOffsetX, localY + pivotOffsetY);
                rt.sizeDelta = new Vector2(w, h);
                rt.localScale = Vector3.one;

                if (!childrenByParentId.TryGetValue(parentId, out List<RuntimeNode> childList))
                {
                    childList = new List<RuntimeNode>();
                    childrenByParentId[parentId] = childList;
                }
                childList.Add(runtime);

                if (!node.isGroup)
                {
                    bool hasFile = !string.IsNullOrEmpty(node.file);
                    Sprite sprite = null;
                    bool hasSprite = hasFile && spriteCache.TryGetValue(node.file, out sprite);
                    bool useText = createTextObjects && node.isText && !string.IsNullOrEmpty(node.text);

                    if (useText)
                    {
                        TextMeshProUGUI tmp = runtime.GameObject.AddComponent<TextMeshProUGUI>();
                        tmp.text = node.text;
                        tmp.raycastTarget = false;
                        tmp.color = Color.black;
                    }
                    else if (hasFile)
                    {
                        Image img = runtime.GameObject.AddComponent<Image>();
                        img.sprite = hasSprite ? sprite : null;
                        img.raycastTarget = false;
                        Color c = img.color;
                        c.a = Mathf.Clamp01(node.opacity);
                        img.color = c;

                        if (!hasSprite)
                            Debug.LogWarning($"[PSD2UI] Sprite missing for layer '{node.name}' file '{node.file}'.");
                    }
                }
                else if (node.opacity < 1f)
                {
                    CanvasGroup group = runtime.GameObject.AddComponent<CanvasGroup>();
                    group.alpha = Mathf.Clamp01(node.opacity);
                }

                if (respectVisibility && !node.visible)
                    runtime.GameObject.SetActive(false);
            }

            ApplySiblingOrder(string.Empty, childrenByParentId);
        }

        private static void ApplySiblingOrder(string parentId, Dictionary<string, List<RuntimeNode>> childrenByParentId)
        {
            if (!childrenByParentId.TryGetValue(parentId, out List<RuntimeNode> children) || children.Count == 0)
                return;

            children.Sort((a, b) =>
            {
                int byOrder = b.Data.order.CompareTo(a.Data.order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Data.id, b.Data.id);
            });

            for (int i = 0; i < children.Count; i++)
                children[i].RectTransform.SetAsLastSibling();

            for (int i = 0; i < children.Count; i++)
                ApplySiblingOrder(children[i].Data.id, childrenByParentId);
        }


        #endregion

        #region Path Utilities

        private static string NormalizeToAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            string normalized = path.Replace("\\", "/");
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return normalized;
            if (normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                return "Assets";
            if (Path.IsPathRooted(normalized))
            {
                if (normalized.StartsWith(Application.dataPath.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase))
                    return "Assets" + normalized.Substring(Application.dataPath.Length).Replace("\\", "/");
                return string.Empty;
            }
            return "Assets/" + normalized.TrimStart('/');
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            string normalized = assetPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid asset path '{assetPath}'.");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, normalized.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private static string AbsolutePathToAssetPath(string absolutePath)
        {
            string full = Path.GetFullPath(absolutePath).Replace("\\", "/");
            string assets = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
            if (!full.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return "Assets" + full.Substring(assets.Length);
        }

        private static bool PathsEqual(string a, string b)
        {
            string pa = Path.GetFullPath(a).TrimEnd('\\', '/');
            string pb = Path.GetFullPath(b).TrimEnd('\\', '/');
            return string.Equals(pa, pb, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Export";
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private static string SafeGameObjectName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Layer";
            return name.Replace("/", "_");
        }

        #endregion
    }

    [Serializable]
    public class PsExportLayout
    {
        public int version = 1;
        public PsExportDocument document;
        public PsExportNode[] nodes;
        public PsExportLayer[] layers;
    }

    [Serializable]
    public class PsExportDocument
    {
        public int width;
        public int height;
        public string name;
    }

    [Serializable]
    public class PsExportNode
    {
        public string id;
        public string parentId;
        public string name;
        public string file;
        public float x;
        public float y;
        public float width;
        public float height;
        public float opacity = 1f;
        public bool visible = true;
        public bool isText;
        public string text;
        public bool isGroup;
        public int order = -1;
    }

    [Serializable]
    public class PsExportLayer
    {
        public string name;
        public string file;
        public float x;
        public float y;
        public float width;
        public float height;
        public float opacity = 1f;
        public bool visible = true;
        public bool isText;
        public string text;
    }
}
