using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class LevelPlanV2PlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static object Get(object target, string member)
        {
            var field = target.GetType().GetField(member, Flags);
            if (field != null) return field.GetValue(target);
            return target.GetType().GetProperty(member, Flags).GetValue(target);
        }

        private Component SystemScreen => Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(g => g.GetType().Name == "GameSystem");

        private IEnumerator Open(int zeroBasedLevel)
        {
            SystemScreen.GetType().GetMethod("ShowGameplayPage").Invoke(SystemScreen, null);
            Call("PreviewLevelForTesting", zeroBasedLevel);
            yield return new WaitForSecondsRealtime(1.4f);
        }

        private object[] Pieces => ((IList)Field("pieces")).Cast<object>().ToArray();
        private static object Definition(object piece) => Get(piece, "Definition");
        private static bool IsFixed(object piece) => (bool)Get(Definition(piece), "IsFixed");

        // Lands a cat on its own solution spot through the real drop path.
        private void PlaceAtSolution(object piece)
        {
            object definition = Definition(piece);
            Call("PlacePiece", piece, (int)Get(definition, "SolutionRow"), (int)Get(definition, "SolutionCol"), true);
        }

        [UnityTest]
        public IEnumerator FixedCat_StartsOnItsSpotOffTheShelfAndStaysPut()
        {
            yield return Open(50);
            object napper = Pieces.Single(IsFixed);
            Assert.That(Get(napper, "Placed"), Is.EqualTo(true));
            Assert.That(Get(napper, "Row"), Is.EqualTo(Get(Definition(napper), "SolutionRow")));
            Assert.That(((RectTransform)Get(napper, "Slot")).gameObject.activeSelf, Is.False, "A fixed cat never appears on the shelf.");
            var rect = (RectTransform)Get(napper, "Rect");
            Assert.That(rect.GetComponentsInChildren<Image>(true).Any(i => i.name == "Settled Badge" && i.gameObject.activeInHierarchy), Is.True);

            var canvas = (Canvas)Game.GetType().GetProperty("PreparedCanvas").GetValue(Game);
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { pointerId = 7, position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position) };
            Assert.That(Call("BeginPieceDrag", napper, pointer), Is.EqualTo(false), "A fixed cat cannot be picked up.");
            Assert.That(Call("ShowHint", Enum("HintReveal", "FullShape")), Is.EqualTo(true));
            Assert.That(Field("drops"), Is.EqualTo(0), "The fixed cat is not a drop.");
        }

        [UnityTest]
        public IEnumerator Fog_ClearsOnceACatLandsBesideIt()
        {
            yield return Open(60);
            object fog = Field("fog");
            int before = (int)Get(fog, "CloudCount");
            Assert.That(before, Is.EqualTo(9));
            var occupancy = (IDictionary)Field("occupancy");
            Assert.That(occupancy.Count, Is.Zero);
            foreach (object piece in Pieces.Where(p => !(bool)Get(Definition(p), "IsSpare")))
            {
                PlaceAtSolution(piece);
                if ((int)Get(fog, "CloudCount") < before) break;
            }
            Assert.That((int)Get(fog, "CloudCount"), Is.LessThan(before));
            foreach (object piece in Pieces.Where(p => !(bool)Get(p, "Placed"))) PlaceAtSolution(piece);
            Assert.That((int)Get(fog, "CloudCount"), Is.Zero, "A full board leaves no cloud.");
        }

        [UnityTest]
        public IEnumerator Drops_CountEveryLandingAndShowWhereTheStarsWere()
        {
            yield return Open(70);
            var hud = (Component)Field("gameplayHud");
            var stars = (RectTransform)hud.GetType().GetField("starPanel", Flags).GetValue(hud);
            Assert.That(stars.gameObject.activeSelf, Is.False);
            var label = stars.parent.Find("Drops Counter").GetComponent<Text>();
            Assert.That(label.text, Is.EqualTo("Drops 0/10"));
            PlaceAtSolution(Pieces[0]);
            PlaceAtSolution(Pieces[1]);
            Assert.That(Field("drops"), Is.EqualTo(2));
            Assert.That(label.text, Is.EqualTo("Drops 2/10"));
            Assert.That(Game.GetType().GetProperty("CurrentStarCount").GetValue(Game), Is.EqualTo(3));
            SetField("drops", 14);
            Assert.That(Game.GetType().GetProperty("CurrentStarCount").GetValue(Game), Is.EqualTo(1));

            yield return Open(10);
            Assert.That(stars.gameObject.activeSelf, Is.True, "Timer stars come back on a normal level.");
            Assert.That(label.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstTenLevels_HideTheClockAndFreezeAndNeverCountDown()
        {
            var hud = (Component)Field("gameplayHud");
            var timer = (Text)hud.GetType().GetField("timerText", Flags).GetValue(hud);
            var freeze = (Button)hud.GetType().GetField("freezeButton", Flags).GetValue(hud);
            yield return Open(9);
            Assert.That(timer.gameObject.activeInHierarchy, Is.False);
            Assert.That(freeze.gameObject.activeSelf, Is.False);
            Assert.That(Field("timerRunning"), Is.EqualTo(true), "The run is live so Hint still works.");
            float remaining = (float)Field("levelRemainingSeconds");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(remaining));
            Assert.That(Game.GetType().GetProperty("CurrentStarCount").GetValue(Game), Is.EqualTo(3));

            yield return Open(10);
            Assert.That(timer.gameObject.activeInHierarchy, Is.True);
            Assert.That(freeze.gameObject.activeSelf, Is.True);
            remaining = (float)Field("levelRemainingSeconds");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That((float)Field("levelRemainingSeconds"), Is.LessThan(remaining));
        }

        [UnityTest]
        public IEnumerator ThreeFails_StartTheNextAttemptWithAFreeHint()
        {
            const string key = "CatBlockPuzzle.FailStreak.12";
            try
            {
                yield return Open(11);
                for (int i = 0; i < 3; i++)
                {
                    Call("FailLevel");
                    Invoke("RestartLevel");
                    yield return new WaitForSecondsRealtime(1.4f);
                    if (i < 2) Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(i + 1));
                }

                int hints = (int)SystemScreen.GetType().GetMethod("GetPowerUpCount").Invoke(SystemScreen, new[] { Enum("PowerUpKind", "Hint") });
                object toast = Field("toast");
                Assert.That(Get(toast, "IsShowing"), Is.EqualTo(true));
                Assert.That(Get(toast, "Message"), Is.EqualTo("Here's a free hint!"));
                Assert.That(PlayerPrefs.HasKey(key), Is.False, "Using the free hint resets the count.");
                Assert.That(SystemScreen.GetType().GetMethod("GetPowerUpCount").Invoke(SystemScreen, new[] { Enum("PowerUpKind", "Hint") }), Is.EqualTo(hints));
                Assert.That(Field("freeHintPending"), Is.EqualTo(false));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        private void Invoke(string method) => SystemScreen.GetType().GetMethod(method).Invoke(SystemScreen, null);

        private object Enum(string type, string value) =>
            System.Enum.Parse(Game.GetType().Assembly.GetType("CatBlockPuzzle." + type), value);
    }
}
