using System;

namespace FluencySDK
{
    /// <summary>
    /// Contains information about regression from fluency to mastery mode
    /// </summary>
    public class FluencyToMasteryRegressionInfo
    {
        /// <summary>
        /// The ID of the fact set where regression occurred
        /// </summary>
        public string FactSetId { get; }

        /// <summary>
        /// The number of questions asked for this fact set during fluency before regression
        /// </summary>
        public int QuestionCount { get; }

        /// <summary>
        /// The number of facts in the fact set
        /// </summary>
        public int FactCount { get; }

        /// <summary>
        /// The overall accuracy percentage for this fact set during fluency that caused regression (0.0 to 1.0)
        /// </summary>
        public float OverallAccuracy { get; }

        /// <summary>
        /// Timestamp when the regression occurred
        /// </summary>
        public DateTimeOffset RegressionTimestamp { get; }

        public FluencyToMasteryRegressionInfo(string factSetId, int questionCount, int factCount, float overallAccuracy, DateTimeOffset regressionTimestamp)
        {
            FactSetId = factSetId ?? throw new ArgumentNullException(nameof(factSetId));
            QuestionCount = questionCount;
            FactCount = factCount;
            OverallAccuracy = overallAccuracy;
            RegressionTimestamp = regressionTimestamp;
        }

        /// <summary>
        /// Creates a snapshot of the current regression info
        /// </summary>
        /// <returns>A new instance with the same values</returns>
        public FluencyToMasteryRegressionInfo TakeSnapshot()
        {
            return new FluencyToMasteryRegressionInfo(FactSetId, QuestionCount, FactCount, OverallAccuracy, RegressionTimestamp);
        }
    }
} 