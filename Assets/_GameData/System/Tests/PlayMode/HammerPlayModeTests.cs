using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace CatBlockPuzzle.Tests
{
    public sealed class HammerPlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";

        private static object System_() => Type.GetType("CatBlockPuzzle.GameSystem, Assembly-CSharp").GetProperty("Instance").GetValue(null);
        private static object Sys(string method, params object[] args) =>
            System_().GetType().GetMethod(method).Invoke(System_(), args);
        private static bool SysFlag(string name) => (bool)System_().GetType().GetProperty(name).GetValue(System_());
        private static int HammerCount() => (int)Sys("GetPowerUpCount", Enum.Parse(Type.GetType("CatBlockPuzzle.PowerUpKind, Assembly-CSharp"), "Hammer"));

        private IEnumerator Open(int levelIndex)
        {
            Sys("PreviewLevel", levelIndex);
            float until = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < until && ((bool)Field("inputLocked") || (bool)Field("levelFailed"))) yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private bool PressHammer() => (bool)Sys("TryUseHammerPowerUp");

        private static Vector2 ScreenOf(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(rect.rect.center));
        }

        private static void TapCatcher(Vector2 screen)
        {
            GameObject catcher = GameObject.Find("Hammer Catcher");
            Assert.That(catcher, Is.Not.Null, "The Hammer did not put its tap layer over the board.");
            var data = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(catcher, data, ExecuteEvents.pointerClickHandler);
        }

        private IEnumerable<object> Pieces() => ((IEnumerable)Field("pieces")).Cast<object>();
        private static T Get<T>(object o, string name) => (T)o.GetType().GetField(name).GetValue(o);

        [UnityTest]
        public IEnumerator Hammer_KnocksALandedCatBackToTheShelf_AndIsSpentOnce()
        {
            yield return Open(4);
            object piece = Pieces().First();
            Call("PlacePiece", piece, 0, 0, true);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Get<bool>(piece, "Placed"), Is.True, "Test setup: the cat should be on the board.");
            int before = HammerCount();
            Assert.That(before, Is.GreaterThan(0));

            Assert.That(PressHammer(), Is.True, "A landed cat is something to smash, so the Hammer arms.");
            Assert.That(SysFlag("IsHammerActive"), Is.True);
            Assert.That(HammerCount(), Is.EqualTo(before), "Arming the Hammer is free.");

            TapCatcher(ScreenOf(Get<RectTransform>(piece, "Rect")));
            yield return new WaitForSecondsRealtime(1.4f);

            Assert.That(Get<bool>(piece, "Placed"), Is.False, "The smashed cat is back on the shelf.");
            Assert.That(HammerCount(), Is.EqualTo(before - 1), "A real smash spends exactly one Hammer.");
            Assert.That(SysFlag("IsHammerActive"), Is.False);
            Assert.That(GameObject.Find("Hammer Catcher"), Is.Null);
            Assert.That((bool)Field("inputLocked"), Is.False, "Input is handed back once the cat is home.");
        }

        [UnityTest]
        public IEnumerator Hammer_PressedAgain_PutsItAwayForFree()
        {
            yield return Open(4);
            object piece = Pieces().First();
            Call("PlacePiece", piece, 0, 0, true);
            yield return new WaitForSecondsRealtime(0.5f);
            int before = HammerCount();
            Assert.That(PressHammer(), Is.True);
            Assert.That(PressHammer(), Is.False, "The second press cancels instead of arming again.");
            Assert.That(SysFlag("IsHammerActive"), Is.False);
            Assert.That(HammerCount(), Is.EqualTo(before));
            Assert.That(GameObject.Find("Hammer Catcher"), Is.Null);
        }

        [UnityTest]
        public IEnumerator Hammer_WontArmOnAnEmptyBoard()
        {
            yield return Open(4);
            Assert.That(PressHammer(), Is.False, "Nothing is on the board, so a Hammer would be wasted.");
            Assert.That(SysFlag("IsHammerActive"), Is.False);
        }

        [UnityTest]
        public IEnumerator Hammer_TappingAnEmptyCell_SpendsNothing()
        {
            yield return Open(4);
            object piece = Pieces().First();
            Call("PlacePiece", piece, 0, 0, true);
            yield return new WaitForSecondsRealtime(0.5f);
            int before = HammerCount();
            PressHammer();
            var cells = (IDictionary)Field("boardCells");
            // A cell the placed cat does not cover.
            var occupied = (IDictionary)Field("occupancy");
            RectTransform free = null;
            foreach (DictionaryEntry e in cells)
                if (!occupied.Contains(e.Key)) { free = (RectTransform)e.Value.GetType().GetProperty("Rect").GetValue(e.Value); break; }
            Assert.That(free, Is.Not.Null);
            TapCatcher(ScreenOf(free));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(SysFlag("IsHammerActive"), Is.True, "A miss leaves the Hammer armed.");
            Assert.That(HammerCount(), Is.EqualTo(before));
            Assert.That(Get<bool>(piece, "Placed"), Is.True);
        }

        [UnityTest]
        public IEnumerator Hammer_SmashesAPadlockOpen()
        {
            for (int index = 11; index < 60; index++)
            {
                yield return Open(index);
                var level = Field("activeLevel");
                var locked = (IDictionary)level.GetType().GetField("LockedCells").GetValue(level);
                if (locked.Count == 0) continue;
                Vector2Int cell = locked.Keys.Cast<Vector2Int>().First();
                var cells = (IDictionary)Field("boardCells");
                object view = cells[cell];
                RectTransform rect = (RectTransform)view.GetType().GetProperty("Rect").GetValue(view);
                var locks = Field("locks");
                Assert.That((bool)locks.GetType().GetMethod("IsLocked").Invoke(locks, new object[] { cell }), Is.True);
                int before = HammerCount();
                Assert.That(PressHammer(), Is.True, "A closed padlock is something to smash.");
                TapCatcher(ScreenOf(rect));
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That((bool)locks.GetType().GetMethod("IsLocked").Invoke(locks, new object[] { cell }), Is.False, "The padlock is open.");
                Assert.That(HammerCount(), Is.EqualTo(before - 1));
                yield break;
            }
            Assert.Inconclusive("No level in 12-60 starts with a padlock.");
        }

        [UnityTest]
        public IEnumerator Hammer_BlowsFogAway()
        {
            for (int index = 40; index < 100; index++)
            {
                yield return Open(index);
                var level = Field("activeLevel");
                var fogCells = ((IEnumerable)level.GetType().GetField("FogCells").GetValue(level)).Cast<Vector2Int>().ToList();
                if (fogCells.Count == 0) continue;
                Vector2Int cell = fogCells[0];
                var fog = Field("fog");
                var isFoggy = fog.GetType().GetMethod("IsFoggy");
                Assert.That((bool)isFoggy.Invoke(fog, new object[] { cell }), Is.True);
                var cells = (IDictionary)Field("boardCells");
                object view = cells[cell];
                RectTransform rect = (RectTransform)view.GetType().GetProperty("Rect").GetValue(view);
                int before = HammerCount();
                Assert.That(PressHammer(), Is.True, "Fog is something to smash.");
                TapCatcher(ScreenOf(rect));
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That((bool)isFoggy.Invoke(fog, new object[] { cell }), Is.False, "The cloud is gone.");
                Assert.That(HammerCount(), Is.EqualTo(before - 1));
                yield break;
            }
            Assert.Inconclusive("No level in 41-100 starts with fog.");
        }
    }
}
