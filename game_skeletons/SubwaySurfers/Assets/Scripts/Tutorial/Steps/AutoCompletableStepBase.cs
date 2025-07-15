using DG.Tweening;
using SubwaySurfers.Tutorial.Core;
using SubwaySurfers.Tutorial.Data;
using SubwaySurfers.Tutorial.Events;
using UnityEngine;

namespace SubwaySurfers.Tutorial.Steps
{
    public abstract class AutoCompletableStepBase : TutorialStepBase
    {
        
        protected AutoCompletableStepBase(TutorialStepData stepData) : base(stepData)
        {
        }

        protected override bool ValidateAction(TutorialActionPerformedEvent actionEvent)
        {
            return true; // Always return true - we don't care about specific player input
        }

        protected override void OnStepStarted()
        {
            float delay = _stepData.autoCompleteDelay;
            Debug.Log($"{GetType().Name}: Step started - Auto-completing in {delay} seconds");
            DOVirtual.DelayedCall(delay, CompleteStep, ignoreTimeScale: false);
        }
        
        protected override void OnValidActionPerformed(TutorialActionPerformedEvent actionEvent)
        {
            Debug.Log($"{GetType().Name}: Action performed - Still auto-completing on timer");
        }
        
        protected override void OnInvalidActionPerformed(TutorialActionPerformedEvent actionEvent)
        {
            Debug.Log($"{GetType().Name}: Action performed - Still auto-completing on timer");
        }
        
        protected override void OnStepCompleted()
        {
            Debug.Log($"{GetType().Name}: Step completed - Proceeding to next step");
        }
    }
}