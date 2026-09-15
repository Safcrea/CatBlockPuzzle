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
                var serializedHud=new SerializedObject(hud);
                var background=(UnityEngine.UI.Image)serializedHud.FindProperty("backgroundImage").objectReferenceValue;
                Assert.That(background.GetComponent<CanvasBackgroundFitter>(),Is.Not.Null);
                Assert.That(background.sprite,Is.Not.Null);
                Assert.That(serializedHud.FindProperty("freezeButton").objectReferenceValue,Is.Not.Null);
                Assert.That(all.Single(t=>t.name=="Tile Break").gameObject.activeSelf,Is.False);
                var daily=all.Select(t=>t.GetComponent<DailyRewardScreen>()).Single(s=>s!=null);
                var days=new SerializedObject(daily).FindProperty("days");
                Assert.That(days.arraySize,Is.EqualTo(7));
                for(int i=0;i<days.arraySize;i++)
                {
                    var day=days.GetArrayElementAtIndex(i);
                    Assert.That(day.FindPropertyRelative("cardButton").objectReferenceValue,Is.Not.Null);
                    Assert.That(day.FindPropertyRelative("cardImage").objectReferenceValue,Is.Not.Null);
                    if(i>=6) continue;
                    foreach(string field in new[]{"claimButton","claimImage","availableCardSprite","claimedCardSprite","availableClaimSprite","claimedClaimSprite"})
                        Assert.That(day.FindPropertyRelative(field).objectReferenceValue,Is.Not.Null,$"Day {i+1}: {field}");
                }
                var presentation=new SerializedObject(new SerializedObject(game).FindProperty("presentationAssets").objectReferenceValue);
                Assert.That(AssetDatabase.GetAssetPath(presentation.FindProperty("starSprite").objectReferenceValue),Does.Contain("Slicing/Gameplay"));
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
