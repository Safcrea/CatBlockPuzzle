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
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            RefreshVisuals();
        }

        public void Hide() { if (root != null) root.SetActive(false); }

        private void UpdateListeners(bool bind)
        {
            Listen(closeButton, Close, bind);
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

        private void ToggleMusic()
        {
            SoundManager manager = SoundManager.EnsureInstance();
            manager.SetMusicEnabled(!manager.MusicEnabled);
            RefreshVisuals();
        }

        private void ToggleSfx()
        {
            SoundManager manager = SoundManager.EnsureInstance();
            manager.SetSfxEnabled(!manager.SfxEnabled);
            RefreshVisuals();
        }

        private void ToggleHaptics()
        {
            PlayerPrefs.SetInt(HapticsEnabledKey, HapticsEnabled ? 0 : 1);
            PlayerPrefs.Save();
            GameSystem.Instance?.ApplyHapticsPreference();
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (!Application.isPlaying) return;
            SoundManager manager = SoundManager.EnsureInstance();
            SetState(musicButton, musicStateText, manager.MusicEnabled);
            SetState(sfxButton, sfxStateText, manager.SfxEnabled);
            SetState(hapticsButton, hapticsStateText, HapticsEnabled);
        }

        private void SetState(Button button, Text text, bool enabled)
        {
            Sprite sprite = enabled ? enabledSprite : disabledSprite;
            if (button != null && button.targetGraphic is Image image && sprite != null) image.sprite = sprite;
            if (text != null) text.text = enabled ? "ON" : "OFF";
        }

        private void Close()
        {
            Hide();
            if (returnToPause) GameSystem.Instance?.ReturnToPause();
        }
    }
}
