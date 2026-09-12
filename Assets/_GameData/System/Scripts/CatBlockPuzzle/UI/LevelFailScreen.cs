using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class LevelFailScreen : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal Text titleText;
        [SerializeField] internal Text messageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeSelf;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings() => CaptureExisting(root);

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            root = screenRoot;
            Transform panelTransform = root.transform.Find("Fail Panel");
            if (panelTransform != null) panel = panelTransform as RectTransform;
            Button foundRetry = FindButton("Retry");
            Button foundHome = FindButton("Home");
            if (foundRetry != null) retryButton = foundRetry;
            if (foundHome != null) homeButton = foundHome;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0) titleText = texts[i];
                if (texts[i].name.IndexOf("Message", System.StringComparison.OrdinalIgnoreCase) >= 0) messageText = texts[i];
            }
            BindButtons();
        }

        public void Show(string title, string message)
        {
            if (root == null) return;
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            root.SetActive(true);
            if (panel != null)
            {
                StopAllCoroutines();
                StartCoroutine(PopPanel());
            }
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        private IEnumerator PopPanel()
        {
            panel.localScale = Vector3.one * 0.88f;
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = t < 0.7f ? Mathf.Lerp(0.88f, 1.03f, t / 0.7f) : Mathf.Lerp(1.03f, 1f, (t - 0.7f) / 0.3f);
                panel.localScale = Vector3.one * scale;
                yield return null;
            }
            panel.localScale = Vector3.one;
        }

        private Button FindButton(string buttonName)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true)) if (button.name == buttonName) return button;
            return null;
        }

        private void BindButtons()
        {
            if (retryButton != null && retryButton.onClick.GetPersistentEventCount() == 0) retryButton.onClick.AddListener(() => GameSystem.Instance?.RestartLevel());
            if (homeButton != null && homeButton.onClick.GetPersistentEventCount() == 0) homeButton.onClick.AddListener(() => GameSystem.Instance?.GoHome());
        }
    }
}
