using System;
using System.Collections.Generic;
using Characters;
using UnityEngine;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Core difficulty management system that handles difficulty state and adjustments
    /// </summary>
    public class DifficultyManager : MonoBehaviour, IDifficultyProvider, IScoreMultiplier
    {
        /// <summary>
        /// Fired when a new difficulty configuration is applied
        /// </summary>
        public event Action<DifficultyLevel> OnDifficultyApplied; // current config
        
        [Header("Configuration")]
        [SerializeField] private DifficultySystemConfig config;

        public DifficultySystemConfig Config => config;

        // IDifficultyProvider implementation
        public int CurrentDifficultyLevelIndex { get; private set; }
        public DifficultyLevel CurrentDifficultyConfig => GetDifficultyConfigByIndex(CurrentDifficultyLevelIndex);
        public bool IsDifficultyAdjustmentEnabled { get; private set; } = true;
        public bool IsTimeBasedMode { get; private set; } = false;

        // Internal state
        private float lastDifficultyChangeTime = -1f;
        private readonly List<IAdaptiveSystem> adaptiveSystems = new List<IAdaptiveSystem>();

        public TimeBasedDifficultyAdjuster TimeBasedAdjuster { get; private set; }
        public LifeBasedDifficultyAdjuster LifeBasedAdjuster { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to load default config if none assigned
            if (config == null)
            {
                config = Resources.Load<DifficultySystemConfig>("DifficultySystemConfig");
                if (config == null)
                {
                    Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [Manager] No config assigned and no default config found in Resources folder");
                }
            }

            InitializeDifficultyLevels();
            InitializeAdjusters();
            SetDifficultyLevel(config?.startingDifficultyLevel ?? 0, immediate: true);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            IPlayerStateProvider.Instance.RegisterScoreMultiplier(this);
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            IPlayerStateProvider.Instance.UnRegisterScoreMultiplier(this);
        }

        private void Update()
        {
            if (TimeBasedAdjuster != null)
            {
                TimeBasedAdjuster.Update();
            }
            
            if (LifeBasedAdjuster != null)
            {
                LifeBasedAdjuster.Update();
            }
        }

        #endregion

        #region IDifficultyProvider Implementation

        public void SetDifficultyLevel(int index, bool immediate = false)
        {
            // Validate level
            index = Mathf.Clamp(index, 0, config.LevelCount - 1);

            // Check cooldown unless immediate
            if (!immediate && !CanChangeDifficulty())
            {
                LogDebug($"Difficulty change blocked by cooldown. Last change: {lastDifficultyChangeTime}, Current: {Time.time}");
                return;
            }

            // No change needed
            if (index == CurrentDifficultyLevelIndex)
            {
                return;
            }

            int oldLevel = CurrentDifficultyLevelIndex;
            CurrentDifficultyLevelIndex = index;
            lastDifficultyChangeTime = Time.time;

            LogDebug($"Difficulty changed from {oldLevel} to {index} ({CurrentDifficultyConfig?.displayName ?? "Unknown"})");

            // Notify systems
            OnDifficultyApplied?.Invoke(CurrentDifficultyConfig);
            
            NotifyAdaptiveSystems();
        }

        public void IncreaseDifficulty(bool ignoreRules = false)
        {
            if (!IsDifficultyAdjustmentEnabled && !ignoreRules)
            {
                LogDebug("Difficulty increase blocked - adjustments disabled");
                return;
            }

            int newLevel = Mathf.Min(CurrentDifficultyLevelIndex + 1, config.LevelCount - 1);
            if (newLevel != CurrentDifficultyLevelIndex)
            {
                SetDifficultyLevel(newLevel, immediate: ignoreRules);
            }
            else
            {
                LogDebug("Already at maximum difficulty level");
            }
        }

        public void DecreaseDifficulty(bool ignoreRules = false)
        {
            if (!IsDifficultyAdjustmentEnabled && !ignoreRules)
            {
                LogDebug("Difficulty decrease blocked - adjustments disabled");
                return;
            }

            int newLevel = Mathf.Max(CurrentDifficultyLevelIndex - 1, 0);
            if (newLevel != CurrentDifficultyLevelIndex)
            {
                SetDifficultyLevel(newLevel, immediate: ignoreRules);
            }
            else
            {
                LogDebug("Already at minimum difficulty level");
            }
        }

        public void ResetDifficulty()
        {
            SetDifficultyLevel(config?.startingDifficultyLevel ?? 0, immediate: true);
        }

        public void SetDifficultyAdjustmentEnabled(bool enabled)
        {
            bool wasEnabled = IsDifficultyAdjustmentEnabled;
            IsDifficultyAdjustmentEnabled = enabled;
            
            if (wasEnabled != enabled)
            {
                LogDebug($"Difficulty adjustment {(enabled ? "enabled" : "disabled")}");
            }
        }

        public void SetTimeBasedMode(bool timeBasedMode)
        {
            bool wasTimeBased = IsTimeBasedMode;
            IsTimeBasedMode = timeBasedMode;
            
            if (wasTimeBased != timeBasedMode)
            {
                LogDebug($"Difficulty mode changed to {(timeBasedMode ? "time-based" : "life-based")}");
            }
        }

        #endregion

        #region Adaptive System Management

        /// <summary>
        /// Registers an adaptive system to receive difficulty change notifications
        /// </summary>
        public void RegisterAdaptiveSystem(IAdaptiveSystem adaptiveSystem)
        {
            if (adaptiveSystem != null && !adaptiveSystems.Contains(adaptiveSystem))
            {
                adaptiveSystems.Add(adaptiveSystem);
                
                // Immediately notify of current difficulty
                adaptiveSystem.OnDifficultyChanged(CurrentDifficultyConfig);
                
                LogDebug($"Registered adaptive system: {adaptiveSystem.GetType().Name}");
            }
        }

        /// <summary>
        /// Unregisters an adaptive system
        /// </summary>
        public void UnregisterAdaptiveSystem(IAdaptiveSystem adaptiveSystem)
        {
            if (adaptiveSystem != null && adaptiveSystems.Contains(adaptiveSystem))
            {
                adaptiveSystems.Remove(adaptiveSystem);
                LogDebug($"Unregistered adaptive system: {adaptiveSystem.GetType().Name}");
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Gets the difficulty configuration for a specific level
        /// </summary>
        public DifficultyLevel GetDifficultyConfigByIndex(int index)
        {
            if (config == null)
            {
                LogDebug("No config available, returning null");
                return null;
            }

            return config.GetDifficultyLevel(index);
        }

        /// <summary>
        /// Gets all difficulty configurations
        /// </summary>
        public DifficultyLevel[] GetAllDifficultyConfigs()
        {
            return config?.DifficultyLevels ?? new DifficultyLevel[0];
        }

        /// <summary>
        /// Checks if difficulty can be changed (respects cooldown)
        /// </summary>
        public bool CanChangeDifficulty()
        {
            float cooldownTime = config?.difficultyCooldown ?? 60f;
            return lastDifficultyChangeTime < 0f || (Time.time - lastDifficultyChangeTime) >= cooldownTime;
        }

        /// <summary>
        /// Gets remaining cooldown time in seconds
        /// </summary>
        public float GetCooldownTimeRemaining()
        {
            if (CanChangeDifficulty()) return 0f;
            
            float cooldownTime = config?.difficultyCooldown ?? 60f;
            return cooldownTime - (Time.time - lastDifficultyChangeTime);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes difficulty levels from config
        /// </summary>
        private void InitializeDifficultyLevels()
        {
            if (config == null)
            {
                LogDebug("No config available for initialization");
                return;
            }

            LogDebug($"Initialized with {config.LevelCount} difficulty levels from config");
        }

        /// <summary>
        /// Initializes the difficulty adjusters
        /// </summary>
        private void InitializeAdjusters()
        {
            // Find required dependencies for time-based adjuster
            var gamePauser = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
            var gameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            
            if (gamePauser != null && gameState != null)
            {
                TimeBasedAdjuster = new TimeBasedDifficultyAdjuster(this, gamePauser, gameState);
                LogDebug("TimeBasedDifficultyAdjuster initialized");
            }
            else
            {
                LogDebug("Could not initialize TimeBasedDifficultyAdjuster - missing dependencies");
            }

            // Initialize life-based adjuster
            if (gamePauser != null && gameState != null)
            {
                LifeBasedAdjuster = new LifeBasedDifficultyAdjuster(this, gamePauser, gameState);
                LogDebug("LifeBasedDifficultyAdjuster initialized");
            }
            else
            {
                LogDebug("Could not initialize LifeBasedDifficultyAdjuster - missing dependencies");
            }
        }

        private void SubscribeToEvents()
        {
            // Subscribe to any global events if needed
            LogDebug("Subscribed to events");
        }

        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from events
            LogDebug("Unsubscribed from events");
        }

        private void NotifyAdaptiveSystems()
        {
            foreach (var system in adaptiveSystems)
            {
                try
                {
                    system.OnDifficultyChanged(CurrentDifficultyConfig);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{DifficultySystemConfig.LOG_PREFIX} [Manager] Error notifying adaptive system {system.GetType().Name}: {e.Message}");
                }
            }

            LogDebug($"Notified {adaptiveSystems.Count} adaptive systems of difficulty change");
        }

        private void LogDebug(string message)
        {
            if (config != null && config.enableDebugLogging)
            {
                Debug.Log($"{DifficultySystemConfig.LOG_PREFIX} [Manager] {message}");
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Reset Difficulty System")]
        private void TestResetSystem()
        {
            ResetDifficulty();
        }

        [ContextMenu("Set Difficulty Level 0")]
        private void TestSetLevel0() => SetDifficultyLevel(0, immediate: true);

        [ContextMenu("Set Difficulty Level 2")]
        private void TestSetLevel2() => SetDifficultyLevel(2, immediate: true);

        [ContextMenu("Set Difficulty Level 4")]
        private void TestSetLevel4() => SetDifficultyLevel(4, immediate: true);

        [ContextMenu("Toggle Debug Logging")]
        private void TestToggleDebug() => config.enableDebugLogging = !config.enableDebugLogging;
#endif

        #endregion

        public float GetMultiplier()
        {
            return CurrentDifficultyLevelIndex + 1;
        }
    }
} 