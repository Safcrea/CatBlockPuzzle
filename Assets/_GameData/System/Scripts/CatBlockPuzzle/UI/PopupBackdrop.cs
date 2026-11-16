using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The one dim every popup shares. A single full-screen dark object ("Popup Dim") lives under the canvas and is turned on
    /// behind whichever popup is open, fading in and out, and swallowing taps so nothing behind the popup can be pressed. When
    /// two popups are open it sits just behind the newest one. How dark it is comes from one setting
    /// (<see cref="DimAlpha255"/>, 0 to 255), set on GameSystem and in the Setup window.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupBackdrop : MonoBehaviour, IPointerClickHandler
    {
        public const string ObjectName = "Popup Dim";
        /// <summary>How dark the screen goes behind a popup, 0 (clear) to 255 (black). Set from GameSystem.</summary>
        public static int DimAlpha255 = 244;
        private const float FadeInSeconds = 0.22f;
        private const float FadeOutSeconds = 0.2f;

        private static PopupBackdrop instance;
        private readonly List<PopupDimClient> open = new List<PopupDimClient>();
        private Image image;
        private float shown;
        private static bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        /// <summary>Makes a popup use the shared dim: it is turned on while the popup is active. Returns the popup's handle.</summary>
        public static PopupDimClient Attach(RectTransform popupRoot)
        {
            if (popupRoot == null) return null;
            var client = popupRoot.GetComponent<PopupDimClient>();
            if (client == null) client = popupRoot.gameObject.AddComponent<PopupDimClient>();
            // A popup root may carry its own light shade; the shared dim replaces it so the dim is not doubled or uneven.
            Image own = popupRoot.GetComponent<Image>();
            if (own != null) own.color = new Color(own.color.r, own.color.g, own.color.b, 0f);
            return client;
        }

        internal static void Request(PopupDimClient client)
        {
            if (client == null || client.transform.parent == null) return;
            PopupBackdrop dim = EnsureInstance(client.transform.parent);
            if (!dim.open.Contains(client)) dim.open.Add(client);
            dim.gameObject.SetActive(true);
            dim.Reposition();
        }

        internal static void Release(PopupDimClient client)
        {
            if (instance == null || client == null) return;
            instance.open.Remove(client);
            if (instance.open.Count > 0) instance.Reposition();
        }

        private static PopupBackdrop EnsureInstance(Transform parent)
        {
            if (instance == null)
            {
                Transform existing = parent.Find(ObjectName);
                GameObject go = existing != null ? existing.gameObject : new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
                instance = go.GetComponent<PopupBackdrop>();
                if (instance == null) instance = go.AddComponent<PopupBackdrop>();
                instance.image = go.GetComponent<Image>();
                instance.image.raycastTarget = true;
                instance.image.color = new Color(0f, 0f, 0f, 0f);
                instance.shown = 0f;
            }
            return instance;
        }

        // Sits directly behind the newest open popup, in that popup's own parent.
        private void Reposition()
        {
            if (open.Count == 0) return;
            Transform popup = open[open.Count - 1].transform;
            var rect = (RectTransform)transform;
            if (rect.parent != popup.parent) rect.SetParent(popup.parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetSiblingIndex(popup.GetSiblingIndex());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (open.Count > 0) open[open.Count - 1].Clicked?.Invoke();
        }

        private void Update()
        {
            float target = open.Count > 0 ? 1f : 0f;
            if (!Mathf.Approximately(shown, target))
            {
                float seconds = target > shown ? FadeInSeconds : FadeOutSeconds;
                shown = ReducedMotion ? target : Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / seconds);
            }
            if (image != null) image.color = new Color(0f, 0f, 0f, Mathf.Clamp01(DimAlpha255 / 255f) * shown);
            // Fully faded out and nothing open: turn the object off until the next popup needs it.
            if (open.Count == 0 && shown <= 0.001f) gameObject.SetActive(false);
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>
        /// The close X is small and hangs off the panel corner; this tucks a smaller one into the top-right corner of the cream
        /// panel, fully inside it, with a generous invisible touch area so a thumb still hits it first time.
        /// </summary>
        public static void EnlargeCloseButton(Button close, float scale = 0.85f)
        {
            if (close == null) return;
            var rect = close.transform as RectTransform;
            if (rect != null)
            {
                // The cream panel reaches about +/-337 across and +176 up from the popup's centre.
                rect.localScale = Vector3.one * scale;
                rect.anchoredPosition = new Vector2(295f, 135f);
            }
            var image = close.GetComponent<Image>();
            if (image != null) image.raycastPadding = new Vector4(-18f, -18f, -18f, -18f);
        }
    }

    /// <summary>A popup's handle on the shared dim: turns it on when the popup activates and off when it goes away.</summary>
    public sealed class PopupDimClient : MonoBehaviour
    {
        /// <summary>Raised when the player taps the dim outside the panel. Popups that should close that way set it.</summary>
        public Action Clicked;
        private bool requested;

        private void OnEnable() { requested = true; PopupBackdrop.Request(this); }
        private void OnDisable() => Conceal();

        /// <summary>Starts fading the dim out now (while the popup's own closing animation plays).</summary>
        public void Conceal()
        {
            if (!requested) return;
            requested = false;
            PopupBackdrop.Release(this);
        }
    }
}
