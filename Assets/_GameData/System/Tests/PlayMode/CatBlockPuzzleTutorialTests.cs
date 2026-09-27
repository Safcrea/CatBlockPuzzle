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

        private void Place(object piece)
        {
            object definition = Member(piece, "Definition");
            int row = (int)Member(definition, "SolutionRow");
            int col = (int)Member(definition, "SolutionCol");
            Call("PlacePiece", piece, row, col, false);
        }

        [UnityTest]
        public IEnumerator GuidedHandDestination_ProducesARealValidPlacement()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(Step, Is.EqualTo("PlaceFirst"), "The shortened tutorial should ask for a real placement immediately.");
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
            Call("EndPieceDrag", piece, pointer);
            Assert.That(Member(piece, "Placed"), Is.EqualTo(true), "Following the animated hand must produce a valid real drop.");
            Assert.That(Member(piece, "Row"), Is.EqualTo(row));
            Assert.That(Member(piece, "Col"), Is.EqualTo(col));
            Assert.That(Step, Is.EqualTo("PlaceSecond"));
        }

        [UnityTest]
        public IEnumerator OptionalContext_ExplainsFirstPlacementAndTargetsMatchBoardCells()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Game.GetType().GetField("showTutorialCaptions", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Game, true);
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
            Vector2 expected = (Vector2)Call("TutorialCell", coordinate.x, coordinate.y);
            Assert.That(Vector2.Distance(firstTarget.rectTransform.anchoredPosition, expected), Is.LessThan(.1f),
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
        public IEnumerator SkipButton_EndsTutorialAndRemembersDismissalOnNormalAttempts()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("StartSelectedLevel", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.True);
            var skip = (Button)Field("tutorialSkipButton");
            Assert.That(skip.GetComponent<CanvasGroup>().ignoreParentGroups, Is.True);
            Assert.That(skip.interactable, Is.True);
            skip.onClick.Invoke();
            Assert.That(IsOpen, Is.False);
            Assert.That(Field("inputLocked"), Is.EqualTo(false));
            Assert.That(Field("timerRunning"), Is.EqualTo(true));
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(SeenKey, 0), Is.EqualTo(1));
            Call("RestartCurrentLevel");
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.False, "Restarting must not show a tutorial the player skipped.");
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
            Assert.That((bool)Call("TutorialAllowsPlacement", first, 0, 1), Is.False, "Off-target placements can strand onboarding.");
            Place(first);
            Assert.That(Step, Is.EqualTo("PlaceSecond"));
            Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(time).Within(.001f), "Onboarding must not spend the timer.");
            Assert.That((bool)Call("TutorialAllowsPickup", second), Is.True, "The second step lets the player pick up the second group for real.");
            Assert.That((bool)Call("TutorialAllowsPlacement", second, 0, 1), Is.False, "Off-target placements can strand onboarding.");
            Place(second);
            // This level's two pieces fill the whole board, so the second placement also wins:
            // onboarding must finish immediately rather than linger open under the result screen.
            Assert.That(IsOpen, Is.False, "Onboarding ends as soon as the second real placement lands.");
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
            Call("PreviewLevelForTesting", 1);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(IsOpen, Is.False);
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
