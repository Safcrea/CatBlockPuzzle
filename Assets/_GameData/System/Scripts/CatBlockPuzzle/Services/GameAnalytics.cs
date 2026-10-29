using System.Collections.Generic;
using AVNAnalytics;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The one place the game talks to the analytics SDK. Gameplay and UI code call the typed
    /// methods below; this class owns every event name and parameter key, adds the current
    /// level's context to in-level events, and forwards everything to <see cref="AVNGameAnalytics"/>.
    /// Level previews (the test buttons) are never reported.
    /// </summary>
    internal static class GameAnalytics
    {
        // level_start sources
        public const string SourceHomePlay = "home_play";
        public const string SourceLevelSelect = "level_select";
        public const string SourceNext = "next";
        public const string SourceRetry = "retry";
        public const string SourcePauseRestart = "pause_restart";
        public const string SourceHudReset = "hud_reset";
        public const string SourceSkip = "skip";
        private const string SourceOther = "other";

        // currency_earned / item_earned sources
        public const string EarnFirstClear = "first_clear";
        public const string EarnReplay = "replay";
        public const string EarnLoopLevel = "loop_level";
        public const string EarnAdDouble = "ad_double";
        public const string EarnDailyReward = "daily_reward";
        public const string EarnDailyPuzzle = "daily_puzzle";
        public const string EarnPurchase = "purchase";
        public const string EarnTutorialGrant = "tutorial_grant";

        private const string AttemptsKeyPrefix = "CatBlockPuzzle.Analytics.Attempts.";
        private const string TotalAttemptsKey = "CatBlockPuzzle.Analytics.TotalAttempts";

        private static string pendingLevelSource;
        private static bool loadingReported;
        private static string currentScreen;

        // The level attempt being reported; in-level events carry it.
        private static bool levelActive;
        private static int level, puzzle, chapter, difficulty, attempt, prevBestStars;
        private static bool isReplay, isLoop;

        private static float tutorialStartTime;
        private static float mechanicShownTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pendingLevelSource = null;
            loadingReported = false;
            currentScreen = null;
            levelActive = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => AVNGameAnalytics.Initialize();

        // ------------------------------------------------------------------ app and navigation

        /// <summary>The game scene is up and showing its first page.</summary>
        public static void LoadingComplete()
        {
            if (loadingReported) return;
            loadingReported = true;
            Send("loading_complete", new Dictionary<string, object>
            {
                ["load_ms"] = Mathf.RoundToInt(Time.realtimeSinceStartup * 1000f),
            });
        }

        public static void ScreenView(string screen)
        {
            if (screen == currentScreen) return;
            if (screen != "gameplay")
            {
                // Leaving gameplay ends the attempt's context, and a start that never happened loses its source.
                levelActive = false;
                pendingLevelSource = null;
            }
            Send("screen_view", new Dictionary<string, object>
            {
                ["screen"] = screen,
                ["previous_screen"] = currentScreen ?? "none",
            });
            currentScreen = screen;
        }

        public static void ComingSoonTap(string feature) =>
            Send("coming_soon_tap", new Dictionary<string, object> { ["feature"] = feature });

        public static void PauseOpened(float timeLeft)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["time_left_s"] = Round1(timeLeft);
            Send("pause_open", p);
        }

        public static void PauseAction(string action)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["action"] = action;
            Send("pause_action", p);
        }

        public static void SettingsChanged(string setting, bool value, bool fromPause) =>
            Send("settings_changed", new Dictionary<string, object>
            {
                ["setting"] = setting,
                ["value"] = value,
                ["from"] = fromPause ? "pause" : "home",
            });

        // ------------------------------------------------------------------ level loop

        /// <summary>Tags the next level_start with what the player pressed to get there.</summary>
        public static void SetLevelStartSource(string source) => pendingLevelSource = source;

        /// <summary>The source the next level_start will carry; read before LevelStarted consumes it.</summary>
        public static string PendingLevelStartSource => pendingLevelSource;

        public static void LevelStarted(LevelDefinition definition, int levelIndex, int levelNumber, bool loopLevel, bool replay,
            bool persistProgress, int bestStarsBefore, DifficultyProfile profile)
        {
            string source = pendingLevelSource ?? SourceOther;
            pendingLevelSource = null;
            levelActive = persistProgress && definition != null;
            if (!levelActive) return;

            level = levelNumber;
            puzzle = levelIndex;
            chapter = levelIndex / CatMetaCatalog.LevelsPerChapter + 1;
            difficulty = definition.Difficulty;
            isLoop = loopLevel;
            isReplay = replay;
            prevBestStars = bestStarsBefore;
            attempt = PlayerPrefs.GetInt(AttemptsKeyPrefix + levelNumber, 0) + 1;
            PlayerPrefs.SetInt(AttemptsKeyPrefix + levelNumber, attempt);
            PlayerPrefs.SetInt(TotalAttemptsKey, TotalAttempts + 1);

            var p = LevelParams();
            p["source"] = source;
            p["time_limit"] = Mathf.RoundToInt(profile.DurationSeconds);
            p["can_fail"] = profile.CanFail;
            p["hearts"] = definition.Hearts;
            p["pieces"] = definition.Pieces.Length;
            p["spares"] = definition.Spares.Length;
            p["mechanics"] = Mechanics(definition);
            p["coins"] = Coins;
            p["hints_owned"] = PowerUpInventory.GetCount(PowerUpKind.Hint);
            p["freezes_owned"] = PowerUpInventory.GetCount(PowerUpKind.Freeze);
            p["rotates_owned"] = PowerUpInventory.GetCount(PowerUpKind.Rotate);
            Send("level_start", p);
        }

        public static void LevelCompleted(LevelAttemptStats stats, float timeLeft, int stars, int bestStars, int coinsEarned, int bestCombo)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["duration_s"] = Round1(stats.playSeconds);
            p["time_left_s"] = Round1(timeLeft);
            p["stars"] = stars;
            p["best_stars"] = bestStars;
            p["prev_best_stars"] = prevBestStars;
            p["first_clear"] = !isReplay;
            p["coins_earned"] = coinsEarned;
            AddAttemptStats(p, stats, bestCombo);
            Send("level_complete", p);
        }

        public static void LevelFailed(string reason, LevelAttemptStats stats, float timeLeft, float cellsFilledPct, int bestCombo)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["reason"] = reason;
            p["cells_filled_pct"] = Mathf.RoundToInt(cellsFilledPct * 100f);
            p["time_played_s"] = Round1(stats.playSeconds);
            p["time_left_s"] = Round1(timeLeft);
            AddAttemptStats(p, stats, bestCombo);
            Send("level_fail", p);
        }

        public static void LevelSkipped(string from)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["from"] = from;
            p["best_stars"] = prevBestStars;
            Send("level_skip", p);
        }

        public static void ChapterUnlocked(int chapterIndex, int totalStars, int levelsCleared) =>
            Send("chapter_unlocked", new Dictionary<string, object>
            {
                ["chapter"] = chapterIndex + 1,
                ["total_stars"] = totalStars,
                ["levels_cleared"] = levelsCleared,
                ["total_attempts"] = TotalAttempts,
            });

        /// <summary>"Next" could not continue because the next chapter still needs stars.</summary>
        public static void ChapterGateBlocked(int chapterIndex, int starsHave, int starsNeeded) =>
            Send("chapter_gate_blocked", new Dictionary<string, object>
            {
                ["chapter"] = chapterIndex + 1,
                ["stars_have"] = starsHave,
                ["stars_needed"] = starsNeeded,
            });

        public static void EndlessEntered(int totalStars) =>
            Send("endless_entered", new Dictionary<string, object>
            {
                ["total_stars"] = totalStars,
                ["total_attempts"] = TotalAttempts,
            });

        // ------------------------------------------------------------------ onboarding

        public static void TutorialStarted()
        {
            if (!levelActive) return;
            tutorialStartTime = Time.realtimeSinceStartup;
            Send("tutorial_start", new Dictionary<string, object> { ["tutorial"] = "level_one" });
        }

        public static void TutorialStep(int step)
        {
            if (!levelActive) return;
            Send("tutorial_step", new Dictionary<string, object> { ["tutorial"] = "level_one", ["step"] = step });
        }

        public static void TutorialCompleted()
        {
            if (!levelActive) return;
            Send("tutorial_complete", new Dictionary<string, object>
            {
                ["tutorial"] = "level_one",
                ["duration_s"] = Round1(Time.realtimeSinceStartup - tutorialStartTime),
            });
        }

        public static void TutorialAborted(int step)
        {
            if (!levelActive) return;
            Send("tutorial_abort", new Dictionary<string, object>
            {
                ["tutorial"] = "level_one",
                ["step"] = step,
                ["duration_s"] = Round1(Time.realtimeSinceStartup - tutorialStartTime),
            });
        }

        public static void MechanicIntroShown(string mechanic)
        {
            mechanicShownTime = Time.realtimeSinceStartup;
            if (!levelActive) return;
            var p = LevelParams();
            p["mechanic"] = mechanic;
            Send("mechanic_intro_shown", p);
        }

        public static void MechanicIntroDismissed(string mechanic)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["mechanic"] = mechanic;
            p["view_s"] = Round1(Time.realtimeSinceStartup - mechanicShownTime);
            Send("mechanic_intro_dismissed", p);
        }

        public static void PowerUpTutorialShown(PowerUpKind kind)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["powerup"] = Name(kind);
            Send("powerup_tutorial_shown", p);
        }

        public static void PowerUpTutorialTapped(PowerUpKind kind)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["powerup"] = Name(kind);
            Send("powerup_tutorial_tapped", p);
        }

        public static void MoveTipShown()
        {
            if (!levelActive) return;
            Send("move_tip_shown", LevelParams());
        }

        // ------------------------------------------------------------------ power-ups

        public static void PowerUpUsed(PowerUpKind kind, int cost, float timeLeft, int piecesLeft, bool fromTutorial, bool hintFoundTarget)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["powerup"] = Name(kind);
            p["cost"] = cost;
            p["remaining"] = PowerUpInventory.GetCount(kind);
            p["time_left_s"] = Round1(timeLeft);
            p["pieces_left"] = piecesLeft;
            p["from_tutorial"] = fromTutorial;
            if (kind == PowerUpKind.Hint) p["hint_found_target"] = hintFoundTarget;
            Send("powerup_used", p);
        }

        public static void PowerUpOfferShown(PowerUpKind kind, int price, int quantity, int coins)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["powerup"] = Name(kind);
            p["price"] = price;
            p["qty"] = quantity;
            p["coins"] = coins;
            p["can_afford"] = coins >= price;
            p["shortfall"] = Mathf.Max(0, price - coins);
            Send("powerup_offer_shown", p);
        }

        public static void PowerUpPurchase(PowerUpKind kind, int price, int quantity, PowerUpPurchaseStatus status, int coinsAfter)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["powerup"] = Name(kind);
            p["price"] = price;
            p["qty"] = quantity;
            p["result"] = StatusName(status);
            p["coins_after"] = coinsAfter;
            Send("powerup_purchase", p);
        }

        public static void PowerUpOfferClosed(PowerUpKind kind, bool purchased, bool closedByPlayer)
        {
            if (!levelActive) return;
            var p = LevelParams();
            p["powerup"] = Name(kind);
            p["purchased"] = purchased;
            p["closed_by"] = closedByPlayer ? "player" : "game";
            Send("powerup_offer_closed", p);
        }

        // ------------------------------------------------------------------ economy

        public static void CurrencyEarned(int amount, string source, int balance)
        {
            if (amount <= 0) return;
            var p = levelActive ? LevelParams() : new Dictionary<string, object>();
            p["amount"] = amount;
            p["source"] = source;
            p["balance"] = balance;
            Send("currency_earned", p);
        }

        public static void CurrencySpent(int amount, PowerUpKind item, int balance)
        {
            if (amount <= 0) return;
            var p = levelActive ? LevelParams() : new Dictionary<string, object>();
            p["amount"] = amount;
            p["item"] = Name(item);
            p["balance"] = balance;
            Send("currency_spent", p);
        }

        public static void ItemEarned(PowerUpKind item, int amount, string source)
        {
            if (amount <= 0) return;
            var p = levelActive ? LevelParams() : new Dictionary<string, object>();
            p["item"] = Name(item);
            p["amount"] = amount;
            p["source"] = source;
            p["owned"] = PowerUpInventory.GetCount(item);
            Send("item_earned", p);
        }

        // ------------------------------------------------------------------ daily reward

        public static void DailyRewardShown(int availableDay, bool autoOpened) =>
            Send("daily_reward_shown", new Dictionary<string, object>
            {
                ["day"] = availableDay,
                ["auto_opened"] = autoOpened,
            });

        public static void DailyRewardClaimed(int day, int previousDay, int dayCount, int coins, int hints, int freezes) =>
            Send("daily_reward_claimed", new Dictionary<string, object>
            {
                ["day"] = day,
                ["previous_day"] = previousDay,
                // Back to day 1 before finishing the week means a day was missed.
                ["streak_reset"] = day == 1 && previousDay > 0 && previousDay < dayCount,
                ["coins"] = coins,
                ["hints"] = hints,
                ["freezes"] = freezes,
            });

        public static void DailyRewardDismissed(int availableDay) =>
            Send("daily_reward_dismissed", new Dictionary<string, object> { ["day"] = availableDay });

        // ------------------------------------------------------------------ ads and shop

        public static void RewardedAdRequested(string placement)
        {
            var p = levelActive ? LevelParams() : new Dictionary<string, object>();
            p["placement"] = placement;
            Send("ad_rewarded_request", p);
        }

        public static void RewardedAdResult(string placement, RewardedAds.Outcome outcome)
        {
            var p = levelActive ? LevelParams() : new Dictionary<string, object>();
            p["placement"] = placement;
            p["outcome"] = outcome.ToString().ToLowerInvariant();
            Send("ad_rewarded_result", p);
        }

        public static void ShopOfferTapped(int offerIndex, string productId, string priceLabel) =>
            Send("shop_offer_tapped", new Dictionary<string, object>
            {
                ["offer_index"] = offerIndex,
                ["product_id"] = string.IsNullOrEmpty(productId) ? "none" : productId,
                ["price_label"] = priceLabel ?? string.Empty,
                ["coins"] = Coins,
            });

        // ------------------------------------------------------------------ helpers

        private static int Coins => GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0;
        private static int TotalAttempts => PlayerPrefs.GetInt(TotalAttemptsKey, 0);

        private static Dictionary<string, object> LevelParams() => new Dictionary<string, object>
        {
            ["level"] = level,
            ["puzzle"] = puzzle,
            ["chapter"] = chapter,
            ["difficulty"] = difficulty,
            ["attempt"] = attempt,
            ["is_replay"] = isReplay,
            ["is_loop"] = isLoop,
        };

        private static void AddAttemptStats(Dictionary<string, object> p, LevelAttemptStats stats, int bestCombo)
        {
            p["placements"] = stats.placements;
            p["board_moves"] = stats.boardMoves;
            p["shelf_returns"] = stats.shelfReturns;
            p["misdrops"] = stats.misdrops;
            p["cancelled_drops"] = stats.cancelledDrops;
            p["lock_blocked_drops"] = stats.lockBlockedDrops;
            p["hearts_lost"] = stats.heartsLost;
            p["best_combo"] = bestCombo;
            p["hints_used"] = stats.hintsUsed;
            p["freeze_used"] = stats.freezesUsed > 0;
            p["first_move_s"] = Round1(stats.firstMoveSeconds);
            p["reached_low_time"] = stats.reachedLowTime;
        }

        internal static string Mechanics(LevelDefinition definition)
        {
            var names = new List<string>(6);
            foreach (CellLockRule rule in definition.LockedCells.Values)
                if (!rule.IsKey) { names.Add("locks"); break; }
            if (definition.HasKeyLocks) names.Add("keys");
            if (definition.HasTrayWindow) names.Add("shelf");
            if (definition.HasZones) names.Add("zones");
            if (definition.HasSpares) names.Add("spares");
            if (definition.HasHearts) names.Add("hearts");
            if (definition.HasFixedPieces) names.Add("fixed");
            if (definition.HasFog) names.Add("fog");
            if (definition.HasMoves) names.Add("moves");
            if (definition.HasBlockedCells) names.Add("blocked_tiles");
            return names.Count > 0 ? string.Join(",", names) : "none";
        }

        private static string Name(PowerUpKind kind) => kind == PowerUpKind.Hint ? "hint" : kind == PowerUpKind.Rotate ? "rotate" : "freeze";

        private static string StatusName(PowerUpPurchaseStatus status)
        {
            switch (status)
            {
                case PowerUpPurchaseStatus.Success: return "success";
                case PowerUpPurchaseStatus.InsufficientCoins: return "insufficient_coins";
                case PowerUpPurchaseStatus.InvalidOffer: return "invalid_offer";
                case PowerUpPurchaseStatus.InventoryFull: return "inventory_full";
                default: return "unavailable";
            }
        }

        private static double Round1(float value) => System.Math.Round(value, 1);

        private static void Send(string eventName, Dictionary<string, object> parameters)
        {
            AVNGameAnalytics.LogEvent(eventName, parameters);
            AVNEvents.AvnEvents.Supplement(eventName, parameters);
        }
    }
}
