using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzleMetaUiTests : PreparedScenePlayModeFixture
    {
        [UnityTest]
        public IEnumerator RoomHubAndDetail_UseTenChaptersAndAuthoredArt()
        {
            Screen.SetResolution(540, 960, false);
            MonoBehaviour game = null;
            for (int frame = 0; frame < 120 && game == null; frame++)
            {
                MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i].GetType().FullName == "CatBlockPuzzle.CatBlockPuzzleGame")
                    {
                        game = behaviours[i];
                        break;
                    }
                }

                yield return null;
            }

            Assert.That(game, Is.Not.Null, "Runtime game was not created.");
            System.Type gameType = game.GetType();
            MethodInfo previewLevel = gameType.GetMethod(
                "PreviewLevelForTesting",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(previewLevel, Is.Not.Null);
            previewLevel.Invoke(game, new object[] { 0 });
            yield return new WaitForSecondsRealtime(0.8f);

            Image gameplayBackground = (Image)gameType
                .GetField("backgroundImage", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game);
            Assert.That(gameplayBackground.sprite, Is.Not.Null);
            Assert.That(gameplayBackground.sprite.texture.name, Is.EqualTo("room_01"));

            MethodInfo openHub = gameType.GetMethod(
                "OpenRoomHub",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(openHub, Is.Not.Null);
            openHub.Invoke(game, null);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Transform overlay = FindTransform(canvas.transform, "Home and Rooms");
            Transform hub = FindTransform(overlay, "Home Screen");
            Transform chapters = FindTransform(hub, "Chapters");
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(hub.gameObject.activeSelf, Is.True);
            Assert.That(chapters.childCount, Is.EqualTo(10));

            for (int chapter = 0; chapter < 10; chapter++)
            {
                Transform card = FindTransform(chapters, "Chapter " + (chapter + 1));
                Image thumbnail = FindTransform(card, "Room Thumbnail").GetComponent<Image>();
                Assert.That(thumbnail.sprite, Is.Not.Null, "Missing thumbnail for chapter " + (chapter + 1));
                Assert.That(thumbnail.sprite.texture.name, Is.EqualTo("room_" + (chapter + 1).ToString("00") + "_thumb"));
            }

            CaptureCanvas(canvas, "meta-hub-540x960.png");

            MethodInfo openRoom = gameType.GetMethod(
                "OpenRoomDetail",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(openRoom, Is.Not.Null);
            openRoom.Invoke(game, new object[] { 0 });
            yield return null;
            Canvas.ForceUpdateCanvases();

            Transform roomDetail = FindTransform(overlay, "Room_01");
            Image roomBackground = FindTransform(roomDetail, "Room Background").GetComponent<Image>();
            Transform treasureRail = FindTransform(roomDetail, "Treasure Rail");
            Assert.That(roomDetail.gameObject.activeSelf, Is.True);
            Assert.That(roomBackground.sprite, Is.Not.Null);
            Assert.That(roomBackground.sprite.texture.name, Is.EqualTo("room_01"));
            Assert.That(treasureRail.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(5));

            CaptureCanvas(canvas, "meta-room-01-540x960.png");

            previewLevel.Invoke(game, new object[] { 0 });
        }

        private static void CaptureCanvas(Canvas canvas, string fileName)
        {
            CaptureAssignedCamera(canvas, fileName);
        }

        private static Transform FindTransform(Transform parent, string name)
        {
            Assert.That(parent, Is.Not.Null, "Cannot find " + name + " under a null parent.");
            Transform[] descendants = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == name)
                {
                    return descendants[i];
                }
            }

            Assert.Fail("Missing runtime UI object " + name + ".");
            return null;
        }
    }
}
