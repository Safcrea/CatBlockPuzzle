using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleTuning;

namespace CatBlockPuzzle
{
    /// <summary>Lets onboarding restrict which piece may be picked up and where it may land.</summary>
    internal interface IPlacementGate
    {
        bool TutorialAllowsPickup(PieceState piece);
        bool TutorialAllowsShelfReturn(PieceState piece);
        bool TutorialAllowsPlacement(PieceState piece, int row, int col);
    }

    /// <summary>
    /// Picking a piece up, carrying it with the assisted touch anchor, resolving where it
    /// would snap, and dropping it: onto the board, back to the shelf, or bouncing back
    /// from an invalid spot. Raises <see cref="PiecePlaced"/> after a successful landing.
    /// </summary>
    internal sealed class PieceDragController : HostedService
    {
        private readonly PuzzleSession session;
        private readonly CanvasSpace space;
        private readonly BoardController board;
        private readonly TrayLayout tray;
        private readonly PieceLayout pieceLayout;
        private readonly PlacementFeedback feedback;
        private readonly HintPresenter hint;
        private readonly FxSpawner fx;
        private readonly CatMoodPresenter mood;
        private readonly ThemePresenter theme;
        private readonly PlayerSettings settings;
        private readonly InteractionGates gates;

        // Created at runtime on the full gameplay root. The serialized FX reference is also
        // used by authored power-up artwork, so it is not a safe parent for dragged pieces.
        private RectTransform dragOverlayLayer;

        /// <summary>Optional placement rules; null allows everything.</summary>
        public IPlacementGate Gate { get; set; }

        public event Action<PieceState> PiecePlaced;

        public PieceDragController(IGameplayContext context, PuzzleSession session, CanvasSpace space, BoardController board,
            TrayLayout tray, PieceLayout pieceLayout, PlacementFeedback feedback, HintPresenter hint, FxSpawner fx,
            CatMoodPresenter mood, ThemePresenter theme, PlayerSettings settings, InteractionGates gates) : base(context)
        {
            this.session = session;
            this.space = space;
            this.board = board;
            this.tray = tray;
            this.pieceLayout = pieceLayout;
            this.feedback = feedback;
            this.hint = hint;
            this.fx = fx;
            this.mood = mood;
            this.theme = theme;
            this.settings = settings;
            this.gates = gates;
        }

        private DragState drag { get => session.drag; set => session.drag = value; }
        private bool inputLocked { get => session.inputLocked; set => session.inputLocked = value; }
        private LevelDefinition activeLevel => session.activeLevel;
        private RectTransform boardRoot => session.boardRoot;
        private RectTransform trayRoot => session.trayRoot;
        private Image trayImage => session.trayImage;
        private RectTransform pieceLayer => session.pieceLayer;
        private RectTransform root => Hud.root;
        private PortraitLayoutProfile layoutProfile => Assets.layoutProfile;
        private bool reducedMotion => settings.ReducedMotion;
        private float boardWidth => session.boardWidth;
        private float boardHeight => session.boardHeight;
        private float boardCellWidth => session.boardCellWidth;
        private float boardCellHeight => session.boardCellHeight;
        private Vector2 ScreenCenterToRootLocal(Vector2 screen) => space.ScreenToRootLocal(screen);
        private Vector2 RootLocalToScreen(Vector2 rootLocal) => space.RootLocalToScreen(rootLocal);

        public bool BeginPieceDrag(PieceState state, PointerEventData eventData)
        {
            if (state == null || eventData == null || inputLocked || session.levelFailed || gates.ResultScreenOpen ||
                (Gate != null && !Gate.TutorialAllowsPickup(state)))
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

            tray.SetScrollEnabled(false);
            hint.StopHint();
            fx.ResetDragTrailTimer();
            mood.SetMood(state, CatMood.Neutral);
            if (state.SizeRoutine != null)
            {
                StopCoroutine(state.SizeRoutine);
                state.SizeRoutine = null;
            }
            if (state.SlotMoveRoutine != null)
            {
                StopCoroutine(state.SlotMoveRoutine);
                state.SlotMoveRoutine = null;
            }

            if (state.Placed)
            {
                tray.SetPieceSlotVisible(state, true, true);
                board.ClearOccupancyForPiece(state.Definition.Id);
            }

            if (state.SlotImage != null)
            {
                state.SlotImage.color = PuzzleStyle.CardDimColor;
            }

            Vector2 local = space.ScreenToLocal(state.Rect, eventData.position);
            Vector2 size = state.Rect.rect.size;
            float localX = local.x + (size.x * 0.5f);
            float localY = (size.y * 0.5f) - local.y;
            CellGrab grabbed = GetGrabbedCell(state, localX, localY);

            bool isTouch = eventData.pointerId >= 0;
            float canvasScale = space.ScaleFactor;
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

            MovePieceToOverlayLayer(state);
            Vector2 startScreen = space.WorldToScreen(state.Rect.position);
            Vector2 startRoot = ScreenCenterToRootLocal(startScreen);
            SetPieceCenterRoot(state, startRoot);
            state.Rect.localScale = Vector3.one;
            state.Rect.localEulerAngles = Vector3.zero;
            UpdateDragTarget(state);
            drag.SmoothVelocity = Vector2.zero;
            if (!drag.PreviousPlaced)
            {
                feedback.PlayPickup(state, startScreen);
            }

            return true;
        }

        public void CancelPieceInteraction(PieceState state)
        {
            if (drag == null || drag.Piece != state)
            {
                return;
            }

            DragState cancelled = drag;
            drag = null;
            tray.SetScrollEnabled(true);
            board.ClearPreview();
            if (trayImage != null)
            {
                trayImage.color = theme.ActiveTrayColor;
            }

            RestoreInvalidDrop(cancelled);
        }

        public void DragPiece(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state || eventData == null || drag.PointerId != eventData.pointerId)
            {
                return;
            }

            drag.LastPointerScreen = eventData.position;
            UpdateDragTarget(state);
            fx.SpawnDragTrail(drag.LastAnchorScreen, state.Color);
        }

        private void UpdateDragTarget(PieceState state)
        {
            if (drag == null || drag.Piece != state || state?.Rect == null)
            {
                return;
            }

            board.ClearPreview();
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

            bool overShelf = drag.PreviousPlaced && CanvasSpace.RectsOverlap(pieceScreenRect, space.ExpandedScreenRect(trayRoot, TrayReturnPadding));
            float snapPadding = layoutProfile != null ? layoutProfile.BoardSnapPadding : BoardSnapPadding;
            bool nearBoard = !overShelf && CanvasSpace.RectsOverlap(pieceScreenRect, space.ExpandedScreenRect(boardRoot, snapPadding));
            drag.ReturnToShelf = overShelf && (Gate == null || Gate.TutorialAllowsShelfReturn(state));
            if (trayImage != null)
            {
                trayImage.color = overShelf ? theme.ActiveTrayHoverColor : theme.ActiveTrayColor;
            }

            drag.Valid = false;
            drag.Row = -1;
            drag.Col = -1;

            if (nearBoard && TryResolveCatPlacement(state, target, out CatPlacementResult placement) &&
                (Gate == null || Gate.TutorialAllowsPlacement(state, placement.Row, placement.Col)))
            {
                drag.Valid = true;
                drag.Row = placement.Row;
                drag.Col = placement.Col;
                board.ShowPreview(state, placement.Row, placement.Col, true);
                return;
            }

            if (nearBoard)
            {
                GetBoardOriginFromGrab(anchorScreen, drag.Grabbed, out int row, out int col);
                board.ShowPreview(state, row, col, false);
            }
        }

        private Vector2 GetAssistedAnchorScreen(Vector2 pointerScreen)
        {
            if (drag == null)
            {
                return pointerScreen;
            }

            return AssistedAnchorScreen(pointerScreen, drag.PointerStartScreen, drag.IsTouch, drag.Lift);
        }

        /// <summary>The lifted, reach-extended anchor a pointer position maps to. Shared with the tutorial's hand path.</summary>
        public Vector2 AssistedAnchorScreen(Vector2 pointerScreen, Vector2 pointerStartScreen, bool isTouch, float lift)
        {
            float extraReach = 0f;
            if (isTouch)
            {
                // Reach grows in proportion to upward finger travel, so the cat keeps a steady
                // ratio to the finger instead of accelerating away from it.
                float upwardTravel = Mathf.Max(0f, pointerScreen.y - pointerStartScreen.y);
                float maximumGain = layoutProfile != null ? layoutProfile.MaximumVerticalGain : DragVerticalGain;
                float maximumExtra = layoutProfile != null ? layoutProfile.MaximumExtraReach : DragExtraReach;
                float canvasScale = space.ScaleFactor;
                extraReach = Mathf.Min(maximumExtra * canvasScale, upwardTravel * (maximumGain - 1f));
            }

            return pointerScreen + (Vector2.up * (lift + extraReach));
        }

        private bool TryResolveCatPlacement(PieceState state, Vector2 candidateCenter, out CatPlacementResult result)
        {
            bool[,] placementAvailability = session.placementAvailability;
            for (int row = 0; row < activeLevel.Rows; row++)
            {
                for (int col = 0; col < activeLevel.Cols; col++)
                {
                    Vector2Int coordinate = new Vector2Int(row, col);
                    placementAvailability[row, col] = activeLevel.ActiveCells.Contains(coordinate) && !session.occupancy.ContainsKey(coordinate);
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
                Vector2 screen = space.WorldToScreen(logicalWorldCenter);
                Vector2 local = space.ScreenToLocal(boardRoot, screen);
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

        public void EndPieceDrag(PieceState state, PointerEventData eventData)
        {
            if (drag == null || drag.Piece != state || eventData == null || drag.PointerId != eventData.pointerId)
            {
                return;
            }

            drag.LastPointerScreen = eventData.position;
            UpdateDragTarget(state);
            DragState endedDrag = drag;
            drag = null;
            tray.SetScrollEnabled(true);
            board.ClearPreview();
            if (trayImage != null)
            {
                trayImage.color = theme.ActiveTrayColor;
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

        public void PlacePiece(PieceState state, int row, int col, bool countForCombo)
        {
            state.Placed = true;
            state.Row = row;
            state.Col = col;
            board.OccupyCells(state, row, col);
            board.AttachPieceToBoard(state);
            StartCoroutine(fx.SquishLandTransform(state.Rect));
            feedback.PlaySnap(state, row, col);
            feedback.RegisterValidPlacement(state, countForCombo);
            PiecePlaced?.Invoke(state);
        }

        private void RestoreInvalidDrop(DragState endedDrag)
        {
            inputLocked = true;
            PieceState state = endedDrag.Piece;
            feedback.PlayWrong(state);
            if (endedDrag.PreviousPlaced)
            {
                state.Placed = true;
                state.Row = endedDrag.PreviousRow;
                state.Col = endedDrag.PreviousCol;
                board.OccupyCells(state, state.Row, state.Col);
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
            tray.SetPieceSlotVisible(state, true, true);
            state.Placed = false;
            state.Row = -1;
            state.Col = -1;
            feedback.PlayShelfReturn();
            StartCoroutine(AnimateToTray(state));
        }

        private IEnumerator AnimateToBoard(PieceState state, int row, int col)
        {
            Vector2 start = ScreenCenterToRootLocal(space.WorldToScreen(state.Rect.position));
            float startCellWidth = state.CellWidth;
            float startCellHeight = state.CellHeight;
            float startGapX = state.GapX;
            float startGapY = state.GapY;
            pieceLayout.SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
            Vector2 end = board.BoardPieceCenterInRoot(state, row, col);
            pieceLayout.SetPieceGrid(state, startCellWidth, startGapX, startCellHeight, startGapY);
            MovePieceToOverlayLayer(state);
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
            board.AttachPieceToBoard(state);
            inputLocked = false;
        }

        private IEnumerator AnimateToTray(PieceState state)
        {
            float trayCellSize = tray.TrayCellSize(state);
            Vector2 start = ScreenCenterToRootLocal(space.WorldToScreen(state.Rect.position));
            Vector2 end = tray.SlotCenterInRoot(state);
            MovePieceToOverlayLayer(state);
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
            tray.AttachPieceToTray(state);
            StartCoroutine(fx.PopTransform(state.Slot, 1.04f));
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
                pieceLayout.SetPieceGrid(state, targetCellWidth, targetGapX, targetCellHeight, targetGapY);
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
                pieceLayout.SetPieceGrid(
                    state,
                    Mathf.Lerp(startCellWidth, targetCellWidth, eased),
                    Mathf.Lerp(startGapX, targetGapX, eased),
                    Mathf.Lerp(startCellHeight, targetCellHeight, eased),
                    Mathf.Lerp(startGapY, targetGapY, eased));
                rect.anchoredPosition = Vector2.Lerp(start, end, eased);
                rect.localScale = Vector3.one * (1f + (Mathf.Sin(t * Mathf.PI) * overshoot));
                yield return null;
            }

            pieceLayout.SetPieceGrid(state, targetCellWidth, targetGapX, targetCellHeight, targetGapY);
            rect.anchoredPosition = end;
            rect.localScale = Vector3.one;
        }

        /// <summary>Per-frame follow, tilt and jelly for the held piece.</summary>
        public void UpdateMotion()
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
            pieceLayout.SetPieceGrid(
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

        public CellGrab GetGrabbedCell(PieceState state, float localX, float localY)
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
            Vector2 local = space.ScreenToLocal(boardRoot, anchorScreen);
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

        private void SetPieceCenterRoot(PieceState state, Vector2 rootLocal)
        {
            MovePieceToOverlayLayer(state);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            state.Rect.anchoredPosition = rootLocal;
        }

        private void SetDraggedPieceTarget(PieceState state, Vector2 rootLocal)
        {
            MovePieceToOverlayLayer(state);
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

        // Dragged pieces need a full-screen layer above the authored HUD. Do not use fxLayer
        // here: some scenes bind that field to a small button-local effect transform.
        private void MovePieceToOverlayLayer(PieceState state)
        {
            if (state?.Rect == null) return;
            RectTransform overlayLayer = EnsureDragOverlayLayer();
            if (overlayLayer == null) overlayLayer = pieceLayer;
            if (overlayLayer == null) return;
            // Preserve the tray-space world position for the pickup frame. The existing
            // SmoothDamp follow can then carry the piece to the pointer without a pop.
            if (state.Rect.parent != overlayLayer) state.Rect.SetParent(overlayLayer, true);
            state.Rect.SetAsLastSibling();
        }

        private RectTransform EnsureDragOverlayLayer()
        {
            if (dragOverlayLayer != null) return dragOverlayLayer;
            if (root == null) return null;

            var overlay = new GameObject("Dragged Piece Overlay", typeof(RectTransform), typeof(CanvasGroup));
            overlay.layer = root.gameObject.layer;
            dragOverlayLayer = overlay.GetComponent<RectTransform>();
            dragOverlayLayer.SetParent(root, false);
            dragOverlayLayer.anchorMin = Vector2.zero;
            dragOverlayLayer.anchorMax = Vector2.one;
            dragOverlayLayer.offsetMin = Vector2.zero;
            dragOverlayLayer.offsetMax = Vector2.zero;
            var canvasGroup = overlay.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            dragOverlayLayer.SetAsLastSibling();
            return dragOverlayLayer;
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

        /// <summary>Drops whatever is being held straight back to where it came from, with no animation or feedback.</summary>
        public void CancelActiveDragToRest()
        {
            if (drag == null)
            {
                return;
            }

            DragState cancelled = drag;
            drag = null;
            tray.SetScrollEnabled(true);
            board.ClearPreview();
            if (trayImage != null)
            {
                trayImage.color = theme.ActiveTrayColor;
            }

            PieceState state = cancelled.Piece;
            if (state == null)
            {
                return;
            }

            if (cancelled.PreviousPlaced)
            {
                state.Placed = true;
                state.Row = cancelled.PreviousRow;
                state.Col = cancelled.PreviousCol;
                board.OccupyCells(state, state.Row, state.Col);
                board.AttachPieceToBoard(state);
            }
            else
            {
                tray.AttachPieceToTray(state);
            }
        }
    }
}
