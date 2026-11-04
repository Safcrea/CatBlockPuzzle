using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Plays the error sound when a button that is switched off is tapped, so dimmed
    /// or locked buttons still answer the player. An interactable button already plays its
    /// own sound from its handler, so this stays silent for it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LockedButtonTapSfx : MonoBehaviour, IPointerClickHandler
    {
        private Button button;

        public static void Ensure(Button target)
        {
            if (target == null) return;
            LockedButtonTapSfx existing = target.GetComponent<LockedButtonTapSfx>();
            if (existing == null) existing = target.gameObject.AddComponent<LockedButtonTapSfx>();
            existing.button = target;
        }

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (button != null && button.IsInteractable()) return;
            ButtonSfx.PlayError();
        }
    }
}
