using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>A short "EASY / MEDIUM / HARD" banner shown over the board right before a level starts.</summary>
    internal static class DifficultyBanner
    {
        private const float PopSeconds = 0.25f;
        private const float HoldSeconds = 0.7f;
        private const float FadeSeconds = 0.25f;

        public static IEnumerator Show(Canvas canvas, Font font, DifficultyTier tier, bool reducedMotion)
        {
            if (canvas == null) yield break;

            // A full-screen dim fades in behind the word, like the tutorial and popup overlays.
            var root = new GameObject("DifficultyBanner", typeof(RectTransform), typeof(CanvasGroup));
            var full = root.GetComponent<RectTransform>();
            full.SetParent(canvas.transform, false);
            full.anchorMin = Vector2.zero;
            full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            var group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            // If the level restarts mid-banner the coroutine is stopped without running the cleanup below.
            Object.Destroy(root, PopSeconds + HoldSeconds + FadeSeconds + 0.5f);

            var dimObject = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            var dimRect = dimObject.GetComponent<RectTransform>();
            dimRect.SetParent(full, false);
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            var dim = dimObject.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = false;

            var wordObject = new GameObject("Word", typeof(RectTransform));
            var rect = wordObject.GetComponent<RectTransform>();
            rect.SetParent(full, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.55f);
            rect.sizeDelta = new Vector2(760f, 220f);

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var label = textObject.GetComponent<Text>();
            label.font = font;
            label.text = DifficultyTiers.Label(tier).ToUpperInvariant();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 150;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 40;
            label.resizeTextMaxSize = 150;
            label.color = DifficultyTiers.Colour(tier);
            label.raycastTarget = false;
            var outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.95f);
            outline.effectDistance = new Vector2(6f, -6f);

            try
            {
                float t = 0f;
                float pop = reducedMotion ? 0f : PopSeconds;
                while (t < pop)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / pop);
                    // Slight overshoot so the word lands with a bounce.
                    float scale = Mathf.LerpUnclamped(0.5f, 1f, 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f));
                    rect.localScale = Vector3.one * scale;
                    group.alpha = k;
                    yield return null;
                }
                rect.localScale = Vector3.one;
                group.alpha = 1f;

                for (float held = 0f; held < HoldSeconds; held += Time.unscaledDeltaTime) yield return null;

                float fade = reducedMotion ? 0f : FadeSeconds;
                for (float f = 0f; f < fade; f += Time.unscaledDeltaTime)
                {
                    group.alpha = 1f - f / fade;
                    yield return null;
                }
            }
            finally
            {
                if (root != null) Object.Destroy(root);
            }
        }
    }
}
