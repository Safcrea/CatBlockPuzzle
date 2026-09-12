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

        private bool useFirstSfxSource = true;

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

        public void PlayMusic()
        {
            if (!MusicEnabled || bank == null || bank.MusicTrack == null)
            {
                return;
            }

            EnsureAudioSources();
            if (bgmAudioSource.clip != bank.MusicTrack)
            {
                bgmAudioSource.clip = bank.MusicTrack;
            }

            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bank.MusicVolume * bank.MasterVolume;
            if (!bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Play();
            }
        }

        public void StopMusic()
        {
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
            bgmAudioSource.loop = true;
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
            float master = bank != null ? bank.MasterVolume : 1f;
            if (bgmAudioSource != null)
            {
                bgmAudioSource.mute = !MusicEnabled;
                bgmAudioSource.volume = (bank != null ? bank.MusicVolume : 1f) * master;
            }

            if (sfxAudioSourceA != null) sfxAudioSourceA.mute = !SfxEnabled;
            if (sfxAudioSourceB != null) sfxAudioSourceB.mute = !SfxEnabled;
        }
    }
}
