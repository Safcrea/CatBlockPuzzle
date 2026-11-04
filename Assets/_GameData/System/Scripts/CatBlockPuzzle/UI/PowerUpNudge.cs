using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The "tap this" attention marker for a power-up button: a gold glow pulsing behind it and a hand that taps it.
    /// Both are siblings of the button (the glow just under it, the hand above), so the button's own picture stays clear.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PowerUpNudge : MonoBehaviour
    {
        private RectTransform target;
        private RectTransform glow;
        private Image glowImage;
        private RectTransform hand;
        private float elapsed;

        public static PowerUpNudge Create(RectTransform button, Sprite glowSprite, Sprite handSprite)
        {
            RectTransform parent = (RectTransform)button.parent;
            RectTransform glow = RuntimeUiFactory.CreateRect(parent, "Power Up Nudge Glow");
            glow.SetSiblingIndex(button.GetSiblingIndex());
            PowerUpNudge nudge = glow.gameObject.AddComponent<PowerUpNudge>();
            nudge.target = button;
            nudge.glow = glow;
            nudge.glowImage = glow.gameObject.AddComponent<Image>();
            nudge.glowImage.sprite = glowSprite;
            nudge.glowImage.type = Image.Type.Sliced;
            nudge.glowImage.color = new Color(1f, 0.82f, 0.25f, 0.7f);
            nudge.glowImage.raycastTarget = false;

            if (handSprite != null)
            {
                nudge.hand = RuntimeUiFactory.CreateRect(parent, "Power Up Nudge Hand");
                nudge.hand.pivot = new Vector2(0.25f, 0.9f);
                nudge.hand.sizeDelta = new Vector2(124f, 124f);
                Image image = nudge.hand.gameObject.AddComponent<Image>();
                image.sprite = handSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                nudge.hand.SetAsLastSibling();
            }
            nudge.Follow();
            return nudge;
        }

        private void Follow()
        {
            if (target == null) return;
            glow.anchorMin = target.anchorMin;
            glow.anchorMax = target.anchorMax;
            glow.pivot = target.pivot;
            glow.anchoredPosition = target.anchoredPosition;
            if (hand != null)
            {
                hand.anchorMin = hand.anchorMax = target.anchorMin;
            }
        }

        private void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            elapsed += Time.unscaledDeltaTime;
            float beat = Mathf.Sin(elapsed * 6f) * 0.5f + 0.5f;
            Follow();
            glow.sizeDelta = target.sizeDelta + Vector2.one * Mathf.Lerp(26f, 48f, beat);
            glowImage.color = new Color(1f, 0.82f, 0.25f, Mathf.Lerp(0.4f, 0.9f, beat));
            if (hand != null)
            {
                // The hand rests under the button's lower corner and taps toward it.
                float tap = Mathf.Abs(Mathf.Sin(elapsed * 3.2f));
                Vector2 corner = target.anchoredPosition + new Vector2(target.sizeDelta.x * 0.1f, -target.sizeDelta.y * 0.25f);
                hand.anchoredPosition = corner + new Vector2(0f, -34f * tap);
                hand.localScale = Vector3.one * (1f - 0.12f * (1f - tap));
            }
        }

        private void OnDestroy()
        {
            if (hand != null) Destroy(hand.gameObject);
        }
    }
}
