using System;
using System.Collections;
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
        private static readonly Color Full = new Color(0.95f, 0.36f, 0.42f, 1f);
        private static readonly Color Empty = new Color(0.55f, 0.47f, 0.5f, 0.45f);

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

            const float size = 44f, gap = 10f;
            RectTransform row = RuntimeUiFactory.CreateRect(parent, RowName);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -8f);
            row.sizeDelta = new Vector2(total * size + (total - 1) * gap, size);
            icons = new Image[total];
            for (int i = 0; i < total; i++)
            {
                float x = -row.sizeDelta.x * 0.5f + size * 0.5f + i * (size + gap);
                Image heart = RuntimeUiFactory.CreateImage(row, "Heart " + (i + 1), HeartArt.Heart, Full, new Vector2(x, 0f), new Vector2(size, size));
                heart.preserveAspect = true;
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

        private static Sprite Create(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Mistake Heart" };
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            Color outline = new Color(0.36f, 0.19f, 0.10f, 1f);
            float outlineWidth = 0.05f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Classic implicit heart, scaled to fill the texture.
                    float u = (x + 0.5f) / size * 2.6f - 1.3f;
                    float v = (y + 0.5f) / size * 2.6f - 1.15f;
                    float f = Mathf.Pow(u * u + v * v - 1f, 3f) - u * u * v * v * v;
                    float inside = f <= 0f ? 1f : 0f;
                    float edge = Mathf.Clamp01(-f / outlineWidth);
                    // White fill so the Image colour tints it; the outline stays cocoa.
                    Color c = Color.Lerp(outline, Color.white, edge);
                    c.a = inside;
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
