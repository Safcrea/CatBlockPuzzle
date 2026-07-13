using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame : MonoBehaviour
    {
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private const float MaxBoardWidth = 720f;
        private const float MaxBoardHeight = 820f;
        private const float BoardCenterY = 54f;
        private const float TimerBoardGap = 24f;
        private const float TimerHeight = 70f;
        private const float BoardGap = 8f;
        private const float TrayGap = 5f;
        private const float InvalidReturnSeconds = 0.22f;
        private const float PopSeconds = 0.22f;
        private const float SparkSeconds = 0.55f;
        private const float TouchVisualLift = 42f;
        private const float MouseVisualLift = 22f;
        private const float BoardSnapPadding = 84f;
        private const float TrayReturnPadding = 18f;
        private const float PiecePickDragThreshold = 14f;
        private const float PiecePickVerticalBias = 0.65f;
        private const float TrayScrollHorizontalBias = 1.1f;
        private const float DragFollowDelay = 0.075f;
        private const float DragTiltAmount = 8f;
        private const float DragTiltSpeed = 16f;
        private const float DragTiltVelocityScale = 900f;
        private const float DragJellyAmount = 0.065f;
        private const float DragJellySpeed = 18f;
        private const float DragJellyVelocityScale = 900f;
        private const float BoardRevealCellSeconds = 0.24f;
        private const float BoardRevealStaggerSeconds = 0.035f;
        private const float BoardRevealOvershoot = 1.14f;
        private const float TrayMaxWidth = 1040f;
        private const float TrayFloatAmplitude = 9f;
        private const float TrayFloatRotation = 2.5f;
        private const float TrayFloatSpeed = 1.25f;
        private const float LevelDurationSeconds = 120f;
        private const float TimerWarningSeconds = 20f;
        private const float TimerPulseScale = 1.1f;
        private const int AudioSampleRate = 44100;
        private const string SavedLevelKey = "CatBlockPuzzle.LevelIndex";
        private const string SavedCoinsKey = "CatBlockPuzzle.Coins";
        private const string SavedSoundKey = "CatBlockPuzzle.Settings.Sound";
        private const string SavedHapticsKey = "CatBlockPuzzle.Settings.Haptics";
        private const string SavedReducedMotionKey = "CatBlockPuzzle.Settings.ReducedMotion";
        private const string SavedBestStarsPrefix = "CatBlockPuzzle.BestStars.";

        private static readonly Color PageColor = new Color(251f / 255f, 247f / 255f, 238f / 255f, 1f);
        private static readonly Color PanelColor = new Color(1f, 248f / 255f, 236f / 255f, 0.97f);
        private static readonly Color TrayColor = new Color(248f / 255f, 184f / 255f, 153f / 255f, 0.98f);
        private static readonly Color InkColor = new Color(74f / 255f, 46f / 255f, 42f / 255f, 1f);
        private static readonly Color SoftInkColor = new Color(0.52f, 0.43f, 0.35f, 1f);
        private static readonly Color TargetColor = new Color(217f / 255f, 217f / 255f, 221f / 255f, 1f);
        private static readonly Color TargetDeepColor = new Color(59f / 255f, 151f / 255f, 137f / 255f, 1f);
        private static readonly Color ValidColor = new Color(0.68f, 0.88f, 0.65f, 1f);
        private static readonly Color InvalidColor = new Color(1f, 0.55f, 0.58f, 1f);
        private static readonly Color GoldColor = new Color(246f / 255f, 190f / 255f, 62f / 255f, 1f);
        private static readonly Color PanelOutlineColor = new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.78f);
        private static readonly Color BoardFrameColor = new Color(251f / 255f, 241f / 255f, 221f / 255f, 1f);
        private static readonly Color BoardOutlineColor = new Color(230f / 255f, 214f / 255f, 184f / 255f, 0.9f);
        private static readonly Color BoardTileEdgeColor = new Color(191f / 255f, 193f / 255f, 200f / 255f, 0.55f);
        private static readonly Color CardRestColor = new Color(1f, 252f / 255f, 246f / 255f, 0.92f);
        private static readonly Color CardDimColor = new Color(1f, 248f / 255f, 236f / 255f, 0.42f);
        private static readonly Color TrayHoverColor = new Color(1f, 207f / 255f, 164f / 255f, 1f);
        private static readonly Color TimerNormalColor = new Color(0.05f, 0.045f, 0.04f, 1f);
        private static readonly Color TimerWarningColor = new Color(0.88f, 0.08f, 0.08f, 1f);

        private static readonly Color[] PieceColors =
        {
            new Color(1f, 143f / 255f, 166f / 255f, 1f),
            new Color(102f / 255f, 208f / 255f, 183f / 255f, 1f),
            new Color(111f / 255f, 167f / 255f, 245f / 255f, 1f),
            new Color(246f / 255f, 190f / 255f, 62f / 255f, 1f),
            new Color(0.74f, 0.65f, 0.94f, 1f),
            new Color(0.95f, 0.55f, 0.45f, 1f),
            new Color(0.61f, 0.83f, 0.42f, 1f),
            new Color(0.44f, 0.78f, 0.9f, 1f)
        };

        private readonly Dictionary<Vector2Int, CellView> boardCells = new Dictionary<Vector2Int, CellView>();
        private readonly Dictionary<Vector2Int, string> occupancy = new Dictionary<Vector2Int, string>();
        private readonly List<PieceState> pieces = new List<PieceState>();
        private readonly List<CellView> previewCells = new List<CellView>();
        private readonly List<BoardRevealCell> boardRevealCells = new List<BoardRevealCell>();
        private readonly List<Vector2Int> occupancyRemovalBuffer = new List<Vector2Int>(8);
        private readonly Stack<Image> fxImagePool = new Stack<Image>(64);
        private readonly Vector3[] rectWorldCorners = new Vector3[4];
        private bool[,] placementAvailability = new bool[0, 0];

        private Canvas canvas;
        private RectTransform root;
        private RectTransform boardBackdrop;
        private RectTransform boardRoot;
        private RectTransform trayRoot;
        private RectTransform trayViewport;
        private RectTransform trayContent;
        private RectTransform pieceLayer;
        private RectTransform fxLayer;
        private RectTransform winOverlay;
        private RectTransform winPanel;
        private RectTransform failOverlay;
        private RectTransform failPanel;
        private Image trayImage;
        private Text levelText;
        private RectTransform timerPanel;
        private Text timerText;
        private Text objectiveText;
        private RectTransform objectivePanel;
        private Text coinText;
        private Text winTitleText;
        private Text winRewardText;
        private Text winBestText;
        private Text winUnlockText;
        private Text comboText;
        private RectTransform starPanel;
        private RectTransform comboBadge;
        private RectTransform actionBar;
        private RectTransform settingsOverlay;
        private RectTransform settingsPanel;
        private Image winCatImage;
        private readonly Image[] progressStars = new Image[3];
        private readonly Image[] winStars = new Image[3];
        private Text soundToggleText;
        private Text hapticsToggleText;
        private Text motionToggleText;
        private Text settingsTitleText;
        private Toggle soundToggle;
        private Toggle hapticsToggle;
        private Toggle motionToggle;
        private Button previousTestButton;
        private Button nextTestButton;
        private RectTransform soundToggleKnob;
        private RectTransform hapticsToggleKnob;
        private RectTransform motionToggleKnob;
        private HorizontalLayoutGroup trayLayout;
        private ScrollRect trayScrollRect;
        private Font defaultFont;
        private Sprite whiteSprite;
        private Sprite roundedBoxSprite;
        private Sprite circleSprite;
        private Sprite coinSprite;
        private Sprite catHeadSprite;
        private Sprite mouthSprite;
        private Sprite tailSprite;
        private Sprite pawSprite;
        private Sprite starSprite;
        private Sprite starOutlineSprite;
        private Sprite backIconSprite;
        private Sprite pauseIconSprite;
        private Sprite settingsIconSprite;
        private Sprite hintIconSprite;
        private Sprite resetIconSprite;
        private Sprite closeIconSprite;
        private readonly Sprite[,] catPortraitSprites = new Sprite[3, 8];
        private AudioSource audioSource;
        private AudioClip buttonClip;
        private AudioClip snapClip;
        private AudioClip wrongClip;
        private AudioClip winClip;
        private CatVisualCatalog visualCatalog;
        private PortraitLayoutProfile layoutProfile;
        private LevelDefinition activeLevel;
        private LevelManager levelManager;
        private HapticsController haptics;
        private DragState drag;
        private Coroutine hintRoutine;
        private Coroutine boardRevealRoutine;
        private Coroutine timerPulseRoutine;
        private bool inputLocked;
        private bool timerRunning;
        private bool levelFailed;
        private bool soundEnabled = true;
        private bool hapticsEnabled = true;
        private bool reducedMotion;
        private bool timerWasRunningBeforeSettings;
        private bool levelNavigationTesting;
        private int levelIndex;
        private int coins;
        private int comboCount;
        private int bestCombo;
        private int earnedStars;
        private int lastTimerSecond = -1;
        private float boardWidth;
        private float boardHeight;
        private float boardCellWidth;
        private float boardCellHeight;
        private float levelRemainingSeconds = LevelDurationSeconds;
        private float traySlotPreferredWidth = 196f;
        private float traySlotPreferredHeight = 248f;
        private float traySlotMinWidth = 104f;
        private float traySlotMinHeight = 154f;
        private float trayCellMaxSize = 38f;
        private float nextDragTrailTime;
        private float boardCenterY = BoardCenterY;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<CatBlockPuzzleGame>() != null)
            {
                return;
            }

            GameObject host = new GameObject("Cat Block Puzzle Runtime");
            host.AddComponent<CatBlockPuzzleGame>();
        }

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            levelManager = new LevelManager("CatBlockPuzzle/levels_100");
            levelManager.Load();
            haptics = new HapticsController(this);
            visualCatalog = CatVisualCatalog.LoadOrCreate();
            layoutProfile = PortraitLayoutProfile.LoadOrCreate();

            whiteSprite = CreateSolidSprite(Color.white);
            roundedBoxSprite = CreateRoundedBoxSprite();
            circleSprite = CreateCircleSprite();
            coinSprite = CreateCoinSprite();
            catHeadSprite = CreateCatHeadSprite();
            mouthSprite = CreateMouthSprite();
            tailSprite = CreateTailSprite();
            pawSprite = CreatePawSprite();
            starSprite = CreateStarSprite(false);
            starOutlineSprite = CreateStarSprite(true);
            backIconSprite = CreateUiIconSprite(UiIcon.Back);
            pauseIconSprite = CreateUiIconSprite(UiIcon.Pause);
            settingsIconSprite = CreateUiIconSprite(UiIcon.Settings);
            hintIconSprite = CreateUiIconSprite(UiIcon.Hint);
            resetIconSprite = CreateUiIconSprite(UiIcon.Reset);
            closeIconSprite = CreateUiIconSprite(UiIcon.Close);
            LoadAuthoredCatPortraits();
            BuildAudio();
            LoadPreferences();
            EnsureEventSystem();
            BuildCanvas();
            coins = PlayerPrefs.GetInt(SavedCoinsKey, 0);
        }

        private IEnumerator Start()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            int requestedLevel = Mathf.Clamp(PlayerPrefs.GetInt(SavedLevelKey, 0), 0, levelManager.LevelCount - 1);
            bool previewLevel = false;
#if UNITY_EDITOR
            const string previewLevelKey = "CatBlockPuzzle.EditorPreviewLevel";
            if (UnityEditor.EditorPrefs.HasKey(previewLevelKey))
            {
                requestedLevel = Mathf.Clamp(UnityEditor.EditorPrefs.GetInt(previewLevelKey, requestedLevel), 0, levelManager.LevelCount - 1);
                UnityEditor.EditorPrefs.DeleteKey(previewLevelKey);
                previewLevel = true;
            }
#endif
            LoadLevel(requestedLevel, !previewLevel);
        }

        private void LoadLevel(int nextLevelIndex, bool persistProgress = true)
        {
            inputLocked = true;
            timerRunning = false;
            levelFailed = false;
            comboCount = 0;
            bestCombo = 0;
            earnedStars = 0;
            drag = null;
            levelNavigationTesting = !persistProgress;
            SetTrayScrollEnabled(true);
            StopHint();
            haptics?.CancelLevelComplete();
            StopAllCoroutines();
            boardRevealRoutine = null;
            timerPulseRoutine = null;
            previewCells.Clear();
            occupancy.Clear();
            pieces.Clear();
            ClearChildren(boardRoot);
            if (trayContent != null)
            {
                ClearChildren(trayContent);
            }
            else
            {
                ClearChildren(trayRoot);
            }

            ResetFxPool();
            winOverlay.gameObject.SetActive(false);
            failOverlay.gameObject.SetActive(false);
            if (settingsOverlay != null)
            {
                settingsOverlay.gameObject.SetActive(false);
            }

            levelIndex = Mathf.Clamp(nextLevelIndex, 0, levelManager.LevelCount - 1);
            activeLevel = levelManager.GetLevel(levelIndex);
            ApplyLevelTheme(levelIndex);
            levelText.text = "Level " + (levelIndex + 1).ToString();
            if (objectiveText != null)
            {
                objectiveText.text = BuildThemedObjectiveTitle(activeLevel.Title);
            }

            coinText.text = coins.ToString();
            if (persistProgress)
            {
                SaveProgress();
            }
            ResetLevelTimer();
            UpdateComboDisplay(false);

            BuildBoard();
            BuildPieces();
            UpdateTestLevelButtons();
            boardRevealRoutine = StartCoroutine(PlayLevelStartReveal());
        }

        private void Update()
        {
            UpdateDraggedPieceMotion();
            UpdateLevelTimer();
            UpdateTrayIdleMotion();
        }

        private void ResetLevel()
        {
            LoadLevel(levelIndex, !levelNavigationTesting);
        }

        private bool CanGoPreviousLevel => levelIndex > 0;

        private bool CanGoNextLevel => levelManager != null && levelIndex < levelManager.LevelCount - 1;

        private void LoadPreviousLevel()
        {
            LoadLevel(CanGoPreviousLevel ? levelIndex - 1 : 0, !levelNavigationTesting);
        }

        private void LoadNextLevel()
        {
            if (levelManager == null || levelManager.LevelCount <= 0)
            {
                return;
            }

            int maxLevelIndex = levelManager.LevelCount - 1;
            LoadLevel(CanGoNextLevel ? levelIndex + 1 : maxLevelIndex, !levelNavigationTesting);
        }

        private void LoadPreviousTestLevel()
        {
            LoadLevel(CanGoPreviousLevel ? levelIndex - 1 : 0, false);
        }

        private void LoadNextTestLevel()
        {
            if (levelManager == null || levelManager.LevelCount <= 0)
            {
                return;
            }

            int maxLevelIndex = levelManager.LevelCount - 1;
            LoadLevel(CanGoNextLevel ? levelIndex + 1 : maxLevelIndex, false);
        }

        public void PreviewLevelForTesting(int zeroBasedLevelIndex)
        {
            layoutProfile = PortraitLayoutProfile.LoadOrCreate();
            LoadLevel(zeroBasedLevelIndex, false);
        }

        private void UpdateTestLevelButtons()
        {
            if (previousTestButton != null)
            {
                previousTestButton.interactable = CanGoPreviousLevel;
            }

            if (nextTestButton != null)
            {
                nextTestButton.interactable = CanGoNextLevel;
            }
        }

        private void SaveProgress()
        {
            if (levelNavigationTesting)
            {
                return;
            }

            PlayerPrefs.SetInt(SavedLevelKey, levelIndex);
            PlayerPrefs.SetInt(SavedCoinsKey, coins);
            PlayerPrefs.Save();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
