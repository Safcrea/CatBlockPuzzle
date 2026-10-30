using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AVNAnalytics
{
    [Serializable]
    internal sealed class AvnState
    {
        public string deviceId;
        public string userId;
        public double skewSeconds;
        public bool firstOpenLogged;
        public int sessionNumber;
        // The session that has started but whose session_end has not been logged yet.
        public bool hasOpenSession;
        public string openSessionId;
        public int openSeq;
        public double openForegroundSeconds;
        public long openLastActiveTicks; // UTC ticks of the last time we knew the app was alive
    }

    [Serializable]
    internal sealed class AvnResponse
    {
        public int accepted;
        public int duplicates;
        public string server_ts;
    }

    /// <summary>Event creation, persistence hand-off, batching, retry and backoff.</summary>
    internal sealed class AVNGameAnalyticsClient
    {
        internal readonly AVNGameAnalyticsConfig Config;
        readonly AVNGameAnalyticsEventQueue queue;
        readonly AvnState state;
        readonly string statePath;
        readonly System.Random random = new System.Random();
        readonly object stateGate = new object();
        MonoBehaviour host;

        readonly string appVersion, build, platform, environment;
        string sessionId;
        DateTime? pausedAtUtc;
        long pausedAtStopwatch;

        // session clock (foreground time only)
        int seq;
        double fgAccum;
        long segStart; // Stopwatch timestamp when the app last came to the foreground; 0 = in background
        float nextHeartbeatAt;
        const float HeartbeatSeconds = 30f;

        // sending state (main thread only)
        bool inFlight;
        bool forceFlush;
        bool draining;
        float holdUntil;
        bool holdIsServerDirected;
        float nextSendAt;
        float nextTimerAt;
        float backoff;
        int batchLimit;
        string lastStatus = "idle";
        bool lastRequestCompressed;
        bool compressionRejected; // an older server answered 415 to gzip: send plain JSON from now on
        bool reportedConfigError; // first 401/403/404 is an error in the console, later ones are warnings

        internal string DeviceId => state.deviceId;
        internal string SessionId => sessionId;
        internal string LastStatus => lastStatus;
        internal int PendingCount => queue.Count;
        internal int SessionNumber { get { lock (stateGate) return state.sessionNumber; } }
        internal double SessionSeconds => ForegroundSeconds();

        internal AVNGameAnalyticsClient(AVNGameAnalyticsConfig config)
        {
            Config = config;
            string dir = Path.Combine(Application.persistentDataPath, "avn_analytics");
            Directory.CreateDirectory(dir);
            statePath = Path.Combine(dir, "state.json");
            state = LoadState();
            queue = new AVNGameAnalyticsEventQueue(dir, Mathf.Max(1, config.MaxQueuedEvents), Math.Max(1024, config.MaxQueueBytes), LogVerbose);

            appVersion = Clean(Application.version, "0");
            build = Clean(DetectBuild(config), "0");
            platform = Clean(DetectPlatform(), "unknown");
            environment = Application.isEditor ? "editor" : config.Environment == AVNEnvironment.Development ? "development" : "production";
            batchLimit = Mathf.Clamp(config.BatchSize, 1, 500);
        }

        internal void Start(MonoBehaviour runner)
        {
            host = runner;
            EndSessionFromState(); // a session left open by a previous run (killed, crashed or quit)
            NewSession();
            if (Config.AutoSessionEvents)
            {
                if (!state.firstOpenLogged)
                {
                    state.firstOpenLogged = true;
                    SaveState();
                    Log("first_open", null);
                }
                LogSessionStart();
            }
            Flush(); // launch: send anything left over from earlier runs
        }

        // ------------------------------------------------------------ events

        internal void Log(string name, IDictionary<string, object> parameters, DateTime? timestampUtc = null)
        {
            Enqueue(name, BuildEvent(name, parameters, null, timestampUtc, -1));
        }

        void Enqueue(string name, string line)
        {
            if (queue.Append(line)) LogVerbose("queued " + name + " (" + queue.Count + " pending)");
        }

        DateTime NowCorrected() => CorrectTimestamp(DateTime.UtcNow);

        /// <summary>Shift a device UTC time by the clock error measured from the server.</summary>
        internal DateTime CorrectTimestamp(DateTime utc)
        {
            double skew;
            lock (stateGate) skew = state.skewSeconds;
            return Config.CorrectClockSkew && Math.Abs(skew) > 10 ? utc.AddSeconds(skew) : utc;
        }

        string BuildEvent(string name, IDictionary<string, object> parameters, string sessionOverride, DateTime? tsOverride, int seqOverride)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"event_id\":\"").Append(Guid.NewGuid().ToString()).Append("\",\"name\":");
            AVNGameAnalyticsJson.WriteString(sb, SanitizeName(name));
            sb.Append(",\"params\":{");
            int seqValue = seqOverride >= 0 ? seqOverride : System.Threading.Interlocked.Increment(ref seq);
            sb.Append("\"seq\":").Append(seqValue.ToString(CultureInfo.InvariantCulture));
            if (parameters != null)
            {
                int written = 1; // seq
                foreach (var kv in parameters)
                {
                    if (kv.Key == "seq") continue; // reserved
                    if (written >= 50) { LogVerbose("more than 50 params on " + name + "; extras dropped"); break; }
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    int mark = sb.Length;
                    if (written > 0) sb.Append(',');
                    AVNGameAnalyticsJson.WriteString(sb, AVNGameAnalyticsJson.Truncate(kv.Key, 80));
                    sb.Append(':');
                    if (AVNGameAnalyticsJson.TryWriteParamValue(sb, kv.Value)) written++;
                    else { sb.Length = mark; LogVerbose("param '" + kv.Key + "' dropped (null or non-finite)"); }
                }
            }
            sb.Append("}");
            DateTime ts = tsOverride ?? NowCorrected();
            sb.Append(",\"client_ts\":\"").Append(ts.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture)).Append("\"}");

            // On disk an event is "<context json>\t<event json>". The context (who/where/which build)
            // is identical for most events, so the queue sends it once per batch instead of per event.
            // JSON never contains a raw tab (the writer escapes it), so the tab is an unambiguous separator.
            return BuildContext(sessionOverride ?? sessionId ?? "none") + "\t" + sb;
        }

        string BuildContext(string session)
        {
            string userId;
            lock (stateGate) userId = state.userId;
            var sb = new StringBuilder(220);
            sb.Append('{');
            if (!string.IsNullOrEmpty(userId)) { sb.Append("\"user_id\":"); AVNGameAnalyticsJson.WriteString(sb, AVNGameAnalyticsJson.Truncate(userId, 128)); sb.Append(','); }
            sb.Append("\"device_id\":"); AVNGameAnalyticsJson.WriteString(sb, state.deviceId);
            sb.Append(",\"session_id\":"); AVNGameAnalyticsJson.WriteString(sb, session);
            sb.Append(",\"app_version\":"); AVNGameAnalyticsJson.WriteString(sb, appVersion);
            sb.Append(",\"build\":"); AVNGameAnalyticsJson.WriteString(sb, build);
            sb.Append(",\"platform\":"); AVNGameAnalyticsJson.WriteString(sb, platform);
            int standardVersion = AVNGameAnalytics.StandardVersion;
            if (standardVersion > 0)
                sb.Append(",\"standard\":\"").Append(AVNGameAnalytics.StandardName).Append("\",\"standard_version\":")
                  .Append(standardVersion.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed_event";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' ? c : '_');
            if (!((sb[0] >= 'a' && sb[0] <= 'z') || (sb[0] >= 'A' && sb[0] <= 'Z'))) sb.Insert(0, "e_");
            if (sb.Length > 80) sb.Length = 80;
            return sb.ToString();
        }

        internal void SetUserId(string userId)
        {
            lock (stateGate) state.userId = string.IsNullOrEmpty(userId) ? null : userId;
            SaveState();
        }

        void NewSession()
        {
            sessionId = Guid.NewGuid().ToString("N");
            seq = 0;
            fgAccum = 0;
            segStart = System.Diagnostics.Stopwatch.GetTimestamp();
            lock (stateGate)
            {
                state.sessionNumber++;
                state.hasOpenSession = true;
                state.openSessionId = sessionId;
            }
            PersistSession();
        }

        double ForegroundSeconds()
        {
            double running = segStart != 0
                ? (System.Diagnostics.Stopwatch.GetTimestamp() - segStart) / (double)System.Diagnostics.Stopwatch.Frequency
                : 0;
            return fgAccum + running;
        }

        /// <summary>Move the foreground clock into the accumulator (app going to background / quitting).</summary>
        void StopForegroundClock()
        {
            fgAccum = ForegroundSeconds();
            segStart = 0;
        }

        /// <summary>Record enough on disk to write session_end later, even if the app is killed.</summary>
        /// <param name="durable">fsync the file. Off for the 30 s heartbeat: an fsync can stall a frame on mobile,
        /// and losing one heartbeat to a power cut only makes duration_seconds ~30 s shorter.</param>
        void PersistSession(bool durable = true)
        {
            lock (stateGate)
            {
                state.openForegroundSeconds = ForegroundSeconds();
                state.openLastActiveTicks = NowCorrected().Ticks;
                state.openSeq = seq;
            }
            SaveState(durable);
        }

        /// <summary>
        /// Log session_end for the session recorded in state.json, stamped with the last time the app
        /// was known to be alive. Called when the NEXT session starts, so the user coming back
        /// within the timeout never ends a session.
        /// </summary>
        void EndSessionFromState()
        {
            string id; int number, endSeq; double duration; long lastTicks;
            lock (stateGate)
            {
                if (!state.hasOpenSession || string.IsNullOrEmpty(state.openSessionId)) return;
                id = state.openSessionId; number = state.sessionNumber; endSeq = state.openSeq + 1;
                duration = state.openForegroundSeconds; lastTicks = state.openLastActiveTicks;
                state.hasOpenSession = false;
            }
            SaveState();
            if (!Config.AutoSessionEvents) return;
            DateTime lastActive = lastTicks > 0 ? new DateTime(lastTicks, DateTimeKind.Utc) : NowCorrected();
            var p = new Dictionary<string, object>
            {
                { "duration_seconds", Math.Round(duration, 1) },
                { "session_number", (long)number },
            };
            Enqueue("session_end", BuildEvent("session_end", p, id, lastActive, endSeq));
        }

        void LogSessionStart()
        {
            int number;
            lock (stateGate) number = state.sessionNumber;
            var p = new Dictionary<string, object>
            {
                { "session_number", (long)number },
                { "environment", environment },
                { "timezone_offset_minutes", (long)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes },
                { "device_model", SystemInfo.deviceModel },
                { "os_version", SystemInfo.operatingSystem },
                { "device_type", SystemInfo.deviceType.ToString() },
                { "screen_width", (long)Screen.width },
                { "screen_height", (long)Screen.height },
            };
            Log("session_start", p);
        }

        // ------------------------------------------------------------ lifecycle

        internal void OnPause(bool paused)
        {
            if (paused)
            {
                pausedAtUtc = DateTime.UtcNow;
                pausedAtStopwatch = System.Diagnostics.Stopwatch.GetTimestamp();
                StopForegroundClock();
                PersistSession();
                queue.SyncToDisk();
                Flush();
                return;
            }
            if (pausedAtUtc.HasValue && SecondsAway() > Config.SessionTimeoutSeconds)
            {
                EndSessionFromState(); // the session that ended when the app was backgrounded
                NewSession();
                if (Config.AutoSessionEvents) LogSessionStart();
            }
            else if (segStart == 0)
            {
                segStart = System.Diagnostics.Stopwatch.GetTimestamp(); // same session continues
            }
            pausedAtUtc = null;
            Flush();
        }

        /// <summary>
        /// Time spent in the background. The wall clock counts device sleep but jumps if the player changes
        /// the clock; the monotonic clock can't be changed but may stop while the device sleeps. Taking the
        /// larger keeps a clock set backwards from merging sessions, and sleep from hiding the gap.
        /// </summary>
        double SecondsAway()
        {
            double wall = (DateTime.UtcNow - pausedAtUtc.Value).TotalSeconds;
            double mono = (System.Diagnostics.Stopwatch.GetTimestamp() - pausedAtStopwatch) / (double)System.Diagnostics.Stopwatch.Frequency;
            return Math.Max(wall, mono);
        }

        internal void OnQuit()
        {
            StopForegroundClock();
            PersistSession();
            queue.Close();
        }

        /// <summary>Send everything pending, ignoring the timer and any client-side backoff.</summary>
        internal void Flush()
        {
            forceFlush = true;
            draining = true;
            if (!holdIsServerDirected) holdUntil = 0f;
            nextTimerAt = 0f;
        }

        // ------------------------------------------------------------ sending

        /// <summary>Called every frame by the runner.</summary>
        internal void Tick(float now)
        {
            if (host != null && segStart != 0 && now >= nextHeartbeatAt)
            {
                nextHeartbeatAt = now + HeartbeatSeconds;
                PersistSession(false); // keeps session_end accurate if the app is killed while playing
            }
            if (inFlight || host == null) return;
            if (queue.Count == 0) { draining = false; forceFlush = false; return; }
            if (now < holdUntil || now < nextSendAt) return;

            bool due = draining || forceFlush || queue.Count >= batchLimit || now >= nextTimerAt;
            if (!due) return;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                lastStatus = "offline, waiting for network";
                nextSendAt = now + 5f; // not a failure: no backoff growth
                return;
            }
            host.StartCoroutine(SendOne());
        }

        IEnumerator SendOne()
        {
            inFlight = true;
            forceFlush = false;
            int count;
            string body = queue.PeekBatch(Mathf.Clamp(batchLimit, 1, 500), Config.MaxBatchBytes, out count);
            if (body == null) { inFlight = false; yield break; }

            DateTime sentAt = DateTime.UtcNow;
            byte[] payload = Encoding.UTF8.GetBytes(body);
            lastRequestCompressed = false;
            if (Config.CompressRequests && !compressionRejected && payload.Length >= 256)
            {
                try { payload = AvnGzip.Compress(payload); lastRequestCompressed = true; }
                catch (Exception e) { LogVerbose("gzip failed, sending plain: " + e.Message); }
            }
            using (var req = new UnityWebRequest(Config.Endpoint.Trim(), UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(payload);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (lastRequestCompressed) req.SetRequestHeader("Content-Encoding", "gzip");
                req.SetRequestHeader("X-API-Key", Config.ApiKey.Trim());
                req.timeout = Mathf.Max(1, Mathf.RoundToInt(Config.RequestTimeoutSeconds));

                yield return req.SendWebRequest();

                DateTime receivedAt = DateTime.UtcNow;
                HandleResult(req, count, sentAt, receivedAt);
            }
            inFlight = false;
        }

        void HandleResult(UnityWebRequest req, int count, DateTime sentAt, DateTime receivedAt)
        {
            float now = Time.unscaledTime;
            nextTimerAt = now + Mathf.Max(3f, Config.FlushIntervalSeconds);
            long code = req.responseCode;

            if (req.result == UnityWebRequest.Result.Success && code >= 200 && code < 300)
            {
                var resp = SafeParse(req.downloadHandler.text);
                queue.Advance(count);
                backoff = 0f;
                holdUntil = 0f;
                holdIsServerDirected = false;
                batchLimit = Mathf.Clamp(Config.BatchSize, 1, 500);
                nextSendAt = now + Mathf.Max(0f, Config.PaceGapSeconds);
                if (resp != null) UpdateSkew(resp.server_ts, sentAt, receivedAt);
                lastStatus = "OK sent=" + count + (resp != null ? " accepted=" + resp.accepted + " dup=" + resp.duplicates : "") + " pending=" + queue.Count;
                LogVerbose(lastStatus);
                return;
            }

            if (req.result == UnityWebRequest.Result.ProtocolError)
            {
                float retryAfter = ParseRetryAfter(req);
                if (code == 415 && lastRequestCompressed)
                {
                    // Server predates gzip support: not an error, just stop compressing and retry now.
                    compressionRejected = true;
                    nextSendAt = now;
                    lastStatus = "HTTP 415: server does not accept gzip, sending plain JSON";
                    LogVerbose(lastStatus);
                    return;
                }
                if (code == 400 || code == 413)
                {
                    if (count > 1)
                    {
                        // One bad event poisons the whole batch: halve until the culprit is alone.
                        batchLimit = Mathf.Max(1, count / 2);
                        nextSendAt = now;
                        lastStatus = "HTTP " + code + ", splitting batch to " + batchLimit;
                    }
                    else
                    {
                        queue.Quarantine(1);
                        lastStatus = "HTTP " + code + ": invalid event moved to quarantine.jsonl";
                        nextSendAt = now;
                    }
                    LogVerbose(lastStatus + " " + Truncate(req.downloadHandler.text));
                    return;
                }
                if (code == 429)
                {
                    SetHold(now, Mathf.Max(retryAfter, 5f) + (float)random.NextDouble() * 3f, true);
                    lastStatus = "HTTP 429 rate limited, retry in " + (int)(holdUntil - now) + "s";
                }
                else if (code >= 500)
                {
                    float delay = NextBackoff();
                    SetHold(now, Mathf.Max(delay, retryAfter), retryAfter > 0f);
                    lastStatus = "HTTP " + code + " server error, retry in " + (int)(holdUntil - now) + "s";
                }
                else
                {
                    // 401/403 bad or disabled key, 404/415 misconfiguration: retry rarely, never discard.
                    SetHold(now, Mathf.Max(Config.AuthRetrySeconds, retryAfter), true);
                    lastStatus = "HTTP " + code + " (check ApiKey/Endpoint), retry in " + (int)(holdUntil - now) + "s";
                    if (!reportedConfigError) { reportedConfigError = true; Debug.LogError("[AVN Analytics] " + lastStatus); }
                    else Debug.LogWarning("[AVN Analytics] " + lastStatus);
                }
                LogVerbose(lastStatus);
                return;
            }

            // connection error, timeout, DNS, TLS...
            SetHold(now, NextBackoff(), false);
            lastStatus = "network error: " + req.error + ", retry in " + (int)(holdUntil - now) + "s";
            LogVerbose(lastStatus);
        }

        void SetHold(float now, float seconds, bool serverDirected)
        {
            holdUntil = now + seconds;
            holdIsServerDirected = serverDirected;
            draining = false;
        }

        float NextBackoff()
        {
            backoff = backoff <= 0f ? Config.MinBackoffSeconds : Mathf.Min(backoff * 2f, Config.MaxBackoffSeconds);
            return backoff * (0.5f + 0.5f * (float)random.NextDouble()); // jitter: 50-100% of the step
        }

        static float ParseRetryAfter(UnityWebRequest req)
        {
            string h = req.GetResponseHeader("Retry-After");
            return float.TryParse(h, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) && s > 0f ? Mathf.Min(s, 3600f) : 0f;
        }

        static AvnResponse SafeParse(string json)
        {
            try { return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<AvnResponse>(json); }
            catch (Exception) { return null; }
        }

        /// <summary>skew = server time - device time at the middle of the round trip.</summary>
        void UpdateSkew(string serverTs, DateTime sentAt, DateTime receivedAt)
        {
            if (!Config.CorrectClockSkew || string.IsNullOrEmpty(serverTs)) return;
            if (!DateTimeOffset.TryParse(serverTs, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var server)) return;
            if ((receivedAt - sentAt).TotalSeconds > 10) return; // too slow to trust
            DateTime mid = sentAt.AddTicks((receivedAt - sentAt).Ticks / 2);
            double skew = (server.UtcDateTime - mid).TotalSeconds;
            bool changed;
            lock (stateGate)
            {
                changed = Math.Abs(skew - state.skewSeconds) > 5;
                if (changed) state.skewSeconds = skew;
            }
            if (changed) { LogVerbose("clock skew now " + skew.ToString("F1", CultureInfo.InvariantCulture) + "s"); SaveState(); }
        }

        // ------------------------------------------------------------ state file

        AvnState LoadState()
        {
            AvnState s = null;
            try
            {
                if (File.Exists(statePath)) s = JsonUtility.FromJson<AvnState>(File.ReadAllText(statePath));
            }
            catch (Exception e) { LogVerbose("state load failed: " + e.Message); }
            if (s == null) s = new AvnState();
            if (string.IsNullOrEmpty(s.deviceId))
            {
                s.deviceId = Guid.NewGuid().ToString("N"); // random per install, not a hardware id
                lock (stateGate) { WriteState(s); }
            }
            return s;
        }

        void SaveState(bool durable = true) { lock (stateGate) WriteState(state, durable); }

        void WriteState(AvnState s, bool durable = true)
        {
            try { AVNGameAnalyticsFiles.AtomicWrite(statePath, JsonUtility.ToJson(s), durable); }
            catch (Exception e) { LogVerbose("state save failed: " + e.Message); }
        }

        // ------------------------------------------------------------ helpers

        static string Clean(string detected, string fallback)
        {
            string v = string.IsNullOrEmpty(detected) ? fallback : detected;
            return AVNGameAnalyticsJson.Truncate(v, 128);
        }

        static string DetectBuild(AVNGameAnalyticsConfig config)
        {
#if UNITY_EDITOR
            return AVNGameAnalyticsConfig.EditorBuildNumber(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
#else
            // Stamped into the config by the build step; older builds without it fall back to the build GUID.
            return !string.IsNullOrEmpty(config.BuildNumber) ? config.BuildNumber : Application.buildGUID;
#endif
        }

        static string DetectPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android: return "android";
                case RuntimePlatform.IPhonePlayer: return "ios";
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor: return "editor";
                default: return Application.platform.ToString().ToLowerInvariant();
            }
        }

        static string Truncate(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 300 ? s.Substring(0, 300) : s);

        void LogVerbose(string message)
        {
            if (Config.DebugLogging) Debug.Log("[AVN Analytics] " + message);
        }
    }
}
