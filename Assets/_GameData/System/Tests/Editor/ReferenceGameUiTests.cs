using System.Linq;
using CatBlockPuzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class ReferenceGameUiTests
    {
        [Test]
        public void SavedGameScene_UsesAuthoredHudAndContainsAllLevelSlots()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_GameData/System/Scenes/GameScene.unity");
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                var game = all.Select(t=>t.GetComponent<CatBlockPuzzleGame>()).Single(g=>g!=null);
                Assert.That(game.enabled,Is.True);
                Assert.That(game.ValidatePreparedScene(out var error),Is.True,error);
                var hud=(GameplayHudView)new SerializedObject(game).FindProperty("gameplayHud").objectReferenceValue;
                Assert.That(hud.UsesAuthoredLayout,Is.True);
                Assert.That(hud.transform.parent.name,Is.EqualTo("Reference UI"));
                var selector=all.Select(t=>t.GetComponent<LevelSelectionScreen>()).Single(s=>s!=null);
                Assert.That(selector.SlotCount,Is.EqualTo(100));
                Assert.That(all.Single(t=>t.name=="Safe Area").gameObject.activeSelf,Is.False);
                Assert.That(all.Single(t=>t.name=="Reference UI").gameObject.activeSelf,Is.True);
                var slots=new SerializedObject(selector).FindProperty("levels");
                for(int i=0;i<slots.arraySize;i++)
                {
                    var slot=slots.GetArrayElementAtIndex(i);
                    foreach(string field in new[]{"button","number","background","locked"})
                        Assert.That(slot.FindPropertyRelative(field).objectReferenceValue,Is.Not.Null,$"Level {i+1}: {field}");
                    Assert.That(slot.FindPropertyRelative("stars").arraySize,Is.EqualTo(3));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
