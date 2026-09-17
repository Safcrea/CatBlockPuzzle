using System;
using System.IO;
using System.Linq;
using CatBlockPuzzle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CatPuzzleScenePreparation
{
    public const string ScenePath = "Assets/_GameData/System/Scenes/GameScene.unity";

    [MenuItem("Cat Block Puzzle/Prepare Game Scene")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before preparing the scene.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open " + ScenePath + " before preparing.");
        var existing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CatBlockPuzzleGame>(true)).SingleOrDefault();
        if (existing != null && !Application.isBatchMode &&
            !EditorUtility.DisplayDialog("Reprepare game scene?", "This explicitly regenerates the prepared GameScene hierarchy. A scene backup will be saved first. Edits outside that hierarchy are preserved.", "Reprepare", "Cancel")) return;
        Directory.CreateDirectory("SceneBackups");
        string backup = "SceneBackups/CatBlockPuzzle_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up the scene.");
        var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
        EnsureUiSortingLayer();
        var newRoot = new GameObject("GameScene");
        try
        {
            var logic = new GameObject("Game Logic"); logic.transform.SetParent(newRoot.transform,false);
            var controllers = new GameObject("Controllers"); controllers.transform.SetParent(newRoot.transform,false);
            var ui = new GameObject("UI"); ui.transform.SetParent(newRoot.transform,false);
            var host = new GameObject("GameSystems"); host.transform.SetParent(logic.transform,false);
            var game = host.AddComponent<CatBlockPuzzleGame>();
            game.PrepareInEditor(camera, ui.transform, controllers.transform);
            Undo.RegisterCreatedObjectUndo(newRoot,"Prepare CatBlockPuzzle Scene");
            if (existing != null) Undo.DestroyObjectImmediate(existing.transform.root.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = host;
            Debug.Log("CAT_SCENE_PREPARED levelPrefabs=" + game.LevelPrefabResources.Length + " sceneObjects=" + newRoot.GetComponentsInChildren<Transform>(true).Length + " backup=" + backup);
        }
        catch
        {
            UnityEngine.Object.DestroyImmediate(newRoot);
            throw;
        }
    }

    // Used by automated editor runs after a UI-authoring change. Opening the scene
    // explicitly keeps this independent of whichever scene was last active locally.
    public static void PrepareFromBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before preparing.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Prepare();
    }

    [MenuItem("Cat Block Puzzle/Validate Game Scene")]
    public static void Validate()
    {
        var scene = SceneManager.GetActiveScene();
        ValidateScene(scene);
        Debug.Log("CAT_SCENE_VALIDATED " + scene.path);
    }

    [MenuItem("Cat Block Puzzle/Convert Scene Levels to Prefabs")]
    public static void ConvertLevelsToPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open " + ScenePath + " first.");
        var game = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CatBlockPuzzleGame>(true)).Single();
        Directory.CreateDirectory("SceneBackups");
        string backup = "SceneBackups/BeforeLevelPrefabs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up the scene.");
        game.ExportPreparedLevelsInEditor();
        ValidateScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the converted scene.");
        Debug.Log("CAT_LEVEL_PREFABS_CONVERTED count=" + game.LevelPrefabResources.Length + " backup=" + backup);
    }

    public static void ValidateScene(Scene scene)
    {
        var games = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CatBlockPuzzleGame>(true)).ToArray();
        if (games.Length != 1) throw new BuildFailedException("Expected exactly one prepared CatBlockPuzzle controller.");
        if (!games[0].ValidatePreparedScene(out string error)) throw new BuildFailedException(error);
        // A script import hash changes for unrelated UI edits (and across Unity versions).
        // Validate the data used by this scene instead of demanding a destructive scene rebuild.
        if (!games[0].ValidatePreparedContentSources(out string sourceError))
            throw new BuildFailedException(sourceError);
        for (int i = 0; i < games[0].LevelPrefabResources.Length; i++)
        {
            string path = "Assets/_GameData/System/Resources/" + games[0].LevelPrefabResources[i] + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<CatPuzzleLevelView>(path);
            if (!games[0].ValidateLevelPrefab(prefab, i, out string prefabError)) throw new BuildFailedException(path + ": " + prefabError);
        }
        foreach (var level in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CatPuzzleLevelView>(true)))
            if ((level.gameObject.hideFlags & HideFlags.DontSaveInEditor) == 0)
                throw new BuildFailedException("Level instances must not be saved inside the UI scene.");
        foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new BuildFailedException("Missing script on " + transform.name);
    }

    private static void EnsureUiSortingLayer()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = settings.FindProperty("m_SortingLayers");
        for (int i=0; i<layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "UI") return;
        layers.InsertArrayElementAtIndex(layers.arraySize);
        var entry = layers.GetArrayElementAtIndex(layers.arraySize-1);
        entry.FindPropertyRelative("name").stringValue = "UI";
        entry.FindPropertyRelative("uniqueID").intValue = 172309711;
        entry.FindPropertyRelative("locked").boolValue = false;
        settings.ApplyModifiedProperties();
    }
}

public sealed class CatPuzzlePreparedSceneBuildCheck : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // Unity also invokes scene processors during Play Mode; build validation only.
        if (report != null && scene.path == CatPuzzleScenePreparation.ScenePath)
            CatPuzzleScenePreparation.ValidateScene(scene);
    }
}
