using NUnit.Framework;

namespace CatBlockPuzzle.Tests
{
    public sealed class DifficultyProfileTests
    {
        private static LevelDefinition Level(int difficulty) =>
            new LevelDefinition("test", "Test", difficulty, 3, 3, 10, new PieceDefinition[0]);

        [Test]
        public void LevelOne_KeepsTheRelaxedTutorialRules()
        {
            var profile = DifficultyProfile.For(Level(1), 0);
            Assert.That(profile.DurationSeconds, Is.EqualTo(120f));
            Assert.That(profile.CanFail, Is.False);
            Assert.That(profile.Stars(61f), Is.EqualTo(3));
        }

        [TestCase(1, 60f, false)]
        [TestCase(2, 60f, false)]
        [TestCase(3, 75f, true)]
        [TestCase(6, 90f, true)]
        [TestCase(8, 110f, true)]
        [TestCase(10, 135f, true)]
        public void Bands_ScaleTimeAndFailureWithDifficulty(int difficulty, float seconds, bool canFail)
        {
            var profile = DifficultyProfile.For(Level(difficulty), 5);
            Assert.That(profile.DurationSeconds, Is.EqualTo(seconds));
            Assert.That(profile.CanFail, Is.EqualTo(canFail));
        }

        [TestCase(1, HintReveal.FullShape, 1)]
        [TestCase(4, HintReveal.FullShape, 1)]
        [TestCase(5, HintReveal.AnchorOnly, 1)]
        [TestCase(7, HintReveal.AnchorOnly, 1)]
        [TestCase(8, HintReveal.AnchorOnly, 2)]
        [TestCase(10, HintReveal.AnchorOnly, 2)]
        public void Hints_RevealLessAndCostMoreOnHardLevels(int difficulty, HintReveal reveal, int cost)
        {
            var profile = DifficultyProfile.For(Level(difficulty), 5);
            Assert.That(profile.HintReveal, Is.EqualTo(reveal));
            Assert.That(profile.HintCost, Is.EqualTo(cost));
        }

        [Test]
        public void HarderLevels_NeedLessTimeLeftForThreeStars()
        {
            var easy = DifficultyProfile.For(Level(2), 5);
            var hard = DifficultyProfile.For(Level(10), 5);
            Assert.That(easy.Stars(easy.DurationSeconds * 0.55f), Is.EqualTo(2));
            Assert.That(hard.Stars(hard.DurationSeconds * 0.55f), Is.EqualTo(3));
        }
    }
}
