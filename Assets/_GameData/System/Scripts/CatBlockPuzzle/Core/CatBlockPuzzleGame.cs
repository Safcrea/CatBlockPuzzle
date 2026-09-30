using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The puzzle game's scene component. It owns the authored references, composes the
    /// gameplay services in <see cref="Compose"/>, drives their per-frame updates, and is the
    /// single entry point the rest of the project (GameSystem, level selection, the baked
    /// UI buttons and piece inputs, and the editor tools) calls into.
    /// </summary>
    public sealed class CatBlockPuzzleGame : MonoBehaviour, IGameplayContext, IPuzzleConfig, ILevelFlow
    {
        [Header("Focused Dependencies")]
        [SerializeField] private GameplayHudView gameplayHud;
        [SerializeField] private GameplayPresentationAssets presentationAssets;
        [SerializeField] private MetaProgressionView metaView;
        [SerializeField] private LevelCompleteScreen levelCompleteScreen;
        [SerializeField] private LevelFailScreen levelFailScreen;

        [Header("Power Ups")]
        [SerializeField, Min(0.1f)] private float freezeDurationSeconds = 15f;

        [Header("Prepared scene — assigned in Edit Mode")]
        [SerializeField] private CatPuzzleContentCatalog contentCatalog;
#if UNITY_EDITOR
        // Migration/authoring only. Cleared after exporting; never included in a player build.
        [SerializeField, HideInInspector] private CatPuzzleLevelView[] preparedLevels;
#endif

        [Header("Level 1 Tutorial")]
        [Tooltip("Turn off to play level 1 without the hands-on tutorial. Can also be changed during Play Mode.")]
        [SerializeField] private bool enableLevelOneTutorial = true;
        [Tooltip("Add short contextual captions alongside the visual demonstrations.")]
        [SerializeField] private bool showTutorialCaptions;
        [Tooltip("Optional artwork for the animated tutorial pointer.")]
        [SerializeField] private Sprite tutorialHandSprite;

        private LevelManager levelManager;
        private HapticsController haptics;
        private bool composed;

        private PuzzleSession session;
        private PlayerSettings settings;
        private PlayerProgress progress;
        private InteractionGates gates;
        private CanvasSpace space;
        private FxSpawner fx;
        private LevelTimer timer;
        private PieceLayout pieceLayout;
        private TrayLayout tray;
        private BoardController board;
        private CatMoodPresenter mood;
        private ComboTracker combo;
        private PlacementFeedback feedback;
        private MovePieceTip tip;
        private HintPresenter hint;
        private ThemePresenter theme;
        private PieceDragController drag;
        private LevelOneTutorial tutorial;
        private PreparedLevelLoader loader;
        private MetaProgressionController meta;
        private PowerUpController powerUps;
        private LevelFlowController flow;

        // ------------------------------------------------------------------ scene bindings

        MonoBehaviour IGameplayContext.Host => this;
        GameplayHudView IGameplayContext.Hud => gameplayHud;
        GameplayPresentationAssets IGameplayContext.Assets => presentationAssets;
        MetaProgressionView IGameplayContext.MetaView => metaView;
        LevelCompleteScreen IGameplayContext.CompleteScreen => levelCompleteScreen;
        LevelFailScreen IGameplayContext.FailScreen => levelFailScreen;
        CatPuzzleContentCatalog IGameplayContext.Catalog => contentCatalog;
        LevelManager IGameplayContext.Levels => levelManager;

        internal GameplayHudView Hud { get => gameplayHud; set => gameplayHud = value; }
        internal GameplayPresentationAssets Assets { get => presentationAssets; set => presentationAssets = value; }
        internal MetaProgressionView MetaView { get => metaView; set => metaView = value; }
        internal LevelCompleteScreen CompleteScreen { get => levelCompleteScreen; set => levelCompleteScreen = value; }
        internal LevelFailScreen FailScreen { get => levelFailScreen; set => levelFailScreen = value; }
        internal CatPuzzleContentCatalog Catalog { get => contentCatalog; set => contentCatalog = value; }
        internal LevelManager Levels { get => levelManager; set => levelManager = value; }
#if UNITY_EDITOR
        internal CatPuzzleLevelView[] PreparedLevels { get => preparedLevels; set => preparedLevels = value; }
#endif

        // ------------------------------------------------------------------ live config

        bool IPuzzleConfig.LevelOneTutorialEnabled { get => enableLevelOneTutorial; set => enableLevelOneTutorial = value; }
        bool IPuzzleConfig.ShowTutorialCaptions => showTutorialCaptions;
        Sprite IPuzzleConfig.TutorialHandSprite => tutorialHandSprite;
        float IPuzzleConfig.FreezeDurationSeconds => freezeDurationSeconds;

        // ------------------------------------------------------------------ level flow for the meta/power-up services

        void ILevelFlow.LoadLevel(int levelIndex, bool persistProgress) => flow.LoadLevel(levelIndex, persistProgress);
        bool ILevelFlow.CanGoNextLevel => flow.CanGoNextLevel;
        void ILevelFlow.LoadNextLevel() => flow.LoadNextLevel();
        void ILevelFlow.LoadNextLevelDirect() => flow.LoadNextLevelDirect();
        bool ILevelFlow.IsRevealing => flow.IsRevealing;
        void ILevelFlow.CancelReveal() => flow.CancelReveal();

        // ------------------------------------------------------------------ services (editor authoring and tests)

        internal PuzzleSession Session => session;
        internal PlayerSettings Settings => settings;
        internal PlayerProgress Progress => progress;
        internal BoardController Board => board;
        internal TrayLayout Tray => tray;
        internal PieceLayout PieceLayout => pieceLayout;
        internal CatMoodPresenter Mood => mood;
        internal MetaProgressionController Meta => meta;
        internal PreparedLevelLoader Loader => loader;
        internal ThemePresenter Theme => theme;
        internal LevelFlowController Flow => flow;
        internal FxSpawner Fx => fx;

        /// <summary>Builds the service graph once. Safe in Edit Mode, where the authoring tools need it.</summary>
        internal void EnsureComposed()
        {
            if (composed) return;
            Compose();
        }

        /// <summary>Discards all runtime state and rebuilds the services; the authoring tools use this between passes.</summary>
        internal void Recompose()
        {
            composed = false;
            Compose();
        }

        private void Compose()
        {
            if (haptics == null) haptics = new HapticsController(this);
            session = new PuzzleSession();
            settings = new PlayerSettings(session, haptics);
            progress = new PlayerProgress(session);
            gates = new InteractionGates(this);
            space = new CanvasSpace(this);
            fx = new FxSpawner(this, space, settings);
            timer = new LevelTimer(this, session, this, gates);
            pieceLayout = new PieceLayout(session);
            tray = new TrayLayout(this, session, pieceLayout, settings, space);
            board = new BoardController(this, session, space, pieceLayout, tray, settings);
            mood = new CatMoodPresenter(this, session, settings, gates);
            combo = new ComboTracker(this, session, fx, space, settings);
            feedback = new PlacementFeedback(this, session, fx, haptics, space, board, mood, combo, settings);
            tip = new MovePieceTip(this, session, space, board, settings);
            hint = new HintPresenter(this, session, board, tip);
            theme = new ThemePresenter(this, session);
            drag = new PieceDragController(this, session, space, board, tray, pieceLayout, feedback, hint, fx, mood, theme, settings, gates);
            tutorial = new LevelOneTutorial(this, session, this, space, board, drag, settings, fx, mood, timer, gates);
            drag.Gate = tutorial;
            loader = new PreparedLevelLoader(this, session, tray, mood, drag, this);
            meta = new MetaProgressionController(this, session, progress, settings, timer, hint, drag, tray, fx, mood, haptics, gates, this);
            powerUps = new PowerUpController(this, session, timer, hint, drag, progress, meta, tutorial, gates, this);
            flow = new LevelFlowController(this, session, loader, board, tray, drag, timer, hint, tip, tutorial, meta, theme,
                progress, combo, fx, haptics, mood, gates, this);
            composed = true;
        }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (!ValidatePreparedScene(out string error))
            {
                Debug.LogError("CatBlockPuzzle scene is not prepared: " + error +
                    ". Use Cat Block Puzzle > Prepare Game Scene in Edit Mode.", this);
                enabled = false;
                return;
            }
            Screen.orientation = ScreenOrientation.Portrait;
            if (!LevelValidator.TryValidatePack(contentCatalog.levelPack, out error))
            {
                Debug.LogError("CatBlockPuzzle content catalog is invalid: " + error, this);
                enabled = false;
                return;
            }
            levelManager = contentCatalog.CreateLevelManager();
            haptics = new HapticsController(this);
            GameSystem.EnsureForScene(this);
            Recompose();
            progress.LoadCoins();
            meta.InitializeMetaSystems();
            settings.Load();
            settings.Apply();
            fx.ResetPool();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (gameplayHud.previousTestButton != null) gameplayHud.previousTestButton.gameObject.SetActive(false);
            if (gameplayHud.nextTestButton != null) gameplayHud.nextTestButton.gameObject.SetActive(false);
#endif
        }

        private IEnumerator Start()
        {
            if (!enabled) yield break;
            yield return null;
            Canvas.ForceUpdateCanvases();
            int requestedLevel = Mathf.Clamp(PlayerPrefs.GetInt(PuzzleSaveKeys.SavedLevel, 0), 0, levelManager.LevelCount - 1);
            bool previewLevel = false;
#if UNITY_EDITOR
            const string previewLevelKey = "CatBlockPuzzle.EditorPreviewLevel";
            if (UnityEditor.EditorPrefs.HasKey(previewLevelKey))
            {
                requestedLevel = Mathf.Clamp(UnityEditor.EditorPrefs.GetInt(previewLevelKey, requestedLevel), 0, levelManager.LevelCount - 1);
                UnityEditor.EditorPrefs.DeleteKey(previewLevelKey);
                previewLevel = true;
            }
#endif
            if (GameSystem.Instance != null && GameSystem.Instance.HasReferenceUi)
            {
                if (previewLevel) { GameSystem.Instance.ShowGameplayPage(); flow.LoadLevel(requestedLevel, false); }
                else GameSystem.Instance.GoHome();
            }
            else meta.ShowMetaStartup(requestedLevel, previewLevel);
        }

        private void Update()
        {
            if (!composed) return;
            tutorial.Update();
            if (GameSystem.Instance != null && GameSystem.Instance.HasReferenceUi && !GameSystem.Instance.IsGameplayOpen) return;
            drag.UpdateMotion();
            timer.Tick(tutorial.IsOpen);
            tray.UpdateIdleMotion();
        }

        // ------------------------------------------------------------------ progress and levels

        public int CurrentLevelNumber => (session != null ? session.levelIndex : 0) + 1;
        public int CurrentCoins => progress != null ? progress.Coins : 0;
        public int LevelCount => levelManager != null ? levelManager.LevelCount : 0;
        public int RecommendedLevelIndex => meta != null && meta.Progress != null ? meta.GetRecommendedLevelIndex() : 0;
        public bool IsLevelAvailable(int index) => levelManager != null && index >= 0 && index < levelManager.LevelCount && meta.IsMetaLevelSelectable(index);
        public int GetLevelStars(int index) => levelManager != null && index >= 0 && index < LevelCount ? progress.GetBestStars(levelManager.GetLevel(index).Id) : 0;
        public string GetChapterTitle(int chapter) => meta.Catalog.GetChapter(chapter).Title;
        public Sprite GetChapterThumbnail(int chapter) => contentCatalog.roomThumbnails[chapter];
        public int CurrentStarCount => flow.CurrentStarCount;
        public CatPuzzleLevelView LoadedLevel => loader != null ? loader.LoadedLevel : null;
        public string[] LevelPrefabResources => contentCatalog.levelPrefabResources;
        public Canvas PreparedCanvas => gameplayHud != null ? gameplayHud.canvas : null;

        public void StartSelectedLevel(int index) => meta.PlayMetaLevel(index);
        public void RestartCurrentLevel() => flow.ResetLevel();
        public void LoadNextLevelFromSystem() => meta.LoadNextLevelThroughMetaGate();
        public void SkipCurrentLevel() => flow.SkipCurrentLevel();
        public void ShowHomeFromSystem() => meta.OpenRoomHub();
        public void SuspendForNavigation() => flow.SuspendForNavigation();
        public void SetSystemPaused(bool paused) => flow.SetSystemPaused(paused);
        public void SetHapticsFromSystem(bool enabled) => settings.SetHaptics(enabled);

        // UnityEvent helpers for freely browsing levels; previews do not award progress.
        public void LoadPreviousLevel() => flow.LoadPreviousLevel();
        public void LoadNextLevel() => flow.LoadNextLevel();
        public void PreviewLevelForTesting(int zeroBasedLevelIndex) => flow.PreviewLevelForTesting(zeroBasedLevelIndex);

        public void AwardExternalCoins(int amount)
        {
            if (amount <= 0) return;
            long updated = (long)progress.Coins + amount;
            progress.Coins = updated > int.MaxValue ? int.MaxValue : (int)updated;
            progress.SaveCoins();
            meta.RefreshCoinLabels();
        }

        // ------------------------------------------------------------------ power-ups

        public bool IsTimerFrozen => timer != null && timer.IsFrozen;
        public float FreezeRemainingSeconds => timer != null ? timer.FreezeRemainingSeconds : 0f;
        public bool IsPowerUpTutorialOpen => gameplayHud != null && gameplayHud.IsTutorialOpen;
        public bool IsPowerUpPurchaseOpen => gameplayHud != null && gameplayHud.IsPurchaseOpen;

        public bool CanRequestPowerUp(PowerUpKind kind) => powerUps.CanRequestPowerUp(kind);
        public bool TryUseFreezePowerUp() => powerUps.TryUseFreezePowerUp();
        public bool TryUseHintPowerUp() => powerUps.TryUseHintPowerUp();
        public int HintCost => powerUps != null ? powerUps.HintCost : 1;
        public bool OpenPowerUpPurchase(PowerUpKind kind) => powerUps.OpenPowerUpPurchase(kind);
        internal void SetPowerUpPurchaseOpen(bool open, bool resumeGameplay = true) => powerUps.SetPowerUpPurchaseOpen(open, resumeGameplay);
        internal PowerUpPurchaseStatus PurchasePowerUp(PowerUpKind kind, PowerUpShopConfig.Offer offer) => powerUps.PurchasePowerUp(kind, offer);
        public void RequestHint() => GameSystem.Instance?.TryUseHintPowerUp();
        public void RefreshPowerUpHud() => gameplayHud?.RefreshPowerUpCounts();

        // ------------------------------------------------------------------ level 1 tutorial

        public bool IsLevelOneTutorialOpen => tutorial != null && tutorial.IsOpen;
        public bool LevelOneTutorialEnabled => enableLevelOneTutorial;
        public void SetLevelOneTutorialEnabled(bool enabled) => tutorial.SetEnabled(enabled);
        public void DisableLevelOneTutorial() => SetLevelOneTutorialEnabled(false);
        public void ReplayLevelOneTutorial() => flow.ReplayLevelOneTutorial();

        // ------------------------------------------------------------------ baked UI callbacks

        public void HandlePreparedGesture(int level, int piece, bool slotProxy,
            CatPuzzlePieceDragView.GestureEvent action, BaseEventData data)
        {
            loader.HandleGesture(level, piece, slotProxy, action, data);
        }

        public void HandlePreparedAction(CatPuzzleUiAction.ActionKind action, int index, string itemId)
        {
            PuzzleAudio.PlayButton();
            switch (action)
            {
                case CatPuzzleUiAction.ActionKind.Rooms: meta.OpenRoomHub(); break;
                case CatPuzzleUiAction.ActionKind.Pause: OpenPause(); break;
                case CatPuzzleUiAction.ActionKind.Settings: OpenSettings(); break;
                case CatPuzzleUiAction.ActionKind.Hint: GameSystem.Instance?.TryUseHintPowerUp(); break;
                case CatPuzzleUiAction.ActionKind.Reset: flow.ResetLevel(); break;
                case CatPuzzleUiAction.ActionKind.PreviousTest: GameSystem.Instance?.LoadPreviousLevel(); break;
                case CatPuzzleUiAction.ActionKind.NextTest:
                    if (levelManager != null && levelManager.LevelCount > 0) GameSystem.Instance?.LoadNextLevel();
                    break;
                case CatPuzzleUiAction.ActionKind.NextLevel: meta.LoadNextLevelThroughMetaGate(); break;
                case CatPuzzleUiAction.ActionKind.Home: meta.OpenRoomHub(); break;
                case CatPuzzleUiAction.ActionKind.SkipLevel: flow.SkipCurrentLevel(); break;
                case CatPuzzleUiAction.ActionKind.CloseSettings: CloseSettings(); break;
                case CatPuzzleUiAction.ActionKind.CloseMeta: meta.CloseMetaToGameplay(); break;
                case CatPuzzleUiAction.ActionKind.ContinueHub: meta.ContinueFromHub(); break;
                case CatPuzzleUiAction.ActionKind.OpenRoom: meta.OpenRoomFromHub(index); break;
                case CatPuzzleUiAction.ActionKind.PlayLevel: meta.PlayLevelFromRoom(index); break;
                case CatPuzzleUiAction.ActionKind.Decoration: meta.OnDecorationCardPressed(itemId); break;
                case CatPuzzleUiAction.ActionKind.Install: meta.InstallMetaDecoration(itemId); break;
                case CatPuzzleUiAction.ActionKind.DecorateNow: meta.DecorateNow(); break;
                case CatPuzzleUiAction.ActionKind.StoryContinue: meta.ContinueStory(); break;
                case CatPuzzleUiAction.ActionKind.CompletionContinue: meta.ContinueAfterCompletion(); break;
            }
        }

        public void HandlePreparedToggle(CatPuzzleUiAction.ActionKind action, bool value)
        {
            if (action == CatPuzzleUiAction.ActionKind.Sound) settings.SetSound(value);
            else if (action == CatPuzzleUiAction.ActionKind.Haptics) settings.SetHaptics(value);
            else if (action == CatPuzzleUiAction.ActionKind.Motion) settings.SetReducedMotion(value);
        }

        internal void OpenPause()
        {
            GameSystem system = GameSystem.EnsureForScene(this);
            if (system != null) system.OpenPause();
        }

        internal void OpenSettings()
        {
            GameSystem system = GameSystem.EnsureForScene(this);
            if (system != null) system.OpenSettings(false);
        }

        internal void CloseSettings()
        {
            GameSystem.Instance?.ResumeGame();
        }

        // ------------------------------------------------------------------ validation

        public bool ValidatePreparedScene(out string error)
        {
            error = null;
            // Reference UI replaces the legacy room-decoration flow. Its unused room
            // bindings must not block the authored main-menu and level-selection flow.
            bool usesReferenceUi = FindFirstObjectByType<ReferenceUiNavigation>(FindObjectsInactive.Include) != null;
            if (contentCatalog == null || contentCatalog.LevelCount != 100)
                error = "the 100-level content catalog is missing";
            else if (gameplayHud == null) error = "the Gameplay HUD View is missing";
            else if (!gameplayHud.Validate(out error)) { }
            else if (presentationAssets == null) error = "the Gameplay Presentation Assets component is missing";
            else if (!presentationAssets.Validate(out error)) { }
            else if (!usesReferenceUi && metaView == null) error = "the Meta Progression View is missing";
            else if (!usesReferenceUi && !metaView.Validate(out error)) { }
            else if (levelCompleteScreen == null || !levelCompleteScreen.IsConfigured ||
                levelFailScreen == null || !levelFailScreen.IsConfigured)
                error = "level result screen controllers are missing";
            else if (contentCatalog.levelPrefabResources == null || contentCatalog.levelPrefabResources.Length != contentCatalog.LevelCount)
                error = "level prefab addresses do not match the catalog; convert or prepare the scene";
            if (error != null) return false;
            foreach (var effect in gameplayHud.preparedEffects) if (effect == null) { error = "an effect slot is missing"; return false; }
            foreach (var address in contentCatalog.levelPrefabResources)
                if (string.IsNullOrWhiteSpace(address)) { error = "a level prefab address is missing"; return false; }
            return usesReferenceUi || MetaProgressionController.ValidatePreparedMeta(metaView, out error);
        }

        public bool ValidateLevelPrefab(CatPuzzleLevelView view, int i, out string error) =>
            PreparedLevelLoader.ValidateLevelPrefab(contentCatalog, view, i, out error);

        // ------------------------------------------------------------------ editor authoring entry points

#if UNITY_EDITOR
        public const string PreparedAssetFolder = CatPuzzleScenePreparer.PreparedAssetFolder;
        public const string LevelPrefabFolder = CatPuzzleLevelPrefabExporter.LevelPrefabFolder;

        public bool ValidatePreparedContentSources(out string error) =>
            CatPuzzleScenePreparer.ValidatePreparedContentSources(contentCatalog, out error);

        public static string PreparationFingerprint() => CatPuzzleScenePreparer.PreparationFingerprint();

        public void PrepareInEditor(Camera camera, Transform uiGroup, Transform controllers) =>
            CatPuzzleScenePreparer.PrepareInEditor(this, camera, uiGroup, controllers);

        public void PreviewPreparedLevelInEditor(int index)
        {
            if (Application.isPlaying) { PreviewLevelForTesting(index); return; }
            CatPuzzleScenePreparer.PreviewPreparedLevelInEditor(this, index);
        }

        public void ExportPreparedLevelsInEditor() => CatPuzzleLevelPrefabExporter.ExportPreparedLevelsInEditor(this);
        public void ClearLevelPreviewInEditor() => CatPuzzleLevelPrefabExporter.ClearLevelPreviewInEditor(this);
#endif
    }
}
