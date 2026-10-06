namespace CatBlockPuzzle
{
    /// <summary>Shared "Button" click stinger, called from every UI button handler.</summary>
    internal static class ButtonSfx
    {
        internal const string ClickId = "Button";

        internal const string CoinId = "Coin";

        internal static void PlayClick()
        {
            // The click usually already sounded on press (ButtonPressSfx); don't play it twice.
            if (!ButtonPressSfx.ConsumeRecentPress()) SoundManager.EnsureInstance().PlaySfx(ClickId);
            HapticsController.Current?.Play(HapticKind.UiTap);
        }

        internal const string CoinCountId = "CoinCount";
        private const float CoinCountTickSpacing = 0.06f;
        private static float lastCoinCountTick = -1f;

        /// <summary>Coins collected (daily reward, 2x ad, a win's coins reaching the wallet): one chime.</summary>
        internal static void PlayCoin() => SoundManager.EnsureInstance().PlaySfxOneShot(CoinId);

        /// <summary>One tick of a coin counter rolling up; rapid calls are spaced so it reads as a steady count.</summary>
        internal static void PlayCoinCountTick()
        {
            float now = UnityEngine.Time.unscaledTime;
            if (now - lastCoinCountTick < CoinCountTickSpacing) return;
            lastCoinCountTick = now;
            SoundManager.EnsureInstance().PlaySfxOneShot(CoinCountId);
        }
    }
}
