using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Reference-art level-win overlay: animated earned stars and a single Continue action.</summary>
    public sealed class LevelCompleteScreen : MonoBehaviour
    {
        [System.Serializable]
        internal sealed class StarSprites
        {
            public Sprite blackAndWhite;
            public Sprite disabled;
            public Sprite winStars;
        }

        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [Header("Shared star artwork")]
        [SerializeField] internal StarSprites sprites = new StarSprites();
        [Header("On-screen image placeholders, ordered left to right")]
        [SerializeField] internal Image[] stars = new Image[3];
        [SerializeField] private Button continueButton;

        private Vector2[] starTargets;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings() => BindButtons();
        private void OnEnable() => BindButtons();
        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(Next);
        }

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            root = screenRoot;
            panel = FindChild(root.transform, "Win Panel") as RectTransform;
            continueButton = FindButton(root.transform, "Continue");
            if (continueButton == null) continueButton = FindButton(root.transform, "Next Level");

            Image[] foundImages = root.GetComponentsInChildren<Image>(true);
            var placeholders = new System.Collections.Generic.List<Image>(3);
            for (int i = 0; i < foundImages.Length; i++)
            {
                string name = foundImages[i].name;
                if (name.StartsWith("Result Star", System.StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Star Placeholder", System.StringComparison.OrdinalIgnoreCase))
                    placeholders.Add(foundImages[i]);
            }
            if (placeholders.Count > 0) stars = placeholders.ToArray();
            CacheStarTargets();
            BindButtons();
        }

        // Kept compatible with GameSystem's result contract. The reference art already
        // contains the win title and reward treatment; only the earned star count changes.
        public void Show(string title, int starCount, int reward, int bestStars, int bestCombo)
        {
            if (root == null) return;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            StopAllCoroutines();
            CacheStarTargets();
            StartCoroutine(PlayEntrance(Mathf.Clamp(starCount, 0, stars.Length)));
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        private IEnumerator PlayEntrance(int earnedStars)
        {
            if (panel != null) panel.localScale = Vector3.one * 0.94f;
            SetAllStarStates(StarVisual.Disabled);

            const float panelDuration = 0.18f;
            float panelTime = 0f;
            while (panelTime < panelDuration)
            {
                panelTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(panelTime / panelDuration);
                if (panel != null) panel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            if (panel != null) panel.localScale = Vector3.one;

            for (int i = 0; i < earnedStars; i++)
            {
                if (stars[i] == null) continue;
                SetStarState(i, StarVisual.Win);
                yield return StartCoroutine(FlyStarToSlot(stars[i], i));
            }

            for (int i = earnedStars; i < stars.Length; i++) SetStarState(i, StarVisual.BlackAndWhite);
        }

        private IEnumerator FlyStarToSlot(Image star, int index)
        {
            RectTransform rect = star.rectTransform;
            Vector2 target = starTargets[index];
            Vector2 start = new Vector2(0f, target.y - 210f);
            rect.anchoredPosition = start;
            rect.localScale = Vector3.one * 0.25f;
            const float duration = 0.34f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
                rect.localScale = Vector3.one * (Mathf.Lerp(0.25f, 1f, eased) + (Mathf.Sin(t * Mathf.PI) * 0.20f));
                yield return null;
            }
            rect.anchoredPosition = target;
            rect.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(0.07f);
        }

        private void CacheStarTargets()
        {
            if (starTargets == null || starTargets.Length != stars.Length) starTargets = new Vector2[stars.Length];
            for (int i = 0; i < stars.Length; i++) if (stars[i] != null) starTargets[i] = stars[i].rectTransform.anchoredPosition;
        }

        private enum StarVisual { Disabled, BlackAndWhite, Win }

        private void SetAllStarStates(StarVisual state)
        {
            for (int i = 0; i < stars.Length; i++) SetStarState(i, state);
        }

        private void SetStarState(int index, StarVisual state)
        {
            if (index < 0 || index >= stars.Length || stars[index] == null) return;
            Sprite sprite = state == StarVisual.BlackAndWhite ? sprites?.blackAndWhite :
                state == StarVisual.Disabled ? sprites?.disabled : sprites?.winStars;
            if (sprite == null) return;
            stars[index].sprite = sprite;
            stars[index].gameObject.SetActive(true);
        }

        private void BindButtons()
        {
            if (continueButton == null) return;
            continueButton.onClick.RemoveListener(Next);
            if (continueButton.onClick.GetPersistentEventCount() == 0) continueButton.onClick.AddListener(Next);
        }

        private void Next() => GameSystem.Instance?.NextLevel();

        private static Button FindButton(Transform parent, string name)
        {
            Transform found = FindChild(parent, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
