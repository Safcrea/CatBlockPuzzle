using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Introduces each power-up the first time it unlocks, with the guided sequence from ZombieRush
    /// (<see cref="PowerUpUnlockTutorial"/>): dim, icon pops in with a message, flies onto the real
    /// HUD button (which unlocks and gets a free charge), a hand points at it, and the player taps it.
    /// That tap is remembered and spent for real once the level starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PowerUpTutorialManager : MonoBehaviour
    {
        public const string HintSeenKey = "CatBlockPuzzle.Tutorial.Hint";
        public const string FreezeSeenKey = "CatBlockPuzzle.Tutorial.Freeze";
        public const string RotateSeenKey = "CatBlockPuzzle.Tutorial.Rotate";

        private PowerUpUnlockTutorial overlay;
        private PowerUpKind? pendingUse;

        public bool IsOpen => overlay != null;

        /// <summary>The power-up whose tutorial tap still has to be spent, if any; clears it.</summary>
        public bool TryTakePendingUse(out PowerUpKind kind)
        {
            kind = pendingUse ?? default;
            bool has = pendingUse.HasValue;
            pendingUse = null;
            return has;
        }

        public IEnumerator ShowUnlockedTutorials(GameplayHudView hud, float freezeSeconds)
        {
            while (GameSystem.Instance != null && (GameSystem.Instance.IsPaused || GameSystem.Instance.IsSettingsOpen))
                yield return null;
            if (GameSystem.Instance != null && !GameSystem.Instance.IsGameplayOpen) yield break;
            if (hud.IsPowerUpUnlocked(PowerUpKind.Hint) && PlayerPrefs.GetInt(HintSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Hint, "Stuck?\nTap Hint!", HintSeenKey);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Freeze) && PlayerPrefs.GetInt(FreezeSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Freeze, $"Freeze time for {freezeSeconds:0.#}s!\nTap Freeze!", FreezeSeenKey);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Rotate) && PlayerPrefs.GetInt(RotateSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Rotate, "Turn a cat!\nTap Rotate!", RotateSeenKey);
        }

        private IEnumerator Show(GameplayHudView hud, PowerUpKind kind, string message, string key)
        {
            Button button = hud.PowerUpButton(kind);
            // Unlocking gives the player "Free On Unlock" power-ups to keep (Inspector, Gameplay HUD). When the
            // introduction runs, its tap spends one more straight away, so that one is granted on top.
            void MarkSeenAndGrant(bool demoWillSpendOne)
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                int grant = hud.FreeOnUnlock + (demoWillSpendOne ? 1 : 0);
                if (grant > 0)
                {
                    PowerUpInventory.Add(kind, grant);
                    GameAnalytics.ItemEarned(kind, grant, GameAnalytics.EarnTutorialGrant);
                }
                hud.RefreshPowerUpCounts();
            }

            if (button == null || !button.gameObject.activeInHierarchy || hud.Canvas == null)
            {
                MarkSeenAndGrant(false);
                yield break;
            }

            overlay = PowerUpUnlockTutorial.Create(hud.Canvas, hud.levelText != null ? hud.levelText.font : null);
            if (overlay == null)
            {
                Debug.LogWarning("PowerUpUnlockTutorial prefab missing; skipping the " + kind + " introduction.", this);
                MarkSeenAndGrant(false);
                yield break;
            }

            GameAnalytics.PowerUpTutorialShown(kind);
            yield return overlay.Run(button, hud.GetPowerUpSprite(kind), message, () => MarkSeenAndGrant(true), () =>
            {
                pendingUse = kind;
                GameAnalytics.PowerUpTutorialTapped(kind);
            });
            CloseOverlay();
        }

        /// <summary>Aborts any running introduction (level change, navigation) and forgets its pending tap.</summary>
        public void Cancel()
        {
            StopAllCoroutines();
            pendingUse = null;
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            if (overlay != null)
            {
                overlay.Cancel();
                overlay.gameObject.SetActive(false);
                Destroy(overlay.gameObject);
            }
            overlay = null;
        }

        private void OnDisable() => Cancel();
    }
}
