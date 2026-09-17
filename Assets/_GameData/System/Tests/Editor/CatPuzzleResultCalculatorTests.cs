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

        [TestCase(120f, 1f, 1f, 1f)]
        [TestCase(90f, 1f, 1f, .5f)]
        [TestCase(60f, 1f, 1f, 0f)]
        [TestCase(42f, 1f, .5f, 0f)]
        [TestCase(24f, 1f, 0f, 0f)]
        [TestCase(12f, .5f, 0f, 0f)]
        [TestCase(0f, 0f, 0f, 0f)]
        public void StarFill_DrainsSequentiallyAtScoreThresholds(float remaining, float first, float second, float third)
        {
            float[] expected = { first, second, third };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(CatPuzzleResultCalculator.CalculateStarFill(i, remaining, 120f), Is.EqualTo(expected[i]).Within(.00001f));
            }
        }

        [TestCase(-1, 120f, 120f)]
        [TestCase(3, 120f, 120f)]
        [TestCase(0, 120f, 0f)]
        [TestCase(2, -10f, 120f)]
        public void StarFill_HandlesInvalidInput(int index, float remaining, float duration)
        {
            Assert.That(CatPuzzleResultCalculator.CalculateStarFill(index, remaining, duration), Is.Zero);
        }
    }
}
