using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The never-ending level number. After the last authored level the puzzles loop from the
    /// first one while the number keeps counting (101 plays puzzle 1, 102 puzzle 2, ... 201 puzzle 1
    /// again). Looped levels always open and each loop number pays its full reward once.
    /// </summary>
    public static class EndlessLevels
    {
        private const string NextLoopLevelKey = "CatBlockPuzzle.Endless.NextLevel";
        private const string PaidThroughKey = "CatBlockPuzzle.Endless.PaidThrough";

        /// <summary>The loop level the player plays next, or 0 before the last authored level is beaten.</summary>
        public static int NextLoopLevel => PlayerPrefs.GetInt(NextLoopLevelKey, 0);

        public static bool IsActive => NextLoopLevel > 0;

        /// <summary>Zero-based authored puzzle for a 1-based level number.</summary>
        public static int PuzzleIndexFor(int levelNumber, int authoredCount)
        {
            if (authoredCount <= 0) return 0;
            return (Mathf.Max(1, levelNumber) - 1) % authoredCount;
        }

        public static bool IsLoopLevel(int levelNumber, int authoredCount) => authoredCount > 0 && levelNumber > authoredCount;

        /// <summary>Called on every real win: beating the last authored level, or any loop level, moves the loop on.</summary>
        public static void RecordWin(int levelNumber, int authoredCount)
        {
            if (authoredCount <= 0 || levelNumber < authoredCount) return;
            int next = Mathf.Max(NextLoopLevel, levelNumber + 1);
            PlayerPrefs.SetInt(NextLoopLevelKey, next);
            PlayerPrefs.Save();
        }

        /// <summary>True the first time a loop level number is won, so it pays like a brand-new level.</summary>
        public static bool TryClaimFullReward(int levelNumber)
        {
            if (levelNumber <= PlayerPrefs.GetInt(PaidThroughKey, 0)) return false;
            PlayerPrefs.SetInt(PaidThroughKey, levelNumber);
            PlayerPrefs.Save();
            return true;
        }
    }
}
