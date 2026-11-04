using NUnit.Framework;

namespace CatBlockPuzzle.Tests
{
    public sealed class StuckAdvisorTests
    {
        [Test]
        public void NothingOwnedOrUnlocked_PointsAtNothing() =>
            Assert.That(StuckAdvisor.Choose(default), Is.Null);

        [Test]
        public void HintIsTheDefaultSuggestion() =>
            Assert.That(StuckAdvisor.Choose(new StuckAdvisor.Facts { HintReady = true }), Is.EqualTo(PowerUpKind.Hint));

        [Test]
        public void RepeatedPadlockMisses_PreferTheHammerOverTheHint() =>
            Assert.That(StuckAdvisor.Choose(new StuckAdvisor.Facts
                { HintReady = true, HammerReady = true, HammerHasObstacle = true, LockBlocked = true }), Is.EqualTo(PowerUpKind.Hammer));

        [Test]
        public void PadlockMissesWithoutAHammer_FallBackToTheHint() =>
            Assert.That(StuckAdvisor.Choose(new StuckAdvisor.Facts { HintReady = true, LockBlocked = true }), Is.EqualTo(PowerUpKind.Hint));

        [Test]
        public void LowTimeWithAFreeze_PointsAtFreeze() =>
            Assert.That(StuckAdvisor.Choose(new StuckAdvisor.Facts { HintReady = true, FreezeReady = true, TimeIsLow = true }), Is.EqualTo(PowerUpKind.Freeze));

        [Test]
        public void PlentyOfTime_NeverSuggestsFreeze() =>
            Assert.That(StuckAdvisor.Choose(new StuckAdvisor.Facts { FreezeReady = true }), Is.Null);

        [Test]
        public void WithoutAHint_AHammerStillHelps() =>
            Assert.That(StuckAdvisor.Choose(new StuckAdvisor.Facts { HammerReady = true }), Is.EqualTo(PowerUpKind.Hammer));

        [Test]
        public void Messages_NameTheRightPowerUp()
        {
            Assert.That(StuckAdvisor.MessageFor(PowerUpKind.Hint, true, false), Does.Contain("Hint"));
            Assert.That(StuckAdvisor.MessageFor(PowerUpKind.Hint, false, false), Does.Contain("Hint"));
            Assert.That(StuckAdvisor.MessageFor(PowerUpKind.Hammer, true, true), Does.Contain("Padlock"));
            Assert.That(StuckAdvisor.MessageFor(PowerUpKind.Hammer, true, false), Does.Contain("Hammer"));
            Assert.That(StuckAdvisor.MessageFor(PowerUpKind.Freeze, true, false), Does.Contain("Freeze"));
        }
    }
}
