using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzleTutorialTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";
        private const string SeenKey = "CatBlockPuzzle.Tutorial.LevelOne.v1";
        private string Step => Field("tutorialStep").ToString();
        private IList Pieces => (IList)Field("pieces");
        private object Member(object piece, string name) => piece.GetType().GetField(name).GetValue(piece);
        private bool IsOpen => (bool)Game.GetType().GetProperty("IsLevelOneTutorialOpen").GetValue(Game);
        private MonoBehaviour System => (MonoBehaviour)Game.GetType().Assembly.GetType("CatBlockPuzzle.GameSystem")
            .GetProperty("Instance").GetValue(null);

        [UnitySetUp]
        public IEnumerator ShowTutorialGameplay()
        {
            // PreviewLevelForTesting loads a puzzle but intentionally does not navigate away from Home.
            var system = System;
            system.GetType().GetMethod("ShowGameplayPage").Invoke(system, null);
            yield return null;
        }

        private void Place(object piece)
        {
            object definition = Member(piece, "Definition");
            int row = (int)Member(definition, "SolutionRow");
            int col = (int)Member(definition, "SolutionCol");
            Call("PlacePiece", piece, row, col, false);
        }

        private void AssertRejectsWrongTutorialTarget(object piece)
        {
            object definition = Member(piece, "Definition");
            int row = (int)Member(definition, "SolutionRow");
            int col = (int)Member(definition, "SolutionCol");
            Assert.That((bool)Call("TutorialAllowsPlacement", piece, row + 1, col), Is.False,
                "Off-target placements can strand onboarding.");
        }

        [UnityTest]
        public IEnumerator GuidedHandDestination_ProducesARealValidPlacement()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(Step, Is.EqualTo("PlaceFirst"), "The shortened tutorial should ask for a real placement immediately.");
            var hand = ((RectTransform)Field("tutorialHand")).GetComponentInChildren<Image>();
            Assert.That(hand.sprite, Is.EqualTo(Field("tutorialHandSprite")));
            Assert.That(hand.sprite.name, Is.EqualTo("Handpointer"), "GameScene must use the supplied hand artwork.");
            Assert.That(hand.raycastTarget, Is.False);
            object piece = Pieces[0];
            var definition = Member(piece, "Definition");
            int row = (int)Member(definition, "SolutionRow");
            int col = (int)Member(definition, "SolutionCol");
            Vector2 pickup = (Vector2)Call("TutorialPickupScreen", piece);
            Vector2 destination = (Vector2)Call("TutorialPointerDestination", piece, row, col);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = -1, position = pickup };
            Assert.That(Call("BeginPieceDrag", piece, pointer), Is.EqualTo(true));
            pointer.position = destination;
            Call("DragPiece", piece, pointer);
            yield return new WaitForSecondsRealtime(.35f);
            var canvas = (Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
            CaptureAssignedCamera(canvas, "tutorial-cat-over-target.png");
            var target = (Image)((IList)Field("tutorialTargets"))[0];
            var cat = ((RectTransform)Member(piece, "Rect")).GetComponentInChildren<Image>();
            Assert.That(target.depth, Is.GreaterThanOrEqualTo(0));
            Assert.That(cat.depth, Is.GreaterThan(target.depth), "Green targets must render behind the dragged cat.");
            Call("EndPieceDrag", piece, pointer);
            Assert.That(Member(piece, "Placed"), Is.EqualTo(true), "Following the animated hand must produce a valid real drop.");
            Assert.That(Member(piece, "Row"), Is.EqualTo(row));
            Assert.That(Member(piece, "Col"), Is.EqualTo(col));
            Assert.That(Step, Is.EqualTo("PlaceSecond"));
            yield return new WaitForSecondsRealtime(1f);
            CaptureAssignedCamera(canvas, "tutorial-cat-placed.png");
            target = (Image)((IList)Field("tutorialTargets"))[0];
            Assert.That(target.gameObject.activeInHierarchy, Is.True);
            Assert.That(cat.depth, Is.GreaterThan(target.depth), "Green targets must also render behind settled cats.");
        }

        [UnityTest]
        public IEnumerator OptionalContext_ExplainsFirstPlacementAndTargetsMatchBoardCells()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            SetField("showTutorialCaptions", true);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(Step, Is.EqualTo("PlaceFirst"));
            var title = (Text)Field("tutorialCoachTitle");
            Assert.That(title.gameObject.activeInHierarchy, Is.True);
            Assert.That(title.text, Does.Contain("cat"));
            Assert.That(((Text)Field("tutorialCoachBody")).text, Does.Contain("glowing"));
            var targets = (IList)Field("tutorialTargets");
            var coordinates = (IList)Field("tutorialTargetCoordinates");
            Assert.That(targets.Count, Is.GreaterThan(0));
            var firstTarget = (Image)targets[0];
            Vector2Int coordinate = (Vector2Int)coordinates[0];
            var root = (RectTransform)Field("tutorialRoot");
            Vector3 expected = root.TransformPoint((Vector2)Call("TutorialCell", coordinate.x, coordinate.y));
            Assert.That(Vector3.Distance(firstTarget.rectTransform.position, expected), Is.LessThan(.1f),
                "Placement targets must align with their logical board cells.");
            Assert.That((ICollection)Field("occupancy"), Is.Empty, "The first step must not move real pieces before the player drags them.");
            CaptureAssignedCamera((Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game), "level-one-tutorial-context.png");
        }

        [UnityTest]
        public IEnumerator DisabledTutorial_LeavesNormalLevelOnePlayable()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("SetLevelOneTutorialEnabled", false);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.False);
            Assert.That(Field("inputLocked"), Is.EqualTo(false));
            Assert.That(Field("timerRunning"), Is.EqualTo(true));
            Assert.That((bool)Call("TutorialAllowsPickup", Pieces[1]), Is.True);
            Assert.That(PlayerPrefs.GetInt(SeenKey, 0), Is.Zero, "Disabling should not erase the chance to replay onboarding.");
        }

        [UnityTest]
        public IEnumerator Tutorial_HasNoSkipButton()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("StartSelectedLevel", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.True);
            Assert.That(Game.GetType().GetField("tutorialSkipButton", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        }

        [UnityTest]
        public IEnumerator FullTutorial_PracticesTwoRealPlacementsWithoutSpendingTime()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(Step, Is.EqualTo("PlaceFirst"));
            var root = (RectTransform)Field("tutorialRoot");
            Assert.That(root.GetComponentsInChildren<Text>().Length, Is.EqualTo(1), "Visual-only onboarding should show only the skip label.");
            Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            float time = (float)Field("levelRemainingSeconds");
            Assert.That((ICollection)Field("occupancy"), Is.Empty);
            object first = Pieces[0];
            object second = Pieces[1];
            Assert.That((bool)Call("TutorialAllowsPickup", second), Is.False, "Only the active piece can be picked up.");
            CaptureAssignedCamera((Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game), "level-one-tutorial-placement.png");
            AssertRejectsWrongTutorialTarget(first);
            Place(first);
            Assert.That(Step, Is.EqualTo("PlaceSecond"));
            Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(time).Within(.001f), "Onboarding must not spend the timer.");
            Assert.That((bool)Call("TutorialAllowsPickup", second), Is.False, "The success celebration briefly locks pickup.");
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That((bool)Call("TutorialAllowsPickup", second), Is.True, "The second step lets the player pick up the second group for real.");
            AssertRejectsWrongTutorialTarget(second);
            Place(second);
            // This level's two pieces fill the whole board, so the second placement also wins:
            // onboarding must finish immediately rather than linger open under the result screen.
            Assert.That(IsOpen, Is.False, "Onboarding ends as soon as the second real placement lands.");
            Assert.That((RectTransform)Field("tutorialTargetRoot"), Is.Null);
            Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(time).Within(.001f), "No time should have been spent while onboarding was open.");
            yield return new WaitForSecondsRealtime(1f);
            Assert.That((bool)Field("inputLocked"), Is.True, "Completion should open normal results.");
            Assert.That(PlayerPrefs.GetInt(SeenKey, 0), Is.Zero, "Developer previews must not change onboarding saves.");
        }

        [UnityTest]
        public IEnumerator NavigationAndPause_DoNotLeaveTutorialCuesOrAdvanceDemo()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            float elapsed = (float)Field("tutorialElapsed");
            Call("SetSystemPaused", true);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That((float)Field("tutorialElapsed"), Is.EqualTo(elapsed).Within(.001f));
            Call("SetSystemPaused", false);
            Call("SuspendForNavigation");
            Assert.That(IsOpen, Is.False);
            yield return null;
            Assert.That((RectTransform)Field("tutorialRoot"), Is.Null);
            Assert.That((RectTransform)Field("tutorialTargetRoot"), Is.Null);
            Call("PreviewLevelForTesting", 1);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator ReducedMotion_PauseAndReplayKeepGuidesVisibleOnlyDuringGameplay()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            SetField("reducedMotion", true);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            var hand = (RectTransform)Field("tutorialHand");
            var targets = (RectTransform)Field("tutorialTargetRoot");
            Vector3 handPosition = hand.position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector3.Distance(hand.position, handPosition), Is.LessThan(.01f));
            Assert.That(hand.localScale, Is.EqualTo(Vector3.one));
            Assert.That(targets.gameObject.activeInHierarchy, Is.True);
            CaptureAssignedCamera((Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game), "tutorial-reduced-motion.png");
            var system = System;
            system.GetType().GetMethod("OpenPause").Invoke(system, null);
            yield return null;
            Assert.That(targets.gameObject.activeInHierarchy, Is.False);
            Assert.That(hand.gameObject.activeInHierarchy, Is.False);
            system.GetType().GetMethod("ResumeGame").Invoke(system, null);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(targets.gameObject.activeInHierarchy, Is.True);
            Call("SetLevelOneTutorialEnabled", false);
            yield return null;
            Assert.That(targets == null, Is.True);
            Call("SetLevelOneTutorialEnabled", true);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.True);
            Assert.That(((RectTransform)Field("tutorialTargetRoot")).gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator MisplacedPiece_ShowsMovePieceTipOnce()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.DeleteKey("CatBlockPuzzle.Tutorial.MovePiece.v1");
            Call("SetLevelOneTutorialEnabled", false);
            Call("PreviewLevelForTesting", 1);
            yield return new WaitForSecondsRealtime(.85f);
            object piece = Pieces[0];
            object definition = Member(piece, "Definition");
            int row = (int)Member(definition, "SolutionRow");
            int col = (int)Member(definition, "SolutionCol");
            int wrongRow = row + 100;
            int wrongCol = col + 100;
            // Force an out-of-solution but structurally valid placement to simulate a "wrong spot" drop.
            definition.GetType().GetField("SolutionRow").SetValue(definition, wrongRow);
            definition.GetType().GetField("SolutionCol").SetValue(definition, wrongCol);
            Call("PlacePiece", piece, row, col, false);
            Assert.That(Field("movePieceTipBubble"), Is.Not.Null, "First-ever wrong placement should surface the move tip.");
            Assert.That(PlayerPrefs.GetInt("CatBlockPuzzle.Tutorial.MovePiece.v1", 0), Is.Zero,
                "Developer previews must not burn the one-time flag.");
            Call("StopHint");
            Assert.That(Field("movePieceTipBubble"), Is.Null, "Clearing the hint must also clear the tip bubble.");
        }
    }
}
