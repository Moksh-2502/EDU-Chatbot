using Characters;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SubwaySurfers.LeaderboardSystem
{
    public class LeaderboardGameStateAdaptor : MonoBehaviour
    {
        private IGameManager _gameManager;
        private TrackManager _trackManager;
        private bool _isInitialized = false;

        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            // Find required components
            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            
            if (_gameManager == null)
            {
                Debug.LogError("LeaderboardGameStateAdaptor: GameManager not found!");
                return;
            }
            
            if (_trackManager == null)
            {
                Debug.LogError("LeaderboardGameStateAdaptor: TrackManager not found!");
                return;
            }

            // Subscribe to game state changes
            _gameManager.OnGameStateChanged += OnGameStateChanged;
            _isInitialized = true;
            
            Debug.Log("LeaderboardGameStateAdaptor initialized successfully");
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(AState newState)
        {
            if (!_isInitialized)
                return;

            // Check if we've switched to GameOverState
            if (newState is GameOverState)
            {
                SubmitPlayerScoreAsync().Forget();
            }
        }

        private async UniTaskVoid SubmitPlayerScoreAsync()
        {
            if (_trackManager == null)
            {
                Debug.LogWarning("LeaderboardGameStateAdaptor: Missing required components for score submission");
                return;
            }

            int playerScore = IPlayerStateProvider.Instance.RunScore;
            
            if (playerScore <= 0)
            {
                Debug.Log("LeaderboardGameStateAdaptor: Player score is 0 or negative, skipping submission");
                return;
            }

            Debug.Log($"LeaderboardGameStateAdaptor: Submitting player score: {playerScore}");

            try
            {
                // Submit to both weekly and all-time leaderboards in parallel
                bool success = await ILeaderboardService.Instance.SubmitScoreAsync(LeaderboardSystemConstants.LeaderboardId, playerScore);

                Debug.Log($"LeaderboardGameStateAdaptor: Score sumbit completed={success}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LeaderboardGameStateAdaptor: Exception during score submission: {ex.Message}");
            }
        }
    }
}