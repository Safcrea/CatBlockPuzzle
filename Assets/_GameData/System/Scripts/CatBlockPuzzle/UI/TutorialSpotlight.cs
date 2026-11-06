using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The tutorial focus used for every power-up introduction, in the style of the TangleRings tutorials: the whole screen is
    /// dimmed except a soft round hole over one button, a glowing ring pulses out of the hole, the button bounces, a short
    /// bold label sits next to it and a hand pokes it. The dim swallows every tap outside the hole, so nothing else can be
    /// touched. It is built from code (two small textures made once), so it needs no prefab.
    /// </summary>
    internal sealed class TutorialSpotlight : MonoBehaviour
    {
        private const float HoleInner = 0.62f;  // clear radius of the hole sprite, as a fraction of its half size
        private const float HoleScale = 0.62f;  // hole radius, in button sizes
        private static readonly Color Dim = new Color(0.05f, 0.03f, 0.02f, 0.82f);
        private static readonly Color Glow = new Color(1f, 0.86f, 0.35f, 1f);
        private static readonly Color LabelOutline = new Color(0.28f, 0.16f, 0.1f, 1f);
        private static Sprite holeSprite, ringSprite;

        private RectTransform target, hole, top, bottom, left, right, ring, label, hand, catcher;
        private Image ringImage;
        private Text labelText;
        private CanvasGroup group;
        private Vector3 targetScale;
        private float elapsed;
        private float labelBelow;
        private bool tapped;
        private bool building;

        /// <summary>True once the player pressed the spotlit button.</summary>
        public bool Tapped => tapped;

        /// <summary>Creates the spotlight under <paramref name="canvas"/>, above everything already there; null without a canvas.</summary>
        public static TutorialSpotlight Create(Canvas canvas, Font font)
        {
            if (canvas == null) return null;
            RectTransform root = RuntimeUiFactory.CreateRect(canvas.transform, "Tutorial Spotlight");
            RuntimeUiFactory.Stretch(root);
            root.SetAsLastSibling();
            var spot = root.gameObject.AddComponent<TutorialSpotlight>();
            spot.group = root.gameObject.AddComponent<CanvasGroup>();
            spot.group.alpha = 0f;
            spot.labelText = null;
            spot.font = font;
            return spot;
        }

        private Font font;

        /// <summary>
        /// Shows the spotlight on <paramref name="button"/> and waits. With <paramref name="waitForTap"/> it ends when the
        /// player presses the button (the tap is swallowed here; the caller decides what the press does), otherwise after
        /// <paramref name="seconds"/>.
        /// </summary>
        public IEnumerator Run(RectTransform button, string text, Sprite handSprite, bool waitForTap, float seconds)
        {
            Build(button, text, handSprite, waitForTap);
            float fade = 0f;
            while (fade < 0.3f)
            {
                fade += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(fade / 0.3f);
                yield return null;
            }
            group.alpha = 1f;
            float waited = 0f;
            while (this != null && !(waitForTap && tapped) && (waitForTap || waited < seconds))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (this == null) yield break;
            group.blocksRaycasts = false;
            float fadeOut = 0f;
            while (fadeOut < 0.2f && this != null)
            {
                fadeOut += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(fadeOut / 0.2f);
                yield return null;
            }
            Cancel();
        }

        /// <summary>Takes the spotlight away at once and puts the button back to its own size.</summary>
        public void Cancel()
        {
            if (target != null) target.localScale = targetScale;
            if (this != null) Destroy(gameObject);
        }

        private void Build(RectTransform button, string text, Sprite handSprite, bool tappable)
        {
            building = true;
            target = button;
            targetScale = button.localScale;
            var root = (RectTransform)transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = Mathf.Abs(button.lossyScale.x / Mathf.Max(0.0001f, root.lossyScale.x));
            float size = Mathf.Max(button.rect.width, button.rect.height) * scale;
            float holeRadius = size * HoleScale;

            hole = Panel("Hole", HoleSprite(), Dim, false);
            hole.sizeDelta = Vector2.one * holeRadius / HoleInner * 2f;
            top = Panel("Top", null, Dim, true);
            bottom = Panel("Bottom", null, Dim, true);
            left = Panel("Left", null, Dim, true);
            right = Panel("Right", null, Dim, true);
            ring = Panel("Ring", RingSprite(), Glow, false);
            ring.sizeDelta = Vector2.one * holeRadius * 2.1f;
            ringImage = ring.GetComponent<Image>();

            // A clear catcher over the hole takes the press, so the caller can decide what it does.
            catcher = Panel("Catcher", null, new Color(1f, 1f, 1f, 0.004f), tappable);
            catcher.sizeDelta = Vector2.one * holeRadius * 2f;
            if (tappable)
            {
                var btn = catcher.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => tapped = true);
            }

            RectTransform labelRect = RuntimeUiFactory.CreateRect(root, "Label");
            label = labelRect;
            label.sizeDelta = new Vector2(900f, 150f);
            labelText = labelRect.gameObject.AddComponent<Text>();
            labelText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.text = text;
            labelText.fontSize = 64;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            labelText.raycastTarget = false;
            var outline = labelRect.gameObject.AddComponent<Outline>();
            outline.effectColor = LabelOutline;
            outline.effectDistance = new Vector2(4f, -4f);
            // Below the button when it sits in the top half of the screen, above it otherwise.
            Vector2 centre = (Vector2)root.InverseTransformPoint(button.TransformPoint(button.rect.center));
            labelBelow = centre.y > 0f ? -1f : 1f;

            if (handSprite != null)
            {
                hand = RuntimeUiFactory.CreateRect(root, "Hand");
                hand.pivot = new Vector2(0.25f, 0.9f);
                hand.sizeDelta = new Vector2(130f, 130f);
                var img = hand.gameObject.AddComponent<Image>();
                img.sprite = handSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            building = false;
            Place();
        }

        private RectTransform Panel(string name, Sprite sprite, Color color, bool blocks)
        {
            RectTransform rect = RuntimeUiFactory.CreateRect(transform, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = blocks;
            return rect;
        }

        // The button can move (layout, safe area): keep everything on it, and run the loops from real time.
        private void LateUpdate()
        {
            if (building || target == null) return;
            elapsed += Time.unscaledDeltaTime;
            Place();
            // The ring ripples out of the hole; the button bounces; the label breathes.
            float cycle = elapsed % 1.25f;
            float k = Mathf.Clamp01(cycle / 1.1f);
            float eased = 1f - (1f - k) * (1f - k) * (1f - k);
            ring.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.55f, eased);
            ringImage.color = new Color(Glow.r, Glow.g, Glow.b, 1f - k * k);
            float bounce = 0.5f - 0.5f * Mathf.Cos(elapsed * Mathf.PI * 2f / 0.9f);
            target.localScale = targetScale * (1f + 0.12f * bounce);
            label.localScale = Vector3.one * (1f + 0.05f * (0.5f - 0.5f * Mathf.Cos(elapsed * Mathf.PI * 2f / 1.2f)));
            if (hand != null)
            {
                float poke = Mathf.Abs(Mathf.Sin(elapsed * 3.2f));
                Vector2 centre = Centre();
                Vector2 corner = centre + new Vector2(size() * 0.12f, -size() * 0.2f);
                hand.anchoredPosition = corner + new Vector2(0f, -40f * poke);
                hand.localScale = Vector3.one * (1f - 0.1f * (1f - poke));
                hand.SetAsLastSibling();
            }
        }

        private float size() => hole.sizeDelta.x * HoleInner;

        private Vector2 Centre()
        {
            var root = (RectTransform)transform;
            return root.InverseTransformPoint(target.TransformPoint(target.rect.center));
        }

        private void Place()
        {
            if (target == null || hole == null) return;
            var root = (RectTransform)transform;
            Vector2 centre = Centre();
            hole.anchoredPosition = centre;
            ring.anchoredPosition = centre;
            catcher.anchoredPosition = centre;
            Vector2 half = hole.sizeDelta * 0.5f;
            const float far = 6000f;
            Stretch(top, new Vector2(-far, centre.y + half.y), new Vector2(far, centre.y + far));
            Stretch(bottom, new Vector2(-far, centre.y - far), new Vector2(far, centre.y - half.y));
            Stretch(left, new Vector2(-far, centre.y - half.y), new Vector2(centre.x - half.x, centre.y + half.y));
            Stretch(right, new Vector2(centre.x + half.x, centre.y - half.y), new Vector2(far, centre.y + half.y));
            float offset = half.y * HoleInner + 120f;
            label.anchoredPosition = centre + new Vector2(0f, labelBelow * offset);
            float limit = root.rect.width * 0.5f - label.sizeDelta.x * 0.5f;
            if (limit > 0f) label.anchoredPosition = new Vector2(Mathf.Clamp(label.anchoredPosition.x, -limit, limit), label.anchoredPosition.y);
            else label.anchoredPosition = new Vector2(0f, label.anchoredPosition.y);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.sizeDelta = max - min;
            rect.anchoredPosition = (min + max) * 0.5f;
        }

        private static Sprite HoleSprite()
        {
            if (holeSprite != null) return holeSprite;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Spotlight Hole" };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f).magnitude / (n * 0.5f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HoleInner, HoleInner + 0.3f, d));
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            holeSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            return holeSprite;
        }

        private static Sprite RingSprite()
        {
            if (ringSprite != null) return ringSprite;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Spotlight Ring" };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f).magnitude / (n * 0.5f);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            ringSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            return ringSprite;
        }

        private void OnDestroy()
        {
            if (target != null) target.localScale = targetScale;
        }
    }
}
