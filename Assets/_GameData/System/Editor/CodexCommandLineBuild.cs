#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CodexCommandLineBuild
{
    public static void BuildAndroidApk()
    {
        string outputPath = GetArgumentValue("-buildOutput", "Builds/Android/CatBlockPuzzle.apk");
        if (!Path.IsPathRooted(outputPath))
        {
            outputPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), outputPath));
        }

        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("Cannot build: no enabled scenes in EditorBuildSettings.");
        }

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            throw new InvalidOperationException("Cannot build: failed to switch active build target to Android.");
        }

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.development = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"CODEX_BUILD_SUMMARY result={summary.result} output={summary.outputPath} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Build failed: {summary.result}; errors={summary.totalErrors}; warnings={summary.totalWarnings}");
        }
    }

    private static string GetArgumentValue(string name, string defaultValue)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return defaultValue;
    }
}
#endif
