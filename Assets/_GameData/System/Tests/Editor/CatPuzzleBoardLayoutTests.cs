using CatBlockPuzzle;
using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPuzzleBoardLayoutTests
    {
        [Test]
        public void AllHundredLevels_FitInsideBoardWithSquareCellsAndMargins()
        {
            var pack = JsonUtility.FromJson<LevelPackData>(Resources.Load<TextAsset>("CatBlockPuzzle/levels_100").text);
            Vector2 available = new Vector2(790f - 72f, 790f - 72f);
            foreach (var level in pack.levels)
            {
                Vector2 size = CatPuzzleBoardLayout.Fit(level.rows, level.cols, available, 8f);
                Assert.That(size.x, Is.LessThanOrEqualTo(available.x + .001f), level.id);
                Assert.That(size.y, Is.LessThanOrEqualTo(available.y + .001f), level.id);
                float width = (size.x - 8f * (level.cols - 1)) / level.cols;
                float height = (size.y - 8f * (level.rows - 1)) / level.rows;
                Assert.That(width, Is.EqualTo(height).Within(.001f), level.id);
                Assert.That(width, Is.GreaterThan(0f), level.id);
            }
        }
    }
}
