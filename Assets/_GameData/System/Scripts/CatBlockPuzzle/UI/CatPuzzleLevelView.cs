using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class CatPuzzleLevelView : MonoBehaviour
    {
        public int levelIndex;
        public RectTransform board;
        public RectTransform boardFrame;
        public Image boardFrameImage;
        public Outline boardOutline;
        public Image[] boardEars;
        public RectTransform tray;
        public Image trayImage;
        public RectTransform viewport;
        public RectTransform content;
        public HorizontalLayoutGroup layout;
        public ScrollRect scroll;
        public RectTransform pieceLayer;
        public BoardCell[] cells;
        public CatPuzzlePieceView[] pieces;

        [Serializable]
        public struct BoardCell
        {
            public int row;
            public int col;
            public bool active;
            public Image image;
            public Image preview;
            public RectTransform shine;
            public RectTransform paw;
        }
    }
}
