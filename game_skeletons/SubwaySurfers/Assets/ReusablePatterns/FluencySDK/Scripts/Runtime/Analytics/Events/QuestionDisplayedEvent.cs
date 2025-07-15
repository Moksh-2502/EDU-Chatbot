using FluencySDK;
using ReusablePatterns.FluencySDK.Enums;

namespace FluencySDK.Analytics
{
    /// <summary>
    /// Analytics event fired when a question is displayed to the student
    /// </summary>
    public class QuestionDisplayedEvent : QuestionAnalyticsEvent
    {
        public override string EventName => "question_displayed";

        public QuestionDisplayedEvent(IQuestion question, QuestionGenerationMode questionMode, LearningMode learningMode, string questionGameHandler)
            : base(question, questionMode, learningMode, questionGameHandler)
        {
        }
    }
} 