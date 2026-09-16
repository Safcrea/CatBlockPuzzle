using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class HomeController : MonoBehaviour
    {
        [SerializeField] private GameObject homeScreen;
        [SerializeField] private Text coinText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [Header("Menu Navigation")]
        [SerializeField] private ReferenceUiNavigation navigation;
        [SerializeField] private Button levelsButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button dailyRewardButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button roomsButton;
        [SerializeField] private int gameplayBuildIndex = 1;

        private void OnEnable() => EnsureBindings();
        private void OnDestroy() => UpdateListeners(false);
        public void EnsureBindings() => UpdateListeners(true);

        // Editor authoring can explicitly replace the root without runtime name searches.
        public void CaptureExisting(GameObject screenRoot) { homeScreen = screenRoot; EnsureBindings(); }
        public void Show()
        {
            if (coinText != null) coinText.text = (GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0).ToString();
            if (homeScreen != null) homeScreen.SetActive(true);
        }
        public void Hide() { if (homeScreen != null) homeScreen.SetActive(false); }

        public void Play()
        {
            if (GameSystem.Instance != null) GameSystem.Instance.StartGameplay();
            else LoadingSceneController.LoadScene(gameplayBuildIndex);
        }

        public void Continue() => Play();
        public void OpenSettings() => GameSystem.Instance?.OpenSettings(false);
        private void OpenLevels() => navigation?.ShowLevels();
        private void OpenShop() => navigation?.ShowShop();
        private void OpenDailyReward() => navigation?.ShowDailyReward();
        private void OpenCollection() => navigation?.ShowCollection();
        private void OpenRooms() => GameSystem.Instance?.OpenRooms();

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void UpdateListeners(bool bind)
        {
            Listen(playButton, Play, bind);
            Listen(continueButton, Continue, bind);
            Listen(settingsButton, OpenSettings, bind);
            Listen(quitButton, Quit, bind);
            Listen(levelsButton, OpenLevels, bind);
            Listen(shopButton, OpenShop, bind);
            Listen(dailyRewardButton, OpenDailyReward, bind);
            Listen(collectionButton, OpenCollection, bind);
            Listen(roomsButton, OpenRooms, bind);
            UpdateComingSoonVisibility();
        }

        private void UpdateComingSoonVisibility()
        {
            if (collectionButton != null) collectionButton.gameObject.SetActive(true);
            if (roomsButton != null) roomsButton.gameObject.SetActive(true);
        }

        private static void Listen(Button button, UnityAction action, bool bind)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (bind) button.onClick.AddListener(action);
        }
    }
}
