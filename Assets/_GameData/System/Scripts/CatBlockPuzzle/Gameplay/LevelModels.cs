using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal readonly struct CellOffset
    {
        public readonly int Row;
        public readonly int Col;

        public CellOffset(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    internal sealed class PieceDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly CellOffset[] Cells;
        public readonly int Rows;
        public readonly int Cols;
        public readonly int SolutionRow;
        public readonly int SolutionCol;
        public readonly string ShapeId;
        /// <summary>A decoy cat: it is on the shelf but not part of the solution, so the level is won without it.</summary>
        public readonly bool IsSpare;
        /// <summary>A cat that starts on the board at its solution spot and can never be picked up.</summary>
        public readonly bool IsFixed;

        public PieceDefinition(string id, string name, string shapeId, CellOffset[] cells, int solutionRow, int solutionCol, bool isSpare = false,
            bool isFixed = false)
        {
            IsSpare = isSpare;
            IsFixed = isFixed;
            Id = id;
            Name = name;
            ShapeId = shapeId;
            Cells = cells;
            SolutionRow = solutionRow;
            SolutionCol = solutionCol;
            for (int i = 0; i < cells.Length; i++)
            {
                Rows = Mathf.Max(Rows, cells[i].Row + 1);
                Cols = Mathf.Max(Cols, cells[i].Col + 1);
            }
        }

        /// <summary>The same cat turned 90 degrees clockwise, normalised to a top-left of (0,0). Cell order is kept.</summary>
        public PieceDefinition RotatedClockwise()
        {
            CellOffset[] turned = new CellOffset[Cells.Length];
            int minRow = int.MaxValue, minCol = int.MaxValue;
            for (int i = 0; i < Cells.Length; i++)
            {
                turned[i] = new CellOffset(Cells[i].Col, Rows - 1 - Cells[i].Row);
                minRow = Mathf.Min(minRow, turned[i].Row);
                minCol = Mathf.Min(minCol, turned[i].Col);
            }
            for (int i = 0; i < turned.Length; i++) turned[i] = new CellOffset(turned[i].Row - minRow, turned[i].Col - minCol);
            return new PieceDefinition(Id, Name, ShapeId, turned, SolutionRow, SolutionCol, IsSpare, IsFixed);
        }
    }

    /// <summary>How a padlocked cell opens: when a named cat lands, when enough cats have landed, or when its neighbours are filled.</summary>
    internal readonly struct CellLockRule
    {
        /// <summary>Counter rule: opens once this many cats are placed. Zero with no key means the neighbour rule.</summary>
        public readonly int After;
        /// <summary>Key rule: opens once the piece with this id is placed.</summary>
        public readonly string KeyPieceId;

        public CellLockRule(int after, string keyPieceId = null)
        {
            After = after;
            KeyPieceId = keyPieceId;
        }

        public bool IsKey => !string.IsNullOrEmpty(KeyPieceId);
    }

    internal sealed class LevelDefinition
    {
        public readonly string Id;
        public readonly string Title;
        public readonly int Difficulty;
        public readonly int Rows;
        public readonly int Cols;
        public readonly int Reward;
        public readonly PieceDefinition[] Pieces;
        public readonly HashSet<Vector2Int> ActiveCells;
        /// <summary>Grid cells shown as sleeping cats: visibly part of the board, never placeable.</summary>
        public readonly HashSet<Vector2Int> BlockedCells;

        /// <summary>Solution cells that start padlocked, mapped to the rule that opens each one.</summary>
        public readonly Dictionary<Vector2Int, CellLockRule> LockedCells;

        /// <summary>Drip-fed tray: how many unplaced cats the shelf shows at once (0 = all of them).</summary>
        public readonly int TrayWindow;
        public bool HasTrayWindow => TrayWindow > 0 && TrayWindow < Pieces.Length;

        /// <summary>Colour zones: cells only the named piece may occupy.</summary>
        public readonly Dictionary<Vector2Int, string> ZoneCells;
        public bool HasZones => ZoneCells.Count > 0;

        /// <summary>Decoy cats added to the shelf at runtime; they never count towards the solution.</summary>
        public readonly PieceDefinition[] Spares;
        public bool HasSpares => Spares.Length > 0;

        /// <summary>Mistake hearts for one attempt (0 = unlimited mistakes).</summary>
        public readonly int Hearts;
        public bool HasHearts => Hearts > 0;

        /// <summary>Foggy grid cells: a cloud hides whatever is there until a cat lands beside it.</summary>
        public readonly HashSet<Vector2Int> FogCells;
        public bool HasFog => FogCells.Count > 0;

        /// <summary>Move-limit stars: three stars for this many drops or fewer (0 = stars come from the timer).</summary>
        public readonly int Moves;
        public bool HasMoves => Moves > 0;

        public bool HasFixedPieces
        {
            get
            {
                foreach (PieceDefinition piece in Pieces) if (piece.IsFixed) return true;
                return false;
            }
        }

        public bool HasBlockedCells => BlockedCells.Count > 0;
        public bool HasLockedCells => LockedCells.Count > 0;

        public bool HasKeyLocks
        {
            get
            {
                foreach (CellLockRule rule in LockedCells.Values) if (rule.IsKey) return true;
                return false;
            }
        }

        public LevelDefinition(string id, string title, int difficulty, int rows, int cols, int reward, PieceDefinition[] pieces,
            IEnumerable<Vector2Int> blockedCells = null, IDictionary<Vector2Int, CellLockRule> lockedCells = null, int trayWindow = 0,
            IDictionary<Vector2Int, string> zoneCells = null, PieceDefinition[] spares = null, int hearts = 0,
            IEnumerable<Vector2Int> fogCells = null, int moves = 0)
        {
            FogCells = fogCells != null ? new HashSet<Vector2Int>(fogCells) : new HashSet<Vector2Int>();
            Moves = Mathf.Max(0, moves);
            Hearts = Mathf.Max(0, hearts);
            Spares = spares ?? new PieceDefinition[0];
            ZoneCells = zoneCells != null ? new Dictionary<Vector2Int, string>(zoneCells) : new Dictionary<Vector2Int, string>();
            TrayWindow = Mathf.Max(0, trayWindow);
            LockedCells = lockedCells != null ? new Dictionary<Vector2Int, CellLockRule>(lockedCells) : new Dictionary<Vector2Int, CellLockRule>();
            Id = id;
            Title = title;
            Difficulty = difficulty;
            Rows = rows;
            Cols = cols;
            Reward = reward;
            Pieces = pieces;
            BlockedCells = blockedCells != null ? new HashSet<Vector2Int>(blockedCells) : new HashSet<Vector2Int>();
            ActiveCells = new HashSet<Vector2Int>();
            for (int i = 0; i < pieces.Length; i++)
            {
                PieceDefinition piece = pieces[i];
                for (int c = 0; c < piece.Cells.Length; c++)
                {
                    CellOffset cell = piece.Cells[c];
                    ActiveCells.Add(new Vector2Int(piece.SolutionRow + cell.Row, piece.SolutionCol + cell.Col));
                }
            }
        }
    }

    #pragma warning disable 0649

    [Serializable]
    internal sealed class LevelPackData
    {
        public int version;
        public LevelData[] levels;
    }

    [Serializable]
    internal sealed class LevelData
    {
        public string id;
        public string title;
        public int difficulty;
        public int rows;
        public int cols;
        public int reward;
        public PieceData[] pieces;
        /// <summary>Optional. Free cells drawn as sleeping cats so the board reads as a full rectangle.</summary>
        public BlockedCellData[] blocked;
        /// <summary>Optional. Solution cells that start locked. key opens once that piece is placed; otherwise after > 0 opens once that many cats are placed, and 0 opens once its free neighbours are filled.</summary>
        public LockedCellData[] locked;
        /// <summary>Optional. Drip-fed tray: only this many unplaced cats are on the shelf at once, in piece order.</summary>
        public int trayWindow;
        /// <summary>Optional. Colour zones: each cell only accepts the named piece, which must cover it in the solution.</summary>
        public ZoneCellData[] zones;
        /// <summary>Optional. Decoy cats (id, name, shape; row/col unused) that do not belong in the solution.</summary>
        public PieceData[] spares;
        /// <summary>Optional. Mistake hearts: bad drops on the board allowed before the level fails.</summary>
        public int hearts;
        /// <summary>Optional. Foggy cells inside the grid (board cells, bowls or empty space); a cat landing beside one clears it.</summary>
        public BlockedCellData[] fog;
        /// <summary>Optional. Move-limit stars: 3 stars for this many drops or fewer, 2 for up to three more, else 1.</summary>
        public int moves;
    }

    [Serializable]
    internal sealed class ZoneCellData
    {
        public int row;
        public int col;
        public string piece;
    }

    [Serializable]
    internal sealed class LockedCellData
    {
        public int row;
        public int col;
        public int after;
        public string key;
    }

    [Serializable]
    internal sealed class BlockedCellData
    {
        public int row;
        public int col;
    }

    [Serializable]
    internal sealed class PieceData
    {
        public string id;
        public string name;
        public string shape;
        public int row;
        public int col;
        /// <summary>Optional. The cat starts on the board at row/col and never goes to the shelf.</summary>
        public bool @fixed;
    }

    #pragma warning restore 0649
}
