using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.KawaiiUI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CatTile : MonoBehaviour
    {
        [SerializeField] private Color tileColor = Color.white;
        [SerializeField] private float visualSize = 96f;

        private RectTransform rectTransform;
        private Image body;
        private Image tail;
        private Image leftEar;
        private Image rightEar;
        private Image leftInnerEar;
        private Image rightInnerEar;
        private Image highlight;
        private Image leftEye;
        private Image rightEye;
        private Image nose;
        private Image leftCheek;
        private Image rightCheek;
        private Image whiskerLeftTop;
        private Image whiskerLeftBottom;
        private Image whiskerRightTop;
        private Image whiskerRightBottom;
        private Text mouth;

        private void Awake()
        {
            Apply(tileColor, visualSize);
        }

        public void Apply(Color color, float size)
        {
            tileColor = color;
            visualSize = size;
            EnsureVisuals();
            LayoutVisuals();
        }

        private void EnsureVisuals()
        {
            rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(visualSize, visualSize);

            tail = EnsureImage("Tail");
            body = EnsureImage("Body");
            leftEar = EnsureImage("Left Ear");
            rightEar = EnsureImage("Right Ear");
            leftInnerEar = EnsureImage("Left Inner Ear");
            rightInnerEar = EnsureImage("Right Inner Ear");
            highlight = EnsureImage("Highlight");
            leftEye = EnsureImage("Left Eye");
            rightEye = EnsureImage("Right Eye");
            nose = EnsureImage("Nose");
            leftCheek = EnsureImage("Left Cheek");
            rightCheek = EnsureImage("Right Cheek");
            whiskerLeftTop = EnsureImage("Left Whisker Top");
            whiskerLeftBottom = EnsureImage("Left Whisker Bottom");
            whiskerRightTop = EnsureImage("Right Whisker Top");
            whiskerRightBottom = EnsureImage("Right Whisker Bottom");
            mouth = EnsureText("Mouth");

            body.sprite = KawaiiSprites.RoundedRect;
            body.type = Image.Type.Sliced;
            body.color = tileColor;
            body.raycastTarget = false;
            KawaiiUIRuntime.EnsureShadow(body, new Vector2(0f, -5f), 0.16f);

            tail.sprite = KawaiiSprites.RoundedRect;
            tail.type = Image.Type.Sliced;
            tail.color = Color.Lerp(tileColor, Color.black, 0.08f);
            tail.raycastTarget = false;

            leftEar.sprite = KawaiiSprites.Triangle;
            rightEar.sprite = KawaiiSprites.Triangle;
            leftEar.color = tileColor;
            rightEar.color = tileColor;
            leftEar.raycastTarget = false;
            rightEar.raycastTarget = false;

            leftInnerEar.sprite = KawaiiSprites.Triangle;
            rightInnerEar.sprite = KawaiiSprites.Triangle;
            leftInnerEar.color = KawaiiPalette.WithAlpha(KawaiiPalette.PinkPiece, 0.65f);
            rightInnerEar.color = KawaiiPalette.WithAlpha(KawaiiPalette.PinkPiece, 0.65f);
            leftInnerEar.raycastTarget = false;
            rightInnerEar.raycastTarget = false;

            highlight.sprite = KawaiiSprites.RoundedRect;
            highlight.type = Image.Type.Sliced;
            highlight.color = new Color(1f, 1f, 1f, 0.16f);
            highlight.raycastTarget = false;

            ApplyCircle(leftEye, KawaiiPalette.TextDarkBrown);
            ApplyCircle(rightEye, KawaiiPalette.TextDarkBrown);
            ApplyCircle(nose, KawaiiPalette.WithAlpha(KawaiiPalette.TextDarkBrown, 0.92f));
            ApplyCircle(leftCheek, KawaiiPalette.WithAlpha(KawaiiPalette.PinkPiece, 0.32f));
            ApplyCircle(rightCheek, KawaiiPalette.WithAlpha(KawaiiPalette.PinkPiece, 0.32f));

            ApplyWhisker(whiskerLeftTop);
            ApplyWhisker(whiskerLeftBottom);
            ApplyWhisker(whiskerRightTop);
            ApplyWhisker(whiskerRightBottom);

            mouth.font = KawaiiUIRuntime.DefaultFont;
            mouth.text = "w";
            mouth.fontStyle = FontStyle.Bold;
            mouth.alignment = TextAnchor.MiddleCenter;
            mouth.color = KawaiiPalette.WithAlpha(KawaiiPalette.TextDarkBrown, 0.82f);
            mouth.raycastTarget = false;
        }

        private void LayoutVisuals()
        {
            float size = Mathf.Max(32f, visualSize);
            Set(body.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(size * 0.9f, size * 0.78f), Vector2.zero);
            Set(tail.rectTransform, new Vector2(0.98f, 0.42f), new Vector2(size * 0.24f, size * 0.16f), new Vector2(size * 0.02f, 0f));
            tail.rectTransform.localEulerAngles = new Vector3(0f, 0f, -18f);

            Set(leftEar.rectTransform, new Vector2(0.28f, 0.88f), new Vector2(size * 0.28f, size * 0.26f), Vector2.zero);
            Set(rightEar.rectTransform, new Vector2(0.72f, 0.88f), new Vector2(size * 0.28f, size * 0.26f), Vector2.zero);
            Set(leftInnerEar.rectTransform, new Vector2(0.28f, 0.875f), new Vector2(size * 0.14f, size * 0.14f), Vector2.zero);
            Set(rightInnerEar.rectTransform, new Vector2(0.72f, 0.875f), new Vector2(size * 0.14f, size * 0.14f), Vector2.zero);

            Set(highlight.rectTransform, new Vector2(0.42f, 0.64f), new Vector2(size * 0.48f, size * 0.18f), Vector2.zero);

            Set(leftEye.rectTransform, new Vector2(0.39f, 0.49f), new Vector2(size * 0.07f, size * 0.09f), Vector2.zero);
            Set(rightEye.rectTransform, new Vector2(0.61f, 0.49f), new Vector2(size * 0.07f, size * 0.09f), Vector2.zero);
            Set(nose.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(size * 0.06f, size * 0.045f), Vector2.zero);
            Set(mouth.rectTransform, new Vector2(0.5f, 0.35f), new Vector2(size * 0.22f, size * 0.14f), Vector2.zero);
            mouth.fontSize = Mathf.RoundToInt(size * 0.18f);

            Set(leftCheek.rectTransform, new Vector2(0.29f, 0.38f), new Vector2(size * 0.12f, size * 0.08f), Vector2.zero);
            Set(rightCheek.rectTransform, new Vector2(0.71f, 0.38f), new Vector2(size * 0.12f, size * 0.08f), Vector2.zero);

            LayoutWhisker(whiskerLeftTop.rectTransform, new Vector2(0.26f, 0.43f), -10f, size);
            LayoutWhisker(whiskerLeftBottom.rectTransform, new Vector2(0.27f, 0.36f), 10f, size);
            LayoutWhisker(whiskerRightTop.rectTransform, new Vector2(0.74f, 0.43f), 10f, size);
            LayoutWhisker(whiskerRightBottom.rectTransform, new Vector2(0.73f, 0.36f), -10f, size);
        }

        private Image EnsureImage(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform), typeof(Image));
            if (existing == null)
            {
                go.transform.SetParent(transform, false);
            }

            return go.GetComponent<Image>();
        }

        private Text EnsureText(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform), typeof(Text));
            if (existing == null)
            {
                go.transform.SetParent(transform, false);
            }

            return go.GetComponent<Text>();
        }

        private static void ApplyCircle(Image image, Color color)
        {
            image.sprite = KawaiiSprites.Circle;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void ApplyWhisker(Image image)
        {
            image.sprite = KawaiiSprites.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = KawaiiPalette.WithAlpha(KawaiiPalette.TextDarkBrown, 0.42f);
            image.raycastTarget = false;
        }

        private static void LayoutWhisker(RectTransform rect, Vector2 anchor, float angle, float size)
        {
            Set(rect, anchor, new Vector2(size * 0.14f, Mathf.Max(2f, size * 0.018f)), Vector2.zero);
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private static void Set(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }
    }
}
