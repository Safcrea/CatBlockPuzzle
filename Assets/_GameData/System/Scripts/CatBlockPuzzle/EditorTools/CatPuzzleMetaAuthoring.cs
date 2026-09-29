#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Edit-mode construction of the room rescue pages: the overlay containers, the room
    /// hub with its chapter cards, each furnished room with its treasure rail and placement
    /// targets, and the story/completion cards. Runtime only changes their state.
    /// </summary>
    internal sealed class CatPuzzleMetaAuthoring
    {
        private readonly CatBlockPuzzleGame game;
        private readonly CatPuzzleUiAuthoring ui;
        private readonly Dictionary<string, Sprite> metaResourceSprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly HashSet<string> missingMetaResources = new HashSet<string>(StringComparer.Ordinal);

        public CatPuzzleMetaAuthoring(CatBlockPuzzleGame game, CatPuzzleUiAuthoring ui)
        {
            this.game = game;
            this.ui = ui;
        }

        private MetaProgressionView View => game.MetaView;
        private MetaProgressionController Meta => game.Meta;
        private CatMetaCatalog metaCatalog => Meta.Catalog;
        private CatMetaProgressStore metaProgress => Meta.Progress;
        private string metaSelectedDecorationId => Meta.SelectedDecorationId;
        private int coins => game.Progress.Coins;
        private bool reducedMotion => game.Settings.ReducedMotion;
        private RectTransform root => game.Hud.root;
        private RectTransform metaHubPage => View.metaHubPage;
        private RectTransform metaRoomPage => View.metaRoomPage;
        private RectTransform metaStoryOverlay => View.metaStoryOverlay;
        private RectTransform metaCompletionOverlay => View.metaCompletionOverlay;
        private Text metaStatusText { get => View.metaStatusText; set => View.metaStatusText = value; }
        private Sprite roundedBoxSprite => game.Assets.roundedBoxSprite;
        private Sprite circleSprite => game.Assets.circleSprite;
        private Sprite pawSprite => game.Assets.pawSprite;
        private Sprite coinSprite => game.Assets.coinSprite;
        private Sprite catHeadSprite => game.Assets.catHeadSprite;

        public void BuildMetaUiInEditor()
        {
            Meta.InitializeMetaSystems();
            if (game.MetaView == null) game.MetaView = game.gameObject.AddComponent<MetaProgressionView>();
            MetaProgressionView view = View;
            if (root == null || view.metaOverlay != null)
            {
                return;
            }

            view.metaOverlay = ui.CreatePanel(root, "Meta Overlay", PageColor);
            Stretch(view.metaOverlay);
            view.metaOverlay.SetAsLastSibling();
            view.metaOverlay.GetComponent<Image>().raycastTarget = true;

            view.metaHubPage = ui.CreatePanel(view.metaOverlay, "Room Hub", PageColor);
            Stretch(view.metaHubPage);
            view.metaRoomPage = ui.CreatePanel(view.metaOverlay, "Room Detail", PageColor);
            Stretch(view.metaRoomPage);
            view.metaStoryOverlay = ui.CreatePanel(view.metaOverlay, "Story Modal", new Color(0.1f, 0.08f, 0.07f, 0.62f));
            Stretch(view.metaStoryOverlay);
            view.metaCompletionOverlay = ui.CreatePanel(view.metaOverlay, "Room Complete Modal", new Color(0.1f, 0.08f, 0.07f, 0.68f));
            Stretch(view.metaCompletionOverlay);

            view.metaHubPage.gameObject.SetActive(false);
            view.metaRoomPage.gameObject.SetActive(false);
            view.metaStoryOverlay.gameObject.SetActive(false);
            view.metaCompletionOverlay.gameObject.SetActive(false);
            view.metaOverlay.gameObject.SetActive(false);

            RewireMetaNavigationButtons();
        }

        private void RewireMetaNavigationButtons()
        {
            MetaProgressionView view = View;
            Transform backTransform = root != null ? root.Find("Back") : null;
            if (backTransform == null && root != null)
            {
                backTransform = root.Find("Rooms");
            }
            Button backButton = backTransform != null ? backTransform.GetComponent<Button>() : null;
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(PuzzleAudio.PlayButton);
                backButton.onClick.AddListener(Meta.OpenRoomHub);
            }

            RectTransform winPanel = game.CompleteScreen != null ? game.CompleteScreen.panel : null;
            Transform nextTransform = winPanel != null ? winPanel.Find("Next Level") : null;
            view.metaNextLevelButton = nextTransform != null ? nextTransform.GetComponent<Button>() : null;
            if (view.metaNextLevelButton != null)
            {
                view.metaNextLevelButton.onClick.RemoveAllListeners();
                view.metaNextLevelButton.onClick.AddListener(PuzzleAudio.PlayButton);
                view.metaNextLevelButton.onClick.AddListener(Meta.LoadNextLevelThroughMetaGate);
                view.metaNextLevelButtonText = view.metaNextLevelButton.GetComponentInChildren<Text>(true);

                view.metaDecorateNowButton = CreateMetaButton(
                    winPanel,
                    "Decorate Now",
                    () =>
                    {
                        RectTransform winOverlay = game.CompleteScreen.RootRect;
                        if (winOverlay != null)
                        {
                            winOverlay.gameObject.SetActive(false);
                        }

                        Meta.OpenRoomDetail(metaCatalog.GetChapterForLevel(game.Session.levelIndex).Index);
                    },
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(165f, 42f), new Vector2(300f, 76f),
                    new Color(0.96f, 0.56f, 0.48f, 1f), 24);
                view.metaDecorateNowButton.gameObject.SetActive(false);
            }
        }

        public void RenderRoomHubInEditor()
        {
            ClearMetaChildren(metaHubPage);
            metaStatusText = null;

            RectTransform header = ui.CreatePanel(metaHubPage, "Forever Home Header", new Color(1f, 0.96f, 0.88f, 0.96f));
            SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-24f, 190f));
            ui.StyleCreamPanel(header.GetComponent<Image>(), 0.12f);

            Text title = ui.CreateText(header, "FOREVER HOME", 48, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-300f, 64f));
            Text subtitle = ui.CreateText(header, "10 rooms  •  100 tiny victories", 24, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -91f), new Vector2(-200f, 40f));

            ui.CreateIconButton(header, "Return to Puzzle", game.Assets.closeIconSprite, Meta.CloseMetaToGameplay,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(70f, 70f));
            CreateMetaCoinBadge(header, new Vector2(-26f, -28f));

            RectTransform viewport = ui.CreatePanel(metaHubPage, "Chapter Scroll", new Color(1f, 1f, 1f, 0.04f));
            SetRect(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(-42f, -414f));
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.decelerationRate = 0.14f;

            RectTransform content = ui.CreatePanel(viewport, "Chapters", new Color(1f, 1f, 1f, 0f));
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

            int recommendedLevel = Meta.GetRecommendedLevelIndex();
            string continueLabel = "Continue • Level " + (recommendedLevel + 1);
            CreateMetaButton(metaHubPage, continueLabel,
                () => Meta.PlayMetaLevel(recommendedLevel),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 44f), new Vector2(610f, 88f), TargetDeepColor, 29);

            metaStatusText = ui.CreateText(metaHubPage, "Choose a room or continue your rescue story.", 21, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
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

            RectTransform card = ui.CreatePanel(content, "Chapter " + (chapterIndex + 1),
                unlocked ? new Color(1f, 0.98f, 0.93f, 0.99f) : new Color(0.82f, 0.8f, 0.77f, 0.98f));
            ui.StyleCreamPanel(card.GetComponent<Image>(), 0.14f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            int capturedIndex = chapterIndex;
            button.onClick.AddListener(PuzzleAudio.PlayButton);
            button.onClick.AddListener(() => Meta.OpenRoomFromHub(capturedIndex));

            Image thumb = ui.CreateImage(card, "Room Thumbnail", unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.78f));
            Sprite thumbnailSprite = LoadMetaSpriteInEditor(chapter.ThumbnailResourcePath, false);
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
                Image portrait = ui.CreateImage(thumb.rectTransform, "Cat Portrait", Color.white);
                portrait.sprite = game.Mood.Portrait(unlocked ? CatMood.Happy : CatMood.Neutral, chapter.CatPortraitIndex);
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;
                SetCenteredChild(portrait.rectTransform, Vector2.zero, new Vector2(132f, 132f));
            }

            thumb.raycastTarget = false;
            SetRect(thumb.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 177f));

            Text number = ui.CreateText(card, "ROOM " + (chapterIndex + 1), 18, FontStyle.Bold, TextAnchor.MiddleLeft,
                complete ? TargetDeepColor : SoftInkColor);
            SetRect(number.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -190f), new Vector2(-36f, 28f));
            Text title = ui.CreateText(card, unlocked ? chapter.Title : "Locked Room", 27, FontStyle.Bold, TextAnchor.MiddleLeft, InkColor);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 19;
            title.resizeTextMaxSize = 27;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -224f), new Vector2(-36f, 38f));
            Text progress = ui.CreateText(card,
                unlocked ? chapter.CatName + "  •  " + installed + "/5 treasures" : "Finish Room " + chapterIndex,
                19, FontStyle.Bold, TextAnchor.MiddleLeft, unlocked ? SoftInkColor : new Color(0.42f, 0.4f, 0.38f, 1f));
            SetRect(progress.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -263f), new Vector2(-36f, 30f));

            for (int step = 0; step < CatMetaCatalog.LevelsPerChapter; step++)
            {
                int absoluteLevel = chapter.FirstLevelIndex + step;
                bool cleared = metaProgress.IsLevelFirstCleared(absoluteLevel);
                bool selectable = Meta.IsMetaLevelSelectable(absoluteLevel);
                Image paw = ui.CreateImage(card, "Paw Step " + (step + 1),
                    cleared ? GoldColor : selectable ? TargetDeepColor : new Color(0.54f, 0.5f, 0.46f, 0.32f));
                paw.sprite = pawSprite;
                paw.raycastTarget = false;
                SetRect(paw.rectTransform,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2((step - 4.5f) * 38f, 22f), new Vector2(27f, 27f));
            }

            Text state = ui.CreateText(card, complete ? "COMPLETE" : unlocked ? installed + "/5" : "LOCKED",
                16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            RectTransform badge = ui.CreatePanel(card, "State Badge",
                complete ? TargetDeepColor : unlocked ? new Color(0.96f, 0.56f, 0.48f, 0.96f) : new Color(0.38f, 0.36f, 0.35f, 0.85f));
            ui.UseRoundedSprite(badge.GetComponent<Image>());
            SetRect(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -188f), new Vector2(94f, 30f));
            state.transform.SetParent(badge, false);
            Stretch(state.rectTransform);
        }

        public void RenderRoomDetailInEditor(int chapterIndex)
        {
            ClearMetaChildren(metaRoomPage);
            metaStatusText = null;
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);

            Image roomBackground = ui.CreateImage(metaRoomPage, "Room Background", new Color(0.92f, 0.88f, 0.82f, 1f));
            Stretch(roomBackground.rectTransform);
            Sprite roomSprite = LoadMetaSpriteInEditor(chapter.BackgroundResourcePath);
            if (roomSprite != null)
            {
                roomBackground.sprite = roomSprite;
                roomBackground.color = Color.white;
                roomBackground.preserveAspect = false;
            }
            else
            {
                roomBackground.sprite = game.Theme.BackgroundSprite(game.Theme.ActiveTheme);
            }

            roomBackground.raycastTarget = false;
            BuildRoomDecorationStage(metaRoomPage, chapter);

            RectTransform header = ui.CreatePanel(metaRoomPage, "Room Header", new Color(1f, 0.96f, 0.88f, 0.93f));
            SetRect(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-18f, 214f));
            ui.StyleCreamPanel(header.GetComponent<Image>(), 0.12f);
            ui.CreateIconButton(header, "Back to Rooms", game.Assets.backIconSprite, Meta.OpenRoomHub,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, -22f), new Vector2(70f, 70f));
            CreateMetaCoinBadge(header, new Vector2(-22f, -25f));

            Text title = ui.CreateText(header, chapter.Title, 40, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 26;
            title.resizeTextMaxSize = 40;
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-330f, 58f));
            Text progress = ui.CreateText(header,
                chapter.CatName + "  •  " + metaProgress.InstalledDecorationCount(chapterIndex) + "/5 placed",
                22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(progress.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -79f), new Vector2(-220f, 36f));
            BuildRoomLevelTrack(header, chapter);

            RectTransform rail = ui.CreatePanel(metaRoomPage, "Treasure Rail", new Color(1f, 0.96f, 0.9f, 0.96f));
            SetRect(rail, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(-18f, 362f));
            ui.StyleCreamPanel(rail.GetComponent<Image>(), 0.16f);
            metaStatusText = ui.CreateText(rail, "Buy a treasure, then tap its glowing place in the room.", 20, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
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
                bool selectable = Meta.IsMetaLevelSelectable(absoluteLevel);
                RectTransform marker = ui.CreatePanel(header, "Level " + (absoluteLevel + 1),
                    cleared ? GoldColor : selectable ? TargetDeepColor : new Color(0.56f, 0.52f, 0.48f, 0.42f));
                marker.GetComponent<Image>().sprite = circleSprite;
                SetRect(marker,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2((step - 4.5f) * 88f, 22f), new Vector2(61f, 61f));
                Button button = marker.gameObject.AddComponent<Button>();
                button.targetGraphic = marker.GetComponent<Image>();
                int capturedLevel = absoluteLevel;
                button.onClick.AddListener(PuzzleAudio.PlayButton);
                button.onClick.AddListener(() => Meta.PlayLevelFromRoom(capturedLevel));
                Image paw = ui.CreateImage(marker, "Paw", Color.white);
                paw.sprite = pawSprite;
                paw.preserveAspect = true;
                paw.raycastTarget = false;
                SetCenteredChild(paw.rectTransform, new Vector2(0f, 7f), new Vector2(30f, 30f));
                Text number = ui.CreateText(marker, (step + 1).ToString(), 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
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
                // All placement targets are authored; runtime only changes their state.
                const bool selectedStored = true;

                RectTransform visual = ui.CreatePanel(page, "Placed " + decoration.DisplayName,
                    selectedStored ? new Color(1f, 0.83f, 0.3f, 0.34f) : new Color(1f, 1f, 1f, 0f));
                Image hitImage = visual.GetComponent<Image>();
                if (selectedStored)
                {
                    hitImage.sprite = circleSprite;
                    ui.AddSoftOutline(hitImage, new Color(1f, 0.86f, 0.35f, 0.95f), new Vector2(4f, -4f));
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

                Image art = ui.CreateImage(visual, decoration.DisplayName + " Art",
                    selectedStored ? new Color(1f, 1f, 1f, 0.58f) : Color.white);
                Sprite sprite = LoadMetaSpriteInEditor(decoration.SpriteResourcePath);
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
                    placeButton.onClick.AddListener(PuzzleAudio.PlayButton);
                    placeButton.onClick.AddListener(() => Meta.InstallMetaDecoration(capturedId));
                    Text place = ui.CreateText(visual, "TAP TO PLACE", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                    SetRect(place.rectTransform,
                        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -8f), new Vector2(18f, 32f));
                    ui.AddSoftOutline(place, InkColor, new Vector2(2f, -2f));
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

            RectTransform card = ui.CreatePanel(rail, decoration.DisplayName, cardColor);
            SetRect(card,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2((index - 2f) * 202f, 24f), new Vector2(188f, 276f));
            ui.StyleCreamPanel(card.GetComponent<Image>(), selected ? 0.2f : 0.1f);
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            string capturedId = decoration.Id;
            button.onClick.AddListener(PuzzleAudio.PlayButton);
            button.onClick.AddListener(() => Meta.OnDecorationCardPressed(capturedId));

            Image icon = ui.CreateImage(card, "Treasure", Color.white);
            Sprite sprite = LoadMetaSpriteInEditor(decoration.SpriteResourcePath, false);
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

            Text name = ui.CreateText(card, decoration.DisplayName, 19, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
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

            Text status = ui.CreateText(card, state, 17, FontStyle.Bold, TextAnchor.MiddleCenter, stateColor);
            SetRect(status.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 17f), new Vector2(-12f, 42f));
        }

        public void ShowStoryCardInEditor(
            string titleValue,
            string bodyValue,
            string actionLabel,
            UnityAction action,
            Sprite portraitSprite)
        {
            BuildMetaUiInEditor();
            Meta.EnterMetaOverlay();
            ClearMetaChildren(metaStoryOverlay);
            metaStoryOverlay.gameObject.SetActive(true);
            metaStoryOverlay.SetAsLastSibling();

            RectTransform panel = ui.CreatePanel(metaStoryOverlay, "Story Card", new Color(1f, 0.98f, 0.93f, 0.995f));
            SetCenteredChild(panel, Vector2.zero, new Vector2(770f, 890f));
            ui.StyleCreamPanel(panel.GetComponent<Image>(), 0.24f);
            ui.AddSoftOutline(panel.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.8f), new Vector2(3f, -3f));

            Image portrait = ui.CreateImage(panel, "Story Cat", Color.white);
            portrait.sprite = portraitSprite != null ? portraitSprite : catHeadSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -58f), new Vector2(260f, 260f));

            Text title = ui.CreateText(panel, titleValue, 43, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 29;
            title.resizeTextMaxSize = 43;
            SetRect(title.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -340f), new Vector2(-80f, 76f));
            Text body = ui.CreateText(panel, bodyValue, 29, FontStyle.Normal, TextAnchor.MiddleCenter, SoftInkColor);
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
                game.StartCoroutine(game.Fx.PopTransform(panel, 1.025f));
            }
        }

        public void ShowChapterCompletionInEditor(int chapterIndex)
        {
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);
            Meta.MarkMetaStoryViewed(CatMetaStoryIds.Completion(chapter.Id), chapter.Index, "completion");
            ClearMetaChildren(metaCompletionOverlay);
            metaCompletionOverlay.gameObject.SetActive(true);
            metaCompletionOverlay.SetAsLastSibling();

            RectTransform panel = ui.CreatePanel(metaCompletionOverlay, "Celebration Card", new Color(1f, 0.98f, 0.92f, 0.995f));
            SetCenteredChild(panel, Vector2.zero, new Vector2(830f, 1040f));
            ui.StyleCreamPanel(panel.GetComponent<Image>(), 0.26f);

            Text kicker = ui.CreateText(panel,
                chapterIndex == metaCatalog.Chapters.Count - 1 ? "FOREVER HOME COMPLETE" : "ROOM COMPLETE",
                25, FontStyle.Bold, TextAnchor.MiddleCenter, TargetDeepColor);
            SetRect(kicker.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -35f), new Vector2(-60f, 42f));
            Text title = ui.CreateText(panel, chapter.Title, 48, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(title.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -92f), new Vector2(-70f, 68f));
            Image portrait = ui.CreateImage(panel, "Happy Cat", Color.white);
            portrait.sprite = game.Mood.Portrait(CatMood.Happy, chapter.CatPortraitIndex);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -190f), new Vector2(290f, 290f));

            Text story = ui.CreateText(panel, chapter.CompletionStory, 30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(story.rectTransform,
                new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.56f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            bool finalRoom = chapterIndex >= metaCatalog.Chapters.Count - 1;
            if (!finalRoom)
            {
                CatMetaChapterDefinition next = metaCatalog.GetChapter(chapterIndex + 1);
                RectTransform preview = ui.CreatePanel(panel, "Next Room Preview", new Color(0.52f, 0.78f, 0.71f, 0.18f));
                SetRect(preview,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 190f), new Vector2(590f, 142f));
                ui.UseRoundedSprite(preview.GetComponent<Image>());
                Image nextCat = ui.CreateImage(preview, "Next Cat", Color.white);
                nextCat.sprite = game.Mood.Portrait(CatMood.Neutral, next.CatPortraitIndex);
                nextCat.preserveAspect = true;
                nextCat.raycastTarget = false;
                SetRect(nextCat.rectTransform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(16f, 0f), new Vector2(116f, 116f));
                Text nextText = ui.CreateText(preview,
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
                        Meta.OpenRoomHub();
                    }
                    else
                    {
                        int nextChapterIndex = chapterIndex + 1;
                        CatMetaChapterDefinition nextChapter = metaCatalog.GetChapter(nextChapterIndex);
                        Meta.ShowChapterIntro(nextChapterIndex, () => Meta.PlayMetaLevel(nextChapter.FirstLevelIndex));
                    }
                },
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 58f), new Vector2(520f, 88f), TargetDeepColor, 27);

            if (!reducedMotion)
            {
                panel.localScale = Vector3.one * 0.88f;
                game.StartCoroutine(game.Fx.PopTransform(panel, 1.035f));
            }

            Meta.LogMetaEvent("room_complete", "chapter=" + chapterIndex + " id=" + chapter.Id);
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
            RectTransform rect = ui.CreatePanel(parent, label, background);
            SetRect(rect, anchorMin, anchorMax, pivot, position, size);
            Image image = rect.GetComponent<Image>();
            ui.StyleCreamPanel(image, 0.14f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, InkColor, 0.12f);
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.42f);
            button.colors = colors;
            button.onClick.AddListener(PuzzleAudio.PlayButton);
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            Text text = ui.CreateText(rect, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
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
            RectTransform badge = ui.CreatePanel(parent, "Meta Coins", new Color(1f, 0.86f, 0.56f, 0.98f));
            SetRect(badge,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                position, new Vector2(150f, 66f));
            ui.StyleCreamPanel(badge.GetComponent<Image>(), 0.1f);
            Image icon = ui.CreateImage(badge, "Coin", Color.white);
            icon.sprite = coinSprite;
            icon.raycastTarget = false;
            SetRect(icon.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(38f, 38f));
            Text value = ui.CreateText(badge, coins.ToString(), 27, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            Stretch(value.rectTransform);
            value.rectTransform.offsetMin = new Vector2(48f, 0f);
            value.raycastTarget = false;
        }

        private Sprite LoadMetaSpriteInEditor(string resourcePath, bool logMissing = true)
        {
            CatPuzzleContentCatalog contentCatalog = game.Catalog;
            if (contentCatalog != null && metaCatalog != null)
            {
                foreach (var chapter in metaCatalog.Chapters)
                {
                    if (chapter.BackgroundResourcePath == resourcePath) return contentCatalog.roomBackgrounds[chapter.Index];
                    if (chapter.ThumbnailResourcePath == resourcePath) return contentCatalog.roomThumbnails[chapter.Index];
                    foreach (var decoration in chapter.Decorations)
                        if (decoration.SpriteResourcePath == resourcePath) return Meta.PreparedDecorationSprite(decoration.Id);
                }
            }
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

        private static void ClearMetaChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.DestroyImmediate(child);
            }
        }
    }
}
#endif
