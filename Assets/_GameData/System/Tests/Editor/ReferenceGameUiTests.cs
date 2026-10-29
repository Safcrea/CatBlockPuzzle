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
                Assert.That(all.Where(t=>t.name=="Tile Break"),Is.Empty,"Tile Break is obsolete and removed by FullGameUiAuthoring.");
                var daily=all.Select(t=>t.GetComponent<DailyRewardScreen>()).Single(s=>s!=null);
                var dailySerialized=new SerializedObject(daily);
                // The redesigned popup claims through one Claim button; the day cards only display state.
                foreach(string field in new[]{"claimCurrentButton","laterButton","closeButton"})
                    Assert.That(dailySerialized.FindProperty(field).objectReferenceValue,Is.Not.Null,field);
                var days=dailySerialized.FindProperty("days");
                Assert.That(days.arraySize,Is.EqualTo(7));
                for(int i=0;i<days.arraySize;i++)
                {
                    var day=days.GetArrayElementAtIndex(i);
                    foreach(string field in new[]{"cardImage","claimDaySprite","backgroundDaySprite","claimedCardSprite","lockedCardSprite","dayLabel"})
                        Assert.That(day.FindPropertyRelative(field).objectReferenceValue,Is.Not.Null,$"Day {i+1}: {field}");
                }
                var presentation=new SerializedObject(new SerializedObject(game).FindProperty("presentationAssets").objectReferenceValue);
                Assert.That(AssetDatabase.GetAssetPath(presentation.FindProperty("starSprite").objectReferenceValue),Does.Contain("CatBlock_UI_New/UI/icons"));
                Assert.That(hud.transform.parent.name,Is.EqualTo("MainUi"));
                Assert.That(hud.gameObject.name,Is.EqualTo("Gameplay Screen"));
                // The redesigned Levels page no longer carries a LevelSelectionScreen; when one is present it must still list every level.
                var selector=all.Select(t=>t.GetComponent<LevelSelectionScreen>()).SingleOrDefault(s=>s!=null);
                if(selector!=null) Assert.That(selector.SlotCount,Is.EqualTo(100));
                Assert.That(all.Single(t=>t.name=="MainUi").gameObject.activeSelf,Is.True);
                Assert.That(all.Single(t=>t.name=="MainUi").GetComponent<CatBlockPuzzle.KawaiiUI.SafeAreaFitter>(),Is.Null,
                    "Fitting the entire page clips backgrounds and changes the authored layout on entering Play Mode");
                var slots=selector!=null ? new SerializedObject(selector).FindProperty("levels") : null;
                for(int i=0;slots!=null && i<slots.arraySize;i++)
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
