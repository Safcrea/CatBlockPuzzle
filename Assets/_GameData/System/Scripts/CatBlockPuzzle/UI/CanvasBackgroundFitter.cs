using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Keeps artwork full bleed while its parent UI respects the safe area.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CanvasBackgroundFitter : MonoBehaviour
    {
        private bool applying;
        private void OnEnable()
        {
            Canvas.willRenderCanvases += Fit;
            Fit();
        }
        private void OnDisable() => Canvas.willRenderCanvases -= Fit;
        private void OnRectTransformDimensionsChange() => Fit();
        private void Fit()
        {
            if (applying || transform.parent == null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            applying = true;
            var canvasRect = (RectTransform)canvas.rootCanvas.transform;
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = canvasRect.rect.size;
            rect.localPosition = transform.parent.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.center));
            applying = false;
        }
    }
}
