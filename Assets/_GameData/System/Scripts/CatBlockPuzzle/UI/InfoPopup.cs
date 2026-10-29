using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// A small cat-panel dialog that tells the player what is going on in a few words: a title, one
    /// picture and one line (which can tick, like a countdown). It reuses the look of the power-up
    /// purchase popup. The prefab is Resources/CatBlockPuzzle/InfoPopup.
    /// </summary>
    public sealed class InfoPopup : MonoBehaviour
    {
        public const string PrefabResourcePath = "CatBlockPuzzle/InfoPopup";

        [SerializeField] private RectTransform panel;
        [SerializeField] private Text title;
        [SerializeField] private Image icon;
        [SerializeField] private Text message;
        [SerializeField] private Button okButton;
        [SerializeField] private Button closeButton;

        private Func<string> messageSource;
        private Coroutine motion;
        private bool closing;
        private static bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        public bool IsOpen => gameObject.activeInHierarchy && !closing;

        /// <summary>Raised once the popup has finished closing.</summary>
        public event Action Closed;
        private bool shown;

        /// <summary>Spawns the popup under <paramref name="parent"/>; null when the prefab is missing or broken.</summary>
        public static InfoPopup Create(Transform parent)
        {
            if (parent == null) return null;
            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = "Info Popup";
            var popup = instance.GetComponent<InfoPopup>();
            if (popup == null || !popup.Bind())
            {
                Debug.LogError("InfoPopup prefab is missing its references; rebuild it from the Cat Block Puzzle menu.", prefab);
                Destroy(instance);
                return null;
            }
            return popup;
        }

        private bool Bind()
        {
            if (panel == null) panel = transform.Find("Panel") as RectTransform;
            if (panel == null || title == null || icon == null || message == null || okButton == null || closeButton == null) return false;
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            okButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            foreach (Text text in GetComponentsInChildren<Text>(true)) text.raycastTarget = false;
            gameObject.SetActive(false);
            return true;
        }

        /// <param name="messageLine">Called every quarter second while open, so a countdown stays live.</param>
        /// <param name="iconMaterial">Optional (a grey material to show the picture as unavailable).</param>
        public void Show(string heading, Sprite picture, Func<string> messageLine, Material iconMaterial = null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Vector2 available = ((RectTransform)canvas.rootCanvas.transform).rect.size - new Vector2(64f, 64f);
                Vector2 size = panel.rect.size;
                panel.localScale = Vector3.one * Mathf.Clamp(Mathf.Min(available.x / size.x, available.y / size.y), 0.1f, 1f);
            }
            closing = false;
            messageSource = messageLine;
            shown = true;
            title.text = heading;
            icon.sprite = picture;
            icon.enabled = picture != null;
            icon.material = iconMaterial != null ? iconMaterial : null;
            RefreshMessage();
            transform.SetAsLastSibling();
            if (ReducedMotion) gameObject.SetActive(true);
            else MenuTransition.Show(gameObject, true, new MenuTransition.EntranceSettings { duration = .35f, slideDistance = 24f, overshoot = 1.2f });
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Idle());
        }

        public void Close()
        {
            if (!IsOpen) return;
            ButtonSfx.PlayClick();
            closing = true;
            MenuTransition.Hide(gameObject, null, !ReducedMotion);
        }

        private void RefreshMessage()
        {
            if (message == null) return;
            message.text = messageSource != null ? messageSource() : string.Empty;
        }

        // Ticks the line and keeps the picture gently alive: a bob and a little wiggle.
        private IEnumerator Idle()
        {
            RectTransform iconRect = icon.rectTransform;
            float nextRefresh = 0f, time = 0f;
            if (!ReducedMotion)
                for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
                {
                    iconRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(Mathf.Clamp01(t / 0.4f)));
                    yield return null;
                }
            while (true)
            {
                time += Time.unscaledDeltaTime;
                if (time >= nextRefresh) { RefreshMessage(); nextRefresh = time + 0.25f; }
                if (!ReducedMotion)
                {
                    iconRect.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(time * 3.2f));
                    iconRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 2.1f) * 3f);
                }
                yield return null;
            }
        }

        private void OnDisable()
        {
            motion = null;
            closing = false;
            if (icon != null) { icon.rectTransform.localScale = Vector3.one; icon.rectTransform.localRotation = Quaternion.identity; }
            if (!shown) return;
            shown = false;
            Closed?.Invoke();
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
