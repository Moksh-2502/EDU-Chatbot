using System;

namespace FluencySDK
{
    /// <summary>
    /// Contains information about progression to the next fact set
    /// </summary>
    public class FactSetProgressionInfo
    {
        /// <summary>
        /// The ID of the fact set that was completed
        /// </summary>
        public string CompletedFactSetId { get; }

        /// <summary>
        /// The ID of the next fact set being progressed to
        /// </summary>
        public string NextFactSetId { get; }

        /// <summary>
        /// The number of questions asked for the completed fact set during fluency
        /// </summary>
        public int QuestionCount { get; }

        /// <summary>
        /// The number of facts in the completed fact set
        /// </summary>
        public int FactCount { get; }

        /// <summary>
        /// The overall accuracy percentage for the completed fact set during fluency (0.0 to 1.0)
        /// </summary>
        public float OverallAccuracy { get; }

        /// <summary>
        /// Timestamp when the progression occurred
        /// </summary>
        public DateTimeOffset ProgressionTimestamp { get; }

        public FactSetProgressionInfo(string completedFactSetId, string nextFactSetId, int questionCount, int factCount, float overallAccuracy, DateTimeOffset progressionTimestamp)
        {
            CompletedFactSetId = completedFactSetId ?? throw new ArgumentNullException(nameof(completedFactSetId));
            NextFactSetId = nextFactSetId ?? throw new ArgumentNullException(nameof(nextFactSetId));
            QuestionCount = questionCount;
            FactCount = factCount;
            OverallAccuracy = overallAccuracy;
            ProgressionTimestamp = progressionTimestamp;
        }

        /// <summary>
        /// Creates a snapshot of the current progression info
        /// </summary>
        /// <returns>A new instance with the same values</returns>
        public FactSetProgressionInfo TakeSnapshot()
        {
            return new FactSetProgressionInfo(CompletedFactSetId, NextFactSetId, QuestionCount, FactCount, OverallAccuracy, ProgressionTimestamp);
        }
    }
} 