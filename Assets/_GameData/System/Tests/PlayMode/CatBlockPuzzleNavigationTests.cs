using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class CatBlockPuzzleNavigationTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";
        private Component Screen(string name) => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
            FindObjectsSortMode.None).Single(g => g.GetType().Name == name);
        private object Invoke(object target, string name, params object[] args) => target.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(target, args);

        [UnityTest]
        public IEnumerator LaterLevels_StayCenteredInsideAuthoredBoard()
        {
            var hud = (Component)Field("gameplayHud");
            var area = (RectTransform)hud.GetType().GetField("boardArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            foreach (int level in new[] { 0, 9, 19, 49, 99 })
            {
                Call("PreviewLevelForTesting", level);
                yield return new WaitForSecondsRealtime(.85f);
                var board = (RectTransform)Field("boardRoot");
                var corners = new Vector3[4];
                board.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector3 point = area.InverseTransformPoint(corner);
                    Assert.That(point.x, Is.InRange(area.rect.xMin + 35.9f, area.rect.xMax - 35.9f));
                    Assert.That(point.y, Is.InRange(area.rect.yMin + 35.9f, area.rect.yMax - 35.9f));
                }
                Vector2 center = area.InverseTransformPoint(board.TransformPoint(board.rect.center));
                Assert.That(Vector2.Distance(center, area.rect.center), Is.LessThan(.01f));
                Assert.That((float)Field("boardCellWidth"), Is.EqualTo((float)Field("boardCellHeight")).Within(.001f));
            }
        }

        [UnityTest]
        public IEnumerator FailSkip_AdvancesOneLevelAndSavesAccessWithoutCoinsOrStars()
        {
            var system = Screen("GameSystem");
            Assert.That(Invoke(system, "StartLevel", 0), Is.EqualTo(true));
            yield return new WaitForSecondsRealtime(.85f);
            int coins = (int)Game.GetType().GetProperty("CurrentCoins").GetValue(Game);
            Call("FailLevel");
            var fail = Screen("LevelFailScreen");
            Invoke(fail, "EnsureBindings");
            Invoke(fail, "EnsureBindings");
            var skip = (Button)fail.GetType().GetField("skipButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fail);
            Assert.That(skip, Is.Not.Null);
            skip.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(2));
            Assert.That(Invoke(Game, "IsLevelAvailable", 1), Is.EqualTo(true));
            Assert.That(Invoke(Game, "GetLevelStars", 0), Is.EqualTo(0));
            Assert.That(Game.GetType().GetProperty("CurrentCoins").GetValue(Game), Is.EqualTo(coins));
            Assert.That(Invoke(Field("metaProgress"), "IsLevelFirstCleared", 0), Is.EqualTo(false));
            Assert.That((bool)fail.GetType().GetProperty("IsOpen").GetValue(fail), Is.False);
            Assert.That(Field("timerRunning"), Is.EqualTo(true));
        }

        [UnityTest]
        public IEnumerator ControllerHelpers_BrowseWithoutUnlockingAndStopAtCatalogEnds()
        {
            Call("LoadPreviousLevel");
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(1));
            Call("LoadNextLevel");
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(2));
            Assert.That(Field("levelNavigationTesting"), Is.EqualTo(true));
            Assert.That(Invoke(Field("metaProgress"), "IsLevelPassed", 0), Is.EqualTo(false));
            Call("LoadPreviousLevel");
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(1));
            Call("PreviewLevelForTesting", 99);
            Call("LoadNextLevel");
            Assert.That(Game.GetType().GetProperty("CurrentLevelNumber").GetValue(Game), Is.EqualTo(100));
            yield return null;
        }
    }
}
