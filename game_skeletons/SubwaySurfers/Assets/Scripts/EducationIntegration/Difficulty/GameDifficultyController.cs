using UnityEngine;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Game integration controller that bridges FluencySDK difficulty system with Subway Surfers game mechanics
    /// </summary>
    public class GameDifficultyController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DifficultySystemConfig config;
        
        [Header("FluencySDK References")]

        [Header("Game References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TrackManager trackManager;
        [SerializeField] private CharacterInputController characterController;

        // State tracking
        [SerializeField] private IDifficultyProvider _difficultyProvider;
        private GameState currentGameState;
        private bool isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to load config if not assigned
            if (config == null)
            {
                config = Resources.Load<DifficultySystemConfig>("DifficultySystemConfig");
                if (config == null)
                {
                    Debug.LogWarning($"{DifficultySystemConfig.LOG_PREFIX} [Controller] No config assigned and no default config found in Resources folder");
                }
            }

            InitializeReferences();
        }

        private void Start()
        {
            InitializeSystem();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void InitializeReferences()
        {
            // Auto-find references if not set
            _difficultyProvider = FindFirstObjectByType<DifficultyManager>(FindObjectsInactive.Include);



            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

            if (trackManager == null)
                trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);

            if (characterController == null)
                characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
        }

        private void InitializeSystem()
        {
            if (!ValidateReferences())
            {
                Debug.LogError($"{DifficultySystemConfig.LOG_PREFIX} [Controller] Missing critical references - difficulty system will not function");
                return;
            }

            isInitialized = true;
            LogDebug("Game difficulty controller initialized successfully");
        }

        private bool ValidateReferences()
        {
            return _difficultyProvider != null &&
                   characterController != null;
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            // Game state events
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            // Game state events
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(AState newState)
        {
            LogDebug($"Game state changed to: {newState.GetName()}");

            // Handle game state transitions
            if (newState is GameState gameState)
            {
                OnGameStarted();
            }
            else if (currentGameState != null)
            {
                OnGameEnded();
            }

            // Update current state reference
            if (newState is GameState gs)
                currentGameState = gs;
            else
                currentGameState = null;
        }

        private void OnGameStarted()
        {
            LogDebug("Run started - starting difficulty tracking");

            if (!isInitialized) return;
            ResetDifficultySystem();
            // Start time-based difficulty tracking
            if (_difficultyProvider.TimeBasedAdjuster != null)
            {
                _difficultyProvider.TimeBasedAdjuster.StartGameTimer();
            }

            // Reset life-based adjuster
            if (_difficultyProvider.LifeBasedAdjuster != null)
            {
                _difficultyProvider.LifeBasedAdjuster.Reset();
            }
        }

        private void OnGameEnded()
        {
            LogDebug("Game ended - stopping difficulty system");

            // Stop time-based adjuster
            if (_difficultyProvider.TimeBasedAdjuster != null)
            {
                _difficultyProvider.TimeBasedAdjuster.StopGameTimer();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually resets the entire difficulty system
        /// </summary>
        public void ResetDifficultySystem()
        {
            if (!isInitialized) return;

            LogDebug("Resetting difficulty system");

            // Reset difficulty manager
            if (_difficultyProvider != null)
            {
                _difficultyProvider.ResetDifficulty();
                _difficultyProvider.SetTimeBasedMode(false);
            }

            // Reset adjusters
            if (_difficultyProvider.LifeBasedAdjuster != null)
            {
                _difficultyProvider.LifeBasedAdjuster.Reset();
            }

            if (_difficultyProvider.TimeBasedAdjuster != null)
            {
                _difficultyProvider.TimeBasedAdjuster.StopGameTimer();
            }
        }

        /// <summary>
        /// Forces a specific difficulty level (for testing)
        /// </summary>
        public void SetDifficultyLevel(int level)
        {
            if (!isInitialized) return;

            if (_difficultyProvider != null)
            {
                _difficultyProvider.SetDifficultyLevel(level, immediate: true);
                LogDebug($"Manually set difficulty to level {level}");
            }
        }

        #endregion

        #region Private Methods

        private void LogDebug(string message)
        {
            if (config != null && config.enableDebugLogging)
            {
                Debug.Log($"{DifficultySystemConfig.LOG_PREFIX} [Controller] {message}");
            }
        }

        #endregion
    }
} 