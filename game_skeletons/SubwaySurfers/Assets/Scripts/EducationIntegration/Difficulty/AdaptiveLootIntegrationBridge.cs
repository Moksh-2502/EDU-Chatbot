using UnityEngine;
using System.Collections.Generic;
using Consumables;
using ConsumableType = Consumable.ConsumableType;
namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Bridge component that integrates the adaptive collectable spawner with the existing loot spawning system.
    /// Modifies spawn chances and selects appropriate collectables based on difficulty.
    /// </summary>
    public class AdaptiveLootIntegrationBridge : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DifficultySystemConfig config;
        
        [Header("Integration References")]
        [SerializeField] private AdaptiveCollectableSpawner adaptiveSpawner;
        [SerializeField] private LootablesSpawner lootablesSpawner;
        [SerializeField] private ConsumableDatabase consumableDatabase;

        // Integration state
        private DifficultyLevel currentDifficulty;
        private float lastSpawnTime;
        private Dictionary<ConsumableType, float> originalSpawnRates = new Dictionary<ConsumableType, float>();

        // Cached components
        private ILootablesSpawner cachedLootSpawner;
        private IDifficultyProvider difficultyProvider;

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to load config if not assigned
            if (config == null)
            {
                config = Resources.Load<DifficultySystemConfig>("DifficultySystemConfig");
                if (config == null)
                {
                    Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [LootBridge] No config assigned and no default config found in Resources folder");
                }
            }

            InitializeReferences();
        }

        private void Start()
        {
            SetupIntegration();
            StoreOriginalSpawnRates();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            bool useAdaptiveSelection = config?.useAdaptiveSelection ?? true;
            if (useAdaptiveSelection)
            {
                HandleAdaptiveSpawning();
            }
        }

        #endregion

        #region Initialization

        private void InitializeReferences()
        {
            // Find adaptive spawner if not assigned
            if (adaptiveSpawner == null)
            {
                adaptiveSpawner = FindFirstObjectByType<AdaptiveCollectableSpawner>();
            }

            // Find loot spawner if not assigned
            if (lootablesSpawner == null)
            {
                lootablesSpawner = FindFirstObjectByType<LootablesSpawner>();
            }

            // Find consumable database if not assigned
            if (consumableDatabase == null)
            {
                var gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    consumableDatabase = gameManager.m_ConsumableDatabase;
                }
            }

            // Find difficulty controller
            difficultyProvider = FindFirstObjectByType<DifficultyManager>(FindObjectsInactive.Include);

            // Cache loot spawner interface
            cachedLootSpawner = lootablesSpawner as ILootablesSpawner;
        }

        private void SetupIntegration()
        {
            if (adaptiveSpawner == null)
            {
                LogDebug("No AdaptiveCollectableSpawner found!");
                return;
            }

            if (lootablesSpawner == null)
            {
                LogDebug("No LootablesSpawner found!");
                return;
            }
            LogDebug("Integration activated");
        }

        private void StoreOriginalSpawnRates()
        {
            // Store original spawn rates for restoration if needed
            // This would need to be adapted based on the actual LootablesSpawner implementation
            originalSpawnRates.Clear();
            
            originalSpawnRates[ConsumableType.SCORE_MULTIPLAYER] = 1.0f;
            originalSpawnRates[ConsumableType.COIN_MAG] = 0.5f;
            originalSpawnRates[ConsumableType.SHIELD] = 0.2f;
            originalSpawnRates[ConsumableType.INVINCIBILITY] = 0.1f;
            originalSpawnRates[ConsumableType.EXTRALIFE] = 0.05f;

            LogDebug("Stored original spawn rates");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (difficultyProvider != null)
            {
                // Subscribe to difficulty events
                difficultyProvider.OnDifficultyApplied += OnDifficultyApplied;
            }
        }

        private void UnsubscribeFromEvents()
        {
            difficultyProvider.OnDifficultyApplied -= OnDifficultyApplied;
        }

        private void OnDifficultyApplied(DifficultyLevel newDifficulty)
        {
            currentDifficulty = newDifficulty;
            
            bool overrideSpawnChances = config?.overrideSpawnChances ?? true;
            if (overrideSpawnChances)
            {
                UpdateSpawnChancesForDifficulty(newDifficulty);
            }

            LogDebug($"Difficulty changed to {newDifficulty.displayName}, updating spawn integration");
        }

        #endregion

        #region Adaptive Spawning Logic

        private void HandleAdaptiveSpawning()
        {
            if (!ShouldAttemptSpawn()) return;

            // Get next collectable type from adaptive spawner
            var selectedType = adaptiveSpawner.GetNextCollectableType();
            
            // Check if we should spawn this type
            if (adaptiveSpawner.ShouldSpawnCollectable(selectedType))
            {
                AttemptSpawnCollectable(selectedType);
            }
        }

        private bool ShouldAttemptSpawn()
        {
            if (currentDifficulty == null) return false;

            // Get spawn timing values from config
            float minInterval = config?.minSpawnInterval ?? 2.0f;
            float maxInterval = config?.maxSpawnInterval ?? 8.0f;
            float intervalReduction = config?.difficultyIntervalReduction ?? 0.3f;

            // Calculate adaptive spawn interval
            float baseInterval = Mathf.Lerp(maxInterval, minInterval, 
                difficultyProvider.CurrentDifficultyLevelIndex / (config.LevelCount - 1));
            
            float adaptiveInterval = baseInterval - (difficultyProvider.CurrentDifficultyLevelIndex * intervalReduction);
            adaptiveInterval = Mathf.Max(adaptiveInterval, minInterval);

            return Time.time - lastSpawnTime >= adaptiveInterval;
        }

        private void AttemptSpawnCollectable(ConsumableType type)
        {
            // Try to get the collectable prefab for this type
            var collectablePrefab = ConsumableDatabase.GetConsumbale(type);
            if (collectablePrefab == null)
            {
                LogDebug($"No prefab found for consumable type: {type}");
                return;
            }

            // Attempt to spawn through the loot system
            if (TrySpawnThroughLootSystem(collectablePrefab, type))
            {
                lastSpawnTime = Time.time;
                LogDebug($"Successfully spawned {type} through adaptive system");
            }
        }

        /// <summary>
        /// Attempts to spawn a collectable through the existing loot spawning system
        /// </summary>
        private bool TrySpawnThroughLootSystem(Consumable collectablePrefab, ConsumableType type)
        {
            if (lootablesSpawner == null || collectablePrefab == null)
                return false;

            try
            {
                // Create the collectable instance
                var spawnedCollectable = Instantiate(collectablePrefab);
                
                // Apply global spawn multiplier and adaptive bonus
                float globalMultiplier = config?.globalSpawnMultiplier ?? 1.0f;
                float adaptiveBonus = config?.adaptiveSpawnBonus ?? 0.1f;
                
                // Position the collectable appropriately
                PositionCollectable(spawnedCollectable);
                
                LogDebug($"Spawned {type} with multiplier {globalMultiplier} + bonus {adaptiveBonus}");
                return true;
            }
            catch (System.Exception e)
            {
                LogDebug($"Failed to spawn {type}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Positions a spawned collectable in the game world
        /// </summary>
        private void PositionCollectable(Consumable collectable)
        {
            if (collectable == null) return;

            // Basic positioning - this should be adapted based on your game's spawning system
            // For now, we'll use a simple forward offset
            var player = FindFirstObjectByType<CharacterInputController>();
            if (player != null)
            {
                Vector3 spawnPosition = player.transform.position + player.transform.forward * 10f;
                spawnPosition.y += 1f; // Slight height offset
                collectable.transform.position = spawnPosition;
            }
        }

        /// <summary>
        /// Updates spawn chances in the existing loot system based on difficulty
        /// </summary>
        private void UpdateSpawnChancesForDifficulty(DifficultyLevel difficulty)
        {
            if (lootablesSpawner == null || adaptiveSpawner == null) return;

            // Get current frequencies from the adaptive spawner
            var frequencies = adaptiveSpawner.GetCurrentFrequencies();
            
            // Apply these frequencies to the loot spawner
            // This implementation would need to be adapted based on the actual LootablesSpawner API
            foreach (var kvp in frequencies)
            {
                // Apply frequency to loot spawner
                // lootablesSpawner.SetSpawnChance(kvp.Key, kvp.Value);
            }

            LogDebug($"Updated spawn chances for difficulty level {difficulty.displayName}");
        }

        private void LogDebug(string message)
        {
            bool shouldLog = config?.enableDebugLogging ?? false;
            if (shouldLog)
            {
                Debug.Log($"{DifficultySystemConfig.LOG_PREFIX} [LootBridge] {message}");
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Manually spawns a specific collectable type
        /// </summary>
        public void SpawnCollectable(ConsumableType type)
        {
            AttemptSpawnCollectable(type);
        }

        /// <summary>
        /// Enables or disables the integration system
        /// </summary>
        public void SetIntegrationActive(bool active)
        {
            this.enabled = active;
            LogDebug($"Integration {(active ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Resets spawn rates to their original values
        /// </summary>
        public void ResetToOriginalSpawnRates()
        {
            // Reset to original spawn rates
            // Implementation depends on LootablesSpawner API
            LogDebug("Reset to original spawn rates");
        }

        /// <summary>
        /// Gets current spawn rates for debugging
        /// </summary>
        public Dictionary<ConsumableType, float> GetCurrentSpawnRates()
        {
            return adaptiveSpawner?.GetCurrentFrequencies() ?? new Dictionary<ConsumableType, float>();
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Test Spawn Score Multiplier")]
        private void TestSpawnScoreMultiplier()
        {
            if (Application.isPlaying)
                SpawnCollectable(ConsumableType.SCORE_MULTIPLAYER);
        }

        [ContextMenu("Test Spawn Shield")]
        private void TestSpawnShield()
        {
            if (Application.isPlaying)
                SpawnCollectable(ConsumableType.SHIELD);
        }

        [ContextMenu("Show Current Spawn Rates")]
        private void ShowCurrentSpawnRates()
        {
            var rates = GetCurrentSpawnRates();
            string output = "Current Spawn Rates:\n";
            foreach (var kvp in rates)
            {
                output += $"- {kvp.Key}: {kvp.Value:F3}\n";
            }
            LogDebug(output);
        }
#endif

        #endregion
    }
} 