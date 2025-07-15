using System;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models
{
    /// <summary>
    /// Combines FactItem with FactStats to provide comprehensive individual fact progress information
    /// Used for detailed drill-down views in the Learning HUD
    /// </summary>
    public class FactItemProgress
    {
        public FactItem FactItem { get; }
        public FactStats Stats { get; }
        private readonly LearningAlgorithmConfig _config;

        public FactItemProgress(FactItem factItem, FactStats stats, LearningAlgorithmConfig config)
        {
            FactItem = factItem ?? throw new ArgumentNullException(nameof(factItem));
            Stats = stats ?? new FactStats(); 
            _config = config;
        }

        /// <summary>
        /// Gets the accuracy rate for this fact (0.0 to 1.0)
        /// </summary>
        public float GetAccuracyRate()
        {
            if (Stats.TimesShown == 0)
                return 0f;
            
            return (float)Stats.TimesCorrect / Stats.TimesShown;
        }

        /// <summary>
        /// Gets the accuracy percentage for display (0-100)
        /// </summary>
        public float GetAccuracyPercentage()
        {
            return GetAccuracyRate() * 100f;
        }

        /// <summary>
        /// Gets the total number of attempts (correct + incorrect)
        /// </summary>
        public int GetTotalAttempts()
        {
            return Stats.TimesCorrect + Stats.TimesIncorrect;
        }

        /// <summary>
        /// Gets the last seen time as DateTime, or null if never seen
        /// </summary>
        public DateTime? GetLastSeenDateTime()
        {
            if (Stats.LastSeenUtcMs == 0)
                return null;

            return DateTimeOffset.FromUnixTimeMilliseconds(Stats.LastSeenUtcMs).DateTime;
        }

        /// <summary>
        /// Gets a human-readable description of when the fact was last seen
        /// </summary>
        public string GetLastSeenDescription()
        {
            var lastSeen = GetLastSeenDateTime();
            if (lastSeen == null)
                return "Never";

            var timeSpan = DateTime.UtcNow - lastSeen.Value;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            
            return lastSeen.Value.ToString("MMM dd, yyyy");
        }

        /// <summary>
        /// Gets the current learning stage name for display
        /// </summary>
        public string GetCurrentStageName()
        {
            return _config.GetStageById(FactItem.StageId)?.DisplayName ?? "Unknown";
        }

        /// <summary>
        /// Gets the current consecutive correct streak for this fact
        /// </summary>
        public int GetConsecutiveCorrect()
        {
            return FactItem.ConsecutiveCorrect;
        }

        /// <summary>
        /// Gets the current consecutive incorrect streak for this fact
        /// </summary>
        public int GetConsecutiveIncorrect()
        {
            return FactItem.ConsecutiveIncorrect;
        }

        /// <summary>
        /// Determines if this fact needs attention (low accuracy or high incorrect streak)
        /// </summary>
        public bool NeedsAttention()
        {
            // Needs attention if accuracy is below 70% and has been attempted at least 5 times
            if (GetTotalAttempts() >= 5 && GetAccuracyRate() < 0.7f)
                return true;

            // Or if consecutive incorrect streak is 3 or more
            if (FactItem.ConsecutiveIncorrect >= 3)
                return true;

            return false;
        }

        /// <summary>
        /// Gets a status description for this fact
        /// </summary>
        public string GetStatusDescription()
        {
            if (_config.GetStageById(FactItem.StageId)?.IsFullyLearned ?? false)
                return "Mastered";

            if (NeedsAttention())
                return "Needs Practice";

            if (GetTotalAttempts() == 0)
                return "Not Started";

            if (GetAccuracyRate() >= 0.8f)
                return "Doing Well";

            return "In Progress";
        }

        /// <summary>
        /// Gets the progress weight for this individual fact (0.0 to 1.0)
        /// </summary>
        public float GetProgressWeight()
        {
            return _config.GetStageById(FactItem.StageId)?.ProgressWeight ?? 0f;
        }
    }
} 