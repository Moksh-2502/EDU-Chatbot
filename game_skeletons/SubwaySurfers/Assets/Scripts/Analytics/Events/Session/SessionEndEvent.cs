using SharedCore.Analytics;
using SubwaySurfers.Analytics.Session;

namespace SubwaySurfers.Analytics.Events.Session
{
    /// <summary>
    /// Analytics event fired when a session ends with complete session summary
    /// </summary>
    public class SessionEndEvent : BaseAnalyticsEvent
    {
        public override string EventName => "session_end";

        public string PlayerId { get; }
        public float TotalDurationMinutes { get; }
        public int TotalQuestionsDisplayed { get; }
        public int TotalQuestionsAnswered { get; }
        public int TotalQuestionsCorrect { get; }
        public int TotalQuestionsIncorrect { get; }
        public int TotalQuestionsSkipped { get; }
        public float FinalAccuracyPercentage { get; }
        public float QuestionsPerMinute { get; }
        public int TotalLivesLost { get; }
        public int TotalGameOvers { get; }
        public int MaxStreakAchieved { get; }
        public int TotalStreaksAchieved { get; }
        public int TotalFactSetsCompleted { get; }
        public int MasteryToFluencyProgressions { get; }
        public int FluencyToNextSetProgressions { get; }
        public int FluencyToMasteryRegressions { get; }

        public SessionEndEvent(SessionMetrics sessionMetrics)
        {
            PlayerId = sessionMetrics.PlayerId;
            TotalDurationMinutes = sessionMetrics.DurationMinutes;
            TotalQuestionsDisplayed = sessionMetrics.QuestionsDisplayed;
            TotalQuestionsAnswered = sessionMetrics.QuestionsAnswered;
            TotalQuestionsCorrect = sessionMetrics.QuestionsCorrect;
            TotalQuestionsIncorrect = sessionMetrics.QuestionsIncorrect;
            TotalQuestionsSkipped = sessionMetrics.QuestionsSkipped;
            FinalAccuracyPercentage = sessionMetrics.AccuracyPercentage;
            QuestionsPerMinute = sessionMetrics.QuestionsPerMinute;
            TotalLivesLost = sessionMetrics.LivesLost;
            TotalGameOvers = sessionMetrics.GameOvers;
            MaxStreakAchieved = sessionMetrics.MaxStreak;
            TotalStreaksAchieved = sessionMetrics.StreaksAchieved;
            TotalFactSetsCompleted = sessionMetrics.FactSetsCompleted;
            MasteryToFluencyProgressions = sessionMetrics.MasteryToFluencyProgressions;
            FluencyToNextSetProgressions = sessionMetrics.FluencyToNextSetProgressions;
            FluencyToMasteryRegressions = sessionMetrics.FluencyToMasteryRegressions;
        }
    }
} 