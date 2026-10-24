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
            foreach (var key in new[] { "CatBlockPuzzle.Tutorial.Hint", "CatBlockPuzzle.Tutorial.Freeze", "CatBlockPuzzle.Tutorial.Rotate", "CatBlockPuzzle.Tutorial.LevelOne.v1",
                         "CatBlockPuzzle.Mechanic.Padlocks.v1", "CatBlockPuzzle.Mechanic.KeyCats.v1", "CatBlockPuzzle.Mechanic.Shelf.v1", "CatBlockPuzzle.Mechanic.ColourZones.v1", "CatBlockPuzzle.Mechanic.SpareCats.v1", "CatBlockPuzzle.Mechanic.Hearts.v1",
                         "CatBlockPuzzle.Mechanic.FixedCat.v1", "CatBlockPuzzle.Mechanic.Fog.v1", "CatBlockPuzzle.Mechanic.Moves.v1", "CatBlockPuzzle.DailyPuzzle.LastWonDay" })
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
            PlayerPrefs.SetInt("CatBlockPuzzle.Tutorial.Rotate", 1);
            PlayerPrefs.SetInt("CatBlockPuzzle.Tutorial.LevelOne.v1", 1);
            foreach (var mechanic in new[] { "Padlocks", "KeyCats", "Shelf", "ColourZones", "SpareCats", "Hearts", "FixedCat", "Fog", "Moves" }) PlayerPrefs.SetInt("CatBlockPuzzle.Mechanic." + mechanic + ".v1", 1);
            PlayerPrefs.SetString("CatBlockPuzzle.DailyReward.State.v1", "{\"version\":1,\"hintCount\":10,\"freezeCount\":10,\"rotateCount\":10}");
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

        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// The game is a composition root over single-purpose services (PuzzleSession, LevelTimer,
        /// LevelOneTutorial, ...). Tests address state and behaviour by member name, so lookups walk
        /// that graph: the session first, then the plain C# services, then the root itself, then the
        /// scene views it references (HUD, meta view, result screens), then one more level down
        /// (e.g. the meta controller's progress store).
        /// </summary>
        private IEnumerable<object> Targets()
        {
            var firstLevel = new List<object>();
            foreach (var field in Game.GetType().GetFields(Members))
            {
                if (!IsProjectType(field.FieldType)) continue;
                object value = field.GetValue(Game);
                if (value != null && !(value is UnityEngine.Object o && o == null)) firstLevel.Add(value);
            }
            var session = firstLevel.FirstOrDefault(o => o.GetType().Name == "PuzzleSession");
            if (session != null) yield return session;
            foreach (var service in firstLevel) if (service != session && !(service is UnityEngine.Object)) yield return service;
            yield return Game;
            foreach (var component in firstLevel) if (component is UnityEngine.Object) yield return component;
            foreach (var service in firstLevel)
                foreach (var field in service.GetType().GetFields(Members))
                {
                    if (!IsProjectType(field.FieldType)) continue;
                    object value = field.GetValue(service);
                    if (value != null && !(value is UnityEngine.Object o && o == null)) yield return value;
                }
        }

        private static bool IsProjectType(Type type) =>
            type.Namespace != null && type.Namespace.StartsWith("CatBlockPuzzle", StringComparison.Ordinal) && !type.IsEnum;

        private static MemberInfo FindMember(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, Members);
            if (field != null) return field;
            PropertyInfo property = target.GetType().GetProperty(name, Members);
            return property != null && property.GetIndexParameters().Length == 0 ? property : null;
        }

        protected object Field(string name)
        {
            foreach (var target in Targets())
            {
                MemberInfo member = FindMember(target, name);
                if (member is FieldInfo field) return field.GetValue(target);
                if (member is PropertyInfo property) return property.GetValue(target);
            }
            throw new MissingMemberException("No field or property named '" + name + "' on the game or its services.");
        }

        protected void SetField(string name, object value)
        {
            foreach (var target in Targets())
            {
                MemberInfo member = FindMember(target, name);
                if (member is FieldInfo field) { field.SetValue(target, value); return; }
                if (member is PropertyInfo property && property.CanWrite) { property.SetValue(target, value); return; }
            }
            throw new MissingMemberException("No field or property named '" + name + "' on the game or its services.");
        }

        protected object Call(string name, params object[] arguments)
        {
            foreach (var target in Targets())
            {
                var method = target.GetType().GetMethods(Members)
                    .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == arguments.Length);
                if (method != null) return method.Invoke(target, arguments);
            }
            throw new MissingMethodException("No method named '" + name + "' taking " + arguments.Length + " argument(s) on the game or its services.");
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
