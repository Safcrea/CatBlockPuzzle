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
            SetCatMood(state, CatMood.Neutral);
            if (state.SizeRoutine != null)
            {
                StopCoroutine(state.SizeRoutine);
                state.SizeRoutine = null;
            }

            if (state.Placed)
            {
                SetPieceSlotVisible(state, true, true);
                ClearOccupancyForPiece(state.Definition.Id);
            }

            if (state.SlotImage != null)
            {
                state.SlotImage.color = CardDimColor;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(state.Rect, eventData.position, canvas.worldCamera, out Vector2 local);
            Vector2 size = state.Rect.rect.size;
            float localX = local.x + (size.x * 0.5f);
            float localY = (size.y * 0.5f) - local.y;
            CellGrab grabbed = GetGrabbedCell(state, localX, localY);

            bool isTouch = eventData.pointerId >= 0;
            float canvasScale = canvas != null ? Mathf.Max(0.25f, canvas.scaleFactor) : 1f;
            float configuredLift = isTouch
                ? (layoutProfile != null ? layoutProfile.TouchVisualLift : TouchVisualLift)
                : (layoutProfile != null ? layoutProfile.MouseVisualLift : MouseVisualLift);
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
                Lift = configuredLift * canvasScale,
                PointerId = eventData.pointerId,
                IsTouch = isTouch,
                PointerStartScreen = eventData.position,
                LastPointerScreen = eventData.position
            };

            state.Rect.SetParent(pieceLayer, true);
            state.Rect.SetAsLastSibling();
            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position);
            Vector2 startRoot = ScreenCenterToRootLocal(startScreen);
            SetPieceCenterRoot(state, startRoot);
            state.Rect.localScale = Vector3.one;
            state.Rect.localEulerAngles = Vector3.zero;
            UpdateDragTarget(state);
            drag.SmoothVelocity = Vector2.zero;
            if (!drag.PreviousPlaced)
            {
                PlayPickupFeedback(state, startScreen);
            }

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
                trayImage.color = activeTrayColor;
            }

            RestoreInvalidDrop(cancelled);
        }

        private void DragPiece(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state || eventData == null || drag.PointerId != eventData.pointerId)
            {
                return;
            }

            drag.LastPointerScreen = eventData.position;
            UpdateDragTarget(state);
            SpawnDragTrail(drag.LastAnchorScreen, state.Color);
        }

        private void UpdateDragTarget(PieceState state)
        {
            if (drag == null || drag.Piece != state || state?.Rect == null)
            {
                return;
            }

            ClearPreview();
            Vector2 anchorScreen = GetAssistedAnchorScreen(drag.LastPointerScreen);
            drag.LastAnchorScreen = anchorScreen;
            Vector2 target = PieceFreeCenterRoot(
                anchorScreen,
                state,
                drag.Grabbed,
                state.CellWidth,
                state.CellHeight,
                state.GapX,
                state.GapY);
            SetDraggedPieceTarget(state, target);

            Rect pieceScreenRect = PieceScreenRectFromAnchor(
                anchorScreen,
                state,
                drag.Grabbed,
                state.CellWidth,
                state.CellHeight,
                state.GapX,
                state.GapY);

            bool overShelf = drag.PreviousPlaced && RectsOverlap(pieceScreenRect, ExpandedScreenRect(trayRoot, TrayReturnPadding));
            float snapPadding = layoutProfile != null ? layoutProfile.BoardSnapPadding : BoardSnapPadding;
            bool nearBoard = !overShelf && RectsOverlap(pieceScreenRect, ExpandedScreenRect(boardRoot, snapPadding));
            drag.ReturnToShelf = overShelf;
            if (trayImage != null)
            {
                trayImage.color = overShelf ? activeTrayHoverColor : activeTrayColor;
            }

            drag.Valid = false;
            drag.Row = -1;
            drag.Col = -1;

            if (nearBoard && TryResolveCatPlacement(state, target, out CatPlacementResult placement))
            {
                drag.Valid = true;
                drag.Row = placement.Row;
                drag.Col = placement.Col;
                ShowPreview(state, placement.Row, placement.Col, true);
                return;
            }

            if (nearBoard)
            {
                GetBoardOriginFromGrab(anchorScreen, drag.Grabbed, out int row, out int col);
                ShowPreview(state, row, col, false);
            }
        }

        private Vector2 GetAssistedAnchorScreen(Vector2 pointerScreen)
        {
            if (drag == null)
            {
                return pointerScreen;
            }

            float extraReach = 0f;
            if (drag.IsTouch)
            {
                float upwardTravel = Mathf.Max(0f, pointerScreen.y - drag.PointerStartScreen.y);
                float rampDistance = Mathf.Max(1f, Screen.height * 0.35f);
                float ramp = Mathf.Clamp01(upwardTravel / rampDistance);
                float maximumGain = layoutProfile != null ? layoutProfile.MaximumVerticalGain : 1.35f;
                float maximumExtra = layoutProfile != null ? layoutProfile.MaximumExtraReach : 180f;
                float canvasScale = canvas != null ? Mathf.Max(0.25f, canvas.scaleFactor) : 1f;
                extraReach = Mathf.Min(maximumExtra * canvasScale, upwardTravel * (maximumGain - 1f) * ramp);
            }

            return pointerScreen + (Vector2.up * (drag.Lift + extraReach));
        }

        private bool TryResolveCatPlacement(PieceState state, Vector2 candidateCenter, out CatPlacementResult result)
        {
            for (int row = 0; row < activeLevel.Rows; row++)
            {
                for (int col = 0; col < activeLevel.Cols; col++)
                {
                    Vector2Int coordinate = new Vector2Int(row, col);
                    placementAvailability[row, col] = activeLevel.ActiveCells.Contains(coordinate) && !occupancy.ContainsKey(coordinate);
                }
            }

            float pitchX = Mathf.Max(1f, boardCellWidth + BoardGap);
            float pitchY = Mathf.Max(1f, boardCellHeight + BoardGap);
            float pieceWidth = state.Rect.rect.width;
            float pieceHeight = state.Rect.rect.height;
            Vector3 candidateLocalCenter = state.Rect.localPosition + new Vector3(
                candidateCenter.x - state.Rect.anchoredPosition.x,
                candidateCenter.y - state.Rect.anchoredPosition.y,
                0f);
            for (int i = 0; i < state.PlacementOffsets.Length; i++)
            {
                Vector2Int offset = state.PlacementOffsets[i];
                Vector3 logicalLocalCenter = candidateLocalCenter + new Vector3(
                    -(pieceWidth * 0.5f) + (offset.y * (state.CellWidth + state.GapX)) + (state.CellWidth * 0.5f),
                    (pieceHeight * 0.5f) - (offset.x * (state.CellHeight + state.GapY)) - (state.CellHeight * 0.5f),
                    0f);
                Vector3 logicalWorldCenter = state.Rect.parent.TransformPoint(logicalLocalCenter);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, logicalWorldCenter);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screen, canvas.worldCamera, out Vector2 local);
                float boardX = local.x + (boardWidth * 0.5f);
                float boardY = (boardHeight * 0.5f) - local.y;
                float row = (boardY - (boardCellHeight * 0.5f)) / pitchY;
                float col = (boardX - (boardCellWidth * 0.5f)) / pitchX;
                state.GridCenters[i] = new Vector2(row, col);
            }

            float threshold = layoutProfile != null ? layoutProfile.CatSnapThreshold : 0.45f;
            result = CatPlacementResolver.Resolve(state.GridCenters, state.PlacementOffsets, placementAvailability, threshold);
            return result.IsValid;
        }

        private void EndPieceDrag(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state || eventData == null || drag.PointerId != eventData.pointerId)
            {
                return;
            }

            drag.LastPointerScreen = eventData.position;
            UpdateDragTarget(state);
            DragState endedDrag = drag;
            drag = null;
            SetTrayScrollEnabled(true);
            ClearPreview();
            if (trayImage != null)
            {
                trayImage.color = activeTrayColor;
            }

            state.Rect.localScale = Vector3.one;
            state.Rect.localRotation = Quaternion.identity;

            if (endedDrag.ReturnToShelf)
            {
                ReturnPieceToShelf(state);
                return;
            }

            if (endedDrag.Valid)
            {
                PlacePiece(state, endedDrag.Row, endedDrag.Col, !endedDrag.PreviousPlaced);
                return;
            }

            RestoreInvalidDrop(endedDrag);
        }

        private void PlacePiece(PieceState state, int row, int col, bool countForCombo)
        {
            state.Placed = true;
            state.Row = row;
            state.Col = col;
            OccupyCells(state, row, col);
            AttachPieceToBoard(state);
            StartCoroutine(SquishLandTransform(state.Rect));
            PlaySnapFeedback(state, row, col);
            RegisterValidPlacement(state, countForCombo);
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
            SetPieceSlotVisible(state, true, true);
            state.Placed = false;
            state.Row = -1;
            state.Col = -1;
            PlayShelfReturnFeedback();
            StartCoroutine(AnimateToTray(state));
        }

        private IEnumerator AnimateToBoard(PieceState state, int row, int col)
        {
            Vector2 start = ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position));
            float startCellWidth = state.CellWidth;
            float startCellHeight = state.CellHeight;
            float startGapX = state.GapX;
            float startGapY = state.GapY;
            SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
            Vector2 end = BoardPieceCenterInRoot(state, row, col);
            SetPieceGrid(state, startCellWidth, startGapX, startCellHeight, startGapY);
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            yield return AnimatePieceTransition(
                state,
                start,
                end,
                boardCellWidth,
                boardCellHeight,
                BoardGap,
                BoardGap);
            AttachPieceToBoard(state);
            inputLocked = false;
        }

        private IEnumerator AnimateToTray(PieceState state)
        {
            float trayCellSize = TrayCellSize(state);
            Vector2 start = ScreenCenterToRootLocal(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position));
            Vector2 end = SlotCenterInRoot(state);
            state.Rect.SetParent(pieceLayer, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            yield return AnimatePieceTransition(
                state,
                start,
                end,
                trayCellSize,
                trayCellSize,
                TrayGap,
                TrayGap);
            AttachPieceToTray(state);
            StartCoroutine(PopTransform(state.Slot, 1.04f));
            inputLocked = false;
        }

        private IEnumerator AnimatePieceTransition(
            PieceState state,
            Vector2 start,
            Vector2 end,
            float targetCellWidth,
            float targetCellHeight,
            float targetGapX,
            float targetGapY)
        {
            RectTransform rect = state.Rect;
            float startCellWidth = state.CellWidth;
            float startCellHeight = state.CellHeight;
            float startGapX = state.GapX;
            float startGapY = state.GapY;
            float seconds = layoutProfile != null ? layoutProfile.PieceSizeTransitionSeconds : 0.2f;
            float overshoot = layoutProfile != null ? layoutProfile.DragScaleOvershoot : 0.05f;
            if (reducedMotion)
            {
                SetPieceGrid(state, targetCellWidth, targetGapX, targetCellHeight, targetGapY);
                rect.anchoredPosition = end;
                rect.localScale = Vector3.one;
                yield break;
            }

            float elapsed = 0f;
            rect.anchoredPosition = start;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float eased = EaseOutCubic(t);
                SetPieceGrid(
                    state,
                    Mathf.Lerp(startCellWidth, targetCellWidth, eased),
                    Mathf.Lerp(startGapX, targetGapX, eased),
                    Mathf.Lerp(startCellHeight, targetCellHeight, eased),
                    Mathf.Lerp(startGapY, targetGapY, eased));
                rect.anchoredPosition = Vector2.Lerp(start, end, eased);
                rect.localScale = Vector3.one * (1f + (Mathf.Sin(t * Mathf.PI) * overshoot));
                yield return null;
            }

            SetPieceGrid(state, targetCellWidth, targetGapX, targetCellHeight, targetGapY);
            rect.anchoredPosition = end;
            rect.localScale = Vector3.one;
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

            UpdateDraggedPieceSize(deltaTime);
            UpdateDragTarget(drag.Piece);

            float followDelay = layoutProfile != null ? layoutProfile.DragFollowDelay : DragFollowDelay;
            if (reducedMotion || followDelay <= 0.001f)
            {
                Vector2 previousPosition = rect.anchoredPosition;
                rect.anchoredPosition = drag.TargetPosition;
                drag.SmoothVelocity = (rect.anchoredPosition - previousPosition) / deltaTime;
            }
            else
            {
                rect.anchoredPosition = Vector2.SmoothDamp(
                    rect.anchoredPosition,
                    drag.TargetPosition,
                    ref drag.SmoothVelocity,
                    followDelay,
                    Mathf.Infinity,
                    deltaTime);
            }

            float tiltAmount = layoutProfile != null ? layoutProfile.DragTiltAmount : DragTiltAmount;
            float targetZ = Mathf.Clamp(
                -(drag.SmoothVelocity.x / DragTiltVelocityScale) * tiltAmount,
                -tiltAmount,
                tiltAmount);
            if (reducedMotion)
            {
                targetZ = 0f;
            }

            rect.localRotation = Quaternion.Lerp(
                rect.localRotation,
                Quaternion.Euler(0f, 0f, targetZ),
                1f - Mathf.Exp(-DragTiltSpeed * deltaTime));
            UpdateDraggedPieceJelly(rect, deltaTime);
        }

        private void UpdateDraggedPieceSize(float deltaTime)
        {
            if (drag == null || drag.Piece == null)
            {
                return;
            }

            PieceState state = drag.Piece;
            float duration = layoutProfile != null ? layoutProfile.PieceSizeTransitionSeconds : 0.2f;
            drag.ResizeElapsed = reducedMotion ? duration : Mathf.Min(duration, drag.ResizeElapsed + deltaTime);
            float t = duration <= 0f ? 1f : Mathf.Clamp01(drag.ResizeElapsed / duration);
            float eased = reducedMotion ? 1f : EaseOutCubic(t);
            SetPieceGrid(
                state,
                Mathf.Lerp(drag.SourceCellWidth, boardCellWidth, eased),
                Mathf.Lerp(drag.SourceGapX, BoardGap, eased),
                Mathf.Lerp(drag.SourceCellHeight, boardCellHeight, eased),
                Mathf.Lerp(drag.SourceGapY, BoardGap, eased));

            float overshoot = layoutProfile != null ? layoutProfile.DragScaleOvershoot : 0.05f;
            drag.PickupScale = reducedMotion
                ? 1f
                : 1f + (Mathf.Sin(t * Mathf.PI) * overshoot);
        }

        private void UpdateDraggedPieceJelly(RectTransform rect, float deltaTime)
        {
            if (drag == null || rect == null)
            {
                return;
            }

            if (reducedMotion)
            {
                drag.JellyScale = Vector2.one;
                rect.localScale = Vector3.one;
                return;
            }

            float speed = drag.SmoothVelocity.magnitude;
            Vector2 direction = speed > 0.01f ? drag.SmoothVelocity / speed : Vector2.zero;
            float jellyAmount = layoutProfile != null ? layoutProfile.DragJellyAmount : DragJellyAmount;
            float stretch = jellyAmount * Mathf.Clamp01(speed / DragJellyVelocityScale);
            Vector2 targetScale = new Vector2(
                1f + (stretch * (Mathf.Abs(direction.x) - (Mathf.Abs(direction.y) * 0.45f))),
                1f + (stretch * (Mathf.Abs(direction.y) - (Mathf.Abs(direction.x) * 0.45f))));
            float blend = 1f - Mathf.Exp(-DragJellySpeed * deltaTime);
            drag.JellyScale = Vector2.Lerp(drag.JellyScale, targetScale, blend);
            rect.localScale = new Vector3(
                drag.JellyScale.x * drag.PickupScale,
                drag.JellyScale.y * drag.PickupScale,
                1f);
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

        private float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            float inverse = 1f - value;
            return 1f - (inverse * inverse * inverse);
        }
    }
}
