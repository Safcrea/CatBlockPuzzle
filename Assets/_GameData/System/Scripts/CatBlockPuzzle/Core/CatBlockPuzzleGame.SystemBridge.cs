using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        public int CurrentLevelNumber => levelIndex + 1;
        public int CurrentCoins => coins;
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
        public bool IsLevelAvailable(int index) => IsMetaLevelSelectable(index);
        public int GetLevelStars(int index) => levelManager != null && index >= 0 && index < LevelCount ? GetBestStars(levelManager.GetLevel(index).Id) : 0;
        public string GetChapterTitle(int chapter) => metaCatalog.GetChapter(chapter).Title;
        public Sprite GetChapterThumbnail(int chapter) => contentCatalog.roomThumbnails[chapter];
        public void StartSelectedLevel(int index) => PlayMetaLevel(index);
        public void RequestHint() => GameSystem.Instance?.TryUseHintPowerUp();
        public void SuspendForNavigation()
        {
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
                inputLocked = IsMetaUiOpen || IsResultScreenOpen;
                timerRunning = timerWasRunningBeforeSettings && !levelFailed && !inputLocked;
            }
        }

        public void RestartCurrentLevel() => ResetLevel();
        public void LoadNextLevelFromSystem() => LoadNextLevelThroughMetaGate();
        public void ShowHomeFromSystem() => OpenRoomHub();

        public void SetHapticsFromSystem(bool enabled)
        {
            hapticsEnabled = enabled;
            SavePreferences();
        }
    }
}
