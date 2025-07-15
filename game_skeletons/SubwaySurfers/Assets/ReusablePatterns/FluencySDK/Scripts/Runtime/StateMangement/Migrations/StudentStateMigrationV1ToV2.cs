using System.Collections.Generic;
using System.Linq;
using FluencySDK.Versioning;
using UnityEngine;
using UnityEngine.Scripting;

namespace FluencySDK.Migrations
{
    /// <summary>
    /// Migration from StudentStateV1 to StudentStateV2
    /// </summary>
    [Preserve]
    public class StudentStateMigrationV1ToV2 : StateMigrationBase<StudentStateV1, StudentStateV2>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 2;

        protected override StudentStateV2 PerformMigration(StudentStateV1 source)
        {
            Debug.Log("[FluencyMigration] V1ToV2: Starting migration from V1 to V2");

            var v2State = new StudentStateV2
            {
                Version = ToVersion,
                CreatedAt = source.CreatedAt,
                NextFactSetToLoad = source.CurrentFactSetId,
                Facts = ConvertFactsV1ToV2(source.Facts),
                StageAnswers = ConvertStageAnswersV1ToV2(source.StageAnswers),
                Stats = ConvertStatsV1ToV2(source.Stats)
            };

            Debug.Log($"[FluencyMigration] V1ToV2: Migration completed.");

            return v2State;
        }

        private List<StudentStateV2.FactItemV2> ConvertFactsV1ToV2(List<StudentStateV1.FactItemV1> v1Facts)
        {
            var v2Facts = new List<StudentStateV2.FactItemV2>();

            foreach (var v1Fact in v1Facts)
            {
                var v2Fact = new StudentStateV2.FactItemV2
                {
                    FactId = v1Fact.FactId,
                    FactSetId = v1Fact.FactSetId,
                    Stage = ConvertStageV1ToV2(v1Fact.Stage),
                    LastAskedTime = v1Fact.LastAskedTime,
                    ConsecutiveCorrect = v1Fact.ConsecutiveCorrect,
                    ConsecutiveIncorrect = v1Fact.ConsecutiveIncorrect
                };

                v2Facts.Add(v2Fact);
            }

            return v2Facts;
        }

        private List<StudentStateV2.StageAnswerRecordV2> ConvertStageAnswersV1ToV2(List<StudentStateV1.StageAnswerRecordV1> v1Answers)
        {
            var v2Answers = new List<StudentStateV2.StageAnswerRecordV2>();

            foreach (var v1Answer in v1Answers)
            {
                var v2Answer = new StudentStateV2.StageAnswerRecordV2
                {
                    FactId = v1Answer.FactId,
                    AnswerType = ConvertAnswerTypeV1ToV2(v1Answer.AnswerType),
                    Stage = ConvertStageV1ToV2(v1Answer.Stage),
                    AnswerTime = v1Answer.AnswerTime,
                    FactSetId = v1Answer.FactSetId
                };

                v2Answers.Add(v2Answer);
            }

            return v2Answers;
        }

        private Dictionary<string, StudentStateV2.FactStatsV2> ConvertStatsV1ToV2(Dictionary<string, StudentStateV1.FactStatsV1> v1Stats)
        {
            var v2Stats = new Dictionary<string, StudentStateV2.FactStatsV2>();

            foreach (var kvp in v1Stats)
            {
                var v1Stat = kvp.Value;
                var v2Stat = new StudentStateV2.FactStatsV2
                {
                    TimesShown = v1Stat.TimesShown,
                    TimesCorrect = v1Stat.TimesCorrect,
                    TimesIncorrect = v1Stat.TimesIncorrect,
                    LastSeenUtcMs = v1Stat.LastSeenUtcMs
                };

                v2Stats[kvp.Key] = v2Stat;
            }

            return v2Stats;
        }

        private StudentStateV2.LearningStageV2 ConvertStageV1ToV2(StudentStateV1.LearningStageV1 v1Stage)
        {
            return v1Stage switch
            {
                StudentStateV1.LearningStageV1.Assessment => StudentStateV2.LearningStageV2.Assessment,
                StudentStateV1.LearningStageV1.Mastery => StudentStateV2.LearningStageV2.Mastery,
                StudentStateV1.LearningStageV1.FluencyBig => StudentStateV2.LearningStageV2.FluencyBig,
                StudentStateV1.LearningStageV1.FluencySmall => StudentStateV2.LearningStageV2.FluencySmall,
                StudentStateV1.LearningStageV1.Completed => StudentStateV2.LearningStageV2.Completed,
                _ => StudentStateV2.LearningStageV2.Assessment
            };
        }

        private StudentStateV2.AnswerTypeV2 ConvertAnswerTypeV1ToV2(StudentStateV1.AnswerTypeV1 v1AnswerType)
        {
            return v1AnswerType switch
            {
                StudentStateV1.AnswerTypeV1.Correct => StudentStateV2.AnswerTypeV2.Correct,
                StudentStateV1.AnswerTypeV1.Incorrect => StudentStateV2.AnswerTypeV2.Incorrect,
                StudentStateV1.AnswerTypeV1.Skipped => StudentStateV2.AnswerTypeV2.Skipped,
                StudentStateV1.AnswerTypeV1.Timeout => StudentStateV2.AnswerTypeV2.Timeout,
                _ => StudentStateV2.AnswerTypeV2.Incorrect
            };
        }
    }
}