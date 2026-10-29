using System.Collections;
using AVNEvents;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Moves the player between levels and through a level's lifecycle: loading and laying
    /// out a level, the start reveal, wins and failures, restarts, previews, skips, and the
    /// pause/resume hand-offs with the system menus.
    /// </summary>
    internal sealed class LevelFlowController : HostedService
    {
        private readonly PuzzleSession session;
        private readonly PreparedLevelLoader loader;
        private readonly BoardController board;
        private readonly CellLockController locks;
        private readonly MechanicIntroPresenter intro;
        private readonly MistakeHearts hearts;
        private readonly TrayLayout tray;
        private readonly PieceDragController drag;
        private readonly LevelTimer timer;
        private readonly HintPresenter hint;
        private readonly MovePieceTip tip;
        private readonly LevelOneTutorial tutorial;
        private readonly MetaProgressionController meta;
        private readonly ThemePresenter theme;
        private readonly PlayerProgress progress;
        private readonly ComboTracker combo;
        private readonly FxSpawner fx;
        private readonly HapticsController haptics;
        private readonly CatMoodPresenter mood;
        private readonly InteractionGates gates;
        private readonly IPuzzleConfig config;
        private readonly FogController fog;
        private readonly DropCounter drops;
        private readonly GameplayToast toast;
        private readonly RotateController rotate;

        private Coroutine boardRevealRoutine;
        // The AVN attempt opens once the board is playable, not when the level loads.
        private LevelStart? avnPendingStart;
        // The next LoadLevel is the Daily Puzzle (set by PlayDailyPuzzle and by a retry of one).
        private bool loadAsDailyPuzzle;
        private bool timerWasRunningBeforeSettings;

        public LevelFlowController(IGameplayContext context, PuzzleSession session, PreparedLevelLoader loader, BoardController board,
            CellLockController locks, TrayLayout tray, PieceDragController drag, LevelTimer timer, HintPresenter hint, MovePieceTip tip, LevelOneTutorial tutorial,
            MetaProgressionController meta, ThemePresenter theme, PlayerProgress progress, ComboTracker combo, FxSpawner fx,
            HapticsController haptics, CatMoodPresenter mood, InteractionGates gates, IPuzzleConfig config,
            MechanicIntroPresenter intro, MistakeHearts hearts, FogController fog, DropCounter drops, GameplayToast toast, RotateController rotate) : base(context)
        {
            this.rotate = rotate;
            this.fog = fog;
            this.drops = drops;
            this.toast = toast;
            this.hearts = hearts;
            this.intro = intro;
            this.session = session;
            this.loader = loader;
            this.board = board;
            this.locks = locks;
            this.tray = tray;
            this.drag = drag;
            this.timer = timer;
            this.hint = hint;
            this.tip = tip;
            this.tutorial = tutorial;
            this.meta = meta;
            this.theme = theme;
            this.progress = progress;
            this.combo = combo;
            this.fx = fx;
            this.haptics = haptics;
            this.mood = mood;
            this.gates = gates;
            this.config = config;

            drag.PiecePlaced += OnPiecePlaced;
            timer.Expired += FailLevel;
            drag.MissedDrop += _ => hearts.LoseHeart();
            hearts.OutOfHearts += () => FailLevel("Out of hearts! Place each cat carefully.", "out_of_hearts");
        }

        private LevelManager levelManager => Context.Levels;
        private int levelIndex { get => session.levelIndex; set => session.levelIndex = value; }
        private LevelDefinition activeLevel { get => session.activeLevel; set => session.activeLevel = value; }
        private bool inputLocked { get => session.inputLocked; set => session.inputLocked = value; }
        private bool levelNavigationTesting { get => session.levelNavigationTesting; set => session.levelNavigationTesting = value; }

        /// <summary>True while the level-start reveal (and the power-up teaching that follows it) is still running.</summary>
        public bool IsRevealing => boardRevealRoutine != null;

        public bool CanGoPreviousLevel => levelIndex > 0;
        public bool CanGoNextLevel => levelManager != null && levelIndex < levelManager.LevelCount - 1;

        public int CurrentStarCount => session.earnedStars > 0
            ? session.earnedStars
            : timer.Profile.Stars(activeLevel, session.levelNumber, timer.RemainingSeconds, session.drops);

        public void LoadLevelNumber(int levelNumber, bool persistProgress)
        {
            if (levelManager == null || levelManager.LevelCount <= 0) return;
            LoadLevel(EndlessLevels.PuzzleIndexFor(levelNumber, levelManager.LevelCount), persistProgress, Mathf.Max(1, levelNumber));
        }

        /// <param name="levelNumber">The number shown to the player; 0 means the puzzle's own number.</param>
        public void LoadLevel(int nextLevelIndex, bool persistProgress = true, int levelNumber = 0)
        {
            nextLevelIndex = Mathf.Clamp(nextLevelIndex, 0, levelManager.LevelCount - 1);
            // Resolve/validate before disturbing the current level, so a missing prefab is recoverable.
            var nextPrefab = loader.LoadLevelPrefab(nextLevelIndex);
            if (nextPrefab == null) { loadAsDailyPuzzle = false; return; }
            Hud?.CancelPowerUpTutorial();
            Hud?.CancelPowerUpPurchase(false);
            intro.Cancel();
            tutorial.Cancel();
            session.ResetForLevelLoad();
            timer.timerRunning = false;
            levelNavigationTesting = !persistProgress;
            session.dailyPuzzle = loadAsDailyPuzzle && !persistProgress;
            loadAsDailyPuzzle = false;
            tray.SetScrollEnabled(true);
            hint.StopHint();
            toast.Hide();
            rotate.Cancel();
            haptics?.CancelLevelComplete();
            Host.StopAllCoroutines();
            boardRevealRoutine = null;
            timer.ClearPulseHandle();
            loader.ResetLoadedLevel();

            fx.ResetPool();
            Context.CompleteScreen?.Hide();
            Context.FailScreen?.Hide();
            GameSystem.Instance?.CloseSettings();

            levelIndex = Mathf.Clamp(nextLevelIndex, 0, levelManager.LevelCount - 1);
            session.levelNumber = levelNumber > 0 ? levelNumber : levelIndex + 1;
            activeLevel = levelManager.GetLevel(levelIndex);
            loader.Activate(levelIndex, nextPrefab);
            theme.Apply(levelIndex, loader.LoadedLevel);
            Hud.levelText.text = session.dailyPuzzle ? "Daily Puzzle" : "Level " + session.levelNumber.ToString();
            if (Hud.objectiveText != null)
            {
                Hud.objectiveText.text = theme.ObjectiveTitle(activeLevel.Title);
            }

            meta.ApplyMetaLevelPresentation(levelIndex);

            Hud.coinText.text = progress.Coins.ToString();
            if (persistProgress)
            {
                progress.SaveProgress();
            }
            timer.ResetLevelTimer();
            hearts.ResetForLevel();
            drops.ResetForLevel();
            session.freeHintPending = LevelFailStreak.EarnsFreeHint(session.levelNumber);
            ReportLevelStart(persistProgress);
            combo.UpdateDisplay(false);

            board.LayoutCells(loader.LoadedLevel);
            tray.LayoutPieces();
            board.PlaceFixedPieces();
            if (activeLevel.HasFixedPieces)
            {
                locks.OnBoardChanged();
                fog.OnBoardChanged();
            }
            foreach (var state in session.pieces) mood.SetMood(state, CatMood.Neutral);
            UpdateTestLevelButtons();
            boardRevealRoutine = StartCoroutine(LevelStartSequence());
            if (loader.HasPendingUnusedRelease) StartCoroutine(loader.ReleaseUnusedLevelAssets());
        }

        private void ReportLevelStart(bool persistProgress)
        {
            bool loop = EndlessLevels.IsLoopLevel(session.levelNumber, levelManager.LevelCount);
            int bestBefore = progress.GetBestStars(activeLevel.Id);
            // A loop number is a replay once it has been beaten; an authored level once it has any stars.
            bool replay = loop ? session.levelNumber < EndlessLevels.NextLoopLevel : bestBefore > 0;
            avnPendingStart = persistProgress && !session.dailyPuzzle ? new LevelStart
            {
                Level = session.levelNumber,
                Source = AvnLevelSource(GameAnalytics.PendingLevelStartSource),
                LevelId = activeLevel.Id,
                LevelTime = timer.Untimed ? (float?)null : timer.Profile.DurationSeconds,
                MovesStart = hearts.Active ? hearts.Remaining : (int?)null,
                Difficulty = activeLevel.Difficulty,
                Chapter = levelIndex / CatMetaCatalog.LevelsPerChapter + 1,
                Pieces = activeLevel.Pieces.Length,
                Replay = replay,
                Loop = loop,
            } : (LevelStart?)null;
            GameAnalytics.LevelStarted(activeLevel, levelIndex, session.levelNumber, loop, replay, persistProgress, bestBefore, timer.Profile);
        }

        // Runs the board reveal inline so a reveal that finishes instantly (reduced motion)
        // costs no extra frame, then teaches any newly unlocked power-ups before play begins
        // on a fully shown board.
        private IEnumerator LevelStartSequence()
        {
            IEnumerator reveal = board.RevealCells();
            while (reveal.MoveNext()) yield return reveal.Current;

            if (Hud != null) yield return Hud.ShowPowerUpTutorials(config.FreezeDurationSeconds);
            yield return intro.ShowNewMechanics(activeLevel);
            inputLocked = gates.MetaUiOpen || gates.SystemPaused;
            if (!gates.MetaUiOpen)
            {
                timer.StartLevelTimer();
            }

            boardRevealRoutine = null;
            ReportAvnLevelStarted();
            tutorial.Begin();

            // The unlock tutorial ends with the player tapping the power-up: spend it now that play has started.
            while (Hud != null && Hud.TryTakeTutorialUse(out PowerUpKind taught))
            {
                GameSystem.Instance?.UseTaughtPowerUp(taught);
            }

            if (session.freeHintPending) GiveFreeHint();
        }

        private void ReportAvnLevelStarted()
        {
            if (!avnPendingStart.HasValue || (GameSystem.Instance != null && !GameSystem.Instance.IsGameplayOpen)) return;
            LevelStart start = avnPendingStart.Value;
            avnPendingStart = null;
            AvnEvents.LevelStarted(start,
                () => activeLevel != null && activeLevel.ActiveCells.Count > 0
                    ? Mathf.RoundToInt(100f * session.occupancy.Count / activeLevel.ActiveCells.Count) : 0,
                () => timer.Untimed ? (float?)null : timer.RemainingSeconds,
                () => hearts.Active ? hearts.Remaining : (int?)null);
        }

        private static LevelSource AvnLevelSource(string legacySource)
        {
            switch (legacySource)
            {
                case GameAnalytics.SourceNext:
                case GameAnalytics.SourceSkip: return LevelSource.Next;
                case GameAnalytics.SourceRetry:
                case GameAnalytics.SourcePauseRestart:
                case GameAnalytics.SourceHudReset: return LevelSource.Restart;
                default: return LevelSource.Home;
            }
        }

        // After repeated fails the attempt opens with one hint on the house; it spends no charge.
        private void GiveFreeHint()
        {
            if (!hint.ShowHint(timer.Profile.HintReveal)) return;
            session.freeHintPending = false;
            LevelFailStreak.Reset(session.levelNumber);
            toast.Show("Here's a free hint!");
        }

        /// <summary>Stops a running reveal, optionally showing every cell at full size.</summary>
        public void CancelReveal(bool snapCells = true)
        {
            if (boardRevealRoutine == null) return;
            StopCoroutine(boardRevealRoutine);
            boardRevealRoutine = null;
            if (snapCells) board.SnapRevealScales();
        }

        public void ResetLevel()
        {
            AvnEvents.LevelRestarting();
            loadAsDailyPuzzle = session.dailyPuzzle;
            LoadLevel(levelIndex, !levelNavigationTesting, session.levelNumber);
        }

        // UnityEvent helpers for freely browsing levels; previews do not award progress.
        public void LoadPreviousLevel()
        {
            if (!CanGoPreviousLevel) return;
            if (GameSystem.Instance != null) GameSystem.Instance.LoadPreviousLevel();
            else PreviewLevelForTesting(levelIndex - 1);
        }

        public void LoadNextLevel()
        {
            if (!CanGoNextLevel) return;
            if (GameSystem.Instance != null) GameSystem.Instance.LoadNextLevel();
            else PreviewLevelForTesting(levelIndex + 1);
        }

        public void LoadNextLevelDirect()
        {
            if (levelManager == null || levelManager.LevelCount <= 0)
            {
                return;
            }

            int maxLevelIndex = levelManager.LevelCount - 1;
            LoadLevel(CanGoNextLevel ? levelIndex + 1 : maxLevelIndex, !levelNavigationTesting);
        }

        /// <summary>Plays the day's puzzle as a preview; only its first win of the day pays a bonus.</summary>
        public void PlayDailyPuzzle(int zeroBasedLevelIndex)
        {
            meta.ResetOverlayState();
            loadAsDailyPuzzle = true;
            LoadLevel(zeroBasedLevelIndex, false);
        }

        public void PreviewLevelForTesting(int zeroBasedLevelIndex)
        {
            meta.ResetOverlayState();
            LoadLevel(zeroBasedLevelIndex, false);
        }

        public void UpdateTestLevelButtons()
        {
            Button previousTestButton = Hud != null ? Hud.previousTestButton : null;
            Button nextTestButton = Hud != null ? Hud.nextTestButton : null;
            if (previousTestButton != null)
            {
                previousTestButton.interactable = CanGoPreviousLevel;
            }

            if (nextTestButton != null)
            {
                nextTestButton.interactable = CanGoNextLevel;
            }
        }

        private void OnPiecePlaced(PieceState state)
        {
            tutorial.OnPiecePlaced(state);
            drops.RecordDrop();
            locks.OnBoardChanged();
            fog.OnBoardChanged();
            CheckWin();
            if (tip.TryClaimShow(state, tutorial.IsOpen))
            {
                hint.StopHint();
                tip.Show(state);
                GameAnalytics.MoveTipShown();
            }
        }

        private void CheckWin()
        {
            // A full board wins; spare cats may be left on the shelf.
            if (!session.IsBoardFilled)
            {
                return;
            }

            inputLocked = true;
            timer.StopLevelTimer();
            AvnEvents.LevelCompleted();
            LevelFailStreak.Reset(session.levelNumber);
            StartCoroutine(WinSequence());
        }

        private IEnumerator WinSequence()
        {
            // The piece that completed the board already turned happy on placement,
            // and stays that way while input is locked; other pieces keep their own mood.
            for (int i = 0; i < session.pieces.Count; i++)
            {
                StartCoroutine(fx.PopTransform(session.pieces[i].Rect, 1.08f));
            }

            fx.SpawnBoardBurst(session.boardRoot);
            haptics.PlayLevelComplete();
            int chapterBefore = meta.Progress != null ? meta.Progress.HighestEarnedChapter : 0;
            bool enteringEndless = !levelNavigationTesting && !EndlessLevels.IsActive && session.levelNumber == levelManager.LevelCount;
            int awardedCoins = meta.RecordMetaFirstClear(activeLevel.Reward);
            if (session.dailyPuzzle && DailyPuzzle.TryClaimBonus(System.DateTime.UtcNow))
            {
                progress.Coins = (int)System.Math.Min(int.MaxValue, (long)progress.Coins + DailyPuzzle.BonusCoins);
                progress.SaveCoins();
                GameAnalytics.CurrencyEarned(DailyPuzzle.BonusCoins, GameAnalytics.EarnDailyPuzzle, progress.Coins);
                AvnEvents.EconomyEarned(DailyPuzzle.BonusCoins, progress.Coins, EconomySource.level_reward_bonus, EconomyDetail.daily_puzzle);
                awardedCoins += DailyPuzzle.BonusCoins;
            }
            float coinFlightDuration = awardedCoins > 0 ? fx.FlyCoins(awardedCoins, board.BoardCenterScreen()) : 0f;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.75f, coinFlightDuration));
            Hud.coinText.text = progress.Coins.ToString();
            LevelResult result = progress.SaveLevelResult(timer.RemainingSeconds);
            progress.SaveProgress();
            GameAnalytics.LevelCompleted(session.stats, timer.RemainingSeconds, result.Stars, result.BestStars, awardedCoins, session.bestCombo);
            ReportProgressMilestones(chapterBefore, enteringEndless);
            StartCoroutine(fx.PopTransform(Hud.coinText.rectTransform.parent as RectTransform, 1.08f));
            meta.ConfigureMetaWinPresentation(awardedCoins);
            GameSystem.Instance?.ShowLevelComplete(activeLevel.Title, result.Stars, awardedCoins, result.BestStars, session.bestCombo);
        }

        private void ReportProgressMilestones(int chapterBefore, bool enteredEndless)
        {
            if (levelNavigationTesting) return;
            int chapterAfter = meta.Progress != null ? meta.Progress.HighestEarnedChapter : 0;
            if (chapterAfter <= chapterBefore && !enteredEndless) return;
            int totalStars = 0, levelsCleared = 0;
            for (int i = 0; i < levelManager.LevelCount; i++)
            {
                int stars = progress.GetBestStars(levelManager.GetLevel(i).Id);
                totalStars += stars;
                if (stars > 0) levelsCleared++;
            }
            if (chapterAfter > chapterBefore) GameAnalytics.ChapterUnlocked(chapterAfter, totalStars, levelsCleared);
            if (enteredEndless) GameAnalytics.EndlessEntered(totalStars);
        }

        public void FailLevel() => FailLevel("Complete the puzzle before the timer ends.", "timeout");

        public void FailLevel(string message) => FailLevel(message, "other");

        private void FailLevel(string message, string reason)
        {
            if (session.levelFailed || gates.LevelCompleteOpen)
            {
                return;
            }

            int cellCount = activeLevel != null ? activeLevel.ActiveCells.Count : 0;
            GameAnalytics.LevelFailed(reason, session.stats, timer.RemainingSeconds,
                cellCount > 0 ? session.occupancy.Count / (float)cellCount : 0f, session.bestCombo);
            AvnEvents.LevelFailed(AvnEvents.FailReasonFor(reason));
            session.levelFailed = true;
            if (reason == "timeout" || reason == "out_of_hearts") LevelFailStreak.RecordFail(session.levelNumber);
            timer.timerRunning = false;
            inputLocked = true;
            hint.StopHint();
            rotate.Cancel();
            drag.CancelActiveDragToRest();
            PuzzleAudio.PlayWithMusicDuck("Wrong");
            haptics?.Play(HapticKind.LevelFail);
            GameSystem.Instance?.ShowLevelFailed("Try Again", message);
        }

        /// <summary>Freezes the level while another page owns the screen; the reveal is dropped without finishing.</summary>
        public void SuspendForNavigation()
        {
            tutorial.Cancel();
            intro.Cancel();
            Hud?.CancelPowerUpTutorial();
            Hud?.CancelPowerUpPurchase(false);
            CancelReveal(snapCells: false);
            timer.StopLevelTimer(); hint.StopHint(); rotate.Cancel(); drag.CancelActiveDragToRest(); inputLocked = true;
        }

        public void SetSystemPaused(bool paused)
        {
            if (paused)
            {
                timerWasRunningBeforeSettings = timer.timerRunning || IsRevealing;
                timer.timerRunning = false;
                drag.CancelActiveDragToRest();
                inputLocked = true;
            }
            else
            {
                inputLocked = gates.MetaUiOpen || gates.ResultScreenOpen || IsRevealing || gates.PowerUpTutorialOpen || gates.PowerUpPurchaseOpen;
                timer.timerRunning = timerWasRunningBeforeSettings && !session.levelFailed && !inputLocked;
            }
        }

        public void SkipCurrentLevel()
        {
            if (levelManager == null) return;
            if (!CanGoNextLevel) { GameSystem.Instance?.GoHome(); return; }
            // Validate the destination before changing either progress or the current screen.
            if (loader.LoadLevelPrefab(levelIndex + 1) == null) return;
            if (session.dailyPuzzle) { GameSystem.Instance?.GoHome(); return; }
            if (levelNavigationTesting) { LoadNextLevel(); return; }
            if (meta.Progress != null && !meta.Progress.RecordSkip(levelIndex).Succeeded) return;
            AvnEvents.LevelSkipping();
            GameAnalytics.LevelSkipped(session.levelFailed ? "fail_screen" : gates.LevelCompleteOpen ? "complete_screen" : "other");
            GameAnalytics.SetLevelStartSource(GameAnalytics.SourceSkip);
            meta.ResetOverlayState();
            GameSystem.Instance?.ResumeGame();
            GameSystem.Instance?.ShowGameplayPage();
            LoadLevel(levelIndex + 1, true);
        }

        // Replay uses the same puzzle and never clears the player's completion or rewards.
        public void ReplayLevelOneTutorial()
        {
            config.LevelOneTutorialEnabled = true;
            PlayerPrefs.DeleteKey(PuzzleSaveKeys.LevelOneTutorialSeen);
            LoadLevel(0, !levelNavigationTesting);
        }
    }
}
