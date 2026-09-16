using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [Header("Authored Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button settingsButton;
        [Header("Optional Labels")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text starsText;
        [SerializeField] private Text coinsText;

        public bool IsConfigured => root != null;
        public bool IsOpen => root != null && root.activeInHierarchy;
        private void OnEnable() => EnsureBindings();
        private void OnDestroy() => UpdateListeners(false);
        public void EnsureBindings() => UpdateListeners(true);

        public void Show(int levelNumber, int stars, int coins)
        {
            if (root == null) return;
            if (levelText != null) levelText.text = "Level " + Mathf.Max(1, levelNumber);
            if (starsText != null) starsText.text = new string('★', Mathf.Clamp(stars, 0, 3)) + new string('☆', 3 - Mathf.Clamp(stars, 0, 3));
            if (coinsText != null) coinsText.text = Mathf.Max(0, coins).ToString();
            MenuTransition.Show(root);
        }

        public void Hide() => MenuTransition.Hide(root, null, false);

        private void UpdateListeners(bool bind)
        {
            Listen(closeButton, Resume, bind);
            Listen(resumeButton, Resume, bind);
            Listen(restartButton, Restart, bind);
            Listen(homeButton, Home, bind);
            Listen(settingsButton, Settings, bind);
        }

        private static void Listen(Button button, UnityAction action, bool bind)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (bind) button.onClick.AddListener(action);
        }

        private void Resume() => MenuTransition.Hide(root, () => GameSystem.Instance?.ResumeGame());
        private void Restart() => GameSystem.Instance?.RestartLevel();
        private void Home() => GameSystem.Instance?.GoHome();
        private void Settings() => GameSystem.Instance?.OpenSettings(true);
    }
}
