using ReusablePatterns.FluencySDK.Enums;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Interfaces
{

    public delegate void QuestionHandlerEventHandler(IQuestionGameplayHandler handler, IQuestion question);
    public delegate void QuestionHandlerEndedEventHandler(IQuestionGameplayHandler handler, IQuestion question, UserAnswerSubmission userAnswerSubmission);
    public interface IQuestionGameplayHandler
    {
        string HandlerIdentifier { get; }
        QuestionHandlerFlags Flags { get; }
        static event QuestionHandlerEventHandler QuestionHandlerStartedEvent;
        static event QuestionHandlerEventHandler QuestionHandlerExpiredEvent;
        static event QuestionHandlerEndedEventHandler QuestionHandlerEndedEvent;
        QuestionPresentationType QuestionPresentationType { get; }
        QuestionHandlerResult HandleQuestion(IQuestion question);
        QuestionHandlerResult CanHandleQuestionNow(IQuestion question);

        void NotifyHandlerQuestionStarted(IQuestion question)
        {
            QuestionHandlerStartedEvent?.Invoke(this, question);
        }

        void NotifyHandlerQuestionEnded(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            QuestionHandlerEndedEvent?.Invoke(this, question, userAnswerSubmission);
        }

        void NotifyHandlerQuestionExpired(IQuestion question)
        {
            QuestionHandlerExpiredEvent?.Invoke(this, question);
        }
    }
}