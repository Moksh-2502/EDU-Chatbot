namespace FluencySDK
{
    public class QuestionChoice<TData>
    {
        public TData Value { get; set; }
        public bool IsCorrect { get; set; }

        public override string ToString()
        {
            return $"{Value} (Correct: {IsCorrect})";
        }
    }
} 