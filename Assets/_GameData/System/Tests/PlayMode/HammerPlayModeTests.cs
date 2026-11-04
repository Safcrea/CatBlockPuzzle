using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatBlockPuzzle.Tests
{
    /// <summary>The Hammer clears the way with one tap: cats on the wrong spots, padlocks and fog. It spends one only when it had work.</summary>
    public sealed class HammerPlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";

        private static object Sys() => Type.GetType("CatBlockPuzzle.GameSystem, Assembly-CSharp").GetProperty("Instance").GetValue(null);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
        private static int HammerCount() => (int)Call(Sys(), "GetPowerUpCount", Enum.Parse(Type.GetType("CatBlockPuzzle.PowerUpKind, Assembly-CSharp"), "Hammer"));
        private static bool Press() => (bool)Call(Sys(), "TryUseHammerPowerUp");
        private static T Get<T>(object o, string name) => (T)o.GetType().GetField(name).GetValue(o);
        private IEnumerable<object> Pieces() => ((IEnumerable)Field("pieces")).Cast<object>();

        private IEnumerator Open(int levelIndex)
        {
            Call(Sys(), "PreviewLevel", levelIndex);
            float until = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < until && ((bool)Field("inputLocked") || (bool)Field("levelFailed"))) yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // Puts the first cat on the board at the spot of the SECOND cat, so it blocks it.
        private (object blocker, object wanted) BlockSecondCatWithFirst()
        {
            var pieces = Pieces().ToList();
            object blocker = pieces[0], wanted = pieces[1];
            var def = wanted.GetType().GetField("Solution").GetValue(wanted);
            int row = (int)def.GetType().GetField("SolutionRow").GetValue(def);
            int col = (int)def.GetType().GetField("SolutionCol").GetValue(def);
            Call(Game, "PlacePiece", blocker, row, col, true);
            return (blocker, wanted);
        }

        [UnityTest]
        public IEnumerator Hammer_WithNothingInTheWay_SpendsNothing()
        {
            yield return Open(4);
            int before = HammerCount();
            Assert.That(Press(), Is.False, "An empty board has nothing in the way.");
            Assert.That(HammerCount(), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator Hammer_KnocksACatOffAnotherCatsSpot_AndIsSpentOnce()
        {
            yield return Open(4);
            var (blocker, wanted) = BlockSecondCatWithFirst();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Get<bool>(blocker, "Placed"), Is.True, "Test setup: the blocker is on the board.");
            int before = HammerCount();
            Assert.That(before, Is.GreaterThan(0));
            Assert.That(Press(), Is.True, "A cat in the way gives the Hammer work.");
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.That(Get<bool>(blocker, "Placed"), Is.False, "The blocking cat is back on the shelf.");
            Assert.That(HammerCount(), Is.EqualTo(before - 1), "One use spends exactly one Hammer.");
            Assert.That((bool)Field("inputLocked"), Is.False, "Input is handed back afterwards.");
        }

        [UnityTest]
        public IEnumerator Hammer_LeavesACatThatSitsOnItsOwnSpot()
        {
            yield return Open(4);
            object first = Pieces().First();
            var def = first.GetType().GetField("Solution").GetValue(first);
            Call(Game, "PlacePiece", first, (int)def.GetType().GetField("SolutionRow").GetValue(def), (int)def.GetType().GetField("SolutionCol").GetValue(def), true);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Press(), Is.False, "A correctly placed cat is not in the way.");
            Assert.That(Get<bool>(first, "Placed"), Is.True);
        }

        [UnityTest]
        public IEnumerator Hammer_BreaksAPadlockThatKeepsACatOut()
        {
            for (int index = 11; index < 60; index++)
            {
                yield return Open(index);
                var level = Field("activeLevel");
                var locked = (IDictionary)level.GetType().GetField("LockedCells").GetValue(level);
                if (locked.Count == 0) continue;
                Vector2Int cell = locked.Keys.Cast<Vector2Int>().First();
                var locks = Field("locks");
                var isLocked = locks.GetType().GetMethod("IsLocked");
                Assert.That((bool)isLocked.Invoke(locks, new object[] { cell }), Is.True);
                int before = HammerCount();
                Assert.That(Press(), Is.True, "A closed padlock is something to break.");
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.That(HammerCount(), Is.EqualTo(before - 1));
                var closedLeft = new List<Vector2Int>();
                locks.GetType().GetMethod("CollectClosedLockCells").Invoke(locks, new object[] { closedLeft });
                Assert.That(closedLeft.Count, Is.LessThan(locked.Count), "At least one padlock is open.");
                yield break;
            }
            Assert.Inconclusive("No level in 12-60 starts with a padlock.");
        }

        [UnityTest]
        public IEnumerator Hammer_BlowsAllFogAway()
        {
            for (int index = 40; index < 100; index++)
            {
                yield return Open(index);
                var level = Field("activeLevel");
                if (!((IEnumerable)level.GetType().GetField("FogCells").GetValue(level)).Cast<Vector2Int>().Any()) continue;
                var fog = Field("fog");
                int before = HammerCount();
                Assert.That(Press(), Is.True, "Fog is something to clear.");
                yield return new WaitForSecondsRealtime(1.6f);
                Assert.That((int)fog.GetType().GetProperty("CloudCount").GetValue(fog), Is.Zero, "No cloud is left.");
                Assert.That(HammerCount(), Is.EqualTo(before - 1));
                yield break;
            }
            Assert.Inconclusive("No level in 41-100 starts with fog.");
        }
    }
}
