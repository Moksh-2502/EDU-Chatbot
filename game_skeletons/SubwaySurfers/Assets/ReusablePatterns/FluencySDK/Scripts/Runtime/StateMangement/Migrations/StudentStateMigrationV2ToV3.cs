using System;
using System.Collections.Generic;
using System.Linq;
using FluencySDK.Versioning;
using UnityEngine.Scripting;

namespace FluencySDK.Migrations
{
    /// <summary>
    /// Migrates StudentState from V2 to V3
    /// </summary>
    [Preserve]
    public class StudentStateMigrationV2ToV3 : StateMigrationBase<StudentStateV2, StudentStateV3>
    {
        public override int FromVersion => 2;
        public override int ToVersion => 3;

        protected override StudentStateV3 PerformMigration(StudentStateV2 sourceState)
        {
            var v3State = new StudentStateV3
            {
                Version = 3,
                CreatedAt = sourceState.CreatedAt,
                Stats = MigrateStats(sourceState.Stats)
            };

            v3State.Facts = MigrateFacts(sourceState.Facts);
            v3State.AnswerHistory = MigrateAnswerHistory(sourceState.StageAnswers);

            ValidateMigration(sourceState, v3State);

            return v3State;
        }

        private List<StudentStateV3.FactItemV3> MigrateFacts(List<StudentStateV2.FactItemV2> v2Facts)
        {
            var v3Facts = new List<StudentStateV3.FactItemV3>();

            foreach (var v2Fact in v2Facts)
            {
                var v3Fact = new StudentStateV3.FactItemV3
                {
                    FactId = v2Fact.FactId,
                    FactSetId = v2Fact.FactSetId,
                    Stage = MigrateStage(v2Fact.Stage),
                    LastAskedTime = v2Fact.LastAskedTime,
                    ConsecutiveCorrect = v2Fact.ConsecutiveCorrect,
                    ConsecutiveIncorrect = v2Fact.ConsecutiveIncorrect,

                    LastReviewTime = null,
                    ReviewRepetitionCount = 0,
                    LastRepetitionTime = null,
                    RepetitionCount = 0
                };

                v3Facts.Add(v3Fact);
            }

            return v3Facts;
        }

        private StudentStateV3.LearningStageV3 MigrateStage(StudentStateV2.LearningStageV2 v2Stage)
        {
            return v2Stage switch
            {
                StudentStateV2.LearningStageV2.Assessment => StudentStateV3.LearningStageV3.Assessment,
                StudentStateV2.LearningStageV2.Mastery => StudentStateV3.LearningStageV3.Grounding,
                StudentStateV2.LearningStageV2.FluencyBig => StudentStateV3.LearningStageV3.PracticeSlow,
                StudentStateV2.LearningStageV2.FluencySmall => StudentStateV3.LearningStageV3.PracticeFast,
                StudentStateV2.LearningStageV2.Completed => StudentStateV3.LearningStageV3.Mastered,
                _ => StudentStateV3.LearningStageV3.Assessment
            };
        }

        private List<StudentStateV3.AnswerRecordV3> MigrateAnswerHistory(List<StudentStateV2.StageAnswerRecordV2> v2StageAnswers)
        {
            var v3AnswerHistory = new List<StudentStateV3.AnswerRecordV3>();

            foreach (var v2Answer in v2StageAnswers)
            {
                var v3Stage = MigrateStage(v2Answer.Stage);

                var v3Answer = new StudentStateV3.AnswerRecordV3
                {
                    FactId = v2Answer.FactId,
                    AnswerType = MigrateAnswerType(v2Answer.AnswerType),
                    Stage = v3Stage,
                    AnswerTime = v2Answer.AnswerTime,
                    FactSetId = v2Answer.FactSetId,

                    WasKnownFact = false
                };

                v3AnswerHistory.Add(v3Answer);
            }

            return v3AnswerHistory;
        }

        private StudentStateV3.AnswerTypeV3 MigrateAnswerType(StudentStateV2.AnswerTypeV2 v2AnswerType)
        {
            return v2AnswerType switch
            {
                StudentStateV2.AnswerTypeV2.Correct => StudentStateV3.AnswerTypeV3.Correct,
                StudentStateV2.AnswerTypeV2.Incorrect => StudentStateV3.AnswerTypeV3.Incorrect,
                StudentStateV2.AnswerTypeV2.Skipped => StudentStateV3.AnswerTypeV3.Skipped,
                StudentStateV2.AnswerTypeV2.Timeout => StudentStateV3.AnswerTypeV3.TimedOut,
                _ => StudentStateV3.AnswerTypeV3.Incorrect
            };
        }

        private Dictionary<string, StudentStateV3.FactStatsV3> MigrateStats(Dictionary<string, StudentStateV2.FactStatsV2> v2Stats)
        {
            var v3Stats = new Dictionary<string, StudentStateV3.FactStatsV3>();

            foreach (var kvp in v2Stats)
            {
                var v2Stat = kvp.Value;
                var v3Stat = new StudentStateV3.FactStatsV3
                {
                    TimesShown = v2Stat.TimesShown,
                    TimesCorrect = v2Stat.TimesCorrect,
                    TimesIncorrect = v2Stat.TimesIncorrect,
                    LastSeenUtcMs = v2Stat.LastSeenUtcMs
                };

                v3Stats[kvp.Key] = v3Stat;
            }

            return v3Stats;
        }

        private void ValidateMigration(StudentStateV2 sourceState, StudentStateV3 migratedState)
        {
            if (migratedState.Version != 3)
                throw new InvalidOperationException($"Expected version 3, got {migratedState.Version}");

            if (migratedState.CreatedAt != sourceState.CreatedAt)
                throw new InvalidOperationException("CreatedAt should be preserved");

            if (migratedState.Facts.Count != sourceState.Facts.Count)
                throw new InvalidOperationException($"Facts count mismatch: expected {sourceState.Facts.Count}, got {migratedState.Facts.Count}");

            if (migratedState.AnswerHistory.Count != sourceState.StageAnswers.Count)
                throw new InvalidOperationException($"AnswerHistory count mismatch: expected {sourceState.StageAnswers.Count}, got {migratedState.AnswerHistory.Count}");

            if (migratedState.Stats.Count != sourceState.Stats.Count)
                throw new InvalidOperationException($"Stats count mismatch: expected {sourceState.Stats.Count}, got {migratedState.Stats.Count}");

            foreach (var v3Fact in migratedState.Facts)
            {
                var v2Fact = sourceState.Facts.FirstOrDefault(f => f.FactId == v3Fact.FactId);
                if (v2Fact == null)
                    throw new InvalidOperationException($"FactId {v3Fact.FactId} not found in V2 state");

                var expectedV3Stage = MigrateStage(v2Fact.Stage);
                if (v3Fact.Stage != expectedV3Stage)
                    throw new InvalidOperationException($"Stage migration failed for fact {v3Fact.FactId}: expected {expectedV3Stage}, got {v3Fact.Stage}");

                if (v3Fact.LastReviewTime != null || v3Fact.ReviewRepetitionCount != 0 ||
                    v3Fact.LastRepetitionTime != null || v3Fact.RepetitionCount != 0)
                {
                    throw new InvalidOperationException($"Reinforcement fields should be initialized to defaults for fact {v3Fact.FactId}");
                }
            }

            foreach (var answerRecord in migratedState.AnswerHistory)
            {
                if (answerRecord.WasKnownFact)
                    throw new InvalidOperationException($"All V2 answers should migrate with WasKnownFact = false");
            }
        }
    }
}