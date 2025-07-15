namespace FluencySDK
{
    public class MasteryCheckResult
    {
        public bool IsMastered { get; set; }
        public int MasteredFacts { get; set; }
        public int TotalFacts { get; set; }
        public double AverageSpeed { get; set; } // Average response time in milliseconds for mastered facts
    }
} 