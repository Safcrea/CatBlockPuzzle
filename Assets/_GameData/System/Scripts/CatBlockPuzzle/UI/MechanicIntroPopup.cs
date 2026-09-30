using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The "New!" card that introduces a level mechanic. It is a prefab built from the power-up
    /// purchase popup (same flowered panel, title, icon and wooden button), created with
    /// Cat Block Puzzle > Build Mechanic Intro Popup Prefab.
    /// </summary>
    public sealed class MechanicIntroPopup : MonoBehaviour
    {
        public const string PrefabResourcePath = "CatBlockPuzzle/MechanicIntroPopup";

        [SerializeField] private RectTransform panel;
        [SerializeField] private Text heading;
        [SerializeField] private Text message;
        [SerializeField] private Image icon;
        [SerializeField] private Button continueButton;

        private Action onClosed;
        private bool closing;

        public bool IsOpen => gameObject.activeInHierarchy && !closing;
        private static bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        /// <summary>Instantiates the prefab under <paramref name="canvas"/>; null when the prefab is missing or broken.</summary>
        public static MechanicIntroPopup Create(RectTransform canvas, Font hudFont)
        {
            if (canvas == null) return null;
            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, canvas, false);
            instance.name = "Mechanic Intro";
            MechanicIntroPopup popup = instance.GetComponent<MechanicIntroPopup>();
            if (popup == null || popup.panel == null || popup.heading == null || popup.message == null || popup.continueButton == null)
            {
                Debug.LogError("MechanicIntroPopup prefab is missing its references; rebuild it from the Cat Block Puzzle menu.", prefab);
                Destroy(instance);
                return null;
            }

            // Fit the authored panel inside narrow canvases, like the purchase popup does.
            Vector2 available = canvas.rect.size - new Vector2(64f, 64f);
            Vector2 size = popup.panel.rect.size;
            if (size.x > 0f && size.y > 0f && available.x > 0f && available.y > 0f)
                popup.panel.localScale = Vector3.one * Mathf.Clamp(Mathf.Min(available.x / size.x, available.y / size.y), 0.1f, 1f);

            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Text text in instance.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                if (hudFont != null && (text.font == null || text.font == builtin)) text.font = hudFont;
            }

            instance.SetActive(false);
            return popup;
        }

        public void Show(string title, string body, Sprite iconSprite, Action closed)
        {
            onClosed = closed;
            closing = false;
            heading.text = title;
            message.text = body;
            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            continueButton.onClick.RemoveListener(Continue);
            continueButton.onClick.AddListener(Continue);
            continueButton.interactable = true;
            transform.SetAsLastSibling();
            if (ReducedMotion) gameObject.SetActive(true);
            else MenuTransition.Show(gameObject, true, new MenuTransition.EntranceSettings { duration = .35f, slideDistance = 24f, overshoot = 1.2f });
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }

        public void Continue()
        {
            if (closing) return;
            closing = true;
            continueButton.interactable = false;
            ButtonSfx.PlayClick();
            MenuTransition.Hide(gameObject, () =>
            {
                Action done = onClosed;
                onClosed = null;
                done?.Invoke();
            }, !ReducedMotion);
        }
    }
}
