using System;
using System.IO;
using System.Reflection;
using CatBlockPuzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[InitializeOnLoad]
public static class CatBlockPuzzleGameSceneAuthoring
{
    private const string ScenePath = "Assets/Scenes/CatBlockPuzzle.unity";
    private const string GeneratedAssetFolder = "Assets/Resources/CatBlockPuzzle/GeneratedSceneAssets";
    private const string AutoBuildSessionKey = "CatBlockPuzzle.AuthoredGameScene.AutoBuild.v1";

    private static readonly string[] GeneratedSpriteFields =
    {
        "whiteSprite", "roundedBoxSprite", "circleSprite", "coinSprite", "catHeadSprite",
        "mouthSprite", "tailSprite", "pawSprite", "starSprite", "starOutlineSprite",
        "backIconSprite", "pauseIconSprite", "settingsIconSprite", "hintIconSprite",
        "resetIconSprite", "closeIconSprite"
    };

    private static readonly string[] GeneratedAudioFields =
    {
        "buttonClip", "snapClip", "wrongClip", "winClip"
    };

    static CatBlockPuzzleGameSceneAuthoring()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Tools/Cat Block Puzzle/Build Authored GameScene")]
    public static void BuildFromMenu()
    {
        BuildScene(true);
    }

    private static void TryAutoBuild()
    {
        if (SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryAutoBuild;
            SessionState.SetBool(AutoBuildSessionKey, false);
            return;
        }

        BuildScene(false);
    }

    private static void BuildScene(bool force)
    {
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForBuild = !scene.IsValid() || !scene.isLoaded;
        if (openedForBuild)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            CatBlockPuzzleGame existingGame = FindInScene<CatBlockPuzzleGame>(scene);
            if (!force && existingGame != null && FindRoot(scene, "GameUI") != null)
            {
                return;
            }

            if (scene.isDirty && !openedForBuild && !force)
            {
                Debug.LogWarning("Skipped automatic GameScene authoring because CatBlockPuzzle.unity has unsaved edits. Use Tools > Cat Block Puzzle > Build Authored GameScene when ready.");
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "Main Camera")
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            GameObject gameSystem = new GameObject("GameSystem");
            SceneManager.MoveGameObjectToScene(gameSystem, scene);
            CatBlockPuzzleGame game = gameSystem.AddComponent<CatBlockPuzzleGame>();

            GameObject killZone = new GameObject("Kill Zone");
            SceneManager.MoveGameObjectToScene(killZone, scene);

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

            SceneManager.SetActiveScene(scene);
            game.EditorBuildAuthoredScene(eventSystemObject.GetComponent<EventSystem>(), killZone);
            PersistConfigurationAssets(game);
            PersistGeneratedAssets(game);

            GameObject mainCamera = FindRoot(scene, "Main Camera");
            GameObject gameUi = FindRoot(scene, "GameUI");
            if (mainCamera != null) mainCamera.transform.SetSiblingIndex(0);
            gameSystem.transform.SetSiblingIndex(1);
            if (gameUi != null) gameUi.transform.SetSiblingIndex(2);
            killZone.transform.SetSiblingIndex(3);
            eventSystemObject.transform.SetSiblingIndex(4);

            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Authored CatBlockPuzzle GameScene saved with GameSystem, assigned GameUI screens, Kill Zone, and EventSystem.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (force)
            {
                throw;
            }
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            if (openedForBuild && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void PersistGeneratedAssets(CatBlockPuzzleGame game)
    {
        EnsureFolder(GeneratedAssetFolder);

        foreach (string fieldName in GeneratedSpriteFields)
        {
            FieldInfo field = GetField(fieldName);
            Sprite sprite = field?.GetValue(game) as Sprite;
            if (sprite == null || sprite.texture == null)
            {
                continue;
            }

            string path = GeneratedAssetFolder + "/" + fieldName + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            sprite.texture.name = fieldName + " Texture";
            sprite.name = fieldName;
            AssetDatabase.CreateAsset(sprite.texture, path);
            AssetDatabase.AddObjectToAsset(sprite, path);
            EditorUtility.SetDirty(sprite.texture);
            EditorUtility.SetDirty(sprite);
        }

        foreach (string fieldName in GeneratedAudioFields)
        {
            FieldInfo field = GetField(fieldName);
            AudioClip clip = field?.GetValue(game) as AudioClip;
            if (clip == null)
            {
                continue;
            }

            string path = GeneratedAssetFolder + "/" + fieldName + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            clip.name = fieldName;
            AssetDatabase.CreateAsset(clip, path);
            EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
    }

    private static void PersistConfigurationAssets(CatBlockPuzzleGame game)
    {
        PersistConfigurationAsset(game, "visualCatalog", "Assets/Resources/CatBlockPuzzle/cat_visual_catalog.asset");
        PersistConfigurationAsset(game, "layoutProfile", "Assets/Resources/CatBlockPuzzle/portrait_layout_profile.asset");
        AssetDatabase.SaveAssets();
    }

    private static void PersistConfigurationAsset(CatBlockPuzzleGame game, string fieldName, string path)
    {
        FieldInfo field = GetField(fieldName);
        ScriptableObject value = field?.GetValue(game) as ScriptableObject;
        if (value == null)
        {
            throw new InvalidOperationException("Missing authored configuration: " + fieldName);
        }

        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing == value)
        {
            return;
        }

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        value.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(value, path);
        EditorUtility.SetDirty(value);
    }

    private static FieldInfo GetField(string fieldName)
    {
        return typeof(CatBlockPuzzleGame).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
