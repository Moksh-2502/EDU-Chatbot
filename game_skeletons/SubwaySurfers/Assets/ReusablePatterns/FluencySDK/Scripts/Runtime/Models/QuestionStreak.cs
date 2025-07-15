namespace FluencySDK
{
    public class QuestionStreak
    {
        public int CurrentStreak { get; set; }
        public int MaxStreak { get; set; }
        public int Total {get; set;}
        public int Correct {get; set;}
        public int Incorrect {get; set;}


        public QuestionStreak TakeSnapshot()
        {
            return new QuestionStreak()
            {
                CurrentStreak = CurrentStreak,
                MaxStreak = MaxStreak,
                Total = Total,
                Correct = Correct,
                Incorrect = Incorrect,
            };
        }


        public bool IsDifferent(QuestionStreak other)
        {
            if(other == null)
            {
                return true;
            }
            return CurrentStreak != other.CurrentStreak ||
                MaxStreak != other.MaxStreak ||
                Total != other.Total ||
                Correct != other.Correct ||
                Incorrect != other.Incorrect;
        }
    }
}
