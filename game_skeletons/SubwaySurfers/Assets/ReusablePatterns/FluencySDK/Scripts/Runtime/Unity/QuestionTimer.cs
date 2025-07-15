using System;
using DG.Tweening;
using UnityEngine;

namespace FluencySDK.Unity
{
    /// <summary>
    /// Concrete implementation of question timer using DOTween
    /// </summary>
    public class QuestionTimer : IQuestionTimer
    {
        public event Action<float> OnTimerProgressChanged;
        public event Action<IQuestion> OnTimerExpired;
        public event Action OnTimerStopped;

        public float Progress { get; private set; } = 1f;
        public bool IsRunning { get; private set; }

        private Tween _timerTween;
        private float _duration;
        private float _remainingTime;
        private bool _isPaused;
        private IQuestion _question;

        public QuestionTimer(IQuestion question)
        {
            _question = question;
        }

        public void StartTimer(float duration)
        {
            if (duration <= 0)
            {
                Debug.LogWarning("[QuestionTimer] Invalid duration provided: " + duration);
                return;
            }

            StopTimer(); // Stop any existing timer
            
            _duration = duration;
            _remainingTime = duration;
            Progress = 1f;
            IsRunning = true;
            _isPaused = false;

            // Notify initial progress
            OnTimerProgressChanged?.Invoke(Progress);

            // Create DOTween animation
            _timerTween = DOTween.To(() => Progress, x => Progress = x, 0f, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(true) // Make it timescale independent
                .OnUpdate(() =>
                {
                    _remainingTime = Progress * _duration;
                    OnTimerProgressChanged?.Invoke(Progress);
                })
                .OnComplete(() =>
                {
                    IsRunning = false;
                    Progress = 0f;
                    OnTimerExpired?.Invoke(_question);
                });
        }

        public void StopTimer()
        {
            if (_timerTween != null && _timerTween.IsActive())
            {
                _timerTween.Kill();
                _timerTween = null;
            }

            if (IsRunning)
            {
                IsRunning = false;
                _isPaused = false;
                OnTimerStopped?.Invoke();
            }
        }

        public void PauseTimer()
        {
            if (IsRunning && !_isPaused && _timerTween != null && _timerTween.IsActive())
            {
                _timerTween.Pause();
                _isPaused = true;
            }
        }

        public void ResumeTimer()
        {
            if (IsRunning && _isPaused && _timerTween != null && _timerTween.IsActive())
            {
                _timerTween.Play();
                _isPaused = false;
            }
        }

        /// <summary>
        /// Clean up resources when timer is destroyed
        /// </summary>
        public void Dispose()
        {
            StopTimer();
        }
    }
} 