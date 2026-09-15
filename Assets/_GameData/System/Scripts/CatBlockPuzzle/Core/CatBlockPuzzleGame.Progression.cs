using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private enum CatMood
        {
            Neutral = 0,
            Happy = 1,
            Worried = 2
        }

        private void LoadPreferences()
        {
            soundEnabled = PlayerPrefs.GetInt(SoundManager.SfxEnabledKey, PlayerPrefs.GetInt(SavedSoundKey, 1)) != 0;
            hapticsEnabled = PlayerPrefs.GetInt(SavedHapticsKey, 1) != 0;
            reducedMotion = PlayerPrefs.GetInt(SavedReducedMotionKey, 0) != 0;
            ApplyPreferences();
        }

        private void ApplyPreferences()
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

        private void SavePreferences()
        {
            PlayerPrefs.SetInt(SavedSoundKey, soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SavedHapticsKey, hapticsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SavedReducedMotionKey, reducedMotion ? 1 : 0);
            PlayerPrefs.Save();
            ApplyPreferences();
        }

        private void SetSound(bool value)
        {
            soundEnabled = value;
            SoundManager soundManager = SoundManager.EnsureInstance();
            soundManager.SetSfxEnabled(value);
            SavePreferences();
        }

        private void SetHaptics(bool value)
        {
            hapticsEnabled = value;
            SavePreferences();
        }

        private void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            SavePreferences();
            if (reducedMotion)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i]?.Rect != null && !pieces[i].Placed)
                    {
                        pieces[i].Rect.anchoredPosition = Vector2.zero;
                        pieces[i].Rect.localEulerAngles = Vector3.zero;
                    }
                }
            }
        }

        private void OpenPause()
        {
            GameSystem system = GameSystem.EnsureForScene(this);
            if (system != null)
            {
                system.OpenPause();
                return;
            }

        }

        private void OpenSettings()
        {
            GameSystem system = GameSystem.EnsureForScene(this);
            if (system != null)
            {
                system.OpenSettings(false);
                return;
            }

        }

        private void CloseSettings()
        {
            GameSystem.Instance?.ResumeGame();
        }

        private int GetBestStars(string levelId)
        {
            return PlayerPrefs.GetInt(SavedBestStarsPrefix + levelId, 0);
        }

        private LevelResult SaveLevelResult()
        {
            earnedStars = CatPuzzleResultCalculator.CalculateStars(levelRemainingSeconds, LevelDurationSeconds);
            int previousBest = GetBestStars(activeLevel.Id);
            int best = Mathf.Max(previousBest, earnedStars);
            if (levelNavigationTesting)
            {
                return new LevelResult(activeLevel.Id, LevelDurationSeconds - levelRemainingSeconds, earnedStars, best);
            }

            PlayerPrefs.SetInt(SavedBestStarsPrefix + activeLevel.Id, best);
            PlayerPrefs.Save();
            return new LevelResult(activeLevel.Id, LevelDurationSeconds - levelRemainingSeconds, earnedStars, best);
        }

        private void UpdateStarDisplay()
        {
            int visibleStars = CatPuzzleResultCalculator.CalculateStars(levelRemainingSeconds, LevelDurationSeconds);
            for (int i = 0; i < progressStars.Length; i++)
            {
                Image star = progressStars[i];
                if (star == null)
                {
                    continue;
                }

                bool filled = i < visibleStars;
                star.sprite = filled ? starSprite : starOutlineSprite;
                star.color = gameplayHud.UsesAuthoredLayout ? (filled ? Color.white : new Color(.55f,.55f,.55f)) : filled ? GoldColor : new Color(0.64f, 0.52f, 0.42f, 0.48f);
            }
        }

        private void RegisterValidPlacement(PieceState state, bool countForCombo)
        {
            SetCatMood(state, CatMood.Happy);
            StartCoroutine(RestoreCatMood(state, 0.42f));
            if (!countForCombo)
            {
                return;
            }

            comboCount++;
            bestCombo = Mathf.Max(bestCombo, comboCount);
            UpdateComboDisplay(true);
            if ((comboCount == 3 || comboCount == 5 || comboCount == 8) && comboBadge != null)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, comboBadge.position);
                SpawnFixedBurst(screen, reducedMotion ? 4 : 12, comboCount >= 5 ? GoldColor : ValidColor);
            }
        }

        private void ResetCombo()
        {
            comboCount = 0;
            UpdateComboDisplay(false);
        }

        private void UpdateComboDisplay(bool animate)
        {
            if (comboBadge == null || comboText == null)
            {
                return;
            }

            bool visible = comboCount >= 3;
            comboBadge.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            comboText.text = comboCount + "x  PURRFECT";
            if (animate && !reducedMotion)
            {
                StartCoroutine(PopTransform(comboBadge, 1.1f));
            }
        }

        private void ShowWorriedMood(PieceState state)
        {
            ResetCombo();
            SetCatMood(state, CatMood.Worried);
            StartCoroutine(RestoreCatMood(state, 0.58f));
        }

        private IEnumerator RestoreCatMood(PieceState state, float delay)
        {
            yield return new WaitForSecondsRealtime(reducedMotion ? Mathf.Max(0.28f, delay * 0.5f) : delay);
            if (state != null && state.Rect != null && !inputLocked && (levelCompleteScreen == null || !levelCompleteScreen.IsOpen))
            {
                SetCatMood(state, CatMood.Neutral);
            }
        }

        private void SetCatMood(PieceState state, CatMood mood)
        {
            if (state == null)
            {
                return;
            }

            Sprite sprite = CatPortrait(mood, state.AtlasIndex);
            if (sprite == null)
            {
                return;
            }

            for (int i = 0; i < state.CatViews.Count; i++)
            {
                if (state.CatViews[i].Portrait != null)
                {
                    state.CatViews[i].Portrait.sprite = sprite;
                }
            }
        }

        private Sprite CatPortrait(CatMood mood, int atlasIndex)
        {
            int index = Mathf.Abs(atlasIndex) % 8;
            Sprite requested = catPortraitSprites[(int)mood * 8 + index];
            return requested != null ? requested : catPortraitSprites[index];
        }
    }
}
