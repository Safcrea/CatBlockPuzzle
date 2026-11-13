using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The daily hard puzzle: one of levels 51-100, picked by the UTC date. It plays as a preview (no stars or progress
    /// saved). The first win of each UTC day pays coins per star earned, and winning on consecutive days builds a streak
    /// with bonuses at 3, 7 and 30 days.
    /// </summary>
    public static class DailyPuzzle
    {
        public const int BonusCoins = 50;
        public const string StreakKey = "CatBlockPuzzle.DailyPuzzle.Streak";
        public const string UnlockSeenKey = "CatBlockPuzzle.DailyPuzzle.UnlockSeen";

        /// <summary>Coins paid for each star of the day's first win. Set in the Inspector on <see cref="GameSystem"/>.</summary>
        public static int CoinsPerStar { get; set; } = BonusCoins;

        /// <summary>What the day's first win pays for <paramref name="stars"/> stars (1-3).</summary>
        public static int CoinsFor(int stars) => Mathf.Clamp(stars, 1, 3) * Mathf.Max(0, CoinsPerStar);

        /// <summary>A streak milestone's extra reward.</summary>
        public struct StreakBonus
        {
            public int coins, hints, freezes, hammers;
            public bool Any => coins + hints + freezes + hammers > 0;
        }

        /// <summary>3 days: a Hint. Every 7 days: a Freeze and a Hammer. Every 30 days: 500 coins.</summary>
        public static StreakBonus BonusFor(int streak)
        {
            var bonus = new StreakBonus();
            if (streak == 3) bonus.hints = 1;
            if (streak > 0 && streak % 7 == 0) { bonus.freezes = 1; bonus.hammers = 1; }
            if (streak > 0 && streak % 30 == 0) bonus.coins = 500;
            return bonus;
        }

        /// <summary>Days in a row the puzzle has been won, counting today or yesterday; 0 once a day was missed.</summary>
        public static int Streak(DateTime utcNow)
        {
            if (!PlayerPrefs.HasKey(LastWonDayKey)) return 0;
            int last = PlayerPrefs.GetInt(LastWonDayKey), today = DayNumber(utcNow);
            return last == today || last == today - 1 ? Mathf.Max(1, PlayerPrefs.GetInt(StreakKey, 1)) : 0;
        }

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

        /// <summary>Marks today's puzzle as won; true only for the first win of the day, which earns the bonus. Also moves the
        /// streak on: one more after yesterday's win, otherwise it starts again at 1.</summary>
        public static bool TryClaimBonus(DateTime utcNow)
        {
            if (IsDoneToday(utcNow)) return false;
            int today = DayNumber(utcNow);
            bool continues = PlayerPrefs.HasKey(LastWonDayKey) && PlayerPrefs.GetInt(LastWonDayKey) == today - 1;
            PlayerPrefs.SetInt(StreakKey, continues ? Mathf.Max(1, PlayerPrefs.GetInt(StreakKey, 1)) + 1 : 1);
            PlayerPrefs.SetInt(LastWonDayKey, today);
            PlayerPrefs.Save();
            return true;
        }
    }
}
