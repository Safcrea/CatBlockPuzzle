using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzlePortraitTests
    {
        [UnityTest]
        public IEnumerator PortraitLayout_UsesAuthoredCatsAndSeparatedGameplayZones()
        {
            Screen.SetResolution(540, 960, false);

            GameObject canvasObject = null;
            for (int i = 0; i < 120 && canvasObject == null; i++)
            {
                canvasObject = GameObject.Find("Cat Puzzle Canvas");
                yield return null;
            }

            Assert.That(canvasObject, Is.Not.Null, "Runtime canvas was not created.");
            yield return new WaitForSecondsRealtime(0.7f);

            RectTransform safeArea = FindRect(canvasObject.transform, "Safe Area");
            RectTransform board = FindRect(canvasObject.transform, "Board Frame");
            RectTransform tray = FindRect(canvasObject.transform, "Shelf");
            RectTransform actions = FindRect(canvasObject.transform, "Actions");
            Assert.That(safeArea, Is.Not.Null);
            Assert.That(board, Is.Not.Null);
            Assert.That(tray, Is.Not.Null);
            Assert.That(actions, Is.Not.Null);

            Rect boardRect = ScreenRect(board);
            Rect trayRect = ScreenRect(tray);
            Rect actionRect = ScreenRect(actions);
            Assert.That(boardRect.yMin, Is.GreaterThanOrEqualTo(trayRect.yMax - 1f), "Board overlaps the cat tray.");
            Assert.That(trayRect.yMin, Is.GreaterThanOrEqualTo(actionRect.yMax - 1f), "Tray overlaps the action controls.");

            bool foundAuthoredCat = false;
            Image[] images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                Sprite sprite = images[i].sprite;
                if (images[i].name == "Cat Cell" && sprite != null && sprite.texture != null && sprite.texture.name.StartsWith("cat_portraits"))
                {
                    foundAuthoredCat = true;
                    break;
                }
            }

            Assert.That(foundAuthoredCat, Is.True, "Cat cells did not use the authored portrait atlas.");

            string artifactDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestArtifacts"));
            Directory.CreateDirectory(artifactDirectory);
            string screenshotPath = Path.Combine(artifactDirectory, "portrait-540x960.png");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            GameObject cameraObject = new GameObject("Portrait Test Camera", typeof(Camera));
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
            yield return null;
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
            Assert.That(new FileInfo(screenshotPath).Length, Is.GreaterThan(1024), "Portrait screenshot was empty.");
        }

        private static RectTransform FindRect(Transform parent, string name)
        {
            RectTransform[] children = parent.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
