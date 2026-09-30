using NUnit.Framework;

namespace CatBlockPuzzle.Tests
{
    public sealed class LevelLockValidationTests
    {
        // Two horizontal bars on a 2x3 board: p1 fills row 0, p2 fills row 1.
        private static LevelPackData Pack(params LockedCellData[] locked)
        {
            var level = new LevelData
            {
                id = "lock_test", title = "Locks", difficulty = 3, rows = 2, cols = 3, reward = 10,
                pieces = new[]
                {
                    new PieceData { id = "p1", name = "A", shape = "i3h", row = 0, col = 0 },
                    new PieceData { id = "p2", name = "B", shape = "i3h", row = 1, col = 0 }
                },
                locked = locked
            };
            var levels = new LevelData[100];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = i == 0 ? level : new LevelData
                {
                    id = "filler_" + i, title = "F", difficulty = 1, rows = 1, cols = 3, reward = 1,
                    pieces = new[] { new PieceData { id = "f" + i, name = "F", shape = "i3h", row = 0, col = 0 } }
                };
            }
            return new LevelPackData { version = 1, levels = levels };
        }

        [Test]
        public void CounterLock_OnASolutionCell_IsValid()
        {
            Assert.That(LevelValidator.TryValidatePack(Pack(new LockedCellData { row = 1, col = 0, after = 1 }), out string error), Is.True, error);
        }

        [Test]
        public void NeighbourLock_WithAnotherPieceBesideIt_IsValid()
        {
            Assert.That(LevelValidator.TryValidatePack(Pack(new LockedCellData { row = 1, col = 1, after = 0 }), out string error), Is.True, error);
        }

        [Test]
        public void Lock_OffTheSolution_IsRejected()
        {
            Assert.That(LevelValidator.TryValidatePack(Pack(new LockedCellData { row = 1, col = 5, after = 1 }), out string error), Is.False);
            Assert.That(error, Does.Contain("no piece fills"));
        }

        [Test]
        public void CounterLock_NeedingMorePiecesThanAreFree_IsRejected()
        {
            Assert.That(LevelValidator.TryValidatePack(Pack(new LockedCellData { row = 1, col = 0, after = 2 }), out string error), Is.False);
            Assert.That(error, Does.Contain("only 1 pieces are unlocked"));
        }

        [Test]
        public void LockingEveryPiece_IsRejected()
        {
            var locks = new[] { new LockedCellData { row = 0, col = 0, after = 1 }, new LockedCellData { row = 1, col = 0, after = 1 } };
            Assert.That(LevelValidator.TryValidatePack(Pack(locks), out string error), Is.False);
            Assert.That(error, Does.Contain("locks every piece"));
        }
    }
}
