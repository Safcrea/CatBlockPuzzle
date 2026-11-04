#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    /// <summary>
    /// Plays levels at phone and tablet resolutions (the Game view is resized for each) and checks that every cat on the
    /// shelf stays inside its card while it bobs, pops and grows. Screenshots land in Temp/ResolutionShots.
    /// </summary>
    public sealed class ResolutionLayoutPlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";

        private static readonly (string name, int w, int h)[] Resolutions =
        {
            ("phone_1080x2400", 1080, 2400), ("phone_1080x1920", 1080, 1920), ("phone_720x1280", 720, 1280),
            ("phone_1170x2532", 1170, 2532), ("phone_750x1334", 750, 1334), ("phone_480x854", 480, 854),
            ("phone_1440x3200", 1440, 3200), ("fold_1812x2176", 1812, 2176),
            ("tablet_1536x2048", 1536, 2048), ("tablet_2048x2732", 2048, 2732), ("tablet_1200x1920", 1200, 1920),
            ("tablet_800x1280", 800, 1280)
        };

        private static readonly int[] LevelIndices = { 0, 4, 11, 24, 49, 74, 99 };

        [UnityTest, Timeout(900000)]
        public IEnumerator EveryResolution_KeepsCatsInsideTheirCards()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ResolutionShots"));
            Directory.CreateDirectory(folder);
            var report = new List<string>();
            var failures = new List<string>();
            foreach (var res in Resolutions)
            {
                SetGameViewSize(res.w, res.h);
                for (int i = 0; i < 6; i++) yield return null;
                foreach (int levelIndex in LevelIndices)
                {
                    OpenLevel(levelIndex);
                    yield return new WaitForSecondsRealtime(1.1f);
                    float worstOverflow = 0f;
                    string worstPiece = "";
                    int frames = 0;
                    float end = Time.realtimeSinceStartup + 1.3f;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        frames++;
                        MeasureShelf(out float over, out string who);
                        if (over > worstOverflow) { worstOverflow = over; worstPiece = who; }
                    }
                    string line = $"{res.name} level{levelIndex + 1}: screen={Screen.width}x{Screen.height} overflow={worstOverflow:0.0}px {worstPiece} frames={frames}";
                    report.Add(line);
                    if (worstOverflow > 1f) failures.Add(line);
                    if (levelIndex == 4 || levelIndex == 49)
                    {
                        yield return new WaitForEndOfFrame();
                        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(Path.Combine(folder, $"{res.name}_L{levelIndex + 1}.png"), shot.EncodeToPNG());
                        UnityEngine.Object.Destroy(shot);
                    }
                }
            }
            File.WriteAllLines(Path.Combine(folder, "report.txt"), report);
            SetGameViewSize(1080, 1920);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void OpenLevel(int index)
        {
            Type type = Type.GetType("CatBlockPuzzle.GameSystem, Assembly-CSharp");
            object instance = type.GetProperty("Instance").GetValue(null);
            type.GetMethod("PreviewLevel").Invoke(instance, new object[] { index });
        }

        // How far (screen pixels) any cat cell pokes out of the card it sits on, worst case over all visible cards.
        private void MeasureShelf(out float worst, out string who)
        {
            worst = 0f;
            who = "";
            var pieces = (System.Collections.IEnumerable)Field("pieces");
            foreach (object piece in pieces)
            {
                var t = piece.GetType();
                bool placed = (bool)t.GetField("Placed").GetValue(piece);
                var slot = (RectTransform)t.GetField("Slot").GetValue(piece);
                if (placed || slot == null || !slot.gameObject.activeInHierarchy) continue;
                var cells = (List<Image>)t.GetField("CellImages").GetValue(piece);
                Rect card = ScreenRect(slot);
                float margin = Mathf.Max(card.width, card.height) * 0.03f;
                foreach (Image cell in cells)
                {
                    if (cell == null || !cell.gameObject.activeInHierarchy) continue;
                    Rect r = ScreenRect(cell.rectTransform);
                    float over = Mathf.Max(card.xMin - r.xMin, r.xMax - card.xMax, card.yMin - r.yMin, r.yMax - card.yMax) + margin;
                    if (over > worst) { worst = over; who = slot.name; }
                }
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>Resizes the Game view to an exact pixel size (Standalone group), adding the custom size once.</summary>
        internal static void SetGameViewSize(int width, int height)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = singleton.GetProperty("instance").GetValue(null, null);
            Type groupEnum = editor.GetType("UnityEditor.GameViewSizeGroupType");
            object group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { Enum.Parse(groupEnum, "Standalone") });
            Type groupType = group.GetType();
            string label = "CBP " + width + "x" + height;
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize");
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                var sizeType = size.GetType();
                if ((string)sizeType.GetProperty("baseText").GetValue(size, null) == label) { index = i; break; }
            }
            if (index < 0)
            {
                Type gvSize = editor.GetType("UnityEditor.GameViewSize");
                Type gvSizeType = editor.GetType("UnityEditor.GameViewSizeType");
                object created = Activator.CreateInstance(gvSize, Enum.Parse(gvSizeType, "FixedResolution"), width, height, label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
                index = count;
            }
            Type gameView = editor.GetType("UnityEditor.GameView");
            EditorWindow window = EditorWindow.GetWindow(gameView, false, null, false);
            gameView.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, index, null);
            window.Repaint();
        }
    }
}
#endif
