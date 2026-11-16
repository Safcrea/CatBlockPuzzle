using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The dim behind a popup: one full-screen dark layer that fades in when the popup opens and out while it closes, and
    /// swallows taps so nothing behind the popup can be pressed. Every popup uses this one component so they all dim the same.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupBackdrop : MonoBehaviour, IPointerClickHandler
    {
        public const string ChildName = "Popup Backdrop";
        /// <summary>How dark the screen goes behind a popup (0 = none, 1 = black).</summary>
        public const float DimAlpha = 0.8f;
        private const float FadeInSeconds = 0.22f;
        private const float FadeOutSeconds = 0.2f;

        /// <summary>Raised when the player taps the dim outside the panel. Popups that should close that way set it.</summary>
        public Action Clicked;

        private Image image;
        private float shown;
        private float target;
        private static bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        /// <summary>Adds (or finds) the backdrop as the bottom-most child of a popup's root, so the panel draws above it.</summary>
        public static PopupBackdrop Attach(RectTransform popupRoot)
        {
            if (popupRoot == null) return null;
            Transform existing = popupRoot.Find(ChildName);
            GameObject child = existing != null ? existing.gameObject : new GameObject(ChildName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)child.transform;
            rect.SetParent(popupRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            var image = child.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
            var backdrop = child.GetComponent<PopupBackdrop>();
            if (backdrop == null) backdrop = child.AddComponent<PopupBackdrop>();
            backdrop.image = image;

            // A popup root may carry its own light shade; the backdrop replaces it so the dim is not doubled or uneven.
            Image own = popupRoot.GetComponent<Image>();
            if (own != null) own.color = new Color(own.color.r, own.color.g, own.color.b, 0f);
            return backdrop;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();

        /// <summary>
        /// The red X is small and hangs off the panel corner; this makes it bigger, pulls it inside the corner and gives it a
        /// generous invisible touch area, so a thumb hits it first time.
        /// </summary>
        public static void EnlargeCloseButton(Button close, float scale = 0.85f)
        {
            if (close == null) return;
            var rect = close.transform as RectTransform;
            if (rect != null)
            {
                // The cream panel reaches about +/-337 across and +176 up from the popup's centre; the X is a little one tucked
                // right into that top-right corner, its whole circle still inside the panel.
                rect.localScale = Vector3.one * scale;
                rect.anchoredPosition = new Vector2(295f, 135f);
            }
            var image = close.GetComponent<Image>();
            if (image != null) image.raycastPadding = new Vector4(-18f, -18f, -18f, -18f);
        }

        /// <summary>Starts (or resumes) the fade in. Called when the popup opens.</summary>
        public void Reveal() { target = 1f; if (ReducedMotion) shown = 1f; Apply(); }

        /// <summary>Fades out while the popup's own closing animation plays.</summary>
        public void Conceal() { target = 0f; if (ReducedMotion) shown = 0f; Apply(); }

        private void OnEnable() { shown = 0f; target = 1f; Apply(); }

        // The popup root can be activated (OnEnable) before or after Show is called; either way the dim ends up on.
        private void Update()
        {
            if (Mathf.Approximately(shown, target)) return;
            float seconds = target > shown ? FadeInSeconds : FadeOutSeconds;
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / seconds);
            Apply();
        }

        private void Apply()
        {
            if (image == null) image = GetComponent<Image>();
            if (image != null) image.color = new Color(0f, 0f, 0f, DimAlpha * shown);
        }
    }
}
