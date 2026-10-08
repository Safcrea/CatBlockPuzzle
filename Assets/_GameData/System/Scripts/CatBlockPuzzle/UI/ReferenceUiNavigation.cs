using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>Routes authored menu pages. Puzzle and commerce logic stay in their own systems.</summary>
    [DisallowMultipleComponent]
    public sealed class ReferenceUiNavigation : MonoBehaviour
    {
        [SerializeField] private HomeController home;
        [SerializeField] private ShopScreen shop;
        [SerializeField] private DailyRewardScreen dailyReward;
        [SerializeField] private GameObject levelSelectionScreen;
        [SerializeField] private GameObject gameplayScreen;
        [SerializeField] private GameObject collectionScreen;
        [SerializeField] private GameObject roomsScreen;
        [Header("Coming Soon")]
        [Tooltip("Shared popup shown while Rooms or Collections is unavailable.")]
        [SerializeField] private ComingSoonPopup comingSoonPrefab;
        [SerializeField] private bool collectionEnabled;
        [SerializeField] private bool roomsEnabled;
        [Header("Existing Controls")]
        [SerializeField] private Button levelBackButton;
        [SerializeField] private Button levelPlayButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button[] auxiliaryBackButtons;
        private ComingSoonPopup comingSoonInstance;
        private LevelSelectionScreen levelSelection;
        [Header("Startup Daily Reward")]
        [SerializeField] private bool showDailyRewardOnStartup = true;
        [SerializeField, Min(0f)] private float startupRewardDelay = 0.25f;
        private static bool dailyRewardShownThisSession;

        // Survives scene changes, but resets for each app launch or Editor Play session,
        // including when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDailyRewardSession() => dailyRewardShownThisSession = false;

        public bool IsGameplayOpen => gameplayScreen != null && gameplayScreen.activeInHierarchy;
        public bool HasCollection => collectionEnabled && collectionScreen != null;
        public bool HasRooms => roomsEnabled && roomsScreen != null;
        public bool HasUnreadDailyReward => dailyReward != null && dailyReward.HasUnreadReward;

        private void Start()
        {
            UpdateListeners(true);
            ShowHomeImmediately();
            GameAnalytics.LoadingComplete();
            // Run after every Start method has established the main-menu page.
            if (showDailyRewardOnStartup && !dailyRewardShownThisSession)
                Invoke(nameof(ShowStartupDailyReward), Mathf.Max(0f, startupRewardDelay));
        }
        private void OnDestroy() => UpdateListeners(false);

        public void ShowHome()
        {
            // Reveal Home behind the outgoing page, then clean up after the fade.
            if (dailyReward != null && dailyReward.IsOpen)
            {
                home?.Show();
                dailyReward.HideAnimated(ShowHomeImmediately);
                return;
            }
            if (shop != null && shop.IsOpen)
            {
                home?.Show();
                shop.HideAnimated(ShowHomeImmediately);
                return;
            }
            ShowHomeImmediately();
        }

        private void ShowHomeImmediately()
        {
            HidePages();
            home?.Show();
            GameAnalytics.ScreenView("home");
            UseMusic(MusicContext.Menu);
        }

        private static void UseMusic(MusicContext context) => SoundManager.EnsureInstance().SetMusicContext(context);

        public void ShowShop()
        {
            if (shop == null) return;
            HidePages();
            shop.Show();
            UseMusic(MusicContext.Menu);
            GameAnalytics.ScreenView("shop");
        }

        public void ShowDailyReward() => OpenDailyReward(autoOpened: false);

        private void OpenDailyReward(bool autoOpened)
        {
            if (dailyReward == null) return;
            HidePages();
            dailyReward.Show();
            UseMusic(MusicContext.Menu);
            GameAnalytics.ScreenView("daily_reward");
            GameAnalytics.DailyRewardShown(dailyReward.AvailableDay, autoOpened);
            if (dailyReward.IsOpen) dailyRewardShownThisSession = true;
        }
        public void ShowLevels()
        {
            LevelSelectionScreen levels = ResolveLevelSelection();
            UseMusic(MusicContext.Menu);
            GameAnalytics.ScreenView("levels");
            if (levels == null) { ShowPage(levelSelectionScreen); return; }
            HidePages();
            levels.Show();
        }

        /// <summary>The page root carries the screen component; kept optional so an older scene still works.</summary>
        private LevelSelectionScreen ResolveLevelSelection()
        {
            if (levelSelection == null && levelSelectionScreen != null)
                levelSelection = levelSelectionScreen.GetComponent<LevelSelectionScreen>();
            return levelSelection;
        }
        public void ShowGameplay()
        {
            ShowPage(gameplayScreen);
            UseMusic(MusicContext.Gameplay);
            GameAnalytics.ScreenView("gameplay");
        }
        public void ShowCollection()
        {
            if (!HasCollection) { ShowComingSoon("Collection"); return; }
            ShowPage(collectionScreen);
            GameAnalytics.ScreenView("collection");
        }

        public void ShowRooms()
        {
            if (!HasRooms) { ShowComingSoon("Rooms"); return; }
            ShowPage(roomsScreen);
            GameAnalytics.ScreenView("rooms");
        }

        private void ShowStartupDailyReward()
        {
            // Do not interrupt a player who has already left Home during the delay.
            if (!dailyRewardShownThisSession && home != null && home.IsOpen &&
                dailyReward != null && dailyReward.IsClaimAvailable)
                OpenDailyReward(autoOpened: true);
        }

        private void ShowComingSoon(string feature)
        {
            GameAnalytics.ComingSoonTap(feature.ToLowerInvariant());
            if (comingSoonInstance == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null && levelSelectionScreen != null)
                    canvas = levelSelectionScreen.GetComponentInParent<Canvas>();
                if (canvas == null) return;
                comingSoonInstance = comingSoonPrefab != null
                    ? Instantiate(comingSoonPrefab, canvas.transform)
                    : ComingSoonPopup.Create(canvas.transform);
                comingSoonInstance.name = "Coming Soon Popup";
            }
            comingSoonInstance.Show(feature);
        }

        private void ShowPage(GameObject page)
        {
            // An unimplemented destination must not hide the current screen.
            if (page == null) return;
            HidePages();
            page.SetActive(true);
        }

        private void HidePages()
        {
            if (comingSoonInstance != null) comingSoonInstance.HideImmediately();
            GameSystem.Instance?.CloseSettings();
            home?.Hide();
            shop?.Hide();
            dailyReward?.Hide();
            LevelSelectionScreen levels = ResolveLevelSelection();
            if (levels != null) levels.Hide();
            else if (levelSelectionScreen != null) levelSelectionScreen.SetActive(false);
            if (gameplayScreen != null) gameplayScreen.SetActive(false);
            if (collectionScreen != null) collectionScreen.SetActive(false);
            if (roomsScreen != null) roomsScreen.SetActive(false);
        }

        private void UpdateListeners(bool bind)
        {
            Listen(levelBackButton, Back, bind);
            Listen(levelPlayButton, Play, bind);
            Listen(pauseButton, Pause, bind);
            if (auxiliaryBackButtons != null)
                foreach (Button button in auxiliaryBackButtons) Listen(button, Back, bind);
        }

        private static void Listen(Button button, UnityAction action, bool bind)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (bind) button.onClick.AddListener(action);
        }

        private void Back() { ButtonSfx.PlayClick(); GameSystem.Instance?.GoHome(); }
        private void Play() { ButtonSfx.PlayClick(); GameSystem.Instance?.StartGameplay(); }
        private void Pause() { ButtonSfx.PlayClick(); GameSystem.Instance?.OpenPause(); }
    }
}
