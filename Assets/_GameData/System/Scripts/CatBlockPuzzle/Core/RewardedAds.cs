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
            if (IsRequestPending)
            {
                onFailed?.Invoke(Outcome.Unavailable);
                return;
            }

            IsRequestPending = true;
            bool reported = false;
            void Report(Outcome outcome)
            {
                if (reported) return;
                reported = true;
                IsRequestPending = false;
                Debug.Log("[RewardedAds] " + placement + " -> " + outcome);
                if (outcome == Outcome.Rewarded) onRewarded?.Invoke();
                else onFailed?.Invoke(outcome);
            }

            if (SimulatedOutcome.HasValue)
            {
                Report(SimulatedOutcome.Value);
                return;
            }

#if USE_AVNADS_PLUGIN
            bool started = AVNPlugin.DTInstance != null && AVNPlugin.DTInstance.ShowRewardedAd(
                onRewarded: () => Report(Outcome.Rewarded),
                onNoAd: () => Report(Outcome.Unavailable),
                onCancelled: () => Report(Outcome.Cancelled));
            if (!started) Report(Outcome.Unavailable);
#else
            // No ads SDK in this build yet: grant immediately so the reward path stays testable.
            Report(Outcome.Rewarded);
#endif
        }
    }
}
