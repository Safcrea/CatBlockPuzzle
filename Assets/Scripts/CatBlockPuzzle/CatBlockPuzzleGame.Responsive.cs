using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private Vector2 lastUsableSize;

        private void UpdateResponsiveLayout()
        {
            if (root == null || activeLevel == null || root.rect.size == lastUsableSize) return;
            lastUsableSize = root.rect.size;
            if (drag != null) CancelActiveDragToRest();
            ConfigureTrayForPieceCount(VisibleTrayCardCount());
            Vector2 target = GetBoardTargetMaxSize();
            float aspect = (float)activeLevel.Cols / Mathf.Max(1, activeLevel.Rows);
            boardWidth = Mathf.Min(target.x, target.y * aspect);
            boardHeight = boardWidth / aspect;
            boardCellWidth = (boardWidth - BoardGap * (activeLevel.Cols - 1)) / activeLevel.Cols;
            boardCellHeight = (boardHeight - BoardGap * (activeLevel.Rows - 1)) / activeLevel.Rows;
            ApplyGameplayLayout();
            foreach (var entry in boardCells)
                SetTopLeft(entry.Value.Rect, CellPosition(entry.Key.x, entry.Key.y), new Vector2(boardCellWidth, boardCellHeight));
            foreach (PieceState piece in pieces)
            {
                if (!piece.Placed) continue;
                SetPieceGrid(piece, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
                piece.Rect.anchoredPosition = BoardPieceCenter(piece.Row, piece.Col, piece);
            }
            RefreshTrayLayout(false);
        }
    }
}
