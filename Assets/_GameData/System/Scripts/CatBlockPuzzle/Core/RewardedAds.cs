using AVNEvents;
using System;
using UnityEngine;
#if USE_AVNADS_PLUGIN
using AVN.AdsPlugin.Controllers;
#endif

namespace CatBlockPuzzle
{
    /// <summary>
    /// Single entry point for rewarded ads. With the AVN ads plugin (USE_AVNADS_PLUGIN, as in
    /// ZombieRush) it plays a real rewarded ad; without it (Editor, or before the SDK is added)
    /// the reward is granted straight away so the flow can be played and tested end to end.
    /// Exactly one of the callbacks fires per request.
    /// </summary>
    public static class RewardedAds
    {
        public enum Outcome { Rewarded, Unavailable, Cancelled }

        /// <summary>Tests (and QA) can force an outcome instead of the platform behaviour.</summary>
        internal static Outcome? SimulatedOutcome;

        public static bool IsRequestPending { get; private set; }

        public static void Show(string placement, Action onRewarded, Action<Outcome> onFailed)
        {
            string requestId = Guid.NewGuid().ToString("N");
            bool simulated = SimulatedOutcome.HasValue;
#if !USE_AVNADS_PLUGIN
            simulated = true;
#endif
            AvnEvents.AdRequest(requestId, placement, "Requested", simulated: simulated);
            if (IsRequestPending)
            {
                AvnEvents.AdRequest(requestId, placement, "Result", "request_pending", simulated);
                onFailed?.Invoke(Outcome.Unavailable);
                return;
            }

            IsRequestPending = true;
            GameAnalytics.RewardedAdRequested(placement);
            bool reported = false;
            bool avnAdShown = false; // AD_ANALYSIS only covers ads the network was really asked to show
            bool networkAsked = false;
            bool timedOut = false;
            Action<AVNAnalytics.Events.AvnAd> timeout = null;
            void Report(Outcome outcome)
            {
                if (reported) return;
                reported = true;
                IsRequestPending = false;
                if (timeout != null) AVNAnalytics.Events.AvnAds.TimedOut -= timeout;
                Debug.Log("[RewardedAds] " + placement + " -> " + outcome);
                GameAnalytics.RewardedAdResult(placement, outcome);
                AvnEvents.AdRequest(requestId, placement, "Result", timedOut ? "timeout" : AvnEvents.LowerSnake(outcome.ToString()), simulated);
                if (networkAsked && !timedOut)
                {
                    if (outcome == Outcome.Unavailable) AvnEvents.AdFailed(AdFormat.Rewarded, AdFailReason.NotReady);
                    else if (avnAdShown) AvnEvents.AdClosed(AdFormat.Rewarded, outcome == Outcome.Rewarded);
                }
                if (outcome == Outcome.Rewarded) onRewarded?.Invoke();
                else onFailed?.Invoke(outcome);
            }

            if (SimulatedOutcome.HasValue)
            {
                Report(SimulatedOutcome.Value);
                return;
            }

#if USE_AVNADS_PLUGIN
            AvnEvents.AdAsk(AdFormat.Rewarded, AvnEvents.PlacementFor(placement));
            networkAsked = true;
            bool providerReturned = false;
            Outcome? synchronousOutcome = null;
            void ProviderResult(Outcome outcome)
            {
                if (!providerReturned) { if (!synchronousOutcome.HasValue) synchronousOutcome = outcome; }
                else Report(outcome);
            }
            bool started;
            try
            {
                started = AVNPlugin.DTInstance != null && AVNPlugin.DTInstance.ShowRewardedAd(
                    onRewarded: () => ProviderResult(Outcome.Rewarded),
                    onNoAd: () => ProviderResult(Outcome.Unavailable),
                    onCancelled: () => ProviderResult(Outcome.Cancelled));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                providerReturned = true;
                AvnEvents.AdFailed(AdFormat.Rewarded, AdFailReason.ShowFailed);
                networkAsked = false;
                Report(Outcome.Unavailable);
                return;
            }
            providerReturned = true;
            // The wrapper exposes acceptance, not an impression callback. Never report a Show
            // for a rejected request or a synchronous onNoAd callback.
            if (started && synchronousOutcome != Outcome.Unavailable)
            {
                AvnEvents.AdShow(AdFormat.Rewarded);
                avnAdShown = true;
                string adId = AVNAnalytics.Events.AvnAds.Find(AdFormat.Rewarded.ToString())?.Id;
                timeout = ad =>
                {
                    if (ad.Id != adId) return;
                    timedOut = true;
                    Report(Outcome.Unavailable);
                };
                AVNAnalytics.Events.AvnAds.TimedOut += timeout;
                AvnEvents.AdRequest(requestId, placement, "Accepted");
            }
            if (!started) Report(Outcome.Unavailable);
            else if (synchronousOutcome.HasValue) Report(synchronousOutcome.Value);
#else
            // No ads SDK in this build yet: grant immediately so the reward path stays testable.
            Report(Outcome.Rewarded);
#endif
        }
    }
}
