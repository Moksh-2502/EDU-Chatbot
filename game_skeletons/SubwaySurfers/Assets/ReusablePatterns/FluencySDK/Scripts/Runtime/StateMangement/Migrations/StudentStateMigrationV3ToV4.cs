using System;
using FluencySDK.Versioning;
using UnityEngine;
using UnityEngine.Scripting;
using System.Linq; // Added for .Where() and .Select()

namespace FluencySDK.Migrations
{
    /// <summary>
    /// Migration from V3 (enum-based stages) to V4 (stage ID-based system)
    /// </summary>
    [Preserve]
    public class StudentStateMigrationV3ToV4 : StateMigrationBase<StudentStateV3, StudentState>
    {
        public override int FromVersion => 3;
        public override int ToVersion => 4;

        protected override StudentState PerformMigration(StudentStateV3 source)
        {
            Debug.Log($"[StudentStateMigrationV3ToV4] Starting migration from V3 to V4");

            var target = new StudentState
            {
                Version = ToVersion,
                CreatedAt = source.CreatedAt
            };

            // Migrate fact items
            foreach (var v3Fact in source.Facts)
            {
                var v4Fact = MigrateFactItem(v3Fact);
                target.Facts.Add(v4Fact);
            }

            // Migrate answer history
            foreach (var v3Answer in source.AnswerHistory)
            {
                var v4Answer = MigrateAnswerRecord(v3Answer);
                target.AnswerHistory.Add(v4Answer);
            }

            // Migrate stats (unchanged structure)
            foreach (var statPair in source.Stats)
            {
                target.Stats[statPair.Key] = new FactStats
                {
                    TimesShown = statPair.Value.TimesShown,
                    TimesCorrect = statPair.Value.TimesCorrect,
                    TimesIncorrect = statPair.Value.TimesIncorrect,
                    LastSeenUtcMs = statPair.Value.LastSeenUtcMs
                };
            }

            Debug.Log($"[StudentStateMigrationV3ToV4] Migration completed: {target.Facts.Count} facts, {target.AnswerHistory.Count} answers, {target.Stats.Count} stats");

            return target;
        }

        private FactItem MigrateFactItem(StudentStateV3.FactItemV3 v3Fact)
        {
            var v4Fact = new FactItem
            {
                FactId = v3Fact.FactId,
                FactSetId = v3Fact.FactSetId,
                StageId = MapLearningStageToStageId(v3Fact.Stage, v3Fact.ReviewRepetitionCount, v3Fact.RepetitionCount),
                ConsecutiveCorrect = v3Fact.ConsecutiveCorrect,
                ConsecutiveIncorrect = v3Fact.ConsecutiveIncorrect,
                RandomFactor = v3Fact.RandomFactor
            };

            // Consolidate timing - use the most recent timestamp
            v4Fact.LastAskedTime = GetMostRecentTime(v3Fact.LastAskedTime, v3Fact.LastReviewTime, v3Fact.LastRepetitionTime);

            return v4Fact;
        }

        private AnswerRecord MigrateAnswerRecord(StudentStateV3.AnswerRecordV3 v3Answer)
        {
            var stageId = MapLearningStageToStageId(v3Answer.Stage, v3Answer.ReviewRepetitionCount, 0);
            var wasKnownFact = v3Answer.WasKnownFact; // Preserve the original calculation

            return new AnswerRecord(
                v3Answer.FactId,
                MapAnswerType(v3Answer.AnswerType),
                stageId,
                v3Answer.FactSetId,
                wasKnownFact,
                v3Answer.AnswerTime
            );
        }

        private string MapLearningStageToStageId(StudentStateV3.LearningStageV3 stage, int reviewRepetitionCount, int repetitionCount)
        {
            return stage switch
            {
                StudentStateV3.LearningStageV3.Assessment => "assessment",
                StudentStateV3.LearningStageV3.Grounding => "grounding",
                StudentStateV3.LearningStageV3.PracticeSlow => "practice-slow",
                StudentStateV3.LearningStageV3.PracticeFast => "practice-fast",
                StudentStateV3.LearningStageV3.Review => MapReviewStage(reviewRepetitionCount),
                StudentStateV3.LearningStageV3.Repetition => MapRepetitionStage(repetitionCount),
                StudentStateV3.LearningStageV3.Mastered => "mastered",
                _ => "assessment" // Default fallback
            };
        }

        private string MapReviewStage(int repetitionCount)
        {
            return repetitionCount switch
            {
                0 => "review-1min",
                1 => "review-2min",
                2 => "review-4min",
                _ => "review-1min" // Default to first review stage
            };
        }

        private string MapRepetitionStage(int repetitionCount)
        {
            return repetitionCount switch
            {
                0 => "repetition-1day",
                1 => "repetition-2day",
                2 => "repetition-4day",
                3 => "repetition-1week",
                _ => "repetition-1day" // Default to first repetition stage
            };
        }

        private AnswerType MapAnswerType(StudentStateV3.AnswerTypeV3 v3AnswerType)
        {
            return v3AnswerType switch
            {
                StudentStateV3.AnswerTypeV3.Correct => AnswerType.Correct,
                StudentStateV3.AnswerTypeV3.Incorrect => AnswerType.Incorrect,
                StudentStateV3.AnswerTypeV3.Skipped => AnswerType.Skipped,
                StudentStateV3.AnswerTypeV3.TimedOut => AnswerType.TimedOut,
                _ => AnswerType.Incorrect // Default fallback
            };
        }

        private DateTime? GetMostRecentTime(DateTime? lastAskedTime, DateTime? lastReviewTime, DateTime? lastRepetitionTime)
        {
            var times = new[] { lastAskedTime, lastReviewTime, lastRepetitionTime }
                .Where(t => t.HasValue)
                .Select(t => t.Value);

            return times.Any() ? times.Max() : null;
        }
    }
} 