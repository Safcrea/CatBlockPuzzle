using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzleMetaUiTests
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
            Transform overlay = FindTransform(canvas.transform, "Meta Overlay");
            Transform hub = FindTransform(overlay, "Room Hub");
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

            Transform roomDetail = FindTransform(overlay, "Room Detail");
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
            string artifactDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestArtifacts"));
            Directory.CreateDirectory(artifactDirectory);
            string screenshotPath = Path.Combine(artifactDirectory, fileName);
            GameObject cameraObject = new GameObject("Meta UI Test Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.98f, 0.95f, 0.9f, 1f);
            camera.orthographic = true;
            camera.aspect = 540f / 960f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            RenderTexture target = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D screenshot = new Texture2D(540, 960, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0f, 0f, 540f, 960f), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
            RenderTexture.active = previous;
            Object.Destroy(screenshot);
            Object.Destroy(target);
            Object.Destroy(cameraObject);
            Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(1024));
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
