using System;
using System.Collections.Generic;
using AVNAnalytics.Events;
using CatBlockPuzzle;

namespace AVNEvents
{
    // Extensions keep new game-specific states out of the closed standard v5 enums.
    public static partial class AvnEvents
    {
        public static Func<Dictionary<string, object>> LevelMetrics;
        private static readonly HashSet<AvnScreen> OpenPopups = new HashSet<AvnScreen>();
        private static void ResetGameCoverage() { LevelMetrics = null; OpenPopups.Clear(); }
        private static string LevelEvent(AvnAttemptInfo info) => info.Mode == "daily_puzzle" ? "DAILY_PUZZLE_ANALYSIS" : "LEVEL_ANALYSIS";

        public static void Navigation(string action, string screen, int? targetLevel = null, int? page = null) =>
            Send("NAVIGATION_ANALYSIS", "Interaction", new Dictionary<string, object>
            { ["ACTION"] = action, ["SCREEN"] = screen, ["TARGET_LEVEL"] = targetLevel, ["PAGE"] = page });

        public static void BoardAction(string action)
        {
            if (!AvnAttempt.InProgress) return;
            Send("GAMEPLAY_ANALYSIS", "BoardAction", new Dictionary<string, object>
            { ["ACTION"] = action, ["SECONDS_INTO_LEVEL"] = AvnAttempt.Seconds });
        }

        public static void HintOutcome(bool found, bool free)
        {
            if (!AvnAttempt.InProgress) return;
            Send("GAMEPLAY_ANALYSIS", "Hint", new Dictionary<string, object>
            { ["FOUND_TARGET"] = found ? 1 : 0, ["SOURCE"] = free ? "fail_streak" : "inventory", ["COST"] = free ? 0 : (int?)null });
        }

        public static void PurchaseOutcome(string item, string source, PowerUpPurchaseStatus status, int price, int quantity, int balance) =>
            Send("PURCHASE_ANALYSIS", "Result", new Dictionary<string, object>
            {
                ["ITEM"] = item, ["SOURCE"] = source, ["RESULT"] = LowerSnake(status.ToString()),
                ["PRICE_COINS"] = price, ["QUANTITY"] = quantity, ["BALANCE_COINS"] = balance,
                ["SHORTFALL"] = status == PowerUpPurchaseStatus.InsufficientCoins ? Math.Max(0, price - balance) : 0,
            });

        public static void AdRequest(string requestId, string placement, string state, string outcome = null, bool simulated = false) =>
            Send("AD_REQUEST_ANALYSIS", state, new Dictionary<string, object>
            { ["REQUEST_ID"] = requestId, ["PLACEMENT"] = PlacementFor(placement), ["OUTCOME"] = outcome, ["SIMULATED"] = simulated ? 1 : 0 });

        // Existing custom hooks are also exposed in the new attempt-linked families.
        internal static void Supplement(string name, Dictionary<string, object> payload)
        {
            string family;
            string state;
            switch (name)
            {
                case "chapter_unlocked": family = "PROGRESSION_ANALYSIS"; state = "ChapterUnlocked"; break;
                case "chapter_gate_blocked": family = "PROGRESSION_ANALYSIS"; state = "ChapterGateBlocked"; break;
                case "endless_entered": family = "PROGRESSION_ANALYSIS"; state = "EndlessEntered"; break;
                case "tutorial_step": family = "TUTORIAL_PROGRESS_ANALYSIS"; state = "Step"; break;
                case "move_tip_shown": family = "GAMEPLAY_ANALYSIS"; state = "MoveTipShown"; break;
                default: return;
            }
            var p = new Dictionary<string, object>();
            foreach (var pair in payload) p[pair.Key.ToUpperInvariant()] = pair.Value;
            Send(family, state, p);
        }
    }
}
