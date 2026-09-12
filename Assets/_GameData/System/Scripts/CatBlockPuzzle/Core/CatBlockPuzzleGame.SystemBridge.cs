namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        public int CurrentLevelNumber => levelIndex + 1;
        public int CurrentCoins => coins;
        public int CurrentStarCount => earnedStars > 0 ? earnedStars : CatPuzzleResultCalculator.CalculateStars(levelRemainingSeconds, LevelDurationSeconds);

        public void SetSystemPaused(bool paused)
        {
            if (paused)
            {
                timerWasRunningBeforeSettings = timerRunning;
                timerRunning = false;
                CancelActiveDragToRest();
                inputLocked = true;
            }
            else
            {
                inputLocked = false;
                timerRunning = timerWasRunningBeforeSettings && !levelFailed;
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
