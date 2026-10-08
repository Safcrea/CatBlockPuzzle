using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AVNAnalytics.EditorTools
{
    /// <summary>
    /// Config Inspector: the settings most projects touch are always shown; everything else sits behind
    /// "Show advanced settings". The detected build info is shown read-only at the bottom.
    /// </summary>
    [CustomEditor(typeof(AVNGameAnalyticsConfig))]
    sealed class AVNGameAnalyticsConfigEditor : UnityEditor.Editor
    {
        static readonly string[] BasicFields =
        {
            "Enabled", "Endpoint", "ApiKey", "Environment", "SendInEditor",
            "BatchSize", "FlushIntervalSeconds", "SessionTimeoutSeconds",
        };

        static readonly HashSet<string> NeverDrawn = new HashSet<string> { "m_Script", "buildNumber", "showAdvancedSettings" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLocationWarning();

            foreach (string name in BasicFields) EditorGUILayout.PropertyField(serializedObject.FindProperty(name));

            if (!serializedObject.FindProperty("Enabled").boolValue)
                EditorGUILayout.HelpBox("Analytics is off: Initialize() does nothing and no events are logged or sent.", MessageType.Warning);

            EditorGUILayout.Space();
            SerializedProperty show = serializedObject.FindProperty("showAdvancedSettings");
            show.boolValue = EditorGUILayout.ToggleLeft("Show advanced settings", show.boolValue);
            if (show.boolValue)
            {
                var basic = new HashSet<string>(BasicFields);
                SerializedProperty it = serializedObject.GetIterator();
                for (bool enter = true; it.NextVisible(enter); enter = false)
                {
                    if (basic.Contains(it.name) || NeverDrawn.Contains(it.name)) continue;
                    EditorGUILayout.PropertyField(it, true);
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Detected automatically", EditorStyles.boldLabel);
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string build = AVNGameAnalyticsConfig.EditorBuildNumber(target);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("App Version", PlayerSettings.bundleVersion);
                EditorGUILayout.TextField("Build", build.Length > 0 ? build : "build GUID (no build number on " + target + ")");
                EditorGUILayout.TextField("Platform", "editor in Play mode, " + target + " in builds");
            }
        }

        /// <summary>Initialize() loads Resources/AVNGameAnalyticsConfig, so a config anywhere else is never found.</summary>
        void DrawLocationWarning()
        {
            string path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(path)) return;
            bool inResources = path.Contains("/Resources/");
            bool rightName = System.IO.Path.GetFileNameWithoutExtension(path) == AVNGameAnalytics.ConfigResourceName;
            if (!inResources || !rightName)
                EditorGUILayout.HelpBox(
                    "AVNGameAnalytics.Initialize() only loads a config named '" + AVNGameAnalytics.ConfigResourceName +
                    "' inside a Resources folder. This one is not, so it will be ignored. Use Tools > AVN Analytics > Select Config.",
                    MessageType.Error);
        }
    }
}
