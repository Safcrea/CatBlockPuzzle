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
        [Header("Power Up Purchases")]
        [Tooltip("Leave unassigned to use Resources/CatBlockPuzzle/PowerUpShopConfig.")]
        [SerializeField] private PowerUpShopConfig powerUpShopConfig;
        public PowerUpShopConfig PowerUpShop
        {
            get
            {
                if (powerUpShopConfig == null) powerUpShopConfig = PowerUpShopConfig.Load();
                return powerUpShopConfig;
            }
        }
        [Header("Screens")]
        [SerializeField] private HomeController home;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private SettingsMenu settingsMenu;
        [SerializeField] private LevelCompleteScreen levelComplete;
        [SerializeField] private LevelFailScreen levelFail;
        [SerializeField] private ReferenceUiNavigation referenceUi;
        [SerializeField] private NoInternetScreen noInternet;
        [Header("Daily Puzzle")]
        [Tooltip("The Daily Puzzle button opens once this level has been won. 0 = open from the start.")]
        [SerializeField, Min(0)] private int dailyPuzzleUnlockAfterLevel = 10;
        [Header("Testing")]
        [Tooltip("Testing only: every level and chapter is playable, ignoring the star gates. Untick for the normal flow. A build made with this ticked logs a warning.")]
        [SerializeField] private bool unlockAllLevels;
        [Tooltip("Testing only: shows Previous / Next / Go-to-level buttons on the gameplay screen, in the Editor and in builds. Levels open as previews: no stars, coins or saved progress. A build made with this ticked logs a warning.")]
        [SerializeField] private bool showLevelTestButtons;
        [SerializeField] private LevelTestNavigator levelTestButtons;

        private bool systemPaused;
        private float timeScaleBeforePause = 1f;

        public bool HasGameplayController => gameplayController != null && gameplayController.isActiveAndEnabled;
        public bool IsSettingsOpen => settingsMenu != null && settingsMenu.IsOpen;
        public bool HasReferenceUi => referenceUi != null;
        public bool IsPaused => systemPaused;
        public bool UnlockAllLevels => unlockAllLevels;
        public int DailyPuzzleUnlockAfterLevel => dailyPuzzleUnlockAfterLevel;
        private void OnValidate() => DailyPuzzle.UnlockAfterLevel = dailyPuzzleUnlockAfterLevel;
        public bool ShowLevelTestButtons => showLevelTestButtons;
        public int LevelCount => gameplayController != null ? gameplayController.LevelCount : 0;
        public int CurrentCoins => gameplayController != null ? gameplayController.CurrentCoins : 0;
        public int CurrentLevelNumber => gameplayController != null ? gameplayController.CurrentLevelNumber : 1;
        /// <summary>The number the player sees (keeps counting past the authored levels).</summary>
        public int DisplayLevelNumber => gameplayController != null ? gameplayController.DisplayLevelNumber : 1;
        /// <summary>The furthest level the player has reached (the next one to play), used to keep power-ups unlocked on replays.</summary>
        public int HighestLevelReached => gameplayController != null ? gameplayController.RecommendedLevelNumber : 1;

        // Button helpers for browsing levels without awarding progress or unlocking chapters.
        public void LoadNextLevel() => LoadAdjacentLevel(1);
        public void LoadPreviousLevel() => LoadAdjacentLevel(-1);
        private void LoadAdjacentLevel(int direction)
        {
            if (!HasGameplayController) return;
            int index = gameplayController.CurrentLevelNumber - 1 + direction;
            if (index < 0 || index >= gameplayController.LevelCount) return;
            PreviewLevel(index);
        }

        /// <summary>Opens a level for testing (zero-based) without awarding progress.</summary>
        public void PreviewLevel(int zeroBasedIndex)
        {
            if (!HasGameplayController) return;
            ResumeGame();
            ShowGameplayPage();
            gameplayController.PreviewLevelForTesting(Mathf.Clamp(zeroBasedIndex, 0, gameplayController.LevelCount - 1));
        }
        /// <summary>The Daily Puzzle opens once level 20 is won (or every level is unlocked for testing).</summary>
        public bool IsDailyPuzzleUnlocked => gameplayController != null && (unlockAllLevels || gameplayController.IsDailyPuzzleUnlocked);

        public void StartDailyPuzzle()
        {
            if (gameplayController == null || gameplayController.LevelCount <= 0 || !IsDailyPuzzleUnlocked) return;
            ResumeGame();
            ShowGameplayPage();
            gameplayController.StartDailyPuzzle();
        }

        public bool AwardCoins(int amount)
        {
            if (gameplayController == null || amount <= 0) return false;
            gameplayController.AwardExternalCoins(amount);
            ButtonSfx.PlayCoin();
            return true;
        }
        public int GetPowerUpCount(PowerUpKind kind) => PowerUpInventory.GetCount(kind);
        public bool CanRequestPowerUp(PowerUpKind kind) => HasGameplayController && gameplayController.CanRequestPowerUp(kind);
        public bool TryUseFreezePowerUp() => UseFreezePowerUp(fromTutorial: false);
        public bool TryUseHintPowerUp() => UseHintPowerUp(fromTutorial: false);
        public bool TryUseRotatePowerUp() => UseRotatePowerUp(fromTutorial: false);
        public bool IsRotateActive => HasGameplayController && gameplayController.IsRotateActive;
        /// <summary>Spends the power-up the unlock tutorial asked the player to tap.</summary>
        internal bool UseTaughtPowerUp(PowerUpKind kind) =>
            kind == PowerUpKind.Hint ? UseHintPowerUp(true) : kind == PowerUpKind.Rotate ? UseRotatePowerUp(true) : UseFreezePowerUp(true);

        private bool UseRotatePowerUp(bool fromTutorial)
        {
            if (!HasGameplayController || !gameplayController.CanRequestPowerUp(PowerUpKind.Rotate)) return false;
            if (PowerUpInventory.GetCount(PowerUpKind.Rotate) <= 0)
            {
                gameplayController.OpenPowerUpPurchase(PowerUpKind.Rotate);
                return false;
            }
            if (!gameplayController.TryUseRotatePowerUp()) return false;
            bool consumed = PowerUpInventory.TryConsume(PowerUpKind.Rotate);
            if (consumed)
            {
                gameplayController.RefreshPowerUpHud();
                gameplayController.ReportPowerUpUsed(PowerUpKind.Rotate, 1, fromTutorial);
            }
            return consumed;
        }

        private bool UseFreezePowerUp(bool fromTutorial)
        {
            if (!HasGameplayController || !gameplayController.CanRequestPowerUp(PowerUpKind.Freeze)) return false;
            if (PowerUpInventory.GetCount(PowerUpKind.Freeze) <= 0)
            {
                gameplayController.OpenPowerUpPurchase(PowerUpKind.Freeze);
                return false;
            }
            if (!gameplayController.TryUseFreezePowerUp()) return false;
            bool consumed = PowerUpInventory.TryConsume(PowerUpKind.Freeze);
            if (consumed)
            {
                gameplayController.RefreshPowerUpHud();
                gameplayController.ReportPowerUpUsed(PowerUpKind.Freeze, 1, fromTutorial);
            }
            return consumed;
        }

        private bool UseHintPowerUp(bool fromTutorial)
        {
            if (!HasGameplayController || !gameplayController.CanRequestPowerUp(PowerUpKind.Hint)) return false;
            int cost = gameplayController.HintCost;
            if (PowerUpInventory.GetCount(PowerUpKind.Hint) < cost)
            {
                gameplayController.OpenPowerUpPurchase(PowerUpKind.Hint);
                return false;
            }
            if (!gameplayController.TryUseHintPowerUp()) return false;
            bool consumed = PowerUpInventory.TryConsume(PowerUpKind.Hint, cost);
            if (consumed)
            {
                gameplayController.RefreshPowerUpHud();
                gameplayController.ReportPowerUpUsed(PowerUpKind.Hint, cost, fromTutorial);
            }
            return consumed;
        }
        public void RefreshPowerUpHud() => gameplayController?.RefreshPowerUpHud();
        public PowerUpPurchaseStatus PurchasePowerUp(PowerUpKind kind) => HasGameplayController && PowerUpShop != null
            ? gameplayController.PurchasePowerUp(kind, PowerUpShop.GetOffer(kind)) : PowerUpPurchaseStatus.Unavailable;
        public PowerUpPurchaseStatus PurchaseStarterBundle() => HasGameplayController && PowerUpShop != null
            ? gameplayController.PurchaseBundleFromShop(PowerUpShop.StarterBundle) : PowerUpPurchaseStatus.Unavailable;
        public bool GrantShopCoins(int coins) => HasGameplayController && gameplayController.GrantCoinsFromShop(coins);
        public PowerUpPurchaseStatus PurchasePowerUpFromShop(PowerUpKind kind) => HasGameplayController && PowerUpShop != null
            ? gameplayController.PurchasePowerUpFromShop(kind, PowerUpShop.GetOffer(kind)) : PowerUpPurchaseStatus.Unavailable;
        internal void SetPowerUpPurchaseOpen(bool open, bool resumeGameplay = true) => gameplayController?.SetPowerUpPurchaseOpen(open, resumeGameplay);
        public bool IsGameplayOpen => referenceUi == null || referenceUi.IsGameplayOpen;
        public void ShowGameplayPage() => referenceUi?.ShowGameplay();
        public void ShowRoomsPage() => referenceUi?.ShowRooms();

        /// <summary>Leaves gameplay for the level selection page (Home when the scene has no reference UI).</summary>
        public void ShowLevelSelection()
        {
            if (referenceUi == null) { GoHome(); return; }
            systemPaused = false;
            Time.timeScale = 1f;
            levelComplete?.Hide();
            levelFail?.Hide();
            gameplayController?.SuspendForNavigation();
            referenceUi.ShowLevels();
        }
        public void OpenRooms()
        {
            // Rooms are unavailable in the reference-menu release. Do not fall back
            // to the old meta hub when the feature is intentionally disabled.
            if (referenceUi != null)
            {
                referenceUi.ShowRooms();
                return;
            }

            gameplayController?.ShowHomeFromSystem();
        }
        /// <summary>Starts a 1-based level number; loop levels past the authored ones are always open.</summary>
        public bool StartLevelNumber(int levelNumber)
        {
            if (!HasGameplayController) return false;
            if (levelNumber <= gameplayController.LevelCount) return StartLevel(levelNumber - 1);
            ResumeGame();
            gameplayController.StartLevelNumber(levelNumber);
            return true;
        }

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
            DailyPuzzle.UnlockAfterLevel = dailyPuzzleUnlockAfterLevel;
            Application.targetFrameRate = 60;
            if (home == null) home = GetComponent<HomeController>();
            if (levelTestButtons != null) levelTestButtons.gameObject.SetActive(showLevelTestButtons);
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
            if (gameplayController != null && (gameplayController.IsPowerUpTutorialOpen || gameplayController.IsPowerUpPurchaseOpen)) return;
            if (pauseMenu == null || !pauseMenu.IsConfigured) return;
            if (referenceUi != null && !referenceUi.IsGameplayOpen) return;
            if (!systemPaused)
            {
                timeScaleBeforePause = Time.timeScale;
                systemPaused = true;
                if (HasGameplayController) gameplayController.SetSystemPaused(true);
                Time.timeScale = 0f;
                GameAnalytics.PauseOpened(gameplayController != null ? gameplayController.TimeLeftSeconds : 0f);
            }
            pauseMenu.Show(gameplayController != null ? gameplayController.DisplayLevelNumber : 1,
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
            GameAnalytics.SetLevelStartSource(GameAnalytics.SourceHomePlay);
            if (HasReferenceUi) StartLevelNumber(gameplayController != null ? gameplayController.RecommendedLevelNumber : 1);
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
            GameAnalytics.SetLevelStartSource(GameAnalytics.SourceNext);
            if (HasGameplayController) gameplayController.LoadNextLevelFromSystem();
            else referenceUi?.ShowGameplay();
        }

        public void SkipLevel()
        {
            if (!HasGameplayController) return;
            ResumeGame();
            gameplayController.SkipCurrentLevel();
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
            if (gameplayController != null && (gameplayController.IsPowerUpTutorialOpen || gameplayController.IsPowerUpPurchaseOpen)) return;
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
