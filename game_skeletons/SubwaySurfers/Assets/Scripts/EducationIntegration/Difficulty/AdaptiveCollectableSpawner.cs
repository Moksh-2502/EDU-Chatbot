using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ConsumableType = Consumable.ConsumableType;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Adaptive collectable spawner that adjusts collectable frequency based on difficulty level.
    /// Implements the exact hierarchy: Score Multiplier (base) → Magnet (2x less) → Shield (5x less) → 
    /// Invincibility (10x less) → Extra Life (20x less)
    /// </summary>
    public class AdaptiveCollectableSpawner : MonoBehaviour, IAdaptiveSystem
    {
        [Header("Configuration")]
        [SerializeField] private DifficultySystemConfig config;
        
        [Header("Base Frequencies (Level 0)")]
        [SerializeField] private float baseScoreMultiplierFreq = 1.0f;     // Base frequency
        [SerializeField] private float baseMagnetFreq = 0.5f;              // 2x less than score multiplier  
        [SerializeField] private float baseShieldFreq = 0.2f;              // 5x less than score multiplier
        [SerializeField] private float baseInvincibilityFreq = 0.1f;       // 10x less than score multiplier
        [SerializeField] private float baseExtraLifeFreq = 0.05f;          // 20x less than score multiplier

        // Current frequencies based on difficulty
        private float currentScoreMultiplierFreq;
        private float currentMagnetFreq;
        private float currentShieldFreq;
        private float currentInvincibilityFreq;
        private float currentExtraLifeFreq;

        // Dictionary for easy lookup
        private Dictionary<ConsumableType, float> currentFrequencies = new Dictionary<ConsumableType, float>();

        // Integration with existing systems
        private bool isInitialized = false;
        private DifficultyLevel lastAppliedDifficulty;

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to load config if not assigned
            if (config == null)
            {
                config = Resources.Load<DifficultySystemConfig>("DifficultySystemConfig");
                if (config == null)
                {
                    Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [CollectableSpawner] No config assigned and no default config found in Resources folder");
                }
            }

            InitializeWithDefaults();
        }

        private void OnValidate()
        {
            ValidateFrequencyHierarchy();
        }

        #endregion

        #region IAdaptiveSystem Implementation

        public bool IsEnabled { get; set; } = true;
        public void OnDifficultyChanged(DifficultyLevel newDifficulty)

        {
            if (!IsEnabled || newDifficulty == null) return;

            // Skip if same difficulty already applied
            if (lastAppliedDifficulty != null && lastAppliedDifficulty.Id == newDifficulty.Id)
            {
                return;
            }

            // Get scaling values from config
            float difficultyMultiplier = config?.difficultyFrequencyMultiplier ?? 0.2f;
            float maxMultiplier = config?.maxFrequencyMultiplier ?? 2.0f;

            // Calculate adaptive frequencies
            float multiplier = 1.0f + (newDifficulty.rank * difficultyMultiplier);
            multiplier = Mathf.Clamp(multiplier, 1.0f, maxMultiplier);

            // Apply hierarchy with difficulty scaling
            currentScoreMultiplierFreq = baseScoreMultiplierFreq * multiplier;
            currentMagnetFreq = baseMagnetFreq * multiplier;
            currentShieldFreq = baseShieldFreq * multiplier;
            currentInvincibilityFreq = baseInvincibilityFreq * multiplier;
            currentExtraLifeFreq = baseExtraLifeFreq * multiplier;

            // Update frequency dictionary
            UpdateFrequencyDictionary();

            lastAppliedDifficulty = newDifficulty;

            LogDebug($"Applied difficulty {newDifficulty.displayName} " +
                     $"(multiplier: {multiplier:F2}):\n" +
                     $"- Score Multiplier: {currentScoreMultiplierFreq:F3}\n" +
                     $"- Magnet: {currentMagnetFreq:F3}\n" +
                     $"- Shield: {currentShieldFreq:F3}\n" +
                     $"- Invincibility: {currentInvincibilityFreq:F3}\n" +
                     $"- Extra Life: {currentExtraLifeFreq:F3}");
        }

        #endregion

        #region Initialization

        private void InitializeWithDefaults()
        {
            if (isInitialized) return;

            // Validate base frequencies maintain the hierarchy
            ValidateFrequencyHierarchy();

            // Initialize with level 0 frequencies (no multiplier)
            currentScoreMultiplierFreq = baseScoreMultiplierFreq;
            currentMagnetFreq = baseMagnetFreq;
            currentShieldFreq = baseShieldFreq;
            currentInvincibilityFreq = baseInvincibilityFreq;
            currentExtraLifeFreq = baseExtraLifeFreq;

            UpdateFrequencyDictionary();
            
            isInitialized = true;
            LogDebug("Initialized with base frequencies");
        }

        #endregion

        #region Frequency Management

        private void UpdateFrequencyDictionary()
        {
            currentFrequencies[ConsumableType.SCORE_MULTIPLAYER] = currentScoreMultiplierFreq;
            currentFrequencies[ConsumableType.COIN_MAG] = currentMagnetFreq;
            currentFrequencies[ConsumableType.SHIELD] = currentShieldFreq;
            currentFrequencies[ConsumableType.INVINCIBILITY] = currentInvincibilityFreq;
            currentFrequencies[ConsumableType.EXTRALIFE] = currentExtraLifeFreq;
        }

        private void ValidateFrequencyHierarchy()
        {
            // Ensure hierarchy is maintained: Score Multiplier > Magnet > Shield > Invincibility > Extra Life
            if (baseMagnetFreq > baseScoreMultiplierFreq)
            {
                Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [CollectableSpawner] Magnet frequency ({baseMagnetFreq}) should be less than Score Multiplier ({baseScoreMultiplierFreq})");
            }
            
            if (baseShieldFreq > baseMagnetFreq)
            {
                Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [CollectableSpawner] Shield frequency ({baseShieldFreq}) should be less than Magnet ({baseMagnetFreq})");
            }
            
            if (baseInvincibilityFreq > baseShieldFreq)
            {
                Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [CollectableSpawner] Invincibility frequency ({baseInvincibilityFreq}) should be less than Shield ({baseShieldFreq})");
            }
            
            if (baseExtraLifeFreq > baseInvincibilityFreq)
            {
                Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [CollectableSpawner] Extra Life frequency ({baseExtraLifeFreq}) should be less than Invincibility ({baseInvincibilityFreq})");
            }
        }

        private void LogDebug(string message)
        {
            bool shouldLog = config?.enableDebugLogging ?? false;
            if (shouldLog)
            {
                Debug.Log($"{DifficultySystemConfig.LOG_PREFIX} [CollectableSpawner] {message}");
            }
        }

        #endregion

        #region Public Interface

        public Consumable GetCollectablePrefab(Consumable.ConsumableType type) =>
            ConsumableDatabase.GetConsumbale(type);

        /// <summary>
        /// Selects a collectable type using weighted random selection based on current frequencies
        /// </summary>
        /// <returns>The selected ConsumableType</returns>
        public ConsumableType SelectWeightedCollectable()
        {
            if (!IsEnabled || !isInitialized)
            {
                return ConsumableType.SCORE_MULTIPLAYER; // Safe fallback
            }

            float totalWeight = currentFrequencies.Values.Sum();
            if (totalWeight <= 0)
            {
                LogDebug("Total weight is zero, returning fallback");
                return ConsumableType.SCORE_MULTIPLAYER;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            // Check each type in hierarchy order
            foreach (var kvp in currentFrequencies.OrderByDescending(x => x.Value))
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            // Fallback (should rarely happen)
            return ConsumableType.SCORE_MULTIPLAYER;
        }

        /// <summary>
        /// Gets the current spawn probability for a specific collectable type
        /// </summary>
        public float GetCollectableProbability(ConsumableType type)
        {
            if (!currentFrequencies.ContainsKey(type)) return 0f;

            float totalWeight = currentFrequencies.Values.Sum();
            return totalWeight > 0 ? currentFrequencies[type] / totalWeight : 0f;
        }

        /// <summary>
        /// Determines if a collectable of the specified type should spawn based on its frequency
        /// </summary>
        public bool ShouldSpawnCollectable(ConsumableType type)
        {
            if (!IsEnabled || !isInitialized || !currentFrequencies.ContainsKey(type))
                return false;

            float frequency = currentFrequencies[type];
            return Random.Range(0f, 1f) < frequency;
        }

        /// <summary>
        /// Gets the next collectable type to spawn using weighted selection
        /// </summary>
        public ConsumableType GetNextCollectableType()
        {
            return SelectWeightedCollectable();
        }

        /// <summary>
        /// Gets a copy of current frequency settings
        /// </summary>
        public Dictionary<ConsumableType, float> GetCurrentFrequencies()
        {
            if (!isInitialized) InitializeWithDefaults();
            return new Dictionary<ConsumableType, float>(currentFrequencies);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Test Weighted Selection (10 times)")]
        private void TestWeightedSelection()
        {
            if (!Application.isPlaying) return;
            
            var results = new Dictionary<ConsumableType, int>();
            for (int i = 0; i < 10; i++)
            {
                var selected = SelectWeightedCollectable();
                if (!results.ContainsKey(selected))
                    results[selected] = 0;
                results[selected]++;
            }
            
            string output = "Selection Results:\n";
            foreach (var kvp in results)
            {
                output += $"- {kvp.Key}: {kvp.Value} times\n";
            }
            LogDebug(output);
        }

        [ContextMenu("Show Current Frequencies")]
        private void ShowCurrentFrequencies()
        {
            if (!isInitialized) InitializeWithDefaults();
            
            string output = "Current Frequencies:\n";
            foreach (var kvp in currentFrequencies)
            {
                float probability = GetCollectableProbability(kvp.Key);
                output += $"- {kvp.Key}: {kvp.Value:F3} ({probability:P1})\n";
            }
            LogDebug(output);
        }
#endif

        #endregion
    }
} 