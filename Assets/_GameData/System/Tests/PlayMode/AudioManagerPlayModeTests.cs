using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatBlockPuzzle.Tests
{
    /// <summary>The AudioManager's playback rules, run against the real clips with no scene.</summary>
    public sealed class AudioManagerPlayModeTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private Component manager;
        private bool hadSfxKey;
        private int sfxKey;

        private static Type ManagerType => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CatBlockPuzzle.AudioManager")).First(t => t != null);
        private object Get(string name) => manager.GetType().GetField(name, Flags).GetValue(manager);
        private AudioSource Voice => (AudioSource)Get("voiceSource");
        private AudioSource[] Pool => (AudioSource[])Get("pool");
        private void Call(string name, params object[] args) => manager.GetType().GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(manager, args);

        [SetUp]
        public void CreateManager()
        {
            hadSfxKey = PlayerPrefs.HasKey("CatBlockPuzzle.Settings.Sfx");
            sfxKey = PlayerPrefs.GetInt("CatBlockPuzzle.Settings.Sfx", 1);
            PlayerPrefs.SetInt("CatBlockPuzzle.Settings.Sfx", 1);
            manager = (Component)ManagerType.GetMethod("EnsureInstance").Invoke(null, null);
        }

        [TearDown]
        public void RestorePrefs()
        {
            if (manager != null) UnityEngine.Object.DestroyImmediate(manager.gameObject);
            if (hadSfxKey) PlayerPrefs.SetInt("CatBlockPuzzle.Settings.Sfx", sfxKey); else PlayerPrefs.DeleteKey("CatBlockPuzzle.Settings.Sfx");
        }

        [UnityTest]
        public IEnumerator OnlyOneVoiceSpeaksAtATime()
        {
            yield return null;
            Call("Play", "voice_good");
            Call("Play", "voice_nice");
            Assert.That(Voice.clip.name, Does.Contain("voice_nice"));
            Assert.That(Pool.Count(s => s.isPlaying && s.clip != null && s.clip.name.Contains("voice")), Is.Zero, "Voices must use the voice source only.");
        }

        [UnityTest]
        public IEnumerator PraiseNeverRepeatsTheSameWordTwiceInARow()
        {
            yield return null;
            string previous = null;
            for (int i = 0; i < 12; i++)
            {
                Call("PlayPraise", 2);
                yield return new WaitForSecondsRealtime(0.25f);
                string word = Voice.clip.name;
                Assert.That(word, Is.Not.EqualTo(previous), "iteration " + i);
                Assert.That(word, Does.Contain("voice_good").Or.Contain("voice_nice"));
                previous = word;
            }
        }

        [UnityTest]
        public IEnumerator Praise_OnlyForTwoOrMoreLines_AndFourLinesNeverUsesWow()
        {
            yield return null;
            Call("PlayPraise", 1);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Voice.clip, Is.Null.Or.Property("name").Not.Contain("voice"), "A single line gets no voice.");
            for (int i = 0; i < 6; i++)
            {
                Call("PlayPraise", 4);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(Voice.clip.name, Does.Contain("voice_perfect"));
            }
        }

        [UnityTest]
        public IEnumerator ShortUiSounds_Overlap_AndTheMasterSwitchSilencesEverythingAndIsSaved()
        {
            yield return null;
            Call("Play", "ui_tap"); Call("Play", "ui_popup_open"); Call("Play", "coin_collect");
            Assert.That(Pool.Count(s => s.isPlaying), Is.GreaterThanOrEqualTo(3));
            Call("SetSfxEnabled", false);
            Assert.That(Pool.Count(s => s.isPlaying), Is.Zero);
            Assert.That(PlayerPrefs.GetInt("CatBlockPuzzle.Settings.Sfx"), Is.Zero);
            Call("Play", "ui_tap");
            Assert.That(Pool.Count(s => s.isPlaying), Is.Zero, "No sound while the master switch is off.");
            Call("SetSfxEnabled", true);
            Call("PlayEvent", "ButtonTap", 1f);
            Assert.That(Pool.Count(s => s.isPlaying), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UnknownSoundsAndEvents_AreSkippedWithoutCrashing()
        {
            yield return null;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ui_toggle_off"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("NoSuchEvent"));
            Call("Play", "ui_toggle_off");
            Call("PlayEvent", "NoSuchEvent", 1f);
            Assert.That(Pool.Count(s => s.isPlaying), Is.Zero);
        }
    }
}
