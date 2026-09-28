using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        public const string LevelOneTutorialSeenKey = "CatBlockPuzzle.Tutorial.LevelOne.v1";
        [Header("Level 1 Tutorial")]
        [Tooltip("Turn off to play level 1 without the hands-on tutorial. Can also be changed during Play Mode.")]
        [SerializeField] private bool enableLevelOneTutorial = true;
        [Tooltip("Add short contextual captions alongside the visual demonstrations.")]
        [SerializeField] private bool showTutorialCaptions;
        [Tooltip("Optional artwork for the animated tutorial pointer.")]
        [SerializeField] private Sprite tutorialHandSprite;
        // Kept intentionally short: place the first cat for real, then the second, then
        // hand the rest of the level back to the player.
        private enum TutorialStep { Off, PlaceFirst, PlaceSecond }
        private TutorialStep tutorialStep;
        private RectTransform tutorialRoot;
        private RectTransform tutorialTargetRoot;
        private RectTransform tutorialHand;
        private RectTransform tutorialCheck;
        private RectTransform tutorialCoach;
        private RectTransform tutorialCoachPictures;
        private Text tutorialCoachTitle;
        private Text tutorialCoachBody;
        private readonly List<Image> tutorialProgressDots = new List<Image>();
        private readonly List<Vector2Int> tutorialTargetCoordinates = new List<Vector2Int>();
        private Image tutorialHalo;
        private Button tutorialSkipButton;
        private readonly List<Image> tutorialTargets = new List<Image>();
        private readonly List<Image> tutorialDots = new List<Image>();
        private PieceState tutorialPiece;
        private float tutorialElapsed;
        private float tutorialCelebrationRemaining;
        private bool tutorialPendingFinish;
        public bool IsLevelOneTutorialOpen => tutorialStep != TutorialStep.Off;
        public bool LevelOneTutorialEnabled => enableLevelOneTutorial;

        public void SetLevelOneTutorialEnabled(bool enabled)
        {
            enableLevelOneTutorial = enabled;
            if (!enabled) EndLevelOneTutorialEarly();
        }

        public void DisableLevelOneTutorial() => SetLevelOneTutorialEnabled(false);

        public void SkipLevelOneTutorial()
        {
            if (!IsLevelOneTutorialOpen) return;
            if (!levelNavigationTesting)
            {
                PlayerPrefs.SetInt(LevelOneTutorialSeenKey, 1);
                PlayerPrefs.Save();
            }
            EndLevelOneTutorialEarly();
        }

        private void EndLevelOneTutorialEarly()
        {
            if (!IsLevelOneTutorialOpen) return;
            CancelActiveDragToRest();
            CancelLevelOneTutorial();
            // Preserve any return/landing animation's input lock until it finishes.
            StartLevelTimer();
        }

        // Replay uses the same puzzle and never clears the player's completion or rewards.
        public void ReplayLevelOneTutorial()
        {
            enableLevelOneTutorial = true;
            PlayerPrefs.DeleteKey(LevelOneTutorialSeenKey);
            LoadLevel(0, !levelNavigationTesting);
        }

        private void BeginLevelOneTutorial()
        {
            if (!enableLevelOneTutorial || levelIndex != 0 || pieces.Count < 2 || PlayerPrefs.GetInt(LevelOneTutorialSeenKey, 0) != 0) return;
            tutorialRoot = RuntimeUiFactory.CreateRect(levelRoot, "Level One Hands On Tutorial");
            RuntimeUiFactory.Stretch(tutorialRoot);
            // This layer never intercepts touches; pause/settings remain usable above gameplay.
            var group = tutorialRoot.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            // Board cells draw first; landed pieces are appended above these guides.
            // The separate piece layer also keeps dragged cats above the targets.
            tutorialTargetRoot = RuntimeUiFactory.CreateRect(boardRoot, "Tutorial Placement Targets");
            RuntimeUiFactory.Stretch(tutorialTargetRoot);
            tutorialHalo = TutorialImage(tutorialRoot, "Pickup Halo", circleSprite, GoldColor);
            for (int i = 0; i < 9; i++)
                tutorialDots.Add(TutorialImage(tutorialRoot, "Drag Path", i % 2 == 0 ? pawSprite : circleSprite, GoldColor,
                    Vector2.zero, new Vector2(i % 2 == 0 ? 24f : 14f, i % 2 == 0 ? 24f : 14f)));
            tutorialHand = RuntimeUiFactory.CreateRect(tutorialRoot, "Animated Hand");
            CreateTutorialHand();
            tutorialCheck = RuntimeUiFactory.CreateRect(tutorialRoot, "Successful Fit Check");
            TutorialImage(tutorialCheck, "Badge", circleSprite, PanelColor, Vector2.zero, new Vector2(78f, 78f));
            var checkA = TutorialImage(tutorialCheck, "Check Short", roundedBoxSprite, TargetDeepColor, new Vector2(-12f, -5f), new Vector2(27f, 11f));
            var checkB = TutorialImage(tutorialCheck, "Check Long", roundedBoxSprite, TargetDeepColor, new Vector2(9f, 3f), new Vector2(44f, 11f));
            checkA.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
            checkB.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            CreateTutorialCoach();
            SetTutorialStep(TutorialStep.PlaceFirst, pieces[0]);
            tutorialSkipButton = RuntimeUiFactory.CreateButton(tutorialRoot, "Skip Tutorial", "Skip tutorial",
                Vector2.zero, new Vector2(180f, 58f), PanelColor);
            var skipImage = (Image)tutorialSkipButton.targetGraphic;
            skipImage.sprite = roundedBoxSprite;
            skipImage.type = Image.Type.Sliced;
            tutorialSkipButton.GetComponentInChildren<Text>().raycastTarget = false;
            var skipGroup = tutorialSkipButton.gameObject.AddComponent<CanvasGroup>();
            skipGroup.ignoreParentGroups = true;
            skipGroup.blocksRaycasts = true;
            skipGroup.interactable = true;
            tutorialSkipButton.onClick.AddListener(SkipLevelOneTutorial);
        }

        private void CreateTutorialHand()
        {
            if (tutorialHandSprite != null)
            {
                var hand = TutorialImage(tutorialHand, "Hand Pointer", tutorialHandSprite, Color.white,
                    Vector2.zero, new Vector2(100f, 100f));
                hand.preserveAspect = true;
                // The fingertip, rather than the artwork's center, follows the drag path.
                hand.rectTransform.pivot = new Vector2(.14f, .88f);
                return;
            }

            TutorialImage(tutorialHand, "Palm Outline", roundedBoxSprite, InkColor,
                new Vector2(15f, -38f), new Vector2(52f, 60f));
            TutorialImage(tutorialHand, "Palm", roundedBoxSprite, PanelColor,
                new Vector2(15f, -37f), new Vector2(43f, 51f));
            TutorialImage(tutorialHand, "Finger Outline", roundedBoxSprite, InkColor,
                new Vector2(0f, -13f), new Vector2(23f, 57f));
            TutorialImage(tutorialHand, "Finger", roundedBoxSprite, PanelColor,
                new Vector2(0f, -13f), new Vector2(15f, 49f));
            TutorialImage(tutorialHand, "Thumb", roundedBoxSprite, PanelColor,
                new Vector2(37f, -30f), new Vector2(17f, 35f));
        }

        private Image TutorialImage(Transform parent, string name, Sprite sprite, Color color,
            Vector2 position = default, Vector2 size = default)
        {
            var rect = RuntimeUiFactory.CreateRect(parent, name);
            RuntimeUiFactory.SetRect(rect, position, size == Vector2.zero ? new Vector2(22f, 22f) : size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void SetTutorialStep(TutorialStep step, PieceState piece)
        {
            tutorialStep = step;
            tutorialPiece = piece;
            tutorialElapsed = 0f;
            foreach (var image in tutorialTargets) { image.gameObject.SetActive(false); Destroy(image.gameObject); }
            tutorialTargets.Clear();
            tutorialTargetCoordinates.Clear();
            foreach (var cell in piece.Definition.Cells)
                tutorialTargetCoordinates.Add(new Vector2Int(piece.Definition.SolutionRow + cell.Row, piece.Definition.SolutionCol + cell.Col));
            foreach (var coordinate in tutorialTargetCoordinates)
            {
                var image = TutorialImage(tutorialTargetRoot, "Placement Target", roundedBoxSprite,
                    new Color(ValidColor.r, ValidColor.g, ValidColor.b, .25f), TutorialTargetCell(coordinate.x, coordinate.y),
                    new Vector2(boardCellWidth - 8f, boardCellHeight - 8f));
                var outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = ValidColor;
                outline.effectDistance = new Vector2(4f, -4f);
                tutorialTargets.Add(image);
                image.gameObject.SetActive(tutorialCelebrationRemaining <= 0f);
            }
            SetTutorialContext();
            if (tutorialCoach != null && !reducedMotion) StartCoroutine(PopTransform(tutorialCoach, 1.045f));
        }

        private bool TutorialAllowsPickup(PieceState piece) => !IsLevelOneTutorialOpen ||
            (tutorialCelebrationRemaining <= 0f && piece == tutorialPiece);
        // Undo/relocate is taught contextually after this level via TryShowMovePieceTip, not in this walkthrough.
        private bool TutorialAllowsShelfReturn(PieceState piece) => !IsLevelOneTutorialOpen;
        private bool TutorialAllowsPlacement(PieceState piece, int row, int col) => !IsLevelOneTutorialOpen ||
            (piece == tutorialPiece && row == piece.Definition.SolutionRow && col == piece.Definition.SolutionCol);

        private void TutorialPiecePlaced(PieceState piece)
        {
            if (!IsLevelOneTutorialOpen || piece != tutorialPiece) return;
            CelebrateTutorialAction(piece);
            if (tutorialStep == TutorialStep.PlaceFirst) SetTutorialStep(TutorialStep.PlaceSecond, pieces[1]);
            // If this second placement also completes the board, CheckWin (called right after
            // this method returns) locks input before UpdateLevelOneTutorial gets another turn,
            // so the deferred finish below would never run. Finish immediately instead and let
            // the bigger win celebration take over; otherwise finish once the "Purrfect!" beat
            // (see UpdateLevelOneTutorial) has played out.
            else if (WillCompleteLevel()) FinishLevelOneTutorial(resumeTimer: false);
            else tutorialPendingFinish = true;
        }

        private bool WillCompleteLevel()
        {
            if (occupancy.Count != activeLevel.ActiveCells.Count) return false;
            foreach (var candidate in pieces)
                if (!candidate.Placed) return false;
            return true;
        }

        // Onboarding is done: the rest of the board is finished through ordinary,
        // unrestricted gameplay.
        private void FinishLevelOneTutorial(bool resumeTimer = true)
        {
            if (!levelNavigationTesting)
            {
                PlayerPrefs.SetInt(LevelOneTutorialSeenKey, 1);
                PlayerPrefs.Save();
            }
            CancelLevelOneTutorial();
            if (resumeTimer) StartLevelTimer();
        }

        private Vector2 TutorialLocal(Vector3 world) => tutorialRoot.InverseTransformPoint(world);
        private Vector2 TutorialCell(int row, int col) => TutorialLocal(boardRoot.TransformPoint(
            BoardAnchoredToLocal(CellPosition(row, col))));
        private Vector2 TutorialTargetCell(int row, int col) => tutorialTargetRoot.InverseTransformPoint(
            boardRoot.TransformPoint(BoardAnchoredToLocal(CellPosition(row, col))));

        private bool TutorialUsesTouch => Application.isMobilePlatform;

        private Vector2 TutorialPickupScreen(PieceState piece)
        {
            var portrait = piece.CatViews[0].Portrait.rectTransform;
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, portrait.TransformPoint(portrait.rect.center));
        }

        private CellGrab TutorialGrip(PieceState piece, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(piece.Rect, screen, canvas.worldCamera, out Vector2 local);
            return GetGrabbedCell(piece, local.x + piece.Rect.rect.width * .5f, piece.Rect.rect.height * .5f - local.y);
        }

        private float TutorialLift()
        {
            float lift = TutorialUsesTouch ? (layoutProfile != null ? layoutProfile.TouchVisualLift : TouchVisualLift)
                : (layoutProfile != null ? layoutProfile.MouseVisualLift : MouseVisualLift);
            return lift * (canvas != null ? Mathf.Max(.25f, canvas.scaleFactor) : 1f);
        }

        private Vector2 TutorialPointerForAnchor(Vector2 start, Vector2 anchor)
        {
            // Invert the same monotonic lift/reach function used by real mouse and touch drags.
            float low = anchor.y - TutorialLift() - (layoutProfile != null ? layoutProfile.MaximumExtraReach : 180f) *
                (canvas != null ? Mathf.Max(.25f, canvas.scaleFactor) : 1f) - 1f;
            float high = anchor.y;
            for (int i = 0; i < 24; i++)
            {
                float middle = (low + high) * .5f;
                if (AssistedAnchorScreen(new Vector2(anchor.x, middle), start, TutorialUsesTouch, TutorialLift()).y < anchor.y)
                    low = middle;
                else high = middle;
            }
            return new Vector2(anchor.x, (low + high) * .5f);
        }

        private Vector2 TutorialPointerDestination(PieceState piece, int row, int col)
        {
            Vector2 start = TutorialPickupScreen(piece);
            CellGrab grip = TutorialGrip(piece, start);
            Vector2 logicalAnchor = BoardAnchoredToLocal(CellPosition(row + grip.Row, col + grip.Col)) +
                new Vector2((grip.OffsetX - .5f) * boardCellWidth, (.5f - grip.OffsetY) * boardCellHeight);
            Vector2 anchor = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, boardRoot.TransformPoint(logicalAnchor));
            return TutorialPointerForAnchor(start, anchor);
        }

        private Vector2 TutorialScreenLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tutorialRoot, screen, canvas.worldCamera, out Vector2 local);
            return local;
        }

        private void UpdateLevelOneTutorial()
        {
            if (!IsLevelOneTutorialOpen || tutorialRoot == null) return;
            if (!enableLevelOneTutorial) { EndLevelOneTutorialEarly(); return; }
            bool hidden = IsMetaUiOpen || IsResultScreenOpen || (GameSystem.Instance != null &&
                (GameSystem.Instance.IsPaused || GameSystem.Instance.IsSettingsOpen || !GameSystem.Instance.IsGameplayOpen));
            tutorialRoot.gameObject.SetActive(!hidden);
            tutorialTargetRoot.gameObject.SetActive(!hidden);
            if (tutorialSkipButton != null)
            {
                Vector2 boardBottom = TutorialLocal(boardRoot.TransformPoint(new Vector3(0f, boardRoot.rect.yMin, 0f)));
                Vector2 shelfTop = TutorialLocal(trayRoot.TransformPoint(new Vector3(0f, trayRoot.rect.yMax, 0f)));
                float gapCenter = (boardBottom.y + shelfTop.y) * .5f;
                tutorialSkipButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(boardBottom.x + 250f, gapCenter);
                tutorialCoach.anchoredPosition = new Vector2(boardBottom.x - 110f, gapCenter);
            }
            if (hidden || inputLocked) return;
            if (tutorialCelebrationRemaining > 0f) tutorialCelebrationRemaining = Mathf.Max(0f, tutorialCelebrationRemaining - Time.unscaledDeltaTime);
            else tutorialElapsed += Time.unscaledDeltaTime;
            if (tutorialPendingFinish && tutorialCelebrationRemaining <= 0f)
            {
                FinishLevelOneTutorial();
                return;
            }
            SetTutorialContext();
            Vector2 startScreen = TutorialPickupScreen(tutorialPiece);
            Vector2 start = TutorialScreenLocal(startScreen);
            int destinationRow = tutorialPiece.Definition.SolutionRow;
            int destinationCol = tutorialPiece.Definition.SolutionCol;
            Vector2 destinationScreen = TutorialPointerDestination(tutorialPiece, destinationRow, destinationCol);
            Vector2 destination = TutorialScreenLocal(destinationScreen);

            float wave = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(tutorialElapsed * 4f);
            tutorialHalo.rectTransform.anchoredPosition = start;
            tutorialHalo.rectTransform.sizeDelta = new Vector2(104f, 104f) * (1f + .12f * wave);
            tutorialHalo.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .2f + .15f * wave);
            tutorialHalo.gameObject.SetActive(drag == null && tutorialCelebrationRemaining <= 0f);
            for (int i = 0; i < tutorialDots.Count; i++)
            {
                var dot = tutorialDots[i];
                dot.gameObject.SetActive(drag == null && tutorialCelebrationRemaining <= 0f);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(start, destination, (i + 1f) / (tutorialDots.Count + 1f));
                float travel = reducedMotion ? .5f : Mathf.Repeat(tutorialElapsed * .8f - i * .1f, 1f);
                dot.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .24f + .65f * travel);
                dot.rectTransform.localScale = Vector3.one * (1f + .18f * travel);
            }
            for (int i = 0; i < tutorialTargets.Count; i++)
            {
                var image = tutorialTargets[i];
                image.gameObject.SetActive(tutorialCelebrationRemaining <= 0f);
                var coordinate = tutorialTargetCoordinates[i];
                image.rectTransform.anchoredPosition = TutorialTargetCell(coordinate.x, coordinate.y);
                image.color = new Color(ValidColor.r, ValidColor.g, ValidColor.b, .14f + .18f * wave);
                var outline = image.GetComponent<Outline>();
                if (outline != null) outline.effectColor = ValidColor;
                image.rectTransform.localScale = Vector3.one * (reducedMotion ? 1f : 1f + .025f * wave);
            }
            // A pickup pause, a smooth slide, and a release beat make the gesture legible.
            float cycle = tutorialElapsed % 3.1f;
            float t = Mathf.Clamp01((cycle - .45f) / 1.15f);
            Vector2 pointerScreen = reducedMotion ? destinationScreen : Vector2.Lerp(startScreen, destinationScreen, EaseOutCubic(t));
            tutorialHand.gameObject.SetActive(drag == null && tutorialCelebrationRemaining <= 0f);
            tutorialHand.anchoredPosition = TutorialScreenLocal(pointerScreen);
            tutorialHand.localScale = Vector3.one * (reducedMotion ? 1f : 1f - .08f * Mathf.Sin(Mathf.Clamp01(cycle / .45f) * Mathf.PI));

            Vector2 markerPosition = TutorialCell(destinationRow, destinationCol) + new Vector2(0f, 65f);
            tutorialCheck.gameObject.SetActive(tutorialCelebrationRemaining > 0f);
            tutorialCheck.anchoredPosition = tutorialCelebrationRemaining > 0f ? tutorialCelebrationPosition : markerPosition;
            float celebrationT = 1f - Mathf.Clamp01(tutorialCelebrationRemaining / .7f);
            float markerScale = reducedMotion ? 1f : 1f + Mathf.Sin(celebrationT * Mathf.PI) * .22f;
            tutorialCheck.localScale = Vector3.one * markerScale;
            tutorialHand.SetAsLastSibling();
            tutorialCheck.SetAsLastSibling();
            tutorialCoach.SetAsLastSibling();
            if (tutorialSkipButton != null) tutorialSkipButton.transform.SetAsLastSibling();
        }

        private void CancelLevelOneTutorial()
        {
            tutorialStep = TutorialStep.Off;
            tutorialPiece = null;
            tutorialPendingFinish = false;
            tutorialTargets.Clear();
            tutorialDots.Clear();
            tutorialTargetCoordinates.Clear();
            tutorialProgressDots.Clear();
            tutorialCoachGrid.Clear();
            tutorialCelebrationRemaining = 0f;
            if (tutorialRoot != null) { tutorialRoot.gameObject.SetActive(false); Destroy(tutorialRoot.gameObject); }
            if (tutorialTargetRoot != null) { tutorialTargetRoot.gameObject.SetActive(false); Destroy(tutorialTargetRoot.gameObject); }
            tutorialRoot = null;
            tutorialTargetRoot = null;
            tutorialSkipButton = null;
        }
    }
}
