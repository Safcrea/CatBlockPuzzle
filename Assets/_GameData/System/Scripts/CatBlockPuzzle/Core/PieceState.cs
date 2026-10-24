using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Runtime state of one puzzle piece: its definition, tray card, visual root and grid metrics.</summary>
    internal sealed class PieceState
    {
        /// <summary>The cat as it is now: the Rotate power-up can turn it away from its authored shape.</summary>
        public PieceDefinition Definition;
        /// <summary>The cat as authored; its cells at SolutionRow/SolutionCol are its spot in the solution.</summary>
        public readonly PieceDefinition Solution;
        public readonly Color Color;
        public readonly List<Image> CellImages = new List<Image>();
        public readonly List<CatCellView> CatViews = new List<CatCellView>();
        public RectTransform Rect;
        public RectTransform Slot;
        public Image SlotImage;
        public LayoutElement SlotLayout;
        public bool Placed;
        public int Row = -1;
        public int Col = -1;
        public float CellWidth;
        public float CellHeight;
        public float GapX;
        public float GapY;
        public float FloatPhase;
        public bool HasGridLayout;
        public bool RestingLayout;
        public int AtlasIndex;
        public Coroutine SizeRoutine;
        public Coroutine SlotMoveRoutine;
        public readonly Vector2Int[] PlacementOffsets;
        public readonly Vector2[] GridCenters;

        public PieceState(PieceDefinition definition, Color color)
        {
            Definition = definition;
            Solution = definition;
            Color = color;
            PlacementOffsets = new Vector2Int[definition.Cells.Length];
            GridCenters = new Vector2[definition.Cells.Length];
            for (int i = 0; i < definition.Cells.Length; i++)
            {
                PlacementOffsets[i] = new Vector2Int(definition.Cells[i].Row, definition.Cells[i].Col);
            }
        }

        public bool IsRotated => Definition != Solution;

        /// <summary>Turns the cat 90 degrees clockwise; its cat images keep their order, so a re-layout moves them.</summary>
        public void RotateClockwise() => UseShape(Definition.RotatedClockwise());

        /// <summary>Back to the authored shape, for a fresh attempt.</summary>
        public void ResetRotation()
        {
            if (IsRotated) UseShape(Solution);
        }

        private void UseShape(PieceDefinition shape)
        {
            Definition = shape;
            HasGridLayout = false;
            for (int i = 0; i < shape.Cells.Length; i++) PlacementOffsets[i] = new Vector2Int(shape.Cells[i].Row, shape.Cells[i].Col);
        }
    }

    /// <summary>The visual parts of one cat cell: either an authored portrait or the generated face rig.</summary>
    internal sealed class CatCellView
    {
        public readonly RectTransform Body;
        public readonly Image Portrait;
        public readonly bool Authored;
        public readonly RectTransform LeftEar;
        public readonly RectTransform RightEar;
        public readonly RectTransform LeftInnerEar;
        public readonly RectTransform RightInnerEar;
        public readonly RectTransform Highlight;
        public readonly RectTransform LeftEye;
        public readonly RectTransform RightEye;
        public readonly RectTransform Nose;
        public readonly RectTransform Mouth;
        public readonly RectTransform LeftCheek;
        public readonly RectTransform RightCheek;
        public readonly RectTransform LeftWhiskerTop;
        public readonly RectTransform LeftWhiskerBottom;
        public readonly RectTransform RightWhiskerTop;
        public readonly RectTransform RightWhiskerBottom;
        public readonly RectTransform ForeheadStripe;
        public readonly RectTransform Tail;

        public CatCellView(RectTransform body, Image portrait)
        {
            Body = body;
            Portrait = portrait;
            Authored = true;
        }

        public CatCellView(
            RectTransform body,
            RectTransform leftEar,
            RectTransform rightEar,
            RectTransform leftInnerEar,
            RectTransform rightInnerEar,
            RectTransform highlight,
            RectTransform leftEye,
            RectTransform rightEye,
            RectTransform nose,
            RectTransform mouth,
            RectTransform leftCheek,
            RectTransform rightCheek,
            RectTransform leftWhiskerTop,
            RectTransform leftWhiskerBottom,
            RectTransform rightWhiskerTop,
            RectTransform rightWhiskerBottom,
            RectTransform foreheadStripe,
            RectTransform tail)
        {
            Body = body;
            Portrait = body != null ? body.GetComponent<Image>() : null;
            Authored = false;
            LeftEar = leftEar;
            RightEar = rightEar;
            LeftInnerEar = leftInnerEar;
            RightInnerEar = rightInnerEar;
            Highlight = highlight;
            LeftEye = leftEye;
            RightEye = rightEye;
            Nose = nose;
            Mouth = mouth;
            LeftCheek = leftCheek;
            RightCheek = rightCheek;
            LeftWhiskerTop = leftWhiskerTop;
            LeftWhiskerBottom = leftWhiskerBottom;
            RightWhiskerTop = rightWhiskerTop;
            RightWhiskerBottom = rightWhiskerBottom;
            ForeheadStripe = foreheadStripe;
            Tail = tail;
        }
    }
}
