using System;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Interface for classes that provide difficulty management functionality
    /// </summary>
    public interface IDifficultyProvider
    {
        DifficultySystemConfig Config { get; }
        event Action<DifficultyLevel> OnDifficultyApplied;
        /// <summary>
        /// Current difficulty level
        /// </summary>
        int CurrentDifficultyLevelIndex { get; }
        
        /// <summary>
        /// Current difficulty configuration
        /// </summary>
        DifficultyLevel CurrentDifficultyConfig { get; }
        
        /// <summary>
        /// Whether difficulty adjustments are currently enabled
        /// </summary>
        bool IsDifficultyAdjustmentEnabled { get; }
        
        /// <summary>
        /// Whether the system is in time-based mode (vs life-based)
        /// </summary>
        bool IsTimeBasedMode { get; }

        /// <summary>
        /// Sets the difficulty level
        /// </summary>
        /// <param name="level">Difficulty level (0-4)</param>
        /// <param name="immediate">Whether to apply immediately or wait for cooldown</param>
        void SetDifficultyLevel(int level, bool immediate = false);
        
        /// <summary>
        /// Increases difficulty by one level (if possible)
        /// </summary>
        void IncreaseDifficulty(bool ignoreRules = false);
        
        /// <summary>
        /// Decreases difficulty by one level (if possible)
        /// </summary>
        void DecreaseDifficulty(bool ignoreRules = false);

        /// <summary>
        /// Whether the difficulty is currently being adjusted
        /// </summary>
        bool CanChangeDifficulty();
        /// <summary>
        /// Resets difficulty to the starting level (0)
        /// </summary>
        void ResetDifficulty();
        
        /// <summary>
        /// Enables or disables automatic difficulty adjustments
        /// </summary>
        /// <param name="enabled">Whether to enable adjustments</param>
        void SetDifficultyAdjustmentEnabled(bool enabled);
        
        /// <summary>
        /// Switches between life-based and time-based difficulty adjustment modes
        /// </summary>
        /// <param name="timeBasedMode">True for time-based, false for life-based</param>
        void SetTimeBasedMode(bool timeBasedMode);

        /// <summary>
        /// Gets the remaining cooldown time for difficulty changes
        /// </summary>
        /// <returns>Remaining cooldown time in seconds</returns>
        float GetCooldownTimeRemaining();

        TimeBasedDifficultyAdjuster TimeBasedAdjuster { get; }
        LifeBasedDifficultyAdjuster LifeBasedAdjuster { get; }
    }

    /// <summary>
    /// Interface for systems that respond to difficulty changes
    /// </summary>
    public interface IAdaptiveSystem
    {
        /// <summary>
        /// Called when difficulty level changes
        /// </summary>
        /// <param name="newDifficulty">New difficulty configuration</param>
        void OnDifficultyChanged(DifficultyLevel newDifficulty);
        
        /// <summary>
        /// Whether this system is currently enabled
        /// </summary>
        bool IsEnabled { get; set; }
    }
} 