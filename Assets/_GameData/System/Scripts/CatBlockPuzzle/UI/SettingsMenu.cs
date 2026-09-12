using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class SettingsMenu : MonoBehaviour
    {
        public const string HapticsEnabledKey = "CatBlockPuzzle.Settings.Haptics";

        [SerializeField] private GameObject root;
        [System.NonSerialized] private Button closeButton;
        [System.NonSerialized] private Button musicButton;
        [System.NonSerialized] private Button sfxButton;
        [System.NonSerialized] private Button hapticsButton;
        [System.NonSerialized] private Text musicStateText;
        [System.NonSerialized] private Text sfxStateText;
        [System.NonSerialized] private Text hapticsStateText;

        private bool returnToPause;
        public static bool HapticsEnabled => PlayerPrefs.GetInt(HapticsEnabledKey, 1) != 0;
        public bool IsOpen => root != null && root.activeSelf;

        public void EnsureRuntimeView(Transform canvas)
        {
            if (musicButton != null || canvas == null)
            {
                BindButtons();
                return;
            }

            RectTransform overlay = root != null ? root.GetComponent<RectTransform>() : null;
            if (overlay == null)
            {
                overlay = RuntimeUiFactory.CreateOverlay(canvas, "Settings Menu");
                root = overlay.gameObject;
            }
            else
            {
                RuntimeUiFactory.PrepareOverlay(overlay);
            }

            RectTransform panel = RuntimeUiFactory.CreatePanel(overlay, "Settings Panel", new Vector2(650f, 650f));
            Text title = RuntimeUiFactory.CreateText(panel, "Title", "SETTINGS", 58, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0f, 245f), new Vector2(520f, 80f));
            musicButton = CreateSettingRow(panel, "Music", new Vector2(0f, 115f), out musicStateText);
            sfxButton = CreateSettingRow(panel, "SFX", new Vector2(0f, 0f), out sfxStateText);
            hapticsButton = CreateSettingRow(panel, "Haptics", new Vector2(0f, -115f), out hapticsStateText);
            closeButton = RuntimeUiFactory.CreateButton(panel, "Close", "Done", new Vector2(0f, -245f), new Vector2(360f, 82f), RuntimeUiFactory.Coral);
            root.SetActive(false);
            BindButtons();
            RefreshVisuals();
        }

        public void Show(bool openedFromPause)
        {
            returnToPause = openedFromPause;
            if (root == null) return;
            RefreshVisuals();
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private Button CreateSettingRow(Transform panel, string label, Vector2 position, out Text stateText)
        {
            Button button = RuntimeUiFactory.CreateButton(panel, label, label, position, new Vector2(510f, 86f), Color.white);
            Text labelText = button.GetComponentInChildren<Text>();
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.rectTransform.offsetMin = new Vector2(34f, 0f);
            labelText.rectTransform.offsetMax = new Vector2(-145f, 0f);
            stateText = RuntimeUiFactory.CreateText(button.transform, "State", "On", 30, TextAnchor.MiddleCenter);
            RectTransform stateRect = stateText.rectTransform;
            stateRect.anchorMin = stateRect.anchorMax = new Vector2(1f, 0.5f);
            stateRect.pivot = new Vector2(1f, 0.5f);
            stateRect.anchoredPosition = new Vector2(-24f, 0f);
            stateRect.sizeDelta = new Vector2(110f, 60f);
            return button;
        }

        private void BindButtons()
        {
            Bind(closeButton, Close);
            Bind(musicButton, ToggleMusic);
            Bind(sfxButton, ToggleSfx);
            Bind(hapticsButton, ToggleHaptics);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
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
            SoundManager manager = SoundManager.EnsureInstance();
            SetState(musicStateText, manager.MusicEnabled);
            SetState(sfxStateText, manager.SfxEnabled);
            SetState(hapticsStateText, HapticsEnabled);
        }

        private static void SetState(Text text, bool enabled)
        {
            if (text == null) return;
            text.text = enabled ? "ON" : "OFF";
            text.color = enabled ? new Color(0.2f, 0.62f, 0.5f, 1f) : new Color(0.65f, 0.45f, 0.42f, 1f);
        }

        private void Close()
        {
            Hide();
            if (returnToPause) GameSystem.Instance?.ReturnToPause();
        }
    }
}
