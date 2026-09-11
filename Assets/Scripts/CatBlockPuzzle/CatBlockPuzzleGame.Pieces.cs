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


        private void ConfigureTrayForPieceCount(int visiblePieceCount)
        {
            int visibleCount = Mathf.Max(0, visiblePieceCount);
            int layoutCount = Mathf.Max(1, visibleCount);
            int totalCount = activeLevel != null ? Mathf.Max(1, activeLevel.Pieces.Length) : layoutCount;
            int hiddenCount = Mathf.Max(0, totalCount - visibleCount);
            int horizontalPadding = 24;
            int verticalPadding = 24;
            float densityScale = totalCount >= 7 ? 0.83f : totalCount >= 5 ? 0.9f : 1f;
            float spacing = totalCount >= 7 ? 14f : totalCount >= 5 ? 16f : 18f;
            float growthPerCard = layoutProfile != null ? layoutProfile.RemainingCardGrowth : 0.08f;
            float maximumGrowth = layoutProfile != null ? layoutProfile.MaximumCardGrowth : 0.16f;
            float growth = 1f + Mathf.Min(maximumGrowth, hiddenCount * growthPerCard);
            float baseWidth = layoutProfile != null ? layoutProfile.TrayCardWidth : 212f;
            float baseHeight = layoutProfile != null ? layoutProfile.TrayCardHeight : 246f;
            float baseMinWidth = layoutProfile != null ? layoutProfile.TrayCardMinWidth : 104f;
            float baseMinHeight = layoutProfile != null ? layoutProfile.TrayCardMinHeight : 154f;
            float baseCellSize = layoutProfile != null ? layoutProfile.TrayCatCellSize : 46f;

            traySlotPreferredWidth = baseWidth * densityScale * growth;
            traySlotPreferredHeight = baseHeight * densityScale;
            traySlotMinWidth = Mathf.Min(baseMinWidth * growth, traySlotPreferredWidth);
            traySlotMinHeight = Mathf.Min(baseMinHeight, traySlotPreferredHeight);
            trayCellMaxSize = Mathf.Min(64f, baseCellSize * densityScale * growth);

            float profileTrayHeight = layoutProfile != null ? layoutProfile.TrayHeight : 292f;
            float profileActionHeight = layoutProfile != null ? layoutProfile.ActionBarHeight : 96f;
            traySlotPreferredHeight = Mathf.Min(traySlotPreferredHeight, Mathf.Max(150f, profileTrayHeight - (verticalPadding * 2f)));
            float trayWidth = Mathf.Min(TrayMaxWidth, Mathf.Max(560f, root.rect.width - 64f));
            float trayHeight = Mathf.Clamp(traySlotPreferredHeight + (verticalPadding * 2f), 270f, profileTrayHeight);
            float viewportWidth = Mathf.Max(1f, trayWidth - 36f);
            float desiredContentWidth = (traySlotPreferredWidth * layoutCount) + (spacing * (layoutCount - 1)) + (horizontalPadding * 2f);

            if (trayLayout != null)
            {
                trayLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
                trayLayout.spacing = spacing;
            }

            float trayBottom = profileActionHeight + 40f;
            SetRect(trayRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, trayBottom), new Vector2(trayWidth, trayHeight));
            if (trayContent != null)
            {
                SetRect(trayContent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(Mathf.Max(viewportWidth, desiredContentWidth), 0f));
            }
        }

        private void ApplyTraySlotLayout(LayoutElement layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.preferredWidth = traySlotPreferredWidth;
            layout.minWidth = traySlotMinWidth;
            layout.preferredHeight = traySlotPreferredHeight;
            layout.minHeight = traySlotMinHeight;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private int VisibleTrayCardCount()
        {
            int count = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i]?.Slot != null && pieces[i].Slot.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private void SetPieceSlotVisible(PieceState state, bool visible, bool refreshLayout)
        {
            if (state?.Slot == null)
            {
                return;
            }

            if (state.Slot.gameObject.activeSelf != visible)
            {
                state.Slot.gameObject.SetActive(visible);
            }

            if (state.SlotImage != null)
            {
                state.SlotImage.color = visible ? CardRestColor : CardDimColor;
            }

            if (refreshLayout)
            {
                RefreshTrayLayout(!reducedMotion);
            }
        }

        private void RefreshTrayLayout(bool animateCatSizes)
        {
            if (trayContent == null || trayRoot == null)
            {
                return;
            }

            float previousScroll = trayScrollRect != null ? trayScrollRect.horizontalNormalizedPosition : 0f;
            ConfigureTrayForPieceCount(VisibleTrayCardCount());
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state?.Slot == null || !state.Slot.gameObject.activeSelf)
                {
                    continue;
                }

                ApplyTraySlotLayout(state.SlotLayout);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(trayContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(trayRoot);
            Canvas.ForceUpdateCanvases();
            if (trayScrollRect != null)
            {
                trayScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(previousScroll);
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state?.Rect == null || state.Placed || state.Slot == null || !state.Slot.gameObject.activeSelf ||
                    (drag != null && drag.Piece == state))
                {
                    continue;
                }

                float targetCellSize = TrayCellSize(state);
                if (state.SizeRoutine != null)
                {
                    StopCoroutine(state.SizeRoutine);
                    state.SizeRoutine = null;
                }

                if (animateCatSizes && gameObject.activeInHierarchy && !Mathf.Approximately(state.CellWidth, targetCellSize))
                {
                    state.SizeRoutine = StartCoroutine(AnimateTrayPieceSize(state, targetCellSize));
                }
                else
                {
                    SetPieceGrid(state, targetCellSize, TrayGap, targetCellSize, TrayGap);
                }
            }
        }

        private IEnumerator AnimateTrayPieceSize(PieceState state, float targetCellSize)
        {
            float startWidth = state.CellWidth;
            float startHeight = state.CellHeight;
            float startGapX = state.GapX;
            float startGapY = state.GapY;
            float duration = layoutProfile != null ? layoutProfile.PieceSizeTransitionSeconds : 0.2f;
            float elapsed = 0f;
            while (elapsed < duration && state != null && state.Rect != null && !state.Placed &&
                   (drag == null || drag.Piece != state))
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                SetPieceGrid(
                    state,
                    Mathf.Lerp(startWidth, targetCellSize, t),
                    Mathf.Lerp(startGapX, TrayGap, t),
                    Mathf.Lerp(startHeight, targetCellSize, t),
                    Mathf.Lerp(startGapY, TrayGap, t));
                yield return null;
            }

            if (state != null && state.Rect != null && !state.Placed && (drag == null || drag.Piece != state))
            {
                SetPieceGrid(state, targetCellSize, TrayGap, targetCellSize, TrayGap);
                state.SizeRoutine = null;
            }
        }



        private void AttachPieceToTray(PieceState state)
        {
            state.Placed = false;
            if (state.Slot != null && !state.Slot.gameObject.activeSelf)
            {
                state.Slot.gameObject.SetActive(true);
            }

            state.Rect.SetParent(state.Slot, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            state.Rect.anchoredPosition = Vector2.zero;
            float trayCellSize = TrayCellSize(state);
            SetPieceGrid(state, trayCellSize, TrayGap, trayCellSize, TrayGap);
            state.SlotImage.color = CardRestColor;
            state.Rect.localScale = Vector3.one;
            state.Rect.localEulerAngles = Vector3.zero;
        }

        private void UpdateTrayIdleMotion()
        {
            if (pieces.Count == 0 || reducedMotion)
            {
                return;
            }

            float time = Time.unscaledTime * TrayFloatSpeed;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state == null || state.Rect == null || state.Placed || (drag != null && drag.Piece == state))
                {
                    continue;
                }

                float wave = Mathf.Sin(time + state.FloatPhase);
                state.Rect.anchoredPosition = new Vector2(0f, wave * TrayFloatAmplitude);
                state.Rect.localEulerAngles = new Vector3(0f, 0f, wave * TrayFloatRotation);
            }
        }

        private void AttachPieceToBoard(PieceState state)
        {
            state.Rect.SetParent(boardRoot, false);
            state.Rect.anchorMin = new Vector2(0f, 1f);
            state.Rect.anchorMax = new Vector2(0f, 1f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
            state.Rect.anchoredPosition = BoardPieceCenter(state.Row, state.Col, state);
            state.Rect.localScale = Vector3.one;
            state.Rect.localEulerAngles = Vector3.zero;
            SetPieceSlotVisible(state, false, true);
        }

        private void SetPieceGrid(PieceState state, float cellWidth, float gapX, float cellHeight, float gapY)
        {
            bool resting = state.Placed && (drag == null || drag.Piece != state);
            if (state.HasGridLayout &&
                state.RestingLayout == resting &&
                Mathf.Approximately(state.CellWidth, cellWidth) &&
                Mathf.Approximately(state.CellHeight, cellHeight) &&
                Mathf.Approximately(state.GapX, gapX) &&
                Mathf.Approximately(state.GapY, gapY))
            {
                return;
            }

            state.HasGridLayout = true;
            state.RestingLayout = resting;
            state.CellWidth = cellWidth;
            state.CellHeight = cellHeight;
            state.GapX = gapX;
            state.GapY = gapY;
            float width = (state.Definition.Cols * cellWidth) + ((state.Definition.Cols - 1) * gapX);
            float height = (state.Definition.Rows * cellHeight) + ((state.Definition.Rows - 1) * gapY);
            state.Rect.sizeDelta = new Vector2(width, height);

            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                RectTransform cellRect = state.CellImages[i].rectTransform;
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                cellRect.anchoredPosition = new Vector2(
                    (cell.Col * (cellWidth + gapX)) + (cellWidth * 0.5f),
                    -((cell.Row * (cellHeight + gapY)) + (cellHeight * 0.5f)));
                LayoutCatCell(state.CatViews[i], cellWidth, cellHeight, resting);
            }
        }

        private void LayoutCatCell(CatCellView view, float width, float height, bool resting)
        {
            if (view.Authored)
            {
                view.Portrait.preserveAspect = true;
                return;
            }

            float min = Mathf.Min(width, height);
            SetCenteredChild(view.LeftEar, new Vector2(width * -0.26f, height * 0.43f), new Vector2(min * 0.32f, min * 0.32f));
            SetCenteredChild(view.RightEar, new Vector2(width * 0.26f, height * 0.43f), new Vector2(min * 0.32f, min * 0.32f));
            view.LeftEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            view.RightEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            SetCenteredChild(view.LeftInnerEar, new Vector2(width * -0.26f, height * 0.43f), new Vector2(min * 0.16f, min * 0.16f));
            SetCenteredChild(view.RightInnerEar, new Vector2(width * 0.26f, height * 0.43f), new Vector2(min * 0.16f, min * 0.16f));
            view.LeftInnerEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            view.RightInnerEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            SetCenteredChild(view.Highlight, new Vector2(width * -0.08f, height * 0.08f), new Vector2(width * 0.72f, height * 0.72f));
            view.Highlight.localEulerAngles = Vector3.zero;

            float eyeHeight = resting ? Mathf.Max(2f, height * 0.04f) : height * 0.14f;
            SetCenteredChild(view.LeftEye, new Vector2(width * -0.12f, height * 0.05f), new Vector2(width * 0.09f, eyeHeight));
            SetCenteredChild(view.RightEye, new Vector2(width * 0.12f, height * 0.05f), new Vector2(width * 0.09f, eyeHeight));
            SetCenteredChild(view.Nose, new Vector2(0f, height * -0.06f), new Vector2(width * 0.1f, height * 0.08f));
            SetCenteredChild(view.Mouth, new Vector2(0f, height * -0.2f), new Vector2(width * 0.28f, height * 0.14f));
            SetCenteredChild(view.LeftCheek, new Vector2(width * -0.23f, height * -0.12f), new Vector2(width * 0.13f, height * 0.08f));
            SetCenteredChild(view.RightCheek, new Vector2(width * 0.23f, height * -0.12f), new Vector2(width * 0.13f, height * 0.08f));
            SetCenteredChild(view.LeftWhiskerTop, new Vector2(width * -0.31f, height * -0.08f), new Vector2(width * 0.22f, Mathf.Max(2f, min * 0.025f)));
            SetCenteredChild(view.LeftWhiskerBottom, new Vector2(width * -0.31f, height * -0.17f), new Vector2(width * 0.2f, Mathf.Max(2f, min * 0.022f)));
            SetCenteredChild(view.RightWhiskerTop, new Vector2(width * 0.31f, height * -0.08f), new Vector2(width * 0.22f, Mathf.Max(2f, min * 0.025f)));
            SetCenteredChild(view.RightWhiskerBottom, new Vector2(width * 0.31f, height * -0.17f), new Vector2(width * 0.2f, Mathf.Max(2f, min * 0.022f)));
            view.LeftWhiskerTop.localEulerAngles = new Vector3(0f, 0f, 8f);
            view.LeftWhiskerBottom.localEulerAngles = new Vector3(0f, 0f, -8f);
            view.RightWhiskerTop.localEulerAngles = new Vector3(0f, 0f, -8f);
            view.RightWhiskerBottom.localEulerAngles = new Vector3(0f, 0f, 8f);
            SetCenteredChild(view.ForeheadStripe, new Vector2(0f, height * 0.25f), new Vector2(width * 0.18f, Mathf.Max(2f, min * 0.03f)));
            view.ForeheadStripe.localEulerAngles = new Vector3(0f, 0f, 90f);
            SetCenteredChild(view.Tail, new Vector2(width * 0.55f, height * -0.27f), new Vector2(width * 0.34f, height * 0.22f));
            view.Tail.localEulerAngles = new Vector3(0f, 0f, -13f);
        }

        private void PrepareTrayGesture(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.OnInitializePotentialDrag(eventData);
            trayScrollRect.StopMovement();
        }

        private float GetPieceDragThresholdPixels()
        {
            float configured = layoutProfile != null ? layoutProfile.PiecePickDragThreshold : PiecePickDragThreshold;
            float canvasScale = canvas != null ? Mathf.Max(0.25f, canvas.scaleFactor) : 1f;
            int eventThreshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 0;
            return Mathf.Max(configured * canvasScale, eventThreshold);
        }

        private float GetPiecePickVerticalBias()
        {
            return layoutProfile != null ? layoutProfile.PiecePickVerticalBias : PiecePickVerticalBias;
        }

        private float GetTrayScrollHorizontalBias()
        {
            return layoutProfile != null ? layoutProfile.TrayScrollHorizontalBias : TrayScrollHorizontalBias;
        }

        private void BeginForwardedTrayScroll(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.enabled = true;
            trayScrollRect.OnInitializePotentialDrag(eventData);
            trayScrollRect.OnBeginDrag(eventData);
        }

        private void ForwardTrayScroll(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.OnDrag(eventData);
        }

        private void EndForwardedTrayScroll(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.OnEndDrag(eventData);
        }

        private sealed class PieceDragGesture
        {
            private enum GestureMode
            {
                None,
                Pending,
                PieceDrag,
                TrayScroll
            }

            private CatBlockPuzzleGame controller;
            private PieceState state;
            private bool traySlotProxy;
            private Vector2 pointerDownPosition;
            private int pointerId = int.MinValue;
            private GestureMode gestureMode;

            public void Bind(CatBlockPuzzleGame owner, PieceState pieceState, bool slotProxy)
            {
                controller = owner;
                state = pieceState;
                traySlotProxy = slotProxy;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (eventData == null)
                {
                    return;
                }

                eventData.useDragThreshold = false;
                pointerDownPosition = eventData.position;
                pointerId = eventData.pointerId;
                gestureMode = GestureMode.Pending;
                if (state == null || controller == null)
                {
                    return;
                }

                controller.PrepareTrayGesture(eventData);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                EndGesture(eventData);
            }

            public void OnInitializePotentialDrag(PointerEventData eventData)
            {
                if (eventData != null)
                {
                    eventData.useDragThreshold = false;
                }
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (eventData != null)
                {
                    eventData.useDragThreshold = false;
                }
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (eventData == null || controller == null || state == null || eventData.pointerId != pointerId)
                {
                    return;
                }

                eventData.useDragThreshold = false;
                if (gestureMode == GestureMode.PieceDrag)
                {
                    controller.DragPiece(state, eventData);
                    return;
                }

                if (gestureMode == GestureMode.TrayScroll)
                {
                    controller.ForwardTrayScroll(eventData);
                    return;
                }

                if (gestureMode != GestureMode.Pending)
                {
                    return;
                }

                Vector2 delta = eventData.position - pointerDownPosition;
                if (CanStartPieceDrag(delta))
                {
                    if (controller.BeginPieceDrag(state, eventData))
                    {
                        gestureMode = GestureMode.PieceDrag;
                        controller.DragPiece(state, eventData);
                    }
                    else
                    {
                        ResetGesture();
                    }

                    return;
                }

                if (CanStartTrayScroll(delta))
                {
                    gestureMode = GestureMode.TrayScroll;
                    controller.BeginForwardedTrayScroll(eventData);
                    controller.ForwardTrayScroll(eventData);
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                EndGesture(eventData);
            }

            public void OnCancel(BaseEventData eventData)
            {
                if (controller == null)
                {
                    ResetGesture();
                    return;
                }

                if (gestureMode == GestureMode.PieceDrag)
                {
                    controller.CancelPieceInteraction(state);
                }
                else if (gestureMode == GestureMode.TrayScroll && eventData is PointerEventData pointerEventData)
                {
                    controller.EndForwardedTrayScroll(pointerEventData);
                }

                ResetGesture();
            }

            private bool CanStartPieceDrag(Vector2 delta)
            {
                float threshold = DragThreshold();
                if (!traySlotProxy && state.Placed)
                {
                    return delta.sqrMagnitude >= threshold * threshold;
                }

                if (state.Placed)
                {
                    return false;
                }

                float absX = Mathf.Abs(delta.x);
                return delta.y >= threshold && delta.y >= absX * controller.GetPiecePickVerticalBias();
            }

            private bool CanStartTrayScroll(Vector2 delta)
            {
                float absX = Mathf.Abs(delta.x);
                float absY = Mathf.Abs(delta.y);
                return absX >= DragThreshold() && absX >= absY * controller.GetTrayScrollHorizontalBias();
            }

            private float DragThreshold()
            {
                return controller != null ? controller.GetPieceDragThresholdPixels() : PiecePickDragThreshold;
            }

            private void EndGesture(PointerEventData eventData)
            {
                if (eventData == null || eventData.pointerId != pointerId)
                {
                    return;
                }

                if (gestureMode == GestureMode.PieceDrag)
                {
                    controller.EndPieceDrag(state, eventData);
                }
                else if (gestureMode == GestureMode.TrayScroll)
                {
                    controller.EndForwardedTrayScroll(eventData);
                }

                ResetGesture();
            }

            public void ResetGesture()
            {
                pointerId = int.MinValue;
                gestureMode = GestureMode.None;
            }
        }

        private sealed class PieceState
        {
            public readonly PieceDefinition Definition;
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
            public readonly Vector2Int[] PlacementOffsets;
            public readonly Vector2[] GridCenters;

            public PieceState(PieceDefinition definition, Color color)
            {
                Definition = definition;
                Color = color;
                PlacementOffsets = new Vector2Int[definition.Cells.Length];
                GridCenters = new Vector2[definition.Cells.Length];
                for (int i = 0; i < definition.Cells.Length; i++)
                {
                    PlacementOffsets[i] = new Vector2Int(definition.Cells[i].Row, definition.Cells[i].Col);
                }
            }
        }

        private sealed class CatCellView
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
}
