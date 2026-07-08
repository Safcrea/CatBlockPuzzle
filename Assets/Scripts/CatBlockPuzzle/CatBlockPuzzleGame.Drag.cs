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
        private void BeginPieceDrag(PieceState state, PointerEventData eventData)
        {
            if (inputLocked || winOverlay.gameObject.activeSelf)
            {
                return;
            }

            StopHint();
            if (state.Placed)
            {
                ClearOccupancyForPiece(state.Definition.Id);
            }

            state.SlotImage.color = CardDimColor;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(state.Rect, eventData.position, canvas.worldCamera, out Vector2 local);
            Vector2 size = state.Rect.rect.size;
            float localX = local.x + (size.x * 0.5f);
            float localY = (size.y * 0.5f) - local.y;
            CellGrab grabbed = GetGrabbedCell(state, localX, localY);

            drag = new DragState
            {
                Piece = state,
                PreviousPlaced = state.Placed,
                PreviousRow = state.Row,
                PreviousCol = state.Col,
                Grabbed = grabbed,
                SourceCellWidth = state.CellWidth,
                SourceCellHeight = state.CellHeight,
                SourceGapX = state.GapX,
                SourceGapY = state.GapY,
                Lift = eventData.pointerId >= 0 ? TouchVisualLift : MouseVisualLift
            };

            state.Rect.SetParent(pieceLayer, true);
            state.Rect.SetAsLastSibling();
            Vector2 startRoot = ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position));
            SetPieceCenterRoot(state, startRoot);
            SetPieceGrid(state, drag.SourceCellWidth, drag.SourceGapX, drag.SourceCellHeight, drag.SourceGapY);
            SetDraggedPieceTarget(state, PieceFreeCenterRoot(eventData.position + (Vector2.up * drag.Lift), state, drag.Grabbed, state.CellWidth, state.CellHeight, state.GapX, state.GapY));
            drag.SmoothVelocity = Vector2.zero;
            state.Rect.localScale = Vector3.one * 1.07f;
        }

        private void DragPiece(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state)
            {
                return;
            }

            ClearPreview();
            Vector2 pointerScreen = eventData.position;
            Vector2 anchorScreen = pointerScreen + (Vector2.up * drag.Lift);
            Rect pieceScreenRect = PieceScreenRectFromAnchor(anchorScreen, state, drag.Grabbed, state.CellWidth, state.CellHeight, state.GapX, state.GapY);
            bool nearBoard = RectsOverlap(pieceScreenRect, ExpandedScreenRect(boardRoot, BoardSnapPadding));
            bool overShelf = drag.PreviousPlaced && RectsOverlap(pieceScreenRect, ExpandedScreenRect(trayRoot, TrayReturnPadding));
            drag.ReturnToShelf = overShelf;
            trayImage.color = overShelf ? TrayHoverColor : TrayColor;

            drag.Valid = false;
            drag.Row = -1;
            drag.Col = -1;

            if (nearBoard && !overShelf)
            {
                SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
                GetBoardOriginFromGrab(anchorScreen, drag.Grabbed, out int row, out int col);
                bool valid = CanPlace(state, row, col);
                drag.Valid = valid;
                drag.Row = row;
                drag.Col = col;
                ShowPreview(state, row, col, valid);
                if (valid)
                {
                    SetDraggedPieceTarget(state, BoardPieceCenterInRoot(state, row, col));
                }
                else
                {
                    SetDraggedPieceTarget(state, PieceFreeCenterRoot(anchorScreen, state, drag.Grabbed, boardCellWidth, boardCellHeight, BoardGap, BoardGap));
                }
            }
            else
            {
                SetPieceGrid(state, drag.SourceCellWidth, drag.SourceGapX, drag.SourceCellHeight, drag.SourceGapY);
                SetDraggedPieceTarget(state, PieceFreeCenterRoot(anchorScreen, state, drag.Grabbed, state.CellWidth, state.CellHeight, state.GapX, state.GapY));
            }

            SpawnDragTrail(anchorScreen, state.Color);
        }

        private void EndPieceDrag(PieceState state)
        {
            if (drag == null || drag.Piece != state)
            {
                return;
            }

            DragState endedDrag = drag;
            drag = null;
            ClearPreview();
            trayImage.color = TrayColor;

            if (endedDrag.ReturnToShelf)
            {
                ReturnPieceToShelf(state);
                return;
            }

            if (endedDrag.Valid)
            {
                PlacePiece(state, endedDrag.Row, endedDrag.Col);
                return;
            }

            RestoreInvalidDrop(endedDrag);
        }

        private void PlacePiece(PieceState state, int row, int col)
        {
            state.Placed = true;
            state.Row = row;
            state.Col = col;
            OccupyCells(state, row, col);
            AttachPieceToBoard(state);
            StartCoroutine(PopTransform(state.Rect, 1.08f));
            PlaySnapFeedback(state, row, col);
            CheckWin();
        }

        private void RestoreInvalidDrop(DragState endedDrag)
        {
            inputLocked = true;
            PieceState state = endedDrag.Piece;
            PlayWrongFeedback(state);
            if (endedDrag.PreviousPlaced)
            {
                state.Placed = true;
                state.Row = endedDrag.PreviousRow;
                state.Col = endedDrag.PreviousCol;
                OccupyCells(state, state.Row, state.Col);
                StartCoroutine(AnimateToBoard(state, state.Row, state.Col));
            }
            else
            {
                state.Placed = false;
                state.Row = -1;
                state.Col = -1;
                StartCoroutine(AnimateToTray(state));
            }
        }

        private void ReturnPieceToShelf(PieceState state)
        {
            inputLocked = true;
            state.Placed = false;
            state.Row = -1;
            state.Col = -1;
            PlayShelfReturnFeedback();
            StartCoroutine(AnimateToTray(state));
        }

        private IEnumerator AnimateToBoard(PieceState state, int row, int col)
        {
            SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
            Vector2 start = ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position));
            Vector2 end = BoardPieceCenterInRoot(state, row, col);
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            yield return AnimateAnchored(state.Rect, start, end, InvalidReturnSeconds);
            AttachPieceToBoard(state);
            inputLocked = false;
        }

        private IEnumerator AnimateToTray(PieceState state)
        {
            SetPieceGrid(state, TrayCellSize(state), TrayGap, TrayCellSize(state), TrayGap);
            Vector2 start = ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position));
            Vector2 end = SlotCenterInRoot(state);
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            yield return AnimateAnchored(state.Rect, start, end, InvalidReturnSeconds);
            AttachPieceToTray(state);
            StartCoroutine(PopTransform(state.Slot, 1.04f));
            inputLocked = false;
        }

        private IEnumerator AnimateAnchored(RectTransform rect, Vector2 start, Vector2 end, float seconds)
        {
            float elapsed = 0f;
            rect.anchoredPosition = start;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                rect.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(t));
                yield return null;
            }

            rect.anchoredPosition = end;
        }

        private void UpdateDraggedPieceMotion()
        {
            if (drag == null || drag.Piece == null || drag.Piece.Rect == null)
            {
                return;
            }

            RectTransform rect = drag.Piece.Rect;
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            rect.anchoredPosition = Vector2.SmoothDamp(
                rect.anchoredPosition,
                drag.TargetPosition,
                ref drag.SmoothVelocity,
                DragMoveDelay,
                DragMoveSpeed,
                deltaTime);

            float targetZ = Mathf.Clamp(
                -(drag.SmoothVelocity.x / DragTiltVelocityScale) * DragTiltAmount,
                -DragTiltAmount,
                DragTiltAmount);

            rect.localRotation = Quaternion.Lerp(
                rect.localRotation,
                Quaternion.Euler(0f, 0f, targetZ),
                DragTiltSpeed * deltaTime);
        }

        private CellGrab GetGrabbedCell(PieceState state, float localX, float localY)
        {
            int localCol = Mathf.RoundToInt((localX - (state.CellWidth * 0.5f)) / Mathf.Max(1f, state.CellWidth + state.GapX));
            int localRow = Mathf.RoundToInt((localY - (state.CellHeight * 0.5f)) / Mathf.Max(1f, state.CellHeight + state.GapY));
            CellOffset nearest = state.Definition.Cells[0];
            float bestDistance = float.MaxValue;
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                float rowDistance = cell.Row - localRow;
                float colDistance = cell.Col - localCol;
                float distance = (rowDistance * rowDistance) + (colDistance * colDistance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = cell;
                }
            }

            float offsetX = Mathf.Clamp01((localX - (nearest.Col * (state.CellWidth + state.GapX))) / Mathf.Max(1f, state.CellWidth));
            float offsetY = Mathf.Clamp01((localY - (nearest.Row * (state.CellHeight + state.GapY))) / Mathf.Max(1f, state.CellHeight));
            return new CellGrab(nearest.Row, nearest.Col, offsetX, offsetY);
        }

        private void GetBoardOriginFromGrab(Vector2 anchorScreen, CellGrab grab, out int row, out int col)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, anchorScreen, canvas.worldCamera, out Vector2 local);
            float boardX = local.x + (boardWidth * 0.5f);
            float boardY = (boardHeight * 0.5f) - local.y;
            float grabbedLeft = boardX - (grab.OffsetX * boardCellWidth);
            float grabbedTop = boardY - (grab.OffsetY * boardCellHeight);
            int targetCol = Mathf.RoundToInt(grabbedLeft / Mathf.Max(1f, boardCellWidth + BoardGap));
            int targetRow = Mathf.RoundToInt(grabbedTop / Mathf.Max(1f, boardCellHeight + BoardGap));
            row = targetRow - grab.Row;
            col = targetCol - grab.Col;
        }

        private void MovePieceToBoardOriginScreen(PieceState state, int row, int col)
        {
            SetPieceCenterRoot(state, BoardPieceCenterInRoot(state, row, col));
        }

        private void MovePieceFreely(Vector2 anchorScreen, PieceState state, CellGrab grab, float cellWidth, float cellHeight, float gapX, float gapY)
        {
            SetPieceCenterRoot(state, PieceFreeCenterRoot(anchorScreen, state, grab, cellWidth, cellHeight, gapX, gapY));
        }

        private Vector2 PieceFreeCenterRoot(Vector2 anchorScreen, PieceState state, CellGrab grab, float cellWidth, float cellHeight, float gapX, float gapY)
        {
            Vector2 anchorRoot = ScreenCenterToRootLocal(anchorScreen);
            float pieceWidth = state.Rect.rect.width;
            float pieceHeight = state.Rect.rect.height;
            float centerX = anchorRoot.x - (grab.Col * (cellWidth + gapX)) - (grab.OffsetX * cellWidth) + (pieceWidth * 0.5f);
            float centerY = anchorRoot.y + (grab.Row * (cellHeight + gapY)) + (grab.OffsetY * cellHeight) - (pieceHeight * 0.5f);
            return new Vector2(centerX, centerY);
        }

        private void SetPieceCenterScreen(PieceState state, Vector2 screen)
        {
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            state.Rect.anchoredPosition = ScreenCenterToRootLocal(screen);
        }

        private void SetPieceCenterRoot(PieceState state, Vector2 rootLocal)
        {
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            state.Rect.anchoredPosition = rootLocal;
        }

        private void SetDraggedPieceTarget(PieceState state, Vector2 rootLocal)
        {
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);

            if (drag != null && drag.Piece == state)
            {
                drag.TargetPosition = rootLocal;
            }
            else
            {
                state.Rect.anchoredPosition = rootLocal;
            }
        }

        private Rect PieceScreenRectFromAnchor(Vector2 anchorScreen, PieceState state, CellGrab grab, float cellWidth, float cellHeight, float gapX, float gapY)
        {
            Vector2 anchorRoot = ScreenCenterToRootLocal(anchorScreen);
            float width = state.Rect.rect.width;
            float height = state.Rect.rect.height;
            float left = anchorRoot.x - (grab.Col * (cellWidth + gapX)) - (grab.OffsetX * cellWidth);
            float top = anchorRoot.y + (grab.Row * (cellHeight + gapY)) + (grab.OffsetY * cellHeight);
            Vector2 bottomLeft = RootLocalToScreen(new Vector2(left, top - height));
            Vector2 topRight = RootLocalToScreen(new Vector2(left + width, top));
            return Rect.MinMaxRect(
                Mathf.Min(bottomLeft.x, topRight.x),
                Mathf.Min(bottomLeft.y, topRight.y),
                Mathf.Max(bottomLeft.x, topRight.x),
                Mathf.Max(bottomLeft.y, topRight.y));
        }

        private float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            float inverse = 1f - value;
            return 1f - (inverse * inverse * inverse);
        }
    }
}
