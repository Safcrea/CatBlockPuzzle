using UnityEngine;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>Applies the five-level colour theme to the background, board frame and tray.</summary>
    internal sealed class ThemePresenter
    {
        private readonly IGameplayContext context;
        private readonly PuzzleSession session;

        private CatPuzzleTheme activeTheme;
        internal int activeThemeIndex = -1;
        private Color activeTrayColor = TrayColor;
        private Color activeTrayHoverColor = TrayHoverColor;

        public ThemePresenter(IGameplayContext context, PuzzleSession session)
        {
            this.context = context;
            this.session = session;
        }

        public CatPuzzleTheme ActiveTheme => activeTheme;
        public Color ActiveTrayColor => activeTrayColor;
        public Color ActiveTrayHoverColor => activeTrayHoverColor;

        public void Apply(int zeroBasedLevelIndex, CatPuzzleLevelView loadedLevel)
        {
            GameplayHudView hud = context.Hud;
            activeTheme = CatPuzzleThemeCatalog.GetThemeForLevel(zeroBasedLevelIndex);
            activeThemeIndex = activeTheme.Index;
            activeTrayColor = activeTheme.TrayColor;
            activeTrayHoverColor = activeTheme.TrayHoverColor;
            if (GameUiTheme.Active != null)
            {
                activeTrayColor = Color.white;
                activeTrayHoverColor = new Color(1f, .94f, .8f);
                GameUiTheme.Active.ApplyLevel(loadedLevel);
                return;
            }

            Sprite themeSprite = BackgroundSprite(activeTheme);
            if (hud.backgroundImage != null && themeSprite != null && !hud.UsesAuthoredLayout)
            {
                hud.backgroundImage.sprite = themeSprite;
                hud.backgroundImage.color = hud.BackgroundTint;
            }

            if (hud.objectiveImage != null && !hud.UsesAuthoredLayout)
            {
                hud.objectiveImage.color = activeTheme.ObjectiveColor;
            }

            if (loadedLevel != null)
            {
                loadedLevel.boardFrameImage.color = activeTheme.BoardColor;
                loadedLevel.boardOutline.effectColor = activeTheme.BoardOutlineColor;
                foreach (var ear in loadedLevel.boardEars) ear.color = activeTheme.BoardColor;
            }

            if (session.trayImage != null)
            {
                session.trayImage.color = activeTrayColor;
            }
        }

        public Sprite BackgroundSprite(CatPuzzleTheme theme)
        {
            if (GameUiTheme.Active != null) return GameUiTheme.Active.background;
            Sprite[] themeBackgroundSprites = context.Assets.themeBackgroundSprites;
            return themeBackgroundSprites[Mathf.Clamp(theme.Index, 0, themeBackgroundSprites.Length - 1)];
        }

        public string ObjectiveTitle(string levelTitle)
        {
            return activeTheme.DisplayName + " - " + levelTitle;
        }
    }
}
