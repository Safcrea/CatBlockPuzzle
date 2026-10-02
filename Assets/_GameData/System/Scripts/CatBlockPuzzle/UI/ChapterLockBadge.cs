using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Covers a locked chapter card's level grid with a padlock and the stars still needed,
    /// e.g. "★ 12 / 30". Built on demand from the card's own art so it matches the authored look.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChapterLockBadge : MonoBehaviour
    {
        private static readonly Color CoverColor = new Color(0.36f, 0.2f, 0.1f, 0.95f);
        private static readonly Color TextColor = new Color(1f, 0.95f, 0.84f, 1f);

        private GameObject cover;
        private Text progressText;
        private Text hintText;
        private Image portrait;
        private Color portraitColor;

        public bool IsShowing => cover != null && cover.activeSelf;
        public string ProgressLabel => progressText != null ? progressText.text : string.Empty;

        /// <summary>Shows or hides the lock. Sprites/font come from the card so no extra assets are needed.</summary>
        public void Show(bool locked, int earned, int required, Sprite lockSprite, Sprite starSprite, Font font)
        {
            if (!locked)
            {
                if (cover != null) cover.SetActive(false);
                if (portrait != null) portrait.color = portraitColor;
                return;
            }

            EnsureBuilt(lockSprite, starSprite, font);
            cover.SetActive(true);
            cover.transform.SetAsLastSibling();
            progressText.text = Mathf.Min(earned, required) + " / " + required;
            hintText.text = required <= CatMetaCatalog.LevelsPerChapter * 3
                ? "Get 3 stars on every level of Chapter 01"
                : "Get 3 stars on every level before this chapter";
            if (portrait != null) portrait.color = new Color(portraitColor.r * 0.45f, portraitColor.g * 0.45f, portraitColor.b * 0.45f, portraitColor.a);
        }

        private void EnsureBuilt(Sprite lockSprite, Sprite starSprite, Font font)
        {
            if (cover != null) return;
            var card = (RectTransform)transform;
            Transform portraitArt = card.Find("Room Portrait/Art");
            if (portraitArt != null && portraitArt.TryGetComponent(out portrait)) portraitColor = portrait.color;

            // Cover the level grid only, so the chapter title and portrait stay readable.
            var grid = card.Find("Levels") as RectTransform;
            var coverRect = NewRect("Chapter Lock", card);
            if (grid != null)
            {
                coverRect.anchorMin = coverRect.anchorMax = grid.anchorMin;
                coverRect.pivot = grid.pivot;
                coverRect.anchoredPosition = grid.anchoredPosition;
                coverRect.sizeDelta = grid.rect.size;
            }
            else Stretch(coverRect);
            cover = coverRect.gameObject;
            var background = cover.AddComponent<Image>();
            Image gridImage = grid != null ? grid.GetComponent<Image>() : null;
            background.sprite = gridImage != null ? gridImage.sprite : null;
            background.type = gridImage != null ? gridImage.type : Image.Type.Simple;
            background.color = CoverColor;
            // Swallow taps so the hidden level buttons underneath stay unreachable; drags still bubble to the scroll view.
            background.raycastTarget = true;

            var padlock = NewRect("Padlock", coverRect).gameObject.AddComponent<Image>();
            padlock.sprite = lockSprite;
            padlock.preserveAspect = true;
            padlock.raycastTarget = false;
            var padlockRect = padlock.rectTransform;
            padlockRect.sizeDelta = new Vector2(78f, 88f);
            padlockRect.anchoredPosition = new Vector2(0f, 76f);

            var star = NewRect("Star", coverRect).gameObject.AddComponent<Image>();
            star.sprite = starSprite;
            star.preserveAspect = true;
            star.raycastTarget = false;
            star.rectTransform.sizeDelta = new Vector2(52f, 50f);
            star.rectTransform.anchoredPosition = new Vector2(-100f, -6f);

            progressText = NewText("Stars Needed", coverRect, font, 46, new Vector2(50f, -8f), new Vector2(220f, 64f));
            progressText.alignment = TextAnchor.MiddleLeft;
            hintText = NewText("Unlock Hint", coverRect, font, 24, new Vector2(0f, -86f), new Vector2(560f, 60f));
            hintText.alignment = TextAnchor.MiddleCenter;
        }

        private static Text NewText(string name, RectTransform parent, Font font, int size, Vector2 position, Vector2 boxSize)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = TextColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.sizeDelta = boxSize;
            text.rectTransform.anchoredPosition = position;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -2f);
            return text;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
