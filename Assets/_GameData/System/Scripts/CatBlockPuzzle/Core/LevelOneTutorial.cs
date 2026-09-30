using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The hands-on Level 1 walkthrough: an animated hand shows the player how to drag the
    /// first two cats into their spaces, with glowing targets, a small context card and a
    /// short celebration after each real placement. While open it restricts which piece can
    /// be picked up and where it may land (see <see cref="IPlacementGate"/>).
    /// </summary>
    internal sealed class LevelOneTutorial : HostedService, IPlacementGate
    {
        // Kept intentionally short: place the first cat for real, then the second, then
        // hand the rest of the level back to the player.
        private enum TutorialStep { Off, PlaceFirst, PlaceSecond }
        private const int TutorialProgressStepCount = 2;

        private readonly PuzzleSession session;
        private readonly IPuzzleConfig config;
        private readonly CanvasSpace space;
        private readonly BoardController board;
        private readonly PieceDragController drag;
        private readonly PlayerSettings settings;
        private readonly FxSpawner fx;
        private readonly CatMoodPresenter mood;
        private readonly LevelTimer timer;
        private readonly InteractionGates gates;

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
        private readonly List<Image> tutorialTargets = new List<Image>();
        private readonly List<Image> tutorialDots = new List<Image>();
        private PieceState tutorialPiece;
        private float tutorialElapsed;
        private float tutorialCelebrationRemaining;
        private bool tutorialPendingFinish;
        private Vector2 tutorialCelebrationPosition;
        private Image tutorialCoachCat;
        private readonly List<Image> tutorialCoachGrid = new List<Image>();

        public LevelOneTutorial(IGameplayContext context, PuzzleSession session, IPuzzleConfig config, CanvasSpace space,
            BoardController board, PieceDragController drag, PlayerSettings settings, FxSpawner fx, CatMoodPresenter mood,
            LevelTimer timer, InteractionGates gates) : base(context)
        {
            this.session = session;
            this.config = config;
            this.space = space;
            this.board = board;
            this.drag = drag;
            this.settings = settings;
            this.fx = fx;
            this.mood = mood;
            this.timer = timer;
            this.gates = gates;
        }

        public bool IsOpen => tutorialStep != TutorialStep.Off;

        private List<PieceState> pieces => session.pieces;
        private RectTransform boardRoot => session.boardRoot;
        private RectTransform trayRoot => session.trayRoot;
        private RectTransform levelRoot => Hud.levelRoot;
        private PortraitLayoutProfile layoutProfile => Assets.layoutProfile;
        private Sprite circleSprite => Assets.circleSprite;
        private Sprite pawSprite => Assets.pawSprite;
        private Sprite roundedBoxSprite => Assets.roundedBoxSprite;
        private bool reducedMotion => settings.ReducedMotion;
        private bool enableLevelOneTutorial => config.LevelOneTutorialEnabled;
        private bool showTutorialCaptions => config.ShowTutorialCaptions;
        private float boardCellWidth => session.boardCellWidth;
        private float boardCellHeight => session.boardCellHeight;

        public void SetEnabled(bool enabled)
        {
            config.LevelOneTutorialEnabled = enabled;
            if (!enabled) EndEarly();
        }

        private void EndEarly()
        {
            if (!IsOpen) return;
            drag.CancelActiveDragToRest();
            Cancel();
            // Preserve any return/landing animation's input lock until it finishes.
            timer.StartLevelTimer();
        }

        public void Begin()
        {
            if (!enableLevelOneTutorial || session.levelIndex != 0 || pieces.Count < 2 || PlayerPrefs.GetInt(PuzzleSaveKeys.LevelOneTutorialSeen, 0) != 0) return;
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
            tutorialHalo = RuntimeUiFactory.CreateImage(tutorialRoot, "Pickup Halo", circleSprite, GoldColor);
            for (int i = 0; i < 9; i++)
                tutorialDots.Add(RuntimeUiFactory.CreateImage(tutorialRoot, "Drag Path", i % 2 == 0 ? pawSprite : circleSprite, GoldColor,
                    Vector2.zero, new Vector2(i % 2 == 0 ? 24f : 14f, i % 2 == 0 ? 24f : 14f)));
            tutorialHand = RuntimeUiFactory.CreateRect(tutorialRoot, "Animated Hand");
            CreateTutorialHand();
            tutorialCheck = RuntimeUiFactory.CreateRect(tutorialRoot, "Successful Fit Check");
            RuntimeUiFactory.CreateImage(tutorialCheck, "Badge", circleSprite, PanelColor, Vector2.zero, new Vector2(78f, 78f));
            var checkA = RuntimeUiFactory.CreateImage(tutorialCheck, "Check Short", roundedBoxSprite, TargetDeepColor, new Vector2(-12f, -5f), new Vector2(27f, 11f));
            var checkB = RuntimeUiFactory.CreateImage(tutorialCheck, "Check Long", roundedBoxSprite, TargetDeepColor, new Vector2(9f, 3f), new Vector2(44f, 11f));
            checkA.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
            checkB.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            CreateTutorialCoach();
            SetTutorialStep(TutorialStep.PlaceFirst, pieces[0]);
        }

        private void CreateTutorialHand()
        {
            Sprite tutorialHandSprite = config.TutorialHandSprite;
            if (tutorialHandSprite != null)
            {
                var hand = RuntimeUiFactory.CreateImage(tutorialHand, "Hand Pointer", tutorialHandSprite, Color.white,
                    Vector2.zero, new Vector2(100f, 100f));
                hand.preserveAspect = true;
                // The fingertip, rather than the artwork's center, follows the drag path.
                hand.rectTransform.pivot = new Vector2(.14f, .88f);
                return;
            }

            RuntimeUiFactory.CreateImage(tutorialHand, "Palm Outline", roundedBoxSprite, InkColor,
                new Vector2(15f, -38f), new Vector2(52f, 60f));
            RuntimeUiFactory.CreateImage(tutorialHand, "Palm", roundedBoxSprite, PanelColor,
                new Vector2(15f, -37f), new Vector2(43f, 51f));
            RuntimeUiFactory.CreateImage(tutorialHand, "Finger Outline", roundedBoxSprite, InkColor,
                new Vector2(0f, -13f), new Vector2(23f, 57f));
            RuntimeUiFactory.CreateImage(tutorialHand, "Finger", roundedBoxSprite, PanelColor,
                new Vector2(0f, -13f), new Vector2(15f, 49f));
            RuntimeUiFactory.CreateImage(tutorialHand, "Thumb", roundedBoxSprite, PanelColor,
                new Vector2(37f, -30f), new Vector2(17f, 35f));
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
                var image = RuntimeUiFactory.CreateImage(tutorialTargetRoot, "Placement Target", roundedBoxSprite,
                    new Color(ValidColor.r, ValidColor.g, ValidColor.b, .25f), TutorialTargetCell(coordinate.x, coordinate.y),
                    new Vector2(boardCellWidth - 8f, boardCellHeight - 8f));
                var outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = ValidColor;
                outline.effectDistance = new Vector2(4f, -4f);
                tutorialTargets.Add(image);
                image.gameObject.SetActive(tutorialCelebrationRemaining <= 0f);
            }
            SetTutorialContext();
            if (tutorialCoach != null && !reducedMotion) StartCoroutine(fx.PopTransform(tutorialCoach, 1.045f));
        }

        public bool TutorialAllowsPickup(PieceState piece) => !IsOpen ||
            (tutorialCelebrationRemaining <= 0f && piece == tutorialPiece);
        // Undo/relocate is taught contextually after this level via the move-piece tip, not in this walkthrough.
        public bool TutorialAllowsShelfReturn(PieceState piece) => !IsOpen;
        public bool TutorialAllowsPlacement(PieceState piece, int row, int col) => !IsOpen ||
            (piece == tutorialPiece && row == piece.Definition.SolutionRow && col == piece.Definition.SolutionCol);

        public void OnPiecePlaced(PieceState piece)
        {
            if (!IsOpen || piece != tutorialPiece) return;
            CelebrateTutorialAction(piece);
            if (tutorialStep == TutorialStep.PlaceFirst) SetTutorialStep(TutorialStep.PlaceSecond, pieces[1]);
            // If this second placement also completes the board, the win check (run right after
            // this method returns) locks input before Update gets another turn, so the deferred
            // finish below would never run. Finish immediately instead and let the bigger win
            // celebration take over; otherwise finish once the "Purrfect!" beat (see Update) has
            // played out.
            else if (WillCompleteLevel()) Finish(resumeTimer: false);
            else tutorialPendingFinish = true;
        }

        private bool WillCompleteLevel()
        {
            if (session.occupancy.Count != session.activeLevel.ActiveCells.Count) return false;
            foreach (var candidate in pieces)
                if (!candidate.Placed) return false;
            return true;
        }

        // Onboarding is done: the rest of the board is finished through ordinary,
        // unrestricted gameplay.
        private void Finish(bool resumeTimer = true)
        {
            if (!session.levelNavigationTesting)
            {
                PlayerPrefs.SetInt(PuzzleSaveKeys.LevelOneTutorialSeen, 1);
                PlayerPrefs.Save();
            }
            Cancel();
            if (resumeTimer) timer.StartLevelTimer();
        }

        private Vector2 TutorialLocal(Vector3 world) => tutorialRoot.InverseTransformPoint(world);
        public Vector2 TutorialCell(int row, int col) => TutorialLocal(boardRoot.TransformPoint(
            board.BoardAnchoredToLocal(board.CellPosition(row, col))));
        private Vector2 TutorialTargetCell(int row, int col) => tutorialTargetRoot.InverseTransformPoint(
            boardRoot.TransformPoint(board.BoardAnchoredToLocal(board.CellPosition(row, col))));

        private bool TutorialUsesTouch => Application.isMobilePlatform;

        public Vector2 TutorialPickupScreen(PieceState piece)
        {
            var portrait = piece.CatViews[0].Portrait.rectTransform;
            return space.WorldToScreen(portrait.TransformPoint(portrait.rect.center));
        }

        private CellGrab TutorialGrip(PieceState piece, Vector2 screen)
        {
            Vector2 local = space.ScreenToLocal(piece.Rect, screen);
            return drag.GetGrabbedCell(piece, local.x + piece.Rect.rect.width * .5f, piece.Rect.rect.height * .5f - local.y);
        }

        private float TutorialLift()
        {
            float lift = TutorialUsesTouch ? (layoutProfile != null ? layoutProfile.TouchVisualLift : TouchVisualLift)
                : (layoutProfile != null ? layoutProfile.MouseVisualLift : MouseVisualLift);
            return lift * space.ScaleFactor;
        }

        private Vector2 TutorialPointerForAnchor(Vector2 start, Vector2 anchor)
        {
            // Invert the same monotonic lift/reach function used by real mouse and touch drags.
            float low = anchor.y - TutorialLift() - (layoutProfile != null ? layoutProfile.MaximumExtraReach : DragExtraReach) *
                space.ScaleFactor - 1f;
            float high = anchor.y;
            for (int i = 0; i < 24; i++)
            {
                float middle = (low + high) * .5f;
                if (drag.AssistedAnchorScreen(new Vector2(anchor.x, middle), start, TutorialUsesTouch, TutorialLift()).y < anchor.y)
                    low = middle;
                else high = middle;
            }
            return new Vector2(anchor.x, (low + high) * .5f);
        }

        public Vector2 TutorialPointerDestination(PieceState piece, int row, int col)
        {
            Vector2 start = TutorialPickupScreen(piece);
            CellGrab grip = TutorialGrip(piece, start);
            Vector2 logicalAnchor = board.BoardAnchoredToLocal(board.CellPosition(row + grip.Row, col + grip.Col)) +
                new Vector2((grip.OffsetX - .5f) * boardCellWidth, (.5f - grip.OffsetY) * boardCellHeight);
            Vector2 anchor = space.WorldToScreen(boardRoot.TransformPoint(logicalAnchor));
            return TutorialPointerForAnchor(start, anchor);
        }

        private Vector2 TutorialScreenLocal(Vector2 screen)
        {
            return space.ScreenToLocal(tutorialRoot, screen);
        }

        public void Update()
        {
            if (!IsOpen || tutorialRoot == null) return;
            if (!enableLevelOneTutorial) { EndEarly(); return; }
            bool hidden = gates.MetaUiOpen || gates.ResultScreenOpen || gates.SystemPaused || gates.SettingsOpen || gates.GameplayHidden;
            tutorialRoot.gameObject.SetActive(!hidden);
            tutorialTargetRoot.gameObject.SetActive(!hidden);
            Vector2 boardBottom = TutorialLocal(boardRoot.TransformPoint(new Vector3(0f, boardRoot.rect.yMin, 0f)));
            Vector2 shelfTop = TutorialLocal(trayRoot.TransformPoint(new Vector3(0f, trayRoot.rect.yMax, 0f)));
            float gapCenter = (boardBottom.y + shelfTop.y) * .5f;
            tutorialCoach.anchoredPosition = new Vector2(boardBottom.x - 110f, gapCenter);
            if (hidden || session.inputLocked) return;
            if (tutorialCelebrationRemaining > 0f) tutorialCelebrationRemaining = Mathf.Max(0f, tutorialCelebrationRemaining - Time.unscaledDeltaTime);
            else tutorialElapsed += Time.unscaledDeltaTime;
            if (tutorialPendingFinish && tutorialCelebrationRemaining <= 0f)
            {
                Finish();
                return;
            }
            SetTutorialContext();
            Vector2 startScreen = TutorialPickupScreen(tutorialPiece);
            Vector2 start = TutorialScreenLocal(startScreen);
            int destinationRow = tutorialPiece.Definition.SolutionRow;
            int destinationCol = tutorialPiece.Definition.SolutionCol;
            Vector2 destinationScreen = TutorialPointerDestination(tutorialPiece, destinationRow, destinationCol);
            Vector2 destination = TutorialScreenLocal(destinationScreen);

            bool dragging = session.drag != null;
            float wave = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(tutorialElapsed * 4f);
            tutorialHalo.rectTransform.anchoredPosition = start;
            tutorialHalo.rectTransform.sizeDelta = new Vector2(104f, 104f) * (1f + .12f * wave);
            tutorialHalo.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .2f + .15f * wave);
            tutorialHalo.gameObject.SetActive(!dragging && tutorialCelebrationRemaining <= 0f);
            for (int i = 0; i < tutorialDots.Count; i++)
            {
                var dot = tutorialDots[i];
                dot.gameObject.SetActive(!dragging && tutorialCelebrationRemaining <= 0f);
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
            tutorialHand.gameObject.SetActive(!dragging && tutorialCelebrationRemaining <= 0f);
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
        }

        public void Cancel()
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
        }

        private void CreateTutorialCoach()
        {
            tutorialCoach = RuntimeUiFactory.CreateRect(tutorialRoot, "Tutorial Context Card");
            RuntimeUiFactory.SetRect(tutorialCoach, Vector2.zero, new Vector2(440f, 94f));
            var panel = tutorialCoach.gameObject.AddComponent<Image>();
            panel.sprite = roundedBoxSprite;
            panel.type = Image.Type.Sliced;
            panel.color = PanelColor;
            panel.raycastTarget = false;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.25f, .15f, .1f, .18f);
            shadow.effectDistance = new Vector2(0f, -6f);
            tutorialCoachTitle = RuntimeUiFactory.CreateText(tutorialCoach, "Context Title", "", 27, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(tutorialCoachTitle.rectTransform, new Vector2(0f, 22f), new Vector2(412f, 32f));
            tutorialCoachBody = RuntimeUiFactory.CreateText(tutorialCoach, "Context Detail", "", 21, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(tutorialCoachBody.rectTransform, new Vector2(0f, -10f), new Vector2(412f, 38f));
            tutorialCoachTitle.raycastTarget = tutorialCoachBody.raycastTarget = false;
            tutorialCoachPictures = RuntimeUiFactory.CreateRect(tutorialCoach, "Visual Context");
            tutorialCoachCat = RuntimeUiFactory.CreateImage(tutorialCoachPictures, "Cat Group", mood.Portrait(CatMood.Happy, 0), Color.white,
                new Vector2(-108f, 8f), new Vector2(53f, 53f));
            RuntimeUiFactory.CreateImage(tutorialCoachPictures, "Move Line", roundedBoxSprite, GoldColor,
                new Vector2(-40f, 8f), new Vector2(45f, 8f));
            var arrow = RuntimeUiFactory.CreateImage(tutorialCoachPictures, "Move Arrow", roundedBoxSprite, GoldColor,
                new Vector2(-17f, 15f), new Vector2(24f, 8f));
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -40f);
            arrow = RuntimeUiFactory.CreateImage(tutorialCoachPictures, "Move Arrow", roundedBoxSprite, GoldColor,
                new Vector2(-17f, 1f), new Vector2(24f, 8f));
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 40f);
            for (int i = 0; i < 4; i++)
                tutorialCoachGrid.Add(RuntimeUiFactory.CreateImage(tutorialCoachPictures, "Fill A Space", roundedBoxSprite, ValidColor,
                    new Vector2(33f + (i % 2) * 23f, 20f - (i / 2) * 23f), new Vector2(19f, 19f)));
            RuntimeUiFactory.CreateImage(tutorialCoachPictures, "Happy Home", pawSprite, GoldColor,
                new Vector2(110f, 8f), new Vector2(46f, 46f));
            for (int i = 0; i < TutorialProgressStepCount; i++)
                tutorialProgressDots.Add(RuntimeUiFactory.CreateImage(tutorialCoach, "Learning Progress", circleSprite, TargetColor,
                    new Vector2((i - (TutorialProgressStepCount - 1) * .5f) * 32f, -35f), new Vector2(12f, 12f)));
        }

        private int TutorialProgressIndex() => tutorialStep == TutorialStep.PlaceFirst ? 0 : 1;

        private void SetTutorialContext()
        {
            if (tutorialCoachTitle == null) return;
            string title;
            string detail;
            if (tutorialStep == TutorialStep.PlaceFirst)
            {
                title = "Give this cat a home";
                detail = "Pick up the glowing group and drag it into its matching space.";
            }
            else
            {
                title = "Now the second one";
                detail = "Pick up this glowing group and drag it into its matching space.";
            }
            DragState drag = session.drag;
            if (tutorialCelebrationRemaining > 0f)
            { title = "Purrfect!"; detail = "That's it! You're ready for the next move."; }
            else if (drag != null && drag.Piece == tutorialPiece)
            {
                title = drag.Valid ? "Green means it fits!" : "Keep sliding";
                detail = drag.Valid ? "Release now and the whole group will snap into place." : "Follow the glowing spaces. Red spots won't accept the group.";
            }
            tutorialCoachTitle.text = title;
            tutorialCoachBody.text = detail;
            tutorialCoachTitle.gameObject.SetActive(showTutorialCaptions);
            tutorialCoachBody.gameObject.SetActive(showTutorialCaptions);
            tutorialCoachPictures.gameObject.SetActive(!showTutorialCaptions);
            tutorialCoachCat.sprite = mood.Portrait(CatMood.Happy, tutorialPiece.AtlasIndex);
            foreach (var gridCell in tutorialCoachGrid) gridCell.color = ValidColor;
            int progress = TutorialProgressIndex();
            for (int i = 0; i < tutorialProgressDots.Count; i++)
            {
                tutorialProgressDots[i].color = i < progress ? TargetDeepColor : i == progress ? GoldColor : TargetColor;
                tutorialProgressDots[i].rectTransform.localScale = Vector3.one * (i == progress ? 1.3f : 1f);
            }
        }

        private void CelebrateTutorialAction(PieceState piece)
        {
            tutorialCelebrationRemaining = .7f;
            tutorialCelebrationPosition = TutorialLocal(piece.Rect.position);
            Vector2 screen = space.WorldToScreen(piece.Rect.position);
            fx.SpawnSnapRing(screen, GoldColor);
            fx.SpawnFixedBurst(screen, reducedMotion ? 2 : 8, GoldColor);
        }
    }
}
