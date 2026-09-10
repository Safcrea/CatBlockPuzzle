using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Runtime-only presentation for the room rescue meta game.  Keeping this in a
    /// partial lets the existing scene-free game continue to bootstrap itself.
    /// </summary>
    public sealed partial class CatBlockPuzzleGame
    {
        private const string MetaTelemetryPrefix = "[CatMetaTelemetry] ";

        private readonly Dictionary<string, Sprite> metaResourceSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly HashSet<string> missingMetaResources =
            new HashSet<string>(StringComparer.Ordinal);

        private CatMetaCatalog metaCatalog;
        private CatMetaProgressStore metaProgress;
        [SerializeField] private RectTransform metaOverlay;
        [SerializeField] private RectTransform metaHubPage;
        [SerializeField] private RectTransform metaRoomPage;
        [SerializeField] private RectTransform metaStoryOverlay;
        [SerializeField] private RectTransform metaCompletionOverlay;
        [SerializeField] private Text metaStatusText;
        [SerializeField] private Button metaNextLevelButton;
        [SerializeField] private Text metaNextLevelButtonText;
        [SerializeField] private Button metaDecorateNowButton;
        private bool metaSystemsUnavailable;
        private bool metaOverlayOwnsPause;
        private bool metaTimerWasRunning;
        private bool metaRequiresRoomCompletion;
        private int metaCurrentChapterIndex;
        private int metaLastAwardedCoins;
        private string metaSelectedDecorationId;

        private bool IsMetaUiOpen
        {
            get
            {
                return metaOverlay != null
                    && metaOverlay.gameObject.activeSelf
                    && ((metaHubPage != null && metaHubPage.gameObject.activeSelf)
                        || (metaRoomPage != null && metaRoomPage.gameObject.activeSelf)
                        || (metaStoryOverlay != null && metaStoryOverlay.gameObject.activeSelf)
                        || (metaCompletionOverlay != null && metaCompletionOverlay.gameObject.activeSelf));
            }
        }

        /// <summary>Loads and migrates the meta catalog/save without creating UI.</summary>
        private void InitializeMetaSystems()
        {
            if (metaCatalog != null || metaSystemsUnavailable)
            {
                return;
            }

            try
            {
                int savedLevel = PlayerPrefs.GetInt(SavedLevelKey, 0);
                int savedCoins = PlayerPrefs.GetInt(SavedCoinsKey, Mathf.Max(0, coins));
                metaCatalog = CatMetaCatalog.Load();
                metaProgress = CatMetaProgressStore.Load(metaCatalog, savedLevel, savedCoins);
                coins = metaProgress.CoinBalance;
                if (coinText != null)
                {
                    coinText.text = coins.ToString();
                }

                LogMetaEvent("systems_initialized",
                    "catalogVersion=" + CatMetaCatalog.CurrentVersion
                    + " highestChapter=" + metaProgress.HighestUnlockedChapter);
            }
            catch (Exception exception)
            {
                metaSystemsUnavailable = true;
                metaCatalog = null;
                metaProgress = null;
                Debug.LogError("Cat meta systems were disabled: " + exception);
            }
        }

        /// <summary>Builds the persistent overlay containers and rewires Home/Next.</summary>
        private void BuildMetaUi()
        {
            InitializeMetaSystems();
            if (root == null || metaOverlay != null)
            {
                return;
            }

            metaOverlay = CreatePanel(gameUiRoot != null ? gameUiRoot : root, "HomeScreen", PageColor);
            homeScreen = metaOverlay;
            Stretch(metaOverlay);
            metaOverlay.SetAsLastSibling();
            metaOverlay.GetComponent<Image>().raycastTarget = true;

            metaHubPage = CreatePanel(metaOverlay, "Room Hub", PageColor);
            Stretch(metaHubPage);
            metaRoomPage = CreatePanel(metaOverlay, "Room Detail", PageColor);
            Stretch(metaRoomPage);
            metaStoryOverlay = CreatePanel(metaOverlay, "Story Modal", new Color(0.1f, 0.08f, 0.07f, 0.62f));
            Stretch(metaStoryOverlay);
            metaCompletionOverlay = CreatePanel(metaOverlay, "Room Complete Modal", new Color(0.1f, 0.08f, 0.07f, 0.68f));
            Stretch(metaCompletionOverlay);

            metaHubPage.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(false);
            metaStoryOverlay.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaOverlay.gameObject.SetActive(false);

            RewireMetaNavigationButtons();
        }

        /// <summary>Shows the ten-room map and its dominant continuation action.</summary>
        private void OpenRoomHub()
        {
            BuildMetaUi();
            if (metaCatalog == null || metaProgress == null || metaOverlay == null)
            {
                return;
            }

            EnterMetaOverlay();
            metaCurrentChapterIndex = Mathf.Clamp(
                metaProgress.HighestUnlockedChapter,
                0,
                metaCatalog.Chapters.Count - 1);
            metaSelectedDecorationId = null;
            metaRoomPage.gameObject.SetActive(false);
            metaStoryOverlay.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaHubPage.gameObject.SetActive(true);
            RenderRoomHub();
            LogMetaEvent("hub_open", "recommendedChapter=" + metaCurrentChapterIndex);
        }

        /// <summary>Shows one furnished room, its paw track, and five item cards.</summary>
        private void OpenRoomDetail(int chapterIndex)
        {
            BuildMetaUi();
            if (metaCatalog == null || metaProgress == null || metaOverlay == null)
            {
                return;
            }

            if (chapterIndex < 0
                || chapterIndex >= metaCatalog.Chapters.Count
                || !metaProgress.IsChapterUnlocked(chapterIndex))
            {
                ShowMetaStatus("Finish the previous room to unlock this chapter.");
                LogMetaEvent("room_open_blocked", "chapter=" + chapterIndex);
                return;
            }

            EnterMetaOverlay();
            metaCurrentChapterIndex = chapterIndex;
            metaHubPage.gameObject.SetActive(false);
            metaCompletionOverlay.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(true);
            SelectFirstStoredDecoration(chapterIndex);
            RenderRoomDetail(chapterIndex);

            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            LogMetaEvent("room_open",
                "chapter=" + chapterIndex
                + " installed=" + metaProgress.InstalledDecorationCount(chapterIndex));

            string introId = CatMetaStoryIds.Intro(chapter.Id);
            if (!metaProgress.HasViewedStory(introId))
            {
                ShowChapterIntro(chapterIndex, null);
            }
        }

        /// <summary>Dismisses meta UI and safely resumes the interrupted puzzle.</summary>
        private void CloseMetaToGameplay()
        {
            if (metaRequiresRoomCompletion
                && metaProgress != null
                && metaCatalog != null
                && !metaProgress.IsChapterComplete(metaCurrentChapterIndex))
            {
                OpenRoomDetail(metaCurrentChapterIndex);
                ShowMetaStatus("Place all five decorations to open the next room.");
                return;
            }

            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            bool resumeTimer = metaOverlayOwnsPause && metaTimerWasRunning;
            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;

            if (levelFailed
                || (winOverlay != null && winOverlay.gameObject.activeSelf)
                || (failOverlay != null && failOverlay.gameObject.activeSelf)
                || (settingsOverlay != null && settingsOverlay.gameObject.activeSelf))
            {
                return;
            }

            inputLocked = false;
            SetTrayScrollEnabled(true);
            if (resumeTimer
                || (!timerRunning && boardRevealRoutine == null && activeLevel != null))
            {
                StartLevelTimer();
            }
        }

        /// <summary>Applies the active chapter's full-screen authored background.</summary>
        private void ApplyMetaLevelPresentation(int zeroBasedLevelIndex)
        {
            if (metaCatalog == null)
            {
                InitializeMetaSystems();
            }

            if (metaCatalog == null
                || zeroBasedLevelIndex < 0
                || zeroBasedLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(zeroBasedLevelIndex);
            Sprite roomSprite = LoadMetaSprite(chapter.BackgroundResourcePath);
            if (backgroundImage != null && roomSprite != null && backgroundImage.sprite != roomSprite)
            {
                CrossfadeGameplayBackground(roomSprite);
            }

            if (objectiveText != null && activeLevel != null)
            {
                objectiveText.text = chapter.Title + "  •  " + activeLevel.Title;
            }
        }

        /// <summary>
        /// Loads the requested puzzle behind either the first-run rescue cards or the
        /// returning-player hub. Editor previews intentionally bypass meta UI.
        /// </summary>
        private void ShowMetaStartup(int requestedLevelIndex, bool isEditorPreview)
        {
            InitializeMetaSystems();
            BuildMetaUi();
            int levelCount = levelManager != null ? levelManager.LevelCount : 0;
            if (levelCount <= 0)
            {
                return;
            }

            int safeLevelIndex = Mathf.Clamp(requestedLevelIndex, 0, levelCount - 1);
            LoadLevel(safeLevelIndex, !isEditorPreview);
            ApplyMetaLevelPresentation(safeLevelIndex);

            if (isEditorPreview || metaProgress == null || metaCatalog == null)
            {
                CloseMetaToGameplay();
                return;
            }

            CatMetaChapterDefinition firstChapter = metaCatalog.GetChapter(0);
            string firstIntroId = CatMetaStoryIds.Intro(firstChapter.Id);
            if (!metaProgress.HasViewedStory(firstIntroId))
            {
                ShowFirstRunStory();
            }
            else
            {
                OpenRoomHub();
            }
        }

        /// <summary>Records an idempotent first clear and returns the actual coin delta.</summary>
        private int RecordMetaFirstClear(int reward)
        {
            if (levelNavigationTesting)
            {
                metaLastAwardedCoins = 0;
                return 0;
            }

            InitializeMetaSystems();
            if (metaProgress == null)
            {
                metaLastAwardedCoins = 0;
                return 0;
            }

            CatMetaOperationResult result = metaProgress.RecordFirstClear(levelIndex, reward, ref coins);
            metaLastAwardedCoins = Mathf.Max(0, result.CoinDelta);
            if (coinText != null)
            {
                coinText.text = coins.ToString();
            }

            LogMetaEvent("level_first_clear",
                "level=" + levelIndex
                + " rewardRequested=" + reward
                + " coinDelta=" + result.CoinDelta
                + " status=" + result.Status);
            return metaLastAwardedCoins;
        }

        /// <summary>Overlays decoration/trust milestones on the existing win panel.</summary>
        private void ConfigureMetaWinPresentation(int awardedCoins)
        {
            metaLastAwardedCoins = Mathf.Max(0, awardedCoins);
            if (winRewardText != null)
            {
                winRewardText.text = awardedCoins > 0
                    ? "+" + awardedCoins + " coins • first clear"
                    : "First-clear reward already claimed";
            }

            if (metaCatalog == null || metaProgress == null)
            {
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(levelIndex);
            int chapterLevel = (levelIndex - chapter.FirstLevelIndex) + 1;
            CatMetaDecorationDefinition revealedDecoration = FindDecorationAtMilestone(chapter, chapterLevel);
            bool canDecorateNow = awardedCoins > 0 && revealedDecoration != null;

            if (awardedCoins <= 0)
            {
                if (winUnlockText != null)
                {
                    winUnlockText.gameObject.SetActive(false);
                }

                if (winCatImage != null)
                {
                    winCatImage.gameObject.SetActive(false);
                }
            }

            if (revealedDecoration != null && awardedCoins > 0)
            {
                if (winUnlockText != null)
                {
                    winUnlockText.gameObject.SetActive(true);
                    winUnlockText.text = revealedDecoration.DisplayName + " unlocked for " + chapter.CatName;
                }

                if (winCatImage != null)
                {
                    winCatImage.gameObject.SetActive(true);
                    Sprite decorationSprite = LoadMetaSprite(revealedDecoration.SpriteResourcePath);
                    winCatImage.sprite = decorationSprite != null
                        ? decorationSprite
                        : CatPortrait(CatMood.Happy, chapter.CatPortraitIndex);
                }
            }
            else if (chapterLevel == 5 && awardedCoins > 0)
            {
                if (winUnlockText != null)
                {
                    winUnlockText.gameObject.SetActive(true);
                    winUnlockText.text = chapter.CatName + " is beginning to trust this home";
                }

                if (winCatImage != null)
                {
                    winCatImage.gameObject.SetActive(true);
                    winCatImage.sprite = CatPortrait(CatMood.Happy, chapter.CatPortraitIndex);
                }
            }

            if (metaDecorateNowButton != null)
            {
                metaDecorateNowButton.gameObject.SetActive(canDecorateNow && chapterLevel < CatMetaCatalog.LevelsPerChapter);
            }

            if (metaNextLevelButton != null)
            {
                RectTransform nextRect = metaNextLevelButton.transform as RectTransform;
                bool splitButtons = metaDecorateNowButton != null && metaDecorateNowButton.gameObject.activeSelf;
                SetRect(nextRect,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    splitButtons ? new Vector2(-155f, 42f) : new Vector2(0f, 42f),
                    splitButtons ? new Vector2(280f, 76f) : new Vector2(390f, 76f));
            }

            if (metaNextLevelButtonText != null)
            {
                metaNextLevelButtonText.text = chapterLevel == CatMetaCatalog.LevelsPerChapter
                    && !metaProgress.IsChapterComplete(chapter.Index)
                    ? "Decorate Room"
                    : "Next Level";
            }
        }

        /// <summary>Routes chapter endings through the furnishing gate.</summary>
        private void LoadNextLevelThroughMetaGate()
        {
            if (metaCatalog == null || metaProgress == null)
            {
                LoadNextLevel();
                return;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(levelIndex);
            int chapterLevel = (levelIndex - chapter.FirstLevelIndex) + 1;
            if (winOverlay != null)
            {
                winOverlay.gameObject.SetActive(false);
            }

            string trustId = CatMetaStoryIds.TrustMilestone(chapter.Id);
            if (chapterLevel == 5
                && metaLastAwardedCoins > 0
                && !metaProgress.HasViewedStory(trustId))
            {
                EnterMetaOverlay();
                MarkMetaStoryViewed(trustId, chapter.Index, "trust");
                ShowStoryCard(
                    chapter.CatName + " TRUSTS YOU",
                    chapter.CatName + " is starting to relax. Every puzzle and every caring choice makes this room feel safer.",
                    "Keep Helping",
                    () => ContinueAfterMetaWin(chapter, chapterLevel),
                    CatPortrait(CatMood.Happy, chapter.CatPortraitIndex));
                return;
            }

            ContinueAfterMetaWin(chapter, chapterLevel);
        }

        private void ContinueAfterMetaWin(CatMetaChapterDefinition chapter, int chapterLevel)
        {
            if (chapterLevel >= CatMetaCatalog.LevelsPerChapter)
            {
                if (!metaProgress.IsChapterComplete(chapter.Index))
                {
                    metaRequiresRoomCompletion = true;
                    metaCurrentChapterIndex = chapter.Index;
                    OpenRoomDetail(chapter.Index);
                    ShowMetaStatus("The last treasure is ready. Place all five items to finish the room!");
                    return;
                }

                if (chapter.Index >= metaCatalog.Chapters.Count - 1)
                {
                    OpenRoomHub();
                    return;
                }

                PlayMetaLevel(metaCatalog.GetChapter(chapter.Index + 1).FirstLevelIndex);
                return;
            }

            PlayMetaLevel(levelIndex + 1);
        }

        private void RewireMetaNavigationButtons()
        {
            Transform backTransform = root != null ? root.Find("Back") : null;
            if (backTransform == null && root != null)
            {
                backTransform = root.Find("Rooms");
            }
            Button backButton = backTransform != null ? backTransform.GetComponent<Button>() : null;
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(PlayButtonSound);
                backButton.onClick.AddListener(OpenRoomHub);
            }

            Transform nextTransform = winPanel != null ? winPanel.Find("Next Level") : null;
            metaNextLevelButton = nextTransform != null ? nextTransform.GetComponent<Button>() : null;
            if (metaNextLevelButton != null)
            {
                metaNextLevelButton.onClick.RemoveAllListeners();
                metaNextLevelButton.onClick.AddListener(PlayButtonSound);
                metaNextLevelButton.onClick.AddListener(LoadNextLevelThroughMetaGate);
                metaNextLevelButtonText = metaNextLevelButton.GetComponentInChildren<Text>(true);

                metaDecorateNowButton = CreateMetaButton(
                    winPanel,
                    "Decorate Now",
                    () =>
                    {
                        if (winOverlay != null)
                        {
                            winOverlay.gameObject.SetActive(false);
                        }

                        OpenRoomDetail(metaCatalog.GetChapterForLevel(levelIndex).Index);
                    },
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(165f, 42f), new Vector2(300f, 76f),
                    new Color(0.96f, 0.56f, 0.48f, 1f), 24);
                metaDecorateNowButton.gameObject.SetActive(false);
            }
        }

        private void RenderRoomHub()
        {
            ClearMetaChildren(metaHubPage);
            metaStatusText = null;

            RectTransform header = CreatePanel(metaHubPage, "Forever Home Header", new Color(1f, 0.96f, 0.88f, 0.96f));
            SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-24f, 190f));
            StyleCreamPanel(header.GetComponent<Image>(), 0.12f);

            Text title = CreateText(header, "FOREVER HOME", 48, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-300f, 64f));
            Text subtitle = CreateText(header, "10 rooms  •  100 tiny victories", 24, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -91f), new Vector2(-200f, 40f));

            CreateIconButton(header, "Return to Puzzle", closeIconSprite, CloseMetaToGameplay,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(70f, 70f));
            CreateMetaCoinBadge(header, new Vector2(-26f, -28f));

            RectTransform viewport = CreatePanel(metaHubPage, "Chapter Scroll", new Color(1f, 1f, 1f, 0.04f));
            SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(-42f, -414f));
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.decelerationRate = 0.14f;

            RectTransform content = CreatePanel(viewport, "Chapters", new Color(1f, 1f, 1f, 0f));
            SetRect(content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 1908f));
            content.GetComponent<Image>().raycastTarget = false;
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(22, 22, 22, 22);
            grid.cellSize = new Vector2(460f, 344f);
            grid.spacing = new Vector2(24f, 26f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            scroll.viewport = viewport;
            scroll.content = content;

            for (int i = 0; i < metaCatalog.Chapters.Count; i++)
            {
                BuildHubChapterCard(content, i);
            }

            int recommendedLevel = GetRecommendedLevelIndex();
            CatMetaChapterDefinition recommendedChapter = metaCatalog.GetChapterForLevel(recommendedLevel);
            bool needsDecorating = AreAllChapterLevelsCleared(recommendedChapter)
                && !metaProgress.IsChapterComplete(recommendedChapter.Index);
            string continueLabel = needsDecorating
                ? "Finish " + recommendedChapter.Title
                : "Continue • Level " + (recommendedLevel + 1);
            CreateMetaButton(metaHubPage, continueLabel,
                () =>
                {
                    if (needsDecorating)
                    {
                        OpenRoomDetail(recommendedChapter.Index);
                    }
                    else
                    {
                        PlayMetaLevel(recommendedLevel);
                    }
                },
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 44f), new Vector2(610f, 88f), TargetDeepColor, 29);

            metaStatusText = CreateText(metaHubPage, "Choose a room or continue your rescue story.", 21, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(metaStatusText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 9f), new Vector2(-80f, 30f));
        }

        private void BuildHubChapterCard(RectTransform content, int chapterIndex)
        {
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            bool unlocked = metaProgress.IsChapterUnlocked(chapterIndex);
            bool complete = metaProgress.IsChapterComplete(chapterIndex);
            int installed = metaProgress.InstalledDecorationCount(chapterIndex);

            RectTransform card = CreatePanel(content, "Chapter " + (chapterIndex + 1),
                unlocked ? new Color(1f, 0.98f, 0.93f, 0.99f) : new Color(0.82f, 0.8f, 0.77f, 0.98f));
            StyleCreamPanel(card.GetComponent<Image>(), 0.14f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            int capturedIndex = chapterIndex;
            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(() =>
            {
                if (metaProgress.IsChapterUnlocked(capturedIndex))
                {
                    OpenRoomDetail(capturedIndex);
                }
                else
                {
                    ShowMetaStatus("Complete the room before this one to unlock it.");
                }
            });

            Image thumb = CreateImage(card, "Room Thumbnail", unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.78f));
            Sprite thumbnailSprite = LoadMetaSprite(chapter.ThumbnailResourcePath, false);
            if (thumbnailSprite != null)
            {
                thumb.sprite = thumbnailSprite;
                thumb.preserveAspect = false;
            }
            else
            {
                thumb.sprite = roundedBoxSprite;
                thumb.color = unlocked
                    ? new Color(0.63f, 0.82f, 0.76f, 1f)
                    : new Color(0.52f, 0.52f, 0.5f, 1f);
                Image portrait = CreateImage(thumb.rectTransform, "Cat Portrait", Color.white);
                portrait.sprite = CatPortrait(unlocked ? CatMood.Happy : CatMood.Neutral, chapter.CatPortraitIndex);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                SetCenteredChild(portrait.rectTransform, Vector2.zero, new Vector2(132f, 132f));
            }

            thumb.raycastTarget = false;
            SetRect(thumb.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 177f));

            Text number = CreateText(card, "ROOM " + (chapterIndex + 1), 18, FontStyle.Bold, TextAnchor.MiddleLeft,
                complete ? TargetDeepColor : SoftInkColor);
            SetRect(number.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -190f), new Vector2(-36f, 28f));
            Text title = CreateText(card, unlocked ? chapter.Title : "Locked Room", 27, FontStyle.Bold, TextAnchor.MiddleLeft, InkColor);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 19;
            title.resizeTextMaxSize = 27;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -224f), new Vector2(-36f, 38f));
            Text progress = CreateText(card,
                unlocked ? chapter.CatName + "  •  " + installed + "/5 treasures" : "Finish Room " + chapterIndex,
                19, FontStyle.Bold, TextAnchor.MiddleLeft, unlocked ? SoftInkColor : new Color(0.42f, 0.4f, 0.38f, 1f));
            SetRect(progress.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -263f), new Vector2(-36f, 30f));

            for (int step = 0; step < CatMetaCatalog.LevelsPerChapter; step++)
            {
                int absoluteLevel = chapter.FirstLevelIndex + step;
                bool cleared = metaProgress.IsLevelFirstCleared(absoluteLevel);
                bool selectable = IsMetaLevelSelectable(absoluteLevel);
                Image paw = CreateImage(card, "Paw Step " + (step + 1),
                    cleared ? GoldColor : selectable ? TargetDeepColor : new Color(0.54f, 0.5f, 0.46f, 0.32f));
                paw.sprite = pawSprite;
                paw.raycastTarget = false;
                SetRect(paw.rectTransform,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2((step - 4.5f) * 38f, 22f), new Vector2(27f, 27f));
            }

            Text state = CreateText(card, complete ? "COMPLETE" : unlocked ? installed + "/5" : "LOCKED",
                16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            RectTransform badge = CreatePanel(card, "State Badge",
                complete ? TargetDeepColor : unlocked ? new Color(0.96f, 0.56f, 0.48f, 0.96f) : new Color(0.38f, 0.36f, 0.35f, 0.85f));
            UseRoundedSprite(badge.GetComponent<Image>());
            SetRect(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -188f), new Vector2(94f, 30f));
            state.transform.SetParent(badge, false);
            Stretch(state.rectTransform);
        }

        private void RenderRoomDetail(int chapterIndex)
        {
            ClearMetaChildren(metaRoomPage);
            metaStatusText = null;
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);

            Image roomBackground = CreateImage(metaRoomPage, "Room Background", new Color(0.92f, 0.88f, 0.82f, 1f));
            Stretch(roomBackground.rectTransform);
            Sprite roomSprite = LoadMetaSprite(chapter.BackgroundResourcePath);
            if (roomSprite != null)
            {
                roomBackground.sprite = roomSprite;
                roomBackground.color = Color.white;
                roomBackground.preserveAspect = false;
            }
            else
            {
                roomBackground.sprite = GetThemeBackgroundSprite(activeTheme);
            }

            roomBackground.raycastTarget = false;
            BuildRoomDecorationStage(metaRoomPage, chapter);

            RectTransform header = CreatePanel(metaRoomPage, "Room Header", new Color(1f, 0.96f, 0.88f, 0.93f));
            SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-18f, 214f));
            StyleCreamPanel(header.GetComponent<Image>(), 0.12f);
            CreateIconButton(header, "Back to Rooms", backIconSprite, OpenRoomHub,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -22f), new Vector2(70f, 70f));
            CreateMetaCoinBadge(header, new Vector2(-22f, -25f));

            Text title = CreateText(header, chapter.Title, 40, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 26;
            title.resizeTextMaxSize = 40;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-330f, 58f));
            Text progress = CreateText(header,
                chapter.CatName + "  •  " + metaProgress.InstalledDecorationCount(chapterIndex) + "/5 placed",
                22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(progress.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -79f), new Vector2(-220f, 36f));
            BuildRoomLevelTrack(header, chapter);

            RectTransform rail = CreatePanel(metaRoomPage, "Treasure Rail", new Color(1f, 0.96f, 0.9f, 0.96f));
            SetRect(rail, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(-18f, 362f));
            StyleCreamPanel(rail.GetComponent<Image>(), 0.16f);
            metaStatusText = CreateText(rail, "Buy a treasure, then tap its glowing place in the room.", 20, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(metaStatusText.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -11f), new Vector2(-72f, 40f));

            for (int i = 0; i < chapter.Decorations.Count; i++)
            {
                BuildDecorationCard(rail, chapter.Decorations[i], i);
            }
        }

        private void BuildRoomLevelTrack(RectTransform header, CatMetaChapterDefinition chapter)
        {
            for (int step = 0; step < CatMetaCatalog.LevelsPerChapter; step++)
            {
                int absoluteLevel = chapter.FirstLevelIndex + step;
                bool cleared = metaProgress.IsLevelFirstCleared(absoluteLevel);
                bool selectable = IsMetaLevelSelectable(absoluteLevel);
                RectTransform marker = CreatePanel(header, "Level " + (absoluteLevel + 1),
                    cleared ? GoldColor : selectable ? TargetDeepColor : new Color(0.56f, 0.52f, 0.48f, 0.42f));
                marker.GetComponent<Image>().sprite = circleSprite;
                SetRect(marker,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2((step - 4.5f) * 88f, 22f), new Vector2(61f, 61f));
                Button button = marker.gameObject.AddComponent<Button>();
                button.targetGraphic = marker.GetComponent<Image>();
                int capturedLevel = absoluteLevel;
                button.onClick.AddListener(PlayButtonSound);
                button.onClick.AddListener(() =>
                {
                    if (IsMetaLevelSelectable(capturedLevel))
                    {
                        PlayMetaLevel(capturedLevel);
                    }
                    else
                    {
                        ShowMetaStatus("Clear the previous paw step first.");
                    }
                });
                Image paw = CreateImage(marker, "Paw", Color.white);
                paw.sprite = pawSprite;
                paw.preserveAspect = true;
                paw.raycastTarget = false;
                SetCenteredChild(paw.rectTransform, new Vector2(0f, 7f), new Vector2(30f, 30f));
                Text number = CreateText(marker, (step + 1).ToString(), 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                SetRect(number.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -17f), new Vector2(0f, 23f));
                number.raycastTarget = false;
            }
        }

        private void BuildRoomDecorationStage(RectTransform page, CatMetaChapterDefinition chapter)
        {
            List<CatMetaDecorationDefinition> sorted = new List<CatMetaDecorationDefinition>();
            for (int i = 0; i < chapter.Decorations.Count; i++)
            {
                sorted.Add(chapter.Decorations[i]);
            }

            sorted.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            for (int i = 0; i < sorted.Count; i++)
            {
                CatMetaDecorationDefinition decoration = sorted[i];
                bool installed = metaProgress.IsInstalled(decoration.Id);
                bool selectedStored = metaProgress.IsOwned(decoration.Id)
                    && !installed
                    && string.Equals(metaSelectedDecorationId, decoration.Id, StringComparison.Ordinal);
                if (!installed && !selectedStored)
                {
                    continue;
                }

                RectTransform visual = CreatePanel(page, "Placed " + decoration.DisplayName,
                    selectedStored ? new Color(1f, 0.83f, 0.3f, 0.34f) : new Color(1f, 1f, 1f, 0f));
                Image hitImage = visual.GetComponent<Image>();
                if (selectedStored)
                {
                    hitImage.sprite = circleSprite;
                    AddSoftOutline(hitImage, new Color(1f, 0.86f, 0.35f, 0.95f), new Vector2(4f, -4f));
                }
                else
                {
                    hitImage.raycastTarget = false;
                }

                SetRect(visual,
                    decoration.Hotspot, decoration.Hotspot, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(
                        Mathf.Max(90f, decoration.Size.x * ReferenceWidth),
                        Mathf.Max(90f, decoration.Size.y * ReferenceHeight)));

                Image art = CreateImage(visual, decoration.DisplayName + " Art",
                    selectedStored ? new Color(1f, 1f, 1f, 0.58f) : Color.white);
                Sprite sprite = LoadMetaSprite(decoration.SpriteResourcePath);
                art.sprite = sprite != null ? sprite : pawSprite;
                art.preserveAspect = true;
                art.raycastTarget = false;
                Stretch(art.rectTransform);
                art.rectTransform.offsetMin = new Vector2(8f, 8f);
                art.rectTransform.offsetMax = new Vector2(-8f, -8f);

                if (selectedStored)
                {
                    Button placeButton = visual.gameObject.AddComponent<Button>();
                    placeButton.targetGraphic = hitImage;
                    string capturedId = decoration.Id;
                    placeButton.onClick.AddListener(PlayButtonSound);
                    placeButton.onClick.AddListener(() => InstallMetaDecoration(capturedId));
                    Text place = CreateText(visual, "TAP TO PLACE", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                    SetRect(place.rectTransform,
                        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -8f), new Vector2(18f, 32f));
                    AddSoftOutline(place, InkColor, new Vector2(2f, -2f));
                    place.raycastTarget = false;
                }
            }
        }

        private void BuildDecorationCard(RectTransform rail, CatMetaDecorationDefinition decoration, int index)
        {
            bool unlocked = metaProgress.IsDecorationUnlocked(decoration.Id);
            bool owned = metaProgress.IsOwned(decoration.Id);
            bool installed = metaProgress.IsInstalled(decoration.Id);
            bool selected = !installed
                && owned
                && string.Equals(metaSelectedDecorationId, decoration.Id, StringComparison.Ordinal);
            Color cardColor = selected
                ? new Color(1f, 0.86f, 0.52f, 1f)
                : installed
                    ? new Color(0.72f, 0.9f, 0.75f, 0.99f)
                    : unlocked
                        ? new Color(1f, 0.98f, 0.93f, 0.99f)
                        : new Color(0.78f, 0.76f, 0.73f, 0.96f);

            RectTransform card = CreatePanel(rail, decoration.DisplayName, cardColor);
            SetRect(card,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2((index - 2f) * 202f, 24f), new Vector2(188f, 276f));
            StyleCreamPanel(card.GetComponent<Image>(), selected ? 0.2f : 0.1f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            string capturedId = decoration.Id;
            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(() => OnDecorationCardPressed(capturedId));

            Image icon = CreateImage(card, "Treasure", Color.white);
            Sprite sprite = LoadMetaSprite(decoration.SpriteResourcePath, false);
            icon.sprite = sprite != null ? sprite : pawSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = owned
                ? Color.white
                : unlocked
                    ? new Color(0.27f, 0.24f, 0.22f, 0.28f)
                    : new Color(0.25f, 0.25f, 0.25f, 0.2f);
            SetRect(icon.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -10f), new Vector2(-28f, 118f));

            Text name = CreateText(card, decoration.DisplayName, 19, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 14;
            name.resizeTextMaxSize = 19;
            SetRect(name.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -130f), new Vector2(-16f, 56f));

            string state;
            Color stateColor;
            if (!unlocked)
            {
                state = "CLEAR " + decoration.UnlockAfterChapterLevel;
                stateColor = SoftInkColor;
            }
            else if (!owned)
            {
                state = decoration.Cost + " COINS";
                stateColor = new Color(0.76f, 0.47f, 0.08f, 1f);
            }
            else if (installed)
            {
                state = "STORE";
                stateColor = TargetDeepColor;
            }
            else if (selected)
            {
                state = "TAP GLOW";
                stateColor = new Color(0.72f, 0.43f, 0.05f, 1f);
            }
            else
            {
                state = "SELECT";
                stateColor = TargetDeepColor;
            }

            Text status = CreateText(card, state, 17, FontStyle.Bold, TextAnchor.MiddleCenter, stateColor);
            SetRect(status.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 17f), new Vector2(-12f, 42f));
        }

        private void OnDecorationCardPressed(string decorationId)
        {
            CatMetaDecorationDefinition decoration = metaCatalog.GetDecoration(decorationId);
            if (!metaProgress.IsDecorationUnlocked(decorationId))
            {
                ShowMetaStatus("Clear level " + decoration.UnlockAfterChapterLevel + " in this room first.");
                LogMetaEvent("decoration_locked", "id=" + decorationId);
                return;
            }

            if (!metaProgress.IsOwned(decorationId))
            {
                CatMetaOperationResult purchase = metaProgress.TryPurchase(decorationId, ref coins);
                LogMetaEvent("decoration_purchase",
                    "id=" + decorationId
                    + " cost=" + decoration.Cost
                    + " status=" + purchase.Status
                    + " coins=" + purchase.CoinBalance);
                if (!purchase.Changed)
                {
                    ShowMetaStatus(purchase.Status == CatMetaOperationStatus.InsufficientCoins
                        ? "You need " + (decoration.Cost - coins) + " more coins."
                        : "That treasure is not available yet.");
                    return;
                }

                coins = purchase.CoinBalance;
                if (coinText != null)
                {
                    coinText.text = coins.ToString();
                }

                metaSelectedDecorationId = decorationId;
                RenderRoomDetail(decoration.ChapterIndex);
                ShowMetaStatus("Purchased! Tap the glowing spot in the room to place it.");
                return;
            }

            if (metaProgress.IsInstalled(decorationId))
            {
                CatMetaOperationResult stored = metaProgress.Store(decorationId);
                LogMetaEvent("decoration_store", "id=" + decorationId + " status=" + stored.Status);
                if (stored.Changed)
                {
                    metaSelectedDecorationId = decorationId;
                    RenderRoomDetail(decoration.ChapterIndex);
                    ShowMetaStatus("Stored. Tap its glowing spot whenever you want it back.");
                }

                return;
            }

            metaSelectedDecorationId = decorationId;
            RenderRoomDetail(decoration.ChapterIndex);
            ShowMetaStatus("Now tap the glowing place in the room.");
        }

        private void InstallMetaDecoration(string decorationId)
        {
            CatMetaDecorationDefinition decoration = metaCatalog.GetDecoration(decorationId);
            CatMetaOperationResult installed = metaProgress.TryInstall(decorationId);
            LogMetaEvent("decoration_install",
                "id=" + decorationId
                + " status=" + installed.Status
                + " chapterComplete=" + installed.ChapterJustCompleted);
            if (!installed.Changed)
            {
                ShowMetaStatus("That treasure could not be placed.");
                return;
            }

            metaSelectedDecorationId = null;
            PlayClip(snapClip);
            SelectFirstStoredDecoration(decoration.ChapterIndex);
            RenderRoomDetail(decoration.ChapterIndex);
            ShowMetaStatus(decoration.DisplayName + " placed • "
                + metaProgress.InstalledDecorationCount(decoration.ChapterIndex) + "/5");

            if (installed.ChapterJustCompleted)
            {
                metaRequiresRoomCompletion = false;
                ShowChapterCompletion(decoration.ChapterIndex);
            }
        }

        private void ShowFirstRunStory()
        {
            EnterMetaOverlay();
            metaHubPage.gameObject.SetActive(false);
            metaRoomPage.gameObject.SetActive(false);
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(0);
            ShowStoryCard(
                "A HOME FOR EVERY CAT",
                "Solve cozy block puzzles to earn coins. Use each victory to turn an empty room into a safe home for a rescued cat.",
                "Meet " + chapter.CatName,
                () =>
                {
                    MarkMetaStoryViewed(CatMetaStoryIds.Intro(chapter.Id), chapter.Index, "intro");
                    ShowStoryCard(
                        "MEET " + chapter.CatName.ToUpperInvariant(),
                        chapter.StartStory + "\n\nYour first puzzle can show " + chapter.CatName + " that this home is kind.",
                        "Start Level 1",
                        CloseMetaToGameplay,
                        CatPortrait(CatMood.Neutral, chapter.CatPortraitIndex));
                },
                CatPortrait(CatMood.Happy, chapter.CatPortraitIndex));
        }

        private void ShowChapterIntro(int chapterIndex, UnityAction onDismissed)
        {
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            MarkMetaStoryViewed(CatMetaStoryIds.Intro(chapter.Id), chapter.Index, "intro");
            ShowStoryCard(
                "MEET " + chapter.CatName.ToUpperInvariant(),
                chapter.StartStory + "\n\nComplete this room to help " + chapter.CatName + " feel at home.",
                "Begin the Chapter",
                onDismissed,
                CatPortrait(CatMood.Neutral, chapter.CatPortraitIndex));
        }

        private void ShowStoryCard(
            string titleValue,
            string bodyValue,
            string actionLabel,
            UnityAction action,
            Sprite portraitSprite)
        {
            BuildMetaUi();
            EnterMetaOverlay();
            ClearMetaChildren(metaStoryOverlay);
            metaStoryOverlay.gameObject.SetActive(true);
            metaStoryOverlay.SetAsLastSibling();

            RectTransform panel = CreatePanel(metaStoryOverlay, "Story Card", new Color(1f, 0.98f, 0.93f, 0.995f));
            SetCenteredChild(panel, Vector2.zero, new Vector2(770f, 890f));
            StyleCreamPanel(panel.GetComponent<Image>(), 0.24f);
            AddSoftOutline(panel.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.8f), new Vector2(3f, -3f));

            Image portrait = CreateImage(panel, "Story Cat", Color.white);
            portrait.sprite = portraitSprite != null ? portraitSprite : catHeadSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -58f), new Vector2(260f, 260f));

            Text title = CreateText(panel, titleValue, 43, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 29;
            title.resizeTextMaxSize = 43;
            SetRect(title.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -340f), new Vector2(-80f, 76f));
            Text body = CreateText(panel, bodyValue, 29, FontStyle.Normal, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(body.rectTransform,
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.59f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            CreateMetaButton(panel, actionLabel,
                () =>
                {
                    metaStoryOverlay.gameObject.SetActive(false);
                    if (action != null)
                    {
                        action.Invoke();
                    }
                },
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 56f), new Vector2(470f, 86f), TargetDeepColor, 27);

            if (!reducedMotion)
            {
                panel.localScale = Vector3.one * 0.9f;
                StartCoroutine(PopTransform(panel, 1.025f));
            }
        }

        private void ShowChapterCompletion(int chapterIndex)
        {
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            MarkMetaStoryViewed(CatMetaStoryIds.Completion(chapter.Id), chapter.Index, "completion");
            ClearMetaChildren(metaCompletionOverlay);
            metaCompletionOverlay.gameObject.SetActive(true);
            metaCompletionOverlay.SetAsLastSibling();

            RectTransform panel = CreatePanel(metaCompletionOverlay, "Celebration Card", new Color(1f, 0.98f, 0.92f, 0.995f));
            SetCenteredChild(panel, Vector2.zero, new Vector2(830f, 1040f));
            StyleCreamPanel(panel.GetComponent<Image>(), 0.26f);

            Text kicker = CreateText(panel,
                chapterIndex == metaCatalog.Chapters.Count - 1 ? "FOREVER HOME COMPLETE" : "ROOM COMPLETE",
                25, FontStyle.Bold, TextAnchor.MiddleCenter, TargetDeepColor);
            SetRect(kicker.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -35f), new Vector2(-60f, 42f));
            Text title = CreateText(panel, chapter.Title, 48, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(title.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -92f), new Vector2(-70f, 68f));
            Image portrait = CreateImage(panel, "Happy Cat", Color.white);
            portrait.sprite = CatPortrait(CatMood.Happy, chapter.CatPortraitIndex);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -190f), new Vector2(290f, 290f));

            Text story = CreateText(panel, chapter.CompletionStory, 30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(story.rectTransform,
                new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.56f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            bool finalRoom = chapterIndex >= metaCatalog.Chapters.Count - 1;
            if (!finalRoom)
            {
                CatMetaChapterDefinition next = metaCatalog.GetChapter(chapterIndex + 1);
                RectTransform preview = CreatePanel(panel, "Next Room Preview", new Color(0.52f, 0.78f, 0.71f, 0.18f));
                SetRect(preview,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 190f), new Vector2(590f, 142f));
                UseRoundedSprite(preview.GetComponent<Image>());
                Image nextCat = CreateImage(preview, "Next Cat", Color.white);
                nextCat.sprite = CatPortrait(CatMood.Neutral, next.CatPortraitIndex);
                nextCat.preserveAspect = true;
                nextCat.raycastTarget = false;
                SetRect(nextCat.rectTransform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(16f, 0f), new Vector2(116f, 116f));
                Text nextText = CreateText(preview,
                    "NEXT: " + next.Title + "\nMeet " + next.CatName,
                    23, FontStyle.Bold, TextAnchor.MiddleLeft, InkColor);
                SetRect(nextText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(62f, 0f), new Vector2(-160f, -18f));
            }

            CreateMetaButton(panel, finalRoom ? "See the Forever Home" : "Meet the Next Cat",
                () =>
                {
                    metaCompletionOverlay.gameObject.SetActive(false);
                    if (finalRoom)
                    {
                        OpenRoomHub();
                    }
                    else
                    {
                        int nextChapterIndex = chapterIndex + 1;
                        CatMetaChapterDefinition nextChapter = metaCatalog.GetChapter(nextChapterIndex);
                        ShowChapterIntro(nextChapterIndex, () => PlayMetaLevel(nextChapter.FirstLevelIndex));
                    }
                },
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 58f), new Vector2(520f, 88f), TargetDeepColor, 27);

            if (!reducedMotion)
            {
                panel.localScale = Vector3.one * 0.88f;
                StartCoroutine(PopTransform(panel, 1.035f));
            }

            LogMetaEvent("room_complete", "chapter=" + chapterIndex + " id=" + chapter.Id);
        }

        private void EnterMetaOverlay()
        {
            if (metaOverlay == null)
            {
                return;
            }

            if (!metaOverlayOwnsPause)
            {
                bool revealWasRunning = boardRevealRoutine != null;
                metaTimerWasRunning = timerRunning || revealWasRunning;
                metaOverlayOwnsPause = true;
                if (revealWasRunning)
                {
                    StopCoroutine(boardRevealRoutine);
                    boardRevealRoutine = null;
                    for (int i = 0; i < boardRevealCells.Count; i++)
                    {
                        if (boardRevealCells[i].Rect != null)
                        {
                            boardRevealCells[i].Rect.localScale = Vector3.one;
                        }
                    }
                }
            }

            StopLevelTimer();
            StopHint();
            CancelActiveDragToRest();
            SetTrayScrollEnabled(false);
            inputLocked = true;
            metaOverlay.gameObject.SetActive(true);
            metaOverlay.SetAsLastSibling();
        }

        private void PlayMetaLevel(int requestedLevelIndex)
        {
            if (levelManager == null || levelManager.LevelCount <= 0)
            {
                return;
            }

            int safeLevel = Mathf.Clamp(requestedLevelIndex, 0, levelManager.LevelCount - 1);
            if (metaProgress != null && !IsMetaLevelSelectable(safeLevel))
            {
                ShowMetaStatus("Clear the previous paw step first.");
                return;
            }

            if (metaOverlay != null)
            {
                metaOverlay.gameObject.SetActive(false);
            }

            if (winOverlay != null)
            {
                winOverlay.gameObject.SetActive(false);
            }

            metaOverlayOwnsPause = false;
            metaTimerWasRunning = false;
            metaRequiresRoomCompletion = false;
            LogMetaEvent("level_start", "level=" + safeLevel + " chapter=" + (safeLevel / CatMetaCatalog.LevelsPerChapter));
            LoadLevel(safeLevel, true);
            ApplyMetaLevelPresentation(safeLevel);
        }

        private bool IsMetaLevelSelectable(int absoluteLevelIndex)
        {
            if (metaCatalog == null || metaProgress == null
                || absoluteLevelIndex < 0
                || absoluteLevelIndex >= CatMetaCatalog.SupportedLevelCount)
            {
                return false;
            }

            CatMetaChapterDefinition chapter = metaCatalog.GetChapterForLevel(absoluteLevelIndex);
            if (!metaProgress.IsChapterUnlocked(chapter.Index))
            {
                return false;
            }

            return absoluteLevelIndex == chapter.FirstLevelIndex
                || metaProgress.IsLevelFirstCleared(absoluteLevelIndex)
                || metaProgress.IsLevelFirstCleared(absoluteLevelIndex - 1);
        }

        private int GetRecommendedLevelIndex()
        {
            int chapterIndex = Mathf.Clamp(metaProgress.HighestUnlockedChapter, 0, metaCatalog.Chapters.Count - 1);
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            for (int level = chapter.FirstLevelIndex; level <= chapter.LastLevelIndex; level++)
            {
                if (!metaProgress.IsLevelFirstCleared(level))
                {
                    return level;
                }
            }

            return chapter.LastLevelIndex;
        }

        private bool AreAllChapterLevelsCleared(CatMetaChapterDefinition chapter)
        {
            for (int level = chapter.FirstLevelIndex; level <= chapter.LastLevelIndex; level++)
            {
                if (!metaProgress.IsLevelFirstCleared(level))
                {
                    return false;
                }
            }

            return true;
        }

        private void SelectFirstStoredDecoration(int chapterIndex)
        {
            if (!string.IsNullOrEmpty(metaSelectedDecorationId)
                && metaCatalog.TryGetDecoration(metaSelectedDecorationId, out CatMetaDecorationDefinition selected)
                && selected.ChapterIndex == chapterIndex
                && metaProgress.IsOwned(selected.Id)
                && !metaProgress.IsInstalled(selected.Id))
            {
                return;
            }

            metaSelectedDecorationId = null;
            IReadOnlyList<CatMetaDecorationDefinition> decorations = metaCatalog.GetChapter(chapterIndex).Decorations;
            for (int i = 0; i < decorations.Count; i++)
            {
                if (metaProgress.IsOwned(decorations[i].Id) && !metaProgress.IsInstalled(decorations[i].Id))
                {
                    metaSelectedDecorationId = decorations[i].Id;
                    return;
                }
            }
        }

        private CatMetaDecorationDefinition FindDecorationAtMilestone(
            CatMetaChapterDefinition chapter,
            int chapterLevel)
        {
            for (int i = 0; i < chapter.Decorations.Count; i++)
            {
                if (chapter.Decorations[i].UnlockAfterChapterLevel == chapterLevel)
                {
                    return chapter.Decorations[i];
                }
            }

            return null;
        }

        private void MarkMetaStoryViewed(string storyId, int chapterIndex, string storyType)
        {
            if (metaProgress == null)
            {
                return;
            }

            CatMetaOperationResult result = metaProgress.MarkStoryViewed(storyId);
            if (result.Changed)
            {
                LogMetaEvent("story_view", "chapter=" + chapterIndex + " type=" + storyType + " id=" + storyId);
            }
        }

        private Button CreateMetaButton(
            RectTransform parent,
            string label,
            UnityAction action,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            Color background,
            int fontSize)
        {
            RectTransform rect = CreatePanel(parent, label, background);
            SetRect(rect, anchorMin, anchorMax, pivot, position, size);
            Image image = rect.GetComponent<Image>();
            StyleCreamPanel(image, 0.14f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, InkColor, 0.12f);
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.42f);
            button.colors = colors;
            button.onClick.AddListener(PlayButtonSound);
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            Text text = CreateText(rect, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(15, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(14f, 5f);
            text.rectTransform.offsetMax = new Vector2(-14f, -5f);
            text.raycastTarget = false;
            return button;
        }

        private void CreateMetaCoinBadge(RectTransform parent, Vector2 position)
        {
            RectTransform badge = CreatePanel(parent, "Meta Coins", new Color(1f, 0.86f, 0.56f, 0.98f));
            SetRect(badge,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                position, new Vector2(150f, 66f));
            StyleCreamPanel(badge.GetComponent<Image>(), 0.1f);
            Image icon = CreateImage(badge, "Coin", Color.white);
            icon.sprite = coinSprite;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(38f, 38f));
            Text value = CreateText(badge, coins.ToString(), 27, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            Stretch(value.rectTransform);
            value.rectTransform.offsetMin = new Vector2(48f, 0f);
            value.raycastTarget = false;
        }

        private void ShowMetaStatus(string message)
        {
            if (metaStatusText != null)
            {
                metaStatusText.text = message;
            }
        }

        private Sprite LoadMetaSprite(string resourcePath, bool logMissing = true)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            if (metaResourceSprites.TryGetValue(resourcePath, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                if (logMissing && missingMetaResources.Add(resourcePath))
                {
                    Debug.LogWarning("Optional meta art is missing at Resources/" + resourcePath
                        + ". A generated UI fallback will be used.");
                }

                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "Meta - " + resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
            metaResourceSprites[resourcePath] = sprite;
            return sprite;
        }

        private void CrossfadeGameplayBackground(Sprite nextSprite)
        {
            if (backgroundImage == null || nextSprite == null)
            {
                return;
            }

            Sprite previous = backgroundImage.sprite;
            backgroundImage.sprite = nextSprite;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
            if (reducedMotion || root == null || previous == null)
            {
                return;
            }

            Transform stale = root.Find("Meta Background Crossfade");
            if (stale != null)
            {
                stale.gameObject.SetActive(false);
                Destroy(stale.gameObject);
            }

            Image oldBackground = CreateImage(root, "Meta Background Crossfade", Color.white);
            oldBackground.sprite = previous;
            oldBackground.preserveAspect = false;
            oldBackground.raycastTarget = false;
            Stretch(oldBackground.rectTransform);
            oldBackground.transform.SetAsFirstSibling();
            StartCoroutine(FadeOutMetaBackground(oldBackground));
        }

        private IEnumerator FadeOutMetaBackground(Image oldBackground)
        {
            const float duration = 0.34f;
            float elapsed = 0f;
            while (elapsed < duration && oldBackground != null)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = oldBackground.color;
                color.a = 1f - Mathf.Clamp01(elapsed / duration);
                oldBackground.color = color;
                yield return null;
            }

            if (oldBackground != null)
            {
                Destroy(oldBackground.gameObject);
            }
        }

        private void ClearMetaChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void LogMetaEvent(string eventName, string values)
        {
            Debug.Log(MetaTelemetryPrefix + "event=" + eventName + " " + values);
        }
    }
}
