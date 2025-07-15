using Characters;
using SharedCore.Analytics;
using UnityEngine;
using SubwaySurfers.Analytics.Events.Gameplay;
using SubwaySurfers.Analytics.Session;

namespace SubwaySurfers.Analytics.Senders
{
    /// <summary>
    /// Analytics event sender responsible for enhanced gameplay tracking
    /// Tracks collisions, deaths, and game overs with session context
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class GameplayAnalyticsEventSender : IAnalyticsEventSender
    {
        private IGameState _gameState;
        private IGameManager _gameManager;
        private TrackManager _trackManager;
        private CharacterInputController _characterInputController;
        private bool _isInitialized;

        // Track run-specific metrics

        public int InitializationPriority => 3; // After session and education senders
        public bool IsActive => _isInitialized && _gameState != null;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                _gameState = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
                _trackManager = Object.FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
                _characterInputController = Object.FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);

                _gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
                if(_gameManager != null)
                {
                    _gameManager.OnGameStateChanged += OnGameStateChanged;
                }

                if (_characterInputController != null)
                {
                    _characterInputController.characterCollider.OnObstacleHit += OnObstacleHit;
                }

                _isInitialized = true;
                Debug.Log("GameplayAnalyticsEventSender initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameplayAnalyticsEventSender initialization failed: {ex.Message}");
            }
        }

        private void OnGameStateChanged(AState newState)
        {
            if(newState is GameOverState)
            {
                OnGameOver();
            }
        }


        private void OnObstacleHit(string source)
        {
            if (!IsActive) return;

            try
            {
                // Get current game state
                var charName = _characterInputController?.character?.characterName ?? "Unknown";
                var themeName = _trackManager?.currentTheme?.themeName ?? "Unknown";
                var distance = _trackManager?.worldDistance ?? 0;
                var score = IPlayerStateProvider.Instance.RunScore;
                var coins = IPlayerStateProvider.Instance.RunCoins;
                var livesRemaining = IPlayerStateProvider.Instance.CurrentLives;

                // Create collision death event
                var collisionEvent = new CollisionLifeLossEvent(
                    charName, themeName, source, distance, score, coins, 
                    livesRemaining);

                IAnalyticsService.Instance?.TrackEvent(collisionEvent);

                Debug.Log($"[GameplayAnalyticsEventSender] Collision death tracked: {source}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameplayAnalyticsEventSender: Error tracking collision death: {ex.Message}");
            }
        }

        private void OnGameOver()
        {
            if (!IsActive) return;

            try
            {
                // Get final game state
                var charName = _characterInputController?.character?.characterName ?? "Unknown";
                var themeName = _trackManager?.currentTheme?.themeName ?? "Unknown";
                var finalDistance = _trackManager?.worldDistance ?? 0;
                var finalScore = IPlayerStateProvider.Instance.RunScore;
                var coinsCollected = IPlayerStateProvider.Instance.RunCoins;

                // Create enhanced game over event
                var gameOverEvent = new GameOverEvent(
                    charName, themeName, finalDistance, finalScore, coinsCollected);

                IAnalyticsService.Instance?.TrackEvent(gameOverEvent);

                Debug.Log($"[GameplayAnalyticsEventSender] Game over tracked - Distance: {finalDistance}, Score: {finalScore}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameplayAnalyticsEventSender: Error tracking run end: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_characterInputController != null)
            {
                _characterInputController.characterCollider.OnObstacleHit -= OnObstacleHit;
            }

            _gameState = null;
            _trackManager = null;
            _characterInputController = null;
            _isInitialized = false;
        }
    }
} 