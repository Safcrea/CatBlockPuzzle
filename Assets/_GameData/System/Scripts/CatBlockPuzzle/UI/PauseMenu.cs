using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] private GameObject root;
        [System.NonSerialized] private Text levelText;
        [System.NonSerialized] private Text starsText;
        [System.NonSerialized] private Text coinsText;
        [Header("Buttons")]
        [System.NonSerialized] private Button closeButton;
        [System.NonSerialized] private Button resumeButton;
        [System.NonSerialized] private Button restartButton;
        [System.NonSerialized] private Button homeButton;
        [System.NonSerialized] private Button settingsButton;

        public bool IsOpen => root != null && root.activeSelf;

        public void EnsureRuntimeView(Transform canvas)
        {
            if (resumeButton != null || canvas == null)
            {
                BindButtons();
                return;
            }

            RectTransform overlay = root != null ? root.GetComponent<RectTransform>() : null;
            if (overlay == null)
            {
                overlay = RuntimeUiFactory.CreateOverlay(canvas, "Pause Screen");
                root = overlay.gameObject;
            }
            else
            {
                RuntimeUiFactory.PrepareOverlay(overlay);
            }

            RectTransform panel = RuntimeUiFactory.CreatePanel(overlay, "Pause Panel", new Vector2(650f, 980f));
            Text title = RuntimeUiFactory.CreateText(panel, "Title", "PAUSED", 78, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0f, 355f), new Vector2(560f, 105f));
            levelText = RuntimeUiFactory.CreateText(panel, "Level", "Level", 34, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(levelText.rectTransform, new Vector2(0f, 278f), new Vector2(520f, 52f));
            starsText = RuntimeUiFactory.CreateText(panel, "Stars", "★ ★ ★", 52, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(starsText.rectTransform, new Vector2(0f, 198f), new Vector2(500f, 70f));
            coinsText = RuntimeUiFactory.CreateText(panel, "Coins", "0 coins", 34, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(coinsText.rectTransform, new Vector2(0f, 125f), new Vector2(500f, 52f));

            resumeButton = RuntimeUiFactory.CreateButton(panel, "Resume", "▶  Resume", new Vector2(0f, 22f), new Vector2(500f, 100f), RuntimeUiFactory.Coral);
            restartButton = RuntimeUiFactory.CreateButton(panel, "Restart", "↻  Restart", new Vector2(0f, -102f), new Vector2(500f, 90f), Color.white);
            homeButton = RuntimeUiFactory.CreateButton(panel, "Home", "⌂  Home", new Vector2(0f, -214f), new Vector2(500f, 90f), Color.white);
            settingsButton = RuntimeUiFactory.CreateButton(panel, "Settings", "⚙  Settings", new Vector2(0f, -326f), new Vector2(500f, 90f), Color.white);
            closeButton = RuntimeUiFactory.CreateButton(panel, "Close", "×", new Vector2(270f, 423f), new Vector2(70f, 70f), Color.white);
            root.SetActive(false);
            BindButtons();
        }

        public void Show(int levelNumber, int stars, int coins)
        {
            if (root == null) return;
            if (levelText != null) levelText.text = "Level " + Mathf.Max(1, levelNumber);
            if (starsText != null) starsText.text = new string('★', Mathf.Clamp(stars, 0, 3)) + new string('☆', 3 - Mathf.Clamp(stars, 0, 3));
            if (coinsText != null) coinsText.text = Mathf.Max(0, coins) + " coins";
            root.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void BindButtons()
        {
            Bind(closeButton, Resume);
            Bind(resumeButton, Resume);
            Bind(restartButton, Restart);
            Bind(homeButton, Home);
            Bind(settingsButton, Settings);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void Resume() => GameSystem.Instance?.ResumeGame();
        private void Restart() => GameSystem.Instance?.RestartLevel();
        private void Home() => GameSystem.Instance?.GoHome();
        private void Settings() => GameSystem.Instance?.OpenSettings(true);
    }
}
