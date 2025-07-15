using FluencySDK;

namespace EducationIntegration.QuestionResultProcessor
{
    /// <summary>
    /// Interface for processing question results and applying appropriate rewards or penalties
    /// </summary>
    public interface IQuestionResultProcessor
    {
        /// <summary>
        /// Process the result of a question and apply appropriate rewards or penalties
        /// </summary>
        /// <param name="question">The question that was answered</param>
        /// <param name="userAnswerSubmission">The user's answer</param>
        void ProcessQuestionResult(IQuestion question, UserAnswerSubmission userAnswerSubmission);
    }
} 