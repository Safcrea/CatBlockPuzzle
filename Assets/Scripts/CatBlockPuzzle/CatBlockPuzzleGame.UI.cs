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

            Image background = canvasObject.GetComponent<Image>();
            background.sprite = CreateBackgroundSprite();
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;

            RectTransform canvasRoot = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRoot);

            root = CreatePanel(canvasRoot, "Safe Area", new Color(1f, 1f, 1f, 0f));
            Stretch(root);
            root.GetComponent<Image>().raycastTarget = false;
            root.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform headerBand = CreatePanel(root, "Top Shelf", new Color(1f, 0.96f, 0.88f, 0.9f));
            SetRect(headerBand, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-30f, 106f));
            UseRoundedSprite(headerBand.GetComponent<Image>());
            AddSoftShadow(headerBand.GetComponent<Image>(), new Vector2(0f, -8f), 0.11f);
            headerBand.GetComponent<Image>().raycastTarget = false;

            CreateIconButton(root, "Back", backIconSprite, LoadPreviousLevel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(74f, 74f));

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
            SetRect(objectivePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(330f, 50f));
            UseRoundedSprite(objectivePanel.GetComponent<Image>());
            AddSoftShadow(objectivePanel.GetComponent<Image>(), new Vector2(0f, -5f), 0.14f);
            objectiveText = CreateText(objectivePanel, "Fill the board", 25, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
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
            BuildSettingsOverlay();
            ApplyPreferences();
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

        private void BuildAudio()
        {
            GameObject audioObject = new GameObject("Cat Puzzle Audio", typeof(AudioSource));
            audioObject.transform.SetParent(transform, false);
            audioSource = audioObject.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;

            buttonClip = CreateToneClip("Cat Button", 0.07f, 0.22f, 520f, 660f);
            snapClip = CreateToneClip("Cat Snap", 0.1f, 0.28f, 720f, 980f);
            wrongClip = CreateToneClip("Cat Wrong", 0.16f, 0.22f, 210f, 140f);
            winClip = CreateToneClip("Cat Win", 0.42f, 0.24f, 520f, 660f, 780f, 1040f);
        }

        private void BuildWinOverlay()
        {
            winOverlay = CreatePanel(root, "Win Overlay", new Color(0.14f, 0.13f, 0.12f, 0.32f));
            Stretch(winOverlay);
            winOverlay.SetAsLastSibling();

            winPanel = CreatePanel(winOverlay, "Win Panel", new Color(1f, 0.98f, 0.94f, 0.98f));
            SetRect(winPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 610f));
            UseRoundedSprite(winPanel.GetComponent<Image>());
            AddSoftShadow(winPanel.GetComponent<Image>(), new Vector2(0f, -18f), 0.22f);
            AddSoftOutline(winPanel.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.7f), new Vector2(2f, -2f));

            Text header = CreateText(winPanel, "LEVEL COMPLETE", 26, FontStyle.Bold, TextAnchor.MiddleCenter, TargetDeepColor);
            SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(0f, 42f));
            winTitleText = CreateText(winPanel, "Perfect Fit", 46, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(winTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(-50f, 62f));

            RectTransform winStarPanel = CreatePanel(winPanel, "Earned Stars", new Color(1f, 1f, 1f, 0f));
            SetRect(winStarPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(300f, 76f));
            winStarPanel.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < winStars.Length; i++)
            {
                winStars[i] = CreateImage(winStarPanel, "Result Star " + (i + 1), GoldColor);
                winStars[i].sprite = starSprite;
                winStars[i].raycastTarget = false;
                SetRect(winStars[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 88f, 0f), new Vector2(72f, 72f));
            }

            winRewardText = CreateText(winPanel, "+25 coins", 30, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(winRewardText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 58f), new Vector2(0f, 44f));
            winBestText = CreateText(winPanel, "New best: 3 stars", 24, FontStyle.Bold, TextAnchor.MiddleCenter, TargetDeepColor);
            SetRect(winBestText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(0f, 38f));

            winCatImage = CreateImage(winPanel, "Unlocked Cat", Color.white);
            winCatImage.preserveAspect = true;
            winCatImage.raycastTarget = false;
            SetRect(winCatImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -72f), new Vector2(126f, 126f));
            winUnlockText = CreateText(winPanel, "New cat friend unlocked", 24, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(winUnlockText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -148f), new Vector2(-48f, 36f));

            CreateButton(winPanel, "Next Level", LoadNextLevel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(390f, 76f), TargetDeepColor);

            winOverlay.gameObject.SetActive(false);
        }

        private void BuildFailOverlay()
        {
            failOverlay = CreatePanel(root, "Fail Overlay", new Color(0.14f, 0.13f, 0.12f, 0.32f));
            Stretch(failOverlay);
            failOverlay.SetAsLastSibling();

            failPanel = CreatePanel(failOverlay, "Fail Panel", new Color(1f, 0.98f, 0.94f, 0.98f));
            SetRect(failPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 330f));
            UseRoundedSprite(failPanel.GetComponent<Image>());
            AddSoftShadow(failPanel.GetComponent<Image>(), new Vector2(0f, -18f), 0.22f);
            AddSoftOutline(failPanel.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.7f), new Vector2(2f, -2f));

            Text header = CreateText(failPanel, "TIME UP", 28, FontStyle.Bold, TextAnchor.MiddleCenter, TimerWarningColor);
            SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0f, 48f));
            Text title = CreateText(failPanel, "Try Again", 52, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(0f, 74f));
            Text message = CreateText(failPanel, "Complete the puzzle before the timer ends.", 28, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(message.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(-72f, 58f));
            CreateButton(failPanel, "Retry", ResetLevel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 66f), TimerWarningColor);

            failOverlay.gameObject.SetActive(false);
        }

        private void BuildSettingsOverlay()
        {
            settingsOverlay = CreatePanel(root, "Settings Overlay", new Color(0.12f, 0.1f, 0.09f, 0.42f));
            Stretch(settingsOverlay);
            settingsOverlay.SetAsLastSibling();

            settingsPanel = CreatePanel(settingsOverlay, "Settings Panel", new Color(1f, 0.97f, 0.9f, 0.99f));
            SetRect(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 590f));
            UseRoundedSprite(settingsPanel.GetComponent<Image>());
            AddSoftShadow(settingsPanel.GetComponent<Image>(), new Vector2(0f, -18f), 0.24f);
            AddSoftOutline(settingsPanel.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.72f), new Vector2(2f, -2f));

            settingsTitleText = CreateText(settingsPanel, "SETTINGS", 38, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(settingsTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(-120f, 58f));
            CreateIconButton(settingsPanel, "Close", closeIconSprite, CloseSettings, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(66f, 66f));

            CreateToggleRow(settingsPanel, "Sound", soundEnabled, new Vector2(0f, 118f), SetSound, out soundToggle, out soundToggleKnob, out soundToggleText);
            CreateToggleRow(settingsPanel, "Haptics", hapticsEnabled, new Vector2(0f, 16f), SetHaptics, out hapticsToggle, out hapticsToggleKnob, out hapticsToggleText);
            CreateToggleRow(settingsPanel, "Reduced motion", reducedMotion, new Vector2(0f, -86f), SetReducedMotion, out motionToggle, out motionToggleKnob, out motionToggleText);

            CreateButton(settingsPanel, "Resume", CloseSettings, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(350f, 76f), TargetDeepColor);
            settingsOverlay.gameObject.SetActive(false);
        }

        private void CreateToggleRow(
            RectTransform parent,
            string label,
            bool initialValue,
            Vector2 position,
            UnityEngine.Events.UnityAction<bool> action,
            out Toggle toggle,
            out RectTransform knob,
            out Text valueText)
        {
            RectTransform row = CreatePanel(parent, label + " Row", new Color(1f, 0.86f, 0.74f, 0.34f));
            SetRect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(540f, 82f));
            UseRoundedSprite(row.GetComponent<Image>());
            row.GetComponent<Image>().raycastTarget = false;

            Text labelText = CreateText(row, label, 30, FontStyle.Bold, TextAnchor.MiddleLeft, InkColor);
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(-250f, 0f));
            labelText.raycastTarget = false;

            GameObject toggleObject = new GameObject(label + " Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(row, false);
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            SetRect(toggleRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(116f, 52f));
            Image track = toggleObject.GetComponent<Image>();
            UseRoundedSprite(track);
            track.color = initialValue ? TargetDeepColor : new Color(0.76f, 0.7f, 0.65f, 1f);

            Image knobImage = CreateImage(toggleRect, "Knob", Color.white);
            knobImage.sprite = circleSprite;
            knobImage.raycastTarget = false;
            knob = knobImage.rectTransform;
            SetRect(knob, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(initialValue ? 24f : -24f, 0f), new Vector2(42f, 42f));
            AddSoftShadow(knobImage, new Vector2(0f, -2f), 0.16f);

            valueText = CreateText(row, initialValue ? "On" : "Off", 22, FontStyle.Bold, TextAnchor.MiddleRight, SoftInkColor);
            SetRect(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-154f, 0f), new Vector2(70f, 42f));
            valueText.raycastTarget = false;

            toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.isOn = initialValue;
            toggle.onValueChanged.AddListener(action);
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

        private void PlayButtonSound()
        {
            PlayClip(buttonClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (soundEnabled && audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void UseRoundedSprite(Image image)
        {
            if (image == null || roundedBoxSprite == null)
            {
                return;
            }

            image.sprite = roundedBoxSprite;
            image.type = Image.Type.Sliced;
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

        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void SetCenteredChild(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
