using System;
using System.Collections.Generic;

namespace AVNAnalytics.Events
{
    /// <summary>One ad attempt, joined by <see cref="Id"/> (the spec's AD_ID).</summary>
    public sealed class AvnAd
    {
        public string Id;
        public string Format;
        public string Placement;
        public bool Fallback;
        /// <summary>A rewarded ad (or a fallback standing in for one) paid out.</summary>
        public bool Rewarded;
        /// <summary>The network reported the ad on screen.</summary>
        public bool Displayed;
        public int? Level;
        public DateTime OpenedAtUtc;
    }

    /// <summary>
    /// Keeps ad state per AD_ID so callbacks may arrive out of order or not at all. It knows nothing about the
    /// event keys; AvnEvents.cs sends them. Formats are opaque strings chosen by AvnEvents (AdFormat names).
    /// </summary>
    public static class AvnAds
    {
        public const string Unattributed = "unattributed";
        public const float TimeoutSeconds = 180f;
        const float RequestLifetimeSeconds = 15f;
        const float NotReadyGraceSeconds = 1.5f; // lets the plugin start a fallback ad first

        sealed class Request
        {
            public string Placement;
            public int? Level;
            public DateTime AtUtc;
        }

        /// <summary>Whether the ad network has an ad of that format loaded. Set by the game's ad integration.</summary>
        public static Func<string, bool> IsReady;

        /// <summary>An ad has been open for <see cref="TimeoutSeconds"/> without closing. AvnEvents sends Failed/Timeout.</summary>
        public static event Action<AvnAd> TimedOut;

        /// <summary>A rewarded button was pressed with no ad loaded. Args: format, placement, level.</summary>
        public static event Action<string, string, int?> NotReady;

        static readonly Dictionary<string, Request> Requests = new Dictionary<string, Request>();
        static readonly List<AvnAd> Open = new List<AvnAd>();
        static string _notReadyFormat;
        static DateTime _notReadyAtUtc;

        /// <summary>A fullscreen ad covers, or is about to cover, the game.</summary>
        public static bool AdActive => Open.Count > 0;

        /// <summary>Call right before asking the network to show an ad. Remembers what asked for it.</summary>
        public static void Ask(string format, string placement, int? level, bool watchReady)
        {
            Requests[format] = new Request
            {
                Placement = string.IsNullOrEmpty(placement) ? Unattributed : placement,
                Level = level,
                AtUtc = DateTime.UtcNow,
            };
            if (watchReady && IsReady != null && !IsReady(format))
            {
                _notReadyFormat = format;
                _notReadyAtUtc = DateTime.UtcNow;
            }
        }

        /// <summary>The network is about to show an ad: opens a record with a fresh AD_ID.</summary>
        /// <param name="requestFormat">The format that asked, when a different one is shown in its place (a fallback).</param>
        public static AvnAd Begin(string format, bool fallback, string placementOverride = null, string requestFormat = null)
        {
            string asked = requestFormat ?? format;
            Requests.TryGetValue(asked, out Request request);
            bool fresh = request != null && (DateTime.UtcNow - request.AtUtc).TotalSeconds <= RequestLifetimeSeconds;
            if (_notReadyFormat == asked) _notReadyFormat = null;

            var ad = new AvnAd
            {
                Id = Guid.NewGuid().ToString("N"),
                Format = format,
                Placement = placementOverride ?? (fresh ? request.Placement : Unattributed),
                Fallback = fallback,
                Level = fresh ? request.Level : null,
                OpenedAtUtc = DateTime.UtcNow,
            };
            Open.Add(ad);
            Requests.Remove(asked);
            return ad;
        }

        /// <summary>The newest open ad of that format, or null.</summary>
        public static AvnAd Find(string format)
        {
            for (int i = Open.Count - 1; i >= 0; i--)
                if (Open[i].Format == format) return Open[i];
            return null;
        }

        /// <summary>Closes the record. False when it was already closed (a duplicate callback).</summary>
        public static bool End(AvnAd ad) => ad != null && Open.Remove(ad);

        /// <summary>The request that is waiting for an ad of that format (its placement), if still fresh.</summary>
        public static string PendingPlacement(string format) =>
            Requests.TryGetValue(format, out Request r) && (DateTime.UtcNow - r.AtUtc).TotalSeconds <= RequestLifetimeSeconds
                ? r.Placement
                : Unattributed;

        public static int? PendingLevel(string format) =>
            Requests.TryGetValue(format, out Request r) ? r.Level : null;

        /// <summary>The network refused or failed to show: drops the waiting request.</summary>
        public static void ForgetRequest(string format)
        {
            Requests.Remove(format);
            if (_notReadyFormat == format) _notReadyFormat = null;
        }

        internal static void Tick()
        {
            DateTime now = DateTime.UtcNow;
            for (int i = Open.Count - 1; i >= 0; i--)
            {
                AvnAd ad = Open[i];
                if ((now - ad.OpenedAtUtc).TotalSeconds < TimeoutSeconds) continue;
                Open.RemoveAt(i);
                TimedOut?.Invoke(ad);
            }

            if (_notReadyFormat != null && (now - _notReadyAtUtc).TotalSeconds >= NotReadyGraceSeconds)
            {
                string format = _notReadyFormat;
                _notReadyFormat = null;
                NotReady?.Invoke(format, PendingPlacement(format), PendingLevel(format));
                Requests.Remove(format);
            }
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsReady = null;
            TimedOut = null;
            NotReady = null;
            Requests.Clear();
            Open.Clear();
            _notReadyFormat = null;
        }
    }
}
