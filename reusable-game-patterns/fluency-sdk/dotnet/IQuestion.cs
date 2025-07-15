namespace FluencySDK
{
    public interface IQuestion
    {
        string Id { get; }
        int[] Factors { get; }
        int Answer { get; }
        long? TimeStarted { get; }
        long? TimeEnded { get; }
        int? UserAnswer { get; }
        bool? IsCorrect { get; }
    }

    public class Question : IQuestion
    {
        public string Id { get; set; }
        public int[] Factors { get; set; }
        public int Answer { get; set; }
        public long? TimeStarted { get; set; }
        public long? TimeEnded { get; set; }
        public int? UserAnswer { get; set; }
        public bool? IsCorrect { get; set; }
    }
} 