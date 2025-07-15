namespace ReusablePatterns.FluencySDK.Scripts.Interfaces
{
    /// <summary>
    /// Interface for components that can gate/block question generation.
    /// Implementations can prevent the next question from being generated while they're in a blocking state.
    /// </summary>
    public interface IQuestionGenerationGate
    {
        /// <summary>
        /// Indicates whether the next question can be generated.
        /// Return false to block question generation until this gate allows it.
        /// </summary>
        bool CanGenerateNextQuestion { get; }
        
        /// <summary>
        /// Optional identifier for debugging purposes
        /// </summary>
        string GateIdentifier { get; }
    }
} 