namespace FluencySDK
{
    /// <summary>
    /// Represents a single fact answer event.
    /// </summary>
    public class FactAnswerRecord
    {
        public string FactId { get; }
        public bool IsCorrect { get; }
        public float TimeTakenToAnswer { get; }
        public LearningMode LearningMode { get; }

        public FactAnswerRecord(string factId, bool isCorrect, float timeTakenToAnswer, LearningMode learningMode = LearningMode.Assessment)
        {
            FactId = factId;
            IsCorrect = isCorrect;
            TimeTakenToAnswer = timeTakenToAnswer;
            LearningMode = learningMode;
        }
    }
} 