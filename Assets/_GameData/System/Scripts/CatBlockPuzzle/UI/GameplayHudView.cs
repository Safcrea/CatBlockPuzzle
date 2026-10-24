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
        [Tooltip("Icy look for the timer counter while Freeze holds the countdown.")]
        [SerializeField] internal TimerFreezeSkin timerFreezeSkin;
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
        private Color? authoredBackgroundTint;

        public Canvas Canvas => canvas;
        public RectTransform GameplayScreen => gameplayScreen;
        public RectTransform LevelRoot => levelRoot;
        [Header("Authored Gameplay Layout")]
        [SerializeField] internal RectTransform boardArea;
        [SerializeField] internal RectTransform trayArea;
        [Tooltip("Space on each side between the puzzle grid and the authored board area.")]
        [SerializeField] internal Vector2 boardContentPadding = new Vector2(36f, 36f);
        public bool UsesAuthoredLayout => boardArea != null && trayArea != null;
        [Header("Power Up Controls")]
        [SerializeField] private Button freezeButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Text hintCountText;
        [SerializeField] private Text freezeCountText;
        [SerializeField] private GameObject hintCountBoard;
        [SerializeField] private GameObject freezeCountBoard;
        [Tooltip("Optional. Turns one shelf cat 90 degrees clockwise.")]
        [SerializeField] private Button rotateButton;
        [SerializeField] private Text rotateCountText;
        [SerializeField] private GameObject rotateCountBoard;
        private bool freezeAvailable = true;
        [Tooltip("Gap between power-up buttons once Rotate joins Hint and Freeze.")]
        [SerializeField, Min(0f)] private float threeButtonSpacing = 265f;
        // Hint and Freeze as authored; Rotate shifts them only while it is shown.
        private Vector2[] authoredButtonPositions;
        [Header("Power Up Unlock Levels")]
        [SerializeField, Min(1)] private int hintUnlockLevel = 3;
        [SerializeField, Min(1)] private int freezeUnlockLevel = 5;
        [SerializeField, Min(1)] private int rotateUnlockLevel = 21;
        [Header("Power Up Purchase")]
        [Tooltip("Popup spawned when a power-up is tapped with an empty bag. Falls back to Resources/" + PowerUpPurchasePopup.PrefabResourcePath + ", then to a code-built layout.")]
        [SerializeField] private GameObject purchasePopupPrefab;
        private PowerUpTutorialManager tutorialManager;
        private PowerUpPurchasePopup purchasePopup;
        public bool IsTutorialOpen => tutorialManager != null && tutorialManager.IsOpen;
        public bool IsPurchaseOpen => purchasePopup != null && purchasePopup.IsOpen;
        internal PowerUpKind PurchaseKind => purchasePopup != null ? purchasePopup.Kind : default;
        public bool ShowPowerUpPurchase(PowerUpKind kind)
        {
            if (canvas == null || IsTutorialOpen || IsPurchaseOpen) return false;
            if (purchasePopup == null) purchasePopup = PowerUpPurchasePopup.Create(this, purchasePopupPrefab);
            return purchasePopup != null && purchasePopup.Show(kind);
        }
        public void CancelPowerUpPurchase(bool resumeGameplay = true)
        {
            if (purchasePopup != null) purchasePopup.HideImmediately(resumeGameplay);
        }

        private void Awake()
        {
            if (legacyHeaderBand != null) legacyHeaderBand.enabled = false;
            if (backgroundImage != null) authoredBackgroundTint = backgroundImage.color;
        }

        /// <summary>The tint authored on the gameplay background in the scene; room and theme swaps keep it.</summary>
        public Color BackgroundTint
        {
            get
            {
                if (!authoredBackgroundTint.HasValue) authoredBackgroundTint = backgroundImage != null ? backgroundImage.color : Color.white;
                return authoredBackgroundTint.Value;
            }
        }

        public bool IsPowerUpUnlocked(PowerUpKind kind)
        {
            int level = GameSystem.Instance != null ? GameSystem.Instance.DisplayLevelNumber : 1;
            // Untimed levels have no clock to freeze, so Freeze first appears (and is taught) on the first timed level.
            if (kind == PowerUpKind.Freeze && DifficultyProfile.IsUntimed(level)) return false;
            if (kind == PowerUpKind.Rotate && rotateButton == null) return false;
            int unlock = kind == PowerUpKind.Hint ? hintUnlockLevel : kind == PowerUpKind.Rotate ? rotateUnlockLevel : freezeUnlockLevel;
            return level >= Mathf.Max(1, unlock);
        }

        internal Button PowerUpButton(PowerUpKind kind) => kind == PowerUpKind.Hint ? hintButton : kind == PowerUpKind.Rotate ? rotateButton : freezeButton;

        /// <summary>The power-up tapped at the end of its unlock tutorial, still to be spent; clears it.</summary>
        internal bool TryTakeTutorialUse(out PowerUpKind kind)
        {
            kind = default;
            return tutorialManager != null && tutorialManager.TryTakePendingUse(out kind);
        }

        public Sprite GetPowerUpSprite(PowerUpKind kind)
        {
            var button = PowerUpButton(kind);
            if (kind == PowerUpKind.Rotate && button != null)
            {
                // The Rotate button reuses the Freeze button's frame; its arrow is a child icon.
                Transform icon = button.transform.Find("Rotate Icon");
                if (icon != null && icon.GetComponent<Image>() != null) return icon.GetComponent<Image>().sprite;
            }
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

            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(Hint);
                if (hintButton.onClick.GetPersistentEventCount() == 0) hintButton.onClick.AddListener(Hint);
            }

            if (rotateButton != null)
            {
                rotateButton.onClick.RemoveListener(Rotate);
                rotateButton.onClick.AddListener(Rotate);
            }
            RefreshPowerUpCounts();
        }
        private void OnDisable()
        {
            CancelPowerUpTutorial();
            CancelPowerUpPurchase(false);
            if (freezeButton != null) freezeButton.onClick.RemoveListener(Freeze);
            if (hintButton != null) hintButton.onClick.RemoveListener(Hint);
            if (rotateButton != null) rotateButton.onClick.RemoveListener(Rotate);
        }
        private void OnDestroy()
        {
            CancelPowerUpPurchase(false);
            if (purchasePopup != null) Destroy(purchasePopup.gameObject);
        }

        private void Start() => RefreshPowerUpCounts();

        private void Freeze() { ButtonSfx.PlayClick(); GameSystem.Instance?.TryUseFreezePowerUp(); }
        private void Hint() { ButtonSfx.PlayClick(); GameSystem.Instance?.TryUseHintPowerUp(); }
        private void Rotate() { ButtonSfx.PlayClick(); GameSystem.Instance?.TryUseRotatePowerUp(); }

        /// <summary>Updates the authored HUD labels and availability from the saved inventory.</summary>
        public void RefreshPowerUpCounts()
        {
            GameSystem system = GameSystem.Instance;
            int hintCount = system != null ? system.GetPowerUpCount(PowerUpKind.Hint) : 0;
            int freezeCount = system != null ? system.GetPowerUpCount(PowerUpKind.Freeze) : 0;

            UpdateCountPresentation(hintCountText, hintCountBoard, hintCount);
            UpdateCountPresentation(freezeCountText, freezeCountBoard, freezeCount);
            bool hintUnlocked = IsPowerUpUnlocked(PowerUpKind.Hint);
            bool freezeUnlocked = IsPowerUpUnlocked(PowerUpKind.Freeze);
            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(hintUnlocked);
                hintButton.interactable = hintUnlocked;
            }
            if (freezeButton != null)
            {
                freezeButton.gameObject.SetActive(freezeUnlocked);
                freezeButton.interactable = freezeUnlocked && freezeAvailable;
            }

            if (rotateButton != null)
            {
                bool rotateUnlocked = IsPowerUpUnlocked(PowerUpKind.Rotate);
                UpdateCountPresentation(rotateCountText, rotateCountBoard, system != null ? system.GetPowerUpCount(PowerUpKind.Rotate) : 0);
                rotateButton.gameObject.SetActive(rotateUnlocked);
                // While a Rotate is in progress the button rests until that cat lands.
                rotateButton.interactable = rotateUnlocked && (system == null || !system.IsRotateActive);
            }
            ArrangePowerUpButtons();
        }

        // The row's buttons are placed by hand. From level 21 Rotate joins them, so the shown
        // buttons spread evenly around the centre of the authored Hint/Freeze pair.
        private void ArrangePowerUpButtons()
        {
            if (hintButton == null || freezeButton == null || rotateButton == null) return;
            var rects = new[] { (RectTransform)hintButton.transform, (RectTransform)freezeButton.transform, (RectTransform)rotateButton.transform };
            if (authoredButtonPositions == null) authoredButtonPositions = new[] { rects[0].anchoredPosition, rects[1].anchoredPosition };
            if (!rotateButton.gameObject.activeSelf)
            {
                rects[0].anchoredPosition = authoredButtonPositions[0];
                rects[1].anchoredPosition = authoredButtonPositions[1];
                return;
            }

            Vector2 centre = (authoredButtonPositions[0] + authoredButtonPositions[1]) * 0.5f;
            int shown = 0;
            foreach (RectTransform rect in rects) if (rect.gameObject.activeSelf) shown++;
            float x = centre.x - (shown - 1) * 0.5f * threeButtonSpacing;
            foreach (RectTransform rect in rects)
            {
                if (!rect.gameObject.activeSelf) continue;
                rect.anchoredPosition = new Vector2(x, centre.y);
                x += threeButtonSpacing;
            }
        }

        private static void UpdateCountPresentation(Text label, GameObject board, int count)
        {
            bool visible = count >= 0;
            if (label != null)
            {
                label.text = $"x{count}";
                label.gameObject.SetActive(visible);
            }
            if (board != null) board.SetActive(visible);
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
