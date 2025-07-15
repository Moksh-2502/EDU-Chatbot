using System;
using UnityEngine;

namespace FluencySDK.Algorithm
{
    public class QuestionFactory
    {
        private readonly LearningAlgorithmConfig _config;

        public QuestionFactory(LearningAlgorithmConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Question CreateQuestionForStage(Fact fact, LearningStage stage)
        {
            int correctAnswer = fact.FactorA * fact.FactorB;
            var learningMode = ConvertStageToLearningMode(stage);

            var choices = LearningAlgorithmUtils.GenerateContextAwareAnswerOptions(
                fact,
                correctAnswer,
                stage.Type,
                learningMode,
                _config.MaxMultiplicationFactor,
                _config.DistractorConfig
            );

            float? timeToAnswer = stage.TimerSeconds;

            return new Question(fact)
            {
                Id = Guid.NewGuid().ToString(),
                Text = fact.Text,
                Choices = choices,
                TimeToAnswer = timeToAnswer,
                LearningMode = learningMode,
                LearningStage = stage
            };
        }

        public Question CreateQuestionForStageId(Fact fact, string stageId)
        {
            var stage = _config.GetStageById(stageId);
            if (stage == null)
            {
                stage = _config.GetFirstStage();
            }
            return CreateQuestionForStage(fact, stage);
        }

        private LearningMode ConvertStageToLearningMode(LearningStage stage)
        {
            return stage.Type switch
            {
                LearningStageType.Assessment => LearningMode.Assessment,
                LearningStageType.Grounding => LearningMode.Grounding,
                _ => LearningMode.Practice
            };
        }
    }
} 