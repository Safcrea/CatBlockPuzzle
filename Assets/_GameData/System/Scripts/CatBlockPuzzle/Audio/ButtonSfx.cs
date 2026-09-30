namespace CatBlockPuzzle
{
    /// <summary>Shared "Button" click stinger, called from every UI button handler.</summary>
    internal static class ButtonSfx
    {
        internal const string ClickId = "Button";

        internal static void PlayClick()
        {
            SoundManager.EnsureInstance().PlaySfx(ClickId);
            HapticsController.Current?.Play(HapticKind.UiTap);
        }
    }
}
