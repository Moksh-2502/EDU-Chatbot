using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace FluencySDK
{

    /// <summary>
    /// Fact representation with reinforcement tracking
    /// </summary>
    [Serializable]
    [Preserve]
    public class FactItem
    {
        public string FactId { get; set; }
        
        /// <summary>
        /// Stage ID reference
        /// </summary>
        public string StageId { get; set; }
        
        /// <summary>
        /// Single timestamp for when this fact was last asked - replaces separate review/repetition timestamps
        /// </summary>
        public DateTime? LastAskedTime { get; set; }
        
        public string FactSetId { get; set; }

        /// <summary>
        /// Consecutive correct answers for this individual fact in current stage
        /// </summary>
        public int ConsecutiveCorrect { get; set; }

        /// <summary>
        /// Consecutive incorrect answers for this individual fact in current stage
        /// </summary>
        public int ConsecutiveIncorrect { get; set; }

        /// <summary>
        /// Random factor between -0.5 and 0.5 for staggering timing to prevent identical sequences
        /// Regenerated each time the fact is shown
        /// </summary>
        public float RandomFactor { get; set; }

        public FactItem()
        {
            GenerateRandomFactor();
        }

        public FactItem(string factId, string factSetId, string stageId = "assessment")
        {
            FactId = factId;
            FactSetId = factSetId;
            StageId = stageId;
            LastAskedTime = null;
            ConsecutiveCorrect = 0;
            ConsecutiveIncorrect = 0;
            GenerateRandomFactor();
        }

        /// <summary>
        /// Updates consecutive counters and resets opposing counter
        /// </summary>
        public void UpdateStreak(AnswerType answerType)
        {
            if (answerType == AnswerType.Correct)
            {
                ConsecutiveCorrect++;
                ConsecutiveIncorrect = 0;
            }
            else if (answerType == AnswerType.Incorrect)
            {
                ConsecutiveIncorrect++;
                ConsecutiveCorrect = 0;
            }
        }

        /// <summary>
        /// Resets consecutive counters when moving to new stage
        /// </summary>
        public void ResetStreak()
        {
            ConsecutiveCorrect = 0;
            ConsecutiveIncorrect = 0;
        }

        /// <summary>
        /// Updates the last asked time
        /// </summary>
        public void UpdateLastAskedTime(DateTime? currentTime = null)
        {
            LastAskedTime = currentTime ?? DateTime.Now;
        }

        /// <summary>
        /// Generates a new random factor between -0.5 and 0.5 for timing staggering
        /// Called each time the fact is shown to prevent identical sequences
        /// </summary>
        public void GenerateRandomFactor()
        {
            RandomFactor = UnityEngine.Random.Range(-0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Unified answer record for all tracking purposes (replaces StageAnswerRecord + QuestionHistory)
    /// </summary>
    [Serializable]
    [Preserve]
    public class AnswerRecord
    {
        public string FactId { get; set; }
        public AnswerType AnswerType { get; set; }
        
        /// <summary>
        /// Stage ID at the time of this answer - replaces LearningStage enum
        /// </summary>
        public string StageId { get; set; }
        
        public DateTime AnswerTime { get; set; }
        public string FactSetId { get; set; }

        /// <summary>
        /// Whether this was a known fact
        /// </summary>
        public bool WasKnownFact { get; set; }

        public AnswerRecord()
        {
        }

        public AnswerRecord(string factId, AnswerType answerType, string stageId, string factSetId, DateTime? answerTime = null)
        {
            FactId = factId;
            AnswerType = answerType;
            StageId = stageId;
            FactSetId = factSetId;
            AnswerTime = answerTime ?? DateTime.Now;
            
            // WasKnownFact will be set based on the stage configuration when we have access to it
            // For now, default to false during migration
            WasKnownFact = false;
        }

        /// <summary>
        /// Create answer record with explicit known fact status
        /// </summary>
        public AnswerRecord(string factId, AnswerType answerType, string stageId, string factSetId, bool wasKnownFact, DateTime? answerTime = null)
        {
            FactId = factId;
            AnswerType = answerType;
            StageId = stageId;
            FactSetId = factSetId;
            AnswerTime = answerTime ?? DateTime.Now;
            WasKnownFact = wasKnownFact;
        }
    }
}