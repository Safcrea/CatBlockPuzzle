using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class TutorialResetTests
    {
        [Test]
        public void ResetAll_ForgetsEveryTutorialIncludingTheHammerAndTheIntroCards()
        {
            string[] keys = TutorialReset.SeenKeys();
            var saved = new System.Collections.Generic.Dictionary<string, int?>();
            foreach (string key in keys) { saved[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null; PlayerPrefs.SetInt(key, 1); }
            try
            {
                Assert.That(keys, Does.Contain(PowerUpTutorialManager.HammerSeenKey), "The Hammer tutorial is part of the reset.");
                Assert.That(keys, Does.Contain(PuzzleSaveKeys.LevelOneTutorialSeen));
                Assert.That(keys, Does.Contain(MechanicIntroPresenter.SeenKey(MechanicIntroPresenter.Fog)));
                Assert.That(keys.Length, Is.EqualTo(new System.Collections.Generic.HashSet<string>(keys).Count), "No key is listed twice.");
                TutorialReset.ResetAll();
                foreach (string key in keys) Assert.That(PlayerPrefs.HasKey(key), Is.False, key + " should be forgotten.");
            }
            finally
            {
                foreach (var pair in saved) { if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key); }
                PlayerPrefs.Save();
            }
        }
    }
}
