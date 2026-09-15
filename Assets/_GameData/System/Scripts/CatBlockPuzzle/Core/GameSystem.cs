using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatBlockPuzzle
{
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(HomeController))]
    public sealed class GameSystem : MonoBehaviour
    {
        public static GameSystem Instance { get; private set; }

        [Header("Gameplay")]
        [SerializeField] private CatBlockPuzzleGame gameplayController;
        [Header("Screens")]
        [SerializeField] private HomeController home;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private SettingsMenu settingsMenu;
        [SerializeField] private LevelCompleteScreen levelComplete;
        [SerializeField] private LevelFailScreen levelFail;
        [SerializeField] private ReferenceUiNavigation referenceUi;
        [SerializeField] private NoInternetScreen noInternet;

        private bool systemPaused;
        private float timeScaleBeforePause = 1f;

        public bool HasGameplayController => gameplayController != null && gameplayController.isActiveAndEnabled;
        public bool IsSettingsOpen => settingsMenu != null && settingsMenu.IsOpen;
        public bool HasReferenceUi => referenceUi != null;
        public bool IsPaused => systemPaused;
        public int CurrentCoins => gameplayController != null ? gameplayController.CurrentCoins : 0;
        public bool IsGameplayOpen => referenceUi == null || referenceUi.IsGameplayOpen;
        public void ShowGameplayPage() => referenceUi?.ShowGameplay();
        public void ShowRoomsPage() => referenceUi?.ShowRooms();
        public void OpenRooms() => gameplayController?.ShowHomeFromSystem();
        public bool StartLevel(int index)
        {
            if (!HasGameplayController || !gameplayController.IsLevelAvailable(index)) return false;
            ResumeGame();
            gameplayController.StartSelectedLevel(index);
            return true;
        }

        public static GameSystem EnsureForScene(CatBlockPuzzleGame gameplay)
        {
            if (Instance != null)
            {
                Instance.gameplayController = gameplay;
                return Instance;
            }

            GameSystem existing = FindFirstObjectByType<GameSystem>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameplayController = gameplay;
                return existing;
            }

            GameObject systemObject = new GameObject("Game System");
            HomeController homeController = systemObject.AddComponent<HomeController>();
            GameSystem system = systemObject.AddComponent<GameSystem>();
            system.home = homeController;
            system.gameplayController = gameplay;
            return system;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (home == null) home = GetComponent<HomeController>();
            SoundManager.EnsureInstance();
        }

        private void Start()
        {
            pauseMenu?.EnsureBindings();
            settingsMenu?.EnsureBindings();
            levelComplete?.EnsureBindings();
            levelFail?.EnsureBindings();
            home?.EnsureBindings();
        }

        public void OpenPause()
        {
            if (pauseMenu == null || !pauseMenu.IsConfigured) return;
            if (referenceUi != null && !referenceUi.IsGameplayOpen) return;
            if (!systemPaused)
            {
                timeScaleBeforePause = Time.timeScale;
                systemPaused = true;
                if (HasGameplayController) gameplayController.SetSystemPaused(true);
                Time.timeScale = 0f;
            }
            pauseMenu.Show(gameplayController != null ? gameplayController.CurrentLevelNumber : 1,
                gameplayController != null ? gameplayController.CurrentStarCount : 0,
                gameplayController != null ? gameplayController.CurrentCoins : 0);
        }

        public void ResumeGame()
        {
            settingsMenu?.Hide();
            pauseMenu?.Hide();
            if (!systemPaused) return;
            Time.timeScale = timeScaleBeforePause;
            systemPaused = false;
            if (HasGameplayController) gameplayController.SetSystemPaused(false);
        }

        public void StartGameplay()
        {
            if (HasReferenceUi) StartLevel(gameplayController != null ? gameplayController.RecommendedLevelIndex : 0);
            else { home?.Hide(); ResumeGame(); }
        }

        public void RestartLevel()
        {
            systemPaused = false;
            Time.timeScale = 1f;
            pauseMenu?.Hide();
            settingsMenu?.Hide();
            levelComplete?.Hide();
            levelFail?.Hide();
            if (HasGameplayController) gameplayController.RestartCurrentLevel();
            else if (referenceUi != null) referenceUi.ShowGameplay();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void NextLevel()
        {
            systemPaused = false;
            Time.timeScale = 1f;
            levelComplete?.Hide();
            if (HasGameplayController) gameplayController.LoadNextLevelFromSystem();
            else referenceUi?.ShowGameplay();
        }

        public void GoHome()
        {
            systemPaused = false;
            Time.timeScale = 1f;
            pauseMenu?.Hide();
            settingsMenu?.Hide();
            levelComplete?.Hide();
            levelFail?.Hide();
            noInternet?.Hide();
            if (referenceUi != null) { gameplayController?.SuspendForNavigation(); referenceUi.ShowHome(); }
            else
            {
                if (HasGameplayController) gameplayController.ShowHomeFromSystem();
                home?.Show();
            }
        }

        public void OpenSettings(bool fromPause)
        {
            if (settingsMenu == null || !settingsMenu.IsConfigured) return;
            if (fromPause) pauseMenu?.Hide();
            settingsMenu?.Show(fromPause);
        }

        public void ReturnToPause()
        {
            if (systemPaused) OpenPause();
        }

        public bool ShowLevelComplete(string title, int stars, int reward, int bestStars, int bestCombo)
        {
            if (levelComplete == null || !levelComplete.IsConfigured) return false;
            levelComplete.Show(title, stars, reward, bestStars, bestCombo);
            return true;
        }

        public bool ShowLevelFailed(string title, string message)
        {
            if (levelFail == null || !levelFail.IsConfigured) return false;
            levelFail.Show(title, message);
            return true;
        }

        public void ApplyHapticsPreference()
        {
            gameplayController?.SetHapticsFromSystem(SettingsMenu.HapticsEnabled);
        }

        public void CloseSettings()
        {
            settingsMenu?.Hide();
        }

        public void ShowNoInternet() => noInternet?.Show();

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (systemPaused) Time.timeScale = timeScaleBeforePause;
            Instance = null;
        }

    }
}
