using SubwaySurfers.Tutorial.Data;
using UnityEngine;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Core
{
    public abstract class TutorialStepBase : ITutorialStep
    {
        protected TutorialStepData _stepData;
        protected int _successfulActions = 0;
        protected bool _isActive = false;
        protected bool _isCompleted = false;
        protected bool _isPaused = false;
        protected float _startTime;

        public TutorialStepBase(TutorialStepData stepData)
        {
            _stepData = stepData;
        }


        public TutorialStepType StepType => _stepData.stepType;
        public string StepName => _stepData.stepName;
        public int RequiredSuccessfulActions => _stepData.requiredSuccessfulActions;
        public bool IsCompleted => _isCompleted;
        public bool IsActive => _isActive;

        public float CompletionPercentage => RequiredSuccessfulActions > 0
            ? Mathf.Clamp01((float)_successfulActions / RequiredSuccessfulActions)
            : 0f;

        public virtual void StartStep()
        {
            _isActive = true;
            _isCompleted = false;
            _isPaused = false;
            _successfulActions = 0;
            _startTime = Time.time;

            OnStepStarted();

            // Publish step started event
            TutorialEventBus.PublishStepStarted(new TutorialStepStartedEvent
            {
                StepType = StepType,
                StepName = StepName
            });

            // Publish UI event to show instructions
            TutorialEventBus.PublishUIEvent(new TutorialUIEvent
            {
                EventType = TutorialUIEvent.UIEventType.ShowInstructions,
                Message = _stepData.GetPlatformInstructions()
            });
        }

        public virtual void EndStep()
        {
            _isActive = false;
            OnStepEnded();
        }

        public virtual void HandleAction(TutorialActionPerformedEvent actionEvent)
        {
            if (!_isActive || _isPaused || _isCompleted)
                return;

            bool wasActionValid = ValidateAction(actionEvent);

            if (wasActionValid)
            {
                _successfulActions++;
                OnValidActionPerformed(actionEvent);

                // Publish progress update
                TutorialEventBus.PublishProgressChanged(new TutorialProgressEvent
                {
                    CurrentStep = StepType,
                    SuccessfulActions = _successfulActions,
                    RequiredActions = RequiredSuccessfulActions,
                    CompletionPercentage = CompletionPercentage
                });

                // Check if step is completed
                if (_successfulActions >= RequiredSuccessfulActions)
                {
                    CompleteStep();
                }
            }
            else
            {
                OnInvalidActionPerformed(actionEvent);
            }
        }

        public virtual void ResetStep()
        {
            _successfulActions = 0;
            _isCompleted = false;
            _isPaused = false;
            OnStepReset();
        }

        public virtual void PauseStep()
        {
            _isPaused = true;
            OnStepPaused();
        }

        public virtual void ResumeStep()
        {
            _isPaused = false;
            OnStepResumed();
        }

        protected virtual void CompleteStep()
        {
            _isCompleted = true;
            OnStepCompleted();
            // Publish step completed event
            TutorialEventBus.PublishStepCompleted(new TutorialStepCompletedEvent
            {
                StepType = StepType,
                StepName = StepName,
                Success = _isCompleted
            });
        }

        // Abstract and virtual methods for derived classes to override
        protected abstract bool ValidateAction(TutorialActionPerformedEvent actionEvent);

        protected virtual void OnStepStarted()
        {
        }

        protected virtual void OnStepEnded()
        {
        }

        protected virtual void OnStepCompleted()
        {
        }

        protected virtual void OnStepReset()
        {
        }

        protected virtual void OnStepPaused()
        {
        }

        protected virtual void OnStepResumed()
        {
        }

        protected virtual void OnValidActionPerformed(TutorialActionPerformedEvent actionEvent)
        {
        }

        protected virtual void OnInvalidActionPerformed(TutorialActionPerformedEvent actionEvent)
        {
        }
    }
}