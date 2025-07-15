using System.Collections.Generic;

namespace FluencySDK
{
    public class StudentState
    {
        public int CurrentPosition { get; set; } // Current position in the learning sequence
        public Dictionary<string, FactRecord> LearnedFacts { get; set; } = new Dictionary<string, FactRecord>();
        public LearningMode Mode { get; set; }

        public StudentState()
        {
            LearnedFacts = new Dictionary<string, FactRecord>();
            Mode = LearningMode.Placement; // Default mode
            CurrentPosition = 0;
        }
    }
} 