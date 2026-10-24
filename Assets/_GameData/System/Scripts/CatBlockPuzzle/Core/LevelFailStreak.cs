using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Counts fails (timer expiry or running out of hearts) per level number in PlayerPrefs.
    /// After <see cref="FreeHintAfter"/> fails the next attempt starts with a free hint; winning
    /// the level or using that hint clears the count.
    /// </summary>
    internal static class LevelFailStreak
    {
        public const int FreeHintAfter = 3;
        private const string KeyPrefix = "CatBlockPuzzle.FailStreak.";

        public static string Key(int levelNumber) => KeyPrefix + Mathf.Max(1, levelNumber);

        public static int Count(int levelNumber) => Mathf.Max(0, PlayerPrefs.GetInt(Key(levelNumber), 0));

        public static bool EarnsFreeHint(int levelNumber) => Count(levelNumber) >= FreeHintAfter;

        public static void RecordFail(int levelNumber)
        {
            int count = Count(levelNumber);
            PlayerPrefs.SetInt(Key(levelNumber), count >= int.MaxValue ? count : count + 1);
            PlayerPrefs.Save();
        }

        public static void Reset(int levelNumber)
        {
            if (!PlayerPrefs.HasKey(Key(levelNumber))) return;
            PlayerPrefs.DeleteKey(Key(levelNumber));
            PlayerPrefs.Save();
        }
    }
}
