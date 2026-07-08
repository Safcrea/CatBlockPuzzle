using System.Collections.Generic;

namespace CatBlockPuzzle
{
    internal static class ShapeLibrary
    {
        private static readonly Dictionary<string, CellOffset[]> Shapes = new Dictionary<string, CellOffset[]>
        {
            { "i3v", Cells((0, 0), (1, 0), (2, 0)) },
            { "i3h", Cells((0, 0), (0, 1), (0, 2)) },
            { "i4v", Cells((0, 0), (1, 0), (2, 0), (3, 0)) },
            { "i4h", Cells((0, 0), (0, 1), (0, 2), (0, 3)) },
            { "i5h", Cells((0, 0), (0, 1), (0, 2), (0, 3), (0, 4)) },
            { "l3", Cells((0, 0), (1, 0), (1, 1)) },
            { "l4", Cells((0, 0), (1, 0), (2, 0), (2, 1)) },
            { "l4Top", Cells((0, 0), (0, 1), (0, 2), (1, 2)) },
            { "l4Corner", Cells((0, 0), (1, 0), (1, 1), (1, 2)) },
            { "j4", Cells((0, 1), (1, 1), (2, 0), (2, 1)) },
            { "l5", Cells((0, 0), (1, 0), (2, 0), (3, 0), (3, 1)) },
            { "l5Top", Cells((0, 0), (0, 1), (0, 2), (0, 3), (1, 3)) },
            { "o4", Cells((0, 0), (0, 1), (1, 0), (1, 1)) },
            { "p5", Cells((0, 0), (0, 1), (1, 0), (1, 1), (2, 1)) },
            { "t4", Cells((0, 0), (0, 1), (0, 2), (1, 1)) },
            { "t5", Cells((0, 1), (1, 0), (1, 1), (1, 2), (2, 1)) },
            { "s4", Cells((0, 1), (0, 2), (1, 0), (1, 1)) },
            { "z4", Cells((0, 0), (0, 1), (1, 1), (1, 2)) },
            { "s5", Cells((0, 1), (0, 2), (1, 0), (1, 1), (2, 0)) },
            { "z5", Cells((0, 0), (0, 1), (1, 1), (1, 2), (2, 2)) },
            { "u5", Cells((0, 0), (0, 2), (1, 0), (1, 1), (1, 2)) },
            { "plus5", Cells((0, 1), (1, 0), (1, 1), (1, 2), (2, 1)) },
            { "stair5", Cells((0, 0), (1, 0), (1, 1), (2, 1), (2, 2)) }
        };

        public static bool TryGetShape(string id, out CellOffset[] cells)
        {
            return Shapes.TryGetValue(id, out cells);
        }

        private static CellOffset[] Cells(params (int row, int col)[] values)
        {
            CellOffset[] cells = new CellOffset[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                cells[i] = new CellOffset(values[i].row, values[i].col);
            }

            return cells;
        }
    }
}
