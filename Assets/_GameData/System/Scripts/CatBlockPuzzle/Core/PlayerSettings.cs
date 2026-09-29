using UnityEngine;
using static CatBlockPuzzle.PuzzleSaveKeys;

namespace CatBlockPuzzle
{
    /// <summary>Sound, haptics and reduced-motion preferences, persisted through PlayerPrefs.</summary>
    internal sealed class PlayerSettings
    {
        private readonly PuzzleSession session;
        private readonly HapticsController haptics;

        internal bool soundEnabled = true;
        internal bool hapticsEnabled = true;
        internal bool reducedMotion;

        public PlayerSettings(PuzzleSession session, HapticsController haptics)
        {
            this.session = session;
            this.haptics = haptics;
        }

        public bool SoundEnabled => soundEnabled;
        public bool HapticsEnabled => hapticsEnabled;
        public bool ReducedMotion => reducedMotion;

        public void Load()
        {
            soundEnabled = PlayerPrefs.GetInt(SoundManager.SfxEnabledKey, PlayerPrefs.GetInt(SavedSound, 1)) != 0;
            hapticsEnabled = PlayerPrefs.GetInt(SavedHaptics, 1) != 0;
            reducedMotion = PlayerPrefs.GetInt(SavedReducedMotion, 0) != 0;
            Apply();
        }

        public void Apply()
        {
            SoundManager soundManager = SoundManager.EnsureInstance();
            soundEnabled = PlayerPrefs.GetInt(SoundManager.SfxEnabledKey, soundEnabled ? 1 : 0) != 0;
            soundManager.SetSfxEnabled(soundEnabled);
            soundManager.SetMusicEnabled(PlayerPrefs.GetInt(SoundManager.MusicEnabledKey, 1) != 0);

            if (haptics != null)
            {
                haptics.Enabled = hapticsEnabled;
            }
        }

        public void Save()
        {
            PlayerPrefs.SetInt(SavedSound, soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SavedHaptics, hapticsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SavedReducedMotion, reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        public void SetSound(bool value)
        {
            soundEnabled = value;
            SoundManager soundManager = SoundManager.EnsureInstance();
            soundManager.SetSfxEnabled(value);
            Save();
        }

        public void SetHaptics(bool value)
        {
            hapticsEnabled = value;
            Save();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            Save();
            if (reducedMotion)
            {
                for (int i = 0; i < session.pieces.Count; i++)
                {
                    if (session.pieces[i]?.Rect != null && !session.pieces[i].Placed)
                    {
                        session.pieces[i].Rect.anchoredPosition = Vector2.zero;
                        session.pieces[i].Rect.localEulerAngles = Vector3.zero;
                    }
                }
            }
        }
    }
}
