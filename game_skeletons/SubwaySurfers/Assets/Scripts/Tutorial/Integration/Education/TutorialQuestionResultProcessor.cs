using EducationIntegration.QuestionResultProcessor;
using FluencySDK;
using FluencySDK.Events;
using FluencySDK.UI;

namespace SubwaySurfers.Tutorial.Integration
{
    public class TutorialQuestionResultProcessor : IQuestionResultProcessor
    {
        public void ProcessQuestionResult(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            if (userAnswerSubmission.AnswerType == AnswerType.Correct)
            {
                var feedbackArgs = new QuestionFeedbackEventArgs(
                    FeedbackType.CorrectWord,
                    "Correct!",
                    null);

                IQuestionFeedbackDisplayer.Instance?.DisplayFeedback(feedbackArgs);
            }
            // No incorrect processing for tutorial questions
        }
    }
}