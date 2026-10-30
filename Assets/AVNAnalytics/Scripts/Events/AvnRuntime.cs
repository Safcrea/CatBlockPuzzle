using System;
using UnityEngine;

namespace AVNAnalytics.Events
{
    /// <summary>Spec enum APP PREV_EXIT: how the previous run ended.</summary>
    public enum AvnExit { Quit, Background, Killed }

    /// <summary>Spec enum APP CAUSE: why the app went to the background and came back.</summary>
    public enum AvnCause { Player, Ad, Purchase }

    /// <summary>
    /// Hidden host that turns Unity callbacks into the few lifecycle signals the events need, and ticks the
    /// SDK-side state (level clock, ad timeouts). Sends no event: AvnEvents.cs subscribes and sends.
    /// </summary>
    public static class AvnRuntime
    {
        const string ExitKey = "AVN.Exit";
        const string ExitForeground = "Foreground";
        const string ExitBackground = "Background";
        const string ExitQuit = "Quit";

        /// <summary>First frame of the process. Arg: how the previous run ended (null on the first launch).</summary>
        public static event Action<AvnExit?> Launched;

        /// <summary>The app went to the background. Args: cause.</summary>
        public static event Action<AvnCause> Backgrounded;

        /// <summary>The app came back (a frame later, so a renewed SDK session is the one stamped). Args: cause, seconds away.</summary>
        public static event Action<AvnCause, float> Foregrounded;

        /// <summary>The app is quitting normally (not sent by the OS on a kill).</summary>
        public static event Action Quitting;

        /// <summary>The SDK started a new session (and the first one after it initialises).</summary>
        public static event Action NewSession;

        public static bool InBackground { get; private set; }

        /// <summary>The game sets this while a store dialog is open, so APP Background gets CAUSE=Purchase.</summary>
        public static bool PurchaseActive;

        static bool _launched;
        static AvnExit? _previousExit;
        static AvnCause _cause;
        static DateTime _backgroundedAtUtc;
        static bool _foregroundPending;
        static int _sessionSeen = -1;

        sealed class Host : MonoBehaviour
        {
            void Update() => Tick(Mathf.Min(Time.unscaledDeltaTime, 1f));
            void OnApplicationPause(bool paused) => SetBackground(paused);
            void OnApplicationQuit() => Quit();

#if UNITY_STANDALONE && !UNITY_EDITOR
            // Desktop with Run In Background keeps running: focus is the only signal.
            void OnApplicationFocus(bool hasFocus)
            {
                if (Application.runInBackground) SetBackground(!hasFocus);
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Launched = null;
            Backgrounded = null;
            Foregrounded = null;
            Quitting = null;
            NewSession = null;
            InBackground = false;
            PurchaseActive = false;
            _launched = false;
            _foregroundPending = false;
            _sessionSeen = -1;
            _lastWritten = null;
            _previousExit = ReadPreviousExit();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var host = new GameObject("AvnRuntime") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<Host>();
            WriteExit(ExitForeground);
        }

        static void Tick(float dt)
        {
            if (!_launched)
            {
                _launched = true;
                Launched?.Invoke(_previousExit);
            }

            AvnAttempt.Tick(dt);
            AvnAds.Tick();

            if (AVNGameAnalytics.IsInitialized && AVNGameAnalytics.SessionNumber != _sessionSeen)
            {
                _sessionSeen = AVNGameAnalytics.SessionNumber;
                NewSession?.Invoke();
            }

            if (_foregroundPending && !InBackground)
            {
                _foregroundPending = false;
                float away = Mathf.Max(0f, (float)(DateTime.UtcNow - _backgroundedAtUtc).TotalSeconds);
                Foregrounded?.Invoke(_cause, away);
            }
        }

        static void SetBackground(bool background)
        {
            if (background == InBackground) return;
            InBackground = background;

            if (background)
            {
                _foregroundPending = false;
                _backgroundedAtUtc = DateTime.UtcNow;
                _cause = AvnAds.AdActive ? AvnCause.Ad : PurchaseActive ? AvnCause.Purchase : AvnCause.Player;
                AvnAttempt.Checkpoint();
                WriteExit(ExitBackground);
                Backgrounded?.Invoke(_cause);
                AVNGameAnalytics.Flush();
            }
            else
            {
                _foregroundPending = true;
                WriteExit(ExitForeground);
            }
        }

        static void Quit()
        {
            if (!InBackground) Quitting?.Invoke();
            WriteExit(ExitQuit);
            AVNGameAnalytics.Flush();
        }

        static AvnExit? ReadPreviousExit()
        {
            switch (PlayerPrefs.GetString(ExitKey, string.Empty))
            {
                case ExitQuit: return AvnExit.Quit;
                case ExitBackground: return AvnExit.Background;
                case ExitForeground: return AvnExit.Killed;
                default: return null;
            }
        }

        static string _lastWritten;
        static void WriteExit(string value)
        {
            if (_lastWritten == value) return;
            _lastWritten = value;
            PlayerPrefs.SetString(ExitKey, value);
            PlayerPrefs.Save();
        }
    }
}
