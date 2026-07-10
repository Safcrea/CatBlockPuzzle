using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private sealed class DragState
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
            public bool BoardClampActive;
            public int Row;
            public int Col;
            public Vector2 PointerStartScreen;
            public Vector2 LastPointerScreen;
            public Vector2 LastAnchorScreen;
            public Vector2 TargetPosition;
            public Vector2 SmoothVelocity;
        }

        private readonly struct CellView
        {
            public readonly RectTransform Rect;
            public readonly Image Image;
            public readonly Image Preview;
            public readonly Color BaseColor;

            public CellView(RectTransform rect, Image image, Image preview, Color baseColor)
            {
                Rect = rect;
                Image = image;
                Preview = preview;
                BaseColor = baseColor;
            }
        }

        private readonly struct CellGrab
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

        private readonly struct BoardRevealCell
        {
            public readonly RectTransform Rect;
            public readonly int Diagonal;

            public BoardRevealCell(RectTransform rect, int diagonal)
            {
                Rect = rect;
                Diagonal = diagonal;
            }
        }

    }
}
