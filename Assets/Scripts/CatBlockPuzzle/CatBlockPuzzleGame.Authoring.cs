#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        [MenuItem("Cat Block Puzzle/Authoring/Create Scene UI")]
        public static void CreateSceneUi()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Author UI in Edit Mode.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/CatBlockPuzzle.unity")
                throw new InvalidOperationException("Open CatBlockPuzzle.unity first.");
            if (FindFirstObjectByType<CatBlockPuzzleGame>(FindObjectsInactive.Include) != null)
                throw new InvalidOperationException("The scene already has a controller; authoring will not replace it.");
            var host = new GameObject("Cat Block Puzzle");
            Undo.RegisterCreatedObjectUndo(host, "Author scene UI");
            var game = Undo.AddComponent<CatBlockPuzzleGame>(host);
            game.LoadBakedUiAssets();
            game.layoutProfile = Resources.Load<PortraitLayoutProfile>("CatBlockPuzzle/portrait_layout_profile");
            game.defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            game.BuildAudio();
            bool hadEvents = FindFirstObjectByType<EventSystem>() != null;
            game.EnsureEventSystem();
            if (!hadEvents) Undo.RegisterCreatedObjectUndo(FindFirstObjectByType<EventSystem>().gameObject, "Author EventSystem");
            game.BuildCanvas();
            Undo.RegisterCreatedObjectUndo(game.canvas.gameObject, "Author scene canvas");
            game.canvas.sortingOrder = 10;
            game.canvas.gameObject.layer = LayerMask.NameToLayer("UI");
            foreach (Transform child in game.canvas.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("UI");
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public void PreviewAuthoredLevel(int index)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Use runtime preview during Play Mode.");
            LoadBakedUiAssets();
            levelManager = new LevelManager("CatBlockPuzzle/levels_100");
            levelManager.Load();
            activeLevel = levelManager.GetLevel(index);
            levelIndex = index;
            pieces.Clear();
            ResetAuthoredViews();
            ResetFxPool();
            Canvas.ForceUpdateCanvases();
            ApplyLevelTheme(index);
            levelText.text = "Level " + (index + 1);
            objectiveText.text = BuildThemedObjectiveTitle(activeLevel.Title);
            BuildBoard();
            BuildPieces();
            foreach (var cell in boardRevealCells) cell.Rect.localScale = Vector3.one;
            timerText.text = "2:00";
            coinText.text = "0";
            comboBadge.gameObject.SetActive(false);
            winOverlay.gameObject.SetActive(false);
            failOverlay.gameObject.SetActive(false);
            settingsOverlay.gameObject.SetActive(false);
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        public void AuthorCanvasOrdering()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Author canvas ordering in Edit Mode.");
            AddSortingCanvas(pieceLayer, 20, true);
            AddSortingCanvas(fxLayer, 30, false);
            AuthorModal(winOverlay, winPanel, 100);
            AuthorModal(failOverlay, failPanel, 100);
            AuthorModal(settingsOverlay, settingsPanel, 110);
            foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
            {
                Undo.RecordObject(text, "Disable decorative text raycasts");
                text.raycastTarget = false;
            }
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private static void AddSortingCanvas(RectTransform layer, int order, bool interactive)
        {
            Canvas nested = layer.GetComponent<Canvas>();
            if (nested == null) nested = Undo.AddComponent<Canvas>(layer.gameObject);
            Undo.RecordObject(nested, "Set UI sorting");
            nested.overrideSorting = true;
            nested.sortingOrder = order;
            // Canvas may ignore the property setter while its GameObject is inactive.
            var serializedCanvas = new SerializedObject(nested);
            serializedCanvas.FindProperty("m_OverrideSorting").boolValue = true;
            serializedCanvas.ApplyModifiedProperties();
            if (interactive && layer.GetComponent<GraphicRaycaster>() == null)
                Undo.AddComponent<GraphicRaycaster>(layer.gameObject);
        }

        private void AuthorModal(RectTransform overlay, RectTransform panel, int order)
        {
            Undo.SetTransformParent(overlay, canvas.transform, "Full-screen modal backdrop");
            Undo.RecordObject(overlay, "Stretch modal backdrop");
            Stretch(overlay);
            AddSortingCanvas(overlay, order, true);
            if (overlay.Find("Dialog Safe Area") != null) return;
            var safe = new GameObject("Dialog Safe Area", typeof(RectTransform), typeof(KawaiiUI.SafeAreaFitter));
            Undo.RegisterCreatedObjectUndo(safe, "Author modal safe area");
            safe.transform.SetParent(overlay, false);
            Stretch((RectTransform)safe.transform);
            safe.GetComponent<KawaiiUI.SafeAreaFitter>().Apply();
            var fit = new GameObject("Dialog Fit", typeof(RectTransform), typeof(KawaiiUI.ModalPanelFitter));
            Undo.RegisterCreatedObjectUndo(fit, "Author modal fitting");
            fit.transform.SetParent(safe.transform, false);
            Stretch((RectTransform)fit.transform);
            Undo.SetTransformParent(panel, fit.transform, "Fit dialog to safe area");
            panel.anchoredPosition = Vector2.zero;
            fit.GetComponent<KawaiiUI.ModalPanelFitter>().SetPanel(panel);
        }
    }
}
#endif
