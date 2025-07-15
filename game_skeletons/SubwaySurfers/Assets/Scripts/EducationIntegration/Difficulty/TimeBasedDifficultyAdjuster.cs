using UnityEngine;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Handles automatic difficulty progression after 4 minutes of gameplay
    /// </summary>
    public class TimeBasedDifficultyAdjuster : BaseDifficultyAdjuster
    {
        // Internal state
        private double _lastAutoIncreaseTime = 0f;
        private bool _hasActivatedTimeBasedMode = false;
        private float _timeAccum = 0;
        private bool _isTimerStarted = false;

        #region Constructor

        public TimeBasedDifficultyAdjuster(IDifficultyProvider difficultyProvider, IGamePauser gamePauser, IGameState gameState)
            : base(difficultyProvider, gamePauser, gameState)
        {
        }

        #endregion

        #region Base Class Overrides

        protected override string GetAdjusterName() => "TimeAdjuster";

        protected override bool ShouldProcess() => _isTimerStarted;

        protected override void UpdateInternal()
        {
            _timeAccum += Time.deltaTime;

            CheckTimeBasedActivation();
            
            if (_hasActivatedTimeBasedMode)
            {
                CheckAutoIncreaseInterval();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts tracking game time for time-based difficulty adjustments
        /// </summary>
        public void StartGameTimer()
        {
            _timeAccum = 0f;
            _lastAutoIncreaseTime = 0;
            _hasActivatedTimeBasedMode = false;
            _isTimerStarted = true;
            
            LogDebug("Game timer started for time-based difficulty adjustments");
        }

        /// <summary>
        /// Stops and resets the game timer
        /// </summary>
        public void StopGameTimer()
        {
            if (_hasActivatedTimeBasedMode && _difficultyProvider != null)
            {
                // Switch back to life-based mode
                _difficultyProvider.SetTimeBasedMode(false);
            }
            
            _timeAccum = 0f;
            _lastAutoIncreaseTime = 0f;
            _hasActivatedTimeBasedMode = false;
            _isTimerStarted = false;
            
            LogDebug("Game timer stopped and reset");
        }

        #endregion

        #region Private Methods

        private void CheckTimeBasedActivation()
        {
            float activationTime = _difficultyProvider.Config == null ? 240f :
             _difficultyProvider.Config.autoProgressionStartTime;
            
            // Check if we should activate time-based mode
            if (!_hasActivatedTimeBasedMode && _timeAccum >= activationTime)
            {
                ActivateTimeBasedMode();
            }
        }

        private void ActivateTimeBasedMode()
        {
            _hasActivatedTimeBasedMode = true;
            _lastAutoIncreaseTime = _timeAccum;
            
            if (_difficultyProvider != null)
            {
                // Disable life-based adjustments and switch to time-based mode
                _difficultyProvider.SetTimeBasedMode(true);
                LogDebug("Time-based difficulty mode activated - life-based adjustments disabled");
            }
        }

        private void CheckAutoIncreaseInterval()
        {
            float interval = _difficultyProvider.Config == null ? 60f :
             _difficultyProvider.Config.autoIncreaseInterval;
            
            // Automatically increase difficulty every interval
            if (_timeAccum - _lastAutoIncreaseTime >= interval)
            {
                AttemptAutoIncrease();
                _lastAutoIncreaseTime = _timeAccum;
            }
        }

        private void AttemptAutoIncrease()
        {
            if (_difficultyProvider != null)
            {
                int currentLevel = _difficultyProvider.CurrentDifficultyLevelIndex;
                
                // Only increase if not at maximum
                if (currentLevel < _difficultyProvider.Config.LevelCount - 1)
                {
                    _difficultyProvider.IncreaseDifficulty();
                    LogDebug($"Auto-increased difficulty to level {_difficultyProvider.CurrentDifficultyLevelIndex} " +
                            $"after {_timeAccum:F1} seconds of gameplay");
                }
                else
                {
                    LogDebug("Already at maximum difficulty - no auto-increase");
                }
            }
        }



        #endregion
    }
} 