using UnityEngine;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Steps
{
    public class LeftRightSwipeStep : AutoCompletableStepBase
    {
        public LeftRightSwipeStep(TutorialStepData stepData) : base(stepData)
        {
        }

        protected override void OnStepStarted()
        {
            Debug.Log("LeftRightSwipeStep: Started - Teaching lane changes");
            base.OnStepStarted(); // This triggers the auto-completion
        }

        protected override void OnStepCompleted()
        {
            Debug.Log("LeftRightSwipeStep: Completed - Player learned lane changes");
        }
    }
} 