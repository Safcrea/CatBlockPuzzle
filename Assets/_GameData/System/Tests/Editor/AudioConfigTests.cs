using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class AudioConfigTests
    {
        private static readonly string[] AllSounds =
        {
            "ui_tap", "ui_back", "ui_toggle_on", "ui_popup_open", "ui_popup_close", "ui_error",
            "block_pick", "block_place", "block_invalid", "block_return", "row_clear", "combo_clear_2", "combo_clear_3", "combo_clear_4", "board_clear",
            "voice_good", "voice_nice", "voice_great", "voice_sweet", "voice_wow", "voice_perfect", "voice_amazing", "voice_excellent",
            "hint_use", "hint_show", "hint_none", "level_start", "level_complete", "star_collect_1", "star_collect_2", "star_collect_3",
            "level_fail", "level_unlock", "game_over", "new_highscore", "cat_meow_happy", "cat_meow_cute", "cat_purr", "cat_meow_surprised",
            "cat_meow_sad", "cat_jump", "confetti_pop", "coin_collect", "reward_chest_open", "daily_reward", "booster_use", "shop_buy", "timer_warning"
        };

        private static AudioConfig Config => AudioConfig.Load();

        [Test]
        public void AllFortyEightSounds_AreListedAndTheirClipsLoad()
        {
            AudioConfig config = Config;
            Assert.That(config.sounds.Select(s => s.name), Is.EquivalentTo(AllSounds));
            foreach (AudioConfig.SoundEntry sound in config.sounds)
                Assert.That(Resources.Load<AudioClip>("CatBlockPuzzle/Audio/" + sound.file), Is.Not.Null, sound.name + " -> " + sound.file);
        }

        [Test]
        public void EveryEventPointsAtAListedSound_AndEveryAudioEventIsMapped()
        {
            AudioConfig config = Config;
            var names = new HashSet<string>(config.sounds.Select(s => s.name));
            foreach (AudioConfig.EventEntry entry in config.events)
                Assert.That(names, Does.Contain(entry.sound), "event " + entry.eventName);
            var mapped = new HashSet<string>(config.events.Select(e => e.eventName));
            foreach (var field in typeof(AudioEvents).GetFields().Where(f => f.IsLiteral))
                Assert.That(mapped, Does.Contain((string)field.GetRawConstantValue()), "AudioEvents." + field.Name);
        }

        [Test]
        public void Volumes_FollowTheStartingValues()
        {
            var expected = new Dictionary<string, float>
            {
                ["ui_tap"] = 0.6f, ["block_place"] = 0.8f, ["hint_show"] = 0.8f, ["combo_clear_2"] = 0.9f, ["board_clear"] = 0.9f,
                ["voice_good"] = 1f, ["cat_purr"] = 0.8f, ["coin_collect"] = 0.8f, ["shop_buy"] = 0.8f
            };
            foreach (var pair in expected)
                Assert.That(Config.sounds.Single(s => s.name == pair.Key).volume, Is.EqualTo(pair.Value).Within(0.001f), pair.Key);
        }

        [Test]
        public void KnownProblemFiles_AreFlaggedInTheConfig()
        {
            AudioConfig config = Config;
            AudioConfig.SoundEntry Get(string n) => config.sounds.Single(s => s.name == n);
            Assert.That(Get("ui_toggle_on").status, Is.EqualTo("duplicate_of ui_back"));
            Assert.That(Get("ui_toggle_on").file, Is.EqualTo(Get("ui_back").file));
            foreach (string n in new[] { "block_invalid", "block_return", "cat_meow_happy", "voice_wow", "new_highscore" })
                Assert.That(Get(n).status, Is.EqualTo("review"), n);
            foreach (AudioConfig.SoundEntry s in config.sounds.Where(s => s.duration < 0.2f || s.duration > 1f))
                Assert.That(s.status, Is.Not.EqualTo("ok"), s.name + " (" + s.duration + "s) must be marked for review");
        }

        [Test]
        public void VoiceWow_IsNotInAnyPraisePool_AndPoolsCoverTwoToFiveLines()
        {
            AudioConfig config = Config;
            Assert.That(config.praise.SelectMany(p => p.sounds), Does.Not.Contain("voice_wow"));
            Assert.That(config.praise.Select(p => p.lines), Is.EquivalentTo(new[] { 2, 3, 4, 5 }));
            Assert.That(config.praise.Single(p => p.lines == 4).sounds, Is.EqualTo(new[] { "voice_perfect" }));
            Assert.That(config.voiceDelaySeconds, Is.EqualTo(0.15f).Within(0.001f));
        }
    }
}
