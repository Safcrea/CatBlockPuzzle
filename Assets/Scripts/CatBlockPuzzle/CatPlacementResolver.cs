using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Result of matching every cat in a dragged group to available board cells.
    /// Grid-center coordinates use X for row and Y for column.
    /// </summary>
    public readonly struct CatPlacementResult
    {
        public readonly bool IsValid;
        public readonly int Row;
        public readonly int Col;
        public readonly float Score;

        public CatPlacementResult(bool isValid, int row, int col, float score)
        {
            IsValid = isValid;
            Row = row;
            Col = col;
            Score = score;
        }
    }

    public static class CatPlacementResolver
    {
        public static CatPlacementResult Resolve(
            IReadOnlyList<Vector2> catGridCenters,
            IReadOnlyList<Vector2Int> pieceCellOffsets,
            bool[,] availableCells,
            float threshold)
        {
            if (catGridCenters == null || pieceCellOffsets == null || availableCells == null ||
                catGridCenters.Count == 0 || catGridCenters.Count != pieceCellOffsets.Count)
            {
                return InvalidResult();
            }

            int rows = availableCells.GetLength(0);
            int cols = availableCells.GetLength(1);
            if (rows == 0 || cols == 0)
            {
                return InvalidResult();
            }

            float allowedDistance = Mathf.Clamp(threshold, 0.01f, 1f);
            float bestScore = float.PositiveInfinity;
            int bestRow = -1;
            int bestCol = -1;

            for (int originRow = 0; originRow < rows; originRow++)
            {
                for (int originCol = 0; originCol < cols; originCol++)
                {
                    float score = 0f;
                    bool valid = true;
                    for (int i = 0; i < pieceCellOffsets.Count; i++)
                    {
                        Vector2Int offset = pieceCellOffsets[i];
                        int targetRow = originRow + offset.x;
                        int targetCol = originCol + offset.y;
                        if (targetRow < 0 || targetRow >= rows || targetCol < 0 || targetCol >= cols ||
                            !availableCells[targetRow, targetCol])
                        {
                            valid = false;
                            break;
                        }

                        Vector2 center = catGridCenters[i];
                        float rowDistance = Mathf.Abs(center.x - targetRow);
                        float colDistance = Mathf.Abs(center.y - targetCol);
                        if (rowDistance > allowedDistance || colDistance > allowedDistance)
                        {
                            valid = false;
                            break;
                        }

                        score += (rowDistance * rowDistance) + (colDistance * colDistance);
                    }

                    if (!valid || score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestRow = originRow;
                    bestCol = originCol;
                }
            }

            return bestRow >= 0
                ? new CatPlacementResult(true, bestRow, bestCol, bestScore)
                : InvalidResult();
        }

        private static CatPlacementResult InvalidResult()
        {
            return new CatPlacementResult(false, -1, -1, float.PositiveInfinity);
        }
    }
}
