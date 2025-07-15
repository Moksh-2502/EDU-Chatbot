namespace FluencySDK
{
    /// <summary>
    /// Per-student mutable statistics for a fact.
    /// </summary>
    public class FactStats
    {
        /// <summary>
        /// Number of times the fact has been shown to the student.
        /// </summary>
        public int TimesShown { get; set; }

        /// <summary>
        /// Number of times the student answered the fact correctly.
        /// </summary>
        public int TimesCorrect { get; set; }

        /// <summary>
        /// Number of times the student answered the fact incorrectly.
        /// </summary>
        public int TimesIncorrect { get; set; }

        /// <summary>
        /// Timestamp of when the fact was last seen by the student (Unix milliseconds).
        /// </summary>
        public long LastSeenUtcMs { get; set; }

        public FactStats()
        {
            TimesShown = 0;
            TimesCorrect = 0;
            TimesIncorrect = 0;
            LastSeenUtcMs = 0;
        }
    }
} 