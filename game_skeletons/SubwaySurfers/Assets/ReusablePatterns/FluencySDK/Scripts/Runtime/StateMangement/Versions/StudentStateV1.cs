using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace FluencySDK.Versioning
{
    /// <summary>
    /// Version 1 of StudentState
    /// </summary>
    [Serializable]
    [Preserve]
    public class StudentStateV1 : IStudentStateVersion
    {
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CurrentFactSetId { get; set; }

        public Dictionary<string, FactStatsV1> Stats { get; set; } = new();

        /// <summary>
        /// All facts across all fact sets with their current stages
        /// </summary>
        public List<FactItemV1> Facts { get; set; } = new();

        /// <summary>
        /// Answer tracking for bulk promotion detection
        /// </summary>
        public List<StageAnswerRecordV1> StageAnswers { get; set; } = new();

        public StudentStateV1()
        {
            InitializeReferences();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeReferences();
        }

        public StudentStateV1(string initialFactSetId)
        {
            CurrentFactSetId = initialFactSetId;
            InitializeReferences();
        }

        private void InitializeReferences()
        {
            Facts ??= new List<FactItemV1>();
            StageAnswers ??= new List<StageAnswerRecordV1>();
            Stats ??= new Dictionary<string, FactStatsV1>();
        }

        public bool IsValid()
        {
            return Facts != null &&
                   StageAnswers != null &&
                   Stats != null &&
                   !string.IsNullOrEmpty(CurrentFactSetId);
        }

        public string GetStateSummary()
        {
            return $"StudentStateV1: Version={Version}, CreatedAt={CreatedAt:yyyy-MM-dd HH:mm:ss}, " +
                   $"CurrentFactSetId={CurrentFactSetId}, FactsCount={Facts?.Count ?? 0}, " +
                   $"AnswersCount={StageAnswers?.Count ?? 0}, StatsCount={Stats?.Count ?? 0}";
        }

        // Business logic methods from original StudentState
        public List<FactItemV1> GetFactsForSet(string factSetId)
        {
            return Facts.Where(f => f.FactSetId == factSetId).ToList();
        }

        public List<FactItemV1> GetFactsForSetAndStage(string factSetId, LearningStageV1 stage)
        {
            return Facts.Where(f => f.FactSetId == factSetId && f.Stage == stage).ToList();
        }

        public int GetConsecutiveCorrectForStageAndSet(LearningStageV1 stage, string factSetId)
        {
            var relevantAnswers = StageAnswers
                .Where(a => a.Stage == stage && a.FactSetId == factSetId)
                .OrderByDescending(a => a.AnswerTime)
                .ToList();

            int consecutive = 0;
            foreach (var answer in relevantAnswers)
            {
                if (answer.AnswerType == AnswerTypeV1.Correct)
                    consecutive++;
                else
                    break;
            }

            return consecutive;
        }

        public bool HasFactsInLowerStages(string factSetId, LearningStageV1 currentStage)
        {
            var lowerStages = LearningStageProgressionV1.GetLowerStages(currentStage);
            return Facts.Any(f => f.FactSetId == factSetId && lowerStages.Contains(f.Stage));
        }

        public void InitializeFactSet(string factSetId, IEnumerable<string> factIds)
        {
            Facts.RemoveAll(f => f.FactSetId == factSetId);

            foreach (var factId in factIds)
            {
                Facts.Add(new FactItemV1(factId, factSetId, LearningStageV1.Assessment));
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
                if (answer.AnswerType == AnswerTypeV1.Correct)
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
                if (answer.AnswerType == AnswerTypeV1.Incorrect)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        #region Local Data Structures

        /// <summary>
        /// V1 Learning stage enum - FROZEN, do not modify
        /// </summary>
        public enum LearningStageV1
        {
            Assessment,    // 5s timer, scatter questions
            Mastery,       // untimed, blocking questions
            FluencyBig,    // 5s timer, speed questions
            FluencySmall,  // 2.5s timer, high-speed questions
            Completed      // fact has been mastered and removed from active pool
        }

        /// <summary>
        /// V1 Answer type enum - FROZEN, do not modify
        /// </summary>
        public enum AnswerTypeV1
        {
            Correct,
            Incorrect,
            Skipped,
            Timeout
        }

        /// <summary>
        /// V1 Learning stage progression - FROZEN, do not modify
        /// </summary>
        public static class LearningStageProgressionV1
        {
            public static readonly LearningStageV1[] ProgressionOrder =
            {
                LearningStageV1.Mastery,
                LearningStageV1.Assessment,
                LearningStageV1.FluencyBig,
                LearningStageV1.FluencySmall,
                LearningStageV1.Completed
            };

            public static LearningStageV1 GetNextStage(LearningStageV1 currentStage)
            {
                var currentIndex = Array.IndexOf(ProgressionOrder, currentStage);
                if (currentIndex >= 0 && currentIndex < ProgressionOrder.Length - 1)
                {
                    return ProgressionOrder[currentIndex + 1];
                }
                return LearningStageV1.Completed;
            }

            public static int GetStageOrder(LearningStageV1 stage)
            {
                var index = Array.IndexOf(ProgressionOrder, stage);
                return index >= 0 ? index : 999;
            }

            public static List<LearningStageV1> GetLowerStages(LearningStageV1 stage)
            {
                var currentIndex = Array.IndexOf(ProgressionOrder, stage);
                if (currentIndex <= 0)
                {
                    return new List<LearningStageV1>();
                }

                return ProgressionOrder.Take(currentIndex).ToList();
            }
        }

        /// <summary>
        /// V1 Fact item - FROZEN, do not modify
        /// </summary>
        [Serializable]
        public class FactItemV1
        {
            public string FactId { get; set; }
            public LearningStageV1 Stage { get; set; }
            public DateTime? LastAskedTime { get; set; }
            public string FactSetId { get; set; }
            public int ConsecutiveCorrect { get; set; }
            public int ConsecutiveIncorrect { get; set; }

            public FactItemV1()
            {
            }

            public FactItemV1(string factId, string factSetId, LearningStageV1 stage = LearningStageV1.Assessment)
            {
                FactId = factId;
                FactSetId = factSetId;
                Stage = stage;
                LastAskedTime = null;
                ConsecutiveCorrect = 0;
                ConsecutiveIncorrect = 0;
            }

            public void UpdateStreak(AnswerTypeV1 answerType)
            {
                if (answerType == AnswerTypeV1.Correct)
                {
                    ConsecutiveCorrect++;
                    ConsecutiveIncorrect = 0;
                }
                else if (answerType == AnswerTypeV1.Incorrect)
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
        /// V1 Stage answer record - FROZEN, do not modify
        /// </summary>
        [Serializable]
        public class StageAnswerRecordV1
        {
            public string FactId { get; set; }
            public AnswerTypeV1 AnswerType { get; set; }
            public LearningStageV1 Stage { get; set; }
            public DateTime AnswerTime { get; set; }
            public string FactSetId { get; set; }

            public StageAnswerRecordV1()
            {
            }

            public StageAnswerRecordV1(string factId, AnswerTypeV1 answerType, LearningStageV1 stage, string factSetId)
            {
                FactId = factId;
                AnswerType = answerType;
                Stage = stage;
                FactSetId = factSetId;
                AnswerTime = DateTime.Now;
            }
        }

        /// <summary>
        /// V1 Fact stats - FROZEN, do not modify
        /// </summary>
        [Serializable]
        public class FactStatsV1
        {
            public int TimesShown { get; set; }
            public int TimesCorrect { get; set; }
            public int TimesIncorrect { get; set; }
            public long LastSeenUtcMs { get; set; }

            public FactStatsV1()
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