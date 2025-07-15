using UnityEngine;
using DG.Tweening;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace SubwaySurfers
{
    /// <summary>
    /// Handles game pause functionality including pause/resume with countdown
    /// </summary>
    public class PauseManager : MonoBehaviour, IGamePauser
    {
        [Header("Settings")] public float unpauseCountdownTime = 3.0f;
        public float countdownSpeed = 1.5f;

        private bool m_WasMoving;
        private TrackManager m_TrackManager;
        private Tween m_CountdownTween;

        public event PauseEventHandler OnPaused;
        public event PauseEventHandler OnResumed;
        public event PauseEventHandler<float> OnCountdownUpdated;
        public event PauseEventHandler OnResumeStarted;
        public event PauseEventHandler OnCountdownFinished;
        private IGameState m_GameState;
        private IGameManager m_GameManager;
        private bool _initialized = false;

        // when not null, the game is paused
        private PauseData _pauseData;

        public bool IsPaused { get; private set; }
        private bool _isInGameState = false, _inActiveRun = false;

        private CharacterInputController _characterInputController;

        private void Awake()
        {
            IGamePauser.Instance = this;
            // Find references if not assigned
            m_GameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (m_GameState != null)
            {
                m_GameState.OnGameStarted += OnGameStarted;
                m_GameState.OnGameFinished += OnGameFinished;
            }

            if (m_TrackManager == null)
                m_TrackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);


            _characterInputController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);

            m_GameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (m_GameManager != null)
            {
                m_GameManager.OnGameStateChanged += OnGameStateChanged;
            }

            _initialized = true;
        }

        private void OnEnable()
        {
            GameInputController.OnPauseInput += OnPauseInputReceived;
        }

        private void OnDisable()
        {
            GameInputController.OnPauseInput -= OnPauseInputReceived;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Pause(PauseData.Default);
        }

        private void OnApplicationFocus(bool focusStatus)
        {
            if (!focusStatus) Pause(PauseData.Default);
        }

        private void OnGameStateChanged(AState state)
        {
            _isInGameState = state is GameState;

            if (_isInGameState == false && IsPaused)
            {
                Resume();
            }
        }

        private void OnGameStarted()
        {
            _inActiveRun = true;
        }

        private void OnGameFinished()
        {
            _inActiveRun = false;
        }

        private void OnPauseInputReceived()
        {
            // Only handle pause input during active gameplay
            if (!_initialized || !_isInGameState || !_inActiveRun || m_GameState.IsFinished)
                return;

            // Toggle pause/resume
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause(PauseData.Default);
            }
        }

        public void Pause(PauseData data)
        {
            if (!_initialized)
                return;

            // Check if we aren't already in pause or if game is finished
            if (IsPaused)
                return;

            if (data.ignoreGameState == false && (_isInGameState == false || _inActiveRun == false || m_GameState.IsFinished))
            {
                return;
            }

            _pauseData = data;
            Time.timeScale = 0;
            IsPaused = true;

            if (_characterInputController != null && _characterInputController.character != null &&
                _characterInputController.character.animator != null)
            {
                if (_pauseData.animateCharacter)
                {
                    _characterInputController.character.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                }
                else
                {
                    _characterInputController.character.animator.updateMode = AnimatorUpdateMode.Normal;
                }
            }

            m_WasMoving = m_TrackManager.isMoving;
            m_TrackManager.StopMove();

            OnPaused?.Invoke(data);
        }

        public void Resume()
        {
            if (!_initialized || !IsPaused)
                return;

            // Start countdown process
            OnResumeStarted?.Invoke(_pauseData);

            // Kill any existing tween
            if (m_CountdownTween != null && m_CountdownTween.IsActive())
            {
                m_CountdownTween.Kill();
            }

            if (_pauseData.resumeWithCountdown)
            {
                // Start with the maximum value
                float countdownValue = unpauseCountdownTime;
                OnCountdownUpdated?.Invoke(_pauseData, countdownValue);

                // Create a DOTween animation to count down
                m_CountdownTween = DOTween.To(
                        () => countdownValue,
                        x =>
                        {
                            countdownValue = x;
                            OnCountdownUpdated?.Invoke(_pauseData, countdownValue);
                        },
                        0,
                        unpauseCountdownTime / countdownSpeed)
                    .SetEase(Ease.Linear)
                    .SetUpdate(UpdateType.Normal, true)
                    .OnComplete(FinishResume);
            }
            else
            {
                FinishResume();
            }
        }

        private void FinishResume()
        {
            OnCountdownFinished?.Invoke(_pauseData);

            // Resume game
            if (m_WasMoving && m_GameState.IsFinished == false)
            {
                m_TrackManager.StartMove(false);
            }

            Time.timeScale = 1;
            IsPaused = false;

            OnResumed?.Invoke(_pauseData);
        }

        private void OnDestroy()
        {
            // Clean up any running tweens when the object is destroyed
            if (m_CountdownTween != null && m_CountdownTween.IsActive())
            {
                m_CountdownTween.Kill();
            }

            if (m_GameState != null)
            {
                m_GameState.OnGameStarted -= OnGameStarted;
                m_GameState.OnGameFinished -= OnGameFinished;
            }

            if (m_GameManager != null)
            {
                m_GameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
    }
}