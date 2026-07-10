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
            soundEnabled = PlayerPrefs.GetInt(SavedSoundKey, 1) != 0;
            hapticsEnabled = PlayerPrefs.GetInt(SavedHapticsKey, 1) != 0;
            reducedMotion = PlayerPrefs.GetInt(SavedReducedMotionKey, 0) != 0;
            ApplyPreferences();
        }

        private void ApplyPreferences()
        {
            if (audioSource != null)
            {
                audioSource.mute = !soundEnabled;
            }

            if (haptics != null)
            {
                haptics.Enabled = hapticsEnabled;
            }

            if (soundToggleText != null)
            {
                soundToggleText.text = soundEnabled ? "On" : "Off";
            }

            UpdateToggleVisual(soundToggle, soundToggleKnob, soundEnabled);

            if (hapticsToggleText != null)
            {
                hapticsToggleText.text = hapticsEnabled ? "On" : "Off";
            }

            UpdateToggleVisual(hapticsToggle, hapticsToggleKnob, hapticsEnabled);

            if (motionToggleText != null)
            {
                motionToggleText.text = reducedMotion ? "On" : "Off";
            }

            UpdateToggleVisual(motionToggle, motionToggleKnob, reducedMotion);
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

        private void UpdateToggleVisual(Toggle toggle, RectTransform knob, bool value)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
                toggle.targetGraphic.color = value ? TargetDeepColor : new Color(0.76f, 0.7f, 0.65f, 1f);
            }

            if (knob != null)
            {
                knob.anchoredPosition = new Vector2(value ? 24f : -24f, 0f);
            }
        }

        private void OpenPause()
        {
            OpenSettings("PAUSED");
        }

        private void OpenSettings()
        {
            OpenSettings("SETTINGS");
        }

        private void OpenSettings(string title)
        {
            if (settingsOverlay == null || levelFailed || winOverlay.gameObject.activeSelf || failOverlay.gameObject.activeSelf)
            {
                return;
            }

            timerWasRunningBeforeSettings = timerRunning;
            timerRunning = false;
            CancelActiveDragToRest();
            inputLocked = true;
            settingsTitleText.text = title;
            settingsOverlay.gameObject.SetActive(true);
            settingsPanel.localScale = reducedMotion ? Vector3.one : Vector3.one * 0.9f;
            if (!reducedMotion)
            {
                StartCoroutine(PopTransform(settingsPanel, 1.025f));
            }

            ApplyPreferences();
        }

        private void CloseSettings()
        {
            if (settingsOverlay == null)
            {
                return;
            }

            settingsOverlay.gameObject.SetActive(false);
            inputLocked = false;
            timerRunning = timerWasRunningBeforeSettings && !levelFailed;
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
                star.color = filled ? GoldColor : new Color(0.64f, 0.52f, 0.42f, 0.48f);
            }
        }

        private void ShowWinResult(LevelResult result)
        {
            for (int i = 0; i < winStars.Length; i++)
            {
                if (winStars[i] == null)
                {
                    continue;
                }

                bool filled = i < result.Stars;
                winStars[i].sprite = filled ? starSprite : starOutlineSprite;
                winStars[i].color = filled ? GoldColor : new Color(0.62f, 0.5f, 0.4f, 0.42f);
                winStars[i].rectTransform.localScale = Vector3.one;
            }

            winBestText.text = result.BestStars > result.Stars
                ? "Best: " + result.BestStars + " stars"
                : "New best: " + result.BestStars + " stars";
            if (bestCombo >= 3)
            {
                winBestText.text += "  |  Combo " + bestCombo + "x";
            }

            bool unlock = (levelIndex + 1) % 5 == 0;
            winUnlockText.gameObject.SetActive(unlock);
            winCatImage.gameObject.SetActive(unlock);
            if (unlock)
            {
                int catIndex = (((levelIndex + 1) / 5) - 1) % 8;
                winCatImage.sprite = CatPortrait(CatMood.Happy, catIndex);
                winUnlockText.text = "New cat friend unlocked";
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
            if (state != null && state.Rect != null && !inputLocked && !winOverlay.gameObject.activeSelf)
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
            Sprite requested = catPortraitSprites[(int)mood, index];
            return requested != null ? requested : catPortraitSprites[(int)CatMood.Neutral, index];
        }
    }
}
