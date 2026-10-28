using System.Collections.Generic;
using AVNAnalytics;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The game's single doorway to AVN Analytics. Gameplay code calls the small typed methods
    /// below instead of building event names and parameters inline, so the whole event list
    /// lives in one place. Every method is safe to call at any time: if analytics is off or
    /// not configured the SDK ignores the event.
    ///
    /// Automatic SDK events (first_open, session_start, session_end) are not repeated here.
    /// Level events carry level_number (what the player sees), level_id and attempt, so one
    /// attempt can be followed from level_start to level_complete / level_fail / level_quit.
    /// </summary>
    internal static class CatAnalytics
    {
        private const string AttemptsKeyPrefix = "CatBlockPuzzle.Analytics.Attempts.";

        /// <summary>One try at a level, from the moment it loads until it ends.</summary>
        private sealed class Attempt
        {
            public int levelNumber;
            public int levelIndex;
            public string levelId;
            public int difficulty;
            public int attempt;
            public float timeLimit;
            public int hearts;
            public int cells;
            public int pieces;
            public bool canFail;
            public bool isLoop;
            public float startedAt;
            public int invalidDrops;
            public int hintsUsed;
            public int freezesUsed;
            public int placements;
            public float tutorialStartedAt = -1f;
            public bool open;
        }

        private static Attempt current;
        private static int lastLevelNumber;
        private static string lastOutcome = "none";
        private static string lastScreen;
        private static float lastScreenAt;
        private static readonly Dictionary<string, float> menuOpenedAt = new Dictionary<string, float>();

        // ------------------------------------------------------------------ initialization

        /// <summary>
        /// Starts the SDK once per app launch, before the first scene loads, so every scene
        /// (loading, menu, gameplay) can log events. Endpoint and API key come from
        /// Resources/AVNGameAnalyticsConfig.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            current = null;
            lastLevelNumber = 0;
            lastOutcome = "none";
            lastScreen = null;
            menuOpenedAt.Clear();
            AVNGameAnalytics.Initialize();
        }

        // ------------------------------------------------------------------ levels

        /// <summary>Closes any attempt still open, then opens a new one. Previews (level browsing) are never tracked.</summary>
        public static void LevelStarted(LevelDefinition level, int levelIndex, int levelNumber, float timeLimit, bool canFail,
            bool isPreview, int coins)
        {
            if (level == null) return;
            if (isPreview)
            {
                current = null;
                return;
            }

            int attempt = PlayerPrefs.GetInt(AttemptsKeyPrefix + levelNumber, 0) + 1;
            PlayerPrefs.SetInt(AttemptsKeyPrefix + levelNumber, attempt);
            string previousOutcome = lastLevelNumber == levelNumber ? lastOutcome : "none";
            current = new Attempt
            {
                levelNumber = levelNumber,
                levelIndex = levelIndex,
                levelId = level.Id,
                difficulty = level.Difficulty,
                attempt = attempt,
                timeLimit = timeLimit,
                hearts = level.Hearts,
                cells = level.ActiveCells.Count,
                pieces = level.Pieces.Length,
                canFail = canFail,
                isLoop = levelNumber > levelIndex + 1,
                startedAt = Time.realtimeSinceStartup,
                open = true
            };
            lastLevelNumber = levelNumber;
            lastOutcome = "started";

            Dictionary<string, object> p = LevelParams(current);
            p["previous_outcome"] = previousOutcome;
            p["time_limit_seconds"] = Round(timeLimit);
            p["hearts"] = level.Hearts;
            p["cells"] = current.cells;
            p["pieces"] = current.pieces;
            p["can_fail"] = canFail;
            p["coins"] = coins;
            p["hints_in_stock"] = PowerUpInventory.GetCount(PowerUpKind.Hint);
            p["freezes_in_stock"] = PowerUpInventory.GetCount(PowerUpKind.Freeze);
            AVNGameAnalytics.LogEvent("level_start", p);
        }

        /// <param name="previousBestStars">Best stars for this level before this win (0 = never cleared).</param>
        public static void LevelCompleted(float timeRemaining, int stars, int previousBestStars, int coinsAwarded, int bestCombo)
        {
            Attempt a = current;
            if (a == null || !a.open) return;
            a.open = false;
            lastOutcome = "win";

            Dictionary<string, object> p = LevelParams(a);
            AddProgress(p, a, timeRemaining);
            p["stars"] = stars;
            p["first_clear"] = previousBestStars <= 0;
            p["coins_awarded"] = coinsAwarded;
            p["best_combo"] = bestCombo;
            AVNGameAnalytics.LogEvent("level_complete", p);
        }

        /// <param name="reason">timeout, out_of_hearts or other.</param>
        public static void LevelFailed(string reason, float timeRemaining, int cellsFilled, int bestCombo)
        {
            Attempt a = current;
            if (a == null || !a.open) return;
            a.open = false;
            lastOutcome = "fail";

            Dictionary<string, object> p = LevelParams(a);
            AddProgress(p, a, timeRemaining);
            AddFill(p, a, cellsFilled);
            p["reason"] = reason;
            p["best_combo"] = bestCombo;
            AVNGameAnalytics.LogEvent("level_fail", p);
        }

        /// <summary>The player left an unfinished level. reason: restart, level_switch or menu.</summary>
        public static void LevelQuit(string reason, float timeRemaining, int cellsFilled)
        {
            Attempt a = current;
            if (a == null || !a.open) return;
            a.open = false;
            lastOutcome = "quit";

            Dictionary<string, object> p = LevelParams(a);
            AddProgress(p, a, timeRemaining);
            AddFill(p, a, cellsFilled);
            p["reason"] = reason;
            AVNGameAnalytics.LogEvent("level_quit", p);
        }

        /// <summary>The skip button was used. state says what the player was looking at: in_progress, after_win or after_fail.</summary>
        public static void LevelSkipped(float timeRemaining, int cellsFilled)
        {
            Attempt a = current;
            if (a == null) return;
            string state = a.open ? "in_progress" : lastOutcome == "win" ? "after_win" : lastOutcome == "fail" ? "after_fail" : "other";
            Dictionary<string, object> p = LevelParams(a);
            AddProgress(p, a, timeRemaining);
            if (a.open) AddFill(p, a, cellsFilled);
            p["state"] = state;
            a.open = false;
            lastOutcome = "skip";
            AVNGameAnalytics.LogEvent("level_skip", p);
        }

        /// <summary>A cat was dropped on the board where it does not fit.</summary>
        /// <param name="heartsBefore">Hearts left before this drop, or -1 on levels without hearts.</param>
        public static void InvalidDrop(int heartsBefore)
        {
            Attempt a = current;
            if (a == null || !a.open) return;
            a.invalidDrops++;
            Dictionary<string, object> p = LevelParams(a);
            p["invalid_drops"] = a.invalidDrops;
            p["hearts_left"] = heartsBefore < 0 ? -1 : Mathf.Max(0, heartsBefore - 1);
            p["placements"] = a.placements;
            AVNGameAnalytics.LogEvent("invalid_drop", p);
        }

        /// <summary>Counts a landed cat. It only feeds the totals on the level events; it logs nothing itself.</summary>
        public static void PiecePlaced()
        {
            if (current != null && current.open) current.placements++;
        }

        // ------------------------------------------------------------------ tutorial and teaching

        public static void TutorialBegan()
        {
            if (current != null) current.tutorialStartedAt = Time.realtimeSinceStartup;
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            AVNGameAnalytics.LogEvent("tutorial_begin", p);
        }

        public static void TutorialCompleted()
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            if (current != null && current.tutorialStartedAt >= 0f)
                p["tutorial_seconds"] = Round(Time.realtimeSinceStartup - current.tutorialStartedAt);
            AVNGameAnalytics.LogEvent("tutorial_complete", p);
        }

        public static void MechanicIntroShown(string mechanic)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["mechanic"] = mechanic;
            AVNGameAnalytics.LogEvent("mechanic_intro_shown", p);
        }

        // ------------------------------------------------------------------ power-ups

        public static void PowerUpUsed(PowerUpKind kind, int stockAfter)
        {
            Attempt a = current;
            if (a == null || !a.open) return;
            if (kind == PowerUpKind.Hint) a.hintsUsed++;
            else a.freezesUsed++;
            Dictionary<string, object> p = LevelParams(a);
            p["kind"] = KindName(kind);
            p["stock_after"] = stockAfter;
            p["seconds_into_attempt"] = Round(Time.realtimeSinceStartup - a.startedAt);
            p["invalid_drops"] = a.invalidDrops;
            AVNGameAnalytics.LogEvent("powerup_used", p);
        }

        /// <summary>The "Out of Hints/Freezes" popup opened because the player tried to use one with none left.</summary>
        public static void PowerUpPromptShown(PowerUpKind kind, int coins, int price, int quantity)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["kind"] = KindName(kind);
            p["coins"] = coins;
            p["price"] = price;
            p["quantity"] = quantity;
            p["can_afford"] = coins >= price;
            AVNGameAnalytics.LogEvent("powerup_prompt_shown", p);
        }

        public static void PowerUpPromptClosed(PowerUpKind kind, bool purchased)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["kind"] = KindName(kind);
            p["purchased"] = purchased;
            AVNGameAnalytics.LogEvent("powerup_prompt_closed", p);
        }

        public static void PowerUpPurchased(PowerUpKind kind, int quantity, int price, int coinsAfter)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["kind"] = KindName(kind);
            p["quantity"] = quantity;
            p["price"] = price;
            p["coins_after"] = coinsAfter;
            AVNGameAnalytics.LogEvent("powerup_purchase", p);
            CoinsSpent(KindName(kind) + "_pack", price, coinsAfter);
        }

        // ------------------------------------------------------------------ economy

        /// <summary>Standard Firebase-style currency event, so coin sources can be compared.</summary>
        public static void CoinsEarned(string source, int amount, int balanceAfter)
        {
            if (amount <= 0) return;
            AVNGameAnalytics.LogEvent("earn_virtual_currency", new Dictionary<string, object>
            {
                { "virtual_currency_name", "coins" },
                { "value", amount },
                { "source", source },
                { "balance_after", balanceAfter }
            });
        }

        public static void CoinsSpent(string item, int amount, int balanceAfter)
        {
            if (amount <= 0) return;
            AVNGameAnalytics.LogEvent("spend_virtual_currency", new Dictionary<string, object>
            {
                { "virtual_currency_name", "coins" },
                { "value", amount },
                { "item_name", item },
                { "balance_after", balanceAfter }
            });
        }

        // ------------------------------------------------------------------ ads

        public static void RewardedAdRequested(string placement)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["placement"] = placement;
            AVNGameAnalytics.LogEvent("ad_rewarded_request", p);
        }

        /// <param name="outcome">rewarded, unavailable or cancelled.</param>
        public static void RewardedAdResult(string placement, string outcome, float secondsWaited)
        {
            AVNGameAnalytics.LogEvent("ad_rewarded_result", new Dictionary<string, object>
            {
                { "placement", placement },
                { "outcome", outcome },
                { "seconds", Round(secondsWaited) }
            });
        }

        public static void DoubleCoinsOffered(int coins)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["coins"] = coins;
            AVNGameAnalytics.LogEvent("double_coins_offered", p);
        }

        public static void DoubleCoinsClaimed(int coins)
        {
            Dictionary<string, object> p = current != null ? LevelParams(current) : new Dictionary<string, object>();
            p["coins"] = coins;
            AVNGameAnalytics.LogEvent("double_coins_claimed", p);
        }

        // ------------------------------------------------------------------ menus

        /// <summary>
        /// Logs the page the player is looking at; repeated calls for the same page are ignored.
        /// Carries the page they came from and how long they stayed there.
        /// </summary>
        public static void ScreenView(string screen)
        {
            if (screen == lastScreen) return;
            float now = Time.realtimeSinceStartup;
            var p = new Dictionary<string, object> { { "screen_name", screen } };
            if (lastScreen != null)
            {
                p["previous_screen"] = lastScreen;
                p["seconds_on_previous"] = Round(now - lastScreenAt);
            }
            lastScreen = screen;
            lastScreenAt = now;
            AVNGameAnalytics.LogEvent("screen_view", p);
        }

        public static void MenuOpened(string menu, bool fromPause)
        {
            menuOpenedAt[menu] = Time.realtimeSinceStartup;
            Dictionary<string, object> p = current != null && current.open ? LevelParams(current) : new Dictionary<string, object>();
            p["menu"] = menu;
            p["from_pause"] = fromPause;
            AVNGameAnalytics.LogEvent("menu_open", p);
        }

        /// <param name="action">What the player did to leave it: resume, restart, home, settings or close.</param>
        public static void MenuClosed(string menu, string action)
        {
            if (!menuOpenedAt.TryGetValue(menu, out float openedAt)) return;
            menuOpenedAt.Remove(menu);
            Dictionary<string, object> p = current != null && current.open ? LevelParams(current) : new Dictionary<string, object>();
            p["menu"] = menu;
            p["action"] = action;
            p["seconds_open"] = Round(Time.realtimeSinceStartup - openedAt);
            AVNGameAnalytics.LogEvent("menu_close", p);
        }

        /// <summary>How the player left the main menu: play, continue, levels, shop, daily_reward, collection, rooms, settings or quit.</summary>
        public static void HomeButtonTapped(string button)
        {
            AVNGameAnalytics.LogEvent("home_button_tap", new Dictionary<string, object> { { "button", button } });
        }

        public static void SettingChanged(string setting, bool enabled)
        {
            AVNGameAnalytics.LogEvent("setting_changed", new Dictionary<string, object>
            {
                { "setting", setting },
                { "enabled", enabled }
            });
        }

        public static void ComingSoonShown(string feature)
        {
            AVNGameAnalytics.LogEvent("coming_soon_shown", new Dictionary<string, object> { { "feature", feature } });
        }

        public static void ShopOfferTapped(string productId)
        {
            AVNGameAnalytics.LogEvent("shop_offer_tap", new Dictionary<string, object> { { "product_id", productId ?? "" } });
        }

        public static void DailyRewardOpened(bool automatic, bool claimable)
        {
            AVNGameAnalytics.LogEvent("daily_reward_open", new Dictionary<string, object>
            {
                { "automatic", automatic },
                { "claimable", claimable }
            });
        }

        public static void DailyRewardClaimed(int day, int coins, int hints, int freezes)
        {
            AVNGameAnalytics.LogEvent("daily_reward_claim", new Dictionary<string, object>
            {
                { "day", day },
                { "coins", coins },
                { "hints", hints },
                { "freezes", freezes }
            });
        }

        // ------------------------------------------------------------------ helpers

        private static Dictionary<string, object> LevelParams(Attempt a)
        {
            return new Dictionary<string, object>(24)
            {
                { "level_number", a.levelNumber },
                { "level_id", a.levelId },
                { "difficulty", a.difficulty },
                { "chapter", a.levelIndex / CatMetaCatalog.LevelsPerChapter + 1 },
                { "attempt", a.attempt },
                { "is_loop", a.isLoop }
            };
        }

        /// <summary>How the attempt went: play time (the level clock), wall time (includes pauses), and the player's actions.</summary>
        private static void AddProgress(Dictionary<string, object> p, Attempt a, float timeRemaining)
        {
            p["play_seconds"] = Round(Mathf.Max(0f, a.timeLimit - timeRemaining));
            p["wall_seconds"] = Round(Time.realtimeSinceStartup - a.startedAt);
            p["time_remaining"] = Round(Mathf.Max(0f, timeRemaining));
            p["time_limit_seconds"] = Round(a.timeLimit);
            p["invalid_drops"] = a.invalidDrops;
            p["hints_used"] = a.hintsUsed;
            p["freezes_used"] = a.freezesUsed;
            p["placements"] = a.placements;
        }

        /// <summary>How close the board was to done, so a fail at 90% reads differently from one at 20%.</summary>
        private static void AddFill(Dictionary<string, object> p, Attempt a, int cellsFilled)
        {
            p["cells_filled"] = cellsFilled;
            p["cells_total"] = a.cells;
            p["fill_percent"] = a.cells > 0 ? Mathf.RoundToInt(100f * cellsFilled / a.cells) : 0;
        }

        private static float Round(float value) => Mathf.Round(value * 10f) / 10f;

        private static string KindName(PowerUpKind kind) => kind == PowerUpKind.Hint ? "hint" : "freeze";
    }
}
