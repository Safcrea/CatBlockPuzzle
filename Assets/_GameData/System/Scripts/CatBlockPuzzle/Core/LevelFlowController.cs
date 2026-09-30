using System.Collections;
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

        private Coroutine boardRevealRoutine;
        private bool timerWasRunningBeforeSettings;

        public LevelFlowController(IGameplayContext context, PuzzleSession session, PreparedLevelLoader loader, BoardController board,
            CellLockController locks, TrayLayout tray, PieceDragController drag, LevelTimer timer, HintPresenter hint, MovePieceTip tip, LevelOneTutorial tutorial,
            MetaProgressionController meta, ThemePresenter theme, PlayerProgress progress, ComboTracker combo, FxSpawner fx,
            HapticsController haptics, CatMoodPresenter mood, InteractionGates gates, IPuzzleConfig config,
            MechanicIntroPresenter intro) : base(context)
        {
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
            : timer.Profile.Stars(timer.RemainingSeconds);

        public void LoadLevel(int nextLevelIndex, bool persistProgress = true)
        {
            nextLevelIndex = Mathf.Clamp(nextLevelIndex, 0, levelManager.LevelCount - 1);
            // Resolve/validate before disturbing the current level, so a missing prefab is recoverable.
            var nextPrefab = loader.LoadLevelPrefab(nextLevelIndex);
            if (nextPrefab == null) return;
            Hud?.CancelPowerUpTutorial();
            Hud?.CancelPowerUpPurchase(false);
            intro.Cancel();
            tutorial.Cancel();
            session.ResetForLevelLoad();
            timer.timerRunning = false;
            levelNavigationTesting = !persistProgress;
            tray.SetScrollEnabled(true);
            hint.StopHint();
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
            activeLevel = levelManager.GetLevel(levelIndex);
            loader.Activate(levelIndex, nextPrefab);
            theme.Apply(levelIndex, loader.LoadedLevel);
            Hud.levelText.text = "Level " + (levelIndex + 1).ToString();
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
            combo.UpdateDisplay(false);

            board.LayoutCells(loader.LoadedLevel);
            tray.LayoutPieces();
            foreach (var state in session.pieces) mood.SetMood(state, CatMood.Neutral);
            UpdateTestLevelButtons();
            boardRevealRoutine = StartCoroutine(LevelStartSequence());
            if (loader.HasPendingUnusedRelease) StartCoroutine(loader.ReleaseUnusedLevelAssets());
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
            tutorial.Begin();
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
            LoadLevel(levelIndex, !levelNavigationTesting);
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
            locks.OnBoardChanged();
            CheckWin();
            if (tip.TryClaimShow(state, tutorial.IsOpen))
            {
                hint.StopHint();
                tip.Show(state);
            }
        }

        private void CheckWin()
        {
            if (!session.IsBoardFilled || !session.AllPiecesPlaced)
            {
                return;
            }

            inputLocked = true;
            timer.StopLevelTimer();
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
            int awardedCoins = meta.RecordMetaFirstClear(activeLevel.Reward);
            float coinFlightDuration = awardedCoins > 0 ? fx.FlyCoins(awardedCoins, board.BoardCenterScreen()) : 0f;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.75f, coinFlightDuration));
            Hud.coinText.text = progress.Coins.ToString();
            LevelResult result = progress.SaveLevelResult(timer.RemainingSeconds);
            progress.SaveProgress();
            StartCoroutine(fx.PopTransform(Hud.coinText.rectTransform.parent as RectTransform, 1.08f));
            meta.ConfigureMetaWinPresentation(awardedCoins);
            GameSystem.Instance?.ShowLevelComplete(activeLevel.Title, result.Stars, awardedCoins, result.BestStars, session.bestCombo);
        }

        public void FailLevel()
        {
            if (session.levelFailed || gates.LevelCompleteOpen)
            {
                return;
            }

            session.levelFailed = true;
            timer.timerRunning = false;
            inputLocked = true;
            hint.StopHint();
            drag.CancelActiveDragToRest();
            PuzzleAudio.PlayWithMusicDuck("Wrong");
            GameSystem.Instance?.ShowLevelFailed("Try Again", "Complete the puzzle before the timer ends.");
        }

        /// <summary>Freezes the level while another page owns the screen; the reveal is dropped without finishing.</summary>
        public void SuspendForNavigation()
        {
            tutorial.Cancel();
            intro.Cancel();
            Hud?.CancelPowerUpTutorial();
            Hud?.CancelPowerUpPurchase(false);
            CancelReveal(snapCells: false);
            timer.StopLevelTimer(); hint.StopHint(); drag.CancelActiveDragToRest(); inputLocked = true;
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
            if (levelNavigationTesting) { LoadNextLevel(); return; }
            if (meta.Progress != null && !meta.Progress.RecordSkip(levelIndex).Succeeded) return;
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
