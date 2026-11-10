using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Fits authored portrait content inside the safe area; backgrounds remain full bleed.</summary>
    public sealed class ProductionUiSafeArea : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        private Rect lastSafeArea;
        private Vector2 lastRootSize;
        // The Shop and Gameplay pages shipped with this component switched off, so their frames overflowed the screen on
        // every aspect ratio other than the one they were authored on. Every page now fits its 1080x1920 frame.
        private void Awake() => enabled = true;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            RectTransform root = transform as RectTransform;
            if (content == null || root == null || Screen.width == 0 || Screen.height == 0) return;
            Rect safe = Screen.safeArea;
            Vector2 size = root.rect.size;
            if (safe == lastSafeArea && size == lastRootSize) return;
            lastSafeArea = safe;
            lastRootSize = size;
            // Every screen is authored on one 1080x1920 frame that is scaled to fit, so spacing is the same on every device.
            // A page whose frame was left stretched to the canvas would drift apart on other aspect ratios (header over content).
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1080f, 1920f);
            Vector2 scale = new Vector2(size.x / Screen.width, size.y / Screen.height);
            Vector2 safeSize = Vector2.Scale(safe.size, scale);
            content.localScale = Vector3.one * Mathf.Min(safeSize.x / 1080f, safeSize.y / 1920f);
            content.anchoredPosition = Vector2.Scale(safe.center, scale) - size * .5f;
        }
    }
}
