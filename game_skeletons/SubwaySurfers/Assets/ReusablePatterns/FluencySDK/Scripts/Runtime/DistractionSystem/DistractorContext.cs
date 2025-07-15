using System.Collections.Generic;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Context information for distractor generation strategies
    /// </summary>
    public class DistractorContext
    {
        /// <summary>
        /// Learning stage type of the current question
        /// </summary>
        public LearningStageType LearningStageType { get; set; }

        /// <summary>
        /// Learning mode of the current question
        /// </summary>
        public LearningMode LearningMode { get; set; }

        /// <summary>
        /// Maximum multiplication factor allowed in the system
        /// </summary>
        public int MaxMultiplicationFactor { get; set; }

        /// <summary>
        /// Values that have already been used (to avoid duplicates)
        /// </summary>
        public HashSet<int> UsedValues { get; set; } = new HashSet<int>();

        /// <summary>
        /// Number of distractors needed
        /// </summary>
        public int DistractorsNeeded { get; set; } = 3;

        public DistractorContext()
        {
        }

        public DistractorContext(LearningStageType learningStageType, LearningMode learningMode, int maxFactor)
        {
            LearningStageType = learningStageType;
            LearningMode = learningMode;
            MaxMultiplicationFactor = maxFactor;
        }
    }
} 