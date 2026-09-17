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

        private void FinishDemo(float seconds)
        {
            Game.GetType().GetField("tutorialElapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Game, seconds);
            Call("UpdateLevelOneTutorial");
        }

        [UnityTest]
        public IEnumerator GuidedHandDestination_ProducesARealValidPlacement()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            FinishDemo(1.81f);
            Assert.That(Step, Is.EqualTo("FitDemo"));
            FinishDemo(3.31f);
            Assert.That(Step, Is.EqualTo("EdgeDemo"));
            FinishDemo(3.61f);
            Assert.That(Step, Is.EqualTo("PlaceFirst"));
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
            Assert.That(Step, Is.EqualTo("ReturnFirst"));
        }

        [UnityTest]
        public IEnumerator OptionalContext_ExplainsGoalAndGhostFitsExactBoardCells()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Game.GetType().GetField("showTutorialCaptions", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Game, true);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            var title = (Text)Field("tutorialCoachTitle");
            Assert.That(title.gameObject.activeInHierarchy, Is.True);
            Assert.That(title.text, Does.Contain("every cat"));
            Assert.That(((Text)Field("tutorialCoachBody")).text, Does.Contain("timer is paused"));
            FinishDemo(1.81f);
            Game.GetType().GetField("tutorialElapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Game, 1.8f);
            Call("UpdateLevelOneTutorial");
            var ghost = (RectTransform)Field("tutorialGhost");
            var first = ghost.GetChild(0) as RectTransform;
            Vector2 actual = (Vector2)Call("TutorialLocal", first.TransformPoint(first.rect.center));
            var definition = Member(Pieces[0], "Definition");
            Vector2 expected = (Vector2)Call("TutorialCell", (int)Member(definition, "SolutionRow"), (int)Member(definition, "SolutionCol"));
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(.1f), "Demonstration cats must align with their logical board cells.");
            Assert.That((ICollection)Field("occupancy"), Is.Empty, "The successful fit demonstration must remain presentation only.");
            Assert.That(((RectTransform)Field("tutorialCheck")).gameObject.activeSelf, Is.True);
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
        public IEnumerator FullTutorial_PracticesPlacementReturnAndOverlapWithoutSpendingTime()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            Call("PreviewLevelForTesting", 0);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(Step, Is.EqualTo("GoalDemo"));
            var root = (RectTransform)Field("tutorialRoot");
            Assert.That(root.GetComponentsInChildren<Text>().Length, Is.EqualTo(1), "Visual-only onboarding should show only the skip label.");
            Assert.That(root.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            float time = (float)Field("levelRemainingSeconds");
            Assert.That((ICollection)Field("occupancy"), Is.Empty);
            object first = Pieces[0];
            object second = Pieces[1];
            Assert.That((bool)Call("TutorialAllowsPickup", first), Is.False, "Demo must not move real pieces.");
            yield return new WaitForSecondsRealtime(8.9f);
            Assert.That(Step, Is.EqualTo("PlaceFirst"));
            Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(time).Within(.001f));
            Assert.That((ICollection)Field("occupancy"), Is.Empty, "Demo modified real occupancy.");
            Assert.That((bool)Call("TutorialAllowsPickup", second), Is.False);
            CaptureAssignedCamera((Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game), "level-one-tutorial-placement.png");
            Assert.That((bool)Call("TutorialAllowsPlacement", first, 0, 1), Is.False, "Off-target placements can strand onboarding.");
            Place(first);
            Assert.That(Step, Is.EqualTo("ReturnFirst"));
            Assert.That((bool)Call("TutorialAllowsPlacement", first, 0, 0), Is.False);
            yield return new WaitForSecondsRealtime(.8f);

            var rect = (RectTransform)Member(first, "Rect");
            var canvas = (Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position) };
            Assert.That((bool)Call("BeginPieceDrag", first, pointer), Is.True);
            var tray = (RectTransform)Field("trayRoot");
            pointer.position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, tray.position);
            Call("DragPiece", first, pointer);
            Call("EndPieceDrag", first, pointer);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(Step, Is.EqualTo("ReplaceFirst"));
            Assert.That((bool)Member(first, "Placed"), Is.False);
            Assert.That((ICollection)Field("occupancy"), Is.Empty);
            Place(first);
            Assert.That(Step, Is.EqualTo("OverlapDemo"));
            yield return new WaitForSecondsRealtime(4.5f);
            Assert.That(Step, Is.EqualTo("FillBoard"));
            Assert.That((bool)Call("CanPlace", second, 0, 0), Is.False, "Overlapping cats must stay invalid.");
            Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(time).Within(.001f));
            Place(second);
            Assert.That(IsOpen, Is.False);
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
    }
}
