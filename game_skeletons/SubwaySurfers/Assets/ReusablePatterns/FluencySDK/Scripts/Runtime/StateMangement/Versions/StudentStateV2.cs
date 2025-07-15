using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace FluencySDK.Versioning
{
    /// <summary>
    /// Version 2 of StudentState
    /// </summary>
    [Serializable]
    [Preserve]
    public class StudentStateV2 : IStudentStateVersion
    {
        public int Version { get; set; } = 2;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Next fact set to load when more facts are needed (progression marker)
        /// </summary>
        public string NextFactSetToLoad { get; set; }

        public Dictionary<string, FactStatsV2> Stats { get; set; } = new();

        /// <summary>
        /// All facts across all fact sets with their current stages
        /// </summary>
        public List<FactItemV2> Facts { get; set; } = new();

        /// <summary>
        /// Answer tracking for bulk promotion detection
        /// </summary>
        public List<StageAnswerRecordV2> StageAnswers { get; set; } = new();

        public StudentStateV2()
        {
            InitializeReferences();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeReferences();
        }

        public StudentStateV2(string initialFactSetId)
        {
            NextFactSetToLoad = initialFactSetId;
            InitializeReferences();
        }

        private void InitializeReferences()
        {
            Facts ??= new List<FactItemV2>();
            StageAnswers ??= new List<StageAnswerRecordV2>();
            Stats ??= new Dictionary<string, FactStatsV2>();
        }

        public bool IsValid()
        {
            return Facts != null &&
                   StageAnswers != null &&
                   Stats != null &&
                   !string.IsNullOrEmpty(NextFactSetToLoad);
        }

        public string GetStateSummary()
        {
            return $"StudentStateV2: Version={Version}, CreatedAt={CreatedAt:yyyy-MM-dd HH:mm:ss}, " +
                   $"NextFactSet={NextFactSetToLoad}, FactsCount={Facts?.Count ?? 0}, " +
                   $"AnswersCount={StageAnswers?.Count ?? 0}, StatsCount={Stats?.Count ?? 0}";
        }

        // Business logic methods from V2 StudentState
        public List<FactItemV2> GetFactsForSet(string factSetId)
        {
            return Facts.Where(f => f.FactSetId == factSetId).ToList();
        }

        public List<FactItemV2> GetFactsForSetAndStage(string factSetId, LearningStageV2 stage)
        {
            return Facts.Where(f => f.FactSetId == factSetId && f.Stage == stage).ToList();
        }

        public int GetConsecutiveCorrectForStageAndSet(LearningStageV2 stage, string factSetId)
        {
            var relevantAnswers = StageAnswers
                .Where(a => a.Stage == stage && a.FactSetId == factSetId)
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int consecutive = 0;
            foreach (var answer in relevantAnswers)
            {
                if (answer.AnswerType == AnswerTypeV2.Correct)
                    consecutive++;
                else
                    break;
            }

            return consecutive;
        }

        public bool HasFactsInLowerStages(string factSetId, LearningStageV2 currentStage)
        {
            var lowerStages = LearningStageProgressionV2.GetLowerStages(currentStage);
            return Facts.Any(f => f.FactSetId == factSetId && lowerStages.Contains(f.Stage));
        }

        public void InitializeFactSet(string factSetId, IEnumerable<string> factIds)
        {
            Facts.RemoveAll(f => f.FactSetId == factSetId);

            foreach (var factId in factIds)
            {
                Facts.Add(new FactItemV2(factId, factSetId, LearningStageV2.Assessment));
            }
        }

        public int GetFactSetPosition(string factSetId, string[] factSetOrder)
        {
            var index = Array.IndexOf(factSetOrder, factSetId);
            return index >= 0 ? index : int.MaxValue;
        }

        public int GetPersistentCorrectStreak()
        {
            var allAnswers = StageAnswers
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int streak = 0;
            foreach (var answer in allAnswers)
            {
                if (answer.AnswerType == AnswerTypeV2.Correct)
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

        public int GetPersistentIncorrectStreak()
        {
            var allAnswers = StageAnswers
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int streak = 0;
            foreach (var answer in allAnswers)
            {
                if (answer.AnswerType == AnswerTypeV2.Incorrect)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        #region Local Data Structures

        /// <summary>
        /// V2 Learning stage enum
        /// </summary>
        public enum LearningStageV2
        {
            Assessment,    // 5s timer, scatter questions
            Mastery,       // untimed, blocking questions
            FluencyBig,    // 5s timer, speed questions
            FluencySmall,  // 2.5s timer, high-speed questions
            Completed      // fact has been mastered and removed from active pool
        }

        /// <summary>
        /// V2 Answer type enum
        /// </summary>
        public enum AnswerTypeV2
        {
            Correct,
            Incorrect,
            Skipped,
            Timeout
        }

        /// <summary>
        /// V2 Learning stage progression
        /// </summary>
        public static class LearningStageProgressionV2
        {
            public static readonly LearningStageV2[] ProgressionOrder =
            {
                LearningStageV2.Mastery,
                LearningStageV2.Assessment,
                LearningStageV2.FluencyBig,
                LearningStageV2.FluencySmall,
                LearningStageV2.Completed
            };

            public static LearningStageV2 GetNextStage(LearningStageV2 currentStage)
            {
                var currentIndex = Array.IndexOf(ProgressionOrder, currentStage);
                if (currentIndex >= 0 && currentIndex < ProgressionOrder.Length - 1)
                {
                    return ProgressionOrder[currentIndex + 1];
                }
                return LearningStageV2.Completed;
            }

            public static int GetStageOrder(LearningStageV2 stage)
            {
                var index = Array.IndexOf(ProgressionOrder, stage);
                return index >= 0 ? index : 999;
            }

            public static List<LearningStageV2> GetLowerStages(LearningStageV2 stage)
            {
                var currentIndex = Array.IndexOf(ProgressionOrder, stage);
                if (currentIndex <= 0)
                {
                    return new List<LearningStageV2>();
                }

                return ProgressionOrder.Take(currentIndex).ToList();
            }
        }

        /// <summary>
        /// V2 Fact item
        /// </summary>
        [Serializable]
        public class FactItemV2
        {
            public string FactId { get; set; }
            public LearningStageV2 Stage { get; set; }
            public DateTime? LastAskedTime { get; set; }
            public string FactSetId { get; set; }
            public int ConsecutiveCorrect { get; set; }
            public int ConsecutiveIncorrect { get; set; }

            public FactItemV2()
            {
            }

            public FactItemV2(string factId, string factSetId, LearningStageV2 stage = LearningStageV2.Assessment)
            {
                FactId = factId;
                FactSetId = factSetId;
                Stage = stage;
                LastAskedTime = null;
                ConsecutiveCorrect = 0;
                ConsecutiveIncorrect = 0;
            }

            public void UpdateStreak(AnswerTypeV2 answerType)
            {
                if (answerType == AnswerTypeV2.Correct)
                {
                    ConsecutiveCorrect++;
                    ConsecutiveIncorrect = 0;
                }
                else if (answerType == AnswerTypeV2.Incorrect)
                {
                    ConsecutiveIncorrect++;
                    ConsecutiveCorrect = 0;
                }
            }

            public void ResetStreak()
            {
                ConsecutiveCorrect = 0;
                ConsecutiveIncorrect = 0;
            }
        }

        /// <summary>
        /// V2 Stage answer record
        /// </summary>
        [Serializable]
        public class StageAnswerRecordV2
        {
            public string FactId { get; set; }
            public AnswerTypeV2 AnswerType { get; set; }
            public LearningStageV2 Stage { get; set; }
            public DateTime AnswerTime { get; set; }
            public string FactSetId { get; set; }

            public StageAnswerRecordV2()
            {
            }

            public StageAnswerRecordV2(string factId, AnswerTypeV2 answerType, LearningStageV2 stage, string factSetId)
            {
                FactId = factId;
                AnswerType = answerType;
                Stage = stage;
                FactSetId = factSetId;
                AnswerTime = DateTime.Now;
            }
        }

        /// <summary>
        /// V2 Fact stats
        /// </summary>
        [Serializable]
        public class FactStatsV2
        {
            public int TimesShown { get; set; }
            public int TimesCorrect { get; set; }
            public int TimesIncorrect { get; set; }
            public long LastSeenUtcMs { get; set; }

            public FactStatsV2()
            {
                TimesShown = 0;
                TimesCorrect = 0;
                TimesIncorrect = 0;
                LastSeenUtcMs = 0;
            }
        }

        #endregion
    }
}