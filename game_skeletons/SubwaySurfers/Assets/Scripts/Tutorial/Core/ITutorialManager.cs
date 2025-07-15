using Cysharp.Threading.Tasks;
using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.Core
{
    public interface ITutorialManager
    {
        static ITutorialManager Instance { get; set; }
        bool IsActive { get; }
        bool IsPaused { get; }
        TutorialStepType? CurrentStepType { get; }
        ITutorialStep CurrentStep { get; }
        float OverallProgress { get; }

        UniTask StartTutorialAsync(bool forceStart);
        void PauseTutorial();
        void ResumeTutorial();
        void StopTutorial(string reason = "User cancelled");
        void RestartTutorial();
        void SkipCurrentStep();
        void HandlePlayerAction(TutorialActionPerformedEvent actionEvent);
    }
} 