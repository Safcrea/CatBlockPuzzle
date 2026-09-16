using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CatBlockPuzzle.Tests
{
    public abstract class PreparedScenePlayModeFixture
    {
        protected MonoBehaviour Game;
        protected virtual string SceneName => "CatBlockPuzzle";
        private readonly Dictionary<string,int?> ints = new Dictionary<string,int?>();
        private string metaSave;
        private bool hadMetaSave;
        private string rewardSave;
        private bool hadRewardSave;
#if UNITY_EDITOR
        private bool hadPreview;
        private int oldPreview;
#endif
        [Serializable] private class Pack { public Level[] levels; }
        [Serializable] private class Level { public string id; }

        [UnitySetUp]
        public IEnumerator LoadPreparedScene()
        {
            ints.Clear();
            string[] keys = {"CatBlockPuzzle.LevelIndex", "CatBlockPuzzle.Coins", "CatBlockPuzzle.Settings.Sound", "CatBlockPuzzle.Settings.Sfx", "CatBlockPuzzle.Settings.Music", "CatBlockPuzzle.Settings.Haptics", "CatBlockPuzzle.Settings.ReducedMotion"};
            foreach (var key in keys) ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            foreach (var key in new[] { "CatBlockPuzzle.Tutorial.Hint", "CatBlockPuzzle.Tutorial.Freeze" })
                ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            hadRewardSave = PlayerPrefs.HasKey("CatBlockPuzzle.DailyReward.State.v1");
            rewardSave = PlayerPrefs.GetString("CatBlockPuzzle.DailyReward.State.v1", "");
            var pack = JsonUtility.FromJson<Pack>(Resources.Load<TextAsset>("CatBlockPuzzle/levels_100").text);
            foreach (var level in pack.levels) { string key = "CatBlockPuzzle.BestStars." + level.id; ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null; }
            hadMetaSave = PlayerPrefs.HasKey("CatBlockPuzzle.Meta.Progress");
            metaSave = PlayerPrefs.GetString("CatBlockPuzzle.Meta.Progress", "");
            // Tests run against a deterministic new-player state, then restore the user's exact save.
            foreach (var key in ints.Keys) PlayerPrefs.DeleteKey(key);
            // Existing interaction tests run after onboarding; tutorial tests reset these explicitly.
            PlayerPrefs.SetInt("CatBlockPuzzle.Tutorial.Hint", 1);
            PlayerPrefs.SetInt("CatBlockPuzzle.Tutorial.Freeze", 1);
            PlayerPrefs.SetString("CatBlockPuzzle.DailyReward.State.v1", "{\"version\":1,\"hintCount\":10,\"freezeCount\":10}");
            PlayerPrefs.DeleteKey("CatBlockPuzzle.Meta.Progress");
#if UNITY_EDITOR
            hadPreview = UnityEditor.EditorPrefs.HasKey("CatBlockPuzzle.EditorPreviewLevel");
            oldPreview = UnityEditor.EditorPrefs.GetInt("CatBlockPuzzle.EditorPreviewLevel",0);
            UnityEditor.EditorPrefs.SetInt("CatBlockPuzzle.EditorPreviewLevel",0);
#endif
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Game = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Single(g => g.GetType().FullName == "CatBlockPuzzle.CatBlockPuzzleGame");
            Assert.That(Game.enabled, Is.True, "Prepared scene validation rejected the game.");
            yield return null;
            Call("PreviewLevelForTesting",0);
            yield return new WaitForSecondsRealtime(.85f);
        }

        [UnityTearDown]
        public IEnumerator RestorePlayerPreferences()
        {
            // Stop the game before restoring its save keys, including failure cases.
            var scene = SceneManager.GetSceneByName(SceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                var empty = SceneManager.CreateScene("Test Cleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            foreach (var pair in ints)
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key,pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
            if (hadMetaSave) PlayerPrefs.SetString("CatBlockPuzzle.Meta.Progress",metaSave); else PlayerPrefs.DeleteKey("CatBlockPuzzle.Meta.Progress");
            if (hadRewardSave) PlayerPrefs.SetString("CatBlockPuzzle.DailyReward.State.v1", rewardSave);
            else PlayerPrefs.DeleteKey("CatBlockPuzzle.DailyReward.State.v1");
            PlayerPrefs.Save();
#if UNITY_EDITOR
            if (hadPreview) UnityEditor.EditorPrefs.SetInt("CatBlockPuzzle.EditorPreviewLevel",oldPreview);
            else UnityEditor.EditorPrefs.DeleteKey("CatBlockPuzzle.EditorPreviewLevel");
#endif
        }

        protected object Field(string name) => Game.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Game);
        protected object Call(string name, params object[] arguments)
        {
            var method = Game.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                .Single(m => m.Name == name && m.GetParameters().Length == arguments.Length);
            return method.Invoke(Game,arguments);
        }

        protected static void CaptureAssignedCamera(UnityEngine.Canvas canvas, string fileName, int width=540, int height=960)
        {
            var camera = canvas.worldCamera;
            Assert.That(camera,Is.Not.Null);
            var oldTarget = camera.targetTexture;
            float oldAspect = camera.aspect;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture = target; camera.aspect = (float)width/height;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath,"../TestArtifacts"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder,fileName),pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.aspect = oldAspect; RenderTexture.active = oldActive;
                UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
