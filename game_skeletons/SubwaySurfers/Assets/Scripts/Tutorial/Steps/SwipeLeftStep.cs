using UnityEngine;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Steps
{
    public class SwipeLeftStep : AutoCompletableStepBase
    {
        public SwipeLeftStep(TutorialStepData stepData) : base(stepData)
        {
        }

        protected override void OnStepStarted()
        {
            Debug.Log("SwipeLeftStep: Started - Teaching left lane movement");
            base.OnStepStarted(); // This triggers the auto-completion
        }

        protected override void OnStepCompleted()
        {
            Debug.Log("SwipeLeftStep: Completed - Player learned left lane movement");
        }
    }
} 