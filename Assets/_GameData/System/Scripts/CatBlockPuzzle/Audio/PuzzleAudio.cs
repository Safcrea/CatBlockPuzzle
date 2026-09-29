namespace CatBlockPuzzle
{
    /// <summary>Gameplay sound effect entry points, resolved against the shared SoundManager.</summary>
    internal static class PuzzleAudio
    {
        public static void Play(string id)
        {
            SoundManager manager = SoundManager.EnsureInstance();
            if (manager != null) manager.PlaySfx(id);
        }

        public static void PlayWithMusicDuck(string id)
        {
            SoundManager manager = SoundManager.EnsureInstance();
            if (manager != null) manager.PlaySfxWithMusicDuck(id);
        }

        public static void PlayButton() => Play("Button");
    }
}
