namespace FluencySDK
{
    public class SubmitAnswerResult
    {
        public UserAnswerSubmission UserAnswerSubmission { get; }
        public QuestionChoice<int> CorrectAnswer { get; }

        public float TimeToNextQuestion { get; }

        public bool ShouldRetry { get; }

        public SubmitAnswerResult(UserAnswerSubmission userAnswerSubmission, QuestionChoice<int> correctAnswer, float timeToNextQuestion,
            bool shouldRetry)
        {
            UserAnswerSubmission = userAnswerSubmission;
            CorrectAnswer = correctAnswer;
            TimeToNextQuestion = timeToNextQuestion;
            ShouldRetry = shouldRetry;
        }
    }
} 