using System;

namespace FluencySDK.Unity
{
    /// <summary>
    /// Interface for question timer functionality
    /// </summary>
    public interface IQuestionTimer
    {
        /// <summary>
        /// Event fired when timer progress changes (0.0 to 1.0)
        /// </summary>
        event Action<float> OnTimerProgressChanged;
        
        /// <summary>
        /// Event fired when timer expires
        /// </summary>
        event Action<IQuestion> OnTimerExpired;
        
        /// <summary>
        /// Event fired when timer is stopped
        /// </summary>
        event Action OnTimerStopped;
        
        /// <summary>
        /// Current timer progress (0.0 = expired, 1.0 = full time)
        /// </summary>
        float Progress { get; }
        
        /// <summary>
        /// Whether the timer is currently running
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// Start the timer with the specified duration
        /// </summary>
        /// <param name="duration">Timer duration in seconds</param>
        void StartTimer(float duration);
        
        /// <summary>
        /// Stop the timer
        /// </summary>
        void StopTimer();
        
        /// <summary>
        /// Pause the timer
        /// </summary>
        void PauseTimer();
        
        /// <summary>
        /// Resume the timer
        /// </summary>
        void ResumeTimer();
    }
} 