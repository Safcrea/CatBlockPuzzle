using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal sealed class LevelManager
    {
        private readonly string resourcePath;
        private LevelDefinition[] levels = Array.Empty<LevelDefinition>();

        public int LevelCount => levels.Length;

        public LevelManager(string resourcePath)
        {
            this.resourcePath = resourcePath;
        }

        internal LevelManager(LevelData[] data)
        {
            levels = ConvertLevels(data);
        }

        public void Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing level JSON at Resources/" + resourcePath + ".json");
            }

            LevelPackData pack = JsonUtility.FromJson<LevelPackData>(asset.text);
            if (!LevelValidator.TryValidatePack(pack, out string error))
            {
                throw new InvalidOperationException("Invalid Cat Block Puzzle level data: " + error);
            }

            levels = ConvertLevels(pack.levels);
            Debug.Log(BuildDebugReport());
        }

        public LevelDefinition GetLevel(int index)
        {
            if (levels.Length == 0)
            {
                throw new InvalidOperationException("Levels have not been loaded.");
            }

            return levels[Mathf.Clamp(index, 0, levels.Length - 1)];
        }

        private static LevelDefinition[] ConvertLevels(LevelData[] data)
        {
            LevelDefinition[] converted = new LevelDefinition[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                PieceDefinition[] pieces = new PieceDefinition[data[i].pieces.Length];
                for (int p = 0; p < data[i].pieces.Length; p++)
                {
                    PieceData piece = data[i].pieces[p];
                    ShapeLibrary.TryGetShape(piece.shape, out CellOffset[] cells);
                    pieces[p] = new PieceDefinition(piece.id, piece.name, piece.shape, cells, piece.row, piece.col);
                }

                List<Vector2Int> blocked = new List<Vector2Int>();
                if (data[i].blocked != null)
                {
                    foreach (BlockedCellData cell in data[i].blocked) blocked.Add(new Vector2Int(cell.row, cell.col));
                }

                PieceDefinition[] spares = new PieceDefinition[data[i].spares != null ? data[i].spares.Length : 0];
                for (int s = 0; s < spares.Length; s++)
                {
                    PieceData spare = data[i].spares[s];
                    ShapeLibrary.TryGetShape(spare.shape, out CellOffset[] spareCells);
                    spares[s] = new PieceDefinition(spare.id, spare.name, spare.shape, spareCells, -1, -1, true);
                }

                Dictionary<Vector2Int, string> zones = new Dictionary<Vector2Int, string>();
                if (data[i].zones != null)
                {
                    foreach (ZoneCellData cell in data[i].zones) zones[new Vector2Int(cell.row, cell.col)] = cell.piece;
                }

                Dictionary<Vector2Int, CellLockRule> locked = new Dictionary<Vector2Int, CellLockRule>();
                if (data[i].locked != null)
                {
                    foreach (LockedCellData cell in data[i].locked) locked[new Vector2Int(cell.row, cell.col)] = new CellLockRule(cell.after, cell.key);
                }

                converted[i] = new LevelDefinition(data[i].id, data[i].title, data[i].difficulty, data[i].rows, data[i].cols, data[i].reward, pieces, blocked, locked, data[i].trayWindow, zones, spares, data[i].hearts);
            }

            return converted;
        }

        private string BuildDebugReport()
        {
            int maxRows = 0;
            int maxCols = 0;
            int[] difficultyCounts = new int[11];
            for (int i = 0; i < levels.Length; i++)
            {
                maxRows = Mathf.Max(maxRows, levels[i].Rows);
                maxCols = Mathf.Max(maxCols, levels[i].Cols);
                difficultyCounts[Mathf.Clamp(levels[i].Difficulty, 1, 10)]++;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Cat Block Puzzle loaded ").Append(levels.Length).Append(" levels. Max grid ")
                .Append(maxRows).Append("x").Append(maxCols).Append(". Difficulty distribution:");
            for (int i = 1; i < difficultyCounts.Length; i++)
            {
                if (difficultyCounts[i] > 0)
                {
                    builder.Append(" ").Append(i).Append("=").Append(difficultyCounts[i]);
                }
            }

            return builder.ToString();
        }
    }
}
