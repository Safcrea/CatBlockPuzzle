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
        private void BuildPieces()
        {
            ConfigureTrayForPieceCount(activeLevel.Pieces.Length);

            for (int i = 0; i < activeLevel.Pieces.Length; i++)
            {
                PieceDefinition definition = activeLevel.Pieces[i];
                PieceState state = new PieceState(definition, PieceColors[i % PieceColors.Length]);
                state.FloatPhase = i * 0.83f;
                state.Slot = CreatePanel(trayContent, definition.Name + " Slot", CardRestColor);
                state.SlotImage = state.Slot.GetComponent<Image>();
                StyleCreamPanel(state.SlotImage, 0.11f);
                LayoutElement slotLayout = state.Slot.gameObject.AddComponent<LayoutElement>();
                ApplyTraySlotLayout(slotLayout);
                AddDragDots(state.Slot);
                PieceDragView slotDragView = state.Slot.gameObject.AddComponent<PieceDragView>();
                slotDragView.Bind(this, state, true);

                state.Rect = CreatePanel(state.Slot, definition.Name, new Color(1f, 1f, 1f, 0f));
                PieceDragView dragView = state.Rect.gameObject.AddComponent<PieceDragView>();
                dragView.Bind(this, state, false);
                state.Rect.SetAsLastSibling();
                CreatePieceCells(state);
                AttachPieceToTray(state);
                pieces.Add(state);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(trayContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(trayRoot);
            if (trayScrollRect != null)
            {
                trayScrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        private void ConfigureTrayForPieceCount(int pieceCount)
        {
            int count = Mathf.Max(1, pieceCount);
            int horizontalPadding = 24;
            int verticalPadding = 24;
            float spacing = count >= 7 ? 14f : count >= 5 ? 16f : 18f;
            traySlotPreferredWidth = count >= 7 ? 176f : count >= 5 ? 190f : 212f;
            traySlotPreferredHeight = count >= 7 ? 222f : count >= 5 ? 236f : 246f;
            traySlotMinWidth = Mathf.Min(132f, traySlotPreferredWidth);
            traySlotMinHeight = Mathf.Min(188f, traySlotPreferredHeight);
            trayCellMaxSize = count >= 7 ? 40f : count >= 5 ? 43f : 46f;

            float trayWidth = TrayMaxWidth;
            float trayHeight = Mathf.Clamp(traySlotPreferredHeight + (verticalPadding * 2f), 280f, 318f);
            float viewportWidth = Mathf.Max(1f, trayWidth - 36f);
            float desiredContentWidth = (traySlotPreferredWidth * count) + (spacing * (count - 1)) + (horizontalPadding * 2f);

            if (trayLayout != null)
            {
                trayLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
                trayLayout.spacing = spacing;
            }

            SetRect(trayRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, TrayCenterY), new Vector2(trayWidth, trayHeight));
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

        private void CreatePieceCells(PieceState state)
        {
            state.CellImages.Clear();
            state.CatViews.Clear();
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                RectTransform cellRect = CreatePanel(state.Rect, "Cat Cell", state.Color).GetComponent<RectTransform>();
                Image body = cellRect.GetComponent<Image>();
                body.sprite = catHeadSprite;
                body.type = Image.Type.Simple;
                body.raycastTarget = false;
                AddSoftShadow(body, new Vector2(0f, -4f), 0.18f);
                AddSoftOutline(body, new Color(1f, 1f, 1f, 0.78f), new Vector2(2f, -2f));

                Image leftEar = CreateImage(cellRect, "Ear Left", state.Color);
                Image rightEar = CreateImage(cellRect, "Ear Right", state.Color);
                Image leftInnerEar = CreateImage(cellRect, "Inner Ear Left", new Color(1f, 0.76f, 0.78f, 0.58f));
                Image rightInnerEar = CreateImage(cellRect, "Inner Ear Right", new Color(1f, 0.76f, 0.78f, 0.58f));
                Image highlight = CreateImage(cellRect, "Body Highlight", new Color(1f, 1f, 1f, 0.18f));
                Image leftEye = CreateImage(cellRect, "Eye Left", InkColor);
                Image rightEye = CreateImage(cellRect, "Eye Right", InkColor);
                Image nose = CreateImage(cellRect, "Nose", new Color(0.38f, 0.23f, 0.22f, 0.62f));
                Image mouth = CreateImage(cellRect, "Mouth", new Color(0.18f, 0.16f, 0.14f, 0.62f));
                Image leftCheek = CreateImage(cellRect, "Cheek Left", new Color(1f, 0.47f, 0.55f, 0.2f));
                Image rightCheek = CreateImage(cellRect, "Cheek Right", new Color(1f, 0.47f, 0.55f, 0.2f));
                Image leftWhiskerTop = CreateImage(cellRect, "Whisker Left Top", new Color(0.18f, 0.16f, 0.14f, 0.28f));
                Image leftWhiskerBottom = CreateImage(cellRect, "Whisker Left Bottom", new Color(0.18f, 0.16f, 0.14f, 0.24f));
                Image rightWhiskerTop = CreateImage(cellRect, "Whisker Right Top", new Color(0.18f, 0.16f, 0.14f, 0.28f));
                Image rightWhiskerBottom = CreateImage(cellRect, "Whisker Right Bottom", new Color(0.18f, 0.16f, 0.14f, 0.24f));
                Image foreheadStripe = CreateImage(cellRect, "Forehead Stripe", new Color(0.18f, 0.16f, 0.14f, 0.16f));
                Image tail = CreateImage(cellRect, "Tail", new Color(0.18f, 0.16f, 0.14f, 0.18f));

                leftEar.raycastTarget = false;
                rightEar.raycastTarget = false;
                leftInnerEar.raycastTarget = false;
                rightInnerEar.raycastTarget = false;
                highlight.raycastTarget = false;
                leftEye.raycastTarget = false;
                rightEye.raycastTarget = false;
                nose.raycastTarget = false;
                mouth.raycastTarget = false;
                leftCheek.raycastTarget = false;
                rightCheek.raycastTarget = false;
                leftWhiskerTop.raycastTarget = false;
                leftWhiskerBottom.raycastTarget = false;
                rightWhiskerTop.raycastTarget = false;
                rightWhiskerBottom.raycastTarget = false;
                foreheadStripe.raycastTarget = false;
                tail.raycastTarget = false;
                leftEar.sprite = catHeadSprite;
                rightEar.sprite = catHeadSprite;
                leftInnerEar.sprite = catHeadSprite;
                rightInnerEar.sprite = catHeadSprite;
                highlight.sprite = catHeadSprite;
                leftEar.type = Image.Type.Simple;
                rightEar.type = Image.Type.Simple;
                leftInnerEar.type = Image.Type.Simple;
                rightInnerEar.type = Image.Type.Simple;
                leftEye.sprite = circleSprite;
                rightEye.sprite = circleSprite;
                nose.sprite = circleSprite;
                mouth.sprite = mouthSprite;
                leftCheek.sprite = circleSprite;
                rightCheek.sprite = circleSprite;
                tail.sprite = tailSprite;

                state.CellImages.Add(body);
                state.CatViews.Add(new CatCellView(
                    cellRect,
                    leftEar.rectTransform,
                    rightEar.rectTransform,
                    leftInnerEar.rectTransform,
                    rightInnerEar.rectTransform,
                    highlight.rectTransform,
                    leftEye.rectTransform,
                    rightEye.rectTransform,
                    nose.rectTransform,
                    mouth.rectTransform,
                    leftCheek.rectTransform,
                    rightCheek.rectTransform,
                    leftWhiskerTop.rectTransform,
                    leftWhiskerBottom.rectTransform,
                    rightWhiskerTop.rectTransform,
                    rightWhiskerBottom.rectTransform,
                    foreheadStripe.rectTransform,
                    tail.rectTransform));
            }
        }

        private void AttachPieceToTray(PieceState state)
        {
            state.Placed = false;
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
            if (pieces.Count == 0)
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
            state.SlotImage.color = CardDimColor;
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

        private sealed class PieceDragView : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, ICancelHandler
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
                return delta.y >= threshold && delta.y >= absX * PiecePickVerticalBias;
            }

            private bool CanStartTrayScroll(Vector2 delta)
            {
                float absX = Mathf.Abs(delta.x);
                float absY = Mathf.Abs(delta.y);
                return absX >= DragThreshold() && absX >= absY * TrayScrollHorizontalBias;
            }

            private float DragThreshold()
            {
                int eventThreshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 0;
                return Mathf.Max(PiecePickDragThreshold, eventThreshold);
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

            private void ResetGesture()
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

            public PieceState(PieceDefinition definition, Color color)
            {
                Definition = definition;
                Color = color;
            }
        }

        private sealed class CatCellView
        {
            public readonly RectTransform Body;
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
