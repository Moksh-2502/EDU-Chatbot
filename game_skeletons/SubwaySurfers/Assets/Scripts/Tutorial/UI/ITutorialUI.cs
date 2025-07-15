using SubwaySurfers.Tutorial.Events;

namespace SubwaySurfers.Tutorial.UI
{
    public interface ITutorialUI
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
        void UpdateContent(string message, object data = null);
    }

    public interface ITutorialInstructionUI : ITutorialUI
    {
        void ShowInstructions(string instructions);
        void HideInstructions();
        void UpdateInstructions(string instructions);
    }

    public interface ITutorialProgressUI : ITutorialUI
    {
        void ShowProgress(TutorialProgressEvent progressData);
        void HideProgress();
        void UpdateProgress(float percentage, int current, int total);
    }

    public interface ITutorialUIController
    {
        void Initialize();
        void HandleUIEvent(TutorialUIEvent uiEvent);
        void ShowTutorialUI();
        void HideTutorialUI();
    }
} 