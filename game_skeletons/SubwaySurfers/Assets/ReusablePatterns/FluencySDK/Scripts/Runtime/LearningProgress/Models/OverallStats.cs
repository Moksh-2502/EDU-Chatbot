using System.Collections.Generic;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models
{
    /// <summary>
    /// Comprehensive statistics data structure
    /// </summary>
    public class OverallStats
    {
        public int TotalFactSets { get; set; }
        public int CompletedFactSets { get; set; }
        public int TotalFacts { get; set; }
        public int CompletedFacts { get; set; }
        public float OverallProgressPercent { get; set; }
        public int CurrentStreak { get; set; }
        public Dictionary<LearningStage, int> StageDistribution { get; set; } = new();
        public int TotalAttempts { get; set; }
        public float OverallAccuracy { get; set; }
        public int StrugglingFactsCount { get; set; }
        public int MasteredFactsCount { get; set; }
    }
}