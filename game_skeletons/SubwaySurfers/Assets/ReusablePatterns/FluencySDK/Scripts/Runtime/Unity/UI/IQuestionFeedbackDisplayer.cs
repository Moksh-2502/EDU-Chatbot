using FluencySDK.Events;

namespace FluencySDK.UI
{
    public interface IQuestionFeedbackDisplayer
    {
        static IQuestionFeedbackDisplayer Instance { get; set; }
        void DisplayFeedback(QuestionFeedbackEventArgs feedbackArgs);
    }
}