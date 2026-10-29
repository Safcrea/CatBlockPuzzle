using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Small radial sparkle burst for UI celebrations (a power-up landing, a purchase).</summary>
    internal static class UiBurst
    {
        private static Sprite dot;

        /// <summary>Spawns <paramref name="count"/> sparkles at <paramref name="localPosition"/> of <paramref name="parent"/> and lets them fly out and fade.</summary>
        public static void Play(MonoBehaviour runner, RectTransform parent, Vector2 localPosition, Color color, int count = 10, float distance = 150f, float duration = 0.55f)
        {
            if (runner == null || parent == null || !runner.isActiveAndEnabled) return;
            if (PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0) return;
            if (dot == null) dot = CreateDot();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = localPosition;
                rect.sizeDelta = Vector2.one * Random.Range(22f, 40f);
                var image = go.GetComponent<Image>();
                image.sprite = dot;
                image.raycastTarget = false;
                image.color = color;
                float angle = (i + Random.value * 0.6f) / count * Mathf.PI * 2f;
                runner.StartCoroutine(Fly(rect, image, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance * Random.Range(0.7f, 1.15f), duration * Random.Range(0.85f, 1.15f)));
            }
        }

        private static IEnumerator Fly(RectTransform rect, Image image, Vector2 offset, float duration)
        {
            Vector2 start = rect.anchoredPosition;
            Color color = image.color;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                if (rect == null) yield break;
                float t = elapsed / duration;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                rect.anchoredPosition = start + offset * ease;
                rect.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.2f, t);
                image.color = new Color(color.r, color.g, color.b, color.a * (1f - t * t));
                yield return null;
            }
            if (rect != null) Object.Destroy(rect.gameObject);
        }

        private static Sprite CreateDot()
        {
            const int size = 48;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Sparkle", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // A four-point star: bright core with thin rays.
                    Vector2 p = new Vector2(x, y) - center;
                    float radial = Mathf.Clamp01(1f - p.magnitude / (size * 0.5f));
                    float rays = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.y)) / 4f) * radial;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(rays, radial * radial * 1.6f))));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
