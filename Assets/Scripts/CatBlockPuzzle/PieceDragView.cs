using UnityEngine;
using UnityEngine.EventSystems;
namespace CatBlockPuzzle
{
        public sealed class PieceDragView : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, ICancelHandler
        {
            private enum GestureMode
            {
                None,
                Pending,
                PieceDrag,
                TrayScroll
            }

            private CatBlockPuzzleGame controller;
            private CatBlockPuzzleGame.PieceState state;
            private bool traySlotProxy;
            private Vector2 pointerDownPosition;
            private int pointerId = int.MinValue;
            private GestureMode gestureMode;

            internal void Bind(CatBlockPuzzleGame owner, CatBlockPuzzleGame.PieceState pieceState, bool slotProxy)
            {
                ResetGesture();
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
                return controller != null ? controller.GetPieceDragThresholdPixels() : 14f;
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

}
