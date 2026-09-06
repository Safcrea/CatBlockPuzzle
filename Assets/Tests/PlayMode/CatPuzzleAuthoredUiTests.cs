using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPuzzleAuthoredUiTests : AuthoredSceneTestBase
    {
        [UnityTest]
        public IEnumerator EveryLevelAndReset_ReusesSceneObjectsAndSprites()
        {
            Behaviour game = null;
            foreach (Behaviour behaviour in UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
            {
                if (behaviour.GetType().FullName == "CatBlockPuzzle.CatBlockPuzzleGame")
                {
                    game = behaviour;
                    break;
                }
            }
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            var root = GameObject.Find("Cat Puzzle Canvas");
            Assert.That(root, Is.Not.Null);
            gameType.GetField("reducedMotion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(game, true);
            MethodInfo previewLevel = gameType.GetMethod("PreviewLevelForTesting", BindingFlags.Instance | BindingFlags.Public);
            int[] objects = Ids(root);
            int[] sprites = Resources.FindObjectsOfTypeAll<Sprite>().Select(s => s.GetInstanceID()).OrderBy(i => i).ToArray();
            for (int level = 0; level < 100; level++)
            {
                previewLevel.Invoke(game, new object[] { level });
                yield return null;
                CollectionAssert.AreEqual(objects, Ids(root), "UI objects changed at level " + (level + 1));
            }
            Button reset = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "Reset");
            reset.onClick.Invoke();
            yield return null;
            CollectionAssert.AreEqual(objects, Ids(root), "Reset created or destroyed UI.");
            CollectionAssert.AreEqual(sprites, Resources.FindObjectsOfTypeAll<Sprite>().Select(s => s.GetInstanceID()).OrderBy(i => i).ToArray(), "Level transitions generated sprites.");

            Button settings = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "Settings");
            settings.onClick.Invoke();
            yield return null;
            Canvas modal = root.GetComponentsInChildren<Canvas>(true).Single(c => c.name == "Settings Overlay");
            Assert.That(modal.gameObject.activeSelf, Is.True, "Authored settings callback is not bound.");
            Assert.That(modal.overrideSorting, Is.True);
            Assert.That(modal.sortingOrder, Is.GreaterThan(root.GetComponentsInChildren<Canvas>(true).Single(c => c.name == "Piece Layer").sortingOrder));
            modal.GetComponentsInChildren<Button>().Single(b => b.name == "Close").onClick.Invoke();
            yield return null;
            Assert.That(modal.gameObject.activeSelf, Is.False);
            CollectionAssert.AreEqual(objects, Ids(root), "Opening a modal created UI.");
        }

        private static int[] Ids(GameObject root)
        {
            return root.GetComponentsInChildren<Component>(true).Where(c => c != null).Select(c => c.GetInstanceID()).OrderBy(i => i).ToArray();
        }
    }
}
