using Cysharp.Threading.Tasks;
using UnityEngine;
using SubwaySurfers.Tutorial.Events;
using SubwaySurfers.Tutorial.Core;
using SubwaySurfers.Assets.Scripts.Tracks;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Integration
{
    public class TutorialGameStateAdapter : MonoBehaviour
    {
        [SerializeField] private TutorialConfig tutorialConfig;
        private IGameState _gameState;
        private IGameManager _gameManager;
        private ITrackRunnerConfigProvider _configProvider;
        private bool _isInitialized = false;

        private void Awake()
        {
            InitializeAdapter();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeAdapter()
        {
            // Find GameState
            _gameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (_gameState == null)
            {
                Debug.LogWarning("TutorialGameStateAdapter: GameState not found!");
                return;
            }

            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (_gameManager == null)
            {
                Debug.LogWarning("TutorialGameStateAdapter: IGameManager not found!");
                return;
            }

            // Get the config provider instance
            _configProvider = ITrackRunnerConfigProvider.Instance;
            if (_configProvider == null)
            {
                Debug.LogWarning("TutorialGameStateAdapter: ITrackRunnerConfigProvider.Instance not found!");
                return;
            }

            _isInitialized = true;
            Debug.Log("TutorialGameStateAdapter: Initialized successfully");
        }

        private void SubscribeToEvents()
        {
            if (!_isInitialized)
                return;

            TutorialEventBus.OnTutorialCompleted += HandleTutorialCompleted;
            TutorialEventBus.OnStateChanged += HandleTutorialStateChanged;

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += OnGameStateChanged;
            }

            if (_gameState != null)
            {
                _gameState.OnGameFinished += OnGameFinished;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (!_isInitialized)
                return;

            TutorialEventBus.OnTutorialCompleted -= HandleTutorialCompleted;
            TutorialEventBus.OnStateChanged -= HandleTutorialStateChanged;

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }

            if (_gameState != null)
            {
                _gameState.OnGameFinished -= OnGameFinished;
            }
        }

        private void OnGameFinished()
        {
            ITutorialManager.Instance.StopTutorial("Game finished during tutorial");
        }

        private void OnGameStateChanged(AState state)
        {
            if (state is not GameState)
            {
                ITutorialManager.Instance.StopTutorial("Game state changed to non-game state: " + state.GetType().Name);
            }
        }

        private void HandleTutorialStateChanged(TutorialStateChangedEvent stateEvent)
        {
            Debug.Log(
                $"TutorialGameStateAdapter: Tutorial state changed - Active: {stateEvent.IsActive}, Paused: {stateEvent.IsPaused}");

            if (stateEvent.IsActive && !stateEvent.IsPaused)
            {
                // Tutorial started - enable tutorial mode
                EnableTutorialMode();
            }
            else if (!stateEvent.IsActive)
            {
                // Tutorial ended - disable tutorial mode
                DisableTutorialMode();
            }
        }

        private void HandleTutorialCompleted(TutorialCompletedEvent completionEvent)
        {
            Debug.Log($"TutorialGameStateAdapter: Tutorial completed - Success: {completionEvent.Success}");

            if (completionEvent.Success)
            {
                IPlayerDataProvider.Instance.SetLastCompletedTutorialVersionAsync(tutorialConfig.Version).Forget();
            }

            // Ensure tutorial mode is disabled
            DisableTutorialMode();
        }

        private void EnableTutorialMode()
        {
            if (_configProvider == null)
                return;

            // Set overrides for tutorial mode
            _configProvider.SetObstacleDensityOverride(0f); // No regular obstacles
            _configProvider.SetAccelerationOverride(0f); // Constant speed

            Debug.Log(
                "TutorialGameStateAdapter: Tutorial mode enabled - obstacle spawning disabled, speed acceleration disabled");
        }

        private void DisableTutorialMode()
        {
            if (_configProvider == null)
                return;

            // Restore original values
            _configProvider.SetObstacleDensityOverride(null);
            _configProvider.SetAccelerationOverride(null);

            Debug.Log(
                $"TutorialGameStateAdapter: Tutorial mode disabled - obstacle spawning and acceleration restored to defaults");
        }

        private void OnDestroy()
        {
            // Ensure we restore normal behavior if this gets destroyed during tutorial
            DisableTutorialMode();
        }
    }
}