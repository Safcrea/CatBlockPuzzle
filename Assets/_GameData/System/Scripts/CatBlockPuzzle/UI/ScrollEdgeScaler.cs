using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Shrinks scroll content rows as they approach the top and bottom of the viewport, so the
    /// framed centre reads as the live scroll area (ported from the Disc Smash leaderboard).
    /// Layout ignores localScale, so the list never reflows while rows shrink.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollEdgeScaler : MonoBehaviour
    {
        [SerializeField, Range(0.5f, 1f)] private float minScale = 0.9f;
        [Tooltip("Distance from the centre (0 = centre, 1 = edge) where shrinking starts at the top.")]
        [SerializeField, Range(0f, 1f)] private float topFalloffStart = 0.7f;
        [SerializeField, Range(0f, 1f)] private float bottomFalloffStart = 0.7f;
        [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private ScrollRect scroll;

        private void Awake() => scroll = GetComponent<ScrollRect>();
        private void OnDisable() => ResetScales();
        private void LateUpdate() => ApplyScales();

        public void ApplyScales()
        {
            if (scroll == null) scroll = GetComponent<ScrollRect>();
            RectTransform content = scroll.content;
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            if (content == null) return;
            float halfHeight = viewport.rect.height * 0.5f;
            if (halfHeight <= 0f) return;
            Vector2 viewportCentre = viewport.rect.center;
            for (int i = 0; i < content.childCount; i++)
            {
                var row = content.GetChild(i) as RectTransform;
                if (row == null || !row.gameObject.activeSelf) continue;
                float offset = viewport.InverseTransformPoint(row.TransformPoint(row.rect.center)).y - viewportCentre.y;
                float falloffStart = offset < 0f ? bottomFalloffStart : topFalloffStart;
                float distance01 = Mathf.Clamp01(Mathf.Abs(offset) / halfHeight);
                float falloff01 = Mathf.InverseLerp(falloffStart, 1f, distance01);
                float scale = Mathf.Lerp(1f, minScale, falloffCurve.Evaluate(falloff01));
                row.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void ResetScales()
        {
            if (scroll == null || scroll.content == null) return;
            foreach (Transform row in scroll.content) row.localScale = Vector3.one;
        }
    }
}
