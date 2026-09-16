using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] internal Canvas canvas;
        [SerializeField] internal RectTransform root;
        [SerializeField] internal Camera sceneCamera;
        [SerializeField] internal EventSystem sceneEventSystem;
        [SerializeField] internal RectTransform gameplayScreen;
        [SerializeField] internal RectTransform levelRoot;

        [Header("HUD")]
        [SerializeField] internal RectTransform fxLayer;
        [SerializeField] internal Text levelText;
        [SerializeField] internal RectTransform timerPanel;
        [SerializeField] internal Text timerText;
        [SerializeField] internal Text objectiveText;
        [SerializeField] internal RectTransform objectivePanel;
        [SerializeField] internal Text coinText;
        [SerializeField] internal Text comboText;
        [SerializeField] internal RectTransform starPanel;
        [SerializeField] internal RectTransform comboBadge;
        [SerializeField] internal RectTransform actionBar;
        [SerializeField] internal Image[] progressStars = new Image[3];
        [SerializeField] internal Button previousTestButton;
        [SerializeField] internal Button nextTestButton;

        [Header("Prepared Presentation")]
        [SerializeField] internal Image[] preparedEffects;
        [SerializeField] internal Image backgroundCrossfade;
        [SerializeField] internal Image objectiveImage;
        [SerializeField] internal Sprite defaultBackgroundSprite;
        [SerializeField] internal Image backgroundImage;
        [SerializeField, HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("headerBandImage")]
        private Image legacyHeaderBand;

        public Canvas Canvas => canvas;
        public RectTransform GameplayScreen => gameplayScreen;
        public RectTransform LevelRoot => levelRoot;
        [Header("Authored Gameplay Layout")]
        [SerializeField] internal RectTransform boardArea;
        [SerializeField] internal RectTransform trayArea;
        public bool UsesAuthoredLayout => boardArea != null && trayArea != null;
        [Header("Power Up Controls")]
        [SerializeField] private Button freezeButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Text hintCountText;
        [SerializeField] private Text freezeCountText;
        private bool freezeAvailable = true;
        [Header("Power Up Unlock Levels")]
        [SerializeField, Min(1)] private int hintUnlockLevel = 3;
        [SerializeField, Min(1)] private int freezeUnlockLevel = 5;
        private PowerUpTutorialManager tutorialManager;
        public bool IsTutorialOpen => tutorialManager != null && tutorialManager.IsOpen;

        private void Awake()
        {
            if (legacyHeaderBand != null) legacyHeaderBand.enabled = false;
        }

        public bool IsPowerUpUnlocked(PowerUpKind kind)
        {
            int level = GameSystem.Instance != null ? GameSystem.Instance.CurrentLevelNumber : 1;
            return level >= Mathf.Max(1, kind == PowerUpKind.Hint ? hintUnlockLevel : freezeUnlockLevel);
        }

        public Sprite GetPowerUpSprite(PowerUpKind kind)
        {
            var button = kind == PowerUpKind.Hint ? hintButton : freezeButton;
            return button != null && button.targetGraphic is Image image ? image.sprite : null;
        }

        public System.Collections.IEnumerator ShowPowerUpTutorials(float freezeSeconds)
        {
            if (tutorialManager == null) tutorialManager = GetComponent<PowerUpTutorialManager>();
            if (tutorialManager == null) tutorialManager = gameObject.AddComponent<PowerUpTutorialManager>();
            yield return tutorialManager.ShowUnlockedTutorials(this, freezeSeconds);
        }

        public void CancelPowerUpTutorial() { if (tutorialManager != null) tutorialManager.Cancel(); }
        public void LoadNextLevel() => GameSystem.Instance?.LoadNextLevel();
        public void LoadPreviousLevel() => GameSystem.Instance?.LoadPreviousLevel();

        private void OnEnable()
        {
            if (freezeButton != null)
            {
                freezeButton.onClick.RemoveListener(Freeze);
                freezeButton.onClick.AddListener(Freeze);
            }

            RefreshPowerUpCounts();
        }
        private void OnDisable()
        {
            CancelPowerUpTutorial();
            if (freezeButton != null) freezeButton.onClick.RemoveListener(Freeze);
        }

        private void Start() => RefreshPowerUpCounts();

        private void Freeze() => GameSystem.Instance?.TryUseFreezePowerUp();

        /// <summary>Updates the authored HUD labels and availability from the saved inventory.</summary>
        public void RefreshPowerUpCounts()
        {
            GameSystem system = GameSystem.Instance;
            int hintCount = system != null ? system.GetPowerUpCount(PowerUpKind.Hint) : 0;
            int freezeCount = system != null ? system.GetPowerUpCount(PowerUpKind.Freeze) : 0;

            if (hintCountText != null) hintCountText.text = $"x{hintCount}";
            if (freezeCountText != null) freezeCountText.text = $"x{freezeCount}";
            bool hintUnlocked = IsPowerUpUnlocked(PowerUpKind.Hint);
            bool freezeUnlocked = IsPowerUpUnlocked(PowerUpKind.Freeze);
            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(hintUnlocked);
                hintButton.interactable = hintUnlocked && hintCount > 0;
            }
            if (freezeButton != null)
            {
                freezeButton.gameObject.SetActive(freezeUnlocked);
                freezeButton.interactable = freezeUnlocked && freezeAvailable && freezeCount > 0;
            }
        }

        public void SetFreezeAvailable(bool available)
        {
            freezeAvailable = available;
            RefreshPowerUpCounts();
        }

        public bool Validate(out string error)
        {
            error = null;
            if (sceneCamera == null || sceneEventSystem == null || canvas == null ||
                canvas.worldCamera != sceneCamera || canvas.renderMode != RenderMode.ScreenSpaceCamera)
                error = "Camera, Canvas, or EventSystem references are missing";
            else if (root == null || gameplayScreen == null || levelRoot == null || fxLayer == null)
                error = "gameplay scene roots are missing";
            else if (levelText == null || timerText == null || coinText == null || objectiveImage == null ||
                (!UsesAuthoredLayout && (previousTestButton == null || nextTestButton == null)))
                error = "gameplay HUD controls are missing";
            else if (freezeButton == null || hintButton == null || hintCountText == null || freezeCountText == null)
                error = "power-up HUD controls or count labels are missing";
            else if (preparedEffects == null || preparedEffects.Length != 256 || backgroundCrossfade == null)
                error = "the saved effect pool or crossfade is missing";
            return error == null;
        }
    }
}
