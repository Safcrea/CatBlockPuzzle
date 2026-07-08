using System;
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

                converted[i] = new LevelDefinition(data[i].id, data[i].title, data[i].difficulty, data[i].rows, data[i].cols, data[i].reward, pieces);
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
