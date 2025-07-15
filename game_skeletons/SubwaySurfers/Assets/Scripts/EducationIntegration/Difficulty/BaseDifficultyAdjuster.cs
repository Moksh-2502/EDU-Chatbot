using UnityEngine;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace SubwaySurfers.DifficultySystem
{

    /// <summary>
    /// Base class for difficulty adjusters that provides common functionality
    /// </summary>
    public abstract class BaseDifficultyAdjuster : IDifficultyAdjuster
    {
        // Shared dependencies
        protected IDifficultyProvider _difficultyProvider;
        protected IGamePauser _gamePauser;
        protected IGameState _gameState;
        
        // Shared state
        protected bool _hasActiveRun = false;

        #region Constructor

        protected BaseDifficultyAdjuster(IDifficultyProvider difficultyProvider, IGamePauser gamePauser, IGameState gameState)
        {
            _difficultyProvider = difficultyProvider;
            _gamePauser = gamePauser;
            _gameState = gameState;
            
            // Subscribe to game state events
            _gameState.OnGameStarted += OnGameStarted;
            _gameState.OnGameFinished += OnGameFinished;
            
            LogDebug($"{GetAdjusterName()} initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Main update method - calls derived class implementation if conditions are met
        /// </summary>
        public void Update()
        {
            if (_difficultyProvider == null || !ShouldProcess())
                return;

            // Only process during active game runs and when not paused
            if (!_hasActiveRun || _gamePauser.IsPaused)
                return;

            // Call derived class implementation
            UpdateInternal();
        }

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks
        /// </summary>
        public virtual void Dispose()
        {
            // Unsubscribe from game state events
            if (_gameState != null)
            {
                _gameState.OnGameStarted -= OnGameStarted;
                _gameState.OnGameFinished -= OnGameFinished;
            }
            
            LogDebug($"{GetAdjusterName()} disposed");
        }

        #endregion

        #region Protected Abstract Methods

        /// <summary>
        /// Override this to provide the adjuster-specific name for logging
        /// </summary>
        protected abstract string GetAdjusterName();

        /// <summary>
        /// Override this to provide adjuster-specific conditions for processing
        /// </summary>
        protected abstract bool ShouldProcess();

        /// <summary>
        /// Override this to provide the adjuster-specific update logic
        /// </summary>
        protected abstract void UpdateInternal();

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// Called when game starts - can be overridden for additional logic
        /// </summary>
        protected virtual void OnGameStarted()
        {
            _hasActiveRun = true;
            LogDebug($"Game started - {GetAdjusterName()} now active");
        }

        /// <summary>
        /// Called when game finishes - can be overridden for additional logic
        /// </summary>
        protected virtual void OnGameFinished()
        {
            _hasActiveRun = false;
            LogDebug($"Game finished - {GetAdjusterName()} now inactive");
        }

        /// <summary>
        /// Shared logging method
        /// </summary>
        protected virtual void LogDebug(string message)
        {
            if (_difficultyProvider.Config?.enableDebugLogging ?? false)
            {
                Debug.Log($"{DifficultySystemConfig.LOG_PREFIX} [{GetAdjusterName()}] {message}");
            }
        }

        #endregion
    }
} 