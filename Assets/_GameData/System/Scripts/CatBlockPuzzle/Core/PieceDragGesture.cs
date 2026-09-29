using UnityEngine;
using UnityEngine.EventSystems;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Decides, per pointer, whether a touch on a piece (or its tray card) becomes a piece
    /// drag or a horizontal tray scroll, then forwards the gesture to the right service.
    /// </summary>
    internal sealed class PieceDragGesture
    {
        private enum GestureMode
        {
            None,
            Pending,
            PieceDrag,
            TrayScroll
        }

        private PieceDragController drag;
        private TrayLayout tray;
        private PieceState state;
        private bool traySlotProxy;
        private Vector2 pointerDownPosition;
        private int pointerId = int.MinValue;
        private GestureMode gestureMode;

        public void Bind(PieceDragController dragController, TrayLayout trayLayout, PieceState pieceState, bool slotProxy)
        {
            drag = dragController;
            tray = trayLayout;
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
            if (state == null || drag == null)
            {
                return;
            }

            tray.PrepareGesture(eventData);
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
            if (eventData == null || drag == null || state == null || eventData.pointerId != pointerId)
            {
                return;
            }

            eventData.useDragThreshold = false;
            if (gestureMode == GestureMode.PieceDrag)
            {
                drag.DragPiece(state, eventData);
                return;
            }

            if (gestureMode == GestureMode.TrayScroll)
            {
                tray.ForwardScroll(eventData);
                return;
            }

            if (gestureMode != GestureMode.Pending)
            {
                return;
            }

            Vector2 delta = eventData.position - pointerDownPosition;
            if (CanStartPieceDrag(delta))
            {
                if (drag.BeginPieceDrag(state, eventData))
                {
                    gestureMode = GestureMode.PieceDrag;
                    drag.DragPiece(state, eventData);
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
                tray.BeginForwardedScroll(eventData);
                tray.ForwardScroll(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndGesture(eventData);
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (drag == null)
            {
                ResetGesture();
                return;
            }

            if (gestureMode == GestureMode.PieceDrag)
            {
                drag.CancelPieceInteraction(state);
            }
            else if (gestureMode == GestureMode.TrayScroll && eventData is PointerEventData pointerEventData)
            {
                tray.EndForwardedScroll(pointerEventData);
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
            return delta.y >= threshold && delta.y >= absX * tray.PiecePickVerticalBias();
        }

        private bool CanStartTrayScroll(Vector2 delta)
        {
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);
            return absX >= DragThreshold() && absX >= absY * tray.TrayScrollHorizontalBias();
        }

        private float DragThreshold()
        {
            return tray != null ? tray.PieceDragThresholdPixels() : PuzzleTuning.PiecePickDragThreshold;
        }

        private void EndGesture(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != pointerId)
            {
                return;
            }

            if (gestureMode == GestureMode.PieceDrag)
            {
                drag.EndPieceDrag(state, eventData);
            }
            else if (gestureMode == GestureMode.TrayScroll)
            {
                tray.EndForwardedScroll(eventData);
            }

            ResetGesture();
        }

        public void ResetGesture()
        {
            pointerId = int.MinValue;
            gestureMode = GestureMode.None;
        }
    }
}
