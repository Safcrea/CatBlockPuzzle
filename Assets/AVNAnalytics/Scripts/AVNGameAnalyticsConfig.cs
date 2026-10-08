using UnityEngine;

namespace AVNAnalytics
{
    /// <summary>Which kind of build this is. Sent as the environment on session_start.</summary>
    public enum AVNEnvironment
    {
        Development,
        Production,
    }

    /// <summary>
    /// Settings for the AVN Analytics SDK. Create one via Assets > Create > AVN Analytics > Config,
    /// name it AVNGameAnalyticsConfig and keep it in a Resources folder; AVNGameAnalytics.Initialize()
    /// loads it from there. Or build one in code with ScriptableObject.CreateInstance and pass it
    /// to AVNGameAnalytics.Initialize(config).
    /// App version, build and platform are not settings; the SDK detects them itself.
    /// </summary>
    [CreateAssetMenu(fileName = "AVNGameAnalyticsConfig", menuName = "AVN Analytics/Config")]
    public sealed class AVNGameAnalyticsConfig : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Master switch. Off: Initialize() does nothing, nothing is logged, queued or sent, and no hidden object is created.")]
        public bool Enabled = true;

        [Header("Server")]
        [Tooltip("Full ingest URL, e.g. https://analytics.example.com/v1/events")]
        public string Endpoint = "";

        [Tooltip("Game API key from the admin site (sent as X-API-Key). An identifier, not a secret.")]
        public string ApiKey = "";

        [Header("Batching")]
        [Tooltip("Send as soon as this many events are pending (server max is 500).")]
        [Min(1)]
        public int BatchSize = 50;

        [Tooltip("Send pending events at least this often, in seconds. Events are also sent when a batch fills up and when the app is paused or launched.")]
        [Min(3f)]
        public float FlushIntervalSeconds = 120f;

        [Tooltip("Gzip request bodies (about 5x smaller). Turned off automatically if the server is too old to accept gzip.")]
        public bool CompressRequests = true;

        [Tooltip("Hard cap on one request body in bytes. Server limit is 1 MiB.")]
        public int MaxBatchBytes = 700 * 1024;

        [Tooltip("Gap between batches while draining a backlog, in seconds.")]
        public float PaceGapSeconds = 1f;

        public float RequestTimeoutSeconds = 20f;

        [Header("Retry")]
        public float MinBackoffSeconds = 5f;
        public float MaxBackoffSeconds = 300f;

        [Tooltip("How long to wait after 401/403/404/415 (bad key, disabled key, misconfiguration), in seconds.")]
        public float AuthRetrySeconds = 1800f;

        [Header("Disk queue")]
        public int MaxQueuedEvents = 10000;
        public long MaxQueueBytes = 5L * 1024 * 1024;

        [Header("Behaviour")]
        [Tooltip("Start a new session after the app was in the background this long, in seconds.")]
        public float SessionTimeoutSeconds = 1800f;

        [Tooltip("Log first_open (once per install), session_start and session_end automatically.")]
        public bool AutoSessionEvents = true;

        [Tooltip("Shift client_ts by the device clock error measured from server responses.")]
        public bool CorrectClockSkew = true;

        public bool DebugLogging = false;

        [Header("Environment")]
        [Tooltip("Tag sent as environment on session_start. Set Development while testing and Production for the release build, then filter dashboards on it. Play mode in the Editor is always tagged editor.")]
        public AVNEnvironment Environment = AVNEnvironment.Production;

        [Tooltip("Send events while playing in the Editor. Editor sessions are tagged environment=editor either way.")]
        public bool SendInEditor = true;

        // Whether the config Inspector shows the advanced settings. Editor-only convenience.
        [SerializeField, HideInInspector] bool showAdvancedSettings;

        // Android versionCode / iOS build number, written by the build step (AVNGameAnalyticsBuildStamp). Not editable.
        [SerializeField, HideInInspector] string buildNumber = "";

        internal string BuildNumber { get => buildNumber; set => buildNumber = value; }

#if UNITY_EDITOR
        /// <summary>The build number Player Settings will give a build for this target ("" if the platform has none).</summary>
        internal static string EditorBuildNumber(UnityEditor.BuildTarget target)
        {
            switch (target)
            {
                case UnityEditor.BuildTarget.Android: return UnityEditor.PlayerSettings.Android.bundleVersionCode.ToString();
                case UnityEditor.BuildTarget.iOS: return UnityEditor.PlayerSettings.iOS.buildNumber;
                default: return "";
            }
        }
#endif
    }
}
