using UnityEngine;

namespace CatBlockPuzzle
{
    public readonly struct LevelResult
    {
        public readonly string LevelId;
        public readonly float CompletionSeconds;
        public readonly int Stars;
        public readonly int BestStars;

        public LevelResult(string levelId, float completionSeconds, int stars, int bestStars)
        {
            LevelId = levelId;
            CompletionSeconds = completionSeconds;
            Stars = stars;
            BestStars = bestStars;
        }
    }

    public static class CatPuzzleResultCalculator
    {
        public const int MinimumStars = 1;
        public const int MaximumStars = 3;
        private const float ThreeStarThreshold = 0.5f;
        private const float TwoStarThreshold = 0.2f;

        public static int CalculateStars(float remainingSeconds, float durationSeconds)
        {
            float ratio = durationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / durationSeconds) : 0f;
            if (ratio > ThreeStarThreshold)
            {
                return 3;
            }

            return ratio > TwoStarThreshold ? 2 : 1;
        }

        // Drain the rightmost star first, reaching empty at the scoring thresholds.
        public static float CalculateStarFill(int starIndex, float remainingSeconds, float durationSeconds)
        {
            float ratio = durationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / durationSeconds) : 0f;
            switch (starIndex)
            {
                case 0: return Mathf.Clamp01(ratio / TwoStarThreshold);
                case 1: return Mathf.Clamp01((ratio - TwoStarThreshold) / (ThreeStarThreshold - TwoStarThreshold));
                case 2: return Mathf.Clamp01((ratio - ThreeStarThreshold) / (1f - ThreeStarThreshold));
                default: return 0f;
            }
        }

        public static int ClampStars(int stars)
        {
            return Mathf.Clamp(stars, MinimumStars, MaximumStars);
        }
    }
}
