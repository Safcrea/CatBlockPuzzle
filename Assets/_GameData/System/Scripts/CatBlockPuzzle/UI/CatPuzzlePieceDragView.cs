using UnityEngine;
using UnityEngine.EventSystems;

namespace CatBlockPuzzle
{
    public sealed class CatPuzzlePieceDragView : MonoBehaviour, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, ICancelHandler
    {
        // Injected once from the loaded level's saved piece bindings; never a hierarchy search.
        [System.NonSerialized] public CatBlockPuzzleGame controller;
        public int levelIndex;
        public int pieceIndex;
        public bool slotProxy;
        public enum GestureEvent { Initialize, Begin, Drag, End, Down, Up, Cancel }
        private void Send(GestureEvent action, BaseEventData data)
        {
            if (controller != null && controller.isActiveAndEnabled)
                controller.HandlePreparedGesture(levelIndex, pieceIndex, slotProxy, action, data);
        }
        public void OnInitializePotentialDrag(PointerEventData e) => Send(GestureEvent.Initialize, e);
        public void OnBeginDrag(PointerEventData e) => Send(GestureEvent.Begin, e);
        public void OnDrag(PointerEventData e) => Send(GestureEvent.Drag, e);
        public void OnEndDrag(PointerEventData e) => Send(GestureEvent.End, e);
        public void OnPointerDown(PointerEventData e) => Send(GestureEvent.Down, e);
        public void OnPointerUp(PointerEventData e) => Send(GestureEvent.Up, e);
        public void OnCancel(BaseEventData e) => Send(GestureEvent.Cancel, e);
    }
}
