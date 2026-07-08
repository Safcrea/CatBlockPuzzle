using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.KawaiiUI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    public sealed class KawaiiUIButton : MonoBehaviour
    {
        [SerializeField] private Text iconText;
        [SerializeField] private Text labelText;
        [SerializeField] private Image badgeBackground;
        [SerializeField] private Text badgeText;

        private Image background;
        private Button button;

        public Button Button
        {
            get
            {
                EnsureVisual();
                return button;
            }
        }

        private void Awake()
        {
            EnsureVisual();
        }

        public void Apply(string icon, string label, bool showBadge, int badgeCount)
        {
            EnsureVisual();
            iconText.text = icon;
            labelText.text = label;
            SetBadge(showBadge, badgeCount);
        }

        public void SetBadge(bool visible, int count)
        {
            EnsureVisual();
            badgeBackground.gameObject.SetActive(visible);
            badgeText.gameObject.SetActive(visible);
            badgeText.text = count.ToString();
        }

        public void EnsureVisual()
        {
            button = GetComponent<Button>();
            background = KawaiiUIRuntime.EnsureImage(gameObject);
            background.sprite = KawaiiSprites.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = KawaiiPalette.WhitePanel;
            background.raycastTarget = true;
            KawaiiUIRuntime.EnsureShadow(background, new Vector2(0f, -7f), 0.16f);
            KawaiiUIRuntime.EnsureOutline(background, KawaiiPalette.WithAlpha(KawaiiPalette.SoftBeige, 0.72f), new Vector2(2f, -2f));

            if (iconText == null)
            {
                iconText = FindOrCreateText("Icon");
            }

            if (labelText == null)
            {
                labelText = FindOrCreateText("Label");
            }

            if (badgeBackground == null)
            {
                badgeBackground = FindOrCreateImage("Badge");
            }

            if (badgeText == null)
            {
                badgeText = FindOrCreateText("Badge Text");
                badgeText.transform.SetParent(badgeBackground.transform, false);
            }

            ConfigureText(iconText, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureText(labelText, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureText(badgeText, 20, FontStyle.Bold, TextAnchor.MiddleCenter);

            badgeBackground.sprite = KawaiiSprites.Circle;
            badgeBackground.color = KawaiiPalette.BoardTileGreen;
            badgeBackground.raycastTarget = false;

            KawaiiUIRuntime.SetRect(iconText.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            KawaiiUIRuntime.SetRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            KawaiiUIRuntime.SetRect(badgeBackground.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -8f), new Vector2(38f, 38f));
            KawaiiUIRuntime.Stretch(badgeText.rectTransform);
        }

        private Text FindOrCreateText(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform), typeof(Text));
            if (existing == null)
            {
                go.transform.SetParent(transform, false);
            }

            return go.GetComponent<Text>();
        }

        private Image FindOrCreateImage(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform), typeof(Image));
            if (existing == null)
            {
                go.transform.SetParent(transform, false);
            }

            return go.GetComponent<Image>();
        }

        private static void ConfigureText(Text text, int size, FontStyle style, TextAnchor anchor)
        {
            text.font = KawaiiUIRuntime.DefaultFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = KawaiiPalette.TextDarkBrown;
            text.raycastTarget = false;
        }
    }
}
