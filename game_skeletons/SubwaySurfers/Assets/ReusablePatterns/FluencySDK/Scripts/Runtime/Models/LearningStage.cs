using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace FluencySDK
{
    /// <summary>
    /// Stage types for the learning algorithm
    /// </summary>
    public enum LearningStageType
    {
        Grounding,
        Assessment,
        Practice,
        Review,
        Repetition,
        Mastered
    }

    /// <summary>
    /// Base class for all learning stages
    /// </summary>
    [System.Serializable]
    [Preserve]
    public abstract class LearningStage
    {
        public string Id { get; set; }
        public LearningStageType Type { get; set; }
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public float ProgressWeight { get; set; }
        public bool IsRewardEligible { get; set; } = false;
        public bool IsFullyLearned { get; set; } = false;
        public bool IsKnownFact { get; set; }
        public float? TimerSeconds { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Returns a short name for the stage without timing information
        /// </summary>
        public virtual string GetShortName()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// Grounding stage - untimed, blocking questions for struggling concepts
    /// </summary>
    [System.Serializable]
    [Preserve]
    public class GroundingStage : LearningStage
    {

        public GroundingStage()
        {
            Type = LearningStageType.Grounding;
            Description = "Untimed questions for struggling concepts";
            IsKnownFact = false;
            TimerSeconds = null;
            DisplayName = "Grounding";
        }
    }

    /// <summary>
    /// Assessment stage - timed initial evaluation
    /// </summary>
    [System.Serializable]
    [Preserve]
    public class AssessmentStage : LearningStage
    {
        public AssessmentStage()
        {
            Type = LearningStageType.Assessment;
            Description = "Timed initial evaluation";
            IsKnownFact = false;
            TimerSeconds = null;
            DisplayName = "Assessment";
        }
    }

    /// <summary>
    /// Practice stage - speed and accuracy development
    /// </summary>
    [System.Serializable]
    [Preserve]
    public class PracticeStage : LearningStage
    {
        public PracticeStage()
        {
            Type = LearningStageType.Practice;
            Description = "Speed and accuracy development";
            IsKnownFact = false;
            DisplayName = "Practice";
        }

        public override string GetShortName()
        {
            return "Practice";
        }
    }

    /// <summary>
    /// Review stage - within-session reinforcement
    /// </summary>
    [System.Serializable]
    [Preserve]
    public class ReviewStage : LearningStage
    {

        public int DelayMinutes { get; set; }

        public ReviewStage()
        {
            Type = LearningStageType.Review;
            Description = $"Within-session reinforcement ({DelayMinutes} min delay)";
            IsKnownFact = true;
            DisplayName = "Review";
            IsRewardEligible = true;
        }

        /// <summary>
        /// Returns "Review" without timing information
        /// </summary>
        public override string GetShortName()
        {
            return "Review";
        }
    }

    /// <summary>
    /// Repetition stage - cross-session reinforcement
    /// </summary>
    [System.Serializable]
    [Preserve]
    public class RepetitionStage : LearningStage
    {
        public int DelayDays { get; set; }

        public RepetitionStage()
        {
            Type = LearningStageType.Repetition;
            Description = $"Cross-session reinforcement ({DelayDays} day delay)";
            IsKnownFact = true;
            DisplayName = "Repetition";
            IsRewardEligible = true;
        }

        public override string GetShortName()
        {
            return "Repetition";
        }
    }

    /// <summary>
    /// Mastered stage - fully learned facts
    /// </summary>
    [System.Serializable]
    [Preserve]
    public class MasteredStage : LearningStage
    {
        public MasteredStage()
        {
            Type = LearningStageType.Mastered;
            Description = "Fully learned facts";
            IsKnownFact = true;
            DisplayName = "Mastered";
            IsRewardEligible = true;
        }
    }
} 