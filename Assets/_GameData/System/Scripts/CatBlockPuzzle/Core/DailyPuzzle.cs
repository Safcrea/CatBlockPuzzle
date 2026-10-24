using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The daily hard puzzle: one of levels 51-100, picked by the UTC date. It plays as a preview
    /// (no stars or progress saved), and the first win of each UTC day pays a coin bonus.
    /// </summary>
    public static class DailyPuzzle
    {
        public const int BonusCoins = 50;
        /// <summary>The 1-based level that must be won before the button opens.</summary>
        public const int UnlockAfterLevel = 20;
        public const int FirstLevelIndex = 50;
        public const int PoolSize = 50;
        public const string LastWonDayKey = "CatBlockPuzzle.DailyPuzzle.LastWonDay";

        private static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Whole UTC days since 2026-01-01 (negative before it).</summary>
        public static int DayNumber(DateTime utcNow) => (int)Math.Floor((utcNow.ToUniversalTime() - Epoch).TotalDays);

        /// <summary>Zero-based level index of the day's puzzle, clamped to the levels that exist.</summary>
        public static int LevelIndexFor(DateTime utcNow, int levelCount)
        {
            int day = DayNumber(utcNow);
            int index = FirstLevelIndex + ((day % PoolSize) + PoolSize) % PoolSize;
            return Mathf.Clamp(index, 0, Mathf.Max(0, levelCount - 1));
        }

        public static bool IsDoneToday(DateTime utcNow) =>
            PlayerPrefs.HasKey(LastWonDayKey) && PlayerPrefs.GetInt(LastWonDayKey) == DayNumber(utcNow);

        /// <summary>Marks today's puzzle as won; true only for the first win of the day, which earns the bonus.</summary>
        public static bool TryClaimBonus(DateTime utcNow)
        {
            if (IsDoneToday(utcNow)) return false;
            PlayerPrefs.SetInt(LastWonDayKey, DayNumber(utcNow));
            PlayerPrefs.Save();
            return true;
        }
    }
}
