using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>The Easy / Medium / Hard label players see for a level, derived from its authored 1-10 difficulty.</summary>
    public enum DifficultyTier { Easy, Medium, Hard }

    public static class DifficultyTiers
    {
        // Difficulty 1-3 is Easy, 4-7 is Medium, 8-10 is Hard.
        public const int FirstMediumDifficulty = 4;
        public const int FirstHardDifficulty = 8;

        public static DifficultyTier For(int difficulty)
        {
            if (difficulty >= FirstHardDifficulty) return DifficultyTier.Hard;
            return difficulty >= FirstMediumDifficulty ? DifficultyTier.Medium : DifficultyTier.Easy;
        }

        public static string Label(DifficultyTier tier) => tier.ToString();

        public static Color Colour(DifficultyTier tier)
        {
            switch (tier)
            {
                case DifficultyTier.Hard: return new Color(0.86f, 0.30f, 0.30f, 1f);
                case DifficultyTier.Medium: return new Color(0.93f, 0.62f, 0.15f, 1f);
                default: return new Color(0.33f, 0.70f, 0.38f, 1f);
            }
        }

        public static string Hex(DifficultyTier tier) => ColorUtility.ToHtmlStringRGB(Colour(tier));
    }
}
