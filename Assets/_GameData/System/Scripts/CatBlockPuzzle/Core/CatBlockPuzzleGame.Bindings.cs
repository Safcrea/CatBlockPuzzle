using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        [Header("Focused Dependencies")]
        [SerializeField] private GameplayHudView gameplayHud;
        [SerializeField] private GameplayPresentationAssets presentationAssets;
        [SerializeField] private MetaProgressionView metaView;
        [SerializeField] private LevelCompleteScreen levelCompleteScreen;
        [SerializeField] private LevelFailScreen levelFailScreen;

        private bool IsResultScreenOpen =>
            (levelCompleteScreen != null && levelCompleteScreen.IsOpen) ||
            (levelFailScreen != null && levelFailScreen.IsOpen);

        private Canvas canvas { get => gameplayHud != null ? gameplayHud.canvas : null; set => EnsureHud().canvas = value; }
        private RectTransform root { get => gameplayHud != null ? gameplayHud.root : null; set => EnsureHud().root = value; }
        private RectTransform fxLayer { get => gameplayHud != null ? gameplayHud.fxLayer : null; set => EnsureHud().fxLayer = value; }
        private Text levelText { get => gameplayHud != null ? gameplayHud.levelText : null; set => EnsureHud().levelText = value; }
        private RectTransform timerPanel { get => gameplayHud != null ? gameplayHud.timerPanel : null; set => EnsureHud().timerPanel = value; }
        private Text timerText { get => gameplayHud != null ? gameplayHud.timerText : null; set => EnsureHud().timerText = value; }
        private Text objectiveText { get => gameplayHud != null ? gameplayHud.objectiveText : null; set => EnsureHud().objectiveText = value; }
        private RectTransform objectivePanel { get => gameplayHud != null ? gameplayHud.objectivePanel : null; set => EnsureHud().objectivePanel = value; }
        private Text coinText { get => gameplayHud != null ? gameplayHud.coinText : null; set => EnsureHud().coinText = value; }
        private Text comboText { get => gameplayHud != null ? gameplayHud.comboText : null; set => EnsureHud().comboText = value; }
        private RectTransform starPanel { get => gameplayHud != null ? gameplayHud.starPanel : null; set => EnsureHud().starPanel = value; }
        private RectTransform comboBadge { get => gameplayHud != null ? gameplayHud.comboBadge : null; set => EnsureHud().comboBadge = value; }
        private RectTransform actionBar { get => gameplayHud != null ? gameplayHud.actionBar : null; set => EnsureHud().actionBar = value; }
        private Image[] progressStars { get => gameplayHud != null ? gameplayHud.progressStars : null; set => EnsureHud().progressStars = value; }
        private Button previousTestButton { get => gameplayHud != null ? gameplayHud.previousTestButton : null; set => EnsureHud().previousTestButton = value; }
        private Button nextTestButton { get => gameplayHud != null ? gameplayHud.nextTestButton : null; set => EnsureHud().nextTestButton = value; }
        private Camera sceneCamera { get => gameplayHud != null ? gameplayHud.sceneCamera : null; set => EnsureHud().sceneCamera = value; }
        private EventSystem sceneEventSystem { get => gameplayHud != null ? gameplayHud.sceneEventSystem : null; set => EnsureHud().sceneEventSystem = value; }
        private RectTransform gameplayScreen { get => gameplayHud != null ? gameplayHud.gameplayScreen : null; set => EnsureHud().gameplayScreen = value; }
        private RectTransform levelRoot { get => gameplayHud != null ? gameplayHud.levelRoot : null; set => EnsureHud().levelRoot = value; }
        private Image[] preparedEffects { get => gameplayHud != null ? gameplayHud.preparedEffects : null; set => EnsureHud().preparedEffects = value; }
        private Image backgroundCrossfade { get => gameplayHud != null ? gameplayHud.backgroundCrossfade : null; set => EnsureHud().backgroundCrossfade = value; }
        private Image objectiveImage { get => gameplayHud != null ? gameplayHud.objectiveImage : null; set => EnsureHud().objectiveImage = value; }
        private Sprite defaultBackgroundSprite { get => gameplayHud != null ? gameplayHud.defaultBackgroundSprite : null; set => EnsureHud().defaultBackgroundSprite = value; }
        private Image backgroundImage { get => gameplayHud != null ? gameplayHud.backgroundImage : null; set => EnsureHud().backgroundImage = value; }

        private Font defaultFont { get => presentationAssets != null ? presentationAssets.defaultFont : null; set => EnsurePresentation().defaultFont = value; }
        private Sprite whiteSprite { get => presentationAssets != null ? presentationAssets.whiteSprite : null; set => EnsurePresentation().whiteSprite = value; }
        private Sprite roundedBoxSprite { get => presentationAssets != null ? presentationAssets.roundedBoxSprite : null; set => EnsurePresentation().roundedBoxSprite = value; }
        private Sprite circleSprite { get => presentationAssets != null ? presentationAssets.circleSprite : null; set => EnsurePresentation().circleSprite = value; }
        private Sprite coinSprite { get => presentationAssets != null ? presentationAssets.coinSprite : null; set => EnsurePresentation().coinSprite = value; }
        private Sprite catHeadSprite { get => presentationAssets != null ? presentationAssets.catHeadSprite : null; set => EnsurePresentation().catHeadSprite = value; }
        private Sprite mouthSprite { get => presentationAssets != null ? presentationAssets.mouthSprite : null; set => EnsurePresentation().mouthSprite = value; }
        private Sprite tailSprite { get => presentationAssets != null ? presentationAssets.tailSprite : null; set => EnsurePresentation().tailSprite = value; }
        private Sprite pawSprite { get => presentationAssets != null ? presentationAssets.pawSprite : null; set => EnsurePresentation().pawSprite = value; }
        private Sprite starSprite { get => presentationAssets != null ? presentationAssets.starSprite : null; set => EnsurePresentation().starSprite = value; }
        private Sprite starOutlineSprite { get => presentationAssets != null ? presentationAssets.starOutlineSprite : null; set => EnsurePresentation().starOutlineSprite = value; }
        private Sprite backIconSprite { get => presentationAssets != null ? presentationAssets.backIconSprite : null; set => EnsurePresentation().backIconSprite = value; }
        private Sprite pauseIconSprite { get => presentationAssets != null ? presentationAssets.pauseIconSprite : null; set => EnsurePresentation().pauseIconSprite = value; }
        private Sprite settingsIconSprite { get => presentationAssets != null ? presentationAssets.settingsIconSprite : null; set => EnsurePresentation().settingsIconSprite = value; }
        private Sprite hintIconSprite { get => presentationAssets != null ? presentationAssets.hintIconSprite : null; set => EnsurePresentation().hintIconSprite = value; }
        private Sprite resetIconSprite { get => presentationAssets != null ? presentationAssets.resetIconSprite : null; set => EnsurePresentation().resetIconSprite = value; }
        private Sprite closeIconSprite { get => presentationAssets != null ? presentationAssets.closeIconSprite : null; set => EnsurePresentation().closeIconSprite = value; }
        private Sprite[] catPortraitSprites { get => presentationAssets != null ? presentationAssets.catPortraitSprites : null; set => EnsurePresentation().catPortraitSprites = value; }
        private Sprite[] themeBackgroundSprites { get => presentationAssets != null ? presentationAssets.themeBackgroundSprites : null; set => EnsurePresentation().themeBackgroundSprites = value; }
        private CatVisualCatalog visualCatalog { get => presentationAssets != null ? presentationAssets.visualCatalog : null; set => EnsurePresentation().visualCatalog = value; }
        private PortraitLayoutProfile layoutProfile { get => presentationAssets != null ? presentationAssets.layoutProfile : null; set => EnsurePresentation().layoutProfile = value; }

        private RectTransform metaOverlay { get => metaView != null ? metaView.metaOverlay : null; set => EnsureMeta().metaOverlay = value; }
        private RectTransform metaHubPage { get => metaView != null ? metaView.metaHubPage : null; set => EnsureMeta().metaHubPage = value; }
        private RectTransform metaRoomPage { get => metaView != null ? metaView.metaRoomPage : null; set => EnsureMeta().metaRoomPage = value; }
        private RectTransform metaStoryOverlay { get => metaView != null ? metaView.metaStoryOverlay : null; set => EnsureMeta().metaStoryOverlay = value; }
        private RectTransform metaCompletionOverlay { get => metaView != null ? metaView.metaCompletionOverlay : null; set => EnsureMeta().metaCompletionOverlay = value; }
        private Text metaStatusText { get => metaView != null ? metaView.metaStatusText : null; set => EnsureMeta().metaStatusText = value; }
        private Button metaNextLevelButton { get => metaView != null ? metaView.metaNextLevelButton : null; set => EnsureMeta().metaNextLevelButton = value; }
        private Text metaNextLevelButtonText { get => metaView != null ? metaView.metaNextLevelButtonText : null; set => EnsureMeta().metaNextLevelButtonText = value; }
        private Button metaDecorateNowButton { get => metaView != null ? metaView.metaDecorateNowButton : null; set => EnsureMeta().metaDecorateNowButton = value; }
        private HubCardView[] hubCards { get => metaView != null ? metaView.hubCards : null; set => EnsureMeta().hubCards = value; }
        private CatPuzzleRoomView[] roomViews { get => metaView != null ? metaView.roomViews : null; set => EnsureMeta().roomViews = value; }
        private Text hubContinueLabel { get => metaView != null ? metaView.hubContinueLabel : null; set => EnsureMeta().hubContinueLabel = value; }
        private Text hubStatus { get => metaView != null ? metaView.hubStatus : null; set => EnsureMeta().hubStatus = value; }
        private Text[] metaCoinLabels { get => metaView != null ? metaView.metaCoinLabels : null; set => EnsureMeta().metaCoinLabels = value; }
        private RectTransform storyPanel { get => metaView != null ? metaView.storyPanel : null; set => EnsureMeta().storyPanel = value; }
        private Image storyPortrait { get => metaView != null ? metaView.storyPortrait : null; set => EnsureMeta().storyPortrait = value; }
        private Text storyTitle { get => metaView != null ? metaView.storyTitle : null; set => EnsureMeta().storyTitle = value; }
        private Text storyBody { get => metaView != null ? metaView.storyBody : null; set => EnsureMeta().storyBody = value; }
        private Text storyActionLabel { get => metaView != null ? metaView.storyActionLabel : null; set => EnsureMeta().storyActionLabel = value; }
        private RectTransform completionPanel { get => metaView != null ? metaView.completionPanel : null; set => EnsureMeta().completionPanel = value; }
        private Text completionKicker { get => metaView != null ? metaView.completionKicker : null; set => EnsureMeta().completionKicker = value; }
        private Text completionTitle { get => metaView != null ? metaView.completionTitle : null; set => EnsureMeta().completionTitle = value; }
        private Image completionPortrait { get => metaView != null ? metaView.completionPortrait : null; set => EnsureMeta().completionPortrait = value; }
        private Text completionBody { get => metaView != null ? metaView.completionBody : null; set => EnsureMeta().completionBody = value; }
        private RectTransform completionNextPreview { get => metaView != null ? metaView.completionNextPreview : null; set => EnsureMeta().completionNextPreview = value; }
        private Image completionNextPortrait { get => metaView != null ? metaView.completionNextPortrait : null; set => EnsureMeta().completionNextPortrait = value; }
        private Text completionNextText { get => metaView != null ? metaView.completionNextText : null; set => EnsureMeta().completionNextText = value; }
        private Text completionActionLabel { get => metaView != null ? metaView.completionActionLabel : null; set => EnsureMeta().completionActionLabel = value; }

        private RectTransform winOverlay { get => levelCompleteScreen != null ? levelCompleteScreen.RootRect : null; set => EnsureComplete(value).root = value != null ? value.gameObject : null; }
        private RectTransform winPanel { get => levelCompleteScreen != null ? levelCompleteScreen.panel : null; set => EnsureComplete(value).panel = value; }
        private RectTransform failOverlay { get => levelFailScreen != null ? levelFailScreen.RootRect : null; set => EnsureFail(value).root = value != null ? value.gameObject : null; }
        private RectTransform failPanel { get => levelFailScreen != null ? levelFailScreen.panel : null; set => EnsureFail(value).panel = value; }

        private GameplayHudView EnsureHud()
        {
            if (gameplayHud == null) gameplayHud = GetComponentInChildren<GameplayHudView>(true);
            if (gameplayHud == null) gameplayHud = gameObject.AddComponent<GameplayHudView>();
            return gameplayHud;
        }

        private GameplayPresentationAssets EnsurePresentation()
        {
            if (presentationAssets == null) presentationAssets = GetComponent<GameplayPresentationAssets>();
            if (presentationAssets == null) presentationAssets = gameObject.AddComponent<GameplayPresentationAssets>();
            return presentationAssets;
        }

        private MetaProgressionView EnsureMeta()
        {
            if (metaView == null) metaView = GetComponentInChildren<MetaProgressionView>(true);
            if (metaView == null) metaView = gameObject.AddComponent<MetaProgressionView>();
            return metaView;
        }

        private LevelCompleteScreen EnsureComplete(Component owner)
        {
            if (levelCompleteScreen == null && owner != null) levelCompleteScreen = owner.GetComponentInParent<LevelCompleteScreen>(true);
            if (levelCompleteScreen == null) levelCompleteScreen = FindFirstObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
            if (levelCompleteScreen == null) levelCompleteScreen = owner != null
                ? owner.gameObject.AddComponent<LevelCompleteScreen>()
                : gameObject.AddComponent<LevelCompleteScreen>();
            return levelCompleteScreen;
        }

        private LevelFailScreen EnsureFail(Component owner)
        {
            if (levelFailScreen == null && owner != null) levelFailScreen = owner.GetComponentInParent<LevelFailScreen>(true);
            if (levelFailScreen == null) levelFailScreen = FindFirstObjectByType<LevelFailScreen>(FindObjectsInactive.Include);
            if (levelFailScreen == null) levelFailScreen = owner != null
                ? owner.gameObject.AddComponent<LevelFailScreen>()
                : gameObject.AddComponent<LevelFailScreen>();
            return levelFailScreen;
        }

    }
}
