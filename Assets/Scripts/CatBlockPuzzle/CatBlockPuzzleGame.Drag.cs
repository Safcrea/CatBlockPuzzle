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
        private bool BeginPieceDrag(PieceState state, PointerEventData eventData)
        {
            if (state == null || eventData == null || inputLocked || levelFailed || winOverlay.gameObject.activeSelf || failOverlay.gameObject.activeSelf)
            {
                return false;
            }

            if (drag != null)
            {
                if (drag.Piece == state && drag.PointerId == eventData.pointerId)
                {
                    DragPiece(state, eventData);
                    return true;
                }

                return false;
            }

            SetTrayScrollEnabled(false);
            StopHint();
            nextDragTrailTime = 0f;
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
                Lift = eventData.pointerId >= 0 ? TouchVisualLift : MouseVisualLift,
                PointerId = eventData.pointerId,
                BoardClampActive = state.Placed
            };

            state.Rect.SetParent(pieceLayer, true);
            state.Rect.SetAsLastSibling();
            Vector2 startRoot = ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position));
            SetPieceCenterRoot(state, startRoot);
            SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
            Vector2 target = PieceFreeCenterRoot(eventData.position + (Vector2.up * drag.Lift), state, drag.Grabbed, boardCellWidth, boardCellHeight, BoardGap, BoardGap);
            if (drag.BoardClampActive)
            {
                target = ClampPieceCenterInsideBoard(target, state);
            }

            SetDraggedPieceTarget(state, target);
            state.Rect.anchoredPosition = drag.TargetPosition;
            drag.SmoothVelocity = Vector2.zero;
            state.Rect.localScale = Vector3.one * 1.07f;
            return true;
        }

        private void SetTrayScrollEnabled(bool enabled)
        {
            if (trayScrollRect == null)
            {
                return;
            }

            if (!enabled)
            {
                trayScrollRect.StopMovement();
            }

            trayScrollRect.enabled = enabled;
        }

        private void CancelPieceInteraction(PieceState state)
        {
            if (drag == null || drag.Piece != state)
            {
                return;
            }

            DragState cancelled = drag;
            drag = null;
            SetTrayScrollEnabled(true);
            ClearPreview();
            if (trayImage != null)
            {
                trayImage.color = TrayColor;
            }

            RestoreInvalidDrop(cancelled);
        }

        private void DragPiece(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state || eventData == null || drag.PointerId != eventData.pointerId)
            {
                return;
            }

            ClearPreview();
            Vector2 pointerScreen = eventData.position;
            Vector2 anchorScreen = pointerScreen + (Vector2.up * drag.Lift);
            Rect pieceScreenRect = PieceScreenRectFromAnchor(anchorScreen, state, drag.Grabbed, state.CellWidth, state.CellHeight, state.GapX, state.GapY);
            bool nearBoard = RectsOverlap(pieceScreenRect, ExpandedScreenRect(boardRoot, BoardSnapPadding));
            if (nearBoard)
            {
                drag.BoardClampActive = true;
            }

            bool overShelf = drag.PreviousPlaced && !drag.BoardClampActive && RectsOverlap(pieceScreenRect, ExpandedScreenRect(trayRoot, TrayReturnPadding));
            drag.ReturnToShelf = overShelf;
            trayImage.color = overShelf ? TrayHoverColor : TrayColor;

            drag.Valid = false;
            drag.Row = -1;
            drag.Col = -1;

            SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);

            if (nearBoard && !overShelf)
            {
                GetBoardOriginFromGrab(anchorScreen, drag.Grabbed, out int row, out int col);
                bool valid = CanPlace(state, row, col);
                drag.Valid = valid;
                drag.Row = row;
                drag.Col = col;
                ShowPreview(state, row, col, valid);
                Vector2 target;
                if (valid)
                {
                    target = BoardPieceCenterInRoot(state, row, col);
                }
                else
                {
                    target = PieceFreeCenterRoot(anchorScreen, state, drag.Grabbed, boardCellWidth, boardCellHeight, BoardGap, BoardGap);
                }

                if (drag.BoardClampActive)
                {
                    target = ClampPieceCenterInsideBoard(target, state);
                }

                SetDraggedPieceTarget(state, target);
            }
            else
            {
                Vector2 target = PieceFreeCenterRoot(anchorScreen, state, drag.Grabbed, boardCellWidth, boardCellHeight, BoardGap, BoardGap);
                if (drag.BoardClampActive)
                {
                    target = ClampPieceCenterInsideBoard(target, state);
                }

                SetDraggedPieceTarget(state, target);
            }

            SpawnDragTrail(anchorScreen, state.Color);
        }

        private void EndPieceDrag(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state || eventData == null || drag.PointerId != eventData.pointerId)
            {
                return;
            }

            DragState endedDrag = drag;
            drag = null;
            SetTrayScrollEnabled(true);
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
            float trayCellSize = TrayCellSize(state);
            SetPieceGrid(state, trayCellSize, TrayGap, trayCellSize, TrayGap);
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

            Vector2 previousPosition = rect.anchoredPosition;
            rect.anchoredPosition = drag.TargetPosition;
            drag.SmoothVelocity = (rect.anchoredPosition - previousPosition) / deltaTime;

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

        private Vector2 ClampPieceCenterInsideBoard(Vector2 centerRoot, PieceState state)
        {
            Rect boardRect = RootLocalRect(boardRoot);
            float scaleX = Mathf.Abs(state.Rect.localScale.x);
            float scaleY = Mathf.Abs(state.Rect.localScale.y);
            float halfWidth = state.Rect.rect.width * 0.5f * Mathf.Max(1f, scaleX);
            float halfHeight = state.Rect.rect.height * 0.5f * Mathf.Max(1f, scaleY);
            float minX = boardRect.xMin + halfWidth;
            float maxX = boardRect.xMax - halfWidth;
            float minY = boardRect.yMin + halfHeight;
            float maxY = boardRect.yMax - halfHeight;
            float x = minX <= maxX ? Mathf.Clamp(centerRoot.x, minX, maxX) : boardRect.center.x;
            float y = minY <= maxY ? Mathf.Clamp(centerRoot.y, minY, maxY) : boardRect.center.y;
            return new Vector2(x, y);
        }

        private Rect RootLocalRect(RectTransform rect)
        {
            rect.GetWorldCorners(rectWorldCorners);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < rectWorldCorners.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(rectWorldCorners[i]);
                minX = Mathf.Min(minX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxX = Mathf.Max(maxX, local.x);
                maxY = Mathf.Max(maxY, local.y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            float inverse = 1f - value;
            return 1f - (inverse * inverse * inverse);
        }
    }
}
