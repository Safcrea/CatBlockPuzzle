using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Testing-phase level browser: a slim bar at the top of the gameplay screen that jumps
    /// 10 or 1 levels back and forward without awarding progress. It exists only while the
    /// "Show Test Level Navigation" checkbox on the game component is ticked; with it off,
    /// no bar is created in any build.
    /// </summary>
    internal sealed class TestLevelNavigator : HostedService
    {
        private const string BarName = "Test Level Navigation";
        private static readonly int[] Steps = { -10, -1, 1, 10 };
        private static readonly string[] Labels = { "<<", "<", ">", ">>" };

        private readonly PuzzleSession session;
        private readonly IPuzzleConfig config;
        private readonly Button[] buttons = new Button[Steps.Length];
        private RectTransform bar;
        private int shownLevel = -1;

        public TestLevelNavigator(IGameplayContext context, PuzzleSession session, IPuzzleConfig config) : base(context)
        {
            this.session = session;
            this.config = config;
        }

        public bool Enabled => config.ShowTestLevelNavigation;

        public void Build()
        {
            if (!Enabled || Hud == null || Hud.gameplayScreen == null) return;

            Transform stale = Hud.gameplayScreen.Find(BarName);
            if (stale != null)
            {
                stale.gameObject.SetActive(false);
                stale.SetParent(null, false);
                Destroy(stale.gameObject);
            }

            const float buttonWidth = 72f, buttonHeight = 56f, gap = 8f, pad = 8f;
            float contentWidth = (Steps.Length * buttonWidth) + ((Steps.Length - 1) * gap);
            bar = RuntimeUiFactory.CreateRect(Hud.gameplayScreen, BarName);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(0f, -44f);
            bar.sizeDelta = new Vector2(contentWidth + (pad * 2f), buttonHeight + (pad * 2f));
            Image background = bar.gameObject.AddComponent<Image>();
            background.sprite = Assets.roundedBoxSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.2f, 0.12f, 0.08f, 0.55f);
            background.raycastTarget = false;

            for (int i = 0; i < Steps.Length; i++)
            {
                int step = Steps[i];
                float x = (-contentWidth * 0.5f) + (buttonWidth * 0.5f) + (i * (buttonWidth + gap));
                Button button = RuntimeUiFactory.CreateButton(bar, "Level " + Labels[i], Labels[i], new Vector2(x, 0f),
                    new Vector2(buttonWidth, buttonHeight), new Color(1f, 0.95f, 0.86f, 0.95f));
                button.onClick.AddListener(() => Jump(step));
                buttons[i] = button;
            }

            bar.SetAsLastSibling();
            shownLevel = -1;
            Refresh();
        }

        /// <summary>Greys out the buttons that would run past the first or last level. Cheap enough to call every frame.</summary>
        public void Refresh()
        {
            if (bar == null || shownLevel == session.levelIndex) return;
            shownLevel = session.levelIndex;
            int last = Context.Levels != null ? Context.Levels.LevelCount - 1 : 0;
            buttons[0].interactable = buttons[1].interactable = shownLevel > 0;
            buttons[2].interactable = buttons[3].interactable = shownLevel < last;
        }

        private void Jump(int step)
        {
            int last = Context.Levels != null ? Context.Levels.LevelCount - 1 : 0;
            int target = Mathf.Clamp(session.levelIndex + step, 0, last);
            if (target == session.levelIndex) return;

            ButtonSfx.PlayClick();
            if (GameSystem.Instance != null) GameSystem.Instance.PreviewLevel(target);
            else (Context as ILevelFlow)?.LoadLevel(target, false);
        }
    }
}
