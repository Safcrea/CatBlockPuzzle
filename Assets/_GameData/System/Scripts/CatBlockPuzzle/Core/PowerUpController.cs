namespace CatBlockPuzzle
{
    /// <summary>Hint and Freeze power-ups: when they may be used, using them, and the purchase popup's pause.</summary>
    internal sealed class PowerUpController
    {
        private readonly IGameplayContext context;
        private readonly PuzzleSession session;
        private readonly LevelTimer timer;
        private readonly HintPresenter hint;
        private readonly PieceDragController drag;
        private readonly PlayerProgress progress;
        private readonly MetaProgressionController meta;
        private readonly LevelOneTutorial tutorial;
        private readonly InteractionGates gates;
        private readonly ILevelFlow flow;

        private bool powerUpPurchaseOwnsPause;
        private bool timerWasRunningBeforePurchase;
        private bool inputWasLockedBeforePurchase;

        public PowerUpController(IGameplayContext context, PuzzleSession session, LevelTimer timer, HintPresenter hint,
            PieceDragController drag, PlayerProgress progress, MetaProgressionController meta, LevelOneTutorial tutorial,
            InteractionGates gates, ILevelFlow flow)
        {
            this.context = context;
            this.session = session;
            this.timer = timer;
            this.hint = hint;
            this.drag = drag;
            this.progress = progress;
            this.meta = meta;
            this.tutorial = tutorial;
            this.gates = gates;
            this.flow = flow;
        }

        private GameplayHudView Hud => context.Hud;

        public bool CanRequestPowerUp(PowerUpKind kind)
        {
            if (kind != PowerUpKind.Hint && kind != PowerUpKind.Freeze) return false;
            if (Hud == null || !Hud.IsPowerUpUnlocked(kind)) return false;
            if (!timer.timerRunning || session.inputLocked || session.levelFailed || gates.ResultScreenOpen || gates.MetaUiOpen ||
                tutorial.IsOpen || gates.PowerUpTutorialOpen || gates.PowerUpPurchaseOpen ||
                gates.SystemPaused || gates.GameplayHidden) return false;
            return kind != PowerUpKind.Freeze || !timer.FreezeUsedThisAttempt;
        }

        public bool TryUseFreezePowerUp()
        {
            if (!CanRequestPowerUp(PowerUpKind.Freeze)) return false;
            timer.Freeze();
            return true;
        }

        public bool TryUseHintPowerUp()
        {
            if (!CanRequestPowerUp(PowerUpKind.Hint)) return false;
            hint.ShowHint();
            return true;
        }

        public bool OpenPowerUpPurchase(PowerUpKind kind)
        {
            if (!CanRequestPowerUp(kind)) return false;
            return Hud.ShowPowerUpPurchase(kind);
        }

        /// <summary>Freezes the run behind the purchase popup and restores it afterwards.</summary>
        public void SetPowerUpPurchaseOpen(bool open, bool resumeGameplay = true)
        {
            if (open)
            {
                if (powerUpPurchaseOwnsPause) return;
                timerWasRunningBeforePurchase = timer.timerRunning;
                inputWasLockedBeforePurchase = session.inputLocked;
                powerUpPurchaseOwnsPause = true;
                timer.StopLevelTimer();
                hint.StopHint();
                drag.CancelActiveDragToRest();
                session.inputLocked = true;
                return;
            }
            if (!powerUpPurchaseOwnsPause) return;
            powerUpPurchaseOwnsPause = false;
            if (!resumeGameplay) return;
            session.inputLocked = inputWasLockedBeforePurchase || gates.MetaUiOpen || gates.ResultScreenOpen ||
                flow.IsRevealing || gates.PowerUpTutorialOpen ||
                gates.SystemPaused || gates.GameplayHidden;
            timer.timerRunning = timerWasRunningBeforePurchase && !session.levelFailed && !session.inputLocked;
        }

        public PowerUpPurchaseStatus PurchasePowerUp(PowerUpKind kind, PowerUpShopConfig.Offer offer)
        {
            if (!gates.PowerUpPurchaseOpen || Hud.PurchaseKind != kind || PowerUpInventory.GetCount(kind) > 0)
                return PowerUpPurchaseStatus.Unavailable;
            var status = DailyRewardProgress.TryPurchasePowerUp(kind, offer, progress.Coins, PuzzleSaveKeys.SavedCoins, out int remainingCoins);
            if (status != PowerUpPurchaseStatus.Success) return status;
            progress.Coins = remainingCoins;
            meta.RefreshCoinLabels();
            Hud?.RefreshPowerUpCounts();
            return status;
        }
    }
}
