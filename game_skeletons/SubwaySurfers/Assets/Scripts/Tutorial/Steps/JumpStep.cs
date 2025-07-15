using UnityEngine;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Steps
{
    public class JumpStep : AutoCompletableStepBase
    {
        public JumpStep(TutorialStepData stepData) : base(stepData)
        {
        }

        protected override void OnStepStarted()
        {
            Debug.Log("JumpStep: Started - Teaching jump mechanics");
            base.OnStepStarted(); // This triggers the auto-completion
        }

        protected override void OnStepCompleted()
        {
            Debug.Log("JumpStep: Completed - Player learned jumping");
        }
    }
} 