using UnityEngine;

namespace AVNAnalytics
{
    /// <summary>Hidden persistent object that drives the client from Unity's lifecycle callbacks.</summary>
    internal sealed class AVNGameAnalyticsRunner : MonoBehaviour
    {
        internal AVNGameAnalyticsClient Client;

        void Update() { Client?.Tick(Time.unscaledTime); }
        void OnApplicationPause(bool paused) { Client?.OnPause(paused); }
        void OnApplicationQuit() { Client?.OnQuit(); }
    }
}
