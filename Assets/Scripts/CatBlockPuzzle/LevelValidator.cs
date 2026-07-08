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
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
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

                    cellTotal++;
                }
            }

            if (occupied.Count == 0 || occupied.Count != cellTotal)
            {
                error = level.id + " active-cell coverage is invalid.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
