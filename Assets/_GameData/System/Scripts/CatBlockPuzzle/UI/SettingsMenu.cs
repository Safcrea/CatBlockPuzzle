using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class SettingsMenu : MonoBehaviour
    {
        public const string HapticsEnabledKey = "CatBlockPuzzle.Settings.Haptics";
        [SerializeField] private GameObject root;
        [Header("Authored Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private Button hapticsButton;
        [Header("Toggle Artwork")]
        [SerializeField] private Sprite enabledSprite;
        [SerializeField] private Sprite disabledSprite;
        [Header("Optional Labels")]
        [SerializeField] private Text musicStateText;
        [SerializeField] private Text sfxStateText;
        [SerializeField] private Text hapticsStateText;
        [Header("Opening Animation")]
        [Tooltip("Optional. What pops in, in order - assign the whole rows to carry their labels along. " +
                 "Left empty, only the toggle buttons animate.")]
        [SerializeField] private Transform[] animatedItems;
        [SerializeField] private MenuTransition.EntranceSettings openingAnimation = new MenuTransition.EntranceSettings
        {
            duration = .34f,
            slideDistance = 30f,
            cardDelay = .06f,
            cardStagger = .08f,
            cardDuration = .3f,
            cardStartingScale = .7f,
            cardSlideDistance = 48f,
            overshoot = 2f
        };
        [Header("Production Toggle Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Toggle notificationsToggle;
        private bool refreshingToggles;
        private const string NotificationsKey = "CatBlockPuzzle.Settings.Notifications";
        private bool returnToPause;

        public static bool HapticsEnabled => PlayerPrefs.GetInt(HapticsEnabledKey, 1) != 0;
        public bool IsOpen => root != null && root.activeInHierarchy;
        public bool IsConfigured => root != null;
        private void OnEnable() { EnsureBindings(); RefreshVisuals(); }
        private void OnDestroy() => UpdateListeners(false);
        public void EnsureBindings() => UpdateListeners(true);

        public void Show(bool openedFromPause)
        {
            if (root == null) return;
            returnToPause = openedFromPause;
            // Order here is the order the toggles pop in.
            MenuTransition.Show(root, true, openingAnimation, animatedItems != null && animatedItems.Length > 0
                ? MenuTransition.Cards(animatedItems)
                : MenuTransition.Cards(musicButton, sfxButton, hapticsButton, closeButton));
            RefreshVisuals();
        }

        public void Hide() => MenuTransition.Hide(root, null, false);

        private void UpdateListeners(bool bind)
        {
            Listen(closeButton, Close, bind);
            Listen(backButton, Close, bind);
            ListenToggle(musicToggle, SetMusic, bind);
            ListenToggle(soundToggle, SetSound, bind);
            ListenToggle(vibrationToggle, SetVibration, bind);
            ListenToggle(notificationsToggle, SetNotifications, bind);
            Listen(musicButton, ToggleMusic, bind);
            Listen(sfxButton, ToggleSfx, bind);
            Listen(hapticsButton, ToggleHaptics, bind);
        }

        private static void Listen(Button button, UnityAction action, bool bind)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (bind) button.onClick.AddListener(action);
        }

        private static void ListenToggle(Toggle toggle, UnityAction<bool> action, bool bind)
        {
            if (toggle == null) return;
            toggle.onValueChanged.RemoveListener(action);
            if (bind) toggle.onValueChanged.AddListener(action);
        }

        private void SetMusic(bool value)
        {
            if (!refreshingToggles && value != SoundManager.EnsureInstance().MusicEnabled) ToggleMusic();
        }
        private void SetSound(bool value)
        {
            if (!refreshingToggles && value != SoundManager.EnsureInstance().SfxEnabled) ToggleSfx();
        }
        private void SetVibration(bool value)
        {
            if (!refreshingToggles && value != HapticsEnabled) ToggleHaptics();
        }
        private void SetNotifications(bool value)
        {
            if (refreshingToggles) return;
            ButtonSfx.PlayClick();
            PlayerPrefs.SetInt(NotificationsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ToggleMusic()
        {
            ButtonSfx.PlayClick();
            SoundManager manager = SoundManager.EnsureInstance();
            manager.SetMusicEnabled(!manager.MusicEnabled);
            GameAnalytics.SettingsChanged("music", manager.MusicEnabled, returnToPause);
            RefreshVisuals();
        }

        private void ToggleSfx()
        {
            SoundManager manager = SoundManager.EnsureInstance();
            manager.SetSfxEnabled(!manager.SfxEnabled);
            GameAnalytics.SettingsChanged("sfx", manager.SfxEnabled, returnToPause);
            ButtonSfx.PlayClick();
            RefreshVisuals();
        }

        private void ToggleHaptics()
        {
            ButtonSfx.PlayClick();
            PlayerPrefs.SetInt(HapticsEnabledKey, HapticsEnabled ? 0 : 1);
            PlayerPrefs.Save();
            GameSystem.Instance?.ApplyHapticsPreference();
            GameAnalytics.SettingsChanged("haptics", HapticsEnabled, returnToPause);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (!Application.isPlaying) return;
            SoundManager manager = SoundManager.EnsureInstance();
            SetState(musicButton, musicStateText, manager.MusicEnabled);
            SetState(sfxButton, sfxStateText, manager.SfxEnabled);
            SetState(hapticsButton, hapticsStateText, HapticsEnabled);
            refreshingToggles = true;
            if (musicToggle != null) musicToggle.isOn = manager.MusicEnabled;
            if (soundToggle != null) soundToggle.isOn = manager.SfxEnabled;
            if (vibrationToggle != null) vibrationToggle.isOn = HapticsEnabled;
            if (notificationsToggle != null) notificationsToggle.isOn = PlayerPrefs.GetInt(NotificationsKey, 1) != 0;
            refreshingToggles = false;
        }

        private void SetState(Button button, Text text, bool enabled)
        {
            Sprite sprite = enabled ? enabledSprite : disabledSprite;
            if (button != null && button.targetGraphic is Image image && sprite != null) image.sprite = sprite;
            if (text != null) text.text = enabled ? "ON" : "OFF";
        }

        private void Close()
        {
            ButtonSfx.PlayClick();
            MenuTransition.Hide(root, () =>
            {
                if (returnToPause) GameSystem.Instance?.ReturnToPause();
            });
        }
    }
}
