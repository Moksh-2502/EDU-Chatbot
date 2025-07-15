namespace FluencySDK
{
    public class UserAnswerSubmission
    {
        public QuestionChoice<int> Answer { get; }
        public AnswerType AnswerType { get; }


        private UserAnswerSubmission(QuestionChoice<int> answer, AnswerType answerType)
        {
            Answer = answer;
            AnswerType = answerType;
        }

        public static UserAnswerSubmission FromAnswer(QuestionChoice<int> answer)
        {
            return new UserAnswerSubmission(answer, answer.IsCorrect ? AnswerType.Correct : AnswerType.Incorrect);
        }

        public static UserAnswerSubmission FromSkipped()
        {
            return new UserAnswerSubmission(null, AnswerType.Skipped);
        }
        public static UserAnswerSubmission FromTimedOut()
        {
            return new UserAnswerSubmission(null, AnswerType.TimedOut);
        }

        public override string ToString()
        {
            return $"Answer: {Answer}, AnswerType: {AnswerType}";
        }
    }
} 