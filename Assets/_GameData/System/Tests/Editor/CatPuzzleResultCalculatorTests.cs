using NUnit.Framework;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatPuzzleResultCalculatorTests
    {
        [TestCase(60.01f, 120f, 3)]
        [TestCase(60f, 120f, 2)]
        [TestCase(24.01f, 120f, 2)]
        [TestCase(24f, 120f, 1)]
        [TestCase(0f, 120f, 1)]
        [TestCase(120f, 0f, 1)]
        public void CalculateStars_UsesStrictThresholds(float remaining, float duration, int expected)
        {
            Assert.That(CatPuzzleResultCalculator.CalculateStars(remaining, duration), Is.EqualTo(expected));
        }

        [Test]
        public void LevelResult_RetainsBestStars()
        {
            LevelResult result = new LevelResult("level-01", 42f, 2, 3);

            Assert.That(result.LevelId, Is.EqualTo("level-01"));
            Assert.That(result.CompletionSeconds, Is.EqualTo(42f));
            Assert.That(result.Stars, Is.EqualTo(2));
            Assert.That(result.BestStars, Is.EqualTo(3));
        }
    }
}
