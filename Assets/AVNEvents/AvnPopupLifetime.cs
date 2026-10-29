using UnityEngine;

namespace AVNEvents
{
    // Bound to the actual popup root, including when its controller lives elsewhere.
    public sealed class AvnPopupLifetime : MonoBehaviour
    {
        public AvnScreen Screen;
        private void OnDisable() => AvnEvents.PopupClosed(Screen, ScreenClosedBy.System);
    }
}
