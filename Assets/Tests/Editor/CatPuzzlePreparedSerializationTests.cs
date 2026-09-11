using System.Linq;
using CatBlockPuzzle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPuzzlePreparedSerializationTests
    {
        [Test]
        public void EditorPreview_LoadsOnlyOneTemporaryPrefab()
        {
            var scene = EditorSceneManager.OpenPreviewScene(CatPuzzleScenePreparation.ScenePath);
            try
            {
                var game = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatBlockPuzzleGame>(true)).Single();
                game.PreviewPreparedLevelInEditor(0);
                var first = game.LoadedLevel;
                Assert.That(first,Is.Not.Null);
                Assert.That(first.gameObject.hideFlags.HasFlag(HideFlags.DontSaveInEditor),Is.True);
                game.PreviewPreparedLevelInEditor(99);
                Assert.That(first == null,Is.True,"Old preview was retained");
                Assert.That(game.LoadedLevel.levelIndex,Is.EqualTo(99));
                Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatPuzzleLevelView>(true)).Count(),Is.EqualTo(1));
                game.ClearLevelPreviewInEditor();
                Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatPuzzleLevelView>(true)),Is.Empty);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void SavedScene_ReopensWithAllLevelAssetsAndEventsAssigned()
        {
            var scene = EditorSceneManager.OpenPreviewScene(CatPuzzleScenePreparation.ScenePath);
            try
            {
                CatPuzzleScenePreparation.ValidateScene(scene);
                var game = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatBlockPuzzleGame>(true)).Single();
                Assert.That(game.LevelPrefabResources.Length,Is.EqualTo(100));
                Assert.That(game.LevelPrefabResources.Distinct().Count(),Is.EqualTo(100));
                Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatPuzzleLevelView>(true)),Is.Empty);
                var levels = game.LevelPrefabResources.Select(p=>AssetDatabase.LoadAssetAtPath<CatPuzzleLevelView>("Assets/Resources/" + p + ".prefab")).ToArray();
                Assert.That(levels.Sum(l=>l.pieces.Length),Is.EqualTo(529));
                foreach (var level in levels)
                {
                    Assert.That(EditorUtility.IsPersistent(level),Is.True);
                    foreach (var piece in level.pieces)
                    {
                        Assert.That(piece.slotInput.levelIndex,Is.EqualTo(level.levelIndex));
                        Assert.That(piece.pieceInput.pieceIndex,Is.EqualTo(piece.pieceIndex));
                        foreach (var cat in piece.cats) Assert.That(EditorUtility.IsPersistent(cat.sprite),Is.True);
                    }
                    foreach (var component in level.GetComponentsInChildren<Component>(true))
                    {
                        Assert.That(component,Is.Not.Null,"Missing prefab script");
                        // Every object/component dependency must be inside this prefab, never a scene object.
                        var property = new SerializedObject(component).GetIterator();
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var target = property.objectReferenceValue;
                            if (target is GameObject || target is Component)
                                Assert.That(AssetDatabase.GetAssetPath(target),Is.EqualTo(AssetDatabase.GetAssetPath(level)),property.propertyPath);
                        }
                    }
                }
                var dependencies = AssetDatabase.GetDependencies(CatPuzzleScenePreparation.ScenePath,true);
                Assert.That(dependencies.Any(p=>p.StartsWith(CatBlockPuzzleGame.LevelPrefabFolder + "/")),Is.False,
                    "UI scene must not directly reference all level prefab assets.");
                foreach (var button in game.PreparedCanvas.GetComponentsInChildren<Button>(true))
                {
                    Assert.That(button.onClick.GetPersistentEventCount(),Is.EqualTo(1),button.name);
                    Assert.That(button.onClick.GetPersistentTarget(0),Is.Not.Null,button.name);
                }
                foreach (var text in game.PreparedCanvas.GetComponentsInChildren<Text>(true))
                    Assert.That(text.font,Is.Not.Null,text.name);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
