namespace CatBlockPuzzle
{
    /// <summary>Gameplay sound entry points, resolved against the shared AudioManager.</summary>
    internal static class PuzzleAudio
    {
        public static void Play(string eventName) => AudioManager.EnsureInstance().PlayEvent(eventName);

        public static void PlayButton()
        {
            Play(AudioEvents.ButtonTap);
            HapticsController.Current?.Play(HapticKind.UiTap);
        }
    }
}
