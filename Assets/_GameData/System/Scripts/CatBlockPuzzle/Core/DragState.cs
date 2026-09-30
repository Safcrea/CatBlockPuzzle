using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Everything about the piece currently held by the pointer.</summary>
    internal sealed class DragState
    {
        public PieceState Piece;
        public bool PreviousPlaced;
        public int PreviousRow;
        public int PreviousCol;
        public CellGrab Grabbed;
        public float SourceCellWidth;
        public float SourceCellHeight;
        public float SourceGapX;
        public float SourceGapY;
        public float Lift;
        public float ResizeElapsed;
        public int PointerId;
        public bool IsTouch;
        public bool Valid;
        public bool ReturnToShelf;
        /// <summary>True while the held cat overlaps the board area, whether or not it can land there.</summary>
        public bool NearBoard;
        public int Row;
        public int Col;
        public Vector2 PointerStartScreen;
        public Vector2 LastPointerScreen;
        public Vector2 LastAnchorScreen;
        public Vector2 TargetPosition;
        public Vector2 SmoothVelocity;
        public Vector2 JellyScale = Vector2.one;
        public float PickupScale = 1f;
    }

    /// <summary>Which cell of a piece the pointer grabbed, and where inside that cell.</summary>
    internal readonly struct CellGrab
    {
        public readonly int Row;
        public readonly int Col;
        public readonly float OffsetX;
        public readonly float OffsetY;

        public CellGrab(int row, int col, float offsetX, float offsetY)
        {
            Row = row;
            Col = col;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }
}
