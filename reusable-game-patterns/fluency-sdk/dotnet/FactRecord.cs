namespace FluencySDK
{
    public class FactRecord
    {
        public long LastSeen { get; set; } // Unix milliseconds timestamp
        public int TimesCorrect { get; set; }
        public int TimesIncorrect { get; set; }
        public double AverageResponseTime { get; set; } // In milliseconds
    }
} 