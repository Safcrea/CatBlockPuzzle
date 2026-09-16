using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class LevelCompleteScreen : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal Text titleText;
        [SerializeField] internal Text rewardText;
        [SerializeField] internal Text bestText;
        [SerializeField] internal Text unlockText;
        [SerializeField] internal Image unlockImage;
        [SerializeField] internal Image[] stars = new Image[3];
        [SerializeField] private Button nextButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        internal RectTransform RootRect => root != null ? root.transform as RectTransform : null;

        public void EnsureBindings() => BindButtons();
        private void OnEnable() => BindButtons();
        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (homeButton != null) homeButton.onClick.RemoveListener(Home);
        }

        public void CaptureExisting(GameObject screenRoot)
        {
            if (screenRoot == null) return;
            root = screenRoot;
            panel = FindChild(root.transform, "Win Panel") as RectTransform;
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            Text foundTitle = FindText(texts, "Win Title", "Title");
            Text foundReward = FindText(texts, "Reward");
            Text foundBest = FindText(texts, "Best");
            if (foundTitle != null) titleText = foundTitle;
            if (foundReward != null) rewardText = foundReward;
            if (foundBest != null) bestText = foundBest;
            Text foundUnlock = FindText(texts, "Unlock");
            if (foundUnlock != null) unlockText = foundUnlock;
            Transform unlockTransform = FindChild(root.transform, "Unlocked Cat");
            if (unlockTransform != null) unlockImage = unlockTransform.GetComponent<Image>();
            Transform starRoot = FindChild(root.transform, "Earned Stars");
            if (starRoot != null) stars = starRoot.GetComponentsInChildren<Image>(true);
            Button foundNext = FindButton(root.transform, "Next Level");
            if (foundNext != null) nextButton = foundNext;
            BindButtons();
        }

        public void Show(string title, int starCount, int reward, int bestStars, int bestCombo)
        {
            if (root == null) return;
            if (titleText != null) titleText.text = title;
            if (rewardText != null) rewardText.text = reward > 0 ? "+" + reward + " coins • first clear" : "First-clear reward already claimed";
            if (bestText != null)
            {
                bestText.text = "Best: " + Mathf.Clamp(bestStars, 0, 3) + " stars";
                if (bestCombo >= 3) bestText.text += "  |  Combo " + bestCombo + "x";
            }
            for (int i = 0; i < stars.Length; i++) if (stars[i] != null) stars[i].enabled = i < starCount;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            root.transform.SetAsLastSibling();
            if (panel != null)
            {
                StopAllCoroutines();
                StartCoroutine(PopPanel());
            }
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        public void SetUnlock(Sprite sprite, string message)
        {
            bool visible = sprite != null && !string.IsNullOrWhiteSpace(message);
            if (unlockImage != null)
            {
                unlockImage.sprite = sprite;
                unlockImage.gameObject.SetActive(visible);
            }
            if (unlockText != null)
            {
                unlockText.text = visible ? message : string.Empty;
                unlockText.gameObject.SetActive(visible);
            }
        }

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

        private void BindButtons()
        {
            BindIfEmpty(nextButton, Next);
            BindIfEmpty(restartButton, Restart);
            BindIfEmpty(homeButton, Home);
        }

        private static void BindIfEmpty(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (button.onClick.GetPersistentEventCount() == 0) button.onClick.AddListener(action);
        }

        private void Next() => GameSystem.Instance?.NextLevel();
        private void Restart() => GameSystem.Instance?.RestartLevel();
        private void Home() => GameSystem.Instance?.GoHome();

        private static Text FindText(Text[] texts, params string[] names)
        {
            for (int n = 0; n < names.Length; n++)
                for (int i = 0; i < texts.Length; i++)
                    if (texts[i].name.IndexOf(names[n], System.StringComparison.OrdinalIgnoreCase) >= 0) return texts[i];
            return null;
        }

        private static Button FindButton(Transform rootTransform, string name)
        {
            Transform found = FindChild(rootTransform, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
