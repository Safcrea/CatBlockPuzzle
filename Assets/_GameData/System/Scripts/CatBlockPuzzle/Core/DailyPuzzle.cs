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
        public enum Reward { Coins, Hint, Freeze }

        /// <summary>Coins paid when the daily reward is coins. Set in the Inspector on <see cref="GameSystem"/>.</summary>
        public static int CoinReward { get; set; } = BonusCoins;

        /// <summary>Hints or Freezes paid when the daily reward is a power-up. Set on <see cref="GameSystem"/>.</summary>
        public static int PowerUpReward { get; set; } = 1;

        /// <summary>One of coins, Hint or Freeze, each equally likely.</summary>
        public static Reward PickReward() => (Reward)UnityEngine.Random.Range(0, 3);

        /// <summary>
        /// The 1-based level that must be won before the button opens (0 = always open).
        /// Set in the Inspector on <see cref="GameSystem"/>; this is the live value.
        /// </summary>
        public static int UnlockAfterLevel { get; set; } = 10;

        /// <summary>Time left until the next UTC day, when a new puzzle becomes available.</summary>
        public static TimeSpan TimeUntilNext(DateTime utcNow)
        {
            DateTime now = utcNow.ToUniversalTime();
            return now.Date.AddDays(1) - now;
        }

        /// <summary>HH:MM:SS, rounded up so the clock never shows 00:00:00 while still locked.</summary>
        public static string FormatCountdown(TimeSpan remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));
            return string.Format("{0:00}:{1:00}:{2:00}", seconds / 3600, seconds / 60 % 60, seconds % 60);
        }
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
