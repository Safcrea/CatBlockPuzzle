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
        [Header("Startup Daily Reward")]
        [SerializeField] private bool showDailyRewardOnStartup = true;
        [SerializeField, Min(0f)] private float startupRewardDelay = 0.25f;

        public bool IsGameplayOpen => gameplayScreen != null && gameplayScreen.activeInHierarchy;
        public bool HasCollection => collectionEnabled && collectionScreen != null;
        public bool HasRooms => roomsEnabled && roomsScreen != null;
        public bool HasUnreadDailyReward => dailyReward != null && dailyReward.HasUnreadReward;

        private void Start()
        {
            UpdateListeners(true);
            ShowHomeImmediately();
            // Run after every Start method has established the main-menu page.
            if (showDailyRewardOnStartup) Invoke(nameof(ShowStartupDailyReward), Mathf.Max(0f, startupRewardDelay));
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
        }

        public void ShowShop() { if (shop != null) { HidePages(); shop.Show(); } }
        public void ShowDailyReward() { if (dailyReward != null) { HidePages(); dailyReward.Show(); } }
        public void ShowLevels() => ShowPage(levelSelectionScreen);
        public void ShowGameplay() => ShowPage(gameplayScreen);
        public void ShowCollection()
        {
            if (!HasCollection) { ShowComingSoon("Collection"); return; }
            ShowPage(collectionScreen);
        }

        public void ShowRooms()
        {
            if (!HasRooms) { ShowComingSoon("Rooms"); return; }
            ShowPage(roomsScreen);
        }

        private void ShowStartupDailyReward()
        {
            // Do not interrupt a player who has already left Home during the delay.
            if (home != null && home.IsOpen && dailyReward != null) ShowDailyReward();
        }

        private void ShowComingSoon(string feature)
        {
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
            if (levelSelectionScreen != null) levelSelectionScreen.SetActive(false);
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

        private void Back() => GameSystem.Instance?.GoHome();
        private void Play() => GameSystem.Instance?.StartGameplay();
        private void Pause() => GameSystem.Instance?.OpenPause();
    }
}
