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
                yield return Show(hud, PowerUpKind.Hint, "Stuck? Hint shows where a cat fits.\nTap it!", HintSeenKey);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Freeze) && PlayerPrefs.GetInt(FreezeSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Freeze, $"Freeze stops the clock for {freezeSeconds:0.#} seconds.\nTap it!", FreezeSeenKey);
        }

        private IEnumerator Show(GameplayHudView hud, PowerUpKind kind, string message, string key)
        {
            Button button = hud.PowerUpButton(kind);
            void MarkSeenAndGrant()
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                // The introduction tap is on the house, so the first use never opens the shop.
                if (PowerUpInventory.GetCount(kind) == 0)
                {
                    PowerUpInventory.Add(kind);
                    GameAnalytics.ItemEarned(kind, 1, GameAnalytics.EarnTutorialGrant);
                }
                hud.RefreshPowerUpCounts();
            }

            if (button == null || !button.gameObject.activeInHierarchy || hud.Canvas == null)
            {
                MarkSeenAndGrant();
                yield break;
            }

            overlay = PowerUpUnlockTutorial.Create(hud.Canvas, hud.levelText != null ? hud.levelText.font : null);
            if (overlay == null)
            {
                Debug.LogWarning("PowerUpUnlockTutorial prefab missing; skipping the " + kind + " introduction.", this);
                MarkSeenAndGrant();
                yield break;
            }

            GameAnalytics.PowerUpTutorialShown(kind);
            yield return overlay.Run(button, hud.GetPowerUpSprite(kind), message, MarkSeenAndGrant, () =>
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
