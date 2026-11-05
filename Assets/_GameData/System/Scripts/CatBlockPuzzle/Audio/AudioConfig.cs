using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The editable audio setup, loaded from Resources/CatBlockPuzzle/Audio/audio_config.json: every sound's clip,
    /// volume and review status, which game event plays which sound, and the praise-voice pools.
    /// </summary>
    [Serializable]
    internal sealed class AudioConfig
    {
        public const string ResourcePath = "CatBlockPuzzle/Audio/audio_config";

        /// <summary>Status values: "ok", "review", "duplicate_of &lt;sound&gt;" or "missing".</summary>
        [Serializable]
        public sealed class SoundEntry
        {
            public string name;
            /// <summary>Clip path under Resources, without extension.</summary>
            public string file;
            public float volume = 1f;
            public string status = "ok";
            public string note;
            /// <summary>Length measured when the config was generated; the Sound Test shows the clip's real length.</summary>
            public float duration;
            /// <summary>When above 0 the sound is faded out and stopped after this many seconds (trims a long tail).</summary>
            public float maxSeconds;
            /// <summary>Name of a sound this one cuts off when it starts, so the two never smear together.</summary>
            public string cuts;
        }

        [Serializable]
        public sealed class EventEntry
        {
            public string eventName;
            public string sound;
        }

        [Serializable]
        public sealed class PraisePool
        {
            /// <summary>Lines cleared (or combo streak) this pool answers; the last pool also covers anything higher.</summary>
            public int lines;
            public string[] sounds;
        }

        public string masterEnabledKey = "CatBlockPuzzle.Settings.Sfx";
        public float voiceDelaySeconds = 0.15f;
        public int maxOverlappingSounds = 10;
        public SoundEntry[] sounds = new SoundEntry[0];
        public EventEntry[] events = new EventEntry[0];
        public PraisePool[] praise = new PraisePool[0];

        public static AudioConfig Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[Audio] " + ResourcePath + " is missing; the game will be silent.");
                return new AudioConfig();
            }

            return JsonUtility.FromJson<AudioConfig>(asset.text) ?? new AudioConfig();
        }

        public static bool IsVoice(string sound) => sound != null && sound.StartsWith("voice_", StringComparison.Ordinal);
    }
}
