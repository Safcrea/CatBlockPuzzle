using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private const int ThemeAtlasColumns = 3;
        private const int ThemeAtlasRows = 3;
        private const float ThemeAtlasInset = 0.75f;

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

            if (objectiveImage != null)
            {
                objectiveImage.color = activeTheme.ObjectiveColor;
            }

            if (preparedLevelIndex >= 0)
            {
                var view = loadedLevel;
                view.boardFrameImage.color = activeTheme.BoardColor;
                view.boardOutline.effectColor = activeTheme.BoardOutlineColor;
                foreach (var ear in view.boardEars) ear.color = activeTheme.BoardColor;
            }

            if (trayImage != null)
            {
                trayImage.color = activeTrayColor;
            }
        }

        private Sprite GetThemeBackgroundSprite(CatPuzzleTheme theme)
        {
            return themeBackgroundSprites[Mathf.Clamp(theme.Index, 0, themeBackgroundSprites.Length - 1)];
        }

        private string BuildThemedObjectiveTitle(string levelTitle)
        {
            return activeTheme.DisplayName + " - " + levelTitle;
        }
    }
}
