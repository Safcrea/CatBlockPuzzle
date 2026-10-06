using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Plays the button click the moment a button is pressed instead of when the finger lifts,
    /// which is what made taps feel late. Buttons inside a scroll view keep the release click so
    /// dragging the level list never clicks. The release-time ButtonSfx.PlayClick then skips its sound.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonPressSfx : MonoBehaviour
    {
        private const float PressWindowSeconds = 1f;

        private static float lastPressTime = -10f;
        private static bool pressPending;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();

        /// <summary>True (once) when a press click just played for the button now being released.</summary>
        internal static bool ConsumeRecentPress()
        {
            bool recent = pressPending && Time.unscaledTime - lastPressTime <= PressWindowSeconds;
            pressPending = false;
            return recent;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            pressPending = false;
            EventSystem events = EventSystem.current;
            if (events == null) return;

            var data = new PointerEventData(events) { position = pointer.position.ReadValue() };
            hits.Clear();
            events.RaycastAll(data, hits);
            if (hits.Count == 0) return;
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            if (handler == null || !handler.TryGetComponent(out Button button) || !button.IsInteractable()) return;
            if (button.GetComponentInParent<ScrollRect>() != null) return;

            SoundManager.EnsureInstance().PlaySfx(ButtonSfx.ClickId);
            lastPressTime = Time.unscaledTime;
            pressPending = true;
        }
    }
}
