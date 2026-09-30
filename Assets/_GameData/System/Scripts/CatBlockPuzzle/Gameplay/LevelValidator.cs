using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal static class LevelValidator
    {
        public static bool TryValidatePack(LevelPackData pack, out string error)
        {
            if (pack == null)
            {
                error = "Level JSON could not be parsed.";
                return false;
            }

            if (pack.levels == null || pack.levels.Length == 0)
            {
                error = "Level JSON has no levels.";
                return false;
            }

            if (pack.levels.Length != 100)
            {
                error = "Level JSON must contain exactly 100 levels. Found " + pack.levels.Length + ".";
                return false;
            }

            HashSet<string> levelIds = new HashSet<string>();
            for (int i = 0; i < pack.levels.Length; i++)
            {
                if (!TryValidateLevel(pack.levels[i], i, levelIds, out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool TryValidateLevel(LevelData level, int index, HashSet<string> levelIds, out string error)
        {
            int number = index + 1;
            if (level == null)
            {
                error = "Level " + number + " is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(level.id))
            {
                error = "Level " + number + " has no id.";
                return false;
            }

            if (!levelIds.Add(level.id))
            {
                error = "Duplicate level id: " + level.id + ".";
                return false;
            }

            if (level.rows < 1 || level.rows > 8 || level.cols < 1 || level.cols > 8)
            {
                error = level.id + " grid must be between 1x1 and 8x8.";
                return false;
            }

            if (level.difficulty < 1 || level.difficulty > 10)
            {
                error = level.id + " difficulty must be 1-10.";
                return false;
            }

            if (level.pieces == null || level.pieces.Length == 0)
            {
                error = level.id + " has no pieces.";
                return false;
            }

            HashSet<string> pieceIds = new HashSet<string>();
            Dictionary<string, int> pieceIndex = new Dictionary<string, int>();
            List<Vector2Int>[] pieceCells = new List<Vector2Int>[level.pieces.Length];
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, int> owner = new Dictionary<Vector2Int, int>();
            int cellTotal = 0;

            for (int p = 0; p < level.pieces.Length; p++)
            {
                PieceData piece = level.pieces[p];
                if (piece == null)
                {
                    error = level.id + " piece " + p + " is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(piece.id) || !pieceIds.Add(piece.id))
                {
                    error = level.id + " has a missing or duplicate piece id.";
                    return false;
                }

                pieceIndex[piece.id] = p;
                pieceCells[p] = new List<Vector2Int>();
                if (!ShapeLibrary.TryGetShape(piece.shape, out CellOffset[] cells))
                {
                    error = level.id + " uses unknown shape " + piece.shape + ".";
                    return false;
                }

                for (int c = 0; c < cells.Length; c++)
                {
                    Vector2Int coord = new Vector2Int(piece.row + cells[c].Row, piece.col + cells[c].Col);
                    if (coord.x < 0 || coord.x >= level.rows || coord.y < 0 || coord.y >= level.cols)
                    {
                        error = level.id + " piece " + piece.id + " is out of bounds.";
                        return false;
                    }

                    if (!occupied.Add(coord))
                    {
                        error = level.id + " has overlapping pieces at " + coord + ".";
                        return false;
                    }

                    owner[coord] = p;
                    pieceCells[p].Add(coord);

                    cellTotal++;
                }
            }

            if (occupied.Count == 0 || occupied.Count != cellTotal)
            {
                error = level.id + " active-cell coverage is invalid.";
                return false;
            }

            if (level.blocked != null)
            {
                HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
                for (int b = 0; b < level.blocked.Length; b++)
                {
                    BlockedCellData cell = level.blocked[b];
                    Vector2Int coord = new Vector2Int(cell.row, cell.col);
                    if (cell == null || coord.x < 0 || coord.x >= level.rows || coord.y < 0 || coord.y >= level.cols)
                    {
                        error = level.id + " has a blocked cell outside the grid.";
                        return false;
                    }

                    if (occupied.Contains(coord))
                    {
                        error = level.id + " blocks a cell its solution needs at " + coord + ".";
                        return false;
                    }

                    if (!blocked.Add(coord))
                    {
                        error = level.id + " lists blocked cell " + coord + " twice.";
                        return false;
                    }
                }
            }

            if (level.trayWindow < 0)
            {
                error = level.id + " has a negative tray window.";
                return false;
            }

            if (level.locked != null && level.locked.Length > 0 && !TryValidateLocks(level, occupied, owner, pieceIndex, pieceCells, out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        // Locks must sit on solution cells and be openable: a counter lock needs enough
        // unlocked pieces to reach its count, a neighbour lock needs at least one free
        // neighbour that belongs to another piece (its own piece can never fill them).
        private static bool TryValidateLocks(LevelData level, HashSet<Vector2Int> occupied, Dictionary<Vector2Int, int> owner,
            Dictionary<string, int> pieceIndex, List<Vector2Int>[] pieceCells, out string error)
        {
            Dictionary<Vector2Int, int> locked = new Dictionary<Vector2Int, int>();
            Dictionary<Vector2Int, int> keys = new Dictionary<Vector2Int, int>();
            HashSet<int> lockedPieces = new HashSet<int>();
            for (int i = 0; i < level.locked.Length; i++)
            {
                LockedCellData cell = level.locked[i];
                if (cell == null)
                {
                    error = level.id + " lock " + i + " is null.";
                    return false;
                }

                Vector2Int coord = new Vector2Int(cell.row, cell.col);
                if (!occupied.Contains(coord))
                {
                    error = level.id + " locks " + coord + ", which no piece fills.";
                    return false;
                }

                if (cell.after < 0)
                {
                    error = level.id + " lock " + coord + " has a negative count.";
                    return false;
                }

                if (locked.ContainsKey(coord))
                {
                    error = level.id + " lists lock " + coord + " twice.";
                    return false;
                }

                if (!string.IsNullOrEmpty(cell.key))
                {
                    if (!pieceIndex.TryGetValue(cell.key, out int keyPiece))
                    {
                        error = level.id + " lock " + coord + " names unknown key piece " + cell.key + ".";
                        return false;
                    }

                    if (keyPiece == owner[coord])
                    {
                        error = level.id + " lock " + coord + " is opened by its own piece " + cell.key + ".";
                        return false;
                    }

                    keys[coord] = keyPiece;
                }

                locked[coord] = cell.after;
                lockedPieces.Add(owner[coord]);
            }

            int freePieces = level.pieces.Length - lockedPieces.Count;
            if (freePieces == 0)
            {
                error = level.id + " locks every piece; nothing can be placed first.";
                return false;
            }

            foreach (KeyValuePair<Vector2Int, int> entry in locked)
            {
                if (keys.ContainsKey(entry.Key))
                {
                    continue;
                }

                if (entry.Value > 0)
                {
                    continue;
                }

                int self = owner[entry.Key];
                bool hasUnlocker = false;
                Vector2Int[] neighbours =
                {
                    new Vector2Int(entry.Key.x - 1, entry.Key.y), new Vector2Int(entry.Key.x + 1, entry.Key.y),
                    new Vector2Int(entry.Key.x, entry.Key.y - 1), new Vector2Int(entry.Key.x, entry.Key.y + 1)
                };
                foreach (Vector2Int n in neighbours)
                {
                    if (occupied.Contains(n) && !locked.ContainsKey(n) && owner[n] != self)
                    {
                        hasUnlocker = true;
                        break;
                    }
                }

                if (!hasUnlocker)
                {
                    error = level.id + " neighbour lock " + entry.Key + " has no free neighbour from another piece to open it.";
                    return false;
                }
            }

            return CanComplete(level, owner, pieceCells, locked, keys, out error);
        }

        // Opening is monotonic (placing more cats never closes a lock), so a greedy pass that
        // places whatever is placeable completes the level exactly when some order does.
        private static bool CanComplete(LevelData level, Dictionary<Vector2Int, int> owner, List<Vector2Int>[] pieceCells,
            Dictionary<Vector2Int, int> locked, Dictionary<Vector2Int, int> keys, out string error)
        {
            int count = level.pieces.Length;
            Dictionary<Vector2Int, List<Vector2Int>> unlockers = new Dictionary<Vector2Int, List<Vector2Int>>();
            foreach (KeyValuePair<Vector2Int, int> entry in locked)
            {
                if (keys.ContainsKey(entry.Key) || entry.Value > 0)
                {
                    continue;
                }

                List<Vector2Int> beside = new List<Vector2Int>();
                foreach (Vector2Int n in Neighbours(entry.Key))
                {
                    if (owner.TryGetValue(n, out int neighbourOwner) && neighbourOwner != owner[entry.Key] && !locked.ContainsKey(n))
                    {
                        beside.Add(n);
                    }
                }

                unlockers[entry.Key] = beside;
            }

            HashSet<Vector2Int> open = new HashSet<Vector2Int>();
            HashSet<Vector2Int> filled = new HashSet<Vector2Int>();
            bool[] placed = new bool[count];
            int placedCount = 0;
            bool progressed = true;
            while (progressed && placedCount < count)
            {
                progressed = false;
                int visibleRank = 0;
                for (int i = 0; i < count; i++)
                {
                    if (placed[i])
                    {
                        continue;
                    }

                    // A drip-fed shelf only offers the first trayWindow unplaced cats.
                    if (level.trayWindow > 0 && visibleRank++ >= level.trayWindow)
                    {
                        break;
                    }

                    if (!IsPlaceable(pieceCells[i], locked, open))
                    {
                        continue;
                    }

                    placed[i] = true;
                    placedCount++;
                    progressed = true;
                    filled.UnionWith(pieceCells[i]);
                    bool changed = true;
                    while (changed)
                    {
                        changed = false;
                        foreach (KeyValuePair<Vector2Int, int> entry in locked)
                        {
                            if (open.Contains(entry.Key))
                            {
                                continue;
                            }

                            bool opens;
                            if (keys.TryGetValue(entry.Key, out int keyPiece)) opens = placed[keyPiece];
                            else if (entry.Value > 0) opens = placedCount >= entry.Value;
                            else opens = AllFilled(unlockers[entry.Key], filled);
                            if (opens)
                            {
                                open.Add(entry.Key);
                                changed = true;
                            }
                        }
                    }

                    break;
                }
            }

            if (placedCount == count)
            {
                error = string.Empty;
                return true;
            }

            error = level.id + " cannot be completed: its locks wait on each other (" + (count - placedCount) + " cats could never be placed).";
            return false;
        }

        private static bool IsPlaceable(List<Vector2Int> cells, Dictionary<Vector2Int, int> locked, HashSet<Vector2Int> open)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (locked.ContainsKey(cells[i]) && !open.Contains(cells[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllFilled(List<Vector2Int> cells, HashSet<Vector2Int> filled)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!filled.Contains(cells[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector2Int[] Neighbours(Vector2Int c) =>
            new[] { new Vector2Int(c.x - 1, c.y), new Vector2Int(c.x + 1, c.y), new Vector2Int(c.x, c.y - 1), new Vector2Int(c.x, c.y + 1) };
    }
}
