namespace CatBlockPuzzle
{
    /// <summary>Answers "is something modal in the way?" for the timer, input and power-up services.</summary>
    internal sealed class InteractionGates
    {
        private readonly IGameplayContext context;

        public InteractionGates(IGameplayContext context)
        {
            this.context = context;
        }

        public bool LevelCompleteOpen => context.CompleteScreen != null && context.CompleteScreen.IsOpen;

        public bool ResultScreenOpen =>
            LevelCompleteOpen || (context.FailScreen != null && context.FailScreen.IsOpen);

        public bool MetaUiOpen
        {
            get
            {
                MetaProgressionView view = context.MetaView;
                return view != null
                    && view.metaOverlay != null
                    && view.metaOverlay.gameObject.activeInHierarchy
                    && ((view.metaHubPage != null && view.metaHubPage.gameObject.activeSelf)
                        || (view.metaRoomPage != null && view.metaRoomPage.gameObject.activeSelf)
                        || (view.metaStoryOverlay != null && view.metaStoryOverlay.gameObject.activeSelf)
                        || (view.metaCompletionOverlay != null && view.metaCompletionOverlay.gameObject.activeSelf));
            }
        }

        public bool PowerUpTutorialOpen => context.Hud != null && context.Hud.IsTutorialOpen;
        public bool PowerUpPurchaseOpen => context.Hud != null && context.Hud.IsPurchaseOpen;
        public bool SystemPaused => GameSystem.Instance != null && GameSystem.Instance.IsPaused;
        public bool GameplayHidden => GameSystem.Instance != null && !GameSystem.Instance.IsGameplayOpen;
        public bool SettingsOpen => GameSystem.Instance != null && GameSystem.Instance.IsSettingsOpen;
        public bool HasReferenceUi => GameSystem.Instance != null && GameSystem.Instance.HasReferenceUi;
    }
}
