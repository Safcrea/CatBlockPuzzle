using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Lets drags that start outside a scroll view (headers, gradients, the page background)
    /// still scroll it, so the whole page behaves like one scroll surface. Drags that start
    /// inside the scroll view are handled by the ScrollRect itself and never reach this.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScrollDragForwarder : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private ScrollRect target;

        public ScrollRect Target
        {
            get => target;
            set => target = value;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) { if (CanForward) target.OnInitializePotentialDrag(eventData); }
        public void OnBeginDrag(PointerEventData eventData) { if (CanForward) target.OnBeginDrag(eventData); }
        public void OnDrag(PointerEventData eventData) { if (CanForward) target.OnDrag(eventData); }
        public void OnEndDrag(PointerEventData eventData) { if (CanForward) target.OnEndDrag(eventData); }
        public void OnScroll(PointerEventData eventData) { if (CanForward) target.OnScroll(eventData); }

        private bool CanForward => target != null && target.isActiveAndEnabled;
    }
}
