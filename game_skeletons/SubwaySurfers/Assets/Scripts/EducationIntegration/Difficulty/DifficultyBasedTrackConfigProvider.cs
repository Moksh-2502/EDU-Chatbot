using UnityEngine;
using SubwaySurfers.Assets.Scripts.Tracks;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Provides track runner configuration values based on the current difficulty level.
    /// Acts as a bridge between the difficulty system and track management.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DifficultyBasedTrackConfigProvider : MonoBehaviour, ITrackRunnerConfigProvider
    {
        [Header("Configuration")]
        [SerializeField] private DifficultyManager difficultyManager;
        
        [Header("Fallback Values")]
        [SerializeField] private float fallbackMinSpeed = 8.0f;
        [SerializeField] private float fallbackMaxSpeed = 12.0f;
        [SerializeField] private float fallbackAcceleration = 0.1f;
        [SerializeField] private int fallbackSpeedStep = 1;
        [SerializeField] private float fallbackObstacleDensity = 1.0f;
        [SerializeField] private float fallbackJumpAnimSpeedRatio = 0.6f;
        [SerializeField] private float fallbackSlideAnimSpeedRatio = 0.9f;

        // Override system for external control
        private float? _obstacleDensityOverride = null;
        private float? _accelerationOverride = null;

        // ITrackRunnerConfigProvider implementation
        public float MinSpeed => GetCurrentDifficultyConfig()?.speedRange.x ?? fallbackMinSpeed;
        public float MaxSpeed => GetCurrentDifficultyConfig()?.speedRange.y ?? fallbackMaxSpeed;
        public float Acceleration => _accelerationOverride ?? GetCurrentDifficultyConfig()?.accelerationRate ?? fallbackAcceleration;
        public int SpeedStep => fallbackSpeedStep; // Always constant for score calculations
        public float ObstacleDensity => _obstacleDensityOverride ?? GetCurrentDifficultyConfig()?.obstacleDensityMultiplier ?? fallbackObstacleDensity;

        // ─── New properties for animation multipliers ────────────────────
        public float JumpAnimSpeedRatio => GetCurrentDifficultyConfig()?.jumpAnimSpeedRatio ?? fallbackJumpAnimSpeedRatio;
        public float SlideAnimSpeedRatio => GetCurrentDifficultyConfig()?.slideAnimSpeedRatio ?? fallbackSlideAnimSpeedRatio;
        // ───────────────────────────────────────────────────────────────────
        /// <summary>
        /// Gets the spawn frequency for a specific consumable type based on difficulty
        /// </summary>
        public float GetConsumableSpawnFrequency(Consumable.ConsumableType consumableType)
        {
            var config = GetCurrentDifficultyConfig()?.collectableConfig;
            if (config == null) return 1.0f;

            return consumableType switch
            {
                Consumable.ConsumableType.COIN_MAG => config.magnetFrequency,
                Consumable.ConsumableType.SHIELD => config.shieldFrequency,
                Consumable.ConsumableType.INVINCIBILITY => config.invincibilityFrequency,
                Consumable.ConsumableType.EXTRALIFE => config.extraLifeFrequency,
                _ => config.baseFrequency
            };
        }
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets an override for obstacle density. Pass null to restore default difficulty-based density.
        /// </summary>
        /// <param name="density">Override density value (0.0 to 1.0) or null to restore default</param>
        public void SetObstacleDensityOverride(float? density)
        {
            _obstacleDensityOverride = density;
            Debug.Log($"[DifficultyBasedTrackConfigProvider] Obstacle density override set to: {density?.ToString() ?? "default"}");
        }

        /// <summary>
        /// Sets an override for acceleration. Pass null to restore default difficulty-based acceleration.
        /// </summary>
        /// <param name="acceleration">Override acceleration value or null to restore default</param>
        public void SetAccelerationOverride(float? acceleration)
        {
            _accelerationOverride = acceleration;
            Debug.Log($"[DifficultyBasedTrackConfigProvider] Acceleration override set to: {acceleration?.ToString() ?? "default"}");
        }

        #region Unity Lifecycle

        private void Awake()
        {
            ITrackRunnerConfigProvider.Instance = this;
            InitializeDifficultyManager();
        }

        #endregion

        #region Initialization

        private void InitializeDifficultyManager()
        {
            if (difficultyManager == null)
            {
                difficultyManager = FindFirstObjectByType<DifficultyManager>(FindObjectsInactive.Include);
                
                if (difficultyManager == null)
                {
                    Debug.LogWarning("[DifficultyBasedTrackConfigProvider] No DifficultyManager found in scene. Using fallback values.");
                }
                else
                {
                    Debug.Log("[DifficultyBasedTrackConfigProvider] Successfully found and connected to DifficultyManager");
                }
            }
        }

        #endregion

        #region Utility Methods

        private DifficultyLevel GetCurrentDifficultyConfig()
        {
            return difficultyManager?.CurrentDifficultyConfig;
        }

        /// <summary>
        /// Gets a summary of current configuration values for debugging
        /// </summary>
        public string GetConfigurationSummary()
        {
            var config = GetCurrentDifficultyConfig();
            string densityInfo = _obstacleDensityOverride.HasValue 
                ? $"{ObstacleDensity:F2} (overridden)" 
                : $"{ObstacleDensity:F2}";
            string accelerationInfo = _accelerationOverride.HasValue
                ? $"{Acceleration:F3} (overridden)"
                : $"{Acceleration:F3}";
            
            return $"Level: {difficultyManager?.CurrentDifficultyLevelIndex ?? -1} " +
                   $"({config?.displayName ?? "Unknown"}), " +
                   $"Speed: {MinSpeed}-{MaxSpeed}, " +
                   $"Acceleration: {accelerationInfo}, " +
                   $"Obstacle Density: {densityInfo}, " +
                   $"Jump/Slide Ratios: {JumpAnimSpeedRatio:F2}/{SlideAnimSpeedRatio:F2}";
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Log Current Configuration")]
        private void LogCurrentConfiguration()
        {
            Debug.Log($"[DifficultyBasedTrackConfigProvider] {GetConfigurationSummary()}");
        }

        [ContextMenu("Test Configuration Changes")]
        private void TestConfigurationChanges()
        {
            if (difficultyManager == null)
            {
                Debug.LogWarning("[DifficultyBasedTrackConfigProvider] No DifficultyManager available for testing");
                return;
            }

            Debug.Log("=== Testing Track Configuration Changes ===");
            
            for (int i = 0; i < 5; i++)
            {
                difficultyManager.SetDifficultyLevel(i, immediate: true);
                Debug.Log($"Level {i}: {GetConfigurationSummary()}");
            }
        }
#endif

        #endregion
    }
} 