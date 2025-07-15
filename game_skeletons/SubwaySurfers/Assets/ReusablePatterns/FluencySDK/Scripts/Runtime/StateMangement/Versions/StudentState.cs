using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using FluencySDK.Versioning;
using FluencySDK.Serialization;
using UnityEngine.Scripting;

namespace FluencySDK
{
    [JsonConverter(typeof(StudentStateJsonConverter))]
    [Preserve]
    public class StudentState : IStudentStateVersion
    {
        public const int LatestVersion = 4;

        public int Version { get; set; } = LatestVersion;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Dictionary<string, FactStats> Stats { get; set; } = new();

        /// <summary>
        /// All facts across all fact sets with their current stages
        /// </summary>
        public List<FactItem> Facts { get; set; } = new();

        /// <summary>
        /// Unified answer history for analytics, ratio tracking, and statistics
        /// </summary>
        public List<AnswerRecord> AnswerHistory { get; set; } = new();

        public StudentState()
        {
            InitializeReferences();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeReferences();
        }

        public bool IsValid()
        {
            return Facts != null &&
                   AnswerHistory != null &&
                   Stats != null;
        }

        public string GetStateSummary()
        {
            return $"StudentState: Version={Version}, CreatedAt={CreatedAt:yyyy-MM-dd HH:mm:ss}, " +
                   $"FactsCount={Facts?.Count ?? 0}, " +
                   $"AnswerHistoryCount={AnswerHistory?.Count ?? 0}, StatsCount={Stats?.Count ?? 0}";
        }

        private void InitializeReferences()
        {
            if (Facts == null) Facts = new List<FactItem>();
            if (AnswerHistory == null) AnswerHistory = new List<AnswerRecord>();
            if (Stats == null) Stats = new Dictionary<string, FactStats>();
        }

        public List<FactItem> GetFactsForSet(string factSetId)
        {
            return Facts.Where(f => f.FactSetId == factSetId).ToList();
        }

        public List<FactItem> GetFactsForSetAndStage(string factSetId, string stageId)
        {
            return Facts.Where(f => f.FactSetId == factSetId && f.StageId == stageId).ToList();
        }

        /// <summary>
        /// Get recent questions for 80-20 ratio tracking
        /// </summary>
        public List<AnswerRecord> GetRecentQuestions(int count = 5)
        {
            return AnswerHistory
                .OrderByDescending(a => a.AnswerTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get known fact ratio from recent questions
        /// </summary>
        public float GetRecentKnownFactRatio(int count = 5)
        {
            var recentQuestions = GetRecentQuestions(count);
            if (recentQuestions.Count == 0) return 0f;


            var knownCount = recentQuestions.Count(q => q.WasKnownFact);
            return (float)knownCount / recentQuestions.Count;
        }

        /// <summary>
        /// Get unknown facts in time window for capping
        /// </summary>
        public int GetUnknownFactsInTimeWindow(DateTime currentTime, float windowSeconds = 20f)
        {
            var windowStart = currentTime.AddSeconds(-windowSeconds);
            return AnswerHistory
                .Count(a => !a.WasKnownFact && a.AnswerTime >= windowStart);
        }

        /// <summary>
        /// Get consecutive correct answers for stage and fact set
        /// </summary>
        public int GetConsecutiveCorrectForStageAndSet(string stageId, string factSetId)
        {
            var relevantAnswers = AnswerHistory
                .Where(a => a.StageId == stageId && a.FactSetId == factSetId)
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int consecutive = 0;
            foreach (var answer in relevantAnswers)
            {
                if (answer.AnswerType == AnswerType.Correct)
                    consecutive++;
                else
                    break;
            }

            return consecutive;
        }

        public bool HasFactsInLowerStages(string factSetId, string currentStageId, LearningAlgorithmConfig config)
        {
            var currentStage = config.GetStageById(currentStageId);
            if (currentStage == null) return false;

            var factsInSet = GetFactsForSet(factSetId);
            return factsInSet.Any(f => {
                var factStage = config.GetStageById(f.StageId);
                return factStage != null && factStage.Order < currentStage.Order;
            });
        }

        public void InitializeFactSet(string factSetId, IEnumerable<string> factIds)
        {
            Facts.RemoveAll(f => f.FactSetId == factSetId);

            foreach (var factId in factIds)
            {
                Facts.Add(new FactItem(factId, factSetId, "assessment"));
            }
        }

        public int GetFactSetPosition(string factSetId, string[] factSetOrder)
        {
            var index = Array.IndexOf(factSetOrder, factSetId);
            return index >= 0 ? index : int.MaxValue;
        }

        /// <summary>
        /// Get persistent correct streak from unified answer history
        /// </summary>
        public int GetPersistentCorrectStreak()
        {
            var allAnswers = AnswerHistory
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int streak = 0;
            foreach (var answer in allAnswers)
            {
                if (answer.AnswerType == AnswerType.Correct)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        public bool IsCorrectStreakThresholdReached(int threshold)
        {
            var streak = GetPersistentCorrectStreak();
            return streak > 0 && streak % threshold == 0;
        }

        /// <summary>
        /// Get persistent incorrect streak from unified answer history
        /// </summary>
        public int GetPersistentIncorrectStreak()
        {
            var allAnswers = AnswerHistory
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int streak = 0;
            foreach (var answer in allAnswers)
            {
                if (answer.AnswerType == AnswerType.Incorrect)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        /// <summary>
        /// Add answer to unified history
        /// </summary>
        public void AddAnswerRecord(string factId, AnswerType answerType, string stageId, string factSetId, DateTime? answerTime = null, bool wasKnownFact = false)
        {
            var answerRecord = new AnswerRecord(factId, answerType, stageId, factSetId, wasKnownFact, answerTime);
            AnswerHistory.Add(answerRecord);
        }

        /// <summary>
        /// Clean up old answer history to prevent unlimited growth
        /// </summary>
        public void TrimAnswerHistory(int maxRecords = 1000)
        {
            if (AnswerHistory.Count > maxRecords)
            {
                AnswerHistory = AnswerHistory
                    .OrderByDescending(a => a.AnswerTime)
                    .Take(maxRecords)
                    .ToList();
            }
        }

        /// <summary>
        /// Get recent answers for dynamic difficulty calculation
        /// </summary>
        public List<AnswerRecord> GetRecentAnswers(int count)
        {
            return AnswerHistory
                .OrderByDescending(a => a.AnswerTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get recent answers for a specific fact set (used for bulk promotion)
        /// </summary>
        public List<AnswerRecord> GetRecentAnswersForFactSet(string factSetId, int count)
        {
            return AnswerHistory
                .Where(a => a.FactSetId == factSetId)
                .OrderByDescending(a => a.AnswerTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Calculate what percentage of a fact set has been covered by answers
        /// </summary>
        public float GetFactSetCoverage(string factSetId)
        {
            var factsInSet = Facts.Where(f => f.FactSetId == factSetId).ToList();
            if (factsInSet.Count == 0) return 0f;

            var answeredFactIds = AnswerHistory
                .Where(a => a.FactSetId == factSetId)
                .Select(a => a.FactId)
                .Distinct()
                .ToHashSet();

            return (float)answeredFactIds.Count / factsInSet.Count;
        }

        /// <summary>
        /// Check if all shown facts in a fact set are at a specific stage or above
        /// </summary>
        public bool AllShownFactsAtStageOrAbove(string factSetId, string minStageId, LearningAlgorithmConfig config)
        {
            var minStage = config.GetStageById(minStageId);
            if (minStage == null) return false;

            // Get all facts that have been shown (have answer history)
            var shownFactIds = AnswerHistory
                .Where(a => a.FactSetId == factSetId)
                .Select(a => a.FactId)
                .Distinct()
                .ToHashSet();

            if (shownFactIds.Count == 0) return true; // No facts shown yet

            var factsInSet = GetFactsForSet(factSetId);
            var shownFacts = factsInSet.Where(f => shownFactIds.Contains(f.FactId));

            return shownFacts.All(f => {
                var factStage = config.GetStageById(f.StageId);
                return factStage != null && factStage.Order >= minStage.Order;
            });
        }
    }
}