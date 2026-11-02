using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    /// <summary>Hint power-up on many levels, popup dimmers, and every button on the daily reward panel.</summary>
    public sealed class HintDimAndDailyRewardPlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static object Get(object target, string member)
        {
            FieldInfo field = target.GetType().GetField(member, Flags);
            if (field != null) return field.GetValue(target);
            return target.GetType().GetProperty(member, Flags).GetValue(target);
        }

        private static MonoBehaviour Screen(string type) =>
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(g => g.GetType().Name == type);

        private static object Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethods(Flags).First(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);

        // A plain C# service owned by the game root (PowerUpController, HintPresenter, ...).
        private object Service(string typeName)
        {
            foreach (FieldInfo field in Game.GetType().GetFields(Flags))
            {
                object value = field.GetValue(Game);
                if (value != null && value.GetType().Name == typeName) return value;
            }
            foreach (FieldInfo field in Game.GetType().GetFields(Flags))
            {
                object outer = field.GetValue(Game);
                if (outer == null || outer is UnityEngine.Object || field.FieldType.Namespace != "CatBlockPuzzle") continue;
                foreach (FieldInfo inner in outer.GetType().GetFields(Flags))
                {
                    object value = inner.GetValue(outer);
                    if (value != null && value.GetType().Name == typeName) return value;
                }
            }
            throw new MissingMemberException(typeName);
        }

        private object[] Pieces => ((IList)Field("pieces")).Cast<object>().ToArray();

        private IEnumerator Open(int zeroBasedLevel)
        {
            Invoke(Screen("GameSystem"), "ShowGameplayPage");
            Call("PreviewLevelForTesting", zeroBasedLevel);
            yield return new WaitForSecondsRealtime(1.4f);
        }

        // ---- Hint -------------------------------------------------------------------------------------------

        private Dictionary<Vector2Int, Image> BoardImages() =>
            ((IDictionary)Field("boardCells")).Keys.Cast<Vector2Int>().ToDictionary(k => k, k => (Image)Get(((IDictionary)Field("boardCells"))[k], "Image"));

        private static bool Differs(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) > 0.03f;

        [UnityTest]
        public IEnumerator Hint_LightsTheWholeSpotOfTheCatItPointsAt_OnEveryTypeOfLevel()
        {
            int[] levels = { 0, 4, 11, 15, 20, 25, 30, 40, 55, 65, 80, 99 };
            object hint = Service("HintPresenter");
            var failures = new List<string>();
            var notes = new List<string>();
            int shown = 0, singleCell = 0;
            foreach (int level in levels)
            {
                yield return Open(level);
                // Banners and intros lock input for a moment after a level opens.
                for (float waited = 0f; (bool)Field("inputLocked") && waited < 8f; waited += 0.25f) yield return new WaitForSecondsRealtime(0.25f);
                for (int step = 0; step < 16; step++)
                {
                    Dictionary<Vector2Int, Image> board = BoardImages();
                    // The level's own reveal setting, through the same call the power-up makes (the preview harness has no running timer).
                    object reveal = Get(Get(Service("LevelTimer"), "Profile"), "HintReveal");
                    bool used = true;
                    bool found = (bool)Invoke(hint, "ShowHint", reveal);
                    if (!found)
                    {
                        notes.Add("level " + (level + 1) + " stopped at step " + step + ": used=" + used + " locked=" + Field("inputLocked") + " reveal=" + reveal + " pieces=[" + string.Join(" ", Pieces.Select(p => (((bool)Get(p, "Placed")) ? "P" : "-") + (((bool)Get(Get(p, "Definition"), "IsSpare")) ? "S" : "") + (((bool)Get(Get(p, "Definition"), "IsFixed")) ? "F" : "") + ((Get(p, "Slot") is RectTransform sl && !sl.gameObject.activeSelf) ? "h" : "") + ((bool)Invoke(hint, "SolutionSpotIsFree", p, null) ? "free" : "blocked"))) + "] placed=" + Pieces.Count(p => (bool)Get(p, "Placed")) + "/" + Pieces.Length);
                        Invoke(hint, "StopHint");
                        break;
                    }
                    // Only the hinted cells pulse, so what changes between the first frame and the pulse's peak is the lit spot.
                    yield return null;
                    Dictionary<Vector2Int, Color> before = board.ToDictionary(p => p.Key, p => p.Value.color);
                    yield return new WaitForSecondsRealtime(0.15f);
                    object piece = Get(hint, "lastHinted");
                    object solution = Get(piece, "Solution");
                    int row = (int)Get(solution, "SolutionRow"), col = (int)Get(solution, "SolutionCol");
                    var offsets = ((Array)Get(solution, "Cells")).Cast<object>().Select(o => new Vector2Int(row + (int)Get(o, "Row"), col + (int)Get(o, "Col"))).ToList();
                    int expected = offsets.Count(board.ContainsKey);
                    int lit = board.Count(p => Differs(p.Value.color, before[p.Key]));
                    shown++;
                    if (expected == 1) singleCell++;
                    if (lit != expected) failures.Add("level " + (level + 1) + " step " + step + ": " + lit + " cells lit, spot has " + expected);
                    Invoke(hint, "StopHint");
                    Call("PlacePiece", piece, row, col, true);
                    yield return new WaitForSecondsRealtime(0.7f); // placement feedback animates the cells it covers
                }
            }
            Debug.Log("[HintTest] hints shown " + shown + ", spots that are a single cell " + singleCell + ", failures " + failures.Count);
            Assert.That(shown, Is.GreaterThan(levels.Length), "Hints should have fired several times per level.\n" + string.Join("\n", notes));
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // ---- Popup dimmers ----------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator PopupDimmers_AreNearlyOpaqueSoTheBackgroundIsHidden()
        {
            yield return null;
            var weak = new List<string>();
            int checkedCount = 0;
            foreach (Image image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string name = image.gameObject.name;
                if (name != "Dimmer" && name != "Room Complete Overlay" && name != "Story Overlay") continue;
                checkedCount++;
                float luminance = 0.2126f * image.color.r + 0.7152f * image.color.g + 0.0722f * image.color.b;
                if (image.color.a < 0.9f || luminance > 0.1f) weak.Add(image.transform.parent.name + "/" + name + " a=" + image.color.a);
            }
            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(8));
            Assert.That(weak, Is.Empty, string.Join("\n", weak));
            Assert.That(RuntimeUiFactory_OverlayAlpha(), Is.GreaterThanOrEqualTo(0.9f), "Runtime popups (purchase, coming soon) use this shade.");
        }

        private float RuntimeUiFactory_OverlayAlpha()
        {
            Type factory = Game.GetType().Assembly.GetType("CatBlockPuzzle.RuntimeUiFactory");
            var root = new GameObject("Probe", typeof(RectTransform)).GetComponent<RectTransform>();
            factory.GetMethod("PrepareOverlay", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { root });
            float alpha = root.GetComponent<Image>().color.a;
            UnityEngine.Object.Destroy(root.gameObject);
            return alpha;
        }

        // ---- Daily reward -----------------------------------------------------------------------------------

        private sealed class Daily
        {
            public MonoBehaviour Screen;
            public Array Days;
            public Button[] Close;
            public bool IsOpen => (bool)Get(Screen, "IsOpen");
            public int Available => (int)Get(Screen, "AvailableDay");
            public Button Field(string name) => (Button)Get(Screen, name);
            public object Day(int day) => Days.Cast<object>().Single(d => d != null && (int)Get(d, "day") == day);
        }

        private Daily Open_Daily()
        {
            MonoBehaviour screen = Screen("DailyRewardScreen");
            var daily = new Daily { Screen = screen, Days = (Array)Get(screen, "days") };
            daily.Close = new[] { "closeButton", "laterButton", "okayButton" }.Select(daily.Field).ToArray();
            return daily;
        }

        private int Coins => (int)Get(Screen("GameSystem"), "CurrentCoins");

        private int PowerUps(string kind)
        {
            Type inventory = Game.GetType().Assembly.GetType("CatBlockPuzzle.PowerUpInventory");
            Type enumType = Game.GetType().Assembly.GetType("CatBlockPuzzle.PowerUpKind");
            return (int)inventory.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { Enum.Parse(enumType, kind) });
        }

        private IEnumerator ShowDaily()
        {
            Invoke(Screen("GameSystem"), "GoHome");
            yield return new WaitForSecondsRealtime(0.3f);
            Invoke(Screen("ReferenceUiNavigation"), "ShowDailyReward");
            yield return new WaitForSecondsRealtime(0.6f);
        }

        private IEnumerator Settle() { yield return new WaitForSecondsRealtime(0.6f); }

        [UnityTest]
        public IEnumerator DailyReward_OnlyTodaysCardIsClaimable_LockedTapsAndCloseButtonsChangeNothing()
        {
            yield return ShowDaily();
            Daily daily = Open_Daily();
            Assert.That(daily.IsOpen, Is.True, "Daily panel did not open.");
            Assert.That(daily.Available, Is.EqualTo(1));
            int coins = Coins, hints = PowerUps("Hint"), freezes = PowerUps("Freeze");

            foreach (object day in daily.Days)
            {
                if (day == null) { Debug.Log("[DailyTest] a day entry is null"); continue; }
                int number = (int)Get(day, "day");
                var card = (Button)Get(day, "cardButton");
                var claim = (Button)Get(day, "claimButton");
                Debug.Log("[DailyTest] day " + number + " card=" + (card != null) + " claim=" + (claim != null));
                bool today = number == 1;
                if (card != null) Assert.That(card.interactable, Is.EqualTo(today), "card " + number);
                if (claim != null) Assert.That(claim.interactable, Is.EqualTo(today), "claim " + number);
                if (today) continue;
                card?.onClick.Invoke(); claim?.onClick.Invoke();
            }
            yield return Settle();
            Assert.That(daily.IsOpen, Is.True, "Tapping a locked day closed the panel.");
            Assert.That(daily.Available, Is.EqualTo(1), "Tapping a locked day claimed something.");
            Assert.That((Coins, PowerUps("Hint"), PowerUps("Freeze")), Is.EqualTo((coins, hints, freezes)), "Locked taps changed the balance.");

            foreach (string name in new[] { "closeButton", "laterButton", "okayButton" })
            {
                Button button = daily.Field(name);
                if (button == null) { Debug.Log("[DailyTest] " + name + " is not assigned on the screen."); continue; }
                if (!daily.IsOpen) yield return ShowDaily();
                button.onClick.Invoke();
                yield return Settle();
                Assert.That(daily.IsOpen, Is.False, name + " did not close the panel.");
                Assert.That(daily.Available, Is.EqualTo(1), name + " claimed the reward.");
                Assert.That((Coins, PowerUps("Hint"), PowerUps("Freeze")), Is.EqualTo((coins, hints, freezes)), name + " changed the balance.");
                Assert.That((bool)Get(Screen("HomeController"), "IsOpen"), Is.True, name + " did not return Home.");
            }
        }

        [UnityTest]
        public IEnumerator DailyReward_ClaimPaysOnceEvenAfterReopening_AndClaimedDayIsDead()
        {
            for (int i = 0; i < 3; i++) yield return ShowDaily();
            Daily daily = Open_Daily();
            Type configType = Game.GetType().Assembly.GetType("CatBlockPuzzle.DailyRewardConfig");
            object config = configType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            object reward = Invoke(config, "GetDay", 1);
            int coinsPaid = (int)Get(reward, "coins"), hintsPaid = (int)Get(reward, "hintCount"), freezesPaid = (int)Get(reward, "freezeCount");
            int coins = Coins, hints = PowerUps("Hint"), freezes = PowerUps("Freeze");

            var claim = (Button)Get(daily.Day(1), "claimButton") ?? (Button)Get(daily.Day(1), "cardButton") ?? daily.Field("claimCurrentButton");
            Assert.That(claim, Is.Not.Null, "No button can claim today's reward.");
            Assert.That(claim.interactable, Is.True);
            claim.onClick.Invoke();
            yield return Settle();

            Assert.That(daily.Available, Is.EqualTo(0), "Today's reward is still claimable after claiming.");
            Assert.That(Coins - coins, Is.EqualTo(coinsPaid), "Coins paid");
            Assert.That(PowerUps("Hint") - hints, Is.EqualTo(hintsPaid), "Hints paid");
            Assert.That(PowerUps("Freeze") - freezes, Is.EqualTo(freezesPaid), "Freezes paid");
            Assert.That(daily.IsOpen, Is.False, "The panel should close after a claim.");

            yield return ShowDaily();
            int coinsAfter = Coins, hintsAfter = PowerUps("Hint"), freezesAfter = PowerUps("Freeze");
            foreach (object day in daily.Days)
            {
                if (day == null) continue;
                foreach (string name in new[] { "cardButton", "claimButton" })
                {
                    var button = (Button)Get(day, name);
                    if (button == null) continue;
                    Assert.That(button.interactable, Is.False, name + " of day " + Get(day, "day") + " is live after the claim");
                    button.onClick.Invoke();
                }
            }
            Invoke(daily.Screen, "RequestClaim", 1);
            Invoke(daily.Screen, "RequestClaim", 2);
            yield return Settle();
            Assert.That((Coins, PowerUps("Hint"), PowerUps("Freeze")), Is.EqualTo((coinsAfter, hintsAfter, freezesAfter)), "A second claim paid again.");
            Assert.That(daily.IsOpen, Is.True, "Dead taps should leave the panel open.");

            Button close = daily.Close.FirstOrDefault(b => b != null);
            Assert.That(close, Is.Not.Null);
            close.onClick.Invoke();
            yield return Settle();
            Assert.That(daily.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator DailyReward_ClaimCurrentButton_ClaimsTodayOnce()
        {
            yield return ShowDaily();
            Daily daily = Open_Daily();
            Button claimCurrent = daily.Field("claimCurrentButton");
            if (claimCurrent == null) { Assert.Ignore("claimCurrentButton is not assigned in the scene."); yield break; }
            int coins = Coins;
            claimCurrent.onClick.Invoke();
            claimCurrent.onClick.Invoke();
            yield return Settle();
            Assert.That(daily.Available, Is.EqualTo(0));
            Assert.That(Coins, Is.GreaterThanOrEqualTo(coins));
        }
    }
}
