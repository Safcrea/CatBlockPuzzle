using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace AVNAnalytics.Events
{
    /// <summary>
    /// The one place an AVN standard event leaves the game. It adds STATE, LEVEL_NUMBER and ATTEMPT_ID,
    /// drops every unknown value (null, empty string, a level number below 1) and hands the rest to
    /// <see cref="AVNGameAnalytics.LogEvent(string, IDictionary{string, object})"/>.
    /// Only the game's AvnEvents.cs calls this; game code calls AvnEvents.
    /// </summary>
    public static class AvnSender
    {
        /// <summary>The 1-based level the player is on, or null when the game has none. Set by the game. Used outside an attempt; during one the attempt's level wins.</summary>
        public static Func<int?> CurrentLevel;

        /// <summary>Replaces the SDK call (edit-mode tests).</summary>
        public static Action<string, Dictionary<string, object>> Sink;

        /// <summary>Echo every event to the Unity console. For debugging only.</summary>
        public static bool LogToConsole;

        public static void Send(string name, string state, Dictionary<string, object> p)
        {
            p["STATE"] = state;
            if (!p.ContainsKey("LEVEL_NUMBER"))
            {
                // While an attempt is open or waiting for its choice, everything belongs to that attempt's level,
                // even if the game already moved its progress to the next level (e.g. on a win).
                int? level = AvnAttempt.InProgress || AvnAttempt.ResultPending ? AvnAttempt.Level : SafeLevel();
                if (level.HasValue) p["LEVEL_NUMBER"] = level.Value;
            }

            // The join key: an ad, powerup, purchase or screen that happened inside an attempt says which
            // one, so the counts on that attempt's result can be opened up into the rows behind them.
            if (!p.ContainsKey("ATTEMPT_ID")) p["ATTEMPT_ID"] = AvnAttempt.Id;

            var clean = new Dictionary<string, object>(p.Count);
            foreach (KeyValuePair<string, object> kv in p)
            {
                if (kv.Value == null) continue;
                if (kv.Value is string s && s.Length == 0) continue;
                if (kv.Key == "LEVEL_NUMBER" && kv.Value is int n && n < 1) continue;
                clean[kv.Key] = kv.Value;
            }

            if (Sink != null) Sink(name, clean);
            else AVNGameAnalytics.LogEvent(name, clean);

            if (LogToConsole) Debug.Log(Describe(name, clean));
        }

        static int? SafeLevel()
        {
            try { return CurrentLevel != null ? CurrentLevel() : null; }
            catch (Exception) { return null; }
        }

        static string Describe(string name, Dictionary<string, object> p)
        {
            var sb = new StringBuilder("[AVN event] ").Append(name);
            foreach (KeyValuePair<string, object> kv in p)
                sb.Append(' ').Append(kv.Key).Append('=').Append(Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            CurrentLevel = null;
            Sink = null;
            LogToConsole = false;
        }
    }
}
