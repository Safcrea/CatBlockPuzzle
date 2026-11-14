using AVNEvents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The hands-on Level 1 walkthrough: an animated hand shows the player how to drag the
    /// first two cats into their spaces, with glowing targets, two small in-world tags ("Drag me" on the cat,
    /// "Drop here" on the target, no dim and no box) and a short celebration after each real placement. While open it restricts which piece can
    /// be picked up and where it may land (see <see cref="IPlacementGate"/>).
    /// </summary>
    internal sealed class LevelOneTutorial : HostedService, IPlacementGate
    {
        // Kept intentionally short: place the first cat for real, then the second, then
        // hand the rest of the level back to the player.
        private enum TutorialStep { Off, PlaceFirst, PlaceSecond }
        private const int TutorialProgressStepCount = 2;
        // On the second cat the player is on their own; the hand only comes back after this long without a move.
        private const float SecondStepHandDelay = 4f;
        private static readonly Color CatTagColor = new Color(.42f, .22f, .1f, 1f);
        private static readonly Color TargetTagColor = new Color(.24f, .58f, .2f, 1f);
        private static readonly Color TipTagColor = new Color(.2f, .42f, .7f, 1f);
        // After the first cat lands, this long is spent telling the player a placed cat can go back to the shelf.
        private const float ShelfTipSeconds = 4f;

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
        private RectTransform tutorialCatTag;
        private RectTransform tutorialTargetTag;
        private Text tutorialCatTagText;
        private Text tutorialTargetTagText;
        private RectTransform tutorialTipTag;
        private Text tutorialTipTagText;
        private readonly List<Vector2Int> tutorialTargetCoordinates = new List<Vector2Int>();
        private Image tutorialHalo;
        private readonly List<Image> tutorialTargets = new List<Image>();
        private readonly List<Image> tutorialDots = new List<Image>();
        private PieceState tutorialPiece;
        private float tutorialElapsed;
        private float tutorialCelebrationRemaining;
        private bool tutorialPendingFinish;
        private Vector2 tutorialCelebrationPosition;

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
            tutorialCatTag = CreateTag("Drag Me Tag", CatTagColor, out tutorialCatTagText);
            tutorialTargetTag = CreateTag("Drop Here Tag", TargetTagColor, out tutorialTargetTagText);
            tutorialTipTag = CreateTag("Shelf Tip Tag", TipTagColor, out tutorialTipTagText);
            tutorialTipTagText.fontSize = 32;
            GameAnalytics.TutorialStarted();
            AvnEvents.TutorialShown(AvnEvents.TutorialFirstLevel, (int)TutorialStep.PlaceFirst);
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
            GameAnalytics.TutorialStep((int)step);
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
        }

        public bool TutorialAllowsPickup(PieceState piece) => !IsOpen ||
            (tutorialCelebrationRemaining <= 0f && (piece == tutorialPiece || CanMoveBack(piece)));
        // On the second cat the first one may go back to the shelf, which is what the tip tells the player.
        public bool TutorialAllowsShelfReturn(PieceState piece) => !IsOpen || CanMoveBack(piece);
        public bool TutorialAllowsPlacement(PieceState piece, int row, int col) => !IsOpen ||
            ((piece == tutorialPiece || CanMoveBack(piece)) && row == piece.Definition.SolutionRow && col == piece.Definition.SolutionCol);

        private bool CanMoveBack(PieceState piece) => tutorialStep == TutorialStep.PlaceSecond && piece != tutorialPiece && pieces.Count > 0 && piece == pieces[0];

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
            GameAnalytics.TutorialCompleted();
            AvnEvents.TutorialCompleted(AvnEvents.TutorialFirstLevel, TutorialProgressStepCount);
            tutorialStep = TutorialStep.Off; // Finished, so the Cancel below does not count as an abort.
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

        // The same grip a real drag uses (the cat held by its middle), so the hand's path matches what the player's drag does.
        private CellGrab TutorialGrip(PieceState piece, Vector2 screen) => drag.CentralGrab(piece);

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

        // The middle of the cells the current cat has to fill.
        private Vector2 TutorialTargetCenter(int fallbackRow, int fallbackCol)
        {
            if (tutorialTargetCoordinates.Count == 0) return TutorialCell(fallbackRow, fallbackCol);
            Vector2 sum = Vector2.zero;
            foreach (Vector2Int coordinate in tutorialTargetCoordinates) sum += TutorialCell(coordinate.x, coordinate.y);
            return sum / tutorialTargetCoordinates.Count;
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
            if (hidden || session.inputLocked) return;
            if (tutorialCelebrationRemaining > 0f) tutorialCelebrationRemaining = Mathf.Max(0f, tutorialCelebrationRemaining - Time.unscaledDeltaTime);
            else tutorialElapsed += Time.unscaledDeltaTime;
            if (tutorialPendingFinish && tutorialCelebrationRemaining <= 0f)
            {
                Finish();
                return;
            }
            Vector2 startScreen = TutorialPickupScreen(tutorialPiece);
            Vector2 start = TutorialScreenLocal(startScreen);
            int destinationRow = tutorialPiece.Definition.SolutionRow;
            int destinationCol = tutorialPiece.Definition.SolutionCol;
            // The hand ends on the green spaces themselves, so it points at the right place on the grid.
            Vector2 destination = TutorialTargetCenter(destinationRow, destinationCol);
            Vector2 destinationScreen = space.WorldToScreen(tutorialRoot.TransformPoint(destination));

            bool dragging = session.drag != null;
            // The first cat is shown by the hand; on the second the player tries alone and gets the hand back only if stuck.
            bool handOn = tutorialStep == TutorialStep.PlaceFirst || tutorialElapsed > SecondStepHandDelay;
            float wave = reducedMotion ? .5f : .5f + .5f * Mathf.Sin(tutorialElapsed * 4f);
            tutorialHalo.rectTransform.anchoredPosition = start;
            tutorialHalo.rectTransform.sizeDelta = new Vector2(104f, 104f) * (1f + .12f * wave);
            tutorialHalo.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .2f + .15f * wave);
            tutorialHalo.gameObject.SetActive(!dragging && handOn && tutorialCelebrationRemaining <= 0f);
            for (int i = 0; i < tutorialDots.Count; i++)
            {
                var dot = tutorialDots[i];
                dot.gameObject.SetActive(!dragging && handOn && tutorialCelebrationRemaining <= 0f);
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
            tutorialHand.gameObject.SetActive(!dragging && handOn && tutorialCelebrationRemaining <= 0f);
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
            UpdateTags(start, destinationRow, destinationCol, dragging);
            tutorialCatTag.SetAsLastSibling();
            tutorialTargetTag.SetAsLastSibling();
            tutorialTipTag.SetAsLastSibling();
        }

        public void Cancel()
        {
            if (IsOpen)
            {
                GameAnalytics.TutorialAborted((int)tutorialStep);
                AvnEvents.TutorialInterrupted(AvnEvents.TutorialFirstLevel, (int)tutorialStep);
            }
            tutorialStep = TutorialStep.Off;
            tutorialPiece = null;
            tutorialPendingFinish = false;
            tutorialTargets.Clear();
            tutorialDots.Clear();
            tutorialTargetCoordinates.Clear();
            tutorialCelebrationRemaining = 0f;
            if (tutorialRoot != null) { tutorialRoot.gameObject.SetActive(false); Destroy(tutorialRoot.gameObject); }
            if (tutorialTargetRoot != null) { tutorialTargetRoot.gameObject.SetActive(false); Destroy(tutorialTargetRoot.gameObject); }
            tutorialRoot = null;
            tutorialTargetRoot = null;
        }

        // A small solid pill with a white edge and a pointer notch: readable on any background and no bigger than its words.
        private RectTransform CreateTag(string name, Color color, out Text label)
        {
            RectTransform tag = RuntimeUiFactory.CreateRect(tutorialRoot, name);
            RuntimeUiFactory.SetRect(tag, Vector2.zero, new Vector2(240f, 70f));
            var notchEdge = RuntimeUiFactory.CreateImage(tag, "Notch Edge", roundedBoxSprite, Color.white, new Vector2(0f, -38f), new Vector2(30f, 30f));
            notchEdge.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            RuntimeUiFactory.CreateImage(tag, "Edge", roundedBoxSprite, Color.white, Vector2.zero, new Vector2(240f, 70f));
            var notch = RuntimeUiFactory.CreateImage(tag, "Notch", roundedBoxSprite, color, new Vector2(0f, -36f), new Vector2(22f, 22f));
            notch.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            RuntimeUiFactory.CreateImage(tag, "Fill", roundedBoxSprite, color, Vector2.zero, new Vector2(228f, 58f));
            label = RuntimeUiFactory.CreateText(tag, "Label", "", 38, TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = Color.white;
            label.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .35f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            RuntimeUiFactory.SetRect(label.rectTransform, Vector2.zero, new Vector2(228f, 58f));
            return tag;
        }

        // Sizes the pill to its words and puts the notch's tip on <paramref name="tip"/>.
        private static void PlaceTag(RectTransform tag, Text label, string text, Vector2 tip)
        {
            label.text = text;
            float width = Mathf.Max(150f, label.preferredWidth + 56f);
            RuntimeUiFactory.SetRect(tag.Find("Edge") as RectTransform, Vector2.zero, new Vector2(width, 70f));
            RuntimeUiFactory.SetRect(tag.Find("Fill") as RectTransform, Vector2.zero, new Vector2(width - 12f, 58f));
            RuntimeUiFactory.SetRect(label.rectTransform, Vector2.zero, new Vector2(width - 12f, 58f));
            tag.sizeDelta = new Vector2(width, 70f);
            // The pill sits above the tip; its notch hangs 50 below the pill's centre.
            tag.anchoredPosition = tip + new Vector2(0f, 50f);
        }

        // "Drag me" floats over the cat and "Drop here" over the green spaces; both hide while a cat lands.
        private void UpdateTags(Vector2 catPoint, int destinationRow, int destinationCol, bool dragging)
        {
            bool show = showTutorialCaptions;
            bool celebrating = tutorialCelebrationRemaining > 0f;
            bool tipOn = show && tutorialStep == TutorialStep.PlaceSecond && !dragging && !celebrating && tutorialElapsed < ShelfTipSeconds;
            tutorialTipTag.gameObject.SetActive(tipOn);
            float bob = reducedMotion ? 0f : Mathf.Sin(Time.unscaledTime * 3.2f) * 6f;
            tutorialCatTag.gameObject.SetActive(show && !dragging && !celebrating && !tipOn);
            tutorialTargetTag.gameObject.SetActive(show);
            if (!show) return;

            if (tipOn)
            {
                // The notch points down at the shelf, the container the cat can go back to.
                var shelfCorners = new Vector3[4];
                trayRoot.GetWorldCorners(shelfCorners);
                Vector2 shelfTop = TutorialLocal((shelfCorners[1] + shelfCorners[2]) * .5f);
                PlaceTag(tutorialTipTag, tutorialTipTagText, "TIP: DRAG A CAT BACK TO THE SHELF", shelfTop + new Vector2(0f, 8f));
            }

            if (tutorialCatTag.gameObject.activeSelf)
            {
                string catText = tutorialStep == TutorialStep.PlaceFirst ? "DRAG ME" : "YOUR TURN";
                PlaceTag(tutorialCatTag, tutorialCatTagText, catText, catPoint + new Vector2(0f, 42f + bob));
            }

            if (celebrating)
            {
                PlaceTag(tutorialTargetTag, tutorialTargetTagText, "PURRFECT!", tutorialCelebrationPosition + new Vector2(0f, boardCellHeight * .85f));
                return;
            }

            // The tip sits just above the topmost green space.
            Vector2 top = TutorialCell(destinationRow, destinationCol);
            foreach (Vector2Int coordinate in tutorialTargetCoordinates)
            {
                Vector2 cell = TutorialCell(coordinate.x, coordinate.y);
                if (cell.y > top.y) top = cell;
            }
            bool fits = dragging && session.drag != null && session.drag.Valid;
            SetTagColor(tutorialTargetTag, TargetTagColor);
            PlaceTag(tutorialTargetTag, tutorialTargetTagText, fits ? "LET GO!" : "DROP HERE", top + new Vector2(0f, boardCellHeight * .55f + bob));
        }

        private static void SetTagColor(RectTransform tag, Color color)
        {
            foreach (string part in new[] { "Notch", "Fill" })
                if (tag.Find(part) != null) tag.Find(part).GetComponent<Image>().color = color;
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
