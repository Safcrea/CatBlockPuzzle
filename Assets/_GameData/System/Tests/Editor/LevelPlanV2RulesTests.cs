using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class LevelPlanV2RulesTests
    {
        // A 2x3 board of two horizontal bars; p1 fills row 0, p2 fills row 1.
        private static LevelPackData Pack(System.Action<LevelData> configure)
        {
            var level = new LevelData
            {
                id = "v2_test", title = "V2", difficulty = 3, rows = 2, cols = 3, reward = 10,
                pieces = new[]
                {
                    new PieceData { id = "p1", name = "A", shape = "i3h", row = 0, col = 0 },
                    new PieceData { id = "p2", name = "B", shape = "i3h", row = 1, col = 0 }
                }
            };
            configure(level);
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

        private static HashSet<Vector2Int> CellSet(CellOffset[] cells) =>
            new HashSet<Vector2Int>(cells.Select(c => new Vector2Int(c.Row, c.Col)));

        [TestCase("l3Top", 0, 0, 0, 1, 1, 0)]
        [TestCase("l3TopRight", 0, 0, 0, 1, 1, 1)]
        [TestCase("j3", 0, 1, 1, 0, 1, 1)]
        public void ThreeCellShapes_MatchTheSpec(string id, int r0, int c0, int r1, int c1, int r2, int c2)
        {
            Assert.That(ShapeLibrary.TryGetShape(id, out CellOffset[] cells), Is.True);
            Assert.That(CellSet(cells), Is.EquivalentTo(new[] { new Vector2Int(r0, c0), new Vector2Int(r1, c1), new Vector2Int(r2, c2) }));
        }

        [TestCase("l4TopLeft", 0, 0, 0, 1, 1, 0, 2, 0)]
        [TestCase("j4Top", 0, 0, 0, 1, 1, 1, 2, 1)]
        [TestCase("s4v", 0, 0, 1, 0, 1, 1, 2, 1)]
        [TestCase("t4Left", 0, 1, 1, 0, 1, 1, 2, 1)]
        public void FourCellShapes_MatchTheSpec(string id, int r0, int c0, int r1, int c1, int r2, int c2, int r3, int c3)
        {
            Assert.That(ShapeLibrary.TryGetShape(id, out CellOffset[] cells), Is.True);
            Assert.That(CellSet(cells), Is.EquivalentTo(new[]
                { new Vector2Int(r0, c0), new Vector2Int(r1, c1), new Vector2Int(r2, c2), new Vector2Int(r3, c3) }));
        }

        [Test]
        public void RotatingClockwise_TurnsAndNormalisesTheShapeAndFourTurnsComeBack()
        {
            ShapeLibrary.TryGetShape("l3", out CellOffset[] l3);
            var piece = new PieceDefinition("p", "P", "l3", l3, 2, 3);
            PieceDefinition once = piece.RotatedClockwise();
            // X.      XX
            // XX  ->  X.
            Assert.That(CellSet(once.Cells), Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) }));
            Assert.That(once.SolutionRow, Is.EqualTo(2));
            Assert.That(once.ShapeId, Is.EqualTo("l3"));

            ShapeLibrary.TryGetShape("i4h", out CellOffset[] bar);
            PieceDefinition upright = new PieceDefinition("b", "B", "i4h", bar, 0, 0).RotatedClockwise();
            Assert.That(upright.Rows, Is.EqualTo(4));
            Assert.That(upright.Cols, Is.EqualTo(1));

            PieceDefinition back = piece.RotatedClockwise().RotatedClockwise().RotatedClockwise().RotatedClockwise();
            Assert.That(CellSet(back.Cells), Is.EquivalentTo(CellSet(l3)));
        }

        [Test]
        public void ShippedPack_HasTheV2MechanicsAndValidates()
        {
            var pack = JsonUtility.FromJson<LevelPackData>(Resources.Load<TextAsset>("CatBlockPuzzle/levels_100").text);
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.True, error);
            var levels = new LevelManager(pack.levels);
            Assert.That(levels.GetLevel(50).HasFixedPieces, Is.True, "Level 51 introduces the fixed cat.");
            Assert.That(levels.GetLevel(60).HasFog, Is.True, "Level 61 introduces fog.");
            Assert.That(levels.GetLevel(70).Moves, Is.EqualTo(10), "Level 71 introduces move-limit stars.");
            for (int i = 0; i < 50; i++)
            {
                LevelDefinition level = levels.GetLevel(i);
                Assert.That(level.HasFixedPieces || level.HasFog || level.HasMoves, Is.False, "Level " + (i + 1) + " uses a v2 mechanic too early.");
            }
        }

        [Test]
        public void FixedCat_CountsAsPlacedForCounterLocks()
        {
            // p3 fills row 2 and waits for two cats; p1 is already on the board, so placing p2 opens it.
            LevelPackData pack = Pack(level =>
            {
                level.rows = 3;
                level.pieces = new[]
                {
                    new PieceData { id = "p1", name = "A", shape = "i3h", row = 0, col = 0, @fixed = true },
                    new PieceData { id = "p2", name = "B", shape = "i3h", row = 1, col = 0 },
                    new PieceData { id = "p3", name = "C", shape = "i3h", row = 2, col = 0 }
                };
                level.locked = new[] { new LockedCellData { row = 2, col = 0, after = 2 } };
            });
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.True, error);
        }

        [Test]
        public void FixedCat_CannotHoldALock()
        {
            LevelPackData pack = Pack(level =>
            {
                level.pieces[0].@fixed = true;
                level.locked = new[] { new LockedCellData { row = 0, col = 1, key = "p2" } };
            });
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.False);
            Assert.That(error, Does.Contain("fixed cat"));
        }

        [Test]
        public void FixedCat_CannotBeAKey()
        {
            LevelPackData pack = Pack(level =>
            {
                level.pieces[0].@fixed = true;
                level.locked = new[] { new LockedCellData { row = 1, col = 1, key = "p1" } };
            });
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.False);
            Assert.That(error, Does.Contain("keyed to fixed cat"));
        }

        [Test]
        public void FixingEveryCat_IsRejected()
        {
            LevelPackData pack = Pack(level => { foreach (PieceData piece in level.pieces) piece.@fixed = true; });
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.False);
            Assert.That(error, Does.Contain("fixes every cat"));
        }

        [TestCase(-1, 0, false)]
        [TestCase(0, 3, false)]
        [TestCase(2, 0, false)]
        [TestCase(1, 2, true)]
        public void FogCells_MustSitInsideTheGrid(int row, int col, bool valid)
        {
            LevelPackData pack = Pack(level => level.fog = new[] { new BlockedCellData { row = row, col = col } });
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.EqualTo(valid), error);
        }

        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(-1, false)]
        public void Moves_MustCoverEveryCatToPlace(int moves, bool valid)
        {
            LevelPackData pack = Pack(level => level.moves = moves);
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.EqualTo(valid), error);
        }

        [Test]
        public void Moves_IgnoreFixedCats()
        {
            LevelPackData pack = Pack(level => { level.pieces[0].@fixed = true; level.moves = 1; });
            Assert.That(LevelValidator.TryValidatePack(pack, out string error), Is.True, error);
        }

        [TestCase(10, 10, 3)]
        [TestCase(13, 10, 2)]
        [TestCase(14, 10, 1)]
        [TestCase(5, 10, 3)]
        public void MoveStars_FollowTheDropLimit(int drops, int moves, int stars)
        {
            Assert.That(DifficultyProfile.MoveStars(drops, moves), Is.EqualTo(stars));
        }

        [Test]
        public void FirstTenLevels_AreUntimedAndAlwaysEarnThreeStars()
        {
            var level = new LevelDefinition("t", "T", 5, 3, 3, 10, new PieceDefinition[0]);
            DifficultyProfile profile = DifficultyProfile.For(level, 9);
            Assert.That(DifficultyProfile.IsUntimed(1), Is.True);
            Assert.That(DifficultyProfile.IsUntimed(10), Is.True);
            Assert.That(DifficultyProfile.IsUntimed(11), Is.False);
            Assert.That(profile.Stars(level, 10, 0f, 99), Is.EqualTo(3));
            Assert.That(profile.Stars(level, 11, 0f, 99), Is.EqualTo(1));
        }

        [Test]
        public void MoveLimitLevels_ScoreDropsNotTime()
        {
            var level = new LevelDefinition("t", "T", 8, 3, 3, 10, new PieceDefinition[0], moves: 6);
            DifficultyProfile profile = DifficultyProfile.For(level, 70);
            Assert.That(profile.Stars(level, 71, 0f, 6), Is.EqualTo(3), "No time left still earns three stars within the limit.");
            Assert.That(profile.Stars(level, 71, profile.DurationSeconds, 10), Is.EqualTo(1), "A full clock does not rescue too many drops.");
        }

        [TestCase(2026, 1, 1, 50)]
        [TestCase(2026, 2, 19, 99)]
        [TestCase(2026, 2, 20, 50)]
        [TestCase(2026, 10, 24, 96)]
        [TestCase(2025, 12, 31, 99)]
        public void DailyPuzzle_PicksOneOfLevels51To100ByUtcDate(int year, int month, int day, int expectedIndex)
        {
            var date = new System.DateTime(year, month, day, 23, 30, 0, System.DateTimeKind.Utc);
            Assert.That(DailyPuzzle.LevelIndexFor(date, 100), Is.EqualTo(expectedIndex));
        }

        [Test]
        public void DailyPuzzle_PaysItsBonusOnceEachUtcDay()
        {
            bool had = PlayerPrefs.HasKey(DailyPuzzle.LastWonDayKey);
            int saved = PlayerPrefs.GetInt(DailyPuzzle.LastWonDayKey);
            PlayerPrefs.DeleteKey(DailyPuzzle.LastWonDayKey);
            try
            {
                var morning = new System.DateTime(2026, 10, 24, 0, 5, 0, System.DateTimeKind.Utc);
                Assert.That(DailyPuzzle.IsDoneToday(morning), Is.False);
                Assert.That(DailyPuzzle.TryClaimBonus(morning), Is.True);
                Assert.That(DailyPuzzle.IsDoneToday(morning.AddHours(23)), Is.True);
                Assert.That(DailyPuzzle.TryClaimBonus(morning.AddHours(23)), Is.False);
                Assert.That(DailyPuzzle.IsDoneToday(morning.AddDays(1)), Is.False, "The check mark clears at the next UTC day.");
                Assert.That(DailyPuzzle.TryClaimBonus(morning.AddDays(1)), Is.True);
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(DailyPuzzle.LastWonDayKey, saved);
                else PlayerPrefs.DeleteKey(DailyPuzzle.LastWonDayKey);
            }
        }

        [Test]
        public void Rotate_IsSoldInTheShopAtTheFreezePrice()
        {
            PowerUpShopConfig config = PowerUpShopConfig.Load();
            Assert.That(config, Is.Not.Null);
            PowerUpShopConfig.Offer rotate = config.GetOffer(PowerUpKind.Rotate);
            PowerUpShopConfig.Offer freeze = config.GetOffer(PowerUpKind.Freeze);
            Assert.That(rotate.coinPrice, Is.EqualTo(freeze.coinPrice));
            Assert.That(rotate.quantity, Is.EqualTo(freeze.quantity));
            Assert.That(PowerUpShopConfig.CalculatePurchase(PowerUpKind.Rotate, rotate, rotate.coinPrice, 0, out int left, out int owned),
                Is.EqualTo(PowerUpPurchaseStatus.Success));
            Assert.That(left, Is.Zero);
            Assert.That(owned, Is.EqualTo(rotate.quantity));
        }

        [Test]
        public void FailStreak_GrantsAFreeHintAfterThreeFailsAndResets()
        {
            const int levelNumber = 987654;
            LevelFailStreak.Reset(levelNumber);
            try
            {
                LevelFailStreak.RecordFail(levelNumber);
                LevelFailStreak.RecordFail(levelNumber);
                Assert.That(LevelFailStreak.EarnsFreeHint(levelNumber), Is.False);
                LevelFailStreak.RecordFail(levelNumber);
                Assert.That(LevelFailStreak.EarnsFreeHint(levelNumber), Is.True);
                Assert.That(LevelFailStreak.EarnsFreeHint(levelNumber + 1), Is.False, "Fails are counted per level.");
                LevelFailStreak.Reset(levelNumber);
                Assert.That(LevelFailStreak.Count(levelNumber), Is.Zero);
            }
            finally
            {
                LevelFailStreak.Reset(levelNumber);
            }
        }
    }
}
