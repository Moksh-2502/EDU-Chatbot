using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace FluencySDK.Versioning
{
    /// <summary>
    /// Frozen V3 Student State - represents the state structure before stage ID refactor
    /// DO NOT MODIFY - This is a frozen version for migration purposes
    /// </summary>
    [Serializable]
    [Preserve]
    public class StudentStateV3 : IStudentStateVersion
    {
        public const int VersionNumber = 3;
        
        public int Version { get; set; } = VersionNumber;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<FactItemV3> Facts { get; set; } = new List<FactItemV3>();
        public List<AnswerRecordV3> AnswerHistory { get; set; } = new List<AnswerRecordV3>();
        public Dictionary<string, FactStatsV3> Stats { get; set; } = new Dictionary<string, FactStatsV3>();

        public StudentStateV3()
        {
        }

        public StudentStateV3(string factSetId)
        {
            CreatedAt = DateTime.UtcNow;
        }

        public string GetStateSummary()
        {
            return $"V3 State: {Facts.Count} facts, {AnswerHistory.Count} answers, {Stats.Count} stats";
        }

        public List<FactItemV3> GetFactsForSet(string factSetId)
        {
            return Facts.Where(f => f.FactSetId == factSetId).ToList();
        }

        public void AddAnswerRecord(string factId, AnswerTypeV3 answerType, LearningStageV3 stage, string factSetId, DateTime? answerTime = null, int reviewRepetitionCount = 0)
        {
            var record = new AnswerRecordV3(factId, answerType, stage, factSetId, answerTime)
            {
                ReviewRepetitionCount = reviewRepetitionCount
            };
            AnswerHistory.Add(record);
        }

        #region V3 Frozen Data Structures

        /// <summary>
        /// V3 LearningStage enum - FROZEN, do not modify
        /// </summary>
        public enum LearningStageV3
        {
            Assessment,
            Grounding,
            PracticeSlow,
            PracticeFast,
            Review,
            Repetition,
            Mastered
        }

        /// <summary>
        /// V3 AnswerType enum - FROZEN, do not modify
        /// </summary>
        public enum AnswerTypeV3
        {
            Correct,
            Incorrect,
            Skipped,
            TimedOut
        }

        /// <summary>
        /// V3 FactItem class - FROZEN, do not modify
        /// </summary>
        [Serializable]
        [Preserve]
        public class FactItemV3
        {
            public string FactId { get; set; }
            public LearningStageV3 Stage { get; set; }
            public DateTime? LastAskedTime { get; set; }
            public string FactSetId { get; set; }
            public int ConsecutiveCorrect { get; set; }
            public int ConsecutiveIncorrect { get; set; }
            public DateTime? LastReviewTime { get; set; }
            public int ReviewRepetitionCount { get; set; }
            public DateTime? LastRepetitionTime { get; set; }
            public int RepetitionCount { get; set; }
            public float RandomFactor { get; set; }

            public FactItemV3()
            {
            }

            public FactItemV3(string factId, string factSetId, LearningStageV3 stage = LearningStageV3.Assessment)
            {
                FactId = factId;
                FactSetId = factSetId;
                Stage = stage;
                LastAskedTime = null;
                ConsecutiveCorrect = 0;
                ConsecutiveIncorrect = 0;
                LastReviewTime = null;
                ReviewRepetitionCount = 0;
                LastRepetitionTime = null;
                RepetitionCount = 0;
                RandomFactor = 0f;
            }
        }

        /// <summary>
        /// V3 AnswerRecord class - FROZEN, do not modify
        /// </summary>
        [Serializable]
        [Preserve]
        public class AnswerRecordV3
        {
            public string FactId { get; set; }
            public AnswerTypeV3 AnswerType { get; set; }
            public LearningStageV3 Stage { get; set; }
            public DateTime AnswerTime { get; set; }
            public string FactSetId { get; set; }
            public bool WasKnownFact { get; set; }
            public int ReviewRepetitionCount { get; set; }

            public AnswerRecordV3()
            {
            }

            public AnswerRecordV3(string factId, AnswerTypeV3 answerType, LearningStageV3 stage, string factSetId, DateTime? answerTime = null)
            {
                FactId = factId;
                AnswerType = answerType;
                Stage = stage;
                FactSetId = factSetId;
                AnswerTime = answerTime ?? DateTime.Now;
                WasKnownFact = stage == LearningStageV3.Review || stage == LearningStageV3.Repetition;
            }
        }

        /// <summary>
        /// V3 FactStats class - FROZEN, do not modify
        /// </summary>
        [Serializable]
        [Preserve]
        public class FactStatsV3
        {
            public int TimesShown { get; set; }
            public int TimesCorrect { get; set; }
            public int TimesIncorrect { get; set; }
            public long LastSeenUtcMs { get; set; }

            public FactStatsV3()
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