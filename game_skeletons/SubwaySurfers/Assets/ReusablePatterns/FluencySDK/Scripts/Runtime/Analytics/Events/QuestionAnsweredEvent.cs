using ReusablePatterns.FluencySDK.Enums;
using System.Collections.Generic;

namespace FluencySDK.Analytics
{
    /// <summary>
    /// Analytics event fired when a student answers a question
    /// </summary>
    public class QuestionAnsweredEvent : QuestionAnalyticsEvent
    {
        public override string EventName => "question_answered";

        public AnswerType AnswerType { get; }

        public QuestionAnsweredEvent(IQuestion question, AnswerType answerType, QuestionGenerationMode questionMode, LearningMode learningMode, string questionGameHandler)
            : base(question, questionMode, learningMode, questionGameHandler)
        {
            AnswerType = answerType;
        }

        protected override Dictionary<string, object> GetCustomProperties()
        {
            var baseCustomProperties = base.GetCustomProperties();
            var actualTimeToAnswer = (Question.TimeEnded - Question.TimeStarted) / 1000f;
            baseCustomProperties.Add("actual_time_to_answer_seconds", actualTimeToAnswer);
            return baseCustomProperties;
        }
    }
} 