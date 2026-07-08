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

            RectTransform canvasRoot = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRoot);

            root = CreatePanel(canvasRoot, "Safe Area", new Color(1f, 1f, 1f, 0f));
            Stretch(root);
            root.GetComponent<Image>().raycastTarget = false;
            root.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform backPanel = CreatePanel(root, "Back Button", PanelColor);
            SetRect(backPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -50f), new Vector2(96f, 96f));
            StyleCreamPanel(backPanel.GetComponent<Image>(), 0.15f);
            Text backText = CreateText(backPanel, "<", 42, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            Stretch(backText.rectTransform);

            levelText = CreateText(root, "Level 1", 64, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(520f, 82f));

            RectTransform pausePanel = CreatePanel(root, "Pause Button", PanelColor);
            SetRect(pausePanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-156f, -50f), new Vector2(96f, 96f));
            StyleCreamPanel(pausePanel.GetComponent<Image>(), 0.15f);
            Text pauseText = CreateText(pausePanel, "II", 34, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            Stretch(pauseText.rectTransform);

            RectTransform settingsPanel = CreatePanel(root, "Settings Button", PanelColor);
            SetRect(settingsPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -50f), new Vector2(96f, 96f));
            StyleCreamPanel(settingsPanel.GetComponent<Image>(), 0.15f);
            Text settingsText = CreateText(settingsPanel, "o", 42, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            Stretch(settingsText.rectTransform);

            RectTransform progressBar = CreatePanel(root, "Star Progress Bar", new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.42f));
            SetRect(progressBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(282f, 38f));
            UseRoundedSprite(progressBar.GetComponent<Image>());
            progressBar.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < 3; i++)
            {
                Text star = CreateText(progressBar, "*", 42, FontStyle.Bold, TextAnchor.MiddleCenter, i == 0 ? GoldColor : new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.95f));
                SetRect(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 64f, 0f), new Vector2(54f, 46f));
            }

            objectiveText = CreateText(root, "Fill the board", 34, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(objectiveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -182f), new Vector2(620f, 48f));

            RectTransform coinPanel = CreatePanel(root, "Coin Counter", PanelColor);
            SetRect(coinPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -158f), new Vector2(164f, 56f));
            StyleCreamPanel(coinPanel.GetComponent<Image>(), 0.1f);
            Image coinIcon = CreateImage(coinPanel, "Coin", Color.white);
            coinIcon.sprite = coinSprite;
            AddSoftShadow(coinIcon, new Vector2(0f, -2f), 0.18f);
            SetRect(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(34f, 34f));
            coinText = CreateText(coinPanel, "0", 30, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(coinText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), Vector2.zero);

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

            pieceLayer = CreatePanel(root, "Piece Layer", new Color(1f, 1f, 1f, 0f));
            Stretch(pieceLayer);
            pieceLayer.GetComponent<Image>().raycastTarget = false;
            pieceLayer.SetAsLastSibling();

            trayRoot = CreatePanel(root, "Shelf", TrayColor);
            SetRect(trayRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, TrayCenterY), new Vector2(1000f, 302f));
            trayImage = trayRoot.GetComponent<Image>();
            StyleCreamPanel(trayImage, 0.18f);
            trayImage.raycastTarget = false;
            trayLayout = trayRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            trayLayout.padding = new RectOffset(22, 22, 22, 22);
            trayLayout.spacing = 16f;
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.childControlWidth = true;
            trayLayout.childControlHeight = true;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = false;

            RectTransform buttons = CreatePanel(root, "Actions", new Color(1f, 1f, 1f, 0f));
            SetRect(buttons, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(960f, 112f));
            buttons.GetComponent<Image>().raycastTarget = false;
            HorizontalLayoutGroup buttonLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandHeight = true;
            CreateButton(buttons, "Previous", LoadPreviousLevel);
            CreateButton(buttons, "Hint", ShowHint);
            CreateButton(buttons, "Reset", ResetLevel);
            CreateButton(buttons, "Next", LoadNextLevel);

            fxLayer = CreatePanel(root, "FX Layer", new Color(1f, 1f, 1f, 0f));
            Stretch(fxLayer);
            fxLayer.GetComponent<Image>().raycastTarget = false;
            fxLayer.SetAsLastSibling();

            BuildWinOverlay();
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
            SetRect(winPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 310f));
            UseRoundedSprite(winPanel.GetComponent<Image>());
            AddSoftShadow(winPanel.GetComponent<Image>(), new Vector2(0f, -18f), 0.22f);
            AddSoftOutline(winPanel.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.7f), new Vector2(2f, -2f));

            Text header = CreateText(winPanel, "LEVEL COMPLETE", 26, FontStyle.Bold, TextAnchor.MiddleCenter, TargetDeepColor);
            SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(0f, 42f));
            winTitleText = CreateText(winPanel, "Perfect Fit", 48, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            SetRect(winTitleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(0f, 70f));
            winRewardText = CreateText(winPanel, "+25 coins", 30, FontStyle.Bold, TextAnchor.MiddleCenter, SoftInkColor);
            SetRect(winRewardText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(0f, 46f));
            CreateButton(winPanel, "Next Level", LoadNextLevel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(360f, 64f), TargetDeepColor);

            winOverlay.gameObject.SetActive(false);
        }

        private void StyleCreamPanel(Image image, float shadowAlpha)
        {
            UseRoundedSprite(image);
            image.color = PanelColor;
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

        private void BuildTrayVisibilitySlider()
        {
            RectTransform sliderRoot = CreatePanel(trayRoot, "Tray Visibility Slider", new Color(1f, 1f, 1f, 0f));
            LayoutElement layoutElement = sliderRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            SetRect(sliderRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(430f, 42f));

            Slider slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = TrayVisibilityMin;
            slider.maxValue = TrayVisibilityMax;
            slider.wholeNumbers = false;

            Image track = CreateImage(sliderRoot, "Track", new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.58f));
            UseRoundedSprite(track);
            track.raycastTarget = false;
            SetRect(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-72f, 10f));

            RectTransform fillArea = CreatePanel(sliderRoot, "Fill Area", new Color(1f, 1f, 1f, 0f));
            fillArea.GetComponent<Image>().raycastTarget = false;
            SetRect(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-72f, 18f));

            Image fill = CreateImage(fillArea, "Fill", TargetDeepColor);
            UseRoundedSprite(fill);
            fill.raycastTarget = false;
            Stretch(fill.rectTransform);

            RectTransform handleArea = CreatePanel(sliderRoot, "Handle Slide Area", new Color(1f, 1f, 1f, 0f));
            handleArea.GetComponent<Image>().raycastTarget = false;
            SetRect(handleArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-72f, 42f));

            Image handle = CreateImage(handleArea, "Handle", Color.white);
            handle.sprite = circleSprite;
            SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            AddSoftShadow(handle, new Vector2(0f, -3f), 0.18f);
            AddSoftOutline(handle, PanelOutlineColor, new Vector2(1f, -1f));

            Image smallPaw = CreateImage(sliderRoot, "Small Paw", new Color(229f / 255f, 208f / 255f, 168f / 255f, 0.92f));
            Image largePaw = CreateImage(sliderRoot, "Large Paw", TargetDeepColor);
            smallPaw.sprite = pawSprite;
            largePaw.sprite = pawSprite;
            smallPaw.raycastTarget = false;
            largePaw.raycastTarget = false;
            SetRect(smallPaw.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(22f, 22f));
            SetRect(largePaw.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, 0f), new Vector2(28f, 28f));

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(trayVisibilityScale);
            slider.onValueChanged.AddListener(OnTrayVisibilitySliderChanged);
            trayVisibilitySlider = slider;
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
            StyleCreamPanel(image, 0.16f);
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = PanelColor;
            colors.highlightedColor = new Color(1f, 252f / 255f, 245f / 255f, 1f);
            colors.pressedColor = new Color(241f / 255f, 228f / 255f, 203f / 255f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 248f / 255f, 236f / 255f, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(PlayButtonSound);
            button.onClick.AddListener(action);
            Text text = CreateText(rect, label, 28, FontStyle.Bold, TextAnchor.MiddleCenter, InkColor);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
        }

        private void PlayButtonSound()
        {
            PlayClip(buttonClip);
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
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

        private void AddPanel(RectTransform parent, RectTransform target, Color color)
        {
            Image image = target.gameObject.AddComponent<Image>();
            image.sprite = whiteSprite;
            image.color = color;
            image.raycastTarget = false;
            target.SetAsFirstSibling();
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
