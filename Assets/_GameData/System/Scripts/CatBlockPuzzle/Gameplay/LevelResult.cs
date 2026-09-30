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

        public static int CalculateStars(float remainingSeconds, float durationSeconds) =>
            CalculateStars(remainingSeconds, durationSeconds, ThreeStarThreshold, TwoStarThreshold);

        public static int CalculateStars(float remainingSeconds, float durationSeconds, float threeStarRatio, float twoStarRatio)
        {
            float ratio = durationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / durationSeconds) : 0f;
            if (ratio > threeStarRatio)
            {
                return 3;
            }

            return ratio > twoStarRatio ? 2 : 1;
        }

        // Drain the rightmost star first, reaching empty at the scoring thresholds.
        public static float CalculateStarFill(int starIndex, float remainingSeconds, float durationSeconds) =>
            CalculateStarFill(starIndex, remainingSeconds, durationSeconds, ThreeStarThreshold, TwoStarThreshold);

        public static float CalculateStarFill(int starIndex, float remainingSeconds, float durationSeconds, float threeStarRatio, float twoStarRatio)
        {
            float ratio = durationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / durationSeconds) : 0f;
            switch (starIndex)
            {
                case 0: return Mathf.Clamp01(ratio / twoStarRatio);
                case 1: return Mathf.Clamp01((ratio - twoStarRatio) / (threeStarRatio - twoStarRatio));
                case 2: return Mathf.Clamp01((ratio - threeStarRatio) / (1f - threeStarRatio));
                default: return 0f;
            }
        }

        public static int ClampStars(int stars)
        {
            return Mathf.Clamp(stars, MinimumStars, MaximumStars);
        }
    }
}
