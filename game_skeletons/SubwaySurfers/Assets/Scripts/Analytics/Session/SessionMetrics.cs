using System;
using System.Collections.Generic;

namespace SubwaySurfers.Analytics.Session
{
    /// <summary>
    /// Data structure for tracking session metrics and statistics
    /// </summary>
    [Serializable]
    public class SessionMetrics
    {
        public string SessionId { get; set; }
        public string PlayerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public float DurationMinutes { get; set; }
        public string Platform { get; set; }
        public string AppVersion { get; set; }

        // Gameplay metrics
        public int QuestionsDisplayed { get; set; }
        public int QuestionsAnswered { get; set; }
        public int QuestionsCorrect { get; set; }
        public int QuestionsIncorrect { get; set; }
        public int QuestionsSkipped { get; set; }
        public int LivesLost { get; set; }
        public int GameOvers { get; set; }
        public int MaxStreak { get; set; }
        public int FactSetsCompleted { get; set; }

        // Educational progression metrics
        public int MasteryToFluencyProgressions { get; set; }
        public int FluencyToNextSetProgressions { get; set; }
        public int FluencyToMasteryRegressions { get; set; }
        public int StreaksAchieved { get; set; }

        // Calculated properties
        public float AccuracyPercentage => QuestionsAnswered > 0 ? (float)QuestionsCorrect / QuestionsAnswered * 100 : 0;
        public float QuestionsPerMinute => DurationMinutes > 0 ? QuestionsDisplayed / DurationMinutes : 0;

        public SessionMetrics()
        {
            // Initialize collections and default values
            QuestionsDisplayed = 0;
            QuestionsAnswered = 0;
            QuestionsCorrect = 0;
            QuestionsIncorrect = 0;
            QuestionsSkipped = 0;
            LivesLost = 0;
            GameOvers = 0;
            MaxStreak = 0;
            FactSetsCompleted = 0;
            MasteryToFluencyProgressions = 0;
            FluencyToNextSetProgressions = 0;
            FluencyToMasteryRegressions = 0;
            StreaksAchieved = 0;
        }

        // Recording methods
        public void RecordQuestionDisplayed()
        {
            QuestionsDisplayed++;
        }

        public void RecordQuestionAnswered(bool isCorrect)
        {
            QuestionsAnswered++;
            if (isCorrect)
                QuestionsCorrect++;
            else
                QuestionsIncorrect++;
        }

        public void RecordQuestionSkipped()
        {
            QuestionsSkipped++;
        }

        public void RecordLifeLost()
        {
            LivesLost++;
        }

        public void RecordGameOver()
        {
            GameOvers++;
        }

        public void RecordStreak(int streakLength)
        {
            StreaksAchieved++;
            if (streakLength > MaxStreak)
                MaxStreak = streakLength;
        }

        public void RecordFactSetProgression()
        {
            FactSetsCompleted++;
        }

        public void RecordMasteryToFluencyProgression()
        {
            MasteryToFluencyProgressions++;
        }

        public void RecordFluencyToNextSetProgression()
        {
            FluencyToNextSetProgressions++;
        }

        public void RecordFluencyToMasteryRegression()
        {
            FluencyToMasteryRegressions++;
        }

        /// <summary>
        /// Gets a summary of the session for analytics
        /// </summary>
        public Dictionary<string, object> GetSessionSummary()
        {
            return new Dictionary<string, object>
            {
                ["session_id"] = SessionId,
                ["player_id"] = PlayerId,
                ["start_time"] = StartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["end_time"] = EndTime?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["duration_minutes"] = DurationMinutes,
                ["platform"] = Platform,
                ["app_version"] = AppVersion,
                ["questions_displayed"] = QuestionsDisplayed,
                ["questions_answered"] = QuestionsAnswered,
                ["questions_correct"] = QuestionsCorrect,
                ["questions_incorrect"] = QuestionsIncorrect,
                ["questions_skipped"] = QuestionsSkipped,
                ["accuracy_percentage"] = AccuracyPercentage,
                ["questions_per_minute"] = QuestionsPerMinute,
                ["lives_lost"] = LivesLost,
                ["game_overs"] = GameOvers,
                ["max_streak"] = MaxStreak,
                ["streaks_achieved"] = StreaksAchieved,
                ["fact_sets_completed"] = FactSetsCompleted,
                ["mastery_to_fluency_progressions"] = MasteryToFluencyProgressions,
                ["fluency_to_next_set_progressions"] = FluencyToNextSetProgressions,
                ["fluency_to_mastery_regressions"] = FluencyToMasteryRegressions
            };
        }
    }
} 