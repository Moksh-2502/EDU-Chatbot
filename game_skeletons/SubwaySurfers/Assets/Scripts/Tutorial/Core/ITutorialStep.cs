using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Core
{
    public interface ITutorialStep
    {
        TutorialStepType StepType { get; }
        string StepName { get; }
        int RequiredSuccessfulActions { get; }
        bool IsCompleted { get; }
        bool IsActive { get; }
        float CompletionPercentage { get; }

        void StartStep();
        void EndStep();
        void HandleAction(TutorialActionPerformedEvent actionEvent);
        void ResetStep();
        void PauseStep();
        void ResumeStep();
    }
} 