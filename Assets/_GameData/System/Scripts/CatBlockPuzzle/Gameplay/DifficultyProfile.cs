using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The time budget, star thresholds and failure rule for a level, derived from the
    /// authored 1–10 difficulty. Tuning lives entirely in <see cref="Bands"/>.
    /// </summary>
    internal readonly struct DifficultyProfile
    {
        public readonly float DurationSeconds;
        public readonly float ThreeStarRatio;
        public readonly float TwoStarRatio;
        /// <summary>When false, running out of time only costs stars; the level never fails.</summary>
        public readonly bool CanFail;

        public DifficultyProfile(float durationSeconds, float threeStarRatio, float twoStarRatio, bool canFail)
        {
            DurationSeconds = durationSeconds;
            ThreeStarRatio = threeStarRatio;
            TwoStarRatio = twoStarRatio;
            CanFail = canFail;
        }

        /// <summary>Level 1 hosts the hands-on tutorial and keeps the original relaxed rules.</summary>
        public static readonly DifficultyProfile Tutorial = new DifficultyProfile(120f, 0.5f, 0.2f, false);

        /// <summary>The lowest difficulty at which the timer can fail the level.</summary>
        public const int FirstFailableDifficulty = 3;

        // Indexed by (difficulty - 1) / 2: 1–2, 3–4, 5–6, 7–8, 9–10.
        private static readonly DifficultyProfile[] Bands =
        {
            new DifficultyProfile(60f, 0.60f, 0.25f, false),
            new DifficultyProfile(75f, 0.55f, 0.25f, true),
            new DifficultyProfile(90f, 0.50f, 0.20f, true),
            new DifficultyProfile(110f, 0.45f, 0.20f, true),
            new DifficultyProfile(135f, 0.40f, 0.15f, true)
        };

        public static DifficultyProfile For(LevelDefinition level, int levelIndex)
        {
            if (levelIndex == 0 || level == null)
            {
                return Tutorial;
            }

            int difficulty = Mathf.Clamp(level.Difficulty, 1, 10);
            DifficultyProfile band = Bands[(difficulty - 1) / 2];
            return new DifficultyProfile(band.DurationSeconds, band.ThreeStarRatio, band.TwoStarRatio,
                band.CanFail && difficulty >= FirstFailableDifficulty);
        }

        public int Stars(float remainingSeconds) =>
            CatPuzzleResultCalculator.CalculateStars(remainingSeconds, DurationSeconds, ThreeStarRatio, TwoStarRatio);

        public float StarFill(int starIndex, float remainingSeconds) =>
            CatPuzzleResultCalculator.CalculateStarFill(starIndex, remainingSeconds, DurationSeconds, ThreeStarRatio, TwoStarRatio);
    }
}
