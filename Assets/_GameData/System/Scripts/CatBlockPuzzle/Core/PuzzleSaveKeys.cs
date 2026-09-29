namespace CatBlockPuzzle
{
    /// <summary>PlayerPrefs keys owned by the puzzle game.</summary>
    internal static class PuzzleSaveKeys
    {
        public const string SavedLevel = "CatBlockPuzzle.LevelIndex";
        public const string SavedCoins = "CatBlockPuzzle.Coins";
        public const string SavedSound = "CatBlockPuzzle.Settings.Sound";
        public const string SavedHaptics = "CatBlockPuzzle.Settings.Haptics";
        public const string SavedReducedMotion = "CatBlockPuzzle.Settings.ReducedMotion";
        public const string SavedBestStarsPrefix = "CatBlockPuzzle.BestStars.";
        public const string LevelOneTutorialSeen = "CatBlockPuzzle.Tutorial.LevelOne.v1";
        public const string MovePieceTipSeen = "CatBlockPuzzle.Tutorial.MovePiece.v1";
    }
}
