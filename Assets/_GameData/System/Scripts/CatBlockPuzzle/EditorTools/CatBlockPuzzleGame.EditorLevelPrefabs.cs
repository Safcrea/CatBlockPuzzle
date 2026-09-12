#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        public const string LevelPrefabFolder = "Assets/_GameData/System/Resources/CatBlockPuzzle/Levels";

        public void ExportPreparedLevelsInEditor()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (preparedLevels == null || preparedLevels.Length != contentCatalog.LevelCount)
                throw new InvalidOperationException("Expected the complete authored level hierarchy before exporting.");
            // Validate all originals before writing or removing any scene object.
            for (int i = 0; i < preparedLevels.Length; i++)
            {
                if (!ValidateLevelPrefab(preparedLevels[i], i, out string error)) throw new InvalidOperationException(error);
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
                    var saved = PrefabUtility.SaveAsPrefabAsset(view.gameObject, path, out bool success);
                    if (!success || saved == null) throw new IOException("Could not export " + path);
                    if (!ValidateLevelPrefab(saved.GetComponent<CatPuzzleLevelView>(), i, out string error))
                        throw new InvalidOperationException(path + ": " + error);
                    addresses[i] = "CatBlockPuzzle/Levels/Level_" + (i + 1).ToString("000");
                }
                finally { view.gameObject.SetActive(wasActive); }
            }
            // All prefabs are saved and validated. The caller retains a complete scene backup.
            Undo.RecordObject(this, "Use level prefabs");
            Undo.RecordObject(contentCatalog, "Assign level prefab addresses");
            contentCatalog.levelPrefabResources = addresses;
            foreach (var view in preparedLevels) Undo.DestroyObjectImmediate(view.gameObject);
            preparedLevels = Array.Empty<CatPuzzleLevelView>();
            ReleaseLoadedLevel();
            contentCatalog.sourceFingerprint = PreparationFingerprint();
            EditorUtility.SetDirty(contentCatalog);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void ClearLevelPreviewInEditor()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            ReleaseLoadedLevel();
            // Domain reload can discard the managed preview handle; remove only our unsaved previews.
            for (int i = levelRoot.childCount - 1; i >= 0; i--)
            {
                var child = levelRoot.GetChild(i).gameObject;
                if ((child.hideFlags & HideFlags.DontSaveInEditor) != 0)
                    DestroyImmediate(child);
            }
        }
    }
}
#endif
