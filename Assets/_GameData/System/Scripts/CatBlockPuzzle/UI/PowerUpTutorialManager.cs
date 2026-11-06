using AVNEvents;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Introduces the power-ups with the <see cref="TutorialSpotlight"/>: the screen dims except a hole over the button, a ring
    /// pulses, the button bounces and a hand pokes it. Hint and Freeze are taught the first time their level is reached (the tap
    /// is remembered and spent once the level starts); the Hammer is taught the first time something is actually in the player's
    /// way (see <see cref="ShowNeed"/>), because that is when it makes sense.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PowerUpTutorialManager : MonoBehaviour
    {
        public const string HintSeenKey = "CatBlockPuzzle.Tutorial.Hint";
        public const string FreezeSeenKey = "CatBlockPuzzle.Tutorial.Freeze";
        public const string RotateSeenKey = "CatBlockPuzzle.Tutorial.Rotate";
        public const string HammerSeenKey = "CatBlockPuzzle.Tutorial.Hammer";
        public const string TimerSeenKey = "CatBlockPuzzle.Tutorial.Timer";
        private const int TimerTutorialLevel = 11;

        private TutorialSpotlight overlay;
        // Every power-up taught in one level start waits here, so none of the taps is lost.
        private readonly System.Collections.Generic.Queue<PowerUpKind> pendingUses = new System.Collections.Generic.Queue<PowerUpKind>();

        public bool IsOpen => overlay != null;

        /// <summary>The power-up whose tutorial tap still has to be spent, if any; clears it.</summary>
        public bool TryTakePendingUse(out PowerUpKind kind)
        {
            kind = default;
            if (pendingUses.Count == 0) return false;
            kind = pendingUses.Dequeue();
            return true;
        }

        public IEnumerator ShowUnlockedTutorials(GameplayHudView hud, float freezeSeconds)
        {
            while (GameSystem.Instance != null && (GameSystem.Instance.IsPaused || GameSystem.Instance.IsSettingsOpen))
                yield return null;
            if (GameSystem.Instance != null && !GameSystem.Instance.IsGameplayOpen) yield break;
            int shownLevel = GameSystem.Instance != null ? GameSystem.Instance.DisplayLevelNumber : 1;
            // Level 11 is the first timed level, so it always explains the clock, even on a retry or with an old save.
            // Any later timed level only shows it if the player has somehow never seen it.
            if (!DifficultyProfile.IsUntimed(shownLevel) && (shownLevel == TimerTutorialLevel || PlayerPrefs.GetInt(TimerSeenKey, 0) == 0))
                yield return ShowTimerTutorial(hud);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Hint) && PlayerPrefs.GetInt(HintSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Hint, "STUCK?\nTAP HINT!", HintSeenKey);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Freeze) && PlayerPrefs.GetInt(FreezeSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Freeze, "FREEZE THE CLOCK\nFOR " + freezeSeconds.ToString("0.#") + "S!", FreezeSeenKey);
            // The Hammer arrives on the first level that has something for it to clear (padlocks or fog), and the tap swings it.
            if (hud.IsPowerUpUnlocked(PowerUpKind.Hammer) && PlayerPrefs.GetInt(HammerSeenKey, 0) == 0 &&
                GameSystem.Instance != null && GameSystem.Instance.HammerHasTarget)
                yield return Show(hud, PowerUpKind.Hammer, GameSystem.Instance.HammerIntroLabel, HammerSeenKey);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Rotate) && PlayerPrefs.GetInt(RotateSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Rotate, "TURN A CAT!\nTAP ROTATE!", RotateSeenKey);
        }

        // The first timed level (level 11): the clock is spotlit for a couple of seconds, then the level carries on by itself.
        private IEnumerator ShowTimerTutorial(GameplayHudView hud)
        {
            RectTransform panel = hud.timerPanel;
            if (panel == null || hud.Canvas == null || !panel.gameObject.activeInHierarchy)
            {
                PlayerPrefs.SetInt(TimerSeenKey, 1);
                yield break;
            }

            overlay = TutorialSpotlight.Create(hud.Canvas, hud.levelText != null ? hud.levelText.font : null);
            if (overlay == null) { PlayerPrefs.SetInt(TimerSeenKey, 1); yield break; }

            AvnEvents.TutorialShown("timer");
            yield return overlay.Run(panel, "BEAT THE CLOCK!\nSTARS FADE AS TIME RUNS OUT", null, false, 2.6f);
            PlayerPrefs.SetInt(TimerSeenKey, 1);
            PlayerPrefs.Save();
            AvnEvents.TutorialCompleted("timer");
            overlay = null;
        }

        private IEnumerator Show(GameplayHudView hud, PowerUpKind kind, string message, string key)
        {
            Button button = hud.PowerUpButton(kind);
            // Unlocking gives the player exactly "Free On Unlock" power-ups (Inspector, Gameplay HUD). The tutorial's
            // demo tap is free, so the HUD shows that number as soon as the power-up unlocks and keeps showing it.
            void MarkSeenAndGrant()
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                // The Hammer's tap swings it for real (it is spent), so one extra leaves the player the usual number.
                int grant = hud.FreeOnUnlock + (kind == PowerUpKind.Hammer ? 1 : 0);
                if (grant > 0)
                {
                    PowerUpInventory.Add(kind, grant);
                    GameAnalytics.ItemEarned(kind, grant, GameAnalytics.EarnTutorialGrant);
                    AvnEvents.PowerupObtained(kind, grant, PowerupSource.Gift);
                }
                hud.RefreshPowerUpCounts();
            }

            if (button == null || !button.gameObject.activeInHierarchy || hud.Canvas == null)
            {
                MarkSeenAndGrant();
                yield break;
            }

            overlay = TutorialSpotlight.Create(hud.Canvas, hud.levelText != null ? hud.levelText.font : null);
            if (overlay == null)
            {
                MarkSeenAndGrant();
                yield break;
            }

            MarkSeenAndGrant();
            GameAnalytics.PowerUpTutorialShown(kind);
            AvnEvents.TutorialShown(AvnEvents.PowerupTutorial(kind));
            yield return overlay.Run((RectTransform)button.transform, message, hud.HandSprite, true, 0f);
            if (overlay == null) { AvnEvents.TutorialInterrupted(AvnEvents.PowerupTutorial(kind)); yield break; }
            pendingUses.Enqueue(kind);
            GameAnalytics.PowerUpTutorialTapped(kind);
            AvnEvents.TutorialCompleted(AvnEvents.PowerupTutorial(kind));
            overlay = null;
        }

        /// <summary>
        /// Teaches a power-up at the moment it is needed (the Hammer, once cats, padlocks or fog are in the way): the free gift
        /// is handed over first, the button is spotlit, and <paramref name="onTapped"/> runs when the player presses it.
        /// </summary>
        public IEnumerator ShowNeed(GameplayHudView hud, PowerUpKind kind, string message, string key, int gift, Action onTapped)
        {
            Button button = hud.PowerUpButton(kind);
            if (button == null || !button.gameObject.activeInHierarchy || hud.Canvas == null || overlay != null) yield break;
            overlay = TutorialSpotlight.Create(hud.Canvas, hud.levelText != null ? hud.levelText.font : null);
            if (overlay == null) yield break;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            if (gift > 0)
            {
                PowerUpInventory.Add(kind, gift);
                GameAnalytics.ItemEarned(kind, gift, GameAnalytics.EarnTutorialGrant);
                AvnEvents.PowerupObtained(kind, gift, PowerupSource.Gift);
            }
            hud.RefreshPowerUpCounts();
            GameAnalytics.PowerUpTutorialShown(kind);
            AvnEvents.TutorialShown(AvnEvents.PowerupTutorial(kind));
            yield return overlay.Run((RectTransform)button.transform, message, hud.HandSprite, true, 0f);
            if (overlay == null) { AvnEvents.TutorialInterrupted(AvnEvents.PowerupTutorial(kind)); yield break; }
            overlay = null;
            GameAnalytics.PowerUpTutorialTapped(kind);
            AvnEvents.TutorialCompleted(AvnEvents.PowerupTutorial(kind));
            onTapped?.Invoke();
        }

        /// <summary>Aborts any running introduction (level change, navigation) and forgets its pending tap.</summary>
        public void Cancel()
        {
            AvnEvents.TutorialsInterrupted("powerup_");
            StopAllCoroutines();
            pendingUses.Clear();
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            if (overlay != null) overlay.Cancel();
            overlay = null;
        }

        private void OnDisable() => Cancel();
    }
}
