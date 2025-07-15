namespace ReusablePatterns.SharedCore.Runtime.PauseSystem
{
    public interface IGamePauser
    {
        static IGamePauser Instance { get; set; }
        event PauseEventHandler OnPaused;
        event PauseEventHandler OnResumed;
        event PauseEventHandler OnResumeStarted;
        event PauseEventHandler<float> OnCountdownUpdated;
        event PauseEventHandler OnCountdownFinished;
        void Pause(PauseData data);
        void Resume();
        bool IsPaused { get; }
    }
}