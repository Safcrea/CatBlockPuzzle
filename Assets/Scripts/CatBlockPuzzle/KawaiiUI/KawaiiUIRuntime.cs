using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.KawaiiUI
{
    public static class KawaiiUIRuntime
    {
        public static Font DefaultFont
        {
            get
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return font;
            }
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        public static Image EnsureImage(GameObject gameObject)
        {
            Image image = gameObject.GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            return image;
        }

        public static Shadow EnsureShadow(Graphic graphic, Vector2 distance, float alpha)
        {
            Shadow shadow = graphic.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = graphic.gameObject.AddComponent<Shadow>();
            }

            shadow.effectDistance = distance;
            shadow.effectColor = new Color(0.22f, 0.16f, 0.1f, alpha);
            shadow.useGraphicAlpha = true;
            return shadow;
        }

        public static Outline EnsureOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.GetComponent<Outline>();
            if (outline == null)
            {
                outline = graphic.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
            return outline;
        }

        public static Text CreateText(Transform parent, string name, string text, int size, FontStyle style, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }
    }
}
