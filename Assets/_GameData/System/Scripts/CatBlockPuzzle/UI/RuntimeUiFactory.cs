using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    internal static class RuntimeUiFactory
    {
        internal static readonly Color Cream = new Color(1f, 0.965f, 0.89f, 0.99f);
        internal static readonly Color Coral = new Color(0.95f, 0.42f, 0.4f, 1f);
        internal static readonly Color Ink = new Color(0.31f, 0.19f, 0.17f, 1f);

        public static RectTransform CreateOverlay(Transform parent, string name)
        {
            RectTransform root = CreateRect(parent, name);
            PrepareOverlay(root);
            return root;
        }

        public static void PrepareOverlay(RectTransform root)
        {
            Stretch(root);
            Image shade = root.GetComponent<Image>();
            if (shade == null) shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0.16f, 0.1f, 0.08f, 0.5f);
            root.SetAsLastSibling();
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
        {
            RectTransform panel = CreateRect(parent, name);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = size;
            Image image = panel.gameObject.AddComponent<Image>();
            image.color = Cream;
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.2f, 0.1f, 0.05f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -12f);
            return panel;
        }

        public static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor)
        {
            RectTransform rect = CreateRect(parent, name);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Ink;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = size;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
            button.colors = colors;
            Text text = CreateText(rect, "Label", label, 34, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.color = color == Coral ? Color.white : Ink;
            return button;
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        public static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
