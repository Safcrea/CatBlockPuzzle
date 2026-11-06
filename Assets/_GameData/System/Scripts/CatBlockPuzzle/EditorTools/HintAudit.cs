#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Plays levels the way a player would who only follows the Hint: it asks the hint for its next target, does what the
    /// hint says (lifting any cat that is in the way first) and repeats until the board is full. It reports every level where
    /// the hint falls silent while cats are still missing, or points at a spot the cat cannot take. Run it in Play Mode.
    /// </summary>
    internal static class HintAudit
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Audits levels [from, to) (zero-based) and returns one line per problem plus a summary.</summary>
        public static string Run(int from, int to, int seed = 0, int wrongDrops = 0)
        {
            if (!Application.isPlaying) return "Enter Play Mode first.";
            var game = Object.FindFirstObjectByType<CatBlockPuzzleGame>();
            var hint = (HintPresenter)typeof(CatBlockPuzzleGame).GetField("hint", Any).GetValue(game);
            MethodInfo pick = typeof(HintPresenter).GetMethod("PickHintTarget", Any);
            var report = new StringBuilder();
            int problems = 0, solved = 0, audited = 0;
            var rng = new System.Random(seed);
            string[] seenKeys = TutorialReset.SeenKeys();
            foreach (string key in seenKeys) PlayerPrefs.SetInt(key, 1); // intro cards would hold the level start
            for (int level = from; level < to && level < game.LevelCount; level++)
            {
                audited++;
                GameSystem.Instance.PreviewLevel(level);
                game.Flow.CancelReveal(true);
                PuzzleSession s = game.Session;
                s.inputLocked = false;
                s.levelFailed = false;
                ScatterWrongCats(game, s, rng, wrongDrops);
                string outcome = Follow(game, hint, pick, s, level);
                if (outcome == null) solved++;
                else { problems++; report.AppendLine("level " + (level + 1) + ": " + outcome); }
            }
            foreach (string key in seenKeys) PlayerPrefs.DeleteKey(key);
            report.Insert(0, "Hint audit of " + audited + " levels: " + solved + " solved by following the hint, " + problems + " with problems\n");
            return report.ToString();
        }

        // Why each cell of the target spot refuses the cat.
        private static string WhyNot(CatBlockPuzzleGame game, PuzzleSession s, PieceState piece, int row, int col)
        {
            var sb = new StringBuilder();
            foreach (CellOffset c in piece.Definition.Cells)
            {
                var coord = new Vector2Int(row + c.Row, col + c.Col);
                bool inLevel = s.activeLevel.ActiveCells.Contains(coord);
                s.occupancy.TryGetValue(coord, out string holder);
                sb.Append("(" + coord.x + "," + coord.y + ": active=" + inLevel + " holder=" + (holder ?? "-") + " locked=" + game.Locks.IsLocked(coord) + " free=" + game.Board.IsCellFree(coord, piece) + ") ");
            }
            return sb.ToString();
        }

        // What is on the board, for a bug report: each cat's name, state and cell.
        private static string Describe(PuzzleSession s)
        {
            var sb = new StringBuilder();
            foreach (PieceState p in s.pieces)
                sb.Append(p.Definition.Name).Append(p.Definition.IsSpare ? "(spare)" : "").Append(p.Definition.IsFixed ? "(fixed)" : "").Append(p.Placed ? "@" + p.Row + "," + p.Col : " on shelf")
                    .Append(p.Slot != null && !p.Slot.gameObject.activeSelf ? "[hidden]" : "").Append("; ");
            return sb.ToString();
        }

        // Drops a few cats on random spots that fit, the way a player who is guessing would.
        private static void ScatterWrongCats(CatBlockPuzzleGame game, PuzzleSession s, System.Random rng, int count)
        {
            for (int n = 0; n < count; n++)
            {
                var shelf = new List<PieceState>();
                foreach (PieceState p in s.pieces) if (!p.Placed && p.Slot != null && p.Slot.gameObject.activeSelf) shelf.Add(p);
                if (shelf.Count <= 1) return;
                PieceState piece = shelf[rng.Next(shelf.Count)];
                for (int attempt = 0; attempt < 25; attempt++)
                {
                    int row = rng.Next(s.activeLevel.Rows), col = rng.Next(s.activeLevel.Cols);
                    if (!game.Board.CanPlace(piece, row, col)) continue;
                    game.Drag.PlacePiece(piece, row, col, true);
                    s.inputLocked = false;
                    break;
                }
                if (s.IsBoardFilled) return;
            }
        }

        // Returns null when following the hint fills the board, else a description of where it went wrong.
        private static string Follow(CatBlockPuzzleGame game, HintPresenter hint, MethodInfo pick, PuzzleSession s, int level)
        {
            int limit = s.pieces.Count * 4 + 8;
            for (int step = 0; step < limit; step++)
            {
                if (s.IsBoardFilled) return null;
                var args = new object[] { null, null };
                var hinted = (PieceState)pick.Invoke(hint, args);
                int left = 0;
                foreach (PieceState p in s.pieces) if (!p.Placed && !p.Definition.IsSpare) left++;
                if (hinted == null) return "hint falls silent after " + step + " steps with " + left + " cat(s) still off the board (" + Describe(s) + ")";
                var blockers = (List<PieceState>)args[0];
                var spot = (PieceState)args[1] ?? hinted;
                if (blockers != null) foreach (PieceState b in blockers) game.Drag.KnockToShelf(b);
                if (hinted.Placed)
                {
                    // A lift-only hint: the cat is wrong where it is. Lifting it is the whole step.
                    game.Drag.KnockToShelf(hinted);
                    s.inputLocked = false;
                    continue;
                }
                s.inputLocked = false;
                int row = spot.Solution.SolutionRow, col = spot.Solution.SolutionCol;
                if (!game.Board.CanPlace(hinted, row, col))
                    return "step " + step + ": hint sends '" + hinted.Definition.Name + "' to (" + row + "," + col + ") where it cannot be placed. " + WhyNot(game, s, hinted, row, col) + " | spotOwner=" + spot.Definition.Name + " blockers=" + (blockers == null ? 0 : blockers.Count) + " | " + Describe(s);
                game.Drag.PlacePiece(hinted, row, col, true);
                s.inputLocked = false;
            }
            return "hint loops without finishing the board";
        }
    }
}
#endif
