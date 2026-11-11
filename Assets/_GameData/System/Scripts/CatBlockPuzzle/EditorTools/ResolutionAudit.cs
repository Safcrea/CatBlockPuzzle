#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Plays every screen of the game at the pixel sizes (and notch / Dynamic Island / home-bar insets) of real Android phones and
    /// tablets, iPhones and iPads: it resizes the Game view, opens each page, checks that nothing sticks out of the screen or sits
    /// under a notch, that buttons are big enough to tap and the gameplay areas do not overlap, and saves a small screenshot of every
    /// page. Run it in Play Mode with Unity in front. The report is Temp/ResolutionAudit.txt, the pictures Temp/ResolutionShots.
    /// </summary>
    internal sealed class ResolutionAudit : MonoBehaviour
    {
        internal struct Device
        {
            public string name;
            public int w, h;
            /// <summary>Insets in pixels: left, bottom, right, top.</summary>
            public int left, bottom, right, top;
            /// <summary>The device's width in dp (points), to judge button and text sizes.</summary>
            public float dpWidth;
            public Device(string name, int w, int h, int top, int bottom, float dpWidth)
            {
                this.name = name; this.w = w; this.h = h; left = right = 0; this.top = top; this.bottom = bottom; this.dpWidth = dpWidth;
            }
        }

        public static readonly Device[] Devices =
        {
            // Android phones (a punch-hole or notch at the top; gesture bars are not part of Screen.safeArea).
            new Device("android_20x9_1080x2400", 1080, 2400, 96, 0, 393),
            new Device("android_19.5x9_1080x2340", 1080, 2340, 100, 0, 393),
            new Device("android_qhd_1440x3200", 1440, 3200, 126, 0, 412),
            new Device("android_small_720x1600", 720, 1600, 66, 0, 360),
            new Device("android_16x9_1080x1920", 1080, 1920, 0, 0, 411),
            new Device("android_foldable_open_1840x2208", 1840, 2208, 90, 0, 673),
            // Android tablets
            new Device("android_tablet_10in_1200x1920", 1200, 1920, 48, 0, 800),
            new Device("android_tablet_16x10_1600x2560", 1600, 2560, 64, 0, 800),
            // iPhones (notch / Dynamic Island on top, home indicator at the bottom)
            new Device("iphone_se_750x1334", 750, 1334, 40, 0, 375),
            new Device("iphone_11_828x1792", 828, 1792, 88, 68, 414),
            new Device("iphone_13_1170x2532", 1170, 2532, 141, 102, 390),
            new Device("iphone_15pro_1179x2556", 1179, 2556, 177, 102, 393),
            new Device("iphone_15promax_1290x2796", 1290, 2796, 177, 102, 430),
            // iPads
            new Device("ipad_10.2_1620x2160", 1620, 2160, 40, 0, 810),
            new Device("ipad_air_1640x2360", 1640, 2360, 48, 40, 820),
            new Device("ipad_pro11_1668x2388", 1668, 2388, 48, 40, 834),
            new Device("ipad_pro13_2048x2732", 2048, 2732, 48, 40, 1024),
            new Device("ipad_mini_1488x2266", 1488, 2266, 48, 40, 744),
        };

        public string[] only;          // when set, only devices whose name contains one of these
        public string[] screens;       // when set, only these screens
        public bool done;

        private static readonly string[] AllScreens = { "home", "levels", "shop", "daily", "settings", "game5", "game16", "game100", "pause", "win", "fail" };
        private readonly StringBuilder report = new StringBuilder();
        private string shots;
        private int problems;

        private IEnumerator Start()
        {
            shots = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ResolutionShots"));
            if (Directory.Exists(shots)) Directory.Delete(shots, true);
            Directory.CreateDirectory(shots);
            string[] keys = TutorialReset.SeenKeys();
            foreach (string key in keys) PlayerPrefs.SetInt(key, 1); // tutorials would dim and cover the page
            const string rewardKey = "CatBlockPuzzle.DailyReward.State.v1";
            string reward = PlayerPrefs.HasKey(rewardKey) ? PlayerPrefs.GetString(rewardKey) : null;
            report.AppendLine("Resolution audit, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            foreach (Device d in Devices)
            {
                if (only != null && only.Length > 0 && Array.FindIndex(only, o => d.name.Contains(o)) < 0) continue;
                yield return RunDevice(d);
            }
            ProductionUiSafeArea.TestSafeAreaFractions = null;
            foreach (string key in keys) PlayerPrefs.DeleteKey(key);
            if (reward != null) PlayerPrefs.SetString(rewardKey, reward); else PlayerPrefs.DeleteKey(rewardKey);
            PlayerPrefs.Save();
            SetGameViewSize(1080, 1920);
            report.AppendLine();
            report.AppendLine(problems == 0 ? "NO PROBLEMS FOUND" : problems + " PROBLEM(S) FOUND");
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ResolutionAudit.txt")), report.ToString());
            Debug.Log("[ResolutionAudit] finished: " + problems + " problem(s)");
            done = true;
        }

        private IEnumerator RunDevice(Device d)
        {
            SetGameViewSize(d.w, d.h);
            for (int i = 0; i < 40 && (Screen.width != d.w || Screen.height != d.h); i++) yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            report.AppendLine();
            report.AppendLine("== " + d.name + " (asked " + d.w + "x" + d.h + ", got " + Screen.width + "x" + Screen.height + ")");
            if (Screen.width != d.w || Screen.height != d.h)
            {
                report.AppendLine("  NOTE: the Game view could not take this exact size; results are for " + Screen.width + "x" + Screen.height);
                problems++;
            }
            int W = Screen.width, H = Screen.height;
            // Insets scale with the size we actually got.
            float kx = W / (float)d.w, ky = H / (float)d.h;
            var fractions = new Rect(d.left * kx / W, d.bottom * ky / H, 1f - (d.left + d.right) * kx / W, 1f - (d.bottom + d.top) * ky / H);
            ProductionUiSafeArea.TestSafeAreaFractions = fractions;
            yield return new WaitForSecondsRealtime(0.2f);

            var game = FindFirstObjectByType<CatBlockPuzzleGame>();
            var nav = FindFirstObjectByType<ReferenceUiNavigation>(FindObjectsInactive.Include);
            GameSystem gs = GameSystem.Instance;
            foreach (string screen in AllScreens)
            {
                if (screens != null && screens.Length > 0 && Array.IndexOf(screens, screen) < 0) continue;
                gs.ResumeGame();
                switch (screen)
                {
                    case "home": nav.ShowHome(); break;
                    case "levels": nav.ShowLevels(); break;
                    case "shop": nav.ShowShop(); break;
                    case "daily": nav.ShowDailyReward(); break;
                    case "settings": nav.ShowHome(); yield return Wait(); gs.OpenSettings(false); break;
                    case "game5": gs.PreviewLevel(4); break;
                    case "game16": gs.PreviewLevel(15); break;
                    case "game100": gs.PreviewLevel(99); break;
                    case "pause": gs.PreviewLevel(4); yield return Wait(); gs.OpenPause(); break;
                    case "win": gs.PreviewLevel(4); yield return Wait(); gs.ShowLevelComplete("Test", 3, 50, 3, 2); break;
                    case "fail": gs.PreviewLevel(4); yield return Wait(); gs.ShowLevelFailed("Try Again", "Out of time"); break;
                }
                yield return Wait();
                if (screen.StartsWith("game")) game.Flow.CancelReveal(true);
                yield return new WaitForSecondsRealtime(0.5f);
                Check(d, screen, W, H, fractions);
                yield return Capture(d, screen);
            }
        }

        private static IEnumerator Wait()
        {
            yield return new WaitForSecondsRealtime(1.6f);
        }

        // ------------------------------------------------------------------ checks

        private void Check(Device d, string screen, int W, int H, Rect safeFractions)
        {
            Canvas canvas = FindCanvas();
            if (canvas == null) { Add(d, screen, "no canvas found"); return; }
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            float dpPerPx = d.dpWidth / W;
            Rect safePx = new Rect(safeFractions.x * W, safeFractions.y * H, safeFractions.width * W, safeFractions.height * H);
            var seen = new HashSet<string>();
            foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.isActiveAndEnabled || g.canvasRenderer.cull || g.canvasRenderer.GetInheritedAlpha() < 0.05f || g.color.a < 0.05f) continue;
                var image = g as Image;
                if (image != null && image.sprite == null && !(g.raycastTarget && g.GetComponentInParent<Selectable>() != null)) continue;
                var text = g as Text;
                if (text != null && string.IsNullOrWhiteSpace(text.text)) continue;
                Rect r = ScreenRect(g.rectTransform, cam);
                if (r.width < 1f || r.height < 1f) continue;
                string path = PathOf(g.transform);
                bool fullBleed = r.width >= 0.85f * W && r.height >= 0.6f * H || r.height >= 0.85f * H && r.width >= 0.6f * W;
                bool decor = IsDecor(path);
                // 1. Sticking out of the screen.
                float out_ = Mathf.Max(-r.xMin, r.xMax - W, -r.yMin, r.yMax - H);
                if (!fullBleed && !decor && out_ > 3f && seen.Add("out:" + g.name))
                    Add(d, screen, "sticks out of the screen by " + out_.ToString("0") + "px: " + path);
                // 2. Text that is too small to read (dp of the rendered glyph height).
                if (text != null && !decor)
                {
                    float px = Mathf.Max(text.fontSize, 1) * (r.height / Mathf.Max(1f, g.rectTransform.rect.height));
                    if (text.resizeTextForBestFit) px = Mathf.Min(px, Mathf.Max(text.resizeTextMaxSize, 1) * (r.height / Mathf.Max(1f, g.rectTransform.rect.height)));
                    float dp = px * dpPerPx;
                    if (dp < 10f && seen.Add("small:" + g.name)) Add(d, screen, "text '" + Trim(text.text) + "' is only about " + dp.ToString("0.0") + "dp tall: " + path);
                }
            }
            foreach (Selectable s in canvas.GetComponentsInChildren<Selectable>(false))
            {
                if (!s.isActiveAndEnabled || !s.interactable) continue;
                var rt = s.transform as RectTransform;
                if (rt == null) continue;
                // A button whose own picture is hidden (a hit box inside another button) is judged by its parent.
                Rect r = ScreenRect(rt, cam);
                if (r.width < 1f || r.height < 1f) continue;
                string path = PathOf(s.transform);
                if (r.width >= 0.85f * W && r.height >= 0.4f * H) continue; // a full-screen tap catcher
                // 3. Buttons too small to tap (about 7 mm; 40 dp).
                float minDp = Mathf.Min(r.width, r.height) * dpPerPx;
                if (minDp < 38f && seen.Add("tiny:" + path)) Add(d, screen, "button is only " + minDp.ToString("0") + "dp across: " + path);
                // 4. Buttons under a notch, a Dynamic Island or the home bar.
                float under = Mathf.Max(safePx.xMin - r.xMin, r.xMax - safePx.xMax, safePx.yMin - r.yMin, r.yMax - safePx.yMax);
                if (under > 3f && seen.Add("safe:" + path)) Add(d, screen, "button reaches " + under.ToString("0") + "px outside the safe area: " + path);
            }
            if (screen.StartsWith("game")) CheckGameplayAreas(d, screen, canvas, cam, W, H);
        }

        // The board, the shelf and the power-up row must stack top to bottom without overlapping.
        private void CheckGameplayAreas(Device d, string screen, Canvas canvas, Camera cam, int W, int H)
        {
            Rect? board = Find(canvas, cam, "Board"), shelf = Find(canvas, cam, "Pieces Panel"), actions = Find(canvas, cam, "Gameplay Actions");
            if (board.HasValue && shelf.HasValue && board.Value.yMin < shelf.Value.yMax - 4f)
                Add(d, screen, "the board overlaps the shelf by " + (shelf.Value.yMax - board.Value.yMin).ToString("0") + "px");
            if (shelf.HasValue && actions.HasValue && shelf.Value.yMin < actions.Value.yMax - 4f)
            {
                // The power-up row is a strip of buttons; compare to the top of the buttons instead of the strip.
                float top = actions.Value.yMax;
                foreach (Button b in canvas.GetComponentsInChildren<Button>(false))
                    if (b.transform.parent != null && b.transform.parent.name == "Gameplay Actions") top = Mathf.Min(top, ScreenRect((RectTransform)b.transform, cam).yMax);
                if (shelf.Value.yMin < top - 4f) Add(d, screen, "the shelf overlaps the power-up buttons by " + (top - shelf.Value.yMin).ToString("0") + "px");
            }
        }

        private static Rect? Find(Canvas canvas, Camera cam, string name)
        {
            foreach (RectTransform rt in canvas.GetComponentsInChildren<RectTransform>(false))
                if (rt.name == name && rt.gameObject.activeInHierarchy) return ScreenRect(rt, cam);
            return null;
        }

        private static bool IsDecor(string path)
        {
            string p = path.ToLowerInvariant();
            return p.Contains("background") || p.Contains("dimmer") || p.Contains("backdrop") || p.Contains("leaf") || p.Contains("leaves") ||
                p.Contains("vignette") || p.Contains("glow") || p.Contains("shadow") || p.Contains("fx layer") || p.Contains("fullscreen") ||
                p.Contains("overlay") || p.Contains("blocker") || p.Contains("catcher") || p.Contains("tail") || p.Contains("decor") ||
                p.Contains("plant") || p.Contains("castle") || p.Contains("sand") || p.Contains("ambient") || p.Contains("clouds");
        }

        private void Add(Device d, string screen, string what)
        {
            problems++;
            report.AppendLine("  [" + screen + "] " + what);
        }

        // ------------------------------------------------------------------ helpers

        private static Canvas FindCanvas()
        {
            GameObject go = GameObject.Find("Canvas");
            return go != null ? go.GetComponent<Canvas>() : FindFirstObjectByType<Canvas>();
        }

        private static Rect ScreenRect(RectTransform rt, Camera cam)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, c[0]), b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            int depth = 0;
            for (Transform p = t.parent; p != null && depth < 6; p = p.parent, depth++) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        private static string Trim(string s) => s.Length > 24 ? s.Substring(0, 24) + "..." : s.Replace("\n", " ");

        private IEnumerator Capture(Device d, string screen)
        {
            yield return new WaitForEndOfFrame();
            Texture2D full = ScreenCapture.CaptureScreenshotAsTexture();
            int w = 300, h = Mathf.Max(1, Mathf.RoundToInt(300f * full.height / full.width));
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(full, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var small = new Texture2D(w, h, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            small.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(shots, screen + "__" + d.name + ".png"), small.EncodeToPNG());
            Destroy(full);
            Destroy(small);
        }

        /// <summary>Resizes the Game view to an exact pixel size (in the build target's size list), adding the custom size once.</summary>
        internal static void SetGameViewSize(int width, int height)
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = singleton.GetProperty("instance").GetValue(null, null);
            // The Game view lists sizes per build target (this project builds for Android), so use the group it is showing now.
            object groupType0 = sizesType.GetProperty("currentGroupType").GetValue(instance, null);
            object group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType0 });
            Type groupType = group.GetType();
            string label = "CBP " + width + "x" + height;
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize");
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                if ((string)size.GetType().GetProperty("baseText").GetValue(size, null) == label) { index = i; break; }
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
