using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Reference-art level-fail overlay with retry, home, and skip controls only.</summary>
    public sealed class LevelFailScreen : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button skipButton;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings() => BindButtons();
        private void OnEnable() => BindButtons();
        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
        }

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            root = screenRoot;
            panel = FindChild(root.transform, "Fail Panel") as RectTransform;
            retryButton = FindButton("Retry");
            homeButton = FindButton("Home");
            skipButton = FindButton("Skip Level");
            BindButtons();
        }

        // The title and encouragement are baked into the reference artwork.
        public void Show(string title, string message)
        {
            if (root == null) return;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (panel != null)
            {
                StopAllCoroutines();
                StartCoroutine(PopPanel());
            }
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        private IEnumerator PopPanel()
        {
            panel.localScale = Vector3.one * 0.94f;
            float elapsed = 0f;
            const float duration = 0.20f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                panel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            panel.localScale = Vector3.one;
        }

        private Button FindButton(string buttonName)
        {
            Transform found = FindChild(root.transform, buttonName);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private void BindButtons()
        {
            BindIfEmpty(retryButton, Retry);
            BindIfEmpty(homeButton, Home);
            BindIfEmpty(skipButton, Skip);
        }

        private static void BindIfEmpty(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (button.onClick.GetPersistentEventCount() == 0) button.onClick.AddListener(action);
        }

        private void Retry() => GameSystem.Instance?.RestartLevel();
        private void Home() => GameSystem.Instance?.GoHome();
        private void Skip() => GameSystem.Instance?.NextLevel();

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
