using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AVNAnalytics.EditorTools
{
    /// <summary>
    /// Before every player build, writes the Android versionCode / iOS build number into each
    /// AVNGameAnalyticsConfig asset, so the SDK can report it at runtime (Player Settings are
    /// editor-only). The field is hidden in the Inspector; nobody edits it by hand.
    /// </summary>
    sealed class AVNGameAnalyticsBuildStamp : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string number = AVNGameAnalyticsConfig.EditorBuildNumber(report.summary.platform);
            bool changed = false;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(AVNGameAnalyticsConfig)))
            {
                var config = AssetDatabase.LoadAssetAtPath<AVNGameAnalyticsConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null || config.BuildNumber == number) continue;
                config.BuildNumber = number;
                EditorUtility.SetDirty(config);
                changed = true;
            }
            if (changed) AssetDatabase.SaveAssets();
            Debug.Log("[AVN Analytics] Build number for analytics: " + (number.Length > 0 ? number : "(none for this platform, build GUID is used)"));
        }
    }
}
