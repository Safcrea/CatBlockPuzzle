using System;
using System.Collections.Generic;
using System.Text;
using AVNAnalytics;
using AVNAnalytics.Events;
using CatBlockPuzzle;
using UnityEngine;

namespace AVNEvents
{
    // Closed enums from the AVN events standard (version 5). The C# name is the exact string sent.
    public enum LevelSource { Launch, Home, Next, Restart, Debug }
    public enum LeftReason { Home, Quit, Killed }
    public enum FailReason { OutOfTime, OutOfMoves, Other }
    public enum ChoiceAction { Next, DoubleReward, Restart, Revive, Home, Abandoned }
    public enum PayMethod { Ad, Coins, Free }
    public enum AdFormat { Interstitial, Rewarded, AppOpen }
    public enum AdFailReason { NotReady, ShowFailed, Timeout }
    public enum PowerupSource { Coins, RewardedAd, Purchase, Subscription, Gift, Other }
    public enum PowerupTrigger { Player, Tutorial }
    public enum ScreenClosedBy { Purchased, WatchedAd, Dismissed, SentToShop, System }

    // ECONOMY_ANALYSIS SOURCE is a lower_snake closed set; the member names are the strings sent.
    public enum EconomySource
    {
        level_reward, level_reward_bonus, rewarded_ad, purchase, subscription, gift,
        refund, powerup, revive, skip_level, unlock, other
    }

    // Game vocabulary (see avn-events-profile.json). Member names are the lower_snake strings sent.
    public enum AvnScreen
    {
        main_menu, shop, settings, daily_gift, level_selection, pause, powerup_popup,
        coming_soon, no_internet, collection, rooms
    }

    public enum ScreenAction
    {
        play, levels, shop, daily_gift, daily_puzzle, settings, collection, rooms, quit,
        resume, restart, home, buy, claim, later,
        music_on, music_off, sfx_on, sfx_off, haptics_on, haptics_off
    }

    public enum EconomyDetail
    {
        first_clear, replay, loop_level, daily_puzzle, level_complete_x2, daily_reward,
        hint, freeze, rotate, starter_bundle, shop_coin_pack
    }

    /// <summary>Everything LEVEL_ANALYSIS Started needs, read from the game where the level loads.</summary>
    public struct LevelStart
    {
        public int Level;
        public LevelSource Source;
        public string LevelId;
        /// <summary>Seconds on the level clock; null on untimed levels.</summary>
        public float? LevelTime;
        /// <summary>Mistake hearts the level starts with (the MOVES unit); null on levels without hearts.</summary>
        public int? MovesStart;
        // Game extras (listed in the profile's level_extras).
        public int Difficulty;
        public int Chapter;
        public int Pieces;
        public bool Replay;
        public bool Loop;
    }

    /// <summary>
    /// The game's only door to the AVN events standard. Every event name, key and enum string lives here;
    /// game code calls these typed methods and never AvnSender or AVNGameAnalytics directly.
    /// The legacy GameAnalytics events keep running beside these, unchanged.
    /// </summary>
    public static class AvnEvents
    {
        public const string Currency = "coins";
        public const string PlacementLevelCompleteX2 = "level_complete_x2";
        public const string TutorialFirstLevel = "first_level";

        /// <summary>The game's current 1-based level, or null (set by the gameplay controller).</summary>
        public static Func<int?> LevelProvider;

        /// <summary>The highest level the player has reached, or null until the save is loaded.</summary>
        public static Func<int?> HighestLevelProvider;

        static AvnScreen? _page;            // the menu page on screen; null while gameplay is up
        static bool _inGameplay;

        /// <summary>Gameplay is the screen, as the events see it (switched after the menu page it replaces is closed).</summary>
        public static bool InGameplay => _inGameplay;
        static bool _snapshotPending;
        static readonly HashSet<string> RunningTutorials = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _page = null;
            _inGameplay = false;
            _snapshotPending = false;
            RunningTutorials.Clear();

            AVNGameAnalytics.StandardVersion = 5;
            AvnSender.CurrentLevel = () => LevelProvider != null ? LevelProvider() : null;
            // No ad network is compiled in yet (USE_AVNADS_PLUGIN); NotReady is reported from the
            // network's own "no ad" callback instead of polling readiness.
            AvnAds.IsReady = null;

            AvnRuntime.Launched += AppLaunch;
            AvnRuntime.Backgrounded += AppBackground;
            AvnRuntime.Foregrounded += AppForeground;
            AvnRuntime.Quitting += AppQuitting;
            AvnRuntime.NewSession += PlayerSnapshotForSession;
            AvnAds.TimedOut += ad => SendAdFailed(ad, AdFailReason.Timeout);
        }

        // ------------------------------------------------------------------ LEVEL_ANALYSIS

        /// <summary>The board is playable. Closes anything a missed hook left open, then opens the attempt.</summary>
        public static void LevelStarted(LevelStart s, Func<int> progressPct, Func<float?> timeLeft, Func<int?> movesLeft)
        {
            CloseLeftovers(s.Source);
            AvnAttempt.ProgressProvider = progressPct;
            AvnAttempt.TimeLeftProvider = timeLeft;
            AvnAttempt.MovesLeftProvider = movesLeft;
            AvnAttemptInfo x = AvnAttempt.Begin(s.Level);

            var p = new Dictionary<string, object>
            {
                ["ATTEMPT_ID"] = x.Id,
                ["ATTEMPT"] = x.Attempt,
                ["LEVEL_NUMBER"] = x.Level,
                ["SOURCE"] = s.Source.ToString(),
                ["LEVEL_ID"] = s.LevelId,
                ["DIFFICULTY"] = s.Difficulty,
                ["CHAPTER"] = s.Chapter,
                ["PIECES"] = s.Pieces,
                ["IS_REPLAY"] = s.Replay ? 1 : 0,
                ["IS_LOOP"] = s.Loop ? 1 : 0,
            };
            if (s.LevelTime.HasValue) p["LEVEL_TIME"] = s.LevelTime.Value;
            if (s.MovesStart.HasValue) p["MOVES_START"] = s.MovesStart.Value;
            Send("LEVEL_ANALYSIS", "Started", p);
        }

        public static void LevelCompleted()
        {
            if (!AvnAttempt.InProgress) return;
            Send("LEVEL_ANALYSIS", "Completed", Result(AvnAttempt.EndWithResult()));
        }

        public static void LevelFailed(FailReason reason)
        {
            if (!AvnAttempt.InProgress) return;
            Dictionary<string, object> p = Result(AvnAttempt.EndWithResult());
            p["FAIL_REASON"] = reason.ToString();
            Send("LEVEL_ANALYSIS", "Failed", p);
        }

        /// <summary>The game's fail reason ids, mapped onto the standard's enum.</summary>
        public static FailReason FailReasonFor(string gameReason)
        {
            switch (gameReason)
            {
                case "timeout": return FailReason.OutOfTime;
                case "out_of_hearts": return FailReason.OutOfMoves;
                default: return FailReason.Other;
            }
        }

        /// <summary>Restart from inside the level (pause, HUD reset), or Retry on the fail screen.</summary>
        public static void LevelRestarting()
        {
            if (AvnAttempt.InProgress)
                Send("LEVEL_ANALYSIS", "Restarted", Progress(AvnAttempt.EndWithoutResult()));
            else
                LevelChoice(ChoiceAction.Restart);
        }

        /// <summary>
        /// The level is skipped. Mid-level that is the attempt's result; after a result the standard has no
        /// Skip action, so the move on to the next level is reported as Next.
        /// </summary>
        public static void LevelSkipping()
        {
            if (AvnAttempt.InProgress) Send("LEVEL_ANALYSIS", "Skipped", Result(AvnAttempt.EndWithResult()));
            LevelChoice(ChoiceAction.Next);
        }

        public static void LevelNext() => LevelChoice(ChoiceAction.Next);

        /// <summary>The player goes to the main menu: an open attempt is Left, a waiting result is answered Home.</summary>
        public static void LevelLeavingHome()
        {
            if (AvnAttempt.InProgress) SendLeft(LeftReason.Home);
            else LevelChoice(ChoiceAction.Home);
        }

        public static void LevelDoubleReward() => LevelChoice(ChoiceAction.DoubleReward, PayMethod.Ad);

        static void LevelChoice(ChoiceAction action, PayMethod? method = null)
        {
            if (!AvnAttempt.TakeChoice(out AvnAttemptInfo info)) return;
            if (action == ChoiceAction.Revive) AvnAttempt.Revive();
            var p = new Dictionary<string, object>
            {
                ["ATTEMPT_ID"] = info.Id,
                ["LEVEL_NUMBER"] = info.Level,
                ["ACTION"] = action.ToString(),
            };
            if (method.HasValue) p["METHOD"] = method.Value.ToString();
            Send("LEVEL_ANALYSIS", "Choice", p);
        }

        static void SendLeft(LeftReason reason)
        {
            Dictionary<string, object> p = Progress(AvnAttempt.EndWithoutResult());
            p["REASON"] = reason.ToString();
            Send("LEVEL_ANALYSIS", "Left", p);
        }

        // A new level starting while the last one is still open means a path the hooks did not see.
        static void CloseLeftovers(LevelSource next)
        {
            if (AvnAttempt.InProgress) SendLeft(LeftReason.Home);
            else LevelChoice(next == LevelSource.Next ? ChoiceAction.Next
                : next == LevelSource.Restart ? ChoiceAction.Restart : ChoiceAction.Home);
        }

        static Dictionary<string, object> Progress(AvnAttemptInfo x)
        {
            var p = new Dictionary<string, object>
            {
                ["ATTEMPT_ID"] = x.Id,
                ["ATTEMPT"] = x.Attempt,
                ["LEVEL_NUMBER"] = x.Level,
                ["SECONDS"] = x.Seconds,
                ["PROGRESS_PCT"] = x.ProgressPct,
            };
            if (x.TimeLeft.HasValue) p["TIME_LEFT"] = x.TimeLeft.Value;
            if (x.MovesLeft.HasValue) p["MOVES_LEFT"] = x.MovesLeft.Value;
            return p;
        }

        static Dictionary<string, object> Result(AvnAttemptInfo x)
        {
            Dictionary<string, object> p = Progress(x);
            p["REVIVES"] = x.Revives;
            p["POWERUPS_USED"] = x.PowerupsUsed;
            p["ADS_SHOWN"] = x.AdsShown;
            return p;
        }

        // ------------------------------------------------------------------ AD_ANALYSIS

        /// <summary>The game's legacy placement ids, mapped onto the standard's placement names.</summary>
        public static string PlacementFor(string gamePlacement) =>
            gamePlacement == LevelCompleteScreen.DoubleCoinsPlacement ? PlacementLevelCompleteX2 : LowerSnake(gamePlacement);

        /// <summary>Right before the game asks the network for an ad.</summary>
        public static void AdAsk(AdFormat format, string placement) =>
            AvnAds.Ask(format.ToString(), placement, AvnAttempt.InProgress ? AvnAttempt.Level : (int?)null,
                watchReady: format == AdFormat.Rewarded);

        /// <summary>Immediately before the provider's show call.</summary>
        public static void AdShow(AdFormat format, bool fallback = false)
        {
            AvnAd ad = AvnAds.Begin(format.ToString(), fallback);
            AvnAttempt.NoteAdShown();
            Send("AD_ANALYSIS", "Show", Ad(ad));
        }

        public static void AdClosed(AdFormat format, bool rewarded)
        {
            AvnAd ad = AvnAds.Find(format.ToString());
            if (!AvnAds.End(ad)) return;
            ad.Rewarded = rewarded;
            Dictionary<string, object> p = Ad(ad);
            p["SECONDS_ON_SCREEN"] = (float)(DateTime.UtcNow - ad.OpenedAtUtc).TotalSeconds;
            if (format == AdFormat.Rewarded || ad.Fallback) p["REWARDED"] = rewarded ? 1 : 0;
            Send("AD_ANALYSIS", "Closed", p);
        }

        /// <summary>No ad was shown when the game wanted one (no ad loaded, or the show call failed).</summary>
        public static void AdFailed(AdFormat format, AdFailReason reason)
        {
            AvnAd ad = AvnAds.Find(format.ToString());
            if (ad != null)
            {
                if (AvnAds.End(ad)) SendAdFailed(ad, reason);
                return;
            }

            // Failed before any Show: give the attempt its own AD_ID from the waiting request.
            string f = format.ToString();
            var pending = new AvnAd
            {
                Id = Guid.NewGuid().ToString("N"),
                Format = f,
                Placement = AvnAds.PendingPlacement(f),
                Level = AvnAds.PendingLevel(f),
            };
            AvnAds.ForgetRequest(f);
            SendAdFailed(pending, reason);
        }

        static void SendAdFailed(AvnAd ad, AdFailReason reason)
        {
            Dictionary<string, object> p = Ad(ad);
            p["REASON"] = reason.ToString();
            Send("AD_ANALYSIS", "Failed", p);
        }

        static Dictionary<string, object> Ad(AvnAd x)
        {
            var p = new Dictionary<string, object>
            {
                ["AD_ID"] = x.Id,
                ["FORMAT"] = x.Format,
                ["PLACEMENT"] = x.Placement,
            };
            if (x.Fallback) p["FALLBACK"] = 1;
            if (x.Level.HasValue) p["LEVEL_NUMBER"] = x.Level.Value;
            return p;
        }

        // ------------------------------------------------------------------ ECONOMY_ANALYSIS

        public static void EconomyEarned(int amount, int balance, EconomySource source, EconomyDetail? detail = null) =>
            SendEconomy("Earned", amount, balance, source, detail);

        public static void EconomySpent(int amount, int balance, EconomySource source, EconomyDetail? detail = null) =>
            SendEconomy("Spent", amount, balance, source, detail);

        /// <summary>The level reward's legacy source id, mapped onto a DETAIL.</summary>
        public static EconomyDetail LevelRewardDetail(string gameSource) =>
            gameSource == GameAnalytics.EarnLoopLevel ? EconomyDetail.loop_level
            : gameSource == GameAnalytics.EarnReplay ? EconomyDetail.replay
            : EconomyDetail.first_clear;

        static void SendEconomy(string state, int amount, int balance, EconomySource source, EconomyDetail? detail)
        {
            if (amount <= 0) return;
            var p = new Dictionary<string, object>
            {
                ["CURRENCY"] = Currency,
                ["AMOUNT"] = amount,
                ["BALANCE"] = balance,
                ["SOURCE"] = source.ToString(),
            };
            if (detail.HasValue) p["DETAIL"] = detail.Value.ToString();
            Send("ECONOMY_ANALYSIS", state, p);
        }

        public static EconomyDetail DetailFor(PowerUpKind kind) =>
            kind == PowerUpKind.Hint ? EconomyDetail.hint : kind == PowerUpKind.Rotate ? EconomyDetail.rotate : EconomyDetail.freeze;

        // ------------------------------------------------------------------ POWERUP_ANALYSIS

        public static void PowerupObtained(PowerUpKind kind, int amount, PowerupSource source)
        {
            if (amount <= 0) return;
            Send("POWERUP_ANALYSIS", "Obtained", new Dictionary<string, object>
            {
                ["POWERUP"] = PowerupName(kind),
                ["AMOUNT"] = amount,
                ["SOURCE"] = source.ToString(),
            });
        }

        public static void PowerupUsed(PowerUpKind kind, bool fromTutorial)
        {
            AvnAttempt.NotePowerupUsed();
            var p = new Dictionary<string, object>
            {
                ["POWERUP"] = PowerupName(kind),
                ["TRIGGER"] = (fromTutorial ? PowerupTrigger.Tutorial : PowerupTrigger.Player).ToString(),
            };
            if (AvnAttempt.InProgress) p["SECONDS_INTO_LEVEL"] = AvnAttempt.Seconds;
            Send("POWERUP_ANALYSIS", "Used", p);
        }

        public static string PowerupName(PowerUpKind kind) =>
            kind == PowerUpKind.Hint ? "hint" : kind == PowerUpKind.Rotate ? "rotate" : "freeze";

        // ------------------------------------------------------------------ TUTORIAL_ANALYSIS

        public static string PowerupTutorial(PowerUpKind kind) => "powerup_" + PowerupName(kind);
        public static string FeatureTutorial(string mechanic) => "feature_" + LowerSnake(mechanic);

        public static void TutorialShown(string tutorial, int? step = null)
        {
            RunningTutorials.Add(tutorial);
            AvnMemory.StartTimer(TimerKey(tutorial));
            Send("TUTORIAL_ANALYSIS", "Shown", Tutorial(tutorial, step));
        }

        public static void TutorialCompleted(string tutorial, int? step = null) => EndTutorial(tutorial, "Completed", step);

        public static void TutorialInterrupted(string tutorial, int? step = null) => EndTutorial(tutorial, "Interrupted", step);

        /// <summary>Interrupts every running tutorial whose name starts with the prefix (a level change, navigation).</summary>
        public static void TutorialsInterrupted(string prefix)
        {
            var running = new List<string>(RunningTutorials);
            foreach (string tutorial in running)
                if (tutorial.StartsWith(prefix, StringComparison.Ordinal)) TutorialInterrupted(tutorial);
        }

        static void EndTutorial(string tutorial, string state, int? step)
        {
            // StopTimer is false when this run already ended: one terminal state per run.
            if (!AvnMemory.StopTimer(TimerKey(tutorial), out float seconds)) return;
            RunningTutorials.Remove(tutorial);
            Dictionary<string, object> p = Tutorial(tutorial, step);
            p["SECONDS"] = seconds;
            Send("TUTORIAL_ANALYSIS", state, p);
        }

        static Dictionary<string, object> Tutorial(string tutorial, int? step)
        {
            var p = new Dictionary<string, object> { ["TUTORIAL"] = tutorial };
            if (step.HasValue) p["STEP"] = step.Value;
            return p;
        }

        static string TimerKey(string tutorial) => "tutorial." + tutorial;

        // ------------------------------------------------------------------ SCREEN_ANALYSIS

        /// <summary>
        /// A full menu page replaces the last one (main menu, shop, daily gift, level selection, ...). The page
        /// it replaces is Closed. Gameplay is not a SCREEN_ANALYSIS screen, so it only closes the page before it.
        /// </summary>
        public static void PageOpened(AvnScreen page, bool auto = false)
        {
            if (!_inGameplay && _page == page) return;
            string from = ClosePage();
            _inGameplay = false;
            _page = page;
            AvnMemory.StartTimer(ScreenTimer(page));
            SendScreenOpened(page, auto ? "auto" : from);
        }

        public static void GameplayOpened()
        {
            if (_inGameplay) return;
            ClosePage();
            _inGameplay = true;
            _page = null;
        }

        /// <summary>A popup over the current page or gameplay. Closed is sent only where the standard requires it.</summary>
        public static void PopupOpened(AvnScreen popup)
        {
            AvnMemory.StartTimer(ScreenTimer(popup));
            SendScreenOpened(popup, CurrentScreenName());
        }

        public static void PopupClosed(AvnScreen popup, ScreenClosedBy? closedBy = null) => SendScreenClosed(popup, closedBy);

        public static void ScreenInteraction(AvnScreen screen, ScreenAction action, string item = null)
        {
            var p = new Dictionary<string, object>
            {
                ["SCREEN"] = screen.ToString(),
                ["ACTION"] = action.ToString(),
            };
            if (!string.IsNullOrEmpty(item)) p["ITEM"] = item;
            Send("SCREEN_ANALYSIS", "Interaction", p);
        }

        public static ScreenAction Toggle(string setting, bool on)
        {
            switch (setting)
            {
                case "music": return on ? ScreenAction.music_on : ScreenAction.music_off;
                case "sfx": return on ? ScreenAction.sfx_on : ScreenAction.sfx_off;
                default: return on ? ScreenAction.haptics_on : ScreenAction.haptics_off;
            }
        }

        static string ClosePage()
        {
            if (_inGameplay) return "gameplay";
            if (!_page.HasValue) return null;
            AvnScreen closing = _page.Value;
            _page = null;
            SendScreenClosed(closing, null);
            return closing.ToString();
        }

        static void SendScreenOpened(AvnScreen screen, string source)
        {
            var p = new Dictionary<string, object> { ["SCREEN"] = screen.ToString() };
            if (!string.IsNullOrEmpty(source)) p["SOURCE"] = source;
            Send("SCREEN_ANALYSIS", "Opened", p);
        }

        static void SendScreenClosed(AvnScreen screen, ScreenClosedBy? closedBy)
        {
            if (!AvnMemory.StopTimer(ScreenTimer(screen), out float seconds)) return;
            var p = new Dictionary<string, object>
            {
                ["SCREEN"] = screen.ToString(),
                ["SECONDS_OPEN"] = seconds,
            };
            if (closedBy.HasValue) p["CLOSED_BY"] = closedBy.Value.ToString();
            Send("SCREEN_ANALYSIS", "Closed", p);
        }

        static string CurrentScreenName() => _inGameplay ? "gameplay" : _page.HasValue ? _page.Value.ToString() : null;

        static string ScreenTimer(AvnScreen screen) => "screen." + screen;

        // ------------------------------------------------------------------ APP_ANALYSIS

        static void AppLaunch(AvnExit? previousExit)
        {
            var p = new Dictionary<string, object>();
            if (previousExit.HasValue) p["PREV_EXIT"] = previousExit.Value.ToString();
            Send("APP_ANALYSIS", "Launch", p);

            // The previous run died inside an attempt, or before answering a result.
            if (AvnAttempt.TakeRecovered(out AvnAttemptInfo info, out bool hadResult))
            {
                if (hadResult)
                {
                    Send("LEVEL_ANALYSIS", "Choice", new Dictionary<string, object>
                    {
                        ["ATTEMPT_ID"] = info.Id,
                        ["LEVEL_NUMBER"] = info.Level,
                        ["ACTION"] = ChoiceAction.Abandoned.ToString(),
                    });
                }
                else
                {
                    Dictionary<string, object> left = Progress(info);
                    left["REASON"] = LeftReason.Killed.ToString();
                    left["RECOVERED"] = 1;
                    Send("LEVEL_ANALYSIS", "Left", left);
                }
            }

            SendDeviceSnapshotIfChanged();
        }

        static void AppBackground(AvnCause cause)
        {
            var p = new Dictionary<string, object>
            {
                ["CAUSE"] = cause.ToString(),
                ["IN_LEVEL"] = AvnAttempt.InProgress ? 1 : 0,
            };
            if (!_inGameplay && _page.HasValue) p["SCREEN"] = _page.Value.ToString();
            Send("APP_ANALYSIS", "Background", p);
        }

        static void AppForeground(AvnCause cause, float awaySeconds) =>
            Send("APP_ANALYSIS", "Foreground", new Dictionary<string, object>
            {
                ["CAUSE"] = cause.ToString(),
                ["AWAY_SECONDS"] = awaySeconds,
            });

        static void AppQuitting()
        {
            if (AvnAttempt.InProgress) SendLeft(LeftReason.Quit);
            else LevelChoice(ChoiceAction.Abandoned);
        }

        // ------------------------------------------------------------------ PLAYER_ANALYSIS

        /// <summary>The gameplay controller is up, so the save is loaded: send a snapshot a new session is waiting for.</summary>
        public static void SaveLoaded()
        {
            if (_snapshotPending) PlayerSnapshot();
        }

        static void PlayerSnapshotForSession()
        {
            if (HighestLevelProvider != null && HighestLevelProvider() != null) PlayerSnapshot();
            else _snapshotPending = true;
        }

        static void PlayerSnapshot()
        {
            int? highest = HighestLevelProvider != null ? HighestLevelProvider() : null;
            if (!highest.HasValue) { _snapshotPending = true; return; }
            _snapshotPending = false;
            Send("PLAYER_ANALYSIS", "Snapshot", new Dictionary<string, object>
            {
                ["HIGHEST_LEVEL"] = highest.Value,
                ["BALANCE_COINS"] = PlayerPrefs.GetInt(PuzzleSaveKeys.SavedCoins, 0),
                ["POWERUP_HINT"] = PowerUpInventory.GetCount(PowerUpKind.Hint),
                ["POWERUP_FREEZE"] = PowerUpInventory.GetCount(PowerUpKind.Freeze),
                ["POWERUP_ROTATE"] = PowerUpInventory.GetCount(PowerUpKind.Rotate),
            });
        }

        // ------------------------------------------------------------------ DEVICE_ANALYSIS

        // Once per install, and again when the app version or quality level changes.
        static void SendDeviceSnapshotIfChanged()
        {
            int quality = QualitySettings.GetQualityLevel();
            string signature = Application.version + "|" + quality;
            if (AvnMemory.GetText("device") == signature) return;
            AvnMemory.SetText("device", signature);

            var p = new Dictionary<string, object>
            {
                ["RAM_MB"] = SystemInfo.systemMemorySize,
                ["CPU_CORES"] = SystemInfo.processorCount,
                ["QUALITY_LEVEL"] = quality,
            };
            if (SystemInfo.graphicsMemorySize > 0) p["GPU_MB"] = SystemInfo.graphicsMemorySize;
            Send("DEVICE_ANALYSIS", "Snapshot", p);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>The standard's lower_snake rule: split CamelCase and digits, lowercase, join with _.</summary>
        public static string LowerSnake(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            var sb = new StringBuilder(id.Length + 4);
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (c == ' ' || c == '-' || c == '_')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
                    continue;
                }
                bool boundary = i > 0 && sb.Length > 0 && sb[sb.Length - 1] != '_' &&
                    ((char.IsUpper(c) && (char.IsLower(id[i - 1]) || char.IsDigit(id[i - 1]))) ||
                     (char.IsDigit(c) && char.IsLetter(id[i - 1])));
                if (boundary) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString().Trim('_');
        }

        // The only exit. While an attempt is open AvnSender stamps its ATTEMPT_ID on every event.
        static void Send(string name, string state, Dictionary<string, object> p)
        {
            if (AvnAttempt.InProgress && !p.ContainsKey("ATTEMPT_ID")) p["ATTEMPT_ID"] = AvnAttempt.Current().Id;
            AvnSender.Send(name, state, p);
        }
    }
}
