using System.Collections;
using UnityEngine;

namespace CatBlockPuzzle
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SoundManager : MonoBehaviour
    {
        public const string MusicEnabledKey = "CatBlockPuzzle.Settings.Music";
        public const string SfxEnabledKey = "CatBlockPuzzle.Settings.Sfx";

        public static SoundManager Instance { get; private set; }

        [SerializeField] private SoundBank bank;
        [SerializeField] private bool playMusicOnStart = true;
        [Header("Runtime Sources")]
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField] private AudioSource sfxAudioSourceA;
        [SerializeField] private AudioSource sfxAudioSourceB;
        [Tooltip("Overlapping one-shots (coin ticks) so rapid repeats never cut the stingers.")]
        [SerializeField] private AudioSource sfxOneShotSource;
        [SerializeField, Min(0f)] private float musicCrossfadeSeconds = 0.6f;

        [Header("Music Ducking")]
        [SerializeField, Range(0f, 1f)] private float duckMusicLevel = 0.35f;
        [SerializeField, Min(0f)] private float duckFadeOutSeconds = 0.15f;
        [SerializeField, Min(0f)] private float duckFadeInSeconds = 0.8f;

        private bool useFirstSfxSource = true;
        private Coroutine duckRoutine;
        private Coroutine musicSwapRoutine;
        private AudioClip musicSwapTarget;
        private MusicContext musicContext = MusicContext.Menu;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;
        public SoundBank Bank => bank;

        public static SoundManager EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            SoundManager existing = FindFirstObjectByType<SoundManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            GameObject managerObject = new GameObject("SoundManager");
            return managerObject.AddComponent<SoundManager>();
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
            EnsureAudioSources();

            if (bank == null)
            {
                bank = Resources.Load<SoundBank>("Sound/SoundBank");
            }

            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
            ApplyVolumes();
        }

        private void Start()
        {
            if (playMusicOnStart)
            {
                PlayMusic();
            }
        }

        public void Configure(SoundBank soundBank)
        {
            bank = soundBank;
            ApplyVolumes();
            if (playMusicOnStart && isActiveAndEnabled)
            {
                PlayMusic();
            }
        }

        public void PlaySfx(string id)
        {
            if (!SfxEnabled || bank == null || !bank.TryGet(id, out SoundBank.SoundEntry entry))
            {
                return;
            }

            PlaySfx(entry.Clip, entry.Volume);
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (!SfxEnabled || clip == null)
            {
                return;
            }

            EnsureAudioSources();
            AudioSource source;
            if (!sfxAudioSourceA.isPlaying)
            {
                source = sfxAudioSourceA;
            }
            else if (!sfxAudioSourceB.isPlaying)
            {
                source = sfxAudioSourceB;
            }
            else
            {
                source = useFirstSfxSource ? sfxAudioSourceA : sfxAudioSourceB;
                source.Stop();
            }

            useFirstSfxSource = source != sfxAudioSourceA;
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume) * (bank != null ? bank.MasterVolume : 1f);
            source.Play();
        }

        /// <summary>Plays a short SFX on its own channel, overlapping earlier ones instead of cutting them.</summary>
        public void PlaySfxOneShot(string id)
        {
            if (!SfxEnabled || bank == null || !bank.TryGet(id, out SoundBank.SoundEntry entry) || entry.Clip == null)
            {
                return;
            }

            EnsureAudioSources();
            sfxOneShotSource.PlayOneShot(entry.Clip, Mathf.Clamp01(entry.Volume) * bank.MasterVolume);
        }

        /// <summary>Switches between the menu and gameplay music, crossfading when the track changes.</summary>
        public void SetMusicContext(MusicContext context)
        {
            musicContext = context;
            if (!MusicEnabled || bank == null || CurrentTrack == null) return;
            EnsureAudioSources();
            AudioClip target = CurrentTrack;
            if (musicSwapRoutine != null)
            {
                // Already fading towards this track: let it finish. Otherwise abandon that swap.
                if (musicSwapTarget == target) return;
                StopCoroutine(musicSwapRoutine);
                musicSwapRoutine = null;
            }

            if (bgmAudioSource.clip == target) { PlayMusic(); return; }
            if (!bgmAudioSource.isPlaying || !isActiveAndEnabled || musicCrossfadeSeconds <= 0f)
            {
                PlayMusic();
                return;
            }

            CancelMusicDuck();
            musicSwapTarget = target;
            musicSwapRoutine = StartCoroutine(SwapMusicRoutine(target));
        }

        private AudioClip CurrentTrack => bank != null ? bank.MusicFor(musicContext) : null;

        private IEnumerator SwapMusicRoutine(AudioClip next)
        {
            float half = musicCrossfadeSeconds * 0.5f;
            yield return FadeMusicTo(0f, half);
            bgmAudioSource.Stop();
            bgmAudioSource.clip = next;
            bgmAudioSource.volume = 0f;
            if (MusicEnabled) bgmAudioSource.Play();
            yield return FadeMusicTo(FullMusicVolume, half);
            musicSwapRoutine = null;
            musicSwapTarget = null;
        }

        /// <summary>
        /// Plays an SFX and fades the background music down while it plays, then fades it back up.
        /// Used for the level complete and level fail stingers so they sit on top of the music.
        /// </summary>
        public void PlaySfxWithMusicDuck(string id)
        {
            if (!SfxEnabled || bank == null || !bank.TryGet(id, out SoundBank.SoundEntry entry) || entry.Clip == null)
            {
                return;
            }

            PlaySfx(entry.Clip, entry.Volume);
            DuckMusic(entry.Clip.length);
        }

        public void DuckMusic(float holdSeconds)
        {
            if (!MusicEnabled || bgmAudioSource == null || !bgmAudioSource.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            if (duckRoutine != null)
            {
                StopCoroutine(duckRoutine);
            }

            duckRoutine = StartCoroutine(DuckMusicRoutine(Mathf.Max(0f, holdSeconds)));
        }

        private IEnumerator DuckMusicRoutine(float holdSeconds)
        {
            float fullVolume = FullMusicVolume;
            float duckedVolume = fullVolume * duckMusicLevel;

            yield return FadeMusicTo(duckedVolume, duckFadeOutSeconds);

            // Keep the music ducked for the rest of the stinger, then ease back up.
            float hold = Mathf.Max(0f, holdSeconds - duckFadeOutSeconds);
            if (hold > 0f)
            {
                yield return new WaitForSecondsRealtime(hold);
            }

            yield return FadeMusicTo(FullMusicVolume, duckFadeInSeconds);
            duckRoutine = null;
        }

        private IEnumerator FadeMusicTo(float target, float duration)
        {
            float start = bgmAudioSource.volume;
            if (duration <= 0f || Mathf.Approximately(start, target))
            {
                bgmAudioSource.volume = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmAudioSource.volume = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            bgmAudioSource.volume = target;
        }

        private float FullMusicVolume => (bank != null ? bank.MusicVolume : 1f) * (bank != null ? bank.MasterVolume : 1f);

        private void CancelMusicDuck()
        {
            if (duckRoutine != null)
            {
                StopCoroutine(duckRoutine);
                duckRoutine = null;
            }
        }

        public void PlayMusic()
        {
            if (!MusicEnabled || bank == null || CurrentTrack == null)
            {
                return;
            }

            EnsureAudioSources();
            if (bgmAudioSource.clip != CurrentTrack)
            {
                bgmAudioSource.Stop();
                bgmAudioSource.clip = CurrentTrack;
            }

            bgmAudioSource.loop = true;
            if (duckRoutine == null && musicSwapRoutine == null)
            {
                bgmAudioSource.volume = bank.MusicVolume * bank.MasterVolume;
            }

            if (!bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Play();
            }
        }

        public void StopMusic()
        {
            CancelMusicDuck();
            if (musicSwapRoutine != null) { StopCoroutine(musicSwapRoutine); musicSwapRoutine = null; musicSwapTarget = null; }
            if (bgmAudioSource != null)
            {
                bgmAudioSource.Stop();
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (enabled) PlayMusic(); else StopMusic();
        }

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        private void EnsureAudioSources()
        {
            bgmAudioSource = EnsureChildSource("BGM Audio", bgmAudioSource);
            sfxAudioSourceA = EnsureChildSource("SFX Audio A", sfxAudioSourceA);
            sfxAudioSourceB = EnsureChildSource("SFX Audio B", sfxAudioSourceB);
            sfxOneShotSource = EnsureChildSource("SFX One Shots", sfxOneShotSource);
            bgmAudioSource.loop = true;
            if (GetComponent<ButtonPressSfx>() == null) gameObject.AddComponent<ButtonPressSfx>();
        }

        private AudioSource EnsureChildSource(string childName, AudioSource current)
        {
            if (current != null)
            {
                return current;
            }

            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            AudioSource source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        private void ApplyVolumes()
        {
            CancelMusicDuck();
            float master = bank != null ? bank.MasterVolume : 1f;
            if (bgmAudioSource != null)
            {
                bgmAudioSource.mute = !MusicEnabled;
                bgmAudioSource.volume = (bank != null ? bank.MusicVolume : 1f) * master;
            }

            if (sfxAudioSourceA != null) sfxAudioSourceA.mute = !SfxEnabled;
            if (sfxAudioSourceB != null) sfxAudioSourceB.mute = !SfxEnabled;
            if (sfxOneShotSource != null) sfxOneShotSource.mute = !SfxEnabled;
        }
    }
}
