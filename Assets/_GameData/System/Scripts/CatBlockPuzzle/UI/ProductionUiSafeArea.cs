using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Fits authored portrait content inside the safe area; backgrounds remain full bleed.</summary>
    public sealed class ProductionUiSafeArea : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        private Rect lastSafeArea;
        private Vector2 lastRootSize;
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
            Vector2 scale = new Vector2(size.x / Screen.width, size.y / Screen.height);
            Vector2 safeSize = Vector2.Scale(safe.size, scale);
            content.localScale = Vector3.one * Mathf.Min(safeSize.x / 1080f, safeSize.y / 1920f);
            content.anchoredPosition = Vector2.Scale(safe.center, scale) - size * .5f;
        }
    }
}
