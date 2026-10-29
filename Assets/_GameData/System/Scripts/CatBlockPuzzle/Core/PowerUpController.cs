using AVNEvents;

namespace CatBlockPuzzle
{
    /// <summary>Hint, Freeze and Rotate power-ups: when they may be used, using them, and the purchase popup's pause.</summary>
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
        private readonly RotateController rotate;

        private bool powerUpPurchaseOwnsPause;
        private bool timerWasRunningBeforePurchase;
        private bool inputWasLockedBeforePurchase;
        private bool lastHintFoundTarget;

        public PowerUpController(IGameplayContext context, PuzzleSession session, LevelTimer timer, HintPresenter hint,
            PieceDragController drag, PlayerProgress progress, MetaProgressionController meta, LevelOneTutorial tutorial,
            InteractionGates gates, ILevelFlow flow, RotateController rotate)
        {
            this.rotate = rotate;
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
            if (kind != PowerUpKind.Hint && kind != PowerUpKind.Freeze && kind != PowerUpKind.Rotate) return false;
            if (Hud == null || !Hud.IsPowerUpUnlocked(kind)) return false;
            if (!timer.timerRunning || session.inputLocked || session.levelFailed || gates.ResultScreenOpen || gates.MetaUiOpen ||
                tutorial.IsOpen || gates.PowerUpTutorialOpen || gates.PowerUpPurchaseOpen ||
                gates.SystemPaused || gates.GameplayHidden) return false;
            // One Rotate at a time, and only while a shelf cat is left to turn.
            if (kind == PowerUpKind.Rotate) return !rotate.Active && rotate.HasTurnableCat;
            return kind != PowerUpKind.Freeze || !timer.FreezeUsedThisAttempt;
        }

        public bool TryUseRotatePowerUp()
        {
            if (!CanRequestPowerUp(PowerUpKind.Rotate)) return false;
            rotate.Begin();
            return true;
        }

        public bool TryUseFreezePowerUp()
        {
            if (!CanRequestPowerUp(PowerUpKind.Freeze)) return false;
            timer.Freeze();
            return true;
        }

        /// <summary>Hints spent by one use on the current level.</summary>
        public int HintCost => timer.Profile.HintCost;

        public bool TryUseHintPowerUp()
        {
            if (!CanRequestPowerUp(PowerUpKind.Hint)) return false;
            lastHintFoundTarget = hint.ShowHint(timer.Profile.HintReveal);
            return true;
        }

        /// <summary>Counts a spent power-up toward the attempt and reports it.</summary>
        public void ReportUse(PowerUpKind kind, int cost, bool fromTutorial)
        {
            if (kind == PowerUpKind.Hint) session.stats.hintsUsed++;
            else if (kind == PowerUpKind.Rotate) session.stats.rotatesUsed++;
            else session.stats.freezesUsed++;
            int piecesLeft = 0;
            foreach (PieceState piece in session.pieces)
                if (!piece.Placed && !piece.Definition.IsSpare) piecesLeft++;
            GameAnalytics.PowerUpUsed(kind, cost, timer.RemainingSeconds, piecesLeft, fromTutorial, lastHintFoundTarget);
            AvnEvents.PowerupUsed(kind, fromTutorial);
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
            return CompletePurchase(kind, offer);
        }

        public PowerUpPurchaseStatus PurchasePowerUpFromShop(PowerUpKind kind, PowerUpShopConfig.Offer offer)
        {
            if (!gates.GameplayHidden || gates.ResultScreenOpen) return PowerUpPurchaseStatus.Unavailable;
            return CompletePurchase(kind, offer);
        }

        /// <summary>Buys a coin-priced bundle from the shop: charges the price, adds every power-up and any bonus coins.</summary>
        public PowerUpPurchaseStatus PurchaseBundleFromShop(PowerUpShopConfig.Bundle bundle)
        {
            if (!gates.GameplayHidden || gates.ResultScreenOpen) return PowerUpPurchaseStatus.Unavailable;
            if (!bundle.IsValid) return PowerUpPurchaseStatus.InvalidOffer;
            if (progress.Coins < bundle.coinPrice) return PowerUpPurchaseStatus.InsufficientCoins;
            AvnEvents.EconomySpent(bundle.coinPrice, progress.Coins - bundle.coinPrice, EconomySource.powerup, EconomyDetail.starter_bundle);
            long grantedCoins = (long)progress.Coins - bundle.coinPrice + bundle.bonusCoins;
            progress.Coins = (int)System.Math.Min(int.MaxValue, grantedCoins);
            progress.SaveCoins();
            AvnEvents.EconomyEarned(bundle.bonusCoins, progress.Coins, EconomySource.other, EconomyDetail.starter_bundle);
            Grant(PowerUpKind.Hint, bundle.hints);
            Grant(PowerUpKind.Freeze, bundle.freezes);
            Grant(PowerUpKind.Rotate, bundle.rotates);
            meta.RefreshCoinLabels();
            Hud?.RefreshPowerUpCounts();
            return PowerUpPurchaseStatus.Success;
        }

        /// <summary>Adds coins to the wallet (used by the testing-only coin pack rows).</summary>
        public bool GrantCoinsFromShop(int coins)
        {
            if (coins <= 0 || !gates.GameplayHidden || gates.ResultScreenOpen) return false;
            progress.Coins = (int)System.Math.Min(int.MaxValue, (long)progress.Coins + coins);
            progress.SaveCoins();
            AvnEvents.EconomyEarned(coins, progress.Coins, EconomySource.other, EconomyDetail.shop_coin_pack);
            meta.RefreshCoinLabels();
            return true;
        }

        private void Grant(PowerUpKind kind, int amount)
        {
            if (amount <= 0) return;
            PowerUpInventory.Add(kind, amount);
            GameAnalytics.ItemEarned(kind, amount, GameAnalytics.EarnPurchase);
            AvnEvents.PowerupObtained(kind, amount, PowerupSource.Coins);
        }

        private PowerUpPurchaseStatus CompletePurchase(PowerUpKind kind, PowerUpShopConfig.Offer offer)
        {
            var status = DailyRewardProgress.TryPurchasePowerUp(kind, offer, progress.Coins, PuzzleSaveKeys.SavedCoins, out int remainingCoins);
            if (status != PowerUpPurchaseStatus.Success) return status;
            progress.Coins = remainingCoins;
            GameAnalytics.CurrencySpent(offer.coinPrice, kind, remainingCoins);
            GameAnalytics.ItemEarned(kind, offer.quantity, GameAnalytics.EarnPurchase);
            AvnEvents.EconomySpent(offer.coinPrice, remainingCoins, EconomySource.powerup, AvnEvents.DetailFor(kind));
            AvnEvents.PowerupObtained(kind, offer.quantity, PowerupSource.Coins);
            meta.RefreshCoinLabels();
            Hud?.RefreshPowerUpCounts();
            return status;
        }
    }
}
