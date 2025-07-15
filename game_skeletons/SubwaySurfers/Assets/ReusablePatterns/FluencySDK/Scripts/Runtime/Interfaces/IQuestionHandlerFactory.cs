namespace ReusablePatterns.FluencySDK.Scripts.Interfaces
{
    public interface IQuestionHandlerFactory
    {
        void RegisterHandler(IQuestionGameplayHandler handler);
        void UnregisterHandler(IQuestionGameplayHandler handler);
    }
}