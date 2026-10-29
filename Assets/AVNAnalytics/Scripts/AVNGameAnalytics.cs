using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AVNAnalytics
{
    /// <summary>
    /// Drop-in replacement for the FirebaseAnalytics.LogEvent pattern.
    ///
    ///   AVNGameAnalytics.Initialize(); // loads Resources/AVNGameAnalyticsConfig
    ///   AVNGameAnalytics.LogEvent("level_complete", new AVNGameAnalyticsParameter("level", 3), new AVNGameAnalyticsParameter("time", 42.5));
    /// </summary>
    public static class AVNGameAnalytics
    {
        /// <summary>Max events held in memory while waiting for Initialize(); extras are dropped.</summary>
        const int MaxEarlyEvents = 100;

        struct EarlyEvent { public string Name; public Dictionary<string, object> Params; public DateTime Utc; }

        static AVNGameAnalyticsClient client;
        static readonly List<EarlyEvent> earlyEvents = new List<EarlyEvent>();
        static bool warnedEarly;
        static bool disabled; // switched off by the config (Enabled / SendInEditor): every call is a no-op
        static string disabledReason;
        static bool hasPendingUserId;
        static string pendingUserId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            client = null;
            StandardVersion = 0;
            lock (earlyEvents) earlyEvents.Clear();
            warnedEarly = false;
            disabled = false;
            disabledReason = null;
            hasPendingUserId = false;
            pendingUserId = null;
        }

        /// <summary>
        /// Version of the AVN events standard the game's events follow (0 = none). Stamped into the context of
        /// every batch as "standard" / "standard_version", so the server can validate each event against the
        /// right version of the spec. Set by the game's AvnEvents at startup; not an event parameter.
        /// </summary>
        public static int StandardVersion { get; set; }

        public const string StandardName = "avn-events";

        public static bool IsInitialized => client != null;
        public static AVNGameAnalyticsConfig Config => client?.Config;
        /// <summary>Events stored on disk and not yet acknowledged by the server.</summary>
        public static int PendingCount => client?.PendingCount ?? 0;
        public static string DeviceId => client?.DeviceId;
        public static string SessionId => client?.SessionId;
        /// <summary>1-based number of the current session on this install (the same number session_start reports). 0 before Initialize().</summary>
        public static int SessionNumber => client?.SessionNumber ?? 0;
        /// <summary>Foreground seconds of the current session so far. 0 before Initialize().</summary>
        public static double SessionSeconds => client?.SessionSeconds ?? 0d;
        /// <summary>Human-readable result of the last send attempt (for debugging UIs).</summary>
        public static string LastStatus => disabled ? disabledReason : client?.LastStatus ?? "not initialized";

        /// <summary>Resources path (no extension) of the config asset used by <see cref="Initialize()"/>.</summary>
        public const string ConfigResourceName = "AVNGameAnalyticsConfig";

        /// <summary>
        /// Initializes with the config asset at Resources/AVNGameAnalyticsConfig
        /// (Assets > Create > AVN Analytics > Config, keep it in a Resources folder).
        /// Call once at startup. Safe to call again; later calls are ignored.
        /// </summary>
        public static void Initialize()
        {
            if (client != null || disabled) { Debug.LogWarning("[AVN Analytics] Already initialized."); return; }
            var config = Resources.Load<AVNGameAnalyticsConfig>(ConfigResourceName);
            if (config == null)
            {
                Debug.LogError("[AVN Analytics] No config found. Create one (Assets > Create > AVN Analytics > Config), name it '"
                    + ConfigResourceName + "' and put it in a Resources folder. Analytics disabled.");
                return;
            }
            Initialize(config);
        }

        /// <summary>Initialize with a specific config (e.g. one built at runtime). Later calls are ignored.</summary>
        public static void Initialize(AVNGameAnalyticsConfig config)
        {
            if (client != null || disabled) { Debug.LogWarning("[AVN Analytics] Already initialized."); return; }

            if (config != null && !config.Enabled) { Disable("disabled in the config (Enabled is off)"); return; }
            if (!IsValid(config)) return;
            if (Application.isEditor && !config.SendInEditor) { Disable("disabled in the Editor (Send In Editor is off)"); return; }

            var go = new GameObject("AVN Analytics") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            var runner = go.AddComponent<AVNGameAnalyticsRunner>();
            client = new AVNGameAnalyticsClient(config);
            runner.Client = client;
            if (hasPendingUserId) { client.SetUserId(pendingUserId); hasPendingUserId = false; pendingUserId = null; }
            client.Start(runner);
            ReplayEarlyEvents();
        }

        static void Disable(string reason)
        {
            disabled = true;
            disabledReason = reason;
            lock (earlyEvents) earlyEvents.Clear();
            Debug.Log("[AVN Analytics] " + reason + ": events are ignored.");
        }

        static bool IsValid(AVNGameAnalyticsConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.Endpoint))
            {
                Debug.LogError("[AVN Analytics] Initialize needs a config with Endpoint and ApiKey. Analytics disabled.");
                return false;
            }
            if (!Uri.TryCreate(config.Endpoint.Trim(), UriKind.Absolute, out Uri uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                Debug.LogError("[AVN Analytics] Endpoint '" + config.Endpoint + "' is not a full http(s) URL, e.g. https://your-domain/v1/events. Analytics disabled.");
                return false;
            }
            if (uri.Scheme == Uri.UriSchemeHttp && !Application.isEditor)
                Debug.LogWarning("[AVN Analytics] Endpoint uses plain http. Android and iOS block it unless insecure HTTP is allowed in Player Settings; use https.");
            return true;
        }

        static void ReplayEarlyEvents()
        {
            EarlyEvent[] pending;
            lock (earlyEvents) { pending = earlyEvents.ToArray(); earlyEvents.Clear(); }
            foreach (var e in pending) client.Log(e.Name, e.Params, client.CorrectTimestamp(e.Utc));
            if (pending.Length > 0) Debug.Log("[AVN Analytics] Sent " + pending.Length + " event(s) logged before Initialize().");
        }

        public static void LogEvent(string name) => LogEventInternal(name, null);

        public static void LogEvent(string name, params AVNGameAnalyticsParameter[] parameters)
        {
            Dictionary<string, object> dict = null;
            if (parameters != null && parameters.Length > 0)
            {
                dict = new Dictionary<string, object>(parameters.Length);
                foreach (var p in parameters)
                    if (!string.IsNullOrEmpty(p.Name)) dict[p.Name] = p.Value; // later duplicates win
            }
            LogEventInternal(name, dict);
        }

        /// <summary>Values may be string, bool, any integer type, float, double. Others are ToString()'d; null/NaN/Infinity are dropped.</summary>
        public static void LogEvent(string name, IDictionary<string, object> parameters) => LogEventInternal(name, parameters);

        static void LogEventInternal(string name, IDictionary<string, object> parameters)
        {
            if (disabled) return;
            if (client == null)
            {
                // Keep it (with its real time) and send once Initialize() succeeds.
                lock (earlyEvents)
                {
                    if (!warnedEarly)
                    {
                        warnedEarly = true;
                        Debug.LogWarning("[AVN Analytics] LogEvent('" + name + "') before Initialize(); buffering up to "
                            + MaxEarlyEvents + " events until it is called.");
                    }
                    if (earlyEvents.Count >= MaxEarlyEvents) return;
                    earlyEvents.Add(new EarlyEvent
                    {
                        Name = name,
                        Params = parameters == null ? null : new Dictionary<string, object>(parameters),
                        Utc = DateTime.UtcNow,
                    });
                }
                return;
            }
            client.Log(name, parameters);
        }

        /// <summary>
        /// Optional stable player id (saved on the device). Pass null to clear.
        /// Can be called before Initialize(); it is applied when Initialize() runs.
        /// </summary>
        public static void SetUserId(string userId)
        {
            if (disabled) return;
            if (client != null) { client.SetUserId(userId); return; }
            pendingUserId = userId;
            hasPendingUserId = true;
        }

        /// <summary>
        /// Try to send everything pending right now (ignores the timer and client-side backoff).
        /// Before Initialize() there is nothing to send; Initialize() itself flushes on launch.
        /// </summary>
        public static void Flush() => client?.Flush();
    }
}
