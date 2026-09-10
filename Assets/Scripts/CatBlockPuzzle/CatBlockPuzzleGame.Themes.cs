using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private const int ThemeAtlasColumns = 3;
        private const int ThemeAtlasRows = 3;
        private const float ThemeAtlasInset = 0.75f;

        private readonly Sprite[] themeBackgroundSprites = new Sprite[CatPuzzleThemeCatalog.ThemeCount];
        private Image backgroundImage;
        private Image headerBandImage;
        private CatPuzzleTheme activeTheme;
        private int activeThemeIndex = -1;
        private Color activeTrayColor = TrayColor;
        private Color activeTrayHoverColor = TrayHoverColor;

        private void ApplyLevelTheme(int zeroBasedLevelIndex)
        {
            activeTheme = CatPuzzleThemeCatalog.GetThemeForLevel(zeroBasedLevelIndex);
            activeThemeIndex = activeTheme.Index;
            activeTrayColor = activeTheme.TrayColor;
            activeTrayHoverColor = activeTheme.TrayHoverColor;

            Sprite themeSprite = GetThemeBackgroundSprite(activeTheme);
            if (backgroundImage != null && themeSprite != null)
            {
                backgroundImage.sprite = themeSprite;
                backgroundImage.color = Color.white;
            }

            if (headerBandImage != null)
            {
                headerBandImage.color = activeTheme.HeaderColor;
            }

            if (objectivePanel != null)
            {
                objectivePanel.GetComponent<Image>().color = activeTheme.ObjectiveColor;
            }

            if (boardBackdrop != null)
            {
                boardBackdrop.GetComponent<Image>().color = activeTheme.BoardColor;
                Outline outline = boardBackdrop.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = activeTheme.BoardOutlineColor;
                }

                SetBoardDecorationColor("Board Ear Left", activeTheme.BoardColor);
                SetBoardDecorationColor("Board Ear Right", activeTheme.BoardColor);
            }

            if (trayImage != null)
            {
                trayImage.color = activeTrayColor;
            }
        }

        private Sprite GetThemeBackgroundSprite(CatPuzzleTheme theme)
        {
            if (theme.Index < 0 || theme.Index >= themeBackgroundSprites.Length)
            {
                return CreateBackgroundSprite();
            }

            Sprite cached = themeBackgroundSprites[theme.Index];
            if (cached != null)
            {
                return cached;
            }

            Texture2D atlas = visualCatalog != null ? visualCatalog.ThemeAtlas : null;
            if (atlas == null)
            {
                return CreateBackgroundSprite();
            }

            float cellWidth = atlas.width / (float)ThemeAtlasColumns;
            float cellHeight = atlas.height / (float)ThemeAtlasRows;
            int column = theme.AtlasIndex % ThemeAtlasColumns;
            int rowFromTop = theme.AtlasIndex / ThemeAtlasColumns;
            Rect atlasRect = new Rect(
                (column * cellWidth) + ThemeAtlasInset,
                atlas.height - ((rowFromTop + 1) * cellHeight) + ThemeAtlasInset,
                cellWidth - (ThemeAtlasInset * 2f),
                cellHeight - (ThemeAtlasInset * 2f));
            Sprite sprite = Sprite.Create(
                atlas,
                atlasRect,
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "Theme Background - " + theme.DisplayName;
            themeBackgroundSprites[theme.Index] = sprite;
            return sprite;
        }

        private void SetBoardDecorationColor(string childName, Color color)
        {
            Transform child = boardBackdrop.Find(childName);
            if (child == null)
            {
                return;
            }

            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private string BuildThemedObjectiveTitle(string levelTitle)
        {
            return activeTheme.DisplayName + " - " + levelTitle;
        }
    }
}
