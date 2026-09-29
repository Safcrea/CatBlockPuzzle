#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Edit-mode construction of the gameplay canvas: the HUD, board frame, tray, action
    /// bar, result overlays, and the per-level board cells and piece cards, plus the shared
    /// widget factories the meta authoring reuses.
    /// </summary>
    internal sealed class CatPuzzleUiAuthoring
    {
        private readonly CatBlockPuzzleGame game;

        public CatPuzzleUiAuthoring(CatBlockPuzzleGame game)
        {
            this.game = game;
        }

        private GameplayHudView Hud => game.Hud;
        private GameplayPresentationAssets Assets => game.Assets;
        private PuzzleSession Session => game.Session;

        private Sprite whiteSprite => Assets.whiteSprite;
        private Sprite roundedBoxSprite => Assets.roundedBoxSprite;
        private Sprite circleSprite => Assets.circleSprite;
        private Sprite coinSprite => Assets.coinSprite;
        private Sprite catHeadSprite => Assets.catHeadSprite;
        private Sprite mouthSprite => Assets.mouthSprite;
        private Sprite tailSprite => Assets.tailSprite;
        private Sprite pawSprite => Assets.pawSprite;
        private Sprite starSprite => Assets.starSprite;
        private Font defaultFont => Assets.defaultFont;

        public void BuildCanvas(CatPuzzleMetaAuthoring metaUi)
        {
            GameplayHudView hud = Hud;
            GameObject canvasObject = new GameObject("Cat Puzzle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            hud.canvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            Image backgroundImage = canvasObject.GetComponent<Image>();
            hud.backgroundImage = backgroundImage;
            backgroundImage.sprite = hud.defaultBackgroundSprite;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;

            RectTransform canvasRoot = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRoot);

            RectTransform root = CreatePanel(canvasRoot, "Safe Area", new Color(1f, 1f, 1f, 0f));
            hud.root = root;
            Stretch(root);
            root.GetComponent<Image>().raycastTarget = false;
            root.gameObject.AddComponent<SafeAreaFitter>();

            CreateIconButton(root, "Rooms", Assets.backIconSprite, game.Meta.OpenRoomHub, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(74f, 74f));

            Text levelText = CreateText(root, "Level 1", 44, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            hud.levelText = levelText;
            SetRect(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-50f, -22f), new Vector2(-600f, 68f));

            RectTransform coinPanel = CreatePanel(root, "Coin Counter", new Color(1f, 0.86f, 0.56f, 0.96f));
            SetRect(coinPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-200f, -24f), new Vector2(128f, 68f));
            StyleCreamPanel(coinPanel.GetComponent<Image>(), 0.12f);
            Image coinIcon = CreateImage(coinPanel, "Coin", Color.white);
            coinIcon.sprite = coinSprite;
            AddSoftShadow(coinIcon, new Vector2(0f, -2f), 0.18f);
            SetRect(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(38f, 38f));
            Text coinText = CreateText(coinPanel, "0", 29, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            hud.coinText = coinText;
            SetRect(coinText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), Vector2.zero);

            CreateIconButton(root, "Pause", Assets.pauseIconSprite, game.OpenPause, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-108f, -18f), new Vector2(74f, 74f));
            CreateIconButton(root, "Settings", Assets.settingsIconSprite, game.OpenSettings, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(74f, 74f));

            RectTransform objectivePanel = CreatePanel(root, "Level Objective", new Color(0.25f, 0.61f, 0.56f, 0.94f));
            hud.objectivePanel = objectivePanel;
            SetRect(objectivePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(520f, 50f));
            UseRoundedSprite(objectivePanel.GetComponent<Image>());
            AddSoftShadow(objectivePanel.GetComponent<Image>(), new Vector2(0f, -5f), 0.14f);
            Text objectiveText = CreateText(objectivePanel, "Fill the board", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            hud.objectiveText = objectiveText;
            objectiveText.resizeTextForBestFit = true;
            objectiveText.resizeTextMinSize = 16;
            objectiveText.resizeTextMaxSize = 22;
            Stretch(objectiveText.rectTransform);
            objectiveText.raycastTarget = false;

            RectTransform boardBackdrop = CreatePanel(root, "Board Frame", BoardFrameColor);
            Session.boardBackdrop = boardBackdrop;
            SetRect(boardBackdrop, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(806f, 806f));
            UseRoundedSprite(boardBackdrop.GetComponent<Image>());
            AddSoftShadow(boardBackdrop.GetComponent<Image>(), new Vector2(0f, -22f), 0.16f);
            AddSoftOutline(boardBackdrop.GetComponent<Image>(), BoardOutlineColor, new Vector2(2f, -2f));
            boardBackdrop.GetComponent<Image>().raycastTarget = false;
            AddBoardFrameDecorations(boardBackdrop);

            RectTransform boardRoot = CreatePanel(root, "Board", new Color(1f, 1f, 1f, 0f));
            Session.boardRoot = boardRoot;
            SetRect(boardRoot, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 720f));
            boardRoot.GetComponent<Image>().raycastTarget = false;

            RectTransform timerPanel = CreatePanel(root, "Timer Badge", new Color(1f, 247f / 255f, 228f / 255f, 0.97f));
            hud.timerPanel = timerPanel;
            SetRect(timerPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-142f, 500f), new Vector2(230f, TimerHeight + 8f));
            StyleCreamPanel(timerPanel.GetComponent<Image>(), 0.14f);
            Image timerPaw = CreateImage(timerPanel, "Timer Paw", new Color(0.88f, 0.45f, 0.4f, 0.74f));
            timerPaw.sprite = pawSprite;
            timerPaw.raycastTarget = false;
            SetRect(timerPaw.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(44f, 44f));
            Text timerText = CreateText(timerPanel, "2:00", 46, FontStyle.Bold, TextAnchor.MiddleCenter, TimerNormalColor);
            hud.timerText = timerText;
            Stretch(timerText.rectTransform);
            timerText.rectTransform.offsetMin = new Vector2(48f, 0f);
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddSoftShadow(timerText, new Vector2(0f, -3f), 0.18f);
            AddSoftOutline(timerText, new Color(1f, 1f, 1f, 0.78f), new Vector2(2f, -2f));
            timerText.raycastTarget = false;

            RectTransform starPanel = CreatePanel(root, "Star Goal", new Color(1f, 0.96f, 0.86f, 0.96f));
            hud.starPanel = starPanel;
            SetRect(starPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(145f, 500f), new Vector2(246f, TimerHeight + 8f));
            StyleCreamPanel(starPanel.GetComponent<Image>(), 0.12f);
            Image[] progressStars = hud.progressStars;
            for (int i = 0; i < progressStars.Length; i++)
            {
                progressStars[i] = CreateImage(starPanel, "Goal Star " + (i + 1), GoldColor);
                progressStars[i].sprite = starSprite;
                progressStars[i].raycastTarget = false;
                SetRect(progressStars[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 66f, 0f), new Vector2(54f, 54f));
            }

            RectTransform comboBadge = CreatePanel(root, "Combo Badge", new Color(0.99f, 0.56f, 0.5f, 0.96f));
            hud.comboBadge = comboBadge;
            SetRect(comboBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(310f, 58f));
            UseRoundedSprite(comboBadge.GetComponent<Image>());
            AddSoftShadow(comboBadge.GetComponent<Image>(), new Vector2(0f, -6f), 0.16f);
            Text comboText = CreateText(comboBadge, "3x  PURRFECT", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            hud.comboText = comboText;
            Stretch(comboText.rectTransform);
            comboBadge.gameObject.SetActive(false);

            RectTransform pieceLayer = CreatePanel(root, "Piece Layer", new Color(1f, 1f, 1f, 0f));
            Session.pieceLayer = pieceLayer;
            Stretch(pieceLayer);
            pieceLayer.GetComponent<Image>().raycastTarget = false;
            pieceLayer.SetAsLastSibling();

            RectTransform trayRoot = CreatePanel(root, "Shelf", TrayColor);
            Session.trayRoot = trayRoot;
            SetRect(trayRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(1000f, 292f));
            Image trayImage = trayRoot.GetComponent<Image>();
            Session.trayImage = trayImage;
            StyleCreamPanel(trayImage, 0.18f);
            trayImage.raycastTarget = true;

            ScrollRect trayScrollRect = trayRoot.gameObject.AddComponent<ScrollRect>();
            Session.trayScrollRect = trayScrollRect;
            trayScrollRect.horizontal = true;
            trayScrollRect.vertical = false;
            trayScrollRect.movementType = ScrollRect.MovementType.Elastic;
            trayScrollRect.inertia = true;
            trayScrollRect.decelerationRate = 0.12f;
            trayScrollRect.scrollSensitivity = 34f;

            RectTransform trayViewport = CreatePanel(trayRoot, "Viewport", new Color(1f, 1f, 1f, 0f));
            Session.trayViewport = trayViewport;
            SetRect(trayViewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-36f, -32f));
            trayViewport.GetComponent<Image>().raycastTarget = false;
            trayViewport.gameObject.AddComponent<RectMask2D>();

            RectTransform trayContent = CreatePanel(trayViewport, "Content", new Color(1f, 1f, 1f, 0f));
            Session.trayContent = trayContent;
            SetRect(trayContent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(960f, 0f));
            trayContent.GetComponent<Image>().raycastTarget = false;
            HorizontalLayoutGroup trayLayout = trayContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            Session.trayLayout = trayLayout;
            trayLayout.padding = new RectOffset(24, 24, 24, 24);
            trayLayout.spacing = 18f;
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.childControlWidth = true;
            trayLayout.childControlHeight = true;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = false;
            trayScrollRect.viewport = trayViewport;
            trayScrollRect.content = trayContent;

            RectTransform actionBar = CreatePanel(root, "Actions", new Color(1f, 1f, 1f, 0f));
            hud.actionBar = actionBar;
            SetRect(actionBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(520f, 92f));
            actionBar.GetComponent<Image>().raycastTarget = false;
            HorizontalLayoutGroup buttonLayout = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandHeight = true;
            CreateActionButton(actionBar, "Hint", Assets.hintIconSprite, game.RequestHint);
            CreateActionButton(actionBar, "Reset", Assets.resetIconSprite, () => game.Flow.ResetLevel());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BuildTestLevelControls();
#endif

            pieceLayer.SetAsLastSibling();

            RectTransform fxLayer = CreatePanel(root, "FX Layer", new Color(1f, 1f, 1f, 0f));
            hud.fxLayer = fxLayer;
            Stretch(fxLayer);
            fxLayer.GetComponent<Image>().raycastTarget = false;
            fxLayer.SetAsLastSibling();

            BuildWinOverlay();
            BuildFailOverlay();
            metaUi.BuildMetaUiInEditor();
        }

        private void BuildTestLevelControls()
        {
            GameplayHudView hud = Hud;
            hud.previousTestButton = CreateIconButton(
                hud.root,
                "Previous Test Level",
                Assets.backIconSprite,
                game.LoadPreviousLevel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 18f),
                new Vector2(72f, 72f));

            hud.nextTestButton = CreateIconButton(
                hud.root,
                "Next Test Level",
                Assets.backIconSprite,
                game.LoadNextLevel,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 18f),
                new Vector2(72f, 72f));

            Transform nextIcon = hud.nextTestButton.transform.Find("Next Test Level Icon");
            if (nextIcon != null)
            {
                nextIcon.localEulerAngles = new Vector3(0f, 0f, 180f);
            }

            game.Flow.UpdateTestLevelButtons();
        }

        private void BuildWinOverlay()
        {
            RectTransform winOverlay = CreatePanel(Hud.root, "Win Overlay", Color.clear);
            Stretch(winOverlay);
            winOverlay.SetAsLastSibling();
            if (game.CompleteScreen == null) game.CompleteScreen = winOverlay.gameObject.AddComponent<LevelCompleteScreen>();
            LevelCompleteScreen levelCompleteScreen = game.CompleteScreen;
            levelCompleteScreen.root = winOverlay.gameObject;

            RectTransform winPanel = CreatePanel(winOverlay, "Win Panel", Color.clear);
            levelCompleteScreen.panel = winPanel;
            Stretch(winPanel);
            AddReferenceLayer(winPanel, "Background", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/BG.png", 0, 0, 1080, 1920);
            AddReferenceLayer(winPanel, "Level Complete Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/Title.png", 195, 175, 691, 362);
            AddReferenceLayer(winPanel, "Cat Win Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/Cat.png", 239, 888, 527, 637);
            AddReferenceLayer(winPanel, "Coin Reward Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/CoinsBoard.png", 415, 786, 247, 104);

            levelCompleteScreen.sprites = new LevelCompleteScreen.StarSprites {
                blackAndWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/Black_&_White Star.png"),
                disabled = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/StarDisabled.png"),
                winStars = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/Stars_Win.png")
            };
            levelCompleteScreen.stars = new Image[3];
            int[] starX = { 286, 441, 596 };
            for (int i = 0; i < levelCompleteScreen.stars.Length; i++)
            {
                levelCompleteScreen.stars[i] = AddReferenceLayer(winPanel, "Result Star " + (i + 1), "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/StarDisabled.png", starX[i], 539, 209, 203);
            }

            CreateReferenceButton(winPanel, "Continue", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/Continue.png", 243, 1499, 594, 172, () => game.Meta.LoadNextLevelThroughMetaGate());

            levelCompleteScreen.CaptureExisting(winOverlay.gameObject);
            winOverlay.gameObject.SetActive(false);
        }

        private void BuildFailOverlay()
        {
            RectTransform failOverlay = CreatePanel(Hud.root, "Fail Overlay", Color.clear);
            Stretch(failOverlay);
            failOverlay.SetAsLastSibling();
            if (game.FailScreen == null) game.FailScreen = failOverlay.gameObject.AddComponent<LevelFailScreen>();
            LevelFailScreen levelFailScreen = game.FailScreen;
            levelFailScreen.root = failOverlay.gameObject;

            RectTransform failPanel = CreatePanel(failOverlay, "Fail Panel", Color.clear);
            levelFailScreen.panel = failPanel;
            Stretch(failPanel);
            AddReferenceLayer(failPanel, "Background", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/BG.png", 0, 0, 1080, 1920);
            AddReferenceLayer(failPanel, "Sad Cat Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/CatSad.png", 232, 723, 635, 527);
            AddReferenceLayer(failPanel, "So Close Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/So_close.png", 728, 645, 221, 212);
            AddReferenceLayer(failPanel, "Level Failed Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/title.png", 133, 234, 816, 208);
            AddReferenceLayer(failPanel, "Encouragement Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/Donotworry.png", 227, 1214, 621, 181);
            CreateReferenceButton(failPanel, "Retry", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/TRY.png", 225, 1403, 626, 176, () => game.Flow.ResetLevel());
            CreateReferenceButton(failPanel, "Skip Level", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/Skip.png", 152, 1620, 362, 113, game.LoadNextLevel);
            CreateReferenceButton(failPanel, "Home", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/HOME.png", 526, 1617, 354, 115, () => game.Meta.OpenRoomHub());

            levelFailScreen.CaptureExisting(failOverlay.gameObject);
            failOverlay.gameObject.SetActive(false);
        }

        private Image AddReferenceLayer(RectTransform parent, string name, string assetPath, float x, float y, float width, float height)
        {
            Image image = CreateImage(parent, name, Color.white);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x + (width * 0.5f) - 540f, 960f - y - (height * 0.5f)), new Vector2(width, height));
            return image;
        }

        private Button CreateReferenceButton(RectTransform parent, string name, string assetPath, float x, float y, float width, float height, UnityAction action)
        {
            Image image = AddReferenceLayer(parent, name, assetPath, x, y, width, height);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(PuzzleAudio.PlayButton);
            button.onClick.AddListener(action);
            return button;
        }

        public Button CreateIconButton(
            RectTransform parent,
            string name,
            Sprite icon,
            UnityAction action,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, pivot, position, size);
            Image background = gameObject.GetComponent<Image>();
            background.sprite = circleSprite;
            background.type = Image.Type.Simple;
            background.color = new Color(1f, 0.95f, 0.86f, 0.98f);
            AddSoftShadow(background, new Vector2(0f, -5f), 0.15f);

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(PuzzleAudio.PlayButton);
            button.onClick.AddListener(action);

            Image iconImage = CreateImage(rect, name + " Icon", InkColor);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            SetRect(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size * 0.48f);
            return button;
        }

        private void CreateActionButton(RectTransform parent, string label, Sprite icon, UnityAction action)
        {
            GameObject gameObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            Image image = gameObject.GetComponent<Image>();
            image.color = label == "Hint" ? new Color(0.35f, 0.72f, 0.67f, 0.97f) : new Color(0.96f, 0.56f, 0.48f, 0.97f);
            UseRoundedSprite(image);
            AddSoftShadow(image, new Vector2(0f, -6f), 0.16f);

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(PuzzleAudio.PlayButton);
            button.onClick.AddListener(action);

            Image iconImage = CreateImage(rect, label + " Icon", Color.white);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            SetRect(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(44f, 44f));
            Text text = CreateText(rect, label, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(64f, 0f);
            text.raycastTarget = false;
        }

        public void UseRoundedSprite(Image image) => RectLayout.UseRoundedSprite(image, roundedBoxSprite);

        public void StyleCreamPanel(Image image, float shadowAlpha)
        {
            Color color = image.color;
            UseRoundedSprite(image);
            image.color = color;
            AddSoftShadow(image, new Vector2(0f, -8f), shadowAlpha);
            AddSoftOutline(image, PanelOutlineColor, new Vector2(2f, -2f));
        }

        private void AddBoardFrameDecorations(RectTransform frame)
        {
            Image leftEar = CreateImage(frame, "Board Ear Left", BoardFrameColor);
            Image rightEar = CreateImage(frame, "Board Ear Right", BoardFrameColor);
            leftEar.sprite = catHeadSprite;
            rightEar.sprite = catHeadSprite;
            leftEar.raycastTarget = false;
            rightEar.raycastTarget = false;
            SetRect(leftEar.rectTransform, new Vector2(0.25f, 1f), new Vector2(0.25f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(86f, 58f));
            SetRect(rightEar.rectTransform, new Vector2(0.75f, 1f), new Vector2(0.75f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(86f, 58f));
            leftEar.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            rightEar.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            leftEar.transform.SetAsFirstSibling();
            rightEar.transform.SetAsFirstSibling();

            Text face = CreateText(frame, "w", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(74f / 255f, 46f / 255f, 42f / 255f, 0.54f));
            SetRect(face.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(62f, 28f));

            Image leftWhisker = CreateImage(frame, "Board Whisker Left", new Color(74f / 255f, 46f / 255f, 42f / 255f, 0.25f));
            Image rightWhisker = CreateImage(frame, "Board Whisker Right", new Color(74f / 255f, 46f / 255f, 42f / 255f, 0.25f));
            UseRoundedSprite(leftWhisker);
            UseRoundedSprite(rightWhisker);
            leftWhisker.raycastTarget = false;
            rightWhisker.raycastTarget = false;
            SetRect(leftWhisker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-48f, -28f), new Vector2(46f, 4f));
            SetRect(rightWhisker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(48f, -28f), new Vector2(46f, 4f));
            leftWhisker.rectTransform.localEulerAngles = new Vector3(0f, 0f, 8f);
            rightWhisker.rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
        }

        private void AddDragDots(RectTransform card)
        {
            RectTransform dots = CreatePanel(card, "Drag Dots", new Color(1f, 1f, 1f, 0f));
            dots.GetComponent<Image>().raycastTarget = false;
            SetRect(dots, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(76f, 14f));
            for (int i = 0; i < 3; i++)
            {
                Image dot = CreateImage(dots, "Dot", new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.78f));
                dot.sprite = circleSprite;
                dot.raycastTarget = false;
                SetRect(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 20f, 0f), new Vector2(8f, 8f));
            }
        }

        public RectTransform CreatePanel(RectTransform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            return gameObject.GetComponent<RectTransform>();
        }

        public Image CreateImage(RectTransform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            return image;
        }

        public Text CreateText(RectTransform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject gameObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = defaultFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public void AddSoftShadow(Graphic graphic, Vector2 distance, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.15f, 0.13f, 0.1f, alpha);
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        public void AddSoftOutline(Graphic graphic, Color color, Vector2 distance)
        {
            if (graphic == null)
            {
                return;
            }

            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        /// <summary>Builds the board cells for the session's active level into the bound board root.</summary>
        public void BuildBoardInEditor()
        {
            PuzzleSession session = Session;
            LevelDefinition activeLevel = session.activeLevel;
            session.boardCells.Clear();
            session.boardRevealCells.Clear();

            Vector2 targetMaxSize = game.Board.GetBoardTargetMaxSize();
            float levelAspect = (float)activeLevel.Cols / Mathf.Max(1, activeLevel.Rows);
            if (levelAspect >= targetMaxSize.x / targetMaxSize.y)
            {
                session.boardWidth = targetMaxSize.x;
                session.boardHeight = targetMaxSize.x / levelAspect;
            }
            else
            {
                session.boardHeight = targetMaxSize.y;
                session.boardWidth = targetMaxSize.y * levelAspect;
            }

            game.Board.ApplyGameplayLayout();

            session.boardCellWidth = (session.boardWidth - (BoardGap * (activeLevel.Cols - 1))) / activeLevel.Cols;
            session.boardCellHeight = (session.boardHeight - (BoardGap * (activeLevel.Rows - 1))) / activeLevel.Rows;
            if (session.placementAvailability.GetLength(0) != activeLevel.Rows || session.placementAvailability.GetLength(1) != activeLevel.Cols)
            {
                session.placementAvailability = new bool[activeLevel.Rows, activeLevel.Cols];
            }

            float boardCellWidth = session.boardCellWidth;
            float boardCellHeight = session.boardCellHeight;
            for (int row = 0; row < activeLevel.Rows; row++)
            {
                for (int col = 0; col < activeLevel.Cols; col++)
                {
                    Vector2Int coord = new Vector2Int(row, col);
                    bool active = activeLevel.ActiveCells.Contains(coord);
                    Image cellImage = CreateImage(session.boardRoot, "Cell " + row + "," + col, active ? TargetColor : Color.clear);
                    if (active)
                    {
                        UseRoundedSprite(cellImage);
                        AddSoftShadow(cellImage, new Vector2(0f, -5f), 0.1f);
                        AddSoftOutline(cellImage, BoardTileEdgeColor, new Vector2(2f, -2f));
                    }

                    cellImage.raycastTarget = false;
                    RectTransform rect = cellImage.rectTransform;
                    SetTopLeft(rect, game.Board.CellPosition(row, col), new Vector2(boardCellWidth, boardCellHeight));
                    Image preview = null;
                    if (active)
                    {
                        rect.localScale = Vector3.zero;
                        session.boardRevealCells.Add(new BoardRevealCell(rect, row + col));

                        Image shine = CreateImage(rect, "Cell Shine", new Color(1f, 1f, 1f, 0.28f));
                        UseRoundedSprite(shine);
                        shine.raycastTarget = false;
                        SetRect(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-boardCellWidth * 0.08f, boardCellHeight * 0.08f), new Vector2(boardCellWidth * 0.72f, boardCellHeight * 0.72f));

                        Image paw = CreateImage(rect, "Paw Print", new Color(241f / 255f, 241f / 255f, 244f / 255f, 0.58f));
                        paw.sprite = pawSprite;
                        paw.raycastTarget = false;
                        SetRect(paw.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(boardCellWidth * 0.48f, boardCellHeight * 0.48f));

                        preview = CreateImage(rect, "Cat Landing Preview", Color.white);
                        preview.preserveAspect = true;
                        preview.raycastTarget = false;
                        Stretch(preview.rectTransform);
                        preview.rectTransform.offsetMin = new Vector2(4f, 4f);
                        preview.rectTransform.offsetMax = new Vector2(-4f, -4f);
                        preview.gameObject.SetActive(false);
                    }

                    session.boardCells[coord] = new CellView(rect, cellImage, preview, cellImage.color);
                }
            }
        }

        /// <summary>Builds a tray card and cat visual for every piece of the session's active level.</summary>
        public void BuildPiecesInEditor()
        {
            PuzzleSession session = Session;
            LevelDefinition activeLevel = session.activeLevel;
            game.Tray.ConfigureTrayForPieceCount(activeLevel.Pieces.Length);

            for (int i = 0; i < activeLevel.Pieces.Length; i++)
            {
                PieceDefinition definition = activeLevel.Pieces[i];
                PieceState state = new PieceState(definition, PieceColors[i % PieceColors.Length]);
                state.AtlasIndex = i % 8;
                state.FloatPhase = i * 0.83f;
                state.Slot = CreatePanel(session.trayContent, definition.Name + " Slot", CardRestColor);
                state.SlotImage = state.Slot.GetComponent<Image>();
                StyleCreamPanel(state.SlotImage, 0.11f);
                state.SlotLayout = state.Slot.gameObject.AddComponent<LayoutElement>();
                game.Tray.ApplyTraySlotLayout(state.SlotLayout);
                AddDragDots(state.Slot);

                state.Rect = CreatePanel(state.Slot, definition.Name, new Color(1f, 1f, 1f, 0f));
                state.Rect.SetAsLastSibling();
                CreatePieceCells(state);
                game.Tray.AttachPieceToTray(state);
                session.pieces.Add(state);
            }

            game.Tray.RefreshTrayLayout(false);
            if (session.trayScrollRect != null)
            {
                session.trayScrollRect.horizontalNormalizedPosition = 0f;
            }
        }

        private void CreatePieceCells(PieceState state)
        {
            state.CellImages.Clear();
            state.CatViews.Clear();
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                RectTransform cellRect = CreatePanel(state.Rect, "Cat Cell", state.Color).GetComponent<RectTransform>();
                Image body = cellRect.GetComponent<Image>();
                Sprite portrait = game.Mood.Portrait(CatMood.Neutral, state.AtlasIndex);
                if (portrait != null)
                {
                    body.sprite = portrait;
                    body.type = Image.Type.Simple;
                    body.color = Color.white;
                    body.preserveAspect = true;
                    body.raycastTarget = false;
                    AddSoftShadow(body, new Vector2(0f, -5f), 0.22f);
                    state.CellImages.Add(body);
                    state.CatViews.Add(new CatCellView(cellRect, body));
                    continue;
                }

                body.sprite = catHeadSprite;
                body.type = Image.Type.Simple;
                body.raycastTarget = false;
                AddSoftShadow(body, new Vector2(0f, -4f), 0.18f);
                AddSoftOutline(body, new Color(1f, 1f, 1f, 0.78f), new Vector2(2f, -2f));

                Image leftEar = CreateImage(cellRect, "Ear Left", state.Color);
                Image rightEar = CreateImage(cellRect, "Ear Right", state.Color);
                Image leftInnerEar = CreateImage(cellRect, "Inner Ear Left", new Color(1f, 0.76f, 0.78f, 0.58f));
                Image rightInnerEar = CreateImage(cellRect, "Inner Ear Right", new Color(1f, 0.76f, 0.78f, 0.58f));
                Image highlight = CreateImage(cellRect, "Body Highlight", new Color(1f, 1f, 1f, 0.18f));
                Image leftEye = CreateImage(cellRect, "Eye Left", InkColor);
                Image rightEye = CreateImage(cellRect, "Eye Right", InkColor);
                Image nose = CreateImage(cellRect, "Nose", new Color(0.38f, 0.23f, 0.22f, 0.62f));
                Image mouth = CreateImage(cellRect, "Mouth", new Color(0.18f, 0.16f, 0.14f, 0.62f));
                Image leftCheek = CreateImage(cellRect, "Cheek Left", new Color(1f, 0.47f, 0.55f, 0.2f));
                Image rightCheek = CreateImage(cellRect, "Cheek Right", new Color(1f, 0.47f, 0.55f, 0.2f));
                Image leftWhiskerTop = CreateImage(cellRect, "Whisker Left Top", new Color(0.18f, 0.16f, 0.14f, 0.28f));
                Image leftWhiskerBottom = CreateImage(cellRect, "Whisker Left Bottom", new Color(0.18f, 0.16f, 0.14f, 0.24f));
                Image rightWhiskerTop = CreateImage(cellRect, "Whisker Right Top", new Color(0.18f, 0.16f, 0.14f, 0.28f));
                Image rightWhiskerBottom = CreateImage(cellRect, "Whisker Right Bottom", new Color(0.18f, 0.16f, 0.14f, 0.24f));
                Image foreheadStripe = CreateImage(cellRect, "Forehead Stripe", new Color(0.18f, 0.16f, 0.14f, 0.16f));
                Image tail = CreateImage(cellRect, "Tail", new Color(0.18f, 0.16f, 0.14f, 0.18f));

                foreach (Image part in new[] { leftEar, rightEar, leftInnerEar, rightInnerEar, highlight, leftEye, rightEye, nose, mouth,
                             leftCheek, rightCheek, leftWhiskerTop, leftWhiskerBottom, rightWhiskerTop, rightWhiskerBottom, foreheadStripe, tail })
                    part.raycastTarget = false;
                leftEar.sprite = catHeadSprite;
                rightEar.sprite = catHeadSprite;
                leftInnerEar.sprite = catHeadSprite;
                rightInnerEar.sprite = catHeadSprite;
                highlight.sprite = catHeadSprite;
                leftEar.type = Image.Type.Simple;
                rightEar.type = Image.Type.Simple;
                leftInnerEar.type = Image.Type.Simple;
                rightInnerEar.type = Image.Type.Simple;
                leftEye.sprite = circleSprite;
                rightEye.sprite = circleSprite;
                nose.sprite = circleSprite;
                mouth.sprite = mouthSprite;
                leftCheek.sprite = circleSprite;
                rightCheek.sprite = circleSprite;
                tail.sprite = tailSprite;

                state.CellImages.Add(body);
                state.CatViews.Add(new CatCellView(
                    cellRect,
                    leftEar.rectTransform,
                    rightEar.rectTransform,
                    leftInnerEar.rectTransform,
                    rightInnerEar.rectTransform,
                    highlight.rectTransform,
                    leftEye.rectTransform,
                    rightEye.rectTransform,
                    nose.rectTransform,
                    mouth.rectTransform,
                    leftCheek.rectTransform,
                    rightCheek.rectTransform,
                    leftWhiskerTop.rectTransform,
                    leftWhiskerBottom.rectTransform,
                    rightWhiskerTop.rectTransform,
                    rightWhiskerBottom.rectTransform,
                    foreheadStripe.rectTransform,
                    tail.rectTransform));
            }
        }
    }
}
#endif
