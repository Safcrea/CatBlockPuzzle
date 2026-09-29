using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>The runtime view of one board cell.</summary>
    internal readonly struct CellView
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

    /// <summary>A board cell that takes part in the diagonal level-start reveal.</summary>
    internal readonly struct BoardRevealCell
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
