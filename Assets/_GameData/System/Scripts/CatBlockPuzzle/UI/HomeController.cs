using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class HomeController : MonoBehaviour
    {
        [SerializeField] private GameObject homeScreen;
        [SerializeField] private Button playButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private int gameplayBuildIndex = 1;

        public void EnsureBindings() => CaptureExisting(homeScreen);

        public void CaptureExisting(GameObject screenRoot)
        {
            homeScreen = screenRoot;
            if (homeScreen == null) return;
            playButton = FindButton("Play");
            continueButton = FindButton("Continue");
            settingsButton = FindButton("Settings");
            quitButton = FindButton("Quit");
            BindButtons();
        }

        public void Show()
        {
            if (homeScreen != null) homeScreen.SetActive(true);
        }

        public void Hide()
        {
            if (homeScreen != null) homeScreen.SetActive(false);
        }

        public void Play()
        {
            Hide();
            if (GameSystem.Instance != null && GameSystem.Instance.HasGameplayController)
            {
                GameSystem.Instance.ResumeGame();
            }
            else
            {
                LoadingSceneController.LoadScene(gameplayBuildIndex);
            }
        }

        public void Continue() => Play();
        public void OpenSettings() => GameSystem.Instance?.OpenSettings(false);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BindButtons()
        {
            BindIfEmpty(playButton, Play);
            BindIfEmpty(continueButton, Continue);
            BindIfEmpty(settingsButton, OpenSettings);
            BindIfEmpty(quitButton, Quit);
        }

        private Button FindButton(string name)
        {
            foreach (Button button in homeScreen.GetComponentsInChildren<Button>(true))
                if (button.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) return button;
            return null;
        }

        private static void BindIfEmpty(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null && button.onClick.GetPersistentEventCount() == 0) button.onClick.AddListener(action);
        }
    }
}
