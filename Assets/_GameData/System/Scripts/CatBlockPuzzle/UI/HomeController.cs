using AVNEvents;
using System;
using TMPro;
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
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private Button coinPlusButton;
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
        [Tooltip("Optional. Plays the day's hard puzzle; opens once the level set on GameSystem (Daily Puzzle > Unlock After Level) is won.")]
        [SerializeField] private Button dailyPuzzleButton;
        [Tooltip("Legacy marker, no longer shown: a won puzzle is greyed out with a countdown instead.")]
        [SerializeField] private GameObject dailyPuzzleDoneMark;
        [Header("Daily Puzzle")]
        [Tooltip("Applied to the Daily Puzzle picture while today's puzzle is already won (uses the Cat Block Puzzle/UI Grayscale shader).")]
        [SerializeField] private Material greyscaleMaterial;
        [Tooltip("Countdown to the next puzzle, shown on the button while it is greyed out.")]
        [SerializeField] private TMP_Text dailyPuzzleTimer;
        [Tooltip("Optional. Spawned from Resources/CatBlockPuzzle/InfoPopup when empty.")]
        [SerializeField] private InfoPopup infoPopup;
        [SerializeField] private int gameplayBuildIndex = 1;
        [Header("Locked Features")]
        [Tooltip("Padlock drawn over menu buttons whose feature is still coming soon.")]
        [SerializeField] private Sprite lockSprite;
        [SerializeField, Range(0f, 1f)] private float lockedDim = 0.55f;
        [SerializeField] private Vector2 lockSize = new Vector2(52f, 60f);
        [Header("Menu Motion")]
        [SerializeField] private RectTransform dailyRewardDot;
        [Tooltip("Controls the Play button's idle pop and pressed scale. Can be adjusted during Play Mode.")]
        [SerializeField] private MenuAttentionPulse.Settings playPop = new MenuAttentionPulse.Settings();
        [Tooltip("Give every other menu button its own idle motion and press squash.")]
        [SerializeField] private bool animateMenuButtons = true;
        private DateTime notificationDate;
        public bool IsOpen => homeScreen != null && homeScreen.activeInHierarchy;

        private void OnEnable() => EnsureBindings();
        private void OnDestroy() => UpdateListeners(false);
        public void EnsureBindings() => UpdateListeners(true);

        // Editor authoring can explicitly replace the root without runtime name searches.
        public void CaptureExisting(GameObject screenRoot) { homeScreen = screenRoot; EnsureBindings(); }
        public void Show()
        {
            if (coinText != null) coinText.text = (GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0).ToString();
            if (coinLabel != null) coinLabel.text = (GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0).ToString();
            if (homeScreen != null) homeScreen.SetActive(true);
            if (playButton != null) MenuAttentionPulse.Configure(playButton.transform, playPop);
            ApplyMenuMotion();
            RefreshDailyRewardDot();
            RefreshDailyPuzzle();
        }

        // Each button gets a different idle so the menu feels alive without everything pulsing in step.
        private void ApplyMenuMotion()
        {
            if (!animateMenuButtons) return;
            Configure(levelsButton, MenuButtonMotion.Style.Sway, 0.0f);
            Configure(shopButton, MenuButtonMotion.Style.Hop, 0.6f);
            Configure(dailyRewardButton, MenuButtonMotion.Style.Wiggle, 1.3f);
            Configure(collectionButton, MenuButtonMotion.Style.Breathe, 0.3f);
            Configure(roomsButton, MenuButtonMotion.Style.Breathe, 1.0f);
            Configure(continueButton, MenuButtonMotion.Style.Hop, 1.8f);
            Configure(dailyPuzzleButton, MenuButtonMotion.Style.Sway, 2.2f);
            // The settings gear stays still; it is a utility control, not something to draw the eye.
            RemoveMotion(settingsButton);
        }

        private static void Configure(Button button, MenuButtonMotion.Style style, float startDelay)
        {
            if (button == null) return;
            MenuButtonMotion.Configure(button.transform, style, startDelay);
        }

        private static void RemoveMotion(Button button)
        {
            if (button == null) return;
            var motion = button.GetComponent<MenuButtonMotion>();
            if (motion != null) Destroy(motion);
        }
        public void Hide() { if (homeScreen != null) homeScreen.SetActive(false); }

        public void Play()
        {
            ButtonSfx.PlayClick();
            AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.play);
            if (GameSystem.Instance != null) GameSystem.Instance.StartGameplay();
            else LoadingSceneController.LoadScene(gameplayBuildIndex);
        }

        public void Continue() => Play();
        public void OpenSettings() { ButtonSfx.PlayClick(); AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.settings); GameSystem.Instance?.OpenSettings(false); }
        private void OpenLevels() { ButtonSfx.PlayClick(); AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.levels); navigation?.ShowLevels(); }
        private void OpenShop() { ButtonSfx.PlayClick(); AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.shop); navigation?.ShowShop(); }
        private void OpenDailyReward() { ButtonSfx.PlayClick(); AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.daily_gift); navigation?.ShowDailyReward(); }
        private void OpenDailyPuzzle()
        {
            AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.daily_puzzle);
            if (IsDailyPuzzleLocked) { ShowDailyPuzzleInfo(locked: true); return; }
            if (IsDailyPuzzleDone) { ShowDailyPuzzleInfo(locked: false); return; }
            ButtonSfx.PlayClick();
            GameSystem.Instance.StartDailyPuzzle();
        }

        // A few words and a picture: why the puzzle is closed and, if it is won, when the next one arrives.
        private void ShowDailyPuzzleInfo(bool locked)
        {
            ButtonSfx.PlayClick();
            if (infoPopup == null && homeScreen != null) infoPopup = InfoPopup.Create(homeScreen.transform.parent);
            if (infoPopup == null) { PlayLockedTap(); return; }
            Graphic graphic = dailyPuzzleButton != null ? dailyPuzzleButton.targetGraphic : null;
            Sprite picture = graphic is Image image ? image.sprite : null;
            if (locked)
                infoPopup.Show("Daily Puzzle", picture, () => "Unlocked after\nlevel " + DailyPuzzle.UnlockAfterLevel, greyscaleMaterial);
            else
                infoPopup.Show("Come back soon!", picture, () => "Next puzzle in\n" + DailyPuzzle.FormatCountdown(DailyPuzzle.TimeUntilNext(DateTime.UtcNow)), greyscaleMaterial);
        }

        private void OpenCollection()
        {
            AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.collection);
            if (IsCollectionLocked) { GameAnalytics.ComingSoonTap("collection"); PlayLockedTap(); return; }
            ButtonSfx.PlayClick();
            navigation?.ShowCollection();
        }

        private void OpenRooms()
        {
            AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.rooms);
            if (IsRoomsLocked) { GameAnalytics.ComingSoonTap("rooms"); PlayLockedTap(); return; }
            ButtonSfx.PlayClick();
            GameSystem.Instance?.OpenRooms();
        }

        private bool IsCollectionLocked => navigation == null || !navigation.HasCollection;
        private bool IsRoomsLocked => navigation == null || !navigation.HasRooms;
        private static bool IsDailyPuzzleLocked => GameSystem.Instance == null || !GameSystem.Instance.IsDailyPuzzleUnlocked;
        private static bool IsDailyPuzzleDone => DailyPuzzle.IsDoneToday(DateTime.UtcNow);
        private string shownTimer;

        // A locked button only answers with a tap in the hand: no popup, no click sound.
        private static void PlayLockedTap() => HapticsController.Current?.Play(HapticKind.UiTap);

        private void Update()
        {
            if (homeScreen != null && homeScreen.activeInHierarchy && notificationDate != DateTime.UtcNow.Date)
            {
                RefreshDailyRewardDot();
                RefreshDailyPuzzle();
            }
            else if (dailyPuzzleTimer != null && dailyPuzzleTimer.gameObject.activeInHierarchy) UpdateDailyPuzzleTimer();
        }

        // Padlocked until the unlock level is won; once today's puzzle is beaten the picture goes grey and counts down.
        private void RefreshDailyPuzzle()
        {
            if (dailyPuzzleButton == null) return;
            bool locked = IsDailyPuzzleLocked;
            bool done = !locked && IsDailyPuzzleDone;
            SetLocked(dailyPuzzleButton, locked);
            Graphic graphic = dailyPuzzleButton.targetGraphic != null ? dailyPuzzleButton.targetGraphic : dailyPuzzleButton.GetComponent<Graphic>();
            if (graphic != null) graphic.material = done ? greyscaleMaterial : null;
            if (dailyPuzzleDoneMark != null) dailyPuzzleDoneMark.SetActive(false);
            if (dailyPuzzleTimer != null)
            {
                dailyPuzzleTimer.gameObject.SetActive(done);
                if (done) UpdateDailyPuzzleTimer();
            }
        }

        private void UpdateDailyPuzzleTimer()
        {
            string text = DailyPuzzle.FormatCountdown(DailyPuzzle.TimeUntilNext(DateTime.UtcNow));
            if (text == shownTimer) return;
            shownTimer = text;
            dailyPuzzleTimer.text = text;
        }

        private void RefreshDailyRewardDot()
        {
            notificationDate = DateTime.UtcNow.Date;
            if (dailyRewardDot == null && dailyRewardButton != null)
                dailyRewardDot = dailyRewardButton.transform.Find("Badge") as RectTransform;
            if (dailyRewardDot == null) return;
            bool unread = navigation != null && navigation.HasUnreadDailyReward;
            dailyRewardDot.gameObject.SetActive(unread);
            if (unread) MenuAttentionPulse.Configure(dailyRewardDot, 0.22f, 1.9f);
        }

        public void Quit()
        {
            ButtonSfx.PlayClick();
            AvnEvents.ScreenInteraction(AvnScreen.main_menu, ScreenAction.quit);
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
            Listen(coinPlusButton, OpenShop, bind);
            Listen(dailyRewardButton, OpenDailyReward, bind);
            Listen(collectionButton, OpenCollection, bind);
            Listen(roomsButton, OpenRooms, bind);
            Listen(dailyPuzzleButton, OpenDailyPuzzle, bind);
            if (bind) UpdateLockedFeatures();
        }

        // Coming-soon buttons stay tappable so the popup explains the lock; they just look locked.
        private void UpdateLockedFeatures()
        {
            if (collectionButton != null) collectionButton.gameObject.SetActive(true);
            if (roomsButton != null) roomsButton.gameObject.SetActive(true);
            SetLocked(collectionButton, IsCollectionLocked);
            SetLocked(roomsButton, IsRoomsLocked);
        }

        private const string LockName = "Lock";

        private void SetLocked(Button button, bool locked)
        {
            if (button == null) return;
            Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (graphic != null) graphic.color = locked ? new Color(lockedDim, lockedDim, lockedDim, 1f) : Color.white;

            Transform existing = button.transform.Find(LockName);
            Image lockImage = existing != null ? existing.GetComponent<Image>() : null;
            if (lockImage == null && locked && lockSprite != null)
            {
                var lockObject = new GameObject(LockName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = lockObject.GetComponent<RectTransform>();
                rect.SetParent(button.transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = lockSize;
                lockImage = lockObject.GetComponent<Image>();
                lockImage.sprite = lockSprite;
                lockImage.preserveAspect = true;
                lockImage.raycastTarget = false;
            }
            if (lockImage == null) return;
            lockImage.transform.SetAsLastSibling();
            lockImage.gameObject.SetActive(locked);
        }

        private static void Listen(Button button, UnityAction action, bool bind)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (bind) button.onClick.AddListener(action);
        }
    }
}
