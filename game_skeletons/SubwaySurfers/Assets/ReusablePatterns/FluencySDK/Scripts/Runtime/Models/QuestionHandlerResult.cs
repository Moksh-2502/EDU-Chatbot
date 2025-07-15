using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Interfaces
{
    public class QuestionHandlerResult
    {
        public IQuestion Question { get; set; }
        public string Error { get; set; }
        public bool Success => string.IsNullOrWhiteSpace(Error);

        public QuestionHandlerResult(IQuestion question, string error = null)
        {
            Question = question;
            Error = error;
        }

        public static QuestionHandlerResult CreateSuccess(IQuestion question)
        {
            return new QuestionHandlerResult(question);
        }

        public static QuestionHandlerResult CreateError(IQuestion question, string error)
        {
            return new QuestionHandlerResult(question, error);
        }
    }
}