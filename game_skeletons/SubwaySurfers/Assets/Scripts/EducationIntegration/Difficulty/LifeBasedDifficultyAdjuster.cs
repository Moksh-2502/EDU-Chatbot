using Characters;
using UnityEngine;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Handles difficulty adjustments based on player life performance during the first 4 minutes of gameplay
    /// </summary>
    public class LifeBasedDifficultyAdjuster : BaseDifficultyAdjuster
    {
        // Internal state
        private float goodPerformanceTimer = 0f;
        private float lastDifficultyChangeTime = -1f;
        private bool hasTriggeredDifficultyIncrease = false;
        private DifficultySystemConfig _config;

        #region Constructor

        public LifeBasedDifficultyAdjuster(IDifficultyProvider difficultyProvider, IGamePauser gamePauser, IGameState gameState)
            : base(difficultyProvider, gamePauser, gameState)
        {
            _config = difficultyProvider.Config;
            
            // Subscribe to life events
            IPlayerStateProvider.Instance.OnLivesChanged += OnLifeChanged;
        }

        #endregion

        #region Base Class Overrides

        protected override string GetAdjusterName() => "LifeAdjuster";

        protected override bool ShouldProcess() => !_difficultyProvider.IsTimeBasedMode;

        protected override void UpdateInternal()
        {
            UpdateGoodPerformanceTimer();
            CheckForDifficultyAdjustments();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the adjuster state (called when starting a new game)
        /// </summary>
        public void Reset()
        {
            goodPerformanceTimer = 0f;
            lastDifficultyChangeTime = -1f;
            hasTriggeredDifficultyIncrease = false;
            // Note: _hasActiveRun is managed by game state events, not reset manually
            LogDebug("Life-based difficulty adjuster reset");
        }

        /// <summary>
        /// Gets the time remaining before next difficulty change is allowed
        /// </summary>
        public float GetCooldownTimeRemaining()
        {
            if (CanChangeDifficulty())
                return 0f;
                
            float cooldown = _config?.difficultyCooldown ?? 60f;
            return cooldown - (Time.time - lastDifficultyChangeTime);
        }

        /// <summary>
        /// Gets the current threshold for good performance (ceil of max lives / 2)
        /// </summary>
        public int GetCurrentGoodPerformanceThreshold()
        {
            return IPlayerStateProvider.Instance != null ? GetGoodPerformanceThreshold() : 0;
        }

        #endregion

        #region Event Handlers

        private void OnCriticalLifeReached()
        {
            LogDebug("Critical life reached - attempting to decrease difficulty");
            AttemptDifficultyChange(false);
        }

        private void OnPlayerDeath()
        {
            LogDebug("Player death - decreasing difficulty and resetting to level 0");
            
            // Player death always resets difficulty to 0 (as per requirements)
            if (_difficultyProvider != null)
            {
                _difficultyProvider.SetDifficultyLevel(0, immediate: true);
                lastDifficultyChangeTime = Time.time;
            }
        }

        private void OnLifeChanged(int currentLives)
        {
            if (currentLives <= 0)
            {
                OnPlayerDeath();
                return;
            }

            if (currentLives <= 1 && currentLives < IPlayerStateProvider.Instance.MaxLives)
            {
                OnCriticalLifeReached();
                return;
            }
            
            // Reset timer when lives drop below good performance threshold
            if (!IsAtGoodPerformanceThreshold())
            {
                ResetGoodPerformanceState();
                LogDebug($"Lives changed to {currentLives}/{IPlayerStateProvider.Instance.MaxLives} - resetting good performance timer");
            }
        }

        #endregion

        #region Private Methods

        private int GetGoodPerformanceThreshold()
        {
            // Calculate ceil(max/2) - the threshold for considering good performance
            return Mathf.CeilToInt(IPlayerStateProvider.Instance.MaxLives / 2f);
        }

        private bool IsAtGoodPerformanceThreshold()
        {
            return IPlayerStateProvider.Instance.CurrentLives >= GetGoodPerformanceThreshold();
        }

        private void UpdateGoodPerformanceTimer()
        {
            if (IsAtGoodPerformanceThreshold())
            {
                goodPerformanceTimer += Time.deltaTime;
            }
            else
            {
                ResetGoodPerformanceState();
            }
        }

        private void ResetGoodPerformanceState()
        {
            goodPerformanceTimer = 0f;
            hasTriggeredDifficultyIncrease = false;
        }

        private void CheckForDifficultyAdjustments()
        {
            float threshold = _config?.maxLivesThreshold ?? 30f;
            
            // Check if player has maintained good performance for threshold duration
            if (IsAtGoodPerformanceThreshold() && 
                goodPerformanceTimer >= threshold && 
                !hasTriggeredDifficultyIncrease &&
                CanChangeDifficulty())
            {
                int performanceThreshold = GetGoodPerformanceThreshold();
                LogDebug($"Player maintained {performanceThreshold}+ lives for {goodPerformanceTimer:F1} seconds - attempting to increase difficulty");
                AttemptDifficultyChange(true);
                hasTriggeredDifficultyIncrease = true;
            }
        }

        private void AttemptDifficultyChange(bool increase)
        {
            if (_difficultyProvider == null || !CanChangeDifficulty())
                return;

            if (increase)
            {
                _difficultyProvider.IncreaseDifficulty();
                LogDebug("Difficulty increased due to good performance");
            }
            else
            {
                _difficultyProvider.DecreaseDifficulty();
                LogDebug("Difficulty decreased due to poor performance");
            }
            
            lastDifficultyChangeTime = Time.time;
        }

        private bool CanChangeDifficulty()
        {
            float cooldown = _config?.difficultyCooldown ?? 60f;
            return lastDifficultyChangeTime < 0f || (Time.time - lastDifficultyChangeTime) >= cooldown;
        }



        #endregion

        #region Cleanup

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks
        /// </summary>
        public override void Dispose()
        {
            // Unsubscribe from life events
            if (IPlayerStateProvider.Instance != null)
            {
                IPlayerStateProvider.Instance.OnLivesChanged -= OnLifeChanged;
            }
            
            // Call base dispose to handle game state events
            base.Dispose();
        }

        #endregion
    }
} 