using System;
using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    /// <summary>The seven-day reward streak: consecutive days advance, a missed day restarts, and day 7 wraps to day 1.</summary>
    public sealed class DailyRewardProgressTests
    {
        private const string SaveKey = "CatBlockPuzzle.DailyReward.State.v1";
        private bool hadSave;
        private string savedState;
        private DailyRewardConfig config;

        [SetUp]
        public void SeedExistingPlayerWithoutClaims()
        {
            hadSave = PlayerPrefs.HasKey(SaveKey);
            savedState = PlayerPrefs.GetString(SaveKey, string.Empty);
            // An existing install (not firstSessionPending) that has never claimed.
            PlayerPrefs.SetString(SaveKey, "{\"version\":1,\"hintCount\":0,\"freezeCount\":0}");
            config = DailyRewardConfig.Load();
            Assert.That(config, Is.Not.Null, "DailyRewardConfig resource is missing");
            Assert.That(config.DayCount, Is.EqualTo(7));
        }

        [TearDown]
        public void RestorePlayerSave()
        {
            if (hadSave) PlayerPrefs.SetString(SaveKey, savedState); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private static DateTime Day(int offset) => new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc).AddDays(offset);

        [Test]
        public void ConsecutiveClaims_WalkTheWeekThenWrapToDayOne()
        {
            for (int offset = 0; offset < 16; offset++)
            {
                int expected = (offset % 7) + 1;
                Assert.That(DailyRewardProgress.GetAvailableDay(config, Day(offset)), Is.EqualTo(expected), "offered on day " + offset);
                Assert.That(DailyRewardProgress.TryClaim(config, Day(offset), expected, out _), Is.True, "claim on day " + offset);
                Assert.That(DailyRewardProgress.GetAvailableDay(config, Day(offset)), Is.Zero, "a second claim on the same day");
            }
        }

        [Test]
        public void DayAfterSeven_IsDayOne_EvenJustPastMidnight()
        {
            PlayerPrefs.SetString(SaveKey, "{\"version\":1,\"lastClaimUtcDate\":\"2026-10-05\",\"lastClaimDay\":7}");
            Assert.That(DailyRewardProgress.GetAvailableDay(config, new DateTime(2026, 10, 6, 0, 5, 0, DateTimeKind.Utc)), Is.EqualTo(1));
        }

        [Test]
        public void MissedDay_RestartsFromDayOne()
        {
            Assert.That(DailyRewardProgress.TryClaim(config, Day(0), 1, out _), Is.True);
            Assert.That(DailyRewardProgress.TryClaim(config, Day(1), 2, out _), Is.True);
            Assert.That(DailyRewardProgress.GetAvailableDay(config, Day(3)), Is.EqualTo(1), "two days later restarts the streak");
            Assert.That(DailyRewardProgress.TryClaim(config, Day(3), 1, out _), Is.True);
            Assert.That(DailyRewardProgress.GetAvailableDay(config, Day(4)), Is.EqualTo(2));
        }

        [Test]
        public void ClaimingTheWrongDay_IsRejected()
        {
            Assert.That(DailyRewardProgress.TryClaim(config, Day(0), 3, out _), Is.False);
            Assert.That(DailyRewardProgress.GetAvailableDay(config, Day(0)), Is.EqualTo(1));
        }
    }
}
