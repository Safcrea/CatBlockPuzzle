using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal static class CatPuzzleBoardLayout
    {
        // Fit the entire grid, including gaps, using square cells at every aspect ratio.
        public static Vector2 Fit(int rows, int cols, Vector2 available, float gap)
        {
            if (rows <= 0 || cols <= 0 || available.x <= 0f || available.y <= 0f || gap < 0f)
                throw new ArgumentOutOfRangeException(nameof(available), "Grid dimensions and available space must be positive.");
            float cell = Mathf.Min((available.x - gap * (cols - 1)) / cols,
                (available.y - gap * (rows - 1)) / rows);
            if (cell <= 0f) throw new ArgumentOutOfRangeException(nameof(available), "Grid gaps exceed the available space.");
            return new Vector2(cell * cols + gap * (cols - 1), cell * rows + gap * (rows - 1));
        }
    }
}
