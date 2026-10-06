using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The state of the puzzle currently on the table: the active level, its pieces, the
    /// board occupancy, the drag in progress, board geometry, and the loaded level's view
    /// bindings. Services read and update this; it holds no behaviour of its own beyond
    /// simple queries.
    /// </summary>
    internal sealed class PuzzleSession
    {
        internal readonly Dictionary<Vector2Int, CellView> boardCells = new Dictionary<Vector2Int, CellView>();
        internal readonly Dictionary<Vector2Int, string> occupancy = new Dictionary<Vector2Int, string>();
        internal readonly List<PieceState> pieces = new List<PieceState>();
        internal readonly List<CellView> previewCells = new List<CellView>();
        internal readonly List<BoardRevealCell> boardRevealCells = new List<BoardRevealCell>();
        internal readonly List<Vector2Int> occupancyRemovalBuffer = new List<Vector2Int>(8);
        internal bool[,] placementAvailability = new bool[0, 0];

        internal LevelDefinition activeLevel;
        internal int levelIndex;
        /// <summary>1-based number shown to the player; keeps counting past the authored levels (see EndlessLevels).</summary>
        internal int levelNumber = 1;
        internal DragState drag;
        internal bool inputLocked;
        internal bool levelFailed;
        internal bool levelNavigationTesting;
        internal int comboCount;
        internal int bestCombo;
        internal int earnedStars;

        internal float boardWidth;
        internal float boardHeight;
        internal float boardCellWidth;
        internal float boardCellHeight;
        internal float boardCenterX;
        internal float boardCenterY = PuzzleTuning.BoardCenterY;

        // Bound from the loaded level prefab; cleared when it is released.
        internal RectTransform boardBackdrop;
        internal RectTransform boardRoot;
        internal RectTransform trayRoot;
        internal RectTransform trayViewport;
        internal RectTransform trayContent;
        internal RectTransform pieceLayer;
        internal Image trayImage;
        internal HorizontalLayoutGroup trayLayout;
        internal ScrollRect trayScrollRect;

        internal void BindLevelView(CatPuzzleLevelView view)
        {
            boardRoot = view.board; boardBackdrop = view.boardFrame;
            trayRoot = view.tray; trayImage = view.trayImage;
            trayViewport = view.viewport; trayContent = view.content;
            trayLayout = view.layout; trayScrollRect = view.scroll; pieceLayer = view.pieceLayer;
        }

        internal void ClearLevelView()
        {
            boardRoot = null; boardBackdrop = null; trayRoot = null; trayImage = null;
            trayViewport = null; trayContent = null; trayLayout = null; trayScrollRect = null; pieceLayer = null;
            boardCells.Clear(); boardRevealCells.Clear(); pieces.Clear(); previewCells.Clear();
        }

        internal void ResetForLevelLoad()
        {
            inputLocked = true;
            levelFailed = false;
            comboCount = 0;
            bestCombo = 0;
            earnedStars = 0;
            drag = null;
            previewCells.Clear();
            occupancy.Clear();
            pieces.Clear();
        }

        internal bool IsDragging(PieceState state) => drag != null && drag.Piece == state;

        internal bool IsBoardFilled => activeLevel != null && occupancy.Count == activeLevel.ActiveCells.Count;

        internal bool AllPiecesPlaced
        {
            get
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (!pieces[i].Placed) return false;
                }
                return true;
            }
        }
    }
}
