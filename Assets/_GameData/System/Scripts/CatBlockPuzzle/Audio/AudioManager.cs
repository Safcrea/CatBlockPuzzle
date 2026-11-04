using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The game's one audio entry point. Preloads every sound listed in audio_config.json, plays them through a
    /// pool of sources (short UI sounds overlap), keeps a single voice at a time, and saves the master on/off switch.
    /// Call <see cref="PlayEvent"/> with an <see cref="AudioEvents"/> name, or <see cref="Play"/> with a sound name.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AudioManager : MonoBehaviour
    {
        public const string SfxEnabledKey = "CatBlockPuzzle.Settings.Sfx";
        public const string MusicEnabledKey = "CatBlockPuzzle.Settings.Music";
        private const int MinimumPraiseLines = 2;
        private const int SoundTestFingers = 4;

        public static AudioManager Instance { get; private set; }

        private readonly Dictionary<string, AudioConfig.SoundEntry> sounds = new Dictionary<string, AudioConfig.SoundEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> eventSounds = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> warned = new HashSet<string>();
        private AudioConfig config = new AudioConfig();
        private AudioSource[] pool;
        private AudioSource voiceSource;
        private int nextPoolIndex;
        private string lastVoice;
        private Coroutine praiseRoutine;
        private SoundTestPanel soundTest;
        private bool soundTestKeyHeld;

        public bool SfxEnabled { get; private set; } = true;
        /// <summary>There is no background music; the flag is only kept so the Settings toggle keeps its saved value.</summary>
        public bool MusicEnabled { get; private set; } = true;
        internal AudioConfig Config => config;

        public static AudioManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            AudioManager existing = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            return new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            config = AudioConfig.Load();
            SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            BuildPool(Mathf.Max(2, config.maxOverlappingSounds));
            Preload();
        }

        private void BuildPool(int size)
        {
            pool = new AudioSource[size];
            for (int i = 0; i < size; i++) pool[i] = CreateSource("Sfx " + i);
            voiceSource = CreateSource("Voice");
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void Preload()
        {
            foreach (AudioConfig.SoundEntry entry in config.sounds)
            {
                if (entry == null || string.IsNullOrEmpty(entry.name)) continue;
                sounds[entry.name] = entry;
                if (entry.status == "missing") continue;
                AudioClip clip = Resources.Load<AudioClip>(AudioPath(entry.file));
                if (clip == null)
                {
                    Warn("clip '" + entry.file + "' for sound '" + entry.name + "' was not found");
                    continue;
                }

                clip.LoadAudioData();
                clips[entry.name] = clip;
            }

            foreach (AudioConfig.EventEntry entry in config.events)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.eventName)) eventSounds[entry.eventName] = entry.sound;
            }
        }

        private static string AudioPath(string file) => "CatBlockPuzzle/Audio/" + file;

        private void Warn(string message)
        {
            if (warned.Add(message)) Debug.LogWarning("[Audio] " + message);
        }

        // ---- Playing ----------------------------------------------------------------------------------------

        /// <summary>Plays the sound mapped to a game event; unmapped events and missing sounds are skipped silently.</summary>
        public void PlayEvent(string eventName, float volumeScale = 1f)
        {
            if (!eventSounds.TryGetValue(eventName, out string sound) || string.IsNullOrEmpty(sound))
            {
                Warn("event '" + eventName + "' has no sound in the config");
                return;
            }

            Play(sound, 0f, volumeScale);
        }

        /// <summary>Plays a sound by name. Voices cut off the previous voice so only one speaks at a time.</summary>
        public void Play(string soundName) => Play(soundName, 0f, 1f);

        /// <summary>Plays an event after a delay, for sounds that belong to a later beat of an animation.</summary>
        public void PlayEventDelayed(string eventName, float delaySeconds) => StartCoroutine(EventAfterDelay(eventName, delaySeconds));

        private IEnumerator EventAfterDelay(string eventName, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySeconds));
            PlayEvent(eventName);
        }

        private void Play(string soundName, float delaySeconds, float volumeScale)
        {
            if (!SfxEnabled) return;
            if (!clips.TryGetValue(soundName ?? string.Empty, out AudioClip clip))
            {
                Warn("sound '" + soundName + "' is not available");
                return;
            }

            AudioConfig.SoundEntry entry = sounds[soundName];
            AudioSource source;
            if (AudioConfig.IsVoice(soundName))
            {
                source = voiceSource;
                source.Stop();
                lastVoice = soundName;
            }
            else
            {
                source = NextPooledSource();
                source.Stop();
            }

            source.clip = clip;
            source.volume = Mathf.Clamp01(entry.volume * volumeScale);
            if (delaySeconds > 0f) source.PlayDelayed(delaySeconds);
            else source.Play();
        }

        // An idle source first; when every source is busy the longest-running one is taken over.
        private AudioSource NextPooledSource()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                AudioSource candidate = pool[(nextPoolIndex + i) % pool.Length];
                if (!candidate.isPlaying)
                {
                    nextPoolIndex = (nextPoolIndex + i + 1) % pool.Length;
                    return candidate;
                }
            }

            AudioSource oldest = pool[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % pool.Length;
            return oldest;
        }

        public void Stop(string soundName)
        {
            if (!clips.TryGetValue(soundName ?? string.Empty, out AudioClip clip)) return;
            foreach (AudioSource source in pool) if (source.clip == clip && source.isPlaying) source.Stop();
            if (voiceSource.clip == clip && voiceSource.isPlaying) voiceSource.Stop();
        }

        public void StopAll()
        {
            foreach (AudioSource source in pool) source.Stop();
            voiceSource.Stop();
            if (praiseRoutine != null) { StopCoroutine(praiseRoutine); praiseRoutine = null; }
        }

        /// <summary>
        /// A streak or clear of <paramref name="lines"/>: the matching combo sound (instead of the single-line clear),
        /// then a praise voice about 150 ms later. A streak of one plays only the single-line clear and no voice.
        /// </summary>
        public void PlayCombo(int lines, bool withPraise = true)
        {
            if (lines < 1) return;
            if (lines == 1) { PlayEvent(AudioEvents.RowClear); return; }
            PlayEvent(lines == 2 ? AudioEvents.Combo2 : lines == 3 ? AudioEvents.Combo3 : AudioEvents.Combo4);
            if (lines >= 4) PlayEvent(AudioEvents.CatSurprised);
            if (withPraise) PlayPraise(lines);
        }

        /// <summary>One random voice from the pool for this many lines, never the same word twice in a row.</summary>
        public void PlayPraise(int lines)
        {
            if (!SfxEnabled || lines < MinimumPraiseLines) return;
            string voice = PickVoice(lines);
            if (voice == null) return;
            if (praiseRoutine != null) StopCoroutine(praiseRoutine);
            praiseRoutine = StartCoroutine(PraiseAfterDelay(voice));
        }

        private IEnumerator PraiseAfterDelay(string voice)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, config.voiceDelaySeconds));
            praiseRoutine = null;
            Play(voice);
        }

        private string PickVoice(int lines)
        {
            AudioConfig.PraisePool pool = null;
            foreach (AudioConfig.PraisePool candidate in config.praise)
            {
                if (candidate == null || candidate.sounds == null) continue;
                if (candidate.lines <= lines && (pool == null || candidate.lines > pool.lines)) pool = candidate;
            }

            if (pool == null) return null;
            var usable = new List<string>();
            foreach (string name in pool.sounds) if (clips.ContainsKey(name) && name != lastVoice) usable.Add(name);
            if (usable.Count == 0)
            {
                foreach (string name in pool.sounds) if (clips.ContainsKey(name)) usable.Add(name);
            }

            return usable.Count == 0 ? null : usable[UnityEngine.Random.Range(0, usable.Count)];
        }

        // ---- Settings ---------------------------------------------------------------------------------------

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) StopAll();
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ---- Sound Test -------------------------------------------------------------------------------------

        internal IEnumerable<AudioConfig.SoundEntry> AllSounds => config.sounds;
        internal bool HasClip(string soundName) => clips.ContainsKey(soundName);
        internal float ClipLength(string soundName) => clips.TryGetValue(soundName, out AudioClip clip) ? clip.length : 0f;

        /// <summary>The config status, plus an automatic "review" for anything shorter than 0.2 s or longer than 1.0 s.</summary>
        internal string EffectiveStatus(AudioConfig.SoundEntry entry)
        {
            if (!clips.TryGetValue(entry.name, out AudioClip clip)) return "missing";
            if (entry.status == "ok" && (clip.length < 0.2f || clip.length > 1f)) return "review";
            return entry.status;
        }

        /// <summary>Plays a sound even when the master switch is off, so it can be auditioned.</summary>
        internal void Audition(string soundName)
        {
            bool was = SfxEnabled;
            SfxEnabled = true;
            Play(soundName);
            SfxEnabled = was;
        }

        public void ToggleSoundTest()
        {
            if (soundTest == null) soundTest = gameObject.AddComponent<SoundTestPanel>();
            soundTest.Visible = !soundTest.Visible;
        }

        // F9 in the Editor, or four fingers at once on a device, opens the Sound Test (development builds only).
        private void Update()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            bool pressed = Keyboard.current != null && Keyboard.current.f9Key.isPressed;
            Touchscreen screen = Touchscreen.current;
            if (screen != null)
            {
                int down = 0;
                foreach (var touch in screen.touches) if (touch.isInProgress) down++;
                pressed |= down >= SoundTestFingers;
            }

            if (pressed && !soundTestKeyHeld) ToggleSoundTest();
            soundTestKeyHeld = pressed;
        }
    }
}
