using UnityEngine;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Comprehensive configuration for the entire difficulty system
    /// Contains all configurable values and pre-populated difficulty levels
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultySystemConfig", menuName = "Trash Dash/Difficulty System Config")]
    public class DifficultySystemConfig : ScriptableObject
    {
        /// <summary>
        /// Unified logging prefix for all difficulty system components
        /// </summary>
        public const string LOG_PREFIX = "[DIFFICULTY]";

        [Header("=== DIFFICULTY LEVELS ===")]
        [Space(10)]
        [SerializeField] private DifficultyLevel[] difficultyLevels = new DifficultyLevel[5];
        
        [Header("=== SYSTEM TIMING CONFIGURATION ===")]
        [Space(5)]
        [Tooltip("Starting difficulty level (0-4)")]
        [Range(0, 4)] public int startingDifficultyLevel = 0;
        
        [Tooltip("Cooldown time between difficulty changes in seconds")]
        public float difficultyCooldown = 60f;

        [Header("=== LIFE-BASED ADJUSTER SETTINGS ===")]
        [Space(5)]
        [Tooltip("Time player must maintain max lives before difficulty increases")]
        public float maxLivesThreshold = 30f;

        [Header("=== TIME-BASED ADJUSTER SETTINGS ===")]
        [Space(5)]
        [Tooltip("Time in seconds before switching to time-based mode (4 minutes)")]
        public float autoProgressionStartTime = 240f;
        
        [Tooltip("Interval for automatic difficulty increases in time-based mode")]
        public float autoIncreaseInterval = 60f;

        [Header("=== ADAPTIVE COLLECTABLE SPAWNER SETTINGS ===")]
        [Space(5)]
        [Tooltip("How much difficulty affects collectable frequency")]
        [Range(0.1f, 0.5f)] public float difficultyFrequencyMultiplier = 0.2f;
        
        [Tooltip("Maximum frequency multiplier for collectables")]
        [Range(1.5f, 3.0f)] public float maxFrequencyMultiplier = 2.0f;

        [Header("=== LOOT INTEGRATION BRIDGE SETTINGS ===")]
        [Space(5)]
        [Tooltip("Override spawn chances in existing loot system")]
        public bool overrideSpawnChances = true;
        
        [Tooltip("Use adaptive selection algorithm")]
        public bool useAdaptiveSelection = true;
        
        [Tooltip("Global multiplier for all spawn rates")]
        [Range(0.5f, 2.0f)] public float globalSpawnMultiplier = 1.0f;
        
        [Tooltip("Bonus spawn rate when adaptive system is active")]
        [Range(0.0f, 0.5f)] public float adaptiveSpawnBonus = 0.1f;

        [Header("=== SPAWN TIMING SETTINGS ===")]
        [Space(5)]
        [Tooltip("Minimum time between spawns")]
        public float minSpawnInterval = 2.0f;
        
        [Tooltip("Maximum time between spawns")]
        public float maxSpawnInterval = 8.0f;
        
        [Tooltip("How much difficulty reduces spawn intervals")]
        [Range(0.1f, 1.0f)] public float difficultyIntervalReduction = 0.3f;

        [Header("=== DEBUG SETTINGS ===")]
        [Space(5)]
        [Tooltip("Enable debug logging for all difficulty systems")]
        public bool enableDebugLogging = false;

        #region Properties

        /// <summary>
        /// Gets all difficulty levels
        /// </summary>
        public DifficultyLevel[] DifficultyLevels => difficultyLevels;

        /// <summary>
        /// Gets a specific difficulty level by index
        /// </summary>
        public DifficultyLevel GetDifficultyLevel(int index)
        {
            return difficultyLevels == null || difficultyLevels.Length == 0 ? null 
            : difficultyLevels[Mathf.Clamp(index, 0, difficultyLevels.Length - 1)];
        }

        /// <summary>
        /// Gets the number of difficulty levels
        /// </summary>
        public int LevelCount => difficultyLevels.Length;

        #endregion
    }
} 