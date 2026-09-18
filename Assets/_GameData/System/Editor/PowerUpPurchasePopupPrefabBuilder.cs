using System.IO;
using CatBlockPuzzle;
using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzleEditor
{
    /// <summary>Regenerates the power up purchase popup prefab from the code-built layout.</summary>
    internal static class PowerUpPurchasePopupPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/" + PowerUpPurchasePopup.PrefabResourcePath + ".prefab";

        [MenuItem("Cat Block Puzzle/Create Power Up Purchase Popup Prefab")]
        internal static void CreatePrefab()
        {
            PowerUpPurchasePopup popup = PowerUpPurchasePopup.Build(null);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                popup.gameObject.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(popup.gameObject, PrefabPath, out bool saved);
                if (!saved)
                {
                    Debug.LogError($"Could not save {PrefabPath}.");
                    return;
                }
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"Saved power up purchase popup prefab to {PrefabPath}.", asset);
        }
    }
}
