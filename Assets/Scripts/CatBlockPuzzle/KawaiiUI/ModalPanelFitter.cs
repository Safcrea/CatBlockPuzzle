using UnityEngine;

namespace CatBlockPuzzle.KawaiiUI
{
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class ModalPanelFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float margin = 24f;

        public void SetPanel(RectTransform target) { panel = target; Apply(); }
        private void OnEnable() { Apply(); }
        private void LateUpdate() { Apply(); }

        private void Apply()
        {
            if (panel == null) return;
            Rect available = ((RectTransform)transform).rect;
            Vector2 size = panel.sizeDelta;
            float scale = Mathf.Min(1f, Mathf.Max(1f, available.width - margin * 2) / Mathf.Max(1f, size.x),
                Mathf.Max(1f, available.height - margin * 2) / Mathf.Max(1f, size.y));
            // Scale the fitting container, leaving the panel's popup animation independent.
            if (!Mathf.Approximately(transform.localScale.x, scale)) transform.localScale = Vector3.one * scale;
        }
    }
}
