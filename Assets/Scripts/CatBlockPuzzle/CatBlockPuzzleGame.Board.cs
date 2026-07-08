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
        private void BuildBoard()
        {
            boardCells.Clear();
            boardRevealCells.Clear();

            Vector2 targetMaxSize = GetBoardTargetMaxSize();
            float levelAspect = (float)activeLevel.Cols / Mathf.Max(1, activeLevel.Rows);
            if (levelAspect >= targetMaxSize.x / targetMaxSize.y)
            {
                boardWidth = targetMaxSize.x;
                boardHeight = targetMaxSize.x / levelAspect;
            }
            else
            {
                boardHeight = targetMaxSize.y;
                boardWidth = targetMaxSize.y * levelAspect;
            }

            ApplyGameplayLayout();

            boardCellWidth = (boardWidth - (BoardGap * (activeLevel.Cols - 1))) / activeLevel.Cols;
            boardCellHeight = (boardHeight - (BoardGap * (activeLevel.Rows - 1))) / activeLevel.Rows;

            for (int row = 0; row < activeLevel.Rows; row++)
            {
                for (int col = 0; col < activeLevel.Cols; col++)
                {
                    Vector2Int coord = new Vector2Int(row, col);
                    bool active = activeLevel.ActiveCells.Contains(coord);
                    Image cellImage = CreateImage(boardRoot, "Cell " + row + "," + col, active ? TargetColor : Color.clear);
                    if (active)
                    {
                        UseRoundedSprite(cellImage);
                        AddSoftShadow(cellImage, new Vector2(0f, -5f), 0.1f);
                        AddSoftOutline(cellImage, BoardTileEdgeColor, new Vector2(2f, -2f));
                    }

                    cellImage.raycastTarget = false;
                    RectTransform rect = cellImage.rectTransform;
                    SetTopLeft(rect, CellPosition(row, col), new Vector2(boardCellWidth, boardCellHeight));
                    boardCells[coord] = new CellView(rect, cellImage, cellImage.color);
                    if (active)
                    {
                        rect.localScale = Vector3.zero;
                        boardRevealCells.Add(new BoardRevealCell(rect, row + col));

                        Image shine = CreateImage(rect, "Cell Shine", new Color(1f, 1f, 1f, 0.28f));
                        UseRoundedSprite(shine);
                        shine.raycastTarget = false;
                        SetRect(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-boardCellWidth * 0.08f, boardCellHeight * 0.08f), new Vector2(boardCellWidth * 0.72f, boardCellHeight * 0.72f));

                        Image paw = CreateImage(rect, "Paw Print", new Color(241f / 255f, 241f / 255f, 244f / 255f, 0.58f));
                        paw.sprite = pawSprite;
                        paw.raycastTarget = false;
                        SetRect(paw.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(boardCellWidth * 0.48f, boardCellHeight * 0.48f));
                    }
                }
            }
        }

        private Vector2 GetBoardTargetMaxSize()
        {
            int maxDimension = Mathf.Max(activeLevel.Rows, activeLevel.Cols);
            if (maxDimension <= 3)
            {
                return new Vector2(520f, 520f);
            }

            if (maxDimension <= 4)
            {
                return new Vector2(620f, 620f);
            }

            if (maxDimension <= 5)
            {
                return new Vector2(690f, 690f);
            }

            return new Vector2(MaxBoardWidth, MaxBoardHeight);
        }

        private void ApplyGameplayLayout()
        {
            SetRect(boardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, BoardCenterY), new Vector2(boardWidth, boardHeight));
            if (boardBackdrop != null)
            {
                SetRect(boardBackdrop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, BoardCenterY), new Vector2(boardWidth + 74f, boardHeight + 74f));
            }

            if (timerText != null)
            {
                float timerY = BoardCenterY + (boardHeight * 0.5f) + TimerBoardGap + (TimerHeight * 0.5f);
                SetRect(timerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, timerY), new Vector2(320f, TimerHeight));
            }
        }

        // Diagonal "pop-in" reveal: every cell starts at scale 0 and springs to full
        // size, staggered by its (row+col) so the fill sweeps across the board. Runs
        // the grid-creation gate only after the whole sweep finishes, so gameplay starts
        // on a fully-shown board. Scaling localScale is safe under the current layout.
        private IEnumerator PlayLevelStartReveal()
        {
            if (boardRevealCells.Count == 0)
            {
                inputLocked = false;
                StartLevelTimer();
                boardRevealRoutine = null;
                yield break;
            }

            int maxDiagonal = 0;
            for (int i = 0; i < boardRevealCells.Count; i++)
            {
                maxDiagonal = Mathf.Max(maxDiagonal, boardRevealCells[i].Diagonal);
            }

            for (int diagonal = 0; diagonal <= maxDiagonal; diagonal++)
            {
                for (int i = 0; i < boardRevealCells.Count; i++)
                {
                    BoardRevealCell cell = boardRevealCells[i];
                    if (cell.Diagonal == diagonal && cell.Rect != null)
                    {
                        StartCoroutine(SpringBoardCell(cell.Rect));
                    }
                }

                yield return new WaitForSecondsRealtime(BoardRevealStaggerSeconds);
            }

            yield return new WaitForSecondsRealtime(BoardRevealCellSeconds);
            for (int i = 0; i < boardRevealCells.Count; i++)
            {
                if (boardRevealCells[i].Rect != null)
                {
                    boardRevealCells[i].Rect.localScale = Vector3.one;
                }
            }

            inputLocked = false;
            StartLevelTimer();
            boardRevealRoutine = null;
        }

        private IEnumerator SpringBoardCell(RectTransform rect)
        {
            float elapsed = 0f;
            while (elapsed < BoardRevealCellSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / BoardRevealCellSeconds);
                float scale = t < 0.72f
                    ? Mathf.Lerp(0f, BoardRevealOvershoot, EaseOutCubic(t / 0.72f))
                    : Mathf.Lerp(BoardRevealOvershoot, 1f, EaseOutCubic((t - 0.72f) / 0.28f));
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        private bool CanPlace(PieceState state, int row, int col)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                Vector2Int coord = new Vector2Int(row + cell.Row, col + cell.Col);
                if (coord.x < 0 || coord.x >= activeLevel.Rows || coord.y < 0 || coord.y >= activeLevel.Cols)
                {
                    return false;
                }

                if (!activeLevel.ActiveCells.Contains(coord) || occupancy.ContainsKey(coord))
                {
                    return false;
                }
            }

            return true;
        }

        private void ShowPreview(PieceState state, int row, int col, bool valid)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                Vector2Int coord = new Vector2Int(row + cell.Row, col + cell.Col);
                if (boardCells.TryGetValue(coord, out CellView cellView) && activeLevel.ActiveCells.Contains(coord))
                {
                    cellView.Image.color = valid ? ValidColor : InvalidColor;
                    cellView.Rect.localScale = Vector3.one * (valid ? 1.045f : 1.03f);
                    previewCells.Add(cellView.Image);
                }
            }
        }

        private void ClearPreview()
        {
            for (int i = 0; i < previewCells.Count; i++)
            {
                if (previewCells[i] != null)
                {
                    previewCells[i].color = TargetColor;
                    previewCells[i].rectTransform.localScale = Vector3.one;
                }
            }

            previewCells.Clear();
        }

        private void OccupyCells(PieceState state, int row, int col)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                occupancy[new Vector2Int(row + cell.Row, col + cell.Col)] = state.Definition.Id;
            }
        }

        private void ClearOccupancyForPiece(string pieceId)
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, string> entry in occupancy)
            {
                if (entry.Value == pieceId)
                {
                    toRemove.Add(entry.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                occupancy.Remove(toRemove[i]);
            }
        }

        private Vector2 CellPosition(int row, int col)
        {
            return new Vector2((col * (boardCellWidth + BoardGap)) + (boardCellWidth * 0.5f), -((row * (boardCellHeight + BoardGap)) + (boardCellHeight * 0.5f)));
        }

        private Vector2 BoardPieceCenter(int row, int col, PieceState state)
        {
            float width = state.Rect.rect.width;
            float height = state.Rect.rect.height;
            return new Vector2((col * (boardCellWidth + BoardGap)) + (width * 0.5f), -((row * (boardCellHeight + BoardGap)) + (height * 0.5f)));
        }

        private Vector2 BoardPieceCenterInBoard(PieceState state, int row, int col)
        {
            return BoardAnchoredToLocal(BoardPieceCenter(row, col, state));
        }

        private Vector2 BoardPieceCenterInRoot(PieceState state, int row, int col)
        {
            Vector3 world = boardRoot.TransformPoint(BoardPieceCenterInBoard(state, row, col));
            return ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, world));
        }

        private Vector2 BoardPieceCenterScreen(PieceState state, int row, int col)
        {
            Vector3 world = boardRoot.TransformPoint(BoardPieceCenterInBoard(state, row, col));
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, world);
        }

        private Vector2 BoardAnchoredToLocal(Vector2 topLeftAnchored)
        {
            return new Vector2(topLeftAnchored.x - (boardWidth * 0.5f), topLeftAnchored.y + (boardHeight * 0.5f));
        }

        private Vector2 BoardCenterScreen()
        {
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, boardRoot.position);
        }

        private Vector2 SlotCenterInRoot(PieceState state)
        {
            return ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Slot.position));
        }

        private Vector2 ScreenCenterToRootLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, canvas.worldCamera, out Vector2 local);
            return local;
        }

        private Vector2 RootLocalToScreen(Vector2 rootLocal)
        {
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, root.TransformPoint(rootLocal));
        }

        private Rect ExpandedScreenRect(RectTransform rect, float padding)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);
            return Rect.MinMaxRect(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
        }

        private bool RectsOverlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
        }

        private float TrayCellSize(PieceState state)
        {
            int rows = Mathf.Max(1, state.Definition.Rows);
            int cols = Mathf.Max(1, state.Definition.Cols);
            int maxCells = Mathf.Max(rows, cols);
            float horizontalInset = Mathf.Clamp(traySlotPreferredWidth * 0.16f, 14f, 30f);
            float verticalInset = Mathf.Clamp(traySlotPreferredHeight * 0.22f, 42f, 58f);
            float widthFit = (traySlotPreferredWidth - horizontalInset - ((cols - 1) * TrayGap)) / cols;
            float heightFit = (traySlotPreferredHeight - verticalInset - ((rows - 1) * TrayGap)) / rows;
            float shapeCap = maxCells >= 5 ? 34f : maxCells >= 4 ? 39f : trayCellMaxSize;
            float cellSize = Mathf.Min(trayCellMaxSize, shapeCap, widthFit, heightFit);
            return Mathf.Clamp(cellSize, 20f, 46f);
        }
    }
}
