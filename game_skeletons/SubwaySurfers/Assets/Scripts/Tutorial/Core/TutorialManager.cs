using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using SubwaySurfers.Tutorial.Data;
using SubwaySurfers.Tutorial.Events;
using SubwaySurfers.Tutorial.Steps;
using UnityEngine.Serialization;

namespace SubwaySurfers.Tutorial.Core
{
    public class TutorialManager : MonoBehaviour, ITutorialManager
    {
        [FormerlySerializedAs("_config")] [SerializeField]
        private TutorialConfig config;

        private Dictionary<TutorialStepType, ITutorialStep> _steps;
        private ITutorialStep _currentStep;
        private bool _isActive = false;
        private bool _isPaused = false;
        private float _tutorialStartTime;
        private int _totalActions = 0;


        public bool IsActive => _isActive;
        public bool IsPaused => _isPaused;
        public TutorialStepType? CurrentStepType => _currentStep?.StepType;
        public ITutorialStep CurrentStep => _currentStep;

        public float OverallProgress
        {
            get
            {
                if (config?.Steps == null || config.Steps.Length == 0)
                    return 0f;

                int completedSteps = 0;
                foreach (var step in _steps.Values)
                {
                    if (step.IsCompleted)
                        completedSteps++;
                }

                float currentStepProgress = _currentStep?.CompletionPercentage ?? 0f;
                return ((float)completedSteps + currentStepProgress) / config.Steps.Length;
            }
        }

        private void Awake()
        {
            ITutorialManager.Instance = this;
            InitializeSteps();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeSteps()
        {
            if (config == null)
            {
                Debug.LogError("TutorialManager: TutorialConfig is not assigned!");
                return;
            }

            _steps = new Dictionary<TutorialStepType, ITutorialStep>();

            foreach (var stepData in config.Steps)
            {
                ITutorialStep step = CreateStep(stepData);
                if (step != null)
                {
                    _steps[stepData.stepType] = step;
                }
            }
        }

        private ITutorialStep CreateStep(TutorialStepData stepData)
        {
            switch (stepData.stepType)
            {
                case TutorialStepType.LeftRightSwipe:
                    return new LeftRightSwipeStep(stepData);
                case TutorialStepType.SwipeLeft:
                    return new SwipeLeftStep(stepData);
                case TutorialStepType.SwipeRight:
                    return new SwipeRightStep(stepData);
                case TutorialStepType.Jump:
                    return new JumpStep(stepData);
                case TutorialStepType.Slide:
                    return new SlideStep(stepData);
                case TutorialStepType.Completion:
                    return new CompletionStep(stepData);
                case TutorialStepType.QuestionTutorial:
                    return new QuestionTutorialStep(stepData);
                case TutorialStepType.Start:
                    return new StartStep(stepData);
                default:
                    Debug.LogWarning($"TutorialManager: Unknown step type {stepData.stepType}");
                    return null;
            }
        }

        private void SubscribeToEvents()
        {
            TutorialEventBus.OnStepCompleted += HandleStepCompleted;
            TutorialEventBus.OnActionPerformed += HandlePlayerAction;
        }

        private void UnsubscribeFromEvents()
        {
            TutorialEventBus.OnStepCompleted -= HandleStepCompleted;
            TutorialEventBus.OnActionPerformed -= HandlePlayerAction;
        }

        private async UniTask<bool> CanStartTutorialAsync(bool forceStart)
        {
            if (forceStart)
            {
                return true;
            }

            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData == null || playerData.LastCompletedTutorialVersion != config.Version)
            {
                return true;
            }

            return false;
        }

        public async UniTask StartTutorialAsync(bool forceStart)
        {
            try
            {
                if (_isActive)
                {
                    Debug.LogWarning("TutorialManager: Tutorial is already active!");
                    return;
                }

                var canStartTutorial = await CanStartTutorialAsync(forceStart);
                if (canStartTutorial == false)
                {
                    return;
                }

                _isActive = true;
                _isPaused = false;
                _tutorialStartTime = Time.time;
                _totalActions = 0;

                // Reset all steps
                foreach (var step in _steps.Values)
                {
                    step.ResetStep();
                }

                // Start the first step
                StartFirstStep();

                PublishTutorialStarted();
                PublishStateChanged();

                Debug.Log("TutorialManager: Tutorial started");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            
        }

        public void PauseTutorial()
        {
            if (!_isActive || _isPaused)
                return;

            _isPaused = true;
            _currentStep?.PauseStep();
            PublishStateChanged();

            Debug.Log("TutorialManager: Tutorial paused");
        }

        public void ResumeTutorial()
        {
            if (!_isActive || !_isPaused)
                return;

            _isPaused = false;
            _currentStep?.ResumeStep();
            PublishStateChanged();

            Debug.Log("TutorialManager: Tutorial resumed");
        }

        public void StopTutorial(string reason = "User cancelled")
        {
            if (!_isActive)
                return;

            _currentStep?.EndStep();
            _isActive = false;
            _isPaused = false;

            var completionEvent = new TutorialCompletedEvent
            {
                Success = false,
                CompletionTime = Time.time - _tutorialStartTime,
                TotalActions = _totalActions,
                CompletionReason = reason
            };

            PublishTutorialCompleted(completionEvent);
            PublishStateChanged();

            Debug.Log($"TutorialManager: Tutorial stopped - {reason}");
        }

        public void RestartTutorial()
        {
            if (_isActive)
            {
                StopTutorial("Restarted");
            }

            StartTutorialAsync(true).Forget();
        }

        public void SkipCurrentStep()
        {
            if (!_isActive || _currentStep == null || !config.AllowSkipping)
                return;

            Debug.Log($"TutorialManager: Skipping step {_currentStep.StepName}");
            AdvanceToNextStep();
        }

        public void HandlePlayerAction(TutorialActionPerformedEvent actionEvent)
        {
            if (!_isActive || _isPaused)
                return;

            _totalActions++;
            _currentStep?.HandleAction(actionEvent);
        }

        private void HandleStepCompleted(TutorialStepCompletedEvent stepEvent)
        {
            if (stepEvent.Success)
            {
                AdvanceToNextStepDelayed();
            }
        }

        private void StartFirstStep()
        {
            if (config.Steps.Length > 0)
            {
                var firstStepType = config.Steps[0].stepType;
                if (_steps.TryGetValue(firstStepType, out var firstStep))
                {
                    SetCurrentStep(firstStep);
                }
            }
        }

        private void AdvanceToNextStepDelayed()
        {
            if (_currentStep == null)
                return;

            DOVirtual.DelayedCall(config.StepTransitionDelay, AdvanceToNextStep, ignoreTimeScale: true);
        }

        private void AdvanceToNextStep()
        {
            if (_currentStep == null)
                return;

            var nextStepType = config.GetNextStepType(_currentStep.StepType);

            if (nextStepType.HasValue && _steps.TryGetValue(nextStepType.Value, out var nextStep))
            {
                _currentStep.EndStep();
                SetCurrentStep(nextStep);
            }
            else
            {
                CompleteTutorial();
            }
        }

        private void SetCurrentStep(ITutorialStep step)
        {
            _currentStep = step;
            _currentStep.StartStep();
            PublishStateChanged();
        }

        private void CompleteTutorial()
        {
            _currentStep?.EndStep();
            _isActive = false;
            _isPaused = false;

            var completionEvent = new TutorialCompletedEvent
            {
                Success = true,
                CompletionTime = Time.time - _tutorialStartTime,
                TotalActions = _totalActions,
                CompletionReason = "Completed successfully"
            };

            PublishTutorialCompleted(completionEvent);
            PublishStateChanged();

            Debug.Log("TutorialManager: Tutorial completed successfully!");
        }

        private void PublishStateChanged()
        {
            var stateEvent = new TutorialStateChangedEvent
            {
                IsActive = _isActive,
                IsPaused = _isPaused,
                CurrentStep = CurrentStepType
            };

            TutorialEventBus.PublishStateChanged(stateEvent);
        }
        
        private void PublishTutorialStarted()
        {
            TutorialEventBus.PublishTutorialStart(new TutorialStartEvent());
        }

        private void PublishTutorialCompleted(TutorialCompletedEvent completionEvent)
        {
            TutorialEventBus.PublishTutorialCompleted(completionEvent);
        }
        
        
    }
}