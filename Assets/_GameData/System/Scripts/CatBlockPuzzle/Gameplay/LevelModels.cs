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

        public PieceDefinition(string id, string name, string shapeId, CellOffset[] cells, int solutionRow, int solutionCol)
        {
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

        /// <summary>Solution cells that start padlocked, mapped to their unlock count (0 = neighbour rule).</summary>
        public readonly Dictionary<Vector2Int, int> LockedCells;

        public bool HasBlockedCells => BlockedCells.Count > 0;
        public bool HasLockedCells => LockedCells.Count > 0;

        public LevelDefinition(string id, string title, int difficulty, int rows, int cols, int reward, PieceDefinition[] pieces,
            IEnumerable<Vector2Int> blockedCells = null, IDictionary<Vector2Int, int> lockedCells = null)
        {
            LockedCells = lockedCells != null ? new Dictionary<Vector2Int, int>(lockedCells) : new Dictionary<Vector2Int, int>();
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
        /// <summary>Optional. Solution cells that start locked; after > 0 opens once that many cats are placed, 0 opens once its free neighbours are filled.</summary>
        public LockedCellData[] locked;
    }

    [Serializable]
    internal sealed class LockedCellData
    {
        public int row;
        public int col;
        public int after;
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
    }

    #pragma warning restore 0649
}
