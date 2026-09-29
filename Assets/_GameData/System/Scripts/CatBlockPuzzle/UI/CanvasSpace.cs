using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Conversions between screen space and the gameplay canvas / root rect.</summary>
    internal sealed class CanvasSpace
    {
        private readonly IGameplayContext context;
        private readonly Vector3[] rectWorldCorners = new Vector3[4];

        public CanvasSpace(IGameplayContext context)
        {
            this.context = context;
        }

        public Canvas Canvas => context.Hud != null ? context.Hud.canvas : null;
        public RectTransform Root => context.Hud != null ? context.Hud.root : null;
        public Camera Camera => Canvas != null ? Canvas.worldCamera : null;
        public float ScaleFactor => Canvas != null ? Mathf.Max(0.25f, Canvas.scaleFactor) : 1f;

        public Vector2 WorldToScreen(Vector3 world)
        {
            return RectTransformUtility.WorldToScreenPoint(Camera, world);
        }

        public Vector2 ScreenToRootLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, Camera, out Vector2 local);
            return local;
        }

        public Vector2 RootLocalToScreen(Vector2 rootLocal)
        {
            return RectTransformUtility.WorldToScreenPoint(Camera, Root.TransformPoint(rootLocal));
        }

        public Vector2 ScreenToLocal(RectTransform rect, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, Camera, out Vector2 local);
            return local;
        }

        public Rect ExpandedScreenRect(RectTransform rect, float padding)
        {
            rect.GetWorldCorners(rectWorldCorners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(Camera, rectWorldCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(Camera, rectWorldCorners[2]);
            return Rect.MinMaxRect(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
        }

        public static bool RectsOverlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
        }
    }
}
