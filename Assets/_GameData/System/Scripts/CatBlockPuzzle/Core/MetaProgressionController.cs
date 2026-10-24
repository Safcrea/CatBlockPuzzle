using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The room rescue meta game: chapter/room progression, story and completion cards,
    /// decoration purchase and placement, and the pause/resume handshake with the puzzle
    /// while the room overlay is open. Renders into the baked MetaProgressionView.
    /// </summary>
    internal sealed class MetaProgressionController : HostedService
    {
        private const string MetaTelemetryPrefix = "[CatMetaTelemetry] ";
        /// <summary>Share of a level's reward paid again when an already-cleared level is cleared.</summary>
        private const float ReplayRewardShare = 0.5f;
        private const int MinimumReplayReward = 5;

        private readonly PuzzleSession session;
        private readonly PlayerProgress progress;
        private readonly PlayerSettings settings;
        private readonly LevelTimer timer;
        private readonly HintPresenter hint;
        private readonly PieceDragController drag;
        private readonly TrayLayout tray;
        private readonly FxSpawner fx;
        private readonly CatMoodPresenter mood;
        private readonly HapticsController haptics;
        private readonly InteractionGates gates;
        private readonly ILevelFlow flow;

        private CatMetaCatalog metaCatalog;
        internal CatMetaProgressStore metaProgress;
        private bool metaSystemsUnavailable;
        private bool metaOverlayOwnsPause;
        private bool metaTimerWasRunning;
        private bool metaRequiresRoomCompletion;
        private int metaCurrentChapterIndex;
        private int metaLastAwardedCoins;
        private string metaSelectedDecorationId;
        private UnityAction pendingStoryAction;
        private int completionChapterIndex;
        private Coroutine backgroundFadeRoutine;

        public MetaProgressionController(IGameplayContext context, PuzzleSession session, PlayerProgress progress, PlayerSettings settings,
            LevelTimer timer, HintPresenter hint, PieceDragController drag, TrayLayout tray, FxSpawner fx, CatMoodPresenter mood,
            HapticsController haptics, InteractionGates gates, ILevelFlow flow) : base(context)
        {
            this.session = session;
            this.progress = progress;
            this.settings = settings;
            this.timer = timer;
            this.hint = hint;
            this.drag = drag;
            this.tray = tray;
            this.fx = fx;
            this.mood = mood;
            this.haptics = haptics;
            this.gates = gates;
            this.flow = flow;
        }

        public CatMetaCatalog Catalog => metaCatalog;
        public CatMetaProgressStore Progress => metaProgress;
        public bool HasSystems => metaCatalog != null && metaProgress != null;
        public bool IsMetaUiOpen => gates.MetaUiOpen;
        public int CurrentChapterIndex => metaCurrentChapterIndex;

        public string SelectedDecorationId
        {
            get => metaSelectedDecorationId;
            set => metaSelectedDecorationId = value;
        }

        private MetaProgressionView View => Context.MetaView;
        private CatPuzzleContentCatalog contentCatalog => Context.Catalog;
        private LevelManager levelManager => Context.Levels;
        private int levelIndex => session.levelIndex;
        private LevelDefinition activeLevel => session.activeLevel;
        private bool levelNavigationTesting => session.levelNavigationTesting;
        private bool reducedMotion => settings.ReducedMotion;
        private int coins { get => progress.Coins; set => progress.Coins = value; }
        private Text coinText => Hud.coinText;
        private RectTransform metaOverlay => View != null ? View.metaOverlay : null;
        private RectTransform metaHubPage => View.metaHubPage;
        private RectTransform metaRoomPage => View.metaRoomPage;
        private RectTransform metaStoryOverlay => View.metaStoryOverlay;
        private RectTransform metaCompletionOverlay => View.metaCompletionOverlay;
        private Text metaStatusText { get => View != null ? View.metaStatusText : null; set => View.metaStatusText = value; }
        private Button metaDecorateNowButton => View != null ? View.metaDecorateNowButton : null;
        private Text metaNextLevelButtonText => View != null ? View.metaNextLevelButtonText : null;
        private HubCardView[] hubCards => View.hubCards;
        private CatPuzzleRoomView[] roomViews => View.roomViews;
        private Text hubContinueLabel => View.hubContinueLabel;
        private Text hubStatus => View.hubStatus;
        private Text[] metaCoinLabels => View != null ? View.metaCoinLabels : null;

        /// <summary>Editor authoring works against a throwaway catalog/save rather than the player's.</summary>
        public void UseAuthoringData(CatMetaCatalog catalog, CatMetaProgressStore store)
        {
            metaCatalog = catalog;
            metaProgress = store;
            metaSystemsUnavailable = false;
        }

        public static bool ValidatePreparedMeta(MetaProgressionView view, out string error)
        {
            error = null;
            if (view == null) { error = "the Meta Progression View is missing"; return false; }
            if (view.hubCards == null || view.hubCards.Length != 10 || view.roomViews == null || view.roomViews.Length != 10 ||
                view.hubContinueLabel == null || view.hubStatus == null || view.metaCoinLabels == null || view.metaCoinLabels.Length != 11 ||
                view.storyPanel == null || view.storyPortrait == null || view.storyTitle == null || view.storyBody == null || view.storyActionLabel == null ||
                view.completionPanel == null || view.completionKicker == null || view.completionTitle == null || view.completionPortrait == null ||
                view.completionBody == null || view.completionNextPreview == null || view.completionNextPortrait == null ||
                view.completionNextText == null || view.completionActionLabel == null)
            { error = "prepared room or modal references are incomplete"; return false; }
            foreach (var coin in view.metaCoinLabels) if (coin == null) { error = "a coin label is missing"; return false; }
            for (int i = 0; i < 10; i++)
            {
                var hub = view.hubCards[i]; var room = view.roomViews[i];
                if (hub == null || hub.background == null || hub.thumbnail == null || hub.title == null ||
                    hub.number == null || hub.progress == null || hub.status == null || hub.badge == null || hub.steps.Length != 10 ||
                    room == null || room.chapterIndex != i || room.background == null || room.background.sprite == null ||
                    room.progress == null || room.status == null || room.levelMarkers.Length != 10 || room.cards.Length != 5 || room.placements.Length != 5)
                { error = "room " + (i + 1) + " is incomplete"; return false; }
                foreach (var card in room.cards) if (card == null || card.background == null || card.art == null || card.art.sprite == null || card.status == null)
                { error = "a decoration card is incomplete in room " + (i + 1); return false; }
                foreach (var target in room.placements) if (target == null || target.root == null || target.background == null || target.art == null || target.button == null || target.outline == null || target.prompt == null)
                { error = "a decoration target is incomplete in room " + (i + 1); return false; }
            }
            return true;
        }

        /// <summary>Loads and migrates the meta catalog/save without creating UI.</summary>
        public void InitializeMetaSystems()
        {
            if (metaCatalog != null || metaSystemsUnavailable)
            {
                return;
            }

            try
            {
                int savedLevel = PlayerPrefs.GetInt(PuzzleSaveKeys.SavedLevel, 0);
                int savedCoins = PlayerPrefs.GetInt(PuzzleSaveKeys.SavedCoins, Mathf.Max(0, coins));
                metaCatalog = contentCatalog.CreateMetaCatalog();
                metaProgress = CatMetaProgressStore.Load(metaCatalog, savedLevel, savedCoins);
                metaProgress.UseStarSource(BestStarsForLevel);
                metaProgress.UseUnlockAllSource(() => GameSystem.Instance != null && GameSystem.Instance.UnlockAllLevels);
                coins = metaProgress.CoinBalance;
                if (coinText != null)
                {
                    coinText.text = coins.ToString();
                }

                LogMetaEvent("systems_initialized",
                    "catalogVersion=" + CatMetaCatalog.CurrentVersion
                    + " highestChapter=" + metaProgress.HighestUnlockedChapter);
            }
            catch (Exception exception)
            {
                metaSystemsUnavailable = true;
                metaCatalog = null;
                metaProgress = null;
                Debug.LogError("Cat meta systems were disabled: " + exception);
            }
        }

        /// <summary>Writes the wallet into the gameplay HUD and every room coin badge.</summary>
        public void RefreshCoinLabels()
        {
            if (coinText != null) coinText.text = coins.ToString();
            if (metaCoinLabels != null)
                foreach (var label in metaCoinLabels) if (label != null) label.text = coins.ToString();
        }

        public Sprite PreparedDecorationSprite(string id)
        {
            foreach (var item in contentCatalog.decorations) if (item.id == id) return item.sprite;
            return null;
        }

        /// <summary>Shows the ten-room map and its dominant continuation action.</summary>
        public void OpenRoomHub()
        {
            if (metaCatalog == null || metaProgress == null || metaOverlay == null)
            {
                return;
            }

            EnterMetaOverlay();
            metaCurrentChapterIndex = Mathf.Clamp(
                metaProgress.HighestUnlockedChapter,
                0,
                metaCatalog.Chapters.Count - 1);
            metaSelectedDecorationId = null;
            metaRoomPage.gameObject.SetActive(false);
            metaStoryOverlay.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaHubPage.gameObject.SetActive(true);
            RenderRoomHub();
            LogMetaEvent("hub_open", "recommendedChapter=" + metaCurrentChapterIndex);
        }

        /// <summary>Shows one furnished room, its paw track, and five item cards.</summary>
        public void OpenRoomDetail(int chapterIndex)
        {
            if (metaCatalog == null || metaProgress == null || metaOverlay == null)
            {
                return;
            }

            if (chapterIndex < 0
                || chapterIndex >= metaCatalog.Chapters.Count
                || !metaProgress.IsChapterUnlocked(chapterIndex))
            {
                ShowMetaStatus("Finish the previous room to unlock this chapter.");
                LogMetaEvent("room_open_blocked", "chapter=" + chapterIndex);
                return;
            }

            EnterMetaOverlay();
            metaCurrentChapterIndex = chapterIndex;
            metaHubPage.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(true);
            SelectFirstStoredDecoration(chapterIndex);
            RenderRoomDetail(chapterIndex);

            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            LogMetaEvent("room_open",
                "chapter=" + chapterIndex
                + " installed=" + metaProgress.InstalledDecorationCount(chapterIndex));

            string introId = CatMetaStoryIds.Intro(chapter.Id);
            if (!metaProgress.HasViewedStory(introId))
            {
                ShowChapterIntro(chapterIndex, null);
            }
        }

        /// <summary>Called from the hub's room cards: locked rooms explain themselves instead of opening.</summary>
        public void OpenRoomFromHub(int chapterIndex)
        {
            if (metaProgress.IsChapterUnlocked(chapterIndex)) OpenRoomDetail(chapterIndex);
            else
            {
                haptics?.PlayWrongMove();
                ShowMetaStatus("Earn " + metaProgress.StarsRequiredForChapter(chapterIndex) + " stars to unlock this room.");
            }
        }

        /// <summary>The hub's big button: furnish the finished room, or continue the next level.</summary>
        public void ContinueFromHub()
        {
            int next = GetRecommendedLevelIndex(); var chapter = metaCatalog.GetChapterForLevel(next);
            if (AreAllChapterLevelsCleared(chapter) && !metaProgress.IsChapterComplete(chapter.Index)) OpenRoomDetail(chapter.Index);
            else PlayMetaLevel(next);
        }

        public void PlayLevelFromRoom(int index)
        {
            if (IsMetaLevelSelectable(index)) PlayMetaLevel(index); else ShowMetaStatus("Clear the previous paw step first.");
        }

        public void DecorateNow()
        {
            Context.CompleteScreen?.Hide();
            OpenRoomDetail(metaCatalog.GetChapterForLevel(levelIndex).Index);
        }

        public void ContinueStory()
        {
            metaStoryOverlay.gameObject.SetActive(false);
            var pending = pendingStoryAction; pendingStoryAction = null; pending?.Invoke();
        }

        public void ContinueAfterCompletion()
        {
            metaCompletionOverlay.gameObject.SetActive(false);
            if (completionChapterIndex == roomViews.Length - 1) OpenRoomHub();
            else { int following = completionChapterIndex + 1; ShowChapterIntro(following, () => PlayMetaLevel(metaCatalog.GetChapter(following).FirstLevelIndex)); }
        }

        /// <summary>Dismisses meta UI and safely resumes the interrupted puzzle.</summary>
        public void CloseMetaToGameplay()
        {
            if (gates.HasReferenceUi) { GameSystem.Instance.GoHome(); return; }
            if (metaRequiresRoomCompletion
                && metaProgress != null
                && metaCatalog != null
                && !metaProgress.IsChapterComplete(metaCurrentChapterIndex))
            {
                OpenRoomDetail(metaCurrentChapterIndex);
                ShowMetaStatus("Place all five decorations to open the next room.");
                return;
            }

            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            bool resumeTimer = metaOverlayOwnsPause && metaTimerWasRunning;
            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;

            if (session.levelFailed || gates.ResultScreenOpen || gates.SettingsOpen)
            {
                return;
            }

            session.inputLocked = false;
            tray.SetScrollEnabled(true);
            if (resumeTimer
                || (!timer.timerRunning && !flow.IsRevealing && activeLevel != null))
            {
                timer.StartLevelTimer();
            }
        }

        /// <summary>Hides the overlay and forgets any pause it owned, e.g. before a level preview or skip.</summary>
        public void ResetOverlayState()
        {
            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;
            metaRequiresRoomCompletion = false;
        }

        /// <summary>Applies the active chapter's full-screen authored background.</summary>
        public void ApplyMetaLevelPresentation(int zeroBasedLevelIndex)
        {
            if (metaCatalog == null)
            {
                InitializeMetaSystems();
            }

            if (metaCatalog == null
                || zeroBasedLevelIndex < 0
                || zeroBasedLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(zeroBasedLevelIndex);
            Sprite roomSprite = contentCatalog.roomBackgrounds[chapter.Index];
            Image backgroundImage = Hud.backgroundImage;
            if (backgroundImage != null && roomSprite != null && backgroundImage.sprite != roomSprite)
            {
                CrossfadeGameplayBackground(roomSprite);
            }

            Text objectiveText = Hud.objectiveText;
            if (objectiveText != null && activeLevel != null)
            {
                objectiveText.text = chapter.Title + "  •  " + activeLevel.Title;
            }
        }

        /// <summary>
        /// Loads the requested puzzle behind either the first-run rescue cards or the
        /// returning-player hub. Editor previews intentionally bypass meta UI.
        /// </summary>
        public void ShowMetaStartup(int requestedLevelIndex, bool isEditorPreview)
        {
            InitializeMetaSystems();
            int levelCount = levelManager != null ? levelManager.LevelCount : 0;
            if (levelCount <= 0)
            {
                return;
            }

            int safeLevelIndex = Mathf.Clamp(requestedLevelIndex, 0, levelCount - 1);
            flow.LoadLevel(safeLevelIndex, !isEditorPreview);
            ApplyMetaLevelPresentation(safeLevelIndex);

            if (isEditorPreview || metaProgress == null || metaCatalog == null)
            {
                CloseMetaToGameplay();
                return;
            }

            CatMetaChapterDefinition firstChapter = metaCatalog.GetChapter(0);
            string firstIntroId = CatMetaStoryIds.Intro(firstChapter.Id);
            if (!metaProgress.HasViewedStory(firstIntroId))
            {
                ShowFirstRunStory();
            }
            else
            {
                OpenRoomHub();
            }
        }

        /// <summary>Records an idempotent first clear and returns the actual coin delta.</summary>
        public int RecordMetaFirstClear(int reward)
        {
            if (levelNavigationTesting)
            {
                metaLastAwardedCoins = 0;
                return 0;
            }

            InitializeMetaSystems();
            if (metaProgress == null)
            {
                metaLastAwardedCoins = 0;
                return 0;
            }

            int balance = coins;
            int authored = levelManager != null ? levelManager.LevelCount : 0;
            int levelNumber = session.levelNumber;
            EndlessLevels.RecordWin(levelNumber, authored);
            CatMetaOperationResult result;
            string earnSource;
            if (EndlessLevels.IsLoopLevel(levelNumber, authored))
            {
                // A loop level is a new level to the player: full coins the first time its number is won.
                bool fullReward = EndlessLevels.TryClaimFullReward(levelNumber);
                earnSource = fullReward ? GameAnalytics.EarnLoopLevel : GameAnalytics.EarnReplay;
                result = metaProgress.AwardCoins(fullReward ? reward : ReplayReward(reward), ref balance);
            }
            else
            {
                earnSource = GameAnalytics.EarnFirstClear;
                result = metaProgress.RecordFirstClear(levelIndex, reward, ref balance);
            }
            if (result.Status == CatMetaOperationStatus.NoChange && !EndlessLevels.IsLoopLevel(levelNumber, authored))
            {
                // Replays still pay, at a reduced rate so farming an easy level stays modest.
                earnSource = GameAnalytics.EarnReplay;
                result = metaProgress.RecordReplayClear(levelIndex, ReplayReward(reward), ref balance);
            }
            coins = balance;
            metaLastAwardedCoins = Mathf.Max(0, result.CoinDelta);
            GameAnalytics.CurrencyEarned(metaLastAwardedCoins, earnSource, balance);
            if (coinText != null)
            {
                coinText.text = coins.ToString();
            }

            LogMetaEvent("level_first_clear",
                "level=" + levelIndex
                + " rewardRequested=" + reward
                + " coinDelta=" + result.CoinDelta
                + " status=" + result.Status);
            return metaLastAwardedCoins;
        }

        /// <summary>Overlays decoration/trust milestones on the existing win panel.</summary>
        public void ConfigureMetaWinPresentation(int awardedCoins)
        {
            metaLastAwardedCoins = Mathf.Max(0, awardedCoins);
            if (metaCatalog == null || metaProgress == null)
            {
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(levelIndex);
            int chapterLevel = (levelIndex - chapter.FirstLevelIndex) + 1;
            // Decoration is not part of the current result flow. Keep its button hidden
            // and preserve the authored, full-width Continue artwork below.
            if (metaDecorateNowButton != null)
            {
                metaDecorateNowButton.gameObject.SetActive(false);
            }

            if (metaNextLevelButtonText != null)
            {
                metaNextLevelButtonText.text = chapterLevel == CatMetaCatalog.LevelsPerChapter
                    && !metaProgress.IsChapterComplete(chapter.Index)
                    ? "Decorate Room"
                    : "Next Level";
            }
        }

        /// <summary>Advances directly through gameplay; room furnishing is optional UI.</summary>
        public void LoadNextLevelThroughMetaGate()
        {
            // The Daily Puzzle is a one-off: Next goes back home instead of browsing on.
            if (session.dailyPuzzle)
            {
                GameSystem.Instance?.GoHome();
                return;
            }
            if (levelNavigationTesting)
            {
                if (flow.CanGoNextLevel) flow.LoadNextLevel();
                else GameSystem.Instance?.GoHome();
                return;
            }
            if (metaCatalog == null || metaProgress == null)
            {
                flow.LoadNextLevelDirect();
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(levelIndex);
            int chapterLevel = (levelIndex - chapter.FirstLevelIndex) + 1;
            Context.CompleteScreen?.Hide();

            ContinueAfterMetaWin(chapter, chapterLevel);
        }

        private void ContinueAfterMetaWin(CatMetaChapterDefinition chapter, int chapterLevel)
        {
            // Past the last authored level the puzzles loop and the next one is always open.
            if (levelManager != null && session.levelNumber >= levelManager.LevelCount && EndlessLevels.IsActive)
            {
                PlayLevelNumber(EndlessLevels.NextLoopLevel);
                return;
            }

            if (chapterLevel >= CatMetaCatalog.LevelsPerChapter)
            {
                if (chapter.Index >= metaCatalog.Chapters.Count - 1)
                {
                    GameSystem.Instance?.GoHome();
                    return;
                }

                if (!metaProgress.IsChapterUnlocked(chapter.Index + 1))
                {
                    // The next chapter still needs stars; send the player to pick a level to improve.
                    GameAnalytics.ChapterGateBlocked(chapter.Index + 1, metaProgress.StarsEarnedBeforeChapter(chapter.Index + 1),
                        metaProgress.StarsRequiredForChapter(chapter.Index + 1));
                    GameSystem.Instance?.ShowLevelSelection();
                    return;
                }

                PlayMetaLevel(metaCatalog.GetChapter(chapter.Index + 1).FirstLevelIndex);
                return;
            }

            PlayMetaLevel(levelIndex + 1);
        }

        public void OnDecorationCardPressed(string decorationId)
        {
            CatMetaDecorationDefinition decoration = metaCatalog.GetDecoration(decorationId);
            if (!metaProgress.IsDecorationUnlocked(decorationId))
            {
                ShowMetaStatus("Clear level " + decoration.UnlockAfterChapterLevel + " in this room first.");
                LogMetaEvent("decoration_locked", "id=" + decorationId);
                return;
            }

            if (!metaProgress.IsOwned(decorationId))
            {
                int balance = coins;
                CatMetaOperationResult purchase = metaProgress.TryPurchase(decorationId, ref balance);
                coins = balance;
                LogMetaEvent("decoration_purchase",
                    "id=" + decorationId
                    + " cost=" + decoration.Cost
                    + " status=" + purchase.Status
                    + " coins=" + purchase.CoinBalance);
                if (!purchase.Changed)
                {
                    ShowMetaStatus(purchase.Status == CatMetaOperationStatus.InsufficientCoins
                        ? "You need " + (decoration.Cost - coins) + " more coins."
                        : "That treasure is not available yet.");
                    return;
                }

                coins = purchase.CoinBalance;
                if (coinText != null)
                {
                    coinText.text = coins.ToString();
                }

                metaSelectedDecorationId = decorationId;
                RenderRoomDetail(decoration.ChapterIndex);
                ShowMetaStatus("Purchased! Tap the glowing spot in the room to place it.");
                return;
            }

            if (metaProgress.IsInstalled(decorationId))
            {
                CatMetaOperationResult stored = metaProgress.Store(decorationId);
                LogMetaEvent("decoration_store", "id=" + decorationId + " status=" + stored.Status);
                if (stored.Changed)
                {
                    metaSelectedDecorationId = decorationId;
                    RenderRoomDetail(decoration.ChapterIndex);
                    ShowMetaStatus("Stored. Tap its glowing spot whenever you want it back.");
                }

                return;
            }

            metaSelectedDecorationId = decorationId;
            RenderRoomDetail(decoration.ChapterIndex);
            ShowMetaStatus("Now tap the glowing place in the room.");
        }

        public void InstallMetaDecoration(string decorationId)
        {
            CatMetaDecorationDefinition decoration = metaCatalog.GetDecoration(decorationId);
            CatMetaOperationResult installed = metaProgress.TryInstall(decorationId);
            LogMetaEvent("decoration_install",
                "id=" + decorationId
                + " status=" + installed.Status
                + " chapterComplete=" + installed.ChapterJustCompleted);
            if (!installed.Changed)
            {
                ShowMetaStatus("That treasure could not be placed.");
                return;
            }

            metaSelectedDecorationId = null;
            PuzzleAudio.Play("Snap");
            SelectFirstStoredDecoration(decoration.ChapterIndex);
            RenderRoomDetail(decoration.ChapterIndex);
            ShowMetaStatus(decoration.DisplayName + " placed • "
                + metaProgress.InstalledDecorationCount(decoration.ChapterIndex) + "/5");

            if (installed.ChapterJustCompleted)
            {
                metaRequiresRoomCompletion = false;
                ShowChapterCompletion(decoration.ChapterIndex);
            }
        }

        private void ShowFirstRunStory()
        {
            EnterMetaOverlay();
            metaHubPage.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(false);
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(0);
            ShowStoryCard(
                "A HOME FOR EVERY CAT",
                "Solve cozy block puzzles to earn coins. Use each victory to turn an empty room into a safe home for a rescued cat.",
                "Meet " + chapter.CatName,
                () =>
                {
                    MarkMetaStoryViewed(CatMetaStoryIds.Intro(chapter.Id), chapter.Index, "intro");
                    ShowStoryCard(
                        "MEET " + chapter.CatName.ToUpperInvariant(),
                        chapter.StartStory + "\n\nYour first puzzle can show " + chapter.CatName + " that this home is kind.",
                        "Start Level 1",
                        CloseMetaToGameplay,
                        mood.Portrait(CatMood.Neutral, chapter.CatPortraitIndex));
                },
                mood.Portrait(CatMood.Happy, chapter.CatPortraitIndex));
        }

        public void ShowChapterIntro(int chapterIndex, UnityAction onDismissed)
        {
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            MarkMetaStoryViewed(CatMetaStoryIds.Intro(chapter.Id), chapter.Index, "intro");
            ShowStoryCard(
                "MEET " + chapter.CatName.ToUpperInvariant(),
                chapter.StartStory + "\n\nComplete this room to help " + chapter.CatName + " feel at home.",
                "Begin the Chapter",
                onDismissed,
                mood.Portrait(CatMood.Neutral, chapter.CatPortraitIndex));
        }

        public void EnterMetaOverlay()
        {
            if (metaOverlay == null)
            {
                return;
            }

            if (!metaOverlayOwnsPause)
            {
                bool revealWasRunning = flow.IsRevealing;
                metaTimerWasRunning = timer.timerRunning || revealWasRunning;
                metaOverlayOwnsPause = true;
                if (revealWasRunning)
                {
                    flow.CancelReveal();
                }
            }

            timer.StopLevelTimer();
            hint.StopHint();
            drag.CancelActiveDragToRest();
            tray.SetScrollEnabled(false);
            session.inputLocked = true;
            GameSystem.Instance?.ShowRoomsPage();
            metaOverlay.gameObject.SetActive(true);
            metaOverlay.SetAsLastSibling();
        }

        public void PlayMetaLevel(int requestedLevelIndex)
        {
            if (levelManager == null || levelManager.LevelCount <= 0)
            {
                return;
            }

            if (requestedLevelIndex < 0 || requestedLevelIndex >= levelManager.LevelCount)
            {
                Debug.LogWarning("Ignoring request for a level outside the loaded catalog: " + requestedLevelIndex, Host);
                return;
            }

            if (metaProgress != null && !IsMetaLevelSelectable(requestedLevelIndex))
            {
                ShowMetaStatus("Clear the previous paw step first.");
                return;
            }

            StartMetaLevel(requestedLevelIndex, requestedLevelIndex + 1);
        }

        /// <summary>Plays a 1-based level number; numbers past the authored levels are loop levels and always open.</summary>
        public void PlayLevelNumber(int levelNumber)
        {
            if (levelManager == null || levelManager.LevelCount <= 0) return;
            if (!EndlessLevels.IsLoopLevel(levelNumber, levelManager.LevelCount)) { PlayMetaLevel(Mathf.Max(1, levelNumber) - 1); return; }
            StartMetaLevel(EndlessLevels.PuzzleIndexFor(levelNumber, levelManager.LevelCount), levelNumber);
        }

        private void StartMetaLevel(int safeLevel, int levelNumber)
        {

            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            Context.CompleteScreen?.Hide();

            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;
            metaRequiresRoomCompletion = false;
            GameSystem.Instance?.ShowGameplayPage();
            LogMetaEvent("level_start", "level=" + safeLevel + " chapter=" + (safeLevel / CatMetaCatalog.LevelsPerChapter));
            flow.LoadLevelNumber(levelNumber, true);
            ApplyMetaLevelPresentation(safeLevel);
        }

        public bool IsMetaLevelSelectable(int absoluteLevelIndex)
        {
            if (metaCatalog == null || metaProgress == null
                || absoluteLevelIndex < 0
                || levelManager == null
                || absoluteLevelIndex >= levelManager.LevelCount
                || absoluteLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return false;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(absoluteLevelIndex);
            if (!metaProgress.IsChapterUnlocked(chapter.Index)) return false;
            if (metaProgress.UnlocksEverything) return true;
            return absoluteLevelIndex == chapter.FirstLevelIndex
                || metaProgress.IsLevelPassed(absoluteLevelIndex)
                || metaProgress.IsLevelPassed(absoluteLevelIndex - 1);
        }

        public int GetRecommendedLevelIndex()
        {
            if (EndlessLevels.IsActive && levelManager != null && levelManager.LevelCount > 0)
                return EndlessLevels.PuzzleIndexFor(EndlessLevels.NextLoopLevel, levelManager.LevelCount);
            // The real next level, even while the testing switch has every chapter open.
            int chapterIndex = Mathf.Clamp(metaProgress.HighestEarnedChapter, 0, metaCatalog.Chapters.Count - 1);
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            for (int level = chapter.FirstLevelIndex; level <= chapter.LastLevelIndex; level++)
            {
                if (!metaProgress.IsLevelPassed(level))
                {
                    return level;
                }
            }

            // Everything open is cleared: point at the first level still short of three stars,
            // since those stars are what unlocks the next chapter.
            for (int level = 0; level <= chapter.LastLevelIndex; level++)
            {
                if (BestStarsForLevel(level) < CatPuzzleResultCalculator.MaximumStars)
                {
                    return level;
                }
            }

            return chapter.LastLevelIndex;
        }

        public bool IsChapterUnlocked(int chapterIndex) => metaProgress != null && metaProgress.IsChapterUnlocked(chapterIndex);
        public int StarsRequiredForChapter(int chapterIndex) => metaProgress != null ? metaProgress.StarsRequiredForChapter(chapterIndex) : 0;
        public int StarsEarnedBeforeChapter(int chapterIndex) => metaProgress != null ? metaProgress.StarsEarnedBeforeChapter(chapterIndex) : 0;

        private int BestStarsForLevel(int absoluteLevelIndex) => levelManager != null && absoluteLevelIndex >= 0 && absoluteLevelIndex < levelManager.LevelCount
            ? progress.GetBestStars(levelManager.GetLevel(absoluteLevelIndex).Id)
            : 0;

        private static int ReplayReward(int reward) => reward <= 0 ? 0 : Mathf.Max(MinimumReplayReward, Mathf.RoundToInt(reward * ReplayRewardShare));

        public bool AreAllChapterLevelsCleared(CatMetaChapterDefinition chapter)
        {
            for (int level = chapter.FirstLevelIndex; level <= chapter.LastLevelIndex; level++)
            {
                if (!metaProgress.IsLevelFirstCleared(level))
                {
                    return false;
                }
            }

            return true;
        }

        private void SelectFirstStoredDecoration(int chapterIndex)
        {
            if (!string.IsNullOrEmpty(metaSelectedDecorationId)
                && metaCatalog.TryGetDecoration(metaSelectedDecorationId, out CatMetaDecorationDefinition selected)
                && selected.ChapterIndex == chapterIndex
                && metaProgress.IsOwned(selected.Id)
                && !metaProgress.IsInstalled(selected.Id))
            {
                return;
            }

            metaSelectedDecorationId = null;
            IReadOnlyList<CatMetaDecorationDefinition> decorations = metaCatalog.GetChapter(chapterIndex).Decorations;
            for (int i = 0; i < decorations.Count; i++)
            {
                if (metaProgress.IsOwned(decorations[i].Id) && !metaProgress.IsInstalled(decorations[i].Id))
                {
                    metaSelectedDecorationId = decorations[i].Id;
                    return;
                }
            }
        }

        public void MarkMetaStoryViewed(string storyId, int chapterIndex, string storyType)
        {
            if (metaProgress == null)
            {
                return;
            }

            CatMetaOperationResult result = metaProgress.MarkStoryViewed(storyId);
            if (result.Changed)
            {
                LogMetaEvent("story_view", "chapter=" + chapterIndex + " type=" + storyType + " id=" + storyId);
            }
        }

        public void ShowMetaStatus(string message)
        {
            if (metaStatusText != null)
            {
                metaStatusText.text = message;
            }
        }

        public void LogMetaEvent(string eventName, string values)
        {
            Debug.Log(MetaTelemetryPrefix + "event=" + eventName + " " + values);
        }

        private void RenderRoomHub()
        {
            foreach (var room in roomViews) room.gameObject.SetActive(false);
            metaStatusText = hubStatus;
            hubStatus.text = "Choose a room or continue your rescue story.";
            RefreshCoinLabels();
            int activeChapterIndex = metaCatalog.GetChapterForLevel(GetRecommendedLevelIndex()).Index;
            float selectedScale = View != null ? Mathf.Max(1f, View.activeChapterScale) : 1.08f;
            for (int i = 0; i < hubCards.Length; i++)
            {
                var view = hubCards[i]; var chapter = metaCatalog.GetChapter(i);
                bool unlocked = metaProgress.IsChapterUnlocked(i), complete = metaProgress.IsChapterComplete(i);
                bool active = i == activeChapterIndex;
                int installed = metaProgress.InstalledDecorationCount(i);
                // The background is the card root. Scaling it keeps the selected chapter
                // prominent without changing the scroll/grid layout's slot positions.
                view.background.rectTransform.localScale = active ? Vector3.one * selectedScale : Vector3.one;
                CanvasGroup cardGroup = view.background.GetComponent<CanvasGroup>();
                if (cardGroup == null) cardGroup = view.background.gameObject.AddComponent<CanvasGroup>();
                cardGroup.alpha = unlocked ? 1f : 0.52f;
                cardGroup.interactable = true;
                cardGroup.blocksRaycasts = true;
                view.background.color = unlocked ? new Color(1f, .98f, .93f, .99f) : new Color(.82f, .8f, .77f, .98f);
                view.thumbnail.color = unlocked ? Color.white : new Color(.42f, .42f, .42f, .78f);
                view.number.color = complete ? TargetDeepColor : SoftInkColor;
                view.title.text = unlocked ? chapter.Title : "Locked Room";
                view.progress.text = unlocked ? chapter.CatName + "  •  " + installed + "/5 treasures" : "Finish Room " + i;
                view.progress.color = unlocked ? SoftInkColor : new Color(.42f, .4f, .38f, 1f);
                view.status.text = complete ? "COMPLETE" : unlocked ? installed + "/5" : "LOCKED";
                view.badge.color = complete ? TargetDeepColor : unlocked ? new Color(.96f, .56f, .48f, .96f) : new Color(.38f, .36f, .35f, .85f);
                for (int p = 0; p < 10; p++) view.steps[p].color = MetaStepColor(chapter.FirstLevelIndex + p);
            }
            int next = GetRecommendedLevelIndex();
            var recommended = metaCatalog.GetChapterForLevel(next);
            bool decorating = AreAllChapterLevelsCleared(recommended) && !metaProgress.IsChapterComplete(recommended.Index);
            hubContinueLabel.text = decorating ? "Finish " + recommended.Title : "Continue • Level " + (next + 1);
        }

        private Color MetaStepColor(int level) => metaProgress.IsLevelFirstCleared(level) ? GoldColor :
            IsMetaLevelSelectable(level) ? TargetDeepColor : new Color(.54f, .5f, .46f, .32f);

        private void RenderRoomDetail(int chapterIndex)
        {
            for (int i = 0; i < roomViews.Length; i++) roomViews[i].gameObject.SetActive(i == chapterIndex);
            var view = roomViews[chapterIndex]; var chapter = metaCatalog.GetChapter(chapterIndex);
            metaStatusText = view.status;
            view.status.text = "Buy a treasure, then tap its glowing place in the room.";
            view.progress.text = chapter.CatName + "  •  " + metaProgress.InstalledDecorationCount(chapterIndex) + "/5 placed";
            RefreshCoinLabels();
            for (int i = 0; i < 10; i++) view.levelMarkers[i].color = MetaStepColor(chapter.FirstLevelIndex + i);
            foreach (var card in view.cards)
            {
                metaCatalog.TryGetDecoration(card.id, out var decoration);
                bool unlocked = metaProgress.IsDecorationUnlocked(card.id), owned = metaProgress.IsOwned(card.id), installed = metaProgress.IsInstalled(card.id);
                bool selected = owned && !installed && metaSelectedDecorationId == card.id;
                card.background.color = selected ? new Color(1f, .86f, .52f, 1f) : installed ? new Color(.72f, .9f, .75f, .99f) :
                    unlocked ? new Color(1f, .98f, .93f, .99f) : new Color(.78f, .76f, .73f, .96f);
                card.art.color = owned ? Color.white : unlocked ? new Color(.27f, .24f, .22f, .28f) : new Color(.25f, .25f, .25f, .2f);
                card.status.text = !unlocked ? "CLEAR " + decoration.UnlockAfterChapterLevel : !owned ? decoration.Cost + " COINS" : installed ? "STORE" : selected ? "TAP GLOW" : "SELECT";
                card.status.color = !unlocked ? SoftInkColor : !owned ? new Color(.76f, .47f, .08f) : selected ? new Color(.72f, .43f, .05f) : TargetDeepColor;
            }
            foreach (var placement in view.placements)
            {
                bool installed = metaProgress.IsInstalled(placement.id);
                bool selected = metaProgress.IsOwned(placement.id) && !installed && metaSelectedDecorationId == placement.id;
                placement.root.gameObject.SetActive(installed || selected);
                placement.background.color = selected ? new Color(1f, .83f, .3f, .34f) : Color.clear;
                placement.background.raycastTarget = selected;
                placement.art.color = selected ? new Color(1f, 1f, 1f, .58f) : Color.white;
                placement.button.interactable = selected;
                placement.outline.enabled = selected;
                placement.prompt.gameObject.SetActive(selected);
            }
        }

        private void ShowStoryCard(string titleValue, string bodyValue, string actionLabel, UnityAction action, Sprite portraitSprite)
        {
            EnterMetaOverlay();
            View.storyTitle.text = titleValue; View.storyBody.text = bodyValue; View.storyActionLabel.text = actionLabel;
            View.storyPortrait.sprite = portraitSprite != null ? portraitSprite : Assets.catHeadSprite;
            pendingStoryAction = action;
            metaStoryOverlay.gameObject.SetActive(true);
            metaStoryOverlay.SetAsLastSibling();
            View.storyPanel.localScale = reducedMotion ? Vector3.one : Vector3.one * .9f;
            if (!reducedMotion) StartCoroutine(fx.PopTransform(View.storyPanel, 1.025f));
        }

        private void ShowChapterCompletion(int chapterIndex)
        {
            completionChapterIndex = chapterIndex;
            var chapter = metaCatalog.GetChapter(chapterIndex);
            MarkMetaStoryViewed(CatMetaStoryIds.Completion(chapter.Id), chapter.Index, "completion");
            bool final = chapterIndex == roomViews.Length - 1;
            View.completionKicker.text = final ? "FOREVER HOME COMPLETE" : "ROOM COMPLETE";
            View.completionTitle.text = chapter.Title; View.completionBody.text = chapter.CompletionStory;
            View.completionPortrait.sprite = mood.Portrait(CatMood.Happy, chapter.CatPortraitIndex);
            View.completionNextPreview.gameObject.SetActive(!final);
            if (!final)
            {
                var next = metaCatalog.GetChapter(chapterIndex + 1);
                View.completionNextPortrait.sprite = mood.Portrait(CatMood.Neutral, next.CatPortraitIndex);
                View.completionNextText.text = "NEXT: " + next.Title + "\nMeet " + next.CatName;
            }
            View.completionActionLabel.text = final ? "See the Forever Home" : "Meet the Next Cat";
            metaCompletionOverlay.gameObject.SetActive(true);
            metaCompletionOverlay.SetAsLastSibling();
            View.completionPanel.localScale = reducedMotion ? Vector3.one : Vector3.one * .88f;
            if (!reducedMotion) StartCoroutine(fx.PopTransform(View.completionPanel, 1.035f));
            LogMetaEvent("room_complete", "chapter=" + chapterIndex + " id=" + chapter.Id);
        }

        private void CrossfadeGameplayBackground(Sprite nextSprite)
        {
            if (nextSprite == null) return;
            Image backgroundImage = Hud.backgroundImage;
            Image backgroundCrossfade = Hud.backgroundCrossfade;
            if (backgroundFadeRoutine != null) StopCoroutine(backgroundFadeRoutine);
            var previous = backgroundImage.sprite;
            Color tint = Hud.BackgroundTint;
            backgroundImage.sprite = nextSprite; backgroundImage.color = tint;
            backgroundCrossfade.gameObject.SetActive(false);
            if (reducedMotion || previous == null || previous == nextSprite) return;
            backgroundCrossfade.sprite = previous; backgroundCrossfade.color = tint;
            backgroundCrossfade.gameObject.SetActive(true);
            backgroundFadeRoutine = StartCoroutine(FadePreparedBackground());
        }

        private IEnumerator FadePreparedBackground()
        {
            Image backgroundCrossfade = Hud.backgroundCrossfade;
            Color tint = Hud.BackgroundTint;
            float elapsed = 0;
            while (elapsed < .35f)
            {
                elapsed += Time.unscaledDeltaTime;
                backgroundCrossfade.color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - Mathf.Clamp01(elapsed / .35f)));
                yield return null;
            }
            backgroundCrossfade.gameObject.SetActive(false);
            backgroundFadeRoutine = null;
        }
    }
}
