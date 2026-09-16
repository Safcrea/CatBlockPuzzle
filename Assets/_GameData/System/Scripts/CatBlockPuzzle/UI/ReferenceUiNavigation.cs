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
        [Header("Existing Controls")]
        [SerializeField] private Button levelBackButton;
        [SerializeField] private Button levelPlayButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button[] auxiliaryBackButtons;

        public bool IsGameplayOpen => gameplayScreen != null && gameplayScreen.activeInHierarchy;
        public bool HasCollection => collectionScreen != null;
        public bool HasRooms => roomsScreen != null;

        private void Start()
        {
            UpdateListeners(true);
            ShowHome();
            // Run after every Start method has established the main-menu page.
            Invoke(nameof(ShowAvailableDailyReward), 0f);
        }
        private void OnDestroy() => UpdateListeners(false);

        public void ShowHome()
        {
            HidePages();
            home?.Show();
        }

        public void ShowShop() { if (shop != null) { HidePages(); shop.Show(); } }
        public void ShowDailyReward() { if (dailyReward != null) { HidePages(); dailyReward.Show(); } }
        public void ShowLevels() => ShowPage(levelSelectionScreen);
        public void ShowGameplay() => ShowPage(gameplayScreen);
        public void ShowCollection() => ShowPage(collectionScreen);
        public void ShowRooms() => ShowPage(roomsScreen);

        private void ShowAvailableDailyReward()
        {
            if (dailyReward != null && dailyReward.IsClaimAvailable) ShowDailyReward();
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
