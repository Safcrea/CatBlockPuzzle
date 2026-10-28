#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Saves the authored in-scene levels as individual prefabs and cleans up scene previews.</summary>
    internal static class CatPuzzleLevelPrefabExporter
    {
        public const string LevelPrefabFolder = "Assets/_GameData/System/Resources/CatBlockPuzzle/Levels";

        public static void ExportPreparedLevelsInEditor(CatBlockPuzzleGame game)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            CatPuzzleLevelView[] preparedLevels = game.PreparedLevels;
            CatPuzzleContentCatalog contentCatalog = game.Catalog;
            if (preparedLevels == null || preparedLevels.Length != contentCatalog.LevelCount)
                throw new InvalidOperationException("Expected the complete authored level hierarchy before exporting.");
            RectTransform levelRoot = game.Hud.levelRoot;
            // Validate all originals before writing or removing any scene object.
            for (int i = 0; i < preparedLevels.Length; i++)
            {
                if (!PreparedLevelLoader.ValidateLevelPrefab(contentCatalog, preparedLevels[i], i, out string error)) throw new InvalidOperationException(error);
                if (preparedLevels[i].transform.parent != levelRoot)
                    throw new InvalidOperationException("Level is outside the expected LevelRoot.");
            }
            Directory.CreateDirectory(LevelPrefabFolder);
            AssetDatabase.Refresh();
            var addresses = new string[preparedLevels.Length];
            for (int i = 0; i < preparedLevels.Length; i++)
            {
                var view = preparedLevels[i];
                bool wasActive = view.gameObject.activeSelf;
                try
                {
                    view.gameObject.SetActive(false);
                    string path = LevelPrefabFolder + "/Level_" + (i + 1).ToString("000") + ".prefab";
                    if (GameUiTheme.Active != null) GameUiTheme.Active.ApplyLevel(view);
                    var saved = PrefabUtility.SaveAsPrefabAsset(view.gameObject, path, out bool success);
                    if (!success || saved == null) throw new IOException("Could not export " + path);
                    if (!PreparedLevelLoader.ValidateLevelPrefab(contentCatalog, saved.GetComponent<CatPuzzleLevelView>(), i, out string error))
                        throw new InvalidOperationException(path + ": " + error);
                    addresses[i] = "CatBlockPuzzle/Levels/Level_" + (i + 1).ToString("000");
                }
                finally { view.gameObject.SetActive(wasActive); }
            }
            // All prefabs are saved and validated. The caller retains a complete scene backup.
            Undo.RecordObject(game, "Use level prefabs");
            Undo.RecordObject(contentCatalog, "Assign level prefab addresses");
            contentCatalog.levelPrefabResources = addresses;
            foreach (var view in preparedLevels) Undo.DestroyObjectImmediate(view.gameObject);
            game.PreparedLevels = Array.Empty<CatPuzzleLevelView>();
            game.EnsureComposed();
            game.Loader.Release();
            contentCatalog.sourceFingerprint = CatPuzzleScenePreparer.PreparationFingerprint();
            EditorUtility.SetDirty(contentCatalog);
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
        }

        public static void ClearLevelPreviewInEditor(CatBlockPuzzleGame game)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            game.EnsureComposed();
            game.Loader.Release();
            // Domain reload can discard the managed preview handle; remove only our unsaved previews.
            RectTransform levelRoot = game.Hud.levelRoot;
            for (int i = levelRoot.childCount - 1; i >= 0; i--)
            {
                var child = levelRoot.GetChild(i).gameObject;
                if ((child.hideFlags & HideFlags.DontSaveInEditor) != 0)
                    UnityEngine.Object.DestroyImmediate(child);
            }
        }
    }
}
#endif
