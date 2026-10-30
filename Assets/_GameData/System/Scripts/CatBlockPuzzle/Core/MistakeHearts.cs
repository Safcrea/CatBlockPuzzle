using System;
using System.Collections;
using CatBlockPuzzle.KawaiiUI;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Mistake hearts: on levels that set hearts, every cat dropped on the board where it
    /// does not fit costs one heart, and losing the last one fails the level. A row of
    /// hearts under the timer shows what is left.
    /// </summary>
    internal sealed class MistakeHearts : HostedService
    {
        private const string RowName = "Mistake Hearts";
        private static readonly Color Full = Color.white;
        private static readonly Color Empty = new Color(0.38f, 0.32f, 0.32f, 0.4f);

        private readonly PuzzleSession session;
        private readonly PlayerSettings settings;
        private Image[] icons = new Image[0];
        private int total;
        private int remaining;

        public event Action OutOfHearts;

        private readonly HapticsController haptics;

        public MistakeHearts(IGameplayContext context, PuzzleSession session, PlayerSettings settings, HapticsController haptics) : base(context)
        {
            this.haptics = haptics;
            this.session = session;
            this.settings = settings;
        }

        public bool Active => total > 0;
        public int Remaining => remaining;

        /// <summary>Refills the hearts for a fresh attempt at the active level (hidden when it has none).</summary>
        public void ResetForLevel()
        {
            LevelDefinition level = session.activeLevel;
            total = level != null ? level.Hearts : 0;
            remaining = total;
            Build();
        }

        /// <summary>Spends a heart for a bad drop; raises <see cref="OutOfHearts"/> on the last one.</summary>
        public void LoseHeart()
        {
            if (!Active || remaining <= 0 || session.levelFailed) return;
            remaining--;
            session.stats.heartsLost++;
            haptics?.Play(remaining == 0 ? HapticKind.LevelFail : HapticKind.HeartLost);
            Image lost = remaining < icons.Length ? icons[remaining] : null;
            if (lost != null)
            {
                lost.color = Empty;
                if (!settings.ReducedMotion) StartCoroutine(Pop(lost.rectTransform));
            }
            if (remaining == 0) OutOfHearts?.Invoke();
        }

        private void Build()
        {
            RectTransform timer = Hud != null ? Hud.timerPanel : null;
            Transform parent = timer != null ? timer : Hud != null ? Hud.gameplayScreen : null;
            if (parent == null) return;
            // Detach every old row now: Destroy only happens at the end of the frame, and a level can
            // reload more than once within one frame (navigation, retry).
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name != RowName) continue;
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            icons = new Image[0];
            if (!Active) return;

            // A bordered cream pill straddling the timer's bottom edge, with big hearts that read at a glance.
            const float size = 56f, gap = 12f, padX = 26f, height = 76f, border = 5f;
            float width = total * size + (total - 1) * gap + padX * 2f;
            RectTransform row = RuntimeUiFactory.CreateRect(parent, RowName);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, -(height * 0.5f) - 2f);
            row.sizeDelta = new Vector2(width, height);
            Image frame = RuntimeUiFactory.CreateImage(row, "Frame", KawaiiSprites.RoundedRect, new Color(0.47f, 0.23f, 0.11f, 1f), Vector2.zero, row.sizeDelta);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            Image fill = RuntimeUiFactory.CreateImage(row, "Fill", KawaiiSprites.RoundedRect, new Color(1f, 0.96f, 0.86f, 1f), Vector2.zero, row.sizeDelta - Vector2.one * border * 2f);
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            icons = new Image[total];
            for (int i = 0; i < total; i++)
            {
                float x = -(total * size + (total - 1) * gap) * 0.5f + size * 0.5f + i * (size + gap);
                Image heart = RuntimeUiFactory.CreateImage(row, "Heart " + (i + 1), HeartArt.Heart, Full, new Vector2(x, 0f), new Vector2(size, size));
                heart.preserveAspect = true;
                heart.raycastTarget = false;
                icons[i] = heart;
            }
        }

        private IEnumerator Pop(RectTransform heart)
        {
            const float seconds = 0.35f;
            float elapsed = 0f;
            while (elapsed < seconds && heart != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                heart.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(t * Mathf.PI));
                heart.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 14f * (1f - t));
                yield return null;
            }
            if (heart == null) yield break;
            heart.localScale = Vector3.one;
            heart.localEulerAngles = Vector3.zero;
        }
    }

    /// <summary>A procedurally drawn heart in the game's cocoa outline style.</summary>
    internal static class HeartArt
    {
        private static Sprite heart;

        public static Sprite Heart
        {
            get
            {
                if (heart == null) heart = Create(128);
                return heart;
            }
        }

        // A pink heart with a soft highlight and a cocoa outline. Classic construction: a square
        // rotated 45 degrees with a circle on each upper edge, as an anti-aliased distance field.
        private const float Half = 0.55f;
        private const float CenterY = -0.1f;

        private static Sprite Create(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Mistake Heart" };
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            Color outline = new Color(0.36f, 0.19f, 0.10f, 1f);
            Color top = new Color(1f, 0.55f, 0.62f, 1f);
            Color bottom = new Color(0.88f, 0.27f, 0.36f, 1f);
            float scale = 2.4f / size;
            const float outlineWidth = 0.08f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) * scale - 1.2f;
                    float v = (y + 0.5f) * scale - 1.2f;
                    float d = Distance(u, v);
                    float coverage = Mathf.Clamp01(0.5f - d / scale);
                    float fill = Mathf.Clamp01(0.5f - (d + outlineWidth) / scale);
                    Color body = Color.Lerp(bottom, top, Mathf.Clamp01((v + 0.9f) / 1.8f));
                    float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(-0.4f, 0.45f)) / 0.24f);
                    body = Color.Lerp(body, Color.white, shine * 0.6f);
                    Color c = Color.Lerp(outline, body, fill);
                    c.a = coverage;
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static float Distance(float u, float v)
        {
            float k = Half / Mathf.Sqrt(2f);
            float leftLobe = Vector2.Distance(new Vector2(u, v), new Vector2(-k, CenterY + k)) - Half;
            float rightLobe = Vector2.Distance(new Vector2(u, v), new Vector2(k, CenterY + k)) - Half;
            const float c45 = 0.70710678f;
            float rx = (u + (v - CenterY)) * c45;
            float ry = (-u + (v - CenterY)) * c45;
            float qx = Mathf.Abs(rx) - Half, qy = Mathf.Abs(ry) - Half;
            float square = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f);
            return Mathf.Min(Mathf.Min(leftLobe, rightLobe), square);
        }
    }
}
