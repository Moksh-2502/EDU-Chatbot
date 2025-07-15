using System;

namespace FluencySDK
{
    /// <summary>
    /// Contains information about progression from mastery to fluency mode
    /// </summary>
    public class MasteryToFluencyProgressionInfo
    {
        /// <summary>
        /// The ID of the fact set that was progressed
        /// </summary>
        public string FactSetId { get; }

        /// <summary>
        /// The number of questions asked for this fact set during mastery
        /// </summary>
        public int QuestionCount { get; }

        /// <summary>
        /// The number of facts in the fact set
        /// </summary>
        public int FactCount { get; }

        /// <summary>
        /// The overall accuracy percentage for this fact set during mastery (0.0 to 1.0)
        /// </summary>
        public float OverallAccuracy { get; }

        /// <summary>
        /// Timestamp when the progression occurred
        /// </summary>
        public DateTimeOffset ProgressionTimestamp { get; }

        public MasteryToFluencyProgressionInfo(string factSetId, int questionCount, int factCount, float overallAccuracy, DateTimeOffset progressionTimestamp)
        {
            FactSetId = factSetId ?? throw new ArgumentNullException(nameof(factSetId));
            QuestionCount = questionCount;
            FactCount = factCount;
            OverallAccuracy = overallAccuracy;
            ProgressionTimestamp = progressionTimestamp;
        }

        /// <summary>
        /// Creates a snapshot of the current progression info
        /// </summary>
        /// <returns>A new instance with the same values</returns>
        public MasteryToFluencyProgressionInfo TakeSnapshot()
        {
            return new MasteryToFluencyProgressionInfo(FactSetId, QuestionCount, FactCount, OverallAccuracy, ProgressionTimestamp);
        }
    }
} 