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

        public bool HasGameplayController => gameplayController != null;
        public bool IsSettingsOpen => settingsMenu != null && settingsMenu.IsOpen;

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
            if (pauseMenu == null)
            {
                pauseMenu = GetComponent<PauseMenu>();
                if (pauseMenu == null) pauseMenu = gameObject.AddComponent<PauseMenu>();
            }
            if (settingsMenu == null)
            {
                settingsMenu = GetComponent<SettingsMenu>();
                if (settingsMenu == null) settingsMenu = gameObject.AddComponent<SettingsMenu>();
            }
            if (levelComplete == null)
            {
                levelComplete = GetComponent<LevelCompleteScreen>();
                if (levelComplete == null) levelComplete = gameObject.AddComponent<LevelCompleteScreen>();
            }
            if (levelFail == null)
            {
                levelFail = GetComponent<LevelFailScreen>();
                if (levelFail == null) levelFail = gameObject.AddComponent<LevelFailScreen>();
            }
            if (gameplayController == null) gameplayController = FindFirstObjectByType<CatBlockPuzzleGame>(FindObjectsInactive.Include);
            SoundManager.EnsureInstance();
        }

        private void Start()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                pauseMenu.EnsureRuntimeView(canvas.transform);
                settingsMenu.EnsureRuntimeView(canvas.transform);
            }

            levelComplete?.EnsureBindings();
            levelFail?.EnsureBindings();
            home?.EnsureBindings();
        }

        public void OpenPause()
        {
            if (pauseMenu == null) return;
            gameplayController?.SetSystemPaused(true);
            pauseMenu.Show(gameplayController != null ? gameplayController.CurrentLevelNumber : 1,
                gameplayController != null ? gameplayController.CurrentStarCount : 0,
                gameplayController != null ? gameplayController.CurrentCoins : 0);
        }

        public void ResumeGame()
        {
            settingsMenu?.Hide();
            pauseMenu?.Hide();
            Time.timeScale = 1f;
            gameplayController?.SetSystemPaused(false);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            pauseMenu?.Hide();
            settingsMenu?.Hide();
            levelComplete?.Hide();
            levelFail?.Hide();
            if (gameplayController != null) gameplayController.RestartCurrentLevel();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;
            levelComplete?.Hide();
            gameplayController?.LoadNextLevelFromSystem();
        }

        public void GoHome()
        {
            Time.timeScale = 1f;
            pauseMenu?.Hide();
            settingsMenu?.Hide();
            levelComplete?.Hide();
            levelFail?.Hide();
            if (gameplayController != null) gameplayController.ShowHomeFromSystem();
            home?.Show();
        }

        public void OpenSettings(bool fromPause)
        {
            if (fromPause) pauseMenu?.Hide();
            settingsMenu?.Show(fromPause);
        }

        public void ReturnToPause()
        {
            if (gameplayController != null) OpenPause();
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

    }
}
