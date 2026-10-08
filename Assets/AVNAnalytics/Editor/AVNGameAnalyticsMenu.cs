using System.IO;
using UnityEditor;
using UnityEngine;

namespace AVNAnalytics.EditorTools
{
    /// <summary>One-click setup: Tools > AVN Analytics > Select Config creates the config in the right place if it is missing.</summary>
    static class AVNGameAnalyticsMenu
    {
        const string Folder = "Assets/AVNAnalytics/Resources";

        [MenuItem("Tools/AVN Analytics/Select Config")]
        static void SelectConfig()
        {
            var config = Resources.Load<AVNGameAnalyticsConfig>(AVNGameAnalytics.ConfigResourceName);
            if (config == null)
            {
                Directory.CreateDirectory(Folder);
                string path = Folder + "/" + AVNGameAnalytics.ConfigResourceName + ".asset";
                config = ScriptableObject.CreateInstance<AVNGameAnalyticsConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                Debug.Log("[AVN Analytics] Created a config at " + path + ". Fill in Endpoint and Api Key.");
            }
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
