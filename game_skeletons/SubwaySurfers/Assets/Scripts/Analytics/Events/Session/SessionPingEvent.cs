using SharedCore.Analytics;
using SubwaySurfers.Analytics.Session;

namespace SubwaySurfers.Analytics.Events.Session
{
    /// <summary>
    /// Analytics event fired periodically during an active session to track ongoing metrics
    /// </summary>
    public class SessionPingEvent : BaseAnalyticsEvent
    {
        public override string EventName => "session_ping";

        public string PlayerId { get; }
        public float SessionDurationMinutes { get; }
        public int QuestionsDisplayed { get; }
        public int QuestionsAnswered { get; }
        public int QuestionsCorrect { get; }
        public int QuestionsIncorrect { get; }
        public int QuestionsSkipped { get; }
        public float AccuracyPercentage { get; }
        public int LivesLost { get; }
        public int GameOvers { get; }
        public int MaxStreak { get; }
        public int FactSetsCompleted { get; }

        public SessionPingEvent(SessionMetrics sessionMetrics, float currentDurationMinutes)
        {
            PlayerId = sessionMetrics.PlayerId;
            SessionDurationMinutes = currentDurationMinutes;
            QuestionsDisplayed = sessionMetrics.QuestionsDisplayed;
            QuestionsAnswered = sessionMetrics.QuestionsAnswered;
            QuestionsCorrect = sessionMetrics.QuestionsCorrect;
            QuestionsIncorrect = sessionMetrics.QuestionsIncorrect;
            QuestionsSkipped = sessionMetrics.QuestionsSkipped;
            AccuracyPercentage = sessionMetrics.AccuracyPercentage;
            LivesLost = sessionMetrics.LivesLost;
            GameOvers = sessionMetrics.GameOvers;
            MaxStreak = sessionMetrics.MaxStreak;
            FactSetsCompleted = sessionMetrics.FactSetsCompleted;
        }
    }
} 