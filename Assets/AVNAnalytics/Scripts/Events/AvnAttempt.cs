using System;
using UnityEngine;

namespace AVNAnalytics.Events
{
    /// <summary>The values of the spec's progress and result sets for one attempt.</summary>
    public struct AvnAttemptInfo
    {
        public string Id;
        public int Attempt;
        public int Level;
        public float Seconds;
        public int ProgressPct;
        public float? TimeLeft;
        public int? MovesLeft;
        public int Revives;
        public int PowerupsUsed;
        public int AdsShown;
    }

    /// <summary>
    /// The current level attempt: its id, attempt number, active play seconds (not background, not
    /// fullscreen ads), revives, powerups and ads. Saved only at moments that matter (start, result,
    /// background, ad) so a force close can be reported on the next launch. Sends nothing itself.
    /// </summary>
    public static class AvnAttempt
    {
        const string SavedKey = "AVN.Attempt.Saved";
        const string CountKey = "AVN.Attempt.Count.";
        const string PhasePlay = "Play";
        const string PhaseResult = "Result";

        [Serializable]
        sealed class Saved
        {
            public string Id;
            public int Attempt;
            public int Level;
            public float Seconds;
            public int Pct;
            public int Revives;
            public int Powerups;
            public int Ads;
            public string Phase;
            // -1 is "the game does not have this", so an untimed level stays untimed after a recovery.
            public float TimeLeft = -1f;
            public int MovesLeft = -1;
        }

        /// <summary>The game's board progress, 0-100. Called when an event needs it, never per frame.</summary>
        public static Func<int> ProgressProvider;

        /// <summary>Seconds left on the level's clock, or null when the level is untimed.</summary>
        public static Func<float?> TimeLeftProvider;

        /// <summary>The game's countable unit still left, or null when the game has none.</summary>
        public static Func<int?> MovesLeftProvider;

        static string _id;
        static int _attempt;
        static int _level;
        static float _seconds;
        static int _revives;
        static int _powerups;
        static int _ads;
        static int _lastPct;
        static float? _lastTimeLeft;
        static int? _lastMovesLeft;
        static Saved _recovered;

        /// <summary>Between Started (or a revive) and a result or Left.</summary>
        public static bool InProgress { get; private set; }

        /// <summary>A result was sent and the player has not chosen yet.</summary>
        public static bool ResultPending { get; private set; }

        public static int Level => _level;

        /// <summary>The open attempt's id, or null when none is open. Every event sent during an attempt carries it.</summary>
        public static string Id => InProgress || ResultPending ? _id : null;

        /// <summary>Active seconds on the current attempt so far.</summary>
        public static float Seconds => _seconds;

        public static AvnAttemptInfo Begin(int level)
        {
            string key = CountKey + level;
            int attempt = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, attempt);

            _id = Guid.NewGuid().ToString("N");
            _attempt = attempt;
            _level = level;
            _seconds = 0f;
            _revives = 0;
            _powerups = 0;
            _ads = 0;
            _lastPct = 0;
            _lastTimeLeft = null;
            _lastMovesLeft = null;
            InProgress = true;
            ResultPending = false;
            Persist(PhasePlay);
            return Info();
        }

        public static AvnAttemptInfo Current() => Info();

        /// <summary>The attempt produced a result (win, loss, skip). Stops the clock and waits for a choice.</summary>
        public static AvnAttemptInfo EndWithResult()
        {
            AvnAttemptInfo info = Info();
            InProgress = false;
            ResultPending = true;
            Persist(PhaseResult);
            return info;
        }

        /// <summary>The attempt ends with no result (Left, Restarted).</summary>
        public static AvnAttemptInfo EndWithoutResult()
        {
            AvnAttemptInfo info = Info();
            InProgress = false;
            ResultPending = false;
            Clear();
            return info;
        }

        /// <summary>One Choice per result: false when none is waiting (a double tap, a button wired twice).</summary>
        public static bool TakeChoice(out AvnAttemptInfo info)
        {
            info = Info();
            if (!ResultPending) return false;
            ResultPending = false;
            Clear();
            return true;
        }

        /// <summary>A Revive choice: the same attempt, same id, back in play.</summary>
        public static void Revive()
        {
            _revives++;
            InProgress = true;
            ResultPending = false;
            Persist(PhasePlay);
        }

        public static void NotePowerupUsed() { if (InProgress) _powerups++; }
        public static void NoteAdShown() { if (InProgress) _ads++; }

        /// <summary>Saves the current values; called when the app is paused or an ad opens.</summary>
        public static void Checkpoint()
        {
            if (InProgress) Persist(PhasePlay);
        }

        /// <summary>
        /// What the previous run left behind: an attempt in play (report Left/Killed) or a result nobody
        /// answered (report Choice/Abandoned). Returns it once.
        /// </summary>
        public static bool TakeRecovered(out AvnAttemptInfo info, out bool hadResult)
        {
            info = default;
            hadResult = false;
            Saved saved = _recovered;
            _recovered = null;
            if (saved == null || saved.Level < 1 || string.IsNullOrEmpty(saved.Id)) return false;

            info = new AvnAttemptInfo
            {
                Id = saved.Id, Attempt = saved.Attempt, Level = saved.Level, Seconds = saved.Seconds,
                ProgressPct = saved.Pct, Revives = saved.Revives, PowerupsUsed = saved.Powerups, AdsShown = saved.Ads,
                TimeLeft = saved.TimeLeft >= 0f ? saved.TimeLeft : (float?)null,
                MovesLeft = saved.MovesLeft >= 0 ? saved.MovesLeft : (int?)null,
            };
            hadResult = saved.Phase == PhaseResult;
            return true;
        }

        internal static void Tick(float deltaTime)
        {
            if (!InProgress || AvnRuntime.InBackground || AvnAds.AdActive) return;
            _seconds += deltaTime;
        }

        static AvnAttemptInfo Info()
        {
            // Read the board only while it is still there: after a result the last reading stands,
            // which is what a revive, a recovered attempt and a second result all report.
            if (InProgress)
            {
                _lastPct = Pct();
                _lastTimeLeft = Ask(TimeLeftProvider, _lastTimeLeft);
                _lastMovesLeft = Ask(MovesLeftProvider, _lastMovesLeft);
            }
            return new AvnAttemptInfo
            {
                Id = _id, Attempt = _attempt, Level = _level, Seconds = _seconds, ProgressPct = _lastPct,
                TimeLeft = _lastTimeLeft, MovesLeft = _lastMovesLeft,
                Revives = _revives, PowerupsUsed = _powerups, AdsShown = _ads,
            };
        }

        static int Pct()
        {
            try { return ProgressProvider != null ? Mathf.Clamp(ProgressProvider(), 0, 100) : _lastPct; }
            catch (Exception) { return _lastPct; }
        }

        static T? Ask<T>(Func<T?> provider, T? last) where T : struct
        {
            try { return provider != null ? provider() : last; }
            catch (Exception) { return last; }
        }

        static void Persist(string phase)
        {
            AvnAttemptInfo x = Info();
            var saved = new Saved
            {
                Id = _id, Attempt = _attempt, Level = _level, Seconds = _seconds, Pct = x.ProgressPct,
                Revives = _revives, Powerups = _powerups, Ads = _ads, Phase = phase,
                TimeLeft = x.TimeLeft ?? -1f, MovesLeft = x.MovesLeft ?? -1,
            };
            PlayerPrefs.SetString(SavedKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
        }

        static void Clear()
        {
            PlayerPrefs.DeleteKey(SavedKey);
            PlayerPrefs.Save();
        }

        // Captured before the game starts a level, so a new attempt cannot hide what the last run left.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ProgressProvider = null;
            TimeLeftProvider = null;
            MovesLeftProvider = null;
            _id = null;
            _attempt = _level = _revives = _powerups = _ads = _lastPct = 0;
            _lastTimeLeft = null;
            _lastMovesLeft = null;
            _seconds = 0f;
            InProgress = false;
            ResultPending = false;
            _recovered = null;

            string json = PlayerPrefs.GetString(SavedKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            try { _recovered = JsonUtility.FromJson<Saved>(json); }
            catch (Exception) { _recovered = null; }
        }
    }
}
