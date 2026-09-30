using UnityEngine;
using static CatBlockPuzzle.PuzzleSaveKeys;

namespace CatBlockPuzzle
{
    /// <summary>The player's coin balance, saved level index and per-level best stars.</summary>
    internal sealed class PlayerProgress
    {
        private readonly PuzzleSession session;

        internal int coins;

        public PlayerProgress(PuzzleSession session)
        {
            this.session = session;
        }

        public int Coins
        {
            get => coins;
            set => coins = value;
        }

        public void LoadCoins()
        {
            coins = PlayerPrefs.GetInt(SavedCoins, 0);
        }

        /// <summary>Persists the current level and wallet. Level previews never save.</summary>
        public void SaveProgress()
        {
            if (session.levelNavigationTesting)
            {
                return;
            }

            PlayerPrefs.SetInt(SavedLevel, session.levelIndex);
            PlayerPrefs.SetInt(SavedCoins, coins);
            PlayerPrefs.Save();
        }

        public void SaveCoins()
        {
            PlayerPrefs.SetInt(SavedCoins, coins);
            PlayerPrefs.Save();
        }

        public int GetBestStars(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId)) return 0;
            int savedStars = PlayerPrefs.GetInt(SavedBestStarsPrefix + levelId, 0);
            // Zero means this level has never been completed. Any non-zero save value
            // is untrusted local data and must remain within the visual three-star range.
            return savedStars <= 0 ? 0 : CatPuzzleResultCalculator.ClampStars(savedStars);
        }

        /// <summary>Scores the finished level, records the stars on the session and saves the best result.</summary>
        public LevelResult SaveLevelResult(float remainingSeconds)
        {
            LevelDefinition activeLevel = session.activeLevel;
            DifficultyProfile profile = DifficultyProfile.For(activeLevel, session.levelIndex);
            float duration = profile.DurationSeconds;
            session.earnedStars = profile.Stars(remainingSeconds);
            int previousBest = GetBestStars(activeLevel.Id);
            int best = CatPuzzleResultCalculator.ClampStars(Mathf.Max(previousBest, session.earnedStars));
            if (session.levelNavigationTesting)
            {
                return new LevelResult(activeLevel.Id, duration - remainingSeconds, session.earnedStars, best);
            }

            PlayerPrefs.SetInt(SavedBestStarsPrefix + activeLevel.Id, best);
            PlayerPrefs.Save();
            return new LevelResult(activeLevel.Id, duration - remainingSeconds, session.earnedStars, best);
        }
    }
}
