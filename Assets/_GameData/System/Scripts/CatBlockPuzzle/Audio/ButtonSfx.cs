using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Button sounds, called from every UI button handler.</summary>
    internal static class ButtonSfx
    {
        // Switching a setting off reuses the tap, quieter, because the pack has no toggle-off sound.
        private const float ToggleOffVolume = 0.5f;

        internal static void PlayClick()
        {
            AudioManager.EnsureInstance().PlayEvent(AudioEvents.ButtonTap);
            HapticsController.Current?.Play(HapticKind.UiTap);
        }

        /// <summary>Back and close-screen buttons.</summary>
        internal static void PlayBack()
        {
            AudioManager.EnsureInstance().PlayEvent(AudioEvents.ButtonBack);
            HapticsController.Current?.Play(HapticKind.UiTap);
        }

        /// <summary>A setting switched on (its own sound) or off (a quiet tap).</summary>
        internal static void PlayToggle(bool on)
        {
            if (on) AudioManager.EnsureInstance().PlayEvent(AudioEvents.ToggleOn);
            else AudioManager.EnsureInstance().PlayEvent(AudioEvents.ToggleOff, ToggleOffVolume);
            HapticsController.Current?.Play(HapticKind.UiTap);
        }

        /// <summary>A locked button or a move that is not allowed.</summary>
        internal static void PlayError() => AudioManager.EnsureInstance().PlayEvent(AudioEvents.LockedTap);

        /// <summary>Coins collected (daily reward, 2x ad, a win's coins reaching the wallet).</summary>
        internal static void PlayCoin() => AudioManager.EnsureInstance().PlayEvent(AudioEvents.CoinCollect);
    }
}
