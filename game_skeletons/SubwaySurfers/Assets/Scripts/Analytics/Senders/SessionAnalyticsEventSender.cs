using SharedCore.Analytics;
using UnityEngine;
using SubwaySurfers.Analytics.Session;

namespace SubwaySurfers.Analytics.Senders
{
    /// <summary>
    /// Analytics event sender responsible for session lifecycle tracking
    /// Integrates with SessionManager and game state to automatically track sessions
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class SessionAnalyticsEventSender : IAnalyticsEventSender
    {
        private SessionManager _sessionManager;
        private IGameState _gameState;
        private IGameManager _gameManager;
        private bool _isInitialized;

        public int InitializationPriority => 1; // Higher priority to initialize after core systems
        public bool IsActive => _isInitialized && _sessionManager != null;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                // Find or create SessionManager
                _sessionManager = SessionManager.Instance;
                if (_sessionManager == null)
                {
                    // Create SessionManager if it doesn't exist
                    var sessionManagerGO = new GameObject("SessionManager");
                    _sessionManager = sessionManagerGO.AddComponent<SessionManager>();
                    Object.DontDestroyOnLoad(sessionManagerGO);
                }

                // Find GameState for lifecycle events
                _gameState = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
                
                if (_gameState != null)
                {
                    _gameState.OnGameStarted += OnGameStarted;
                }

                _gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
                if (_gameManager != null)
                {
                    _gameManager.OnGameStateChanged += OnGameStateChanged;
                }

                _isInitialized = true;
                Debug.Log("SessionAnalyticsEventSender initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SessionAnalyticsEventSender initialization failed: {ex.Message}");
            }
        }

        private void OnGameStateChanged(AState newState)
        {
            if (newState is GameOverState gameOverState)
            {
                if (!IsActive) return;

            try
            {
                // Record activity when game finishes
                _sessionManager.RecordActivity();
                _sessionManager.RecordGameOver();
                Debug.Log("[SessionAnalyticsEventSender] Game finished - recorded activity");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SessionAnalyticsEventSender: Error handling game finish: {ex.Message}");
            }
            }
        }

        private void OnGameStarted()
        {
            if (!IsActive) return;

            try
            {
                if(_sessionManager.IsSessionActive == false)
                {
                    _sessionManager.StartSession();
                }
                else
                {
                    // Record activity to prevent idle timeout during active gameplay
                    _sessionManager.RecordActivity();
                }
                Debug.Log("[SessionAnalyticsEventSender] Game started - recorded activity");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SessionAnalyticsEventSender: Error handling game start: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_gameState != null)
            {
                _gameState.OnGameStarted -= OnGameStarted;
            }

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }

            _gameState = null;
            _sessionManager = null;
            _isInitialized = false;
        }
    }
} 