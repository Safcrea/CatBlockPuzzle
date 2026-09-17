#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using CatBlockPuzzle.KawaiiUI;
namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Cat Puzzle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;

            backgroundImage = canvasObject.GetComponent<Image>();
            backgroundImage.sprite = defaultBackgroundSprite;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;

            RectTransform canvasRoot = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRoot);

            root = CreatePanel(canvasRoot, "Safe Area", new Color(1f, 1f, 1f, 0f));
            Stretch(root);
            root.GetComponent<Image>().raycastTarget = false;
            root.gameObject.AddComponent<SafeAreaFitter>();


            CreateIconButton(root, "Rooms", backIconSprite, OpenRoomHub, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(74f, 74f));

            levelText = CreateText(root, "Level 1", 44, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-50f, -22f), new Vector2(-600f, 68f));

            RectTransform coinPanel = CreatePanel(root, "Coin Counter", new Color(1f, 0.86f, 0.56f, 0.96f));
            SetRect(coinPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-200f, -24f), new Vector2(128f, 68f));
            StyleCreamPanel(coinPanel.GetComponent<Image>(), 0.12f);
            Image coinIcon = CreateImage(coinPanel, "Coin", Color.white);
            coinIcon.sprite = coinSprite;
            AddSoftShadow(coinIcon, new Vector2(0f, -2f), 0.18f);
            SetRect(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(38f, 38f));
            coinText = CreateText(coinPanel, "0", 29, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(coinText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), Vector2.zero);

            CreateIconButton(root, "Pause", pauseIconSprite, OpenPause, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-108f, -18f), new Vector2(74f, 74f));
            CreateIconButton(root, "Settings", settingsIconSprite, OpenSettings, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(74f, 74f));

            objectivePanel = CreatePanel(root, "Level Objective", new Color(0.25f, 0.61f, 0.56f, 0.94f));
            SetRect(objectivePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(520f, 50f));
            UseRoundedSprite(objectivePanel.GetComponent<Image>());
            AddSoftShadow(objectivePanel.GetComponent<Image>(), new Vector2(0f, -5f), 0.14f);
            objectiveText = CreateText(objectivePanel, "Fill the board", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            objectiveText.resizeTextForBestFit = true;
            objectiveText.resizeTextMinSize = 16;
            objectiveText.resizeTextMaxSize = 22;
            Stretch(objectiveText.rectTransform);
            objectiveText.raycastTarget = false;

            boardBackdrop = CreatePanel(root, "Board Frame", BoardFrameColor);
            SetRect(boardBackdrop, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(806f, 806f));
            UseRoundedSprite(boardBackdrop.GetComponent<Image>());
            AddSoftShadow(boardBackdrop.GetComponent<Image>(), new Vector2(0f, -22f), 0.16f);
            AddSoftOutline(boardBackdrop.GetComponent<Image>(), BoardOutlineColor, new Vector2(2f, -2f));
            boardBackdrop.GetComponent<Image>().raycastTarget = false;
            AddBoardFrameDecorations(boardBackdrop);

            boardRoot = CreatePanel(root, "Board", new Color(1f, 1f, 1f, 0f));
            SetRect(boardRoot, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 720f));
            boardRoot.GetComponent<Image>().raycastTarget = false;

            timerPanel = CreatePanel(root, "Timer Badge", new Color(1f, 247f / 255f, 228f / 255f, 0.97f));
            SetRect(timerPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-142f, 500f), new Vector2(230f, TimerHeight + 8f));
            StyleCreamPanel(timerPanel.GetComponent<Image>(), 0.14f);
            Image timerPaw = CreateImage(timerPanel, "Timer Paw", new Color(0.88f, 0.45f, 0.4f, 0.74f));
            timerPaw.sprite = pawSprite;
            timerPaw.raycastTarget = false;
            SetRect(timerPaw.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(44f, 44f));
            timerText = CreateText(timerPanel, "2:00", 46, FontStyle.Bold, TextAnchor.MiddleCenter, TimerNormalColor);
            Stretch(timerText.rectTransform);
            timerText.rectTransform.offsetMin = new Vector2(48f, 0f);
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddSoftShadow(timerText, new Vector2(0f, -3f), 0.18f);
            AddSoftOutline(timerText, new Color(1f, 1f, 1f, 0.78f), new Vector2(2f, -2f));
            timerText.raycastTarget = false;

            starPanel = CreatePanel(root, "Star Goal", new Color(1f, 0.96f, 0.86f, 0.96f));
            SetRect(starPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(145f, 500f), new Vector2(246f, TimerHeight + 8f));
            StyleCreamPanel(starPanel.GetComponent<Image>(), 0.12f);
            for (int i = 0; i < progressStars.Length; i++)
            {
                progressStars[i] = CreateImage(starPanel, "Goal Star " + (i + 1), GoldColor);
                progressStars[i].sprite = starSprite;
                progressStars[i].raycastTarget = false;
                SetRect(progressStars[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 66f, 0f), new Vector2(54f, 54f));
            }

            comboBadge = CreatePanel(root, "Combo Badge", new Color(0.99f, 0.56f, 0.5f, 0.96f));
            SetRect(comboBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(310f, 58f));
            UseRoundedSprite(comboBadge.GetComponent<Image>());
            AddSoftShadow(comboBadge.GetComponent<Image>(), new Vector2(0f, -6f), 0.16f);
            comboText = CreateText(comboBadge, "3x  PURRFECT", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(comboText.rectTransform);
            comboBadge.gameObject.SetActive(false);

            pieceLayer = CreatePanel(root, "Piece Layer", new Color(1f, 1f, 1f, 0f));
            Stretch(pieceLayer);
            pieceLayer.GetComponent<Image>().raycastTarget = false;
            pieceLayer.SetAsLastSibling();

            trayRoot = CreatePanel(root, "Shelf", TrayColor);
            SetRect(trayRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(1000f, 292f));
            trayImage = trayRoot.GetComponent<Image>();
            StyleCreamPanel(trayImage, 0.18f);
            trayImage.raycastTarget = true;
            AddBasketDecorations(trayRoot);

            trayScrollRect = trayRoot.gameObject.AddComponent<ScrollRect>();
            trayScrollRect.horizontal = true;
            trayScrollRect.vertical = false;
            trayScrollRect.movementType = ScrollRect.MovementType.Elastic;
            trayScrollRect.inertia = true;
            trayScrollRect.decelerationRate = 0.12f;
            trayScrollRect.scrollSensitivity = 34f;

            trayViewport = CreatePanel(trayRoot, "Viewport", new Color(1f, 1f, 1f, 0f));
            SetRect(trayViewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-36f, -32f));
            trayViewport.GetComponent<Image>().raycastTarget = false;
            trayViewport.gameObject.AddComponent<RectMask2D>();

            trayContent = CreatePanel(trayViewport, "Content", new Color(1f, 1f, 1f, 0f));
            SetRect(trayContent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(960f, 0f));
            trayContent.GetComponent<Image>().raycastTarget = false;
            trayLayout = trayContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            trayLayout.padding = new RectOffset(24, 24, 24, 24);
            trayLayout.spacing = 18f;
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.childControlWidth = true;
            trayLayout.childControlHeight = true;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = false;
            trayScrollRect.viewport = trayViewport;
            trayScrollRect.content = trayContent;

            actionBar = CreatePanel(root, "Actions", new Color(1f, 1f, 1f, 0f));
            SetRect(actionBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(520f, 92f));
            actionBar.GetComponent<Image>().raycastTarget = false;
            HorizontalLayoutGroup buttonLayout = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandHeight = true;
            CreateActionButton(actionBar, "Hint", hintIconSprite, ShowHint);
            CreateActionButton(actionBar, "Reset", resetIconSprite, ResetLevel);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BuildTestLevelControls();
#endif

            pieceLayer.SetAsLastSibling();

            fxLayer = CreatePanel(root, "FX Layer", new Color(1f, 1f, 1f, 0f));
            Stretch(fxLayer);
            fxLayer.GetComponent<Image>().raycastTarget = false;
            fxLayer.SetAsLastSibling();

            BuildWinOverlay();
            BuildFailOverlay();
            BuildMetaUiInEditor();
        }

        private void BuildTestLevelControls()
        {
            previousTestButton = CreateIconButton(
                root,
                "Previous Test Level",
                backIconSprite,
                LoadPreviousTestLevel,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 18f),
                new Vector2(72f, 72f));

            nextTestButton = CreateIconButton(
                root,
                "Next Test Level",
                backIconSprite,
                LoadNextTestLevel,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 18f),
                new Vector2(72f, 72f));

            Transform nextIcon = nextTestButton.transform.Find("Next Test Level Icon");
            if (nextIcon != null)
            {
                nextIcon.localEulerAngles = new Vector3(0f, 0f, 180f);
            }

            UpdateTestLevelButtons();
        }

        private void BuildWinOverlay()
        {
            winOverlay = CreatePanel(root, "Win Overlay", Color.clear);
            Stretch(winOverlay);
            winOverlay.SetAsLastSibling();

            winPanel = CreatePanel(winOverlay, "Win Panel", Color.clear);
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

            CreateReferenceButton(winPanel, "Continue", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelWin/Continue.png", 243, 1499, 594, 172, LoadNextLevelThroughMetaGate);

            levelCompleteScreen.CaptureExisting(winOverlay.gameObject);
            winOverlay.gameObject.SetActive(false);
        }

        private void BuildFailOverlay()
        {
            failOverlay = CreatePanel(root, "Fail Overlay", Color.clear);
            Stretch(failOverlay);
            failOverlay.SetAsLastSibling();

            failPanel = CreatePanel(failOverlay, "Fail Panel", Color.clear);
            Stretch(failPanel);
            AddReferenceLayer(failPanel, "Background", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/BG.png", 0, 0, 1080, 1920);
            AddReferenceLayer(failPanel, "Sad Cat Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/CatSad.png", 232, 723, 635, 527);
            AddReferenceLayer(failPanel, "So Close Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/So_close.png", 728, 645, 221, 212);
            AddReferenceLayer(failPanel, "Level Failed Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/title.png", 133, 234, 816, 208);
            AddReferenceLayer(failPanel, "Encouragement Artwork", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/Donotworry.png", 227, 1214, 621, 181);
            CreateReferenceButton(failPanel, "Retry", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/TRY.png", 225, 1403, 626, 176, ResetLevel);
            CreateReferenceButton(failPanel, "Skip Level", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/Skip.png", 152, 1620, 362, 113, LoadNextLevel);
            CreateReferenceButton(failPanel, "Home", "Assets/Art/CatBlockPuzzleUI/UI/Slicing/LevelLose/HOME.png", 526, 1617, 354, 115, OpenRoomHub);

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

        private Button CreateReferenceButton(RectTransform parent, string name, string assetPath, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            Image image = AddReferenceLayer(parent, name, assetPath, x, y, width, height);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(action);
            return button;
        }

        private void AddBasketDecorations(RectTransform basket)
        {
            Image rim = CreateImage(basket, "Basket Rim", new Color(0.76f, 0.39f, 0.3f, 0.76f));
            UseRoundedSprite(rim);
            rim.raycastTarget = false;
            SetRect(rim.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 5f), new Vector2(-18f, 28f));

            for (int i = 0; i < 11; i++)
            {
                Image weave = CreateImage(basket, "Basket Weave", new Color(0.72f, 0.36f, 0.28f, 0.13f));
                UseRoundedSprite(weave);
                weave.raycastTarget = false;
                SetRect(weave.rectTransform, new Vector2(i / 10f, 0f), new Vector2(i / 10f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9f, -34f));
                weave.rectTransform.localEulerAngles = new Vector3(0f, 0f, i % 2 == 0 ? -7f : 7f);
            }
        }

        private Button CreateIconButton(
            RectTransform parent,
            string name,
            Sprite icon,
            UnityEngine.Events.UnityAction action,
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
            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(action);

            Image iconImage = CreateImage(rect, name + " Icon", InkColor);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            SetRect(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size * 0.48f);
            return button;
        }

        private void CreateActionButton(RectTransform parent, string label, Sprite icon, UnityEngine.Events.UnityAction action)
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
            button.onClick.AddListener(PlayButtonSound);
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

        private void StyleCreamPanel(Image image, float shadowAlpha)
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

        private RectTransform CreatePanel(RectTransform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            return gameObject.GetComponent<RectTransform>();
        }

        private Image CreateImage(RectTransform parent, string name, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            return image;
        }

        private Text CreateText(RectTransform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color)
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

        private void CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            CreateButton(parent, label, action, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, InkColor);
        }

        private void CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color background)
        {
            GameObject gameObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, pivot, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.color = background;
            StyleCreamPanel(image, 0.16f);
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(background, InkColor, 0.12f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 248f / 255f, 236f / 255f, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(action);
            Text text = CreateText(rect, label, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
        }

        private void AddSoftShadow(Graphic graphic, Vector2 distance, float alpha)
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

        private void AddSoftOutline(Graphic graphic, Color color, Vector2 distance)
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

        private void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private void BuildBoardInEditor()
        {
            boardCells.Clear();
            boardRevealCells.Clear();

            Vector2 targetMaxSize = GetBoardTargetMaxSize();
            float levelAspect = (float)activeLevel.Cols / Mathf.Max(1, activeLevel.Rows);
            if (levelAspect >= targetMaxSize.x / targetMaxSize.y)
            {
                boardWidth = targetMaxSize.x;
                boardHeight = targetMaxSize.x / levelAspect;
            }
            else
            {
                boardHeight = targetMaxSize.y;
                boardWidth = targetMaxSize.y * levelAspect;
            }

            ApplyGameplayLayout();

            boardCellWidth = (boardWidth - (BoardGap * (activeLevel.Cols - 1))) / activeLevel.Cols;
            boardCellHeight = (boardHeight - (BoardGap * (activeLevel.Rows - 1))) / activeLevel.Rows;
            if (placementAvailability.GetLength(0) != activeLevel.Rows || placementAvailability.GetLength(1) != activeLevel.Cols)
            {
                placementAvailability = new bool[activeLevel.Rows, activeLevel.Cols];
            }

            for (int row = 0; row < activeLevel.Rows; row++)
            {
                for (int col = 0; col < activeLevel.Cols; col++)
                {
                    Vector2Int coord = new Vector2Int(row, col);
                    bool active = activeLevel.ActiveCells.Contains(coord);
                    Image cellImage = CreateImage(boardRoot, "Cell " + row + "," + col, active ? TargetColor : Color.clear);
                    if (active)
                    {
                        UseRoundedSprite(cellImage);
                        AddSoftShadow(cellImage, new Vector2(0f, -5f), 0.1f);
                        AddSoftOutline(cellImage, BoardTileEdgeColor, new Vector2(2f, -2f));
                    }

                    cellImage.raycastTarget = false;
                    RectTransform rect = cellImage.rectTransform;
                    SetTopLeft(rect, CellPosition(row, col), new Vector2(boardCellWidth, boardCellHeight));
                    Image preview = null;
                    if (active)
                    {
                        rect.localScale = Vector3.zero;
                        boardRevealCells.Add(new BoardRevealCell(rect, row + col));

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

                    boardCells[coord] = new CellView(rect, cellImage, preview, cellImage.color);
                }
            }
        }

        private void BuildPiecesInEditor()
        {
            ConfigureTrayForPieceCount(activeLevel.Pieces.Length);

            for (int i = 0; i < activeLevel.Pieces.Length; i++)
            {
                PieceDefinition definition = activeLevel.Pieces[i];
                PieceState state = new PieceState(definition, PieceColors[i % PieceColors.Length]);
                state.AtlasIndex = i % 8;
                state.FloatPhase = i * 0.83f;
                state.Slot = CreatePanel(trayContent, definition.Name + " Slot", CardRestColor);
                state.SlotImage = state.Slot.GetComponent<Image>();
                StyleCreamPanel(state.SlotImage, 0.11f);
                state.SlotLayout = state.Slot.gameObject.AddComponent<LayoutElement>();
                ApplyTraySlotLayout(state.SlotLayout);
                AddDragDots(state.Slot);

                state.Rect = CreatePanel(state.Slot, definition.Name, new Color(1f, 1f, 1f, 0f));
                state.Rect.SetAsLastSibling();
                CreatePieceCells(state);
                AttachPieceToTray(state);
                pieces.Add(state);
            }

            RefreshTrayLayout(false);
            if (trayScrollRect != null)
            {
                trayScrollRect.horizontalNormalizedPosition = 0f;
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
                Sprite portrait = CatPortrait(CatMood.Neutral, state.AtlasIndex);
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

                leftEar.raycastTarget = false;
                rightEar.raycastTarget = false;
                leftInnerEar.raycastTarget = false;
                rightInnerEar.raycastTarget = false;
                highlight.raycastTarget = false;
                leftEye.raycastTarget = false;
                rightEye.raycastTarget = false;
                nose.raycastTarget = false;
                mouth.raycastTarget = false;
                leftCheek.raycastTarget = false;
                rightCheek.raycastTarget = false;
                leftWhiskerTop.raycastTarget = false;
                leftWhiskerBottom.raycastTarget = false;
                rightWhiskerTop.raycastTarget = false;
                rightWhiskerBottom.raycastTarget = false;
                foreheadStripe.raycastTarget = false;
                tail.raycastTarget = false;
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

        private void BuildMetaUiInEditor()
        {
            InitializeMetaSystems();
            if (root == null || metaOverlay != null)
            {
                return;
            }

            metaOverlay = CreatePanel(root, "Meta Overlay", PageColor);
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

        private void RenderRoomHubInEditor()
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
            string continueLabel = "Continue • Level " + (recommendedLevel + 1);
            CreateMetaButton(metaHubPage, continueLabel,
                () => PlayMetaLevel(recommendedLevel),
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
                    haptics?.PlayWrongMove();
                    ShowMetaStatus("Complete the room before this one to unlock it.");
                }
            });

            Image thumb = CreateImage(card, "Room Thumbnail", unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.78f));
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

        private void RenderRoomDetailInEditor(int chapterIndex)
        {
            ClearMetaChildren(metaRoomPage);
            metaStatusText = null;
            CatMetaChapterDefinition chapter = metaCatalog.GetChapter(chapterIndex);

            Image roomBackground = CreateImage(metaRoomPage, "Room Background", new Color(0.92f, 0.88f, 0.82f, 1f));
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
                // All placement targets are authored; runtime only changes their state.
                selectedStored = true;

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

        private void ShowStoryCardInEditor(
            string titleValue,
            string bodyValue,
            string actionLabel,
            UnityAction action,
            Sprite portraitSprite)
        {
            BuildMetaUiInEditor();
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

        private void ShowChapterCompletionInEditor(int chapterIndex)
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

        private Sprite LoadMetaSpriteInEditor(string resourcePath, bool logMissing = true)
        {
            if (contentCatalog != null && metaCatalog != null)
            {
                foreach (var chapter in metaCatalog.Chapters)
                {
                    if (chapter.BackgroundResourcePath == resourcePath) return contentCatalog.roomBackgrounds[chapter.Index];
                    if (chapter.ThumbnailResourcePath == resourcePath) return contentCatalog.roomThumbnails[chapter.Index];
                    foreach (var decoration in chapter.Decorations)
                        if (decoration.SpriteResourcePath == resourcePath) return PreparedDecorationSprite(decoration.Id);
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

        private void CrossfadeGameplayBackgroundInEditor(Sprite nextSprite)
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
            StartCoroutine(FadeOutMetaBackgroundInEditor(oldBackground));
        }

        private IEnumerator FadeOutMetaBackgroundInEditor(Image oldBackground)
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
                DestroyImmediate(child);
            }
        }
    }
}
#endif
