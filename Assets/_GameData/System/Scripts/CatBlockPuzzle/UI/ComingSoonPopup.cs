using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class ComingSoonPopup : MonoBehaviour
    {
        private Text title;
        private GameObject previousSelection;
        private Button dismiss;

        public static ComingSoonPopup Create(Transform parent)
        {
            var root = RuntimeUiFactory.CreateOverlay(parent, "Coming Soon Popup");
            var popup = root.gameObject.AddComponent<ComingSoonPopup>();
            var panel = RuntimeUiFactory.CreatePanel(root, "Panel", new Vector2(720f, 420f));
            // Keep the card within the canvas even on narrow aspect ratios.
            var available = parent as RectTransform;
            if (available != null && available.rect.width > 0f)
                panel.sizeDelta = new Vector2(Mathf.Min(720f, available.rect.width - 48f), 420f);
            popup.title = RuntimeUiFactory.CreateText(panel, "Title", "Coming soon", 48, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.title.rectTransform, new Vector2(0f, 115f), new Vector2(panel.sizeDelta.x - 48f, 80f));
            var message = RuntimeUiFactory.CreateText(panel, "Message", "Coming soon!\nMore cozy adventures are on the way.", 30, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(message.rectTransform, new Vector2(0f, 10f), new Vector2(panel.sizeDelta.x - 48f, 120f));
            popup.dismiss = RuntimeUiFactory.CreateButton(panel, "Okay", "Got it!", new Vector2(0f, -125f), new Vector2(280f, 78f), RuntimeUiFactory.Coral);
            popup.dismiss.onClick.AddListener(popup.Close);
            root.gameObject.SetActive(false);
            return popup;
        }

        public void Show(string feature)
        {
            if (!gameObject.activeSelf && EventSystem.current != null)
                previousSelection = EventSystem.current.currentSelectedGameObject;
            title.text = feature;
            MenuTransition.Show(gameObject);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(dismiss.gameObject);
        }

        public void Close() => MenuTransition.Hide(gameObject, RestoreSelection);
        public void HideImmediately() => MenuTransition.Hide(gameObject, null, false);
        private void RestoreSelection()
        {
            if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }
}
