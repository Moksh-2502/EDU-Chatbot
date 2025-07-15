using SharedCore.Analytics;
using SubwaySurfers.Analytics.Session;

namespace SubwaySurfers.Analytics.Events.Session
{
    /// <summary>
    /// Analytics event fired when a new session starts
    /// </summary>
    public class SessionStartEvent : BaseAnalyticsEvent
    {
        public override string EventName => "session_start";

        public string PlayerId { get; }
        public float ExpectedIdleTimeoutMinutes { get; }

        public SessionStartEvent(SessionMetrics sessionMetrics)
        {
            PlayerId = sessionMetrics.PlayerId;
            ExpectedIdleTimeoutMinutes = 5.0f; // Default idle timeout
        }
    }
} 