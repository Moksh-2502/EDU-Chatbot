using UnityEngine;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Steps
{
    public class SlideStep : AutoCompletableStepBase
    {
        public SlideStep(TutorialStepData stepData) : base(stepData)
        {
        }

        protected override void OnStepStarted()
        {
            Debug.Log("SlideStep: Started - Teaching slide mechanics");
            base.OnStepStarted(); // This triggers the auto-completion
        }

        protected override void OnStepCompleted()
        {
            Debug.Log("SlideStep: Completed - Player learned sliding");
        }
    }
} 