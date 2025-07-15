using UnityEngine;

namespace FluencySDK.Events
{
    /// <summary>
    /// Simple event args for question feedback containing only essential data
    /// </summary>
    [System.Serializable]
    public class QuestionFeedbackEventArgs
    {
        public FeedbackType feedbackType;
        public string feedbackText;
        public Sprite feedbackIcon;
        
        public QuestionFeedbackEventArgs(FeedbackType type, string text = "", Sprite icon = null)
        {
            feedbackType = type;
            feedbackText = text;
            feedbackIcon = icon;
        }
    }
    
    /// <summary>
    /// Types of feedback for different contexts
    /// </summary>
    public enum FeedbackType
    {
        CorrectWord,
        CorrectReward,
        IncorrectWord,
        IncorrectPenalty,
        CorrectStreak,
        IncorrectStreak,
        CompleteCorrectStreak,
        CompleteIncorrectStreak,
    }
    
    /// <summary>
    /// Delegate for question feedback events
    /// </summary>
    public delegate void QuestionFeedbackEventHandler(QuestionFeedbackEventArgs feedbackArgs);
} 