using System;
using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    /// <summary>The Daily Puzzle pays coins per star and builds a streak over consecutive days, with milestone bonuses.</summary>
    public sealed class DailyPuzzleStreakTests
    {
        private bool hadDay, hadStreak;
        private int savedDay, savedStreak;

        [SetUp]
        public void SaveKeys()
        {
            hadDay = PlayerPrefs.HasKey(DailyPuzzle.LastWonDayKey); savedDay = PlayerPrefs.GetInt(DailyPuzzle.LastWonDayKey);
            hadStreak = PlayerPrefs.HasKey(DailyPuzzle.StreakKey); savedStreak = PlayerPrefs.GetInt(DailyPuzzle.StreakKey);
            PlayerPrefs.DeleteKey(DailyPuzzle.LastWonDayKey);
            PlayerPrefs.DeleteKey(DailyPuzzle.StreakKey);
        }

        [TearDown]
        public void RestoreKeys()
        {
            if (hadDay) PlayerPrefs.SetInt(DailyPuzzle.LastWonDayKey, savedDay); else PlayerPrefs.DeleteKey(DailyPuzzle.LastWonDayKey);
            if (hadStreak) PlayerPrefs.SetInt(DailyPuzzle.StreakKey, savedStreak); else PlayerPrefs.DeleteKey(DailyPuzzle.StreakKey);
        }

        private static DateTime Day(int offset) => new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(offset);

        [Test]
        public void ConsecutiveWins_BuildTheStreak_AndAMissedDayRestartsIt()
        {
            Assert.That(DailyPuzzle.Streak(Day(0)), Is.Zero);
            Assert.That(DailyPuzzle.TryClaimBonus(Day(0)), Is.True);
            Assert.That(DailyPuzzle.TryClaimBonus(Day(0)), Is.False, "Only the first win of a day counts.");
            Assert.That(DailyPuzzle.TryClaimBonus(Day(1)), Is.True);
            Assert.That(DailyPuzzle.TryClaimBonus(Day(2)), Is.True);
            Assert.That(DailyPuzzle.Streak(Day(2)), Is.EqualTo(3));
            Assert.That(DailyPuzzle.Streak(Day(3)), Is.EqualTo(3), "The streak still shows on the next day, before it is played.");
            Assert.That(DailyPuzzle.Streak(Day(4)), Is.Zero, "A missed day ends the streak.");
            Assert.That(DailyPuzzle.TryClaimBonus(Day(4)), Is.True);
            Assert.That(DailyPuzzle.Streak(Day(4)), Is.EqualTo(1));
        }

        [TestCase(1, 50)]
        [TestCase(2, 100)]
        [TestCase(3, 150)]
        public void FirstWin_PaysCoinsPerStar(int stars, int coins)
        {
            int before = DailyPuzzle.CoinsPerStar;
            DailyPuzzle.CoinsPerStar = 50;
            try { Assert.That(DailyPuzzle.CoinsFor(stars), Is.EqualTo(coins)); }
            finally { DailyPuzzle.CoinsPerStar = before; }
        }

        [Test]
        public void StreakMilestones_PayTheirBonus()
        {
            Assert.That(DailyPuzzle.BonusFor(2).Any, Is.False);
            Assert.That(DailyPuzzle.BonusFor(3).hints, Is.EqualTo(1));
            var week = DailyPuzzle.BonusFor(7);
            Assert.That(week.freezes, Is.EqualTo(1));
            Assert.That(week.hammers, Is.EqualTo(1));
            Assert.That(DailyPuzzle.BonusFor(30).coins, Is.EqualTo(500));
        }
    }
}
