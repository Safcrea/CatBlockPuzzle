using System.Collections.Generic;
using UnityEngine;

namespace AVNAnalytics.Events
{
    /// <summary>
    /// What events need to remember: "has this happened on this install" flags (saved) and wall-clock
    /// timers (in memory). Saves only when a flag changes, never per frame.
    /// </summary>
    public static class AvnMemory
    {
        const string Prefix = "AVN.Mem.";
        static readonly Dictionary<string, float> Timers = new Dictionary<string, float>();

        /// <summary>True the first time it is asked for this key on this install, false ever after.</summary>
        public static bool FirstTime(string key)
        {
            string full = Prefix + key;
            if (PlayerPrefs.HasKey(full)) return false;
            PlayerPrefs.SetInt(full, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static string GetText(string key) => PlayerPrefs.GetString(Prefix + key, string.Empty);

        public static void SetText(string key, string value)
        {
            PlayerPrefs.SetString(Prefix + key, value ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static void StartTimer(string key) => Timers[key] = Time.realtimeSinceStartup;

        public static bool TimerRunning(string key) => Timers.ContainsKey(key);

        /// <summary>Stops the timer. False when it was not running (so a second terminal state is dropped).</summary>
        public static bool StopTimer(string key, out float seconds)
        {
            if (!Timers.TryGetValue(key, out float started))
            {
                seconds = 0f;
                return false;
            }
            Timers.Remove(key);
            seconds = Mathf.Max(0f, Time.realtimeSinceStartup - started);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Timers.Clear();
    }
}
