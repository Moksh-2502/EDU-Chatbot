using SharedCore.Analytics;
using ReusablePatterns.FluencySDK.Enums;
using System.Collections.Generic;
using SharedCore.Analytics.Attributes;

namespace FluencySDK.Analytics
{
    public abstract class QuestionAnalyticsEvent : BaseAnalyticsEvent
    {
        [AnalyticsIgnore]
        public IQuestion Question { get; }

        public QuestionGenerationMode QuestionMode { get; }
        public LearningMode LearningMode { get; }

        public string QuestionGameHandler { get; }

        protected QuestionAnalyticsEvent(IQuestion question, QuestionGenerationMode questionMode, LearningMode learningMode, string questionGameHandler)
        {
            Question = question;
            QuestionMode = questionMode;
            LearningMode = learningMode;
            QuestionGameHandler = questionGameHandler;
        }

        protected override Dictionary<string, object> GetCustomProperties()
        {
            var baseCustomProperties = base.GetCustomProperties();
            var questionProperties = Question.ToAnalyticsProperties();
            foreach (var property in questionProperties)
            {
                baseCustomProperties.Add(property.Key, property.Value);
            }
            return baseCustomProperties;
        }
    }
} 