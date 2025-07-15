using UnityEngine;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Steps
{
    public class SwipeRightStep : AutoCompletableStepBase
    {
        public SwipeRightStep(TutorialStepData stepData) : base(stepData)
        {
        }

        protected override void OnStepStarted()
        {
            Debug.Log("SwipeRightStep: Started - Teaching right swipe movement");
            base.OnStepStarted(); // This triggers the auto-completion
        }

        protected override void OnStepCompleted()
        {
            Debug.Log("SwipeRightStep: Completed - Player learned right swipe movement");
        }
    }
} 