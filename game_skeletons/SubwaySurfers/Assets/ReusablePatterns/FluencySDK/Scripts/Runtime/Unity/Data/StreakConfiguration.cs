using UnityEngine;

namespace FluencySDK.Data
{
    /// <summary>
    /// Configuration settings for streak-based question result processing
    /// </summary>
    [CreateAssetMenu(fileName = "StreakConfiguration", menuName = "FluencySDK/Streak Configuration")]
    public class StreakConfiguration : ScriptableObject
    {
        [Header("Streak Thresholds")]
        [SerializeField] private int correctStreakThreshold = 5;
        [SerializeField] private int incorrectStreakThreshold = 5;
        
        [Header("Reward Settings")]
        [SerializeField] private float correctAnswerShieldDuration = 2f;
        
        [Header("Visual Settings")]
        [SerializeField] private bool enableCorrectStreakVisualIndicator = false;
        [SerializeField] private bool enableIncorrectStreakVisualIndicator = false;
        [SerializeField] private int correctStreakVisualIndicatorThreshold = 3; // Show visual when this close to streak threshold
        [SerializeField] private int incorrectStreakVisualIndicatorThreshold = 2; // Show visual when this close to streak threshold
        public int CorrectStreakThreshold => correctStreakThreshold;
        public int IncorrectStreakThreshold => incorrectStreakThreshold;
        public float CorrectAnswerShieldDuration => correctAnswerShieldDuration;
        public bool EnableCorrectStreakVisualIndicator => enableCorrectStreakVisualIndicator;
        public bool EnableIncorrectStreakVisualIndicator => enableIncorrectStreakVisualIndicator;
        public int CorrectStreakVisualIndicatorThreshold => correctStreakVisualIndicatorThreshold;
        public int IncorrectStreakVisualIndicatorThreshold => incorrectStreakVisualIndicatorThreshold;
        
        private void OnValidate()
        {
            correctStreakThreshold = Mathf.Max(1, correctStreakThreshold);
            incorrectStreakThreshold = Mathf.Max(1, incorrectStreakThreshold);
            correctAnswerShieldDuration = Mathf.Max(0.1f, correctAnswerShieldDuration);
            correctStreakVisualIndicatorThreshold = Mathf.Max(1, correctStreakVisualIndicatorThreshold);
            incorrectStreakVisualIndicatorThreshold = Mathf.Max(1, incorrectStreakVisualIndicatorThreshold);
        }
    }
} 