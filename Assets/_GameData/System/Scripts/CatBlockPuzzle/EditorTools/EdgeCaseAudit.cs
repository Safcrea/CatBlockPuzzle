#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Plays real levels in Play Mode the ways a player can win them (plain, after a Hint, after a rewarded-ad Hint, with the
    /// double-coins ad, after Undo and Freeze, a retry, a replay) and checks that the coins paid are what the rules say. Start it
    /// from Cat Block Puzzle > Testing > Run Edge Case Audit; the result is in <see cref="Report"/> and the console.
    /// </summary>
    internal static class EdgeCaseAudit
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        // Plain boards with no padlocks, so placing every cat on its solution spot always works.
        private static readonly int[] TestLevels = { 5, 6, 7, 8, 9, 10, 11, 12 };

        public static string Report = "not run";
        private static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        private static double waitUntil;
        private static readonly StringBuilder Log = new StringBuilder();
        private static int passed, failed;

        [MenuItem("Cat Block Puzzle/Testing/Run Edge Case Audit (Play Mode)")]
        public static void Start()
        {
            if (!Application.isPlaying) { EditorUtility.DisplayDialog("Edge case audit", "Enter play mode first.", "OK"); return; }
            Report = "running";
            Log.Clear();
            passed = failed = 0;
            stack.Clear();
            stack.Push(Run());
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            bool more = true;
            try
            {
                // Scenarios yield nested routines, so run the innermost one and pop it when it ends.
                if (stack.Count > 0)
                {
                    IEnumerator top = stack.Peek();
                    if (!top.MoveNext()) stack.Pop();
                    else if (top.Current is IEnumerator inner) stack.Push(inner);
                    else if (top.Current is float seconds) waitUntil = EditorApplication.timeSinceStartup + seconds;
                }
                more = stack.Count > 0;
            }
            catch (System.Exception e)
            {
                Log.AppendLine("EXCEPTION: " + e);
                more = false;
            }
            if (more) return;
            EditorApplication.update -= Tick;
            Report = "done: " + passed + " passed, " + failed + " failed\n" + Log;
            Debug.Log("[EdgeCaseAudit] " + Report);
        }

        private static void Check(bool ok, string what)
        {
            if (ok) passed++; else failed++;
            Log.AppendLine((ok ? "PASS  " : "FAIL  ") + what);
        }

        private static int ReplayReward(int reward) => reward <= 0 ? 0 : Mathf.Max(5, Mathf.RoundToInt(reward * 0.5f));

        private static IEnumerator Run()
        {
            var game = Object.FindFirstObjectByType<CatBlockPuzzleGame>();
            GameSystem system = GameSystem.Instance;
            if (game == null || system == null) { Log.AppendLine("No game running."); yield break; }

            // Remember and restore every setting this audit touches.
            FieldInfo unlock = typeof(GameSystem).GetField("unlockAllLevels", Any);
            object unlockBefore = unlock.GetValue(system);
            unlock.SetValue(system, true);
            string[] keys = TutorialReset.SeenKeys();
            var seenBefore = new Dictionary<string, int>();
            foreach (string key in keys) { seenBefore[key] = PlayerPrefs.GetInt(key, 0); PlayerPrefs.SetInt(key, 1); }
            int hintsBefore = PowerUpInventory.GetCount(PowerUpKind.Hint), freezesBefore = PowerUpInventory.GetCount(PowerUpKind.Freeze);
            RewardedAds.SimulatedOutcome = RewardedAds.Outcome.Rewarded;
            int coinsInitial = game.Progress.Coins;
            int hammersBefore = PowerUpInventory.GetCount(PowerUpKind.Hammer);

            int n = 0;

            // 1. A plain win pays exactly what the rules say.
            yield return Win(game, system, TestLevels[n++ % TestLevels.Length], "plain win", null, false);
            // 2. A win after spending a Hint from the bag.
            yield return Win(game, system, TestLevels[n++ % TestLevels.Length], "win after a Hint from the bag", g =>
            {
                PowerUpInventory.Add(PowerUpKind.Hint, 1);
                Check(UseHint(system, g), "  the Hint button works");
            }, false);
            // 3. A win after a Hint that came from a rewarded ad.
            yield return Win(game, system, TestLevels[n++ % TestLevels.Length], "win after a rewarded-ad Hint", g =>
            {
                int before = PowerUpInventory.GetCount(PowerUpKind.Hint);
                RewardedAds.Show("free_hint", () => PowerUpInventory.Add(PowerUpKind.Hint, 1), o => { });
                Check(PowerUpInventory.GetCount(PowerUpKind.Hint) == before + 1, "  the ad grants one Hint");
                Check(UseHint(system, g), "  the Hint is usable");
            }, false);
            // 4. The double-coins ad on the result screen pays the level's coins a second time.
            yield return Win(game, system, TestLevels[n++ % TestLevels.Length], "win then double coins with an ad", null, true);
            // 5. Undo and Freeze before the win.
            yield return Win(game, system, TestLevels[n++ % TestLevels.Length], "win after Undo and Freeze", g =>
            {
                PowerUpInventory.Add(PowerUpKind.Freeze, 1);
                system.TryUseFreezePowerUp();
                PlaceAllExceptLast(g);
                system.Undo?.TryUndo();
            }, false);
            // 6. Winning the same level again pays the replay rate, never zero.
            int replay = TestLevels[0];
            yield return Win(game, system, replay, "replay of a cleared level", null, false);
            yield return Win(game, system, replay, "second replay of the same level", null, false);


            yield return Economy(game, system);

            // 7. Preview play (the level test buttons) pays nothing and must not touch coins.
            yield return PreviewWin(game, system, TestLevels[1]);

            foreach (string key in keys) PlayerPrefs.SetInt(key, seenBefore[key]);
            unlock.SetValue(system, unlockBefore);
            RewardedAds.SimulatedOutcome = null;
            // Leave the power-up bag as it was.
            int hintsNow = PowerUpInventory.GetCount(PowerUpKind.Hint);
            int freezesNow = PowerUpInventory.GetCount(PowerUpKind.Freeze);
            // Put the bag and the wallet back exactly as they were.
            SetBag(PowerUpKind.Hint, hintsBefore); SetBag(PowerUpKind.Freeze, freezesBefore); SetBag(PowerUpKind.Hammer, hammersBefore);
            SetCoins(game, coinsInitial);
            Log.AppendLine("bag restored: hints " + hintsBefore + " -> " + PowerUpInventory.GetCount(PowerUpKind.Hint) + ", freezes " + freezesBefore + " -> " + PowerUpInventory.GetCount(PowerUpKind.Freeze));
            system.GoHome();
        }

        private static IEnumerator Win(CatBlockPuzzleGame game, GameSystem system, int levelNumber, string name, System.Action<CatBlockPuzzleGame> before, bool doubleCoins)
        {
            Log.AppendLine("-- " + name + " (level " + levelNumber + ")");
            system.GoHome();
            yield return 0.6f;
            if (!system.StartLevelNumber(levelNumber)) { Check(false, "  level " + levelNumber + " starts"); yield break; }
            // The level start (reveal, difficulty banner, tutorials) must be over before a player could act.
            yield return 5f;
            game.Flow.CancelReveal(true);
            PuzzleSession s = game.Session;
            s.inputLocked = false;
            int index = s.levelIndex;
            bool firstClear = !game.Meta.Progress.IsLevelFirstCleared(index);
            int reward = s.activeLevel.Reward;
            int expected = firstClear ? reward : ReplayReward(reward);
            int coinsBefore = game.Progress.Coins;
            Check(!s.levelNavigationTesting, "  a real level, not a preview");

            before?.Invoke(game);
            yield return 0.3f;
            PlaceAll(game);
            yield return 3.2f;

            int gained = game.Progress.Coins - coinsBefore;
            Check(gained == expected, "  coins: first clear=" + firstClear + " reward=" + reward + " expected +" + expected + ", got +" + gained);
            if (doubleCoins)
            {
                var screen = Object.FindFirstObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
                Check(screen != null && screen.IsOpen, "  the result screen is open");
                int beforeDouble = game.Progress.Coins;
                if (screen != null) screen.ClaimDoubleCoins();
                yield return 2.5f;
                int extra = game.Progress.Coins - beforeDouble;
                Check(extra == gained, "  double coins: expected +" + gained + " more, got +" + extra);
            }
        }

        private static void SetCoins(CatBlockPuzzleGame game, int coins) { game.Progress.Coins = coins; game.Progress.SaveCoins(); }

        private static void SetBag(PowerUpKind kind, int count)
        {
            int now = PowerUpInventory.GetCount(kind);
            if (now > count) PowerUpInventory.TryConsume(kind, now - count);
            else if (now < count) PowerUpInventory.Add(kind, count - now);
        }

        // A real drop tells the Undo controller where the cat came from before it lands; PlacePiece alone does not.
        private static void RecordForUndo(GameSystem system, PieceState piece)
        {
            UndoController undo = system.Undo;
            if (undo == null) return;
            MethodInfo remember = typeof(UndoController).GetMethod("Remember", Any);
            MethodInfo record = typeof(UndoController).GetMethod("Record", Any);
            remember?.Invoke(undo, new object[] { piece, false, -1, -1 });
            record?.Invoke(undo, new object[] { piece });
        }

        private static bool PlaceOne(CatBlockPuzzleGame game)
        {
            PuzzleSession s = game.Session;
            foreach (PieceState p in s.pieces)
            {
                if (p.Placed || p.Definition.IsSpare || p.Definition.IsFixed) continue;
                if (!game.Board.CanPlace(p, p.Solution.SolutionRow, p.Solution.SolutionCol)) continue;
                game.Drag.PlacePiece(p, p.Solution.SolutionRow, p.Solution.SolutionCol, true);
                s.inputLocked = false;
                RecordForUndo(GameSystem.Instance, p);
                return true;
            }
            return false;
        }

        // Buying, using and undoing: every coin and every charge that moves must be the right amount, and a refused action
        // must change nothing.
        private static IEnumerator Economy(CatBlockPuzzleGame game, GameSystem system)
        {
            Log.AppendLine("-- power-up economy");
            system.GoHome();
            yield return 1f;
            PowerUpShopConfig shop = system.PowerUpShop;
            Check(shop != null, "  the shop config loads");
            if (shop == null) yield break;

            // Buying a pack (from the shop, where gameplay is hidden): the exact price leaves the wallet and the exact quantity
            // lands in the bag; one coin short changes nothing.
            foreach (PowerUpKind kind in new[] { PowerUpKind.Hint, PowerUpKind.Freeze, PowerUpKind.Hammer })
            {
                PowerUpShopConfig.Offer offer = shop.GetOffer(kind);
                SetBag(kind, 0); SetCoins(game, 1000);
                var status = system.PurchasePowerUpFromShop(kind);
                Check(status == PowerUpPurchaseStatus.Success && game.Progress.Coins == 1000 - offer.coinPrice && PowerUpInventory.GetCount(kind) == offer.quantity,
                    "  buy " + kind + ": " + status + ", coins 1000 -> " + game.Progress.Coins + " (price " + offer.coinPrice + "), bag " + PowerUpInventory.GetCount(kind) + " (pack " + offer.quantity + ")");
                SetCoins(game, offer.coinPrice - 1);
                status = system.PurchasePowerUpFromShop(kind);
                Check(status == PowerUpPurchaseStatus.InsufficientCoins && game.Progress.Coins == offer.coinPrice - 1 && PowerUpInventory.GetCount(kind) == offer.quantity,
                    "  buy " + kind + " one coin short: " + status + " and nothing changed (coins " + game.Progress.Coins + ", bag " + PowerUpInventory.GetCount(kind) + ")");
            }

            PowerUpShopConfig.Bundle bundle = shop.StarterBundle;
            if (bundle.IsValid)
            {
                SetBag(PowerUpKind.Hint, 0); SetBag(PowerUpKind.Freeze, 0); SetBag(PowerUpKind.Hammer, 0); SetCoins(game, 1000);
                var status = system.PurchaseStarterBundle();
                int expectedCoins = 1000 - bundle.coinPrice + bundle.bonusCoins;
                Check(status == PowerUpPurchaseStatus.Success && game.Progress.Coins == expectedCoins
                    && PowerUpInventory.GetCount(PowerUpKind.Hint) == bundle.hints && PowerUpInventory.GetCount(PowerUpKind.Freeze) == bundle.freezes && PowerUpInventory.GetCount(PowerUpKind.Hammer) == bundle.hammers,
                    "  starter bundle: " + status + ", coins 1000 -> " + game.Progress.Coins + " (expected " + expectedCoins + "), bag " + PowerUpInventory.GetCount(PowerUpKind.Hint) + "/" + PowerUpInventory.GetCount(PowerUpKind.Freeze) + "/" + PowerUpInventory.GetCount(PowerUpKind.Hammer));
            }
            else Log.AppendLine("  starter bundle is switched off in the shop config; skipped");

            // A coin pack from the shop pays its coins while gameplay is hidden.
            SetCoins(game, 100);
            bool granted = system.GrantShopCoins(50);
            Check(granted && game.Progress.Coins == 150, "  shop coin pack (+50) at the Shop: granted=" + granted + ", coins 100 -> " + game.Progress.Coins);

            // Now on a level for the use checks.
            if (!system.StartLevelNumber(6)) { Check(false, "  level 6 starts"); yield break; }
            yield return 6f;
            game.Flow.CancelReveal(true);
            game.Session.inputLocked = false;
            Log.AppendLine("  level 6 ready: inputLocked=" + game.Session.inputLocked + " canHint=" + system.CanRequestPowerUp(PowerUpKind.Hint) + " purchaseOpen=" + game.IsPowerUpPurchaseOpen + " tutorialOpen=" + game.IsPowerUpTutorialOpen);

            // Hint: free to use, one charge, nothing when the bag is empty.
            SetCoins(game, 500); SetBag(PowerUpKind.Hint, 0);
            bool used = system.TryUseHintPowerUp();
            Check(!used && PowerUpInventory.GetCount(PowerUpKind.Hint) == 0 && game.Progress.Coins == 500, "  Hint with an empty bag does nothing and costs no coins (used=" + used + ", coins " + game.Progress.Coins + ")");
            yield return 0.6f;
            Check(game.IsPowerUpPurchaseOpen, "  an empty Hint opens the purchase popup");
            int hintPrice = shop.GetOffer(PowerUpKind.Hint).coinPrice;
            var popupStatus = system.PurchasePowerUp(PowerUpKind.Hint);
            Check(popupStatus == PowerUpPurchaseStatus.Success && game.Progress.Coins == 500 - hintPrice && PowerUpInventory.GetCount(PowerUpKind.Hint) == shop.GetOffer(PowerUpKind.Hint).quantity,
                "  buying the Hint in the popup: " + popupStatus + ", coins 500 -> " + game.Progress.Coins + " (price " + hintPrice + "), bag " + PowerUpInventory.GetCount(PowerUpKind.Hint));
            // Closing the popup like a player (X) must give the board back.
            game.Hud?.CancelPowerUpPurchase(true);
            yield return 0.6f;
            game.Session.inputLocked = false;
            Check(!game.IsPowerUpPurchaseOpen, "  the purchase popup is closed again");
            SetCoins(game, 500);
            SetBag(PowerUpKind.Hint, 2);
            used = UseHint(system, game);
            Check(used && PowerUpInventory.GetCount(PowerUpKind.Hint) == 1 && game.Progress.Coins == 500, "  Hint with a full bag spends exactly one charge and no coins (bag " + PowerUpInventory.GetCount(PowerUpKind.Hint) + ", coins " + game.Progress.Coins + ")");

            // Freeze unlocks on level 13, so on level 6 it must refuse without taking the charge.
            SetBag(PowerUpKind.Freeze, 2);
            used = system.TryUseFreezePowerUp();
            Check(!used && PowerUpInventory.GetCount(PowerUpKind.Freeze) == 2 && game.Progress.Coins == 500, "  Freeze on a level before it unlocks is refused and keeps the charge (used=" + used + ", bag " + PowerUpInventory.GetCount(PowerUpKind.Freeze) + ", coins " + game.Progress.Coins + ")");
            Log.AppendLine("  note: using Freeze on an unlocked, timed level (13 and up) is not exercised by this audit");

            // Hammer with nothing in the way spends nothing.
            SetBag(PowerUpKind.Hammer, 1);
            bool hasTarget = system.HammerHasTarget;
            used = system.TryUseHammerPowerUp();
            Check(hasTarget || (!used && PowerUpInventory.GetCount(PowerUpKind.Hammer) == 1), "  Hammer with no target spends no charge (target=" + hasTarget + ", used=" + used + ", bag " + PowerUpInventory.GetCount(PowerUpKind.Hammer) + ")");

            // Undo: free uses first, then 20 coins each; refusals change nothing.
            UndoController undo = system.Undo;
            Check(undo != null, "  the Undo controller exists");
            if (undo != null)
            {
                SetCoins(game, 500);
                Check(undo.TryUndo() == UndoController.Result.NothingToUndo && game.Progress.Coins == 500, "  Undo with no move says so and costs nothing");
                int freeStart = undo.FreeLeft;
                for (int i = 0; i < freeStart; i++)
                {
                    PlaceOne(game);
                    var r = undo.TryUndo();
                    Check(r == UndoController.Result.Undone && game.Progress.Coins == 500, "  free Undo " + (i + 1) + "/" + freeStart + ": " + r + ", coins " + game.Progress.Coins);
                    yield return 0.4f;
                }
                PlaceOne(game);
                var paid = undo.TryUndo();
                Check(paid == UndoController.Result.Undone && game.Progress.Coins == 500 - UndoController.CoinPrice, "  Undo after the free ones costs " + UndoController.CoinPrice + ": " + paid + ", coins 500 -> " + game.Progress.Coins);
                yield return 0.4f;
                PlaceOne(game);
                SetCoins(game, UndoController.CoinPrice - 1);
                var broke = undo.TryUndo();
                Check(broke == UndoController.Result.NotEnoughCoins && game.Progress.Coins == UndoController.CoinPrice - 1, "  Undo one coin short is refused and the cat stays: " + broke + ", coins " + game.Progress.Coins);
            }

            // Coins that come from outside the level: an ad reward and a shop coin pack.
            SetCoins(game, 100);
            system.AwardCoins(50, false);
            Check(game.Progress.Coins == 150, "  external coin award (+50) lands: 100 -> " + game.Progress.Coins);
            Log.AppendLine("  note: the shop coin pack only pays while the gameplay screen is hidden (GrantShopCoins); not exercised here");
            system.GoHome();
        }

        private static IEnumerator PreviewWin(CatBlockPuzzleGame game, GameSystem system, int levelNumber)
        {
            Log.AppendLine("-- preview play (level test buttons), level " + levelNumber);
            system.GoHome();
            yield return 0.6f;
            system.PreviewLevel(levelNumber - 1);
            yield return 1.6f;
            game.Flow.CancelReveal(true);
            game.Session.inputLocked = false;
            int coinsBefore = game.Progress.Coins;
            PlaceAll(game);
            yield return 3.2f;
            Check(game.Progress.Coins == coinsBefore, "  preview wins pay nothing (coins " + coinsBefore + " -> " + game.Progress.Coins + ")");
        }

        // Presses Hint and, when it does nothing, says why (locked, level not ready, no target).
        private static bool UseHint(GameSystem system, CatBlockPuzzleGame game)
        {
            bool ok = system.TryUseHintPowerUp();
            if (!ok)
                Log.AppendLine("    hint diagnostics: canRequest=" + system.CanRequestPowerUp(PowerUpKind.Hint) + " bag=" + PowerUpInventory.GetCount(PowerUpKind.Hint)
                    + " inputLocked=" + game.Session.inputLocked + " levelFailed=" + game.Session.levelFailed + " purchaseOpen=" + game.IsPowerUpPurchaseOpen + " tutorialOpen=" + game.IsPowerUpTutorialOpen);
            return ok;
        }

        private static void PlaceAll(CatBlockPuzzleGame game)
        {
            PuzzleSession s = game.Session;
            foreach (PieceState p in s.pieces)
            {
                if (p.Placed || p.Definition.IsSpare || p.Definition.IsFixed) continue;
                int row = p.Solution.SolutionRow, col = p.Solution.SolutionCol;
                if (!game.Board.CanPlace(p, row, col)) { Log.AppendLine("  cannot place " + p.Definition.Name + " at " + row + "," + col); continue; }
                game.Drag.PlacePiece(p, row, col, true);
                s.inputLocked = false;
            }
        }

        private static void PlaceAllExceptLast(CatBlockPuzzleGame game)
        {
            PuzzleSession s = game.Session;
            PieceState last = null;
            foreach (PieceState p in s.pieces) if (!p.Placed && !p.Definition.IsSpare && !p.Definition.IsFixed) last = p;
            foreach (PieceState p in s.pieces)
            {
                if (p == last || p.Placed || p.Definition.IsSpare || p.Definition.IsFixed) continue;
                if (game.Board.CanPlace(p, p.Solution.SolutionRow, p.Solution.SolutionCol)) { game.Drag.PlacePiece(p, p.Solution.SolutionRow, p.Solution.SolutionCol, true); s.inputLocked = false; }
            }
        }
    }
}
#endif
