using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Uniformly shrinks a fixed-size popup so it always fits inside its parent with a margin.
    /// Tall/narrow phones (20:9, fold cover screens) have a canvas narrower than the 1080 reference,
    /// which would otherwise clip authored panels at the sides. Never scales above maxScale.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FitWithinParent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float horizontalMargin = 40f;
        [SerializeField, Min(0f)] private float verticalMargin = 120f;
        [SerializeField, Min(0.1f)] private float maxScale = 1f;

        private Vector2 fittedParentSize = new Vector2(-1f, -1f);

        public float AppliedScale => transform.localScale.x;

        private void OnEnable() => Fit();

        private void Update()
        {
            var parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.size != fittedParentSize) Fit();
        }

        public void Fit()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null || rect.rect.width <= 0f || rect.rect.height <= 0f) return;
            fittedParentSize = parent.rect.size;
            float scale = Mathf.Min(maxScale,
                Mathf.Max(0f, parent.rect.width - horizontalMargin * 2f) / rect.rect.width,
                Mathf.Max(0f, parent.rect.height - verticalMargin * 2f) / rect.rect.height);
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
