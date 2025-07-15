namespace ReusablePatterns.FluencySDK.Enums
{
    [System.Flags]
    public enum QuestionHandlerFlags
    {
        None,
        IsEnabled = 1 << 0,
        PauseTheGame = 1 << 1,
        DisablePauseCountdown = 1 << 2,
        WorksDuringFinishedGame = 1 << 3,
        ProcessResultAfterPresentation = 1 << 4,
    }
}


