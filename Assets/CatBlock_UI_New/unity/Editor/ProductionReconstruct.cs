using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CatBlocksUI.Editor {
    public static class ProductionReconstruct {
        [MenuItem("Cat Blocks/UI/Reconstruct From JSON")]
        public static void Reconstruct() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Cat Blocks", "Leave Play Mode before reconstruction.", "OK");
                return;
            }
            string file = EditorUtility.OpenFilePanel("Choose production layout.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(file)) return;
            try {
                string absolute = Path.GetFullPath(file).Replace('\\', '/');
                string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
                if (!absolute.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Copy the complete CatBlock_UI_New folder under this project's Assets folder first.");
                string path = "Assets" + absolute.Substring(assets.Length);
                string rootAbsolute = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(absolute))).Replace('\\', '/');
                string uiRoot = "Assets" + rootAbsolute.Substring(assets.Length);
                var document = ScreenImporter.Read(path);
                var report = ScreenImporter.Inspect(path, uiRoot);
                if (!report.IsValid) throw new InvalidDataException(string.Join("\n", report.errors));
                // Build first so a font/import failure cannot delete the previous screen.
                var previous = UnityEngine.Object.FindObjectsByType<ScreenLayout>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(s => s.gameObject.scene == UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                        && s.sourceLayout && AssetDatabase.GetAssetPath(s.sourceLayout) == path).ToArray();
                var built = ScreenImporter.Build(path, false, null, true, uiRoot);
                foreach (var screen in previous) Undo.DestroyObjectImmediate(screen.gameObject);
                var canvas = built.GetComponent<Canvas>();
                canvas.sortingOrder = document.modal ? 100 : 0;
                built.SetActive(!document.modal);
                Selection.activeGameObject = built;
                EditorGUIUtility.PingObject(built);
            } catch (Exception error) {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("Cat Blocks reconstruction", error.Message, "OK");
            }
        }
    }
}
