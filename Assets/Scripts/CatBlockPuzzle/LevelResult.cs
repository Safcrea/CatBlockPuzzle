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
        public static int CalculateStars(float remainingSeconds, float durationSeconds)
        {
            float ratio = durationSeconds > 0f ? Mathf.Clamp01(remainingSeconds / durationSeconds) : 0f;
            if (ratio > 0.5f)
            {
                return 3;
            }

            return ratio > 0.2f ? 2 : 1;
        }
    }
}
