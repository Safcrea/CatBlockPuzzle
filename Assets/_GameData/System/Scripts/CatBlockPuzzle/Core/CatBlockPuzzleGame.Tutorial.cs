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
        private enum TutorialStep { Off, GoalDemo, FitDemo, EdgeDemo, PlaceFirst, ReturnFirst, ReplaceFirst, OverlapDemo, FillBoard }
        private TutorialStep tutorialStep;
        private RectTransform tutorialRoot;
        private RectTransform tutorialGhost;
        private RectTransform tutorialHand;
        private RectTransform tutorialMark;
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
        private readonly List<Image> tutorialGhostCats = new List<Image>();
        private PieceState tutorialPiece;
        private float tutorialElapsed;
        private float tutorialCelebrationRemaining;
        private bool tutorialDemoFeedbackPlayed;
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
            tutorialHalo = TutorialImage(tutorialRoot, "Pickup Halo", circleSprite, GoldColor);
            for (int i = 0; i < 9; i++)
                tutorialDots.Add(TutorialImage(tutorialRoot, "Drag Path", i % 2 == 0 ? pawSprite : circleSprite, GoldColor,
                    Vector2.zero, new Vector2(i % 2 == 0 ? 24f : 14f, i % 2 == 0 ? 24f : 14f)));
            tutorialHand = RuntimeUiFactory.CreateRect(tutorialRoot, "Animated Hand");
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
            tutorialMark = RuntimeUiFactory.CreateRect(tutorialRoot, "Invalid Cross");
            TutorialImage(tutorialMark, "Badge", circleSprite, PanelColor, Vector2.zero, new Vector2(74f, 74f));
            var a = TutorialImage(tutorialMark, "Cross A", roundedBoxSprite, InvalidColor, Vector2.zero, new Vector2(44f, 10f));
            var b = TutorialImage(tutorialMark, "Cross B", roundedBoxSprite, InvalidColor, Vector2.zero, new Vector2(44f, 10f));
            a.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            b.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
            tutorialCheck = RuntimeUiFactory.CreateRect(tutorialRoot, "Successful Fit Check");
            TutorialImage(tutorialCheck, "Badge", circleSprite, PanelColor, Vector2.zero, new Vector2(78f, 78f));
            var checkA = TutorialImage(tutorialCheck, "Check Short", roundedBoxSprite, TargetDeepColor, new Vector2(-12f, -5f), new Vector2(27f, 11f));
            var checkB = TutorialImage(tutorialCheck, "Check Long", roundedBoxSprite, TargetDeepColor, new Vector2(9f, 3f), new Vector2(44f, 11f));
            checkA.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
            checkB.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            CreateTutorialCoach();
            SetTutorialStep(TutorialStep.GoalDemo, pieces[0]);
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
            tutorialDemoFeedbackPlayed = false;
            if (tutorialGhost != null) { tutorialGhost.gameObject.SetActive(false); Destroy(tutorialGhost.gameObject); }
            foreach (var image in tutorialTargets) { image.gameObject.SetActive(false); Destroy(image.gameObject); }
            tutorialTargets.Clear();
            tutorialTargetCoordinates.Clear();
            tutorialGhostCats.Clear();
            tutorialGhost = RuntimeUiFactory.CreateRect(tutorialRoot, "Demonstration Cat Group");
            foreach (var cell in piece.Definition.Cells)
            {
                Vector2 position = new Vector2(cell.Col * (boardCellWidth + BoardGap), -cell.Row * (boardCellHeight + BoardGap));
                var image = TutorialImage(tutorialGhost, "Ghost Cat", CatPortrait(CatMood.Neutral, piece.AtlasIndex),
                    new Color(1f, 1f, 1f, .62f), position, new Vector2(boardCellWidth, boardCellHeight));
                image.preserveAspect = true;
                tutorialGhostCats.Add(image);
            }
            if (step == TutorialStep.ReturnFirst)
            {
                tutorialTargets.Add(TutorialImage(tutorialRoot, "Shelf Target", roundedBoxSprite,
                    new Color(ValidColor.r, ValidColor.g, ValidColor.b, .22f)));
            }
            else
            {
                if (step == TutorialStep.GoalDemo)
                    foreach (var coordinate in activeLevel.ActiveCells) tutorialTargetCoordinates.Add(coordinate);
                else foreach (var cell in piece.Definition.Cells)
                    tutorialTargetCoordinates.Add(new Vector2Int(piece.Definition.SolutionRow + cell.Row, piece.Definition.SolutionCol + cell.Col));
                foreach (var coordinate in tutorialTargetCoordinates)
                {
                    var image = TutorialImage(tutorialRoot, "Placement Target", roundedBoxSprite,
                        new Color(ValidColor.r, ValidColor.g, ValidColor.b, .25f), Vector2.zero,
                        new Vector2(boardCellWidth - 8f, boardCellHeight - 8f));
                    var outline = image.gameObject.AddComponent<Outline>();
                    outline.effectColor = ValidColor;
                    outline.effectDistance = new Vector2(4f, -4f);
                    if (step == TutorialStep.GoalDemo)
                    {
                        foreach (var goalPiece in pieces)
                            foreach (var cell in goalPiece.Definition.Cells)
                                if (coordinate == new Vector2Int(goalPiece.Definition.SolutionRow + cell.Row, goalPiece.Definition.SolutionCol + cell.Col))
                                {
                                    image.sprite = CatPortrait(CatMood.Happy, goalPiece.AtlasIndex);
                                    image.preserveAspect = true;
                                }
                    }
                    tutorialTargets.Add(image);
                }
            }
            SetTutorialContext();
            if (tutorialCoach != null && !reducedMotion) StartCoroutine(PopTransform(tutorialCoach, 1.045f));
        }

        private bool TutorialIsDemo => tutorialStep == TutorialStep.GoalDemo || tutorialStep == TutorialStep.FitDemo ||
            tutorialStep == TutorialStep.EdgeDemo || tutorialStep == TutorialStep.OverlapDemo;
        private bool TutorialAllowsPickup(PieceState piece) => !IsLevelOneTutorialOpen ||
            (!TutorialIsDemo && tutorialCelebrationRemaining <= 0f && piece == tutorialPiece);
        private bool TutorialAllowsShelfReturn(PieceState piece) => !IsLevelOneTutorialOpen ||
            (tutorialStep == TutorialStep.ReturnFirst && piece == tutorialPiece);
        private bool TutorialAllowsPlacement(PieceState piece, int row, int col) => !IsLevelOneTutorialOpen ||
            (!TutorialIsDemo && tutorialStep != TutorialStep.ReturnFirst && piece == tutorialPiece &&
             row == piece.Definition.SolutionRow && col == piece.Definition.SolutionCol);

        private void TutorialPiecePlaced(PieceState piece)
        {
            if (!IsLevelOneTutorialOpen || piece != tutorialPiece) return;
            CelebrateTutorialAction(piece);
            if (tutorialStep == TutorialStep.PlaceFirst) SetTutorialStep(TutorialStep.ReturnFirst, piece);
            else if (tutorialStep == TutorialStep.ReplaceFirst) SetTutorialStep(TutorialStep.OverlapDemo, pieces[1]);
            else if (tutorialStep == TutorialStep.FillBoard)
            {
                foreach (var remaining in pieces)
                    if (!remaining.Placed) { SetTutorialStep(TutorialStep.FillBoard, remaining); return; }
                if (!levelNavigationTesting)
                {
                    PlayerPrefs.SetInt(LevelOneTutorialSeenKey, 1);
                    PlayerPrefs.Save();
                }
                CancelLevelOneTutorial();
            }
        }

        private void TutorialPieceReturned(PieceState piece)
        {
            if (tutorialStep == TutorialStep.ReturnFirst && piece == tutorialPiece)
            {
                CelebrateTutorialAction(piece);
                SetTutorialStep(TutorialStep.ReplaceFirst, piece);
            }
        }

        private Vector2 TutorialLocal(Vector3 world) => tutorialRoot.InverseTransformPoint(world);
        private Vector2 TutorialCell(int row, int col) => TutorialLocal(boardRoot.TransformPoint(
            BoardAnchoredToLocal(CellPosition(row, col))));

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
            float demoDuration = tutorialStep == TutorialStep.GoalDemo ? 1.8f : tutorialStep == TutorialStep.FitDemo ? 3.3f : 3.6f;
            if (TutorialIsDemo && tutorialElapsed >= demoDuration)
            {
                SetTutorialStep(tutorialStep == TutorialStep.GoalDemo ? TutorialStep.FitDemo : tutorialStep == TutorialStep.FitDemo
                    ? TutorialStep.EdgeDemo : tutorialStep == TutorialStep.EdgeDemo ? TutorialStep.PlaceFirst : TutorialStep.FillBoard, tutorialPiece);
            }
            SetTutorialContext();
            bool demo = TutorialIsDemo;
            bool goal = tutorialStep == TutorialStep.GoalDemo;
            bool invalidDemo = tutorialStep == TutorialStep.EdgeDemo || tutorialStep == TutorialStep.OverlapDemo;
            bool returning = tutorialStep == TutorialStep.ReturnFirst;
            Vector2 startScreen = TutorialPickupScreen(tutorialPiece);
            Vector2 start = TutorialScreenLocal(startScreen);
            CellGrab grip = TutorialGrip(tutorialPiece, startScreen);
            int destinationRow = tutorialPiece.Definition.SolutionRow;
            int destinationCol = tutorialPiece.Definition.SolutionCol;
            // Use a cell shared with the already placed group to make the overlap explicit.
            if (tutorialStep == TutorialStep.OverlapDemo)
            {
                var firstCell = pieces[0].Definition.Cells[0];
                destinationRow = pieces[0].Row + firstCell.Row;
                destinationCol = pieces[0].Col + firstCell.Col;
            }
            else if (tutorialStep == TutorialStep.EdgeDemo)
                destinationCol = -1;
            Vector2 destinationScreen = returning ? TutorialPointerForAnchor(startScreen,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, trayRoot.TransformPoint(trayRoot.rect.center))) :
                TutorialPointerDestination(tutorialPiece, destinationRow, destinationCol);
            Vector2 destination = TutorialScreenLocal(destinationScreen);

            float wave = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(tutorialElapsed * 4f);
            tutorialHalo.rectTransform.anchoredPosition = start;
            tutorialHalo.rectTransform.sizeDelta = new Vector2(104f, 104f) * (1f + .12f * wave);
            tutorialHalo.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .2f + .15f * wave);
            tutorialHalo.gameObject.SetActive(drag == null && !goal && tutorialCelebrationRemaining <= 0f);
            for (int i = 0; i < tutorialDots.Count; i++)
            {
                var dot = tutorialDots[i];
                dot.gameObject.SetActive(drag == null && !goal && tutorialCelebrationRemaining <= 0f);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(start, destination, (i + 1f) / (tutorialDots.Count + 1f));
                float travel = reducedMotion ? .5f : Mathf.Repeat(tutorialElapsed * .8f - i * .1f, 1f);
                dot.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .24f + .65f * travel);
                dot.rectTransform.localScale = Vector3.one * (1f + .18f * travel);
            }
            for (int i = 0; i < tutorialTargets.Count; i++)
            {
                var image = tutorialTargets[i];
                image.gameObject.SetActive(tutorialCelebrationRemaining <= 0f);
                if (returning)
                {
                    image.rectTransform.anchoredPosition = destination;
                    image.rectTransform.sizeDelta = trayRoot.rect.size;
                }
                else
                {
                    var coordinate = tutorialTargetCoordinates[i];
                    if (invalidDemo)
                    {
                        var cell = tutorialPiece.Definition.Cells[i];
                        coordinate = new Vector2Int(destinationRow + cell.Row, destinationCol + cell.Col);
                    }
                    image.rectTransform.anchoredPosition = TutorialCell(coordinate.x, coordinate.y);
                }
                Color accent = invalidDemo ? InvalidColor : ValidColor;
                image.color = goal ? new Color(1f, 1f, 1f, .45f + .35f * wave) : new Color(accent.r, accent.g, accent.b, .14f + .18f * wave);
                var outline = image.GetComponent<Outline>();
                if (outline != null) outline.effectColor = accent;
                image.rectTransform.localScale = Vector3.one * (reducedMotion ? 1f : 1f + .025f * wave);
            }
            // A pickup pause, a smooth slide, and a release beat make the gesture legible.
            float cycle = demo ? tutorialElapsed : tutorialElapsed % 3.1f;
            float t = Mathf.Clamp01((cycle - .45f) / 1.15f);
            Vector2 pointerScreen = reducedMotion ? destinationScreen : Vector2.Lerp(startScreen, destinationScreen, EaseOutCubic(t));
            tutorialHand.gameObject.SetActive(drag == null && !goal && tutorialCelebrationRemaining <= 0f);
            tutorialHand.anchoredPosition = TutorialScreenLocal(pointerScreen);
            tutorialHand.localScale = Vector3.one * (reducedMotion ? 1f : 1f - .08f * Mathf.Sin(Mathf.Clamp01(cycle / .45f) * Mathf.PI));
            tutorialGhost.gameObject.SetActive(demo && !goal);
            if (demo)
            {
                // The ghost is presentation only: demonstrations never occupy board cells.
                float returnT = reducedMotion ? 0f : Mathf.Clamp01((tutorialElapsed - 2.5f) / .7f);
                float growth = reducedMotion ? 1f : t * (1f - returnT);
                float width = Mathf.Lerp(tutorialPiece.CellWidth, boardCellWidth, growth);
                float height = Mathf.Lerp(tutorialPiece.CellHeight, boardCellHeight, growth);
                float gapX = Mathf.Lerp(tutorialPiece.GapX, BoardGap, growth);
                float gapY = Mathf.Lerp(tutorialPiece.GapY, BoardGap, growth);
                Vector2 anchorScreen = AssistedAnchorScreen(pointerScreen, startScreen, TutorialUsesTouch,
                    TutorialLift() * (reducedMotion ? 1f : Mathf.Clamp01(cycle / .45f)));
                Vector2 gripOffset = new Vector2(-grip.Col * (width + gapX) + (.5f - grip.OffsetX) * width,
                    grip.Row * (height + gapY) + (grip.OffsetY - .5f) * height);
                Vector2 ghostPosition = TutorialScreenLocal(anchorScreen) + gripOffset;
                tutorialGhost.anchoredPosition = Vector2.Lerp(ghostPosition, start, EaseOutCubic(returnT));
                tutorialGhost.localScale = Vector3.one;
                if (invalidDemo && cycle >= 1.6f && cycle < 2.1f && !reducedMotion)
                    tutorialGhost.anchoredPosition += new Vector2(Mathf.Sin((cycle - 1.6f) * 55f) * 9f * (1f - (cycle - 1.6f) / .5f), 0f);
                tutorialHand.anchoredPosition = Vector2.Lerp(TutorialScreenLocal(pointerScreen), start, EaseOutCubic(returnT));
                for (int i = 0; i < tutorialGhostCats.Count; i++)
                {
                    var cat = tutorialGhostCats[i];
                    var cell = tutorialPiece.Definition.Cells[i];
                    cat.rectTransform.anchoredPosition = new Vector2(cell.Col * (width + gapX), -cell.Row * (height + gapY));
                    cat.rectTransform.sizeDelta = new Vector2(width, height);
                    cat.sprite = CatPortrait(cycle >= 1.6f ? invalidDemo ? CatMood.Worried : CatMood.Happy : CatMood.Neutral, tutorialPiece.AtlasIndex);
                    cat.color = invalidDemo && cycle >= 1.6f ? new Color(1f, .5f, .5f, .85f) : new Color(1f, 1f, 1f, .82f);
                }
            }
            if (demo && !tutorialDemoFeedbackPlayed && cycle >= (goal ? .9f : 1.6f))
            {
                tutorialDemoFeedbackPlayed = true;
                PlaySfx(invalidDemo ? "Wrong" : "Snap");
                if (invalidDemo) haptics?.PlayWrongMove();
                else
                {
                    haptics?.PlaySnap();
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, boardRoot.position);
                    SpawnSnapRing(screen, GoldColor);
                    SpawnFixedBurst(screen, reducedMotion ? 2 : 10, GoldColor);
                }
            }
            Vector2 markerPosition = goal ? TutorialLocal(boardRoot.position) : TutorialCell(destinationRow, destinationCol) + new Vector2(0f, 65f);
            tutorialMark.gameObject.SetActive(invalidDemo && cycle >= 1.6f && cycle < 2.6f);
            tutorialMark.anchoredPosition = markerPosition;
            tutorialCheck.gameObject.SetActive(tutorialCelebrationRemaining > 0f || (demo && !invalidDemo && cycle >= (goal ? .9f : 1.6f)));
            tutorialCheck.anchoredPosition = tutorialCelebrationRemaining > 0f ? tutorialCelebrationPosition : markerPosition;
            float markerScale = reducedMotion ? 1f : 1f + Mathf.Sin(Mathf.Clamp01((cycle - 1.6f) / .3f) * Mathf.PI) * .22f;
            tutorialMark.localScale = tutorialCheck.localScale = Vector3.one * markerScale;
            tutorialHand.SetAsLastSibling();
            tutorialMark.SetAsLastSibling();
            tutorialCheck.SetAsLastSibling();
            tutorialCoach.SetAsLastSibling();
            if (tutorialSkipButton != null) tutorialSkipButton.transform.SetAsLastSibling();
        }

        private void CancelLevelOneTutorial()
        {
            tutorialStep = TutorialStep.Off;
            tutorialPiece = null;
            tutorialTargets.Clear();
            tutorialDots.Clear();
            tutorialGhostCats.Clear();
            tutorialTargetCoordinates.Clear();
            tutorialProgressDots.Clear();
            tutorialCoachGrid.Clear();
            tutorialCelebrationRemaining = 0f;
            if (tutorialRoot != null) { tutorialRoot.gameObject.SetActive(false); Destroy(tutorialRoot.gameObject); }
            tutorialRoot = null;
            tutorialSkipButton = null;
        }
    }
}
