using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        public int CurrentLevelNumber => levelIndex + 1;
        public int CurrentCoins => coins;
        private bool powerUpPurchaseOwnsPause;
        private bool timerWasRunningBeforePurchase;
        private bool inputWasLockedBeforePurchase;
        public void AwardExternalCoins(int amount)
        {
            if (amount <= 0) return;
            long updated = (long)coins + amount;
            coins = updated > int.MaxValue ? int.MaxValue : (int)updated;
            PlayerPrefs.SetInt(SavedCoinsKey, coins);
            PlayerPrefs.Save();
            if (coinText != null) coinText.text = coins.ToString();
            if (metaCoinLabels != null)
                foreach (var label in metaCoinLabels) if (label != null) label.text = coins.ToString();
        }
        public int LevelCount => levelManager != null ? levelManager.LevelCount : 0;
        public int RecommendedLevelIndex => metaProgress != null ? GetRecommendedLevelIndex() : 0;
        public bool IsLevelAvailable(int index) => levelManager != null && index >= 0 && index < levelManager.LevelCount && IsMetaLevelSelectable(index);
        public int GetLevelStars(int index) => levelManager != null && index >= 0 && index < LevelCount ? GetBestStars(levelManager.GetLevel(index).Id) : 0;
        public string GetChapterTitle(int chapter) => metaCatalog.GetChapter(chapter).Title;
        public Sprite GetChapterThumbnail(int chapter) => contentCatalog.roomThumbnails[chapter];
        public void StartSelectedLevel(int index) => PlayMetaLevel(index);
        public void RequestHint() => GameSystem.Instance?.TryUseHintPowerUp();
        public void RefreshPowerUpHud() => gameplayHud?.RefreshPowerUpCounts();
        public bool IsPowerUpTutorialOpen => gameplayHud != null && gameplayHud.IsTutorialOpen;
        public bool IsPowerUpPurchaseOpen => gameplayHud != null && gameplayHud.IsPurchaseOpen;
        public bool OpenPowerUpPurchase(PowerUpKind kind)
        {
            if (!CanRequestPowerUp(kind)) return false;
            return gameplayHud.ShowPowerUpPurchase(kind);
        }

        internal void SetPowerUpPurchaseOpen(bool open, bool resumeGameplay = true)
        {
            if (open)
            {
                if (powerUpPurchaseOwnsPause) return;
                timerWasRunningBeforePurchase = timerRunning;
                inputWasLockedBeforePurchase = inputLocked;
                powerUpPurchaseOwnsPause = true;
                StopLevelTimer();
                StopHint();
                CancelActiveDragToRest();
                inputLocked = true;
                return;
            }
            if (!powerUpPurchaseOwnsPause) return;
            powerUpPurchaseOwnsPause = false;
            if (!resumeGameplay) return;
            inputLocked = inputWasLockedBeforePurchase || IsMetaUiOpen || IsResultScreenOpen ||
                boardRevealRoutine != null || IsPowerUpTutorialOpen ||
                (GameSystem.Instance != null && (GameSystem.Instance.IsPaused || !GameSystem.Instance.IsGameplayOpen));
            timerRunning = timerWasRunningBeforePurchase && !levelFailed && !inputLocked;
        }

        internal PowerUpPurchaseStatus PurchasePowerUp(PowerUpKind kind, PowerUpShopConfig.Offer offer)
        {
            if (!IsPowerUpPurchaseOpen || gameplayHud.PurchaseKind != kind || PowerUpInventory.GetCount(kind) > 0)
                return PowerUpPurchaseStatus.Unavailable;
            var status = DailyRewardProgress.TryPurchasePowerUp(kind, offer, coins, SavedCoinsKey, out int remainingCoins);
            if (status != PowerUpPurchaseStatus.Success) return status;
            coins = remainingCoins;
            if (coinText != null) coinText.text = coins.ToString();
            if (metaCoinLabels != null)
                foreach (var label in metaCoinLabels) if (label != null) label.text = coins.ToString();
            RefreshPowerUpHud();
            return status;
        }
        public void SuspendForNavigation()
        {
            CancelLevelOneTutorial();
            gameplayHud?.CancelPowerUpTutorial();
            gameplayHud?.CancelPowerUpPurchase(false);
            if (boardRevealRoutine != null) { StopCoroutine(boardRevealRoutine); boardRevealRoutine = null; }
            StopLevelTimer(); StopHint(); CancelActiveDragToRest(); inputLocked = true;
        }
        public int CurrentStarCount => earnedStars > 0 ? earnedStars : CatPuzzleResultCalculator.CalculateStars(levelRemainingSeconds, LevelDurationSeconds);

        public void SetSystemPaused(bool paused)
        {
            if (paused)
            {
                timerWasRunningBeforeSettings = timerRunning || boardRevealRoutine != null;
                timerRunning = false;
                CancelActiveDragToRest();
                inputLocked = true;
            }
            else
            {
                inputLocked = IsMetaUiOpen || IsResultScreenOpen || boardRevealRoutine != null || IsPowerUpTutorialOpen || IsPowerUpPurchaseOpen;
                timerRunning = timerWasRunningBeforeSettings && !levelFailed && !inputLocked;
            }
        }

        public void RestartCurrentLevel() => ResetLevel();
        public void LoadNextLevelFromSystem() => LoadNextLevelThroughMetaGate();
        public void SkipCurrentLevel()
        {
            if (levelManager == null) return;
            if (!CanGoNextLevel) { GameSystem.Instance?.GoHome(); return; }
            // Validate the destination before changing either progress or the current screen.
            if (LoadLevelPrefab(levelIndex + 1) == null) return;
            if (levelNavigationTesting) { LoadNextLevel(); return; }
            if (metaProgress != null && !metaProgress.RecordSkip(levelIndex).Succeeded) return;
            if (metaOverlay != null) metaOverlay.gameObject.SetActive(false);
            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;
            metaRequiresRoomCompletion = false;
            GameSystem.Instance?.ResumeGame();
            GameSystem.Instance?.ShowGameplayPage();
            LoadLevel(levelIndex + 1, true);
        }
        public void ShowHomeFromSystem() => OpenRoomHub();

        public void SetHapticsFromSystem(bool enabled)
        {
            hapticsEnabled = enabled;
            SavePreferences();
        }
    }
}
