using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The never-ending level number. After the last authored level the hard back half loops (levels 101-300) while the
    /// number keeps counting: 301 plays puzzle 101, 302 puzzle 102, ... 501 puzzle 101 again. Looped levels always open
    /// and each loop number pays its full reward once.
    /// </summary>
    public static class EndlessLevels
    {
        private const string NextLoopLevelKey = "CatBlockPuzzle.Endless.NextLevel";
        private const string PaidThroughKey = "CatBlockPuzzle.Endless.PaidThrough";

        /// <summary>The loop level the player plays next, or 0 before the last authored level is beaten.</summary>
        public static int NextLoopLevel => PlayerPrefs.GetInt(NextLoopLevelKey, 0);

        public static bool IsActive => NextLoopLevel > 0;

        /// <summary>The first (0-based) puzzle the endless loop replays: level 101, so the loop stays hard.</summary>
        public const int LoopFromIndex = 100;

        /// <summary>Zero-based authored puzzle for a 1-based level number.</summary>
        public static int PuzzleIndexFor(int levelNumber, int authoredCount)
        {
            if (authoredCount <= 0) return 0;
            int index = Mathf.Max(1, levelNumber) - 1;
            if (index < authoredCount) return index;
            int loopFrom = authoredCount > LoopFromIndex ? LoopFromIndex : 0;
            return loopFrom + (index - authoredCount) % (authoredCount - loopFrom);
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
