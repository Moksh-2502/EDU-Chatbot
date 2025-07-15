using Cysharp.Threading.Tasks;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using FluencySDK.Unity.Data;
using ReusablePatterns.FluencySDK.Enums;
namespace FluencySDK
{
    public delegate void QuestionStartedEventArgs(IQuestion question);

    public delegate void QuestionEndedEventArgs(IQuestion question, UserAnswerSubmission userAnswerSubmission);

    public delegate void QuestionReadyEventArgs(IQuestion question);
    public delegate void QuestionAnswerSubmitEventArgs(IQuestion question, SubmitAnswerResult submissionResult);
    /// <summary>
    /// Interface for question provider components
    /// </summary>
    public interface IQuestionProvider
    {
        event QuestionAnswerSubmitEventArgs OnQuestionAnswerSubmitAttempted;
        /// <summary>
        /// Event triggered when a new question is started
        /// </summary>
        event QuestionStartedEventArgs OnQuestionStarted;
        
        /// <summary>
        /// Event triggered when a question is ended
        /// </summary>
        event QuestionEndedEventArgs OnQuestionEnded;
        
        event QuestionReadyEventArgs OnQuestionReady;
        QuestionGenerationMode QuestionGenerationMode { get; }
        StudentState StudentState { get; }
        ILearningAlgorithm Algorithm { get; }

        /// <summary>
        /// Exposes the current learning algorithm configuration
        /// </summary>
        LearningAlgorithmConfig Config { get; }

        /// <summary>
        /// Initialize the question provider with configuration
        /// </summary>
        /// <param name="config">Configuration for question generation</param>
        void Initialize(ISDKTimeProvider timeProvider, LearningAlgorithmConfig config, FluencyAudioConfig fluencyAudioConfig);
        
        /// <summary>
        /// Submit an answer to the current question
        /// </summary>
        /// <param name="question">The question</param>
        /// <param name="userAnswerSubmission">The user's answer</param>
        void SubmitAnswer(IQuestion question, UserAnswerSubmission userAnswerSubmission);

        string GetCanShowQuestionError();

        UniTask StartQuestion(IQuestion question);
        void Start();
        void Stop();
    }

    public interface IQuestionGenerationGateRegistry
    {
        void RegisterQuestionGenerationGate(IQuestionGenerationGate gate);
        void UnregisterQuestionGenerationGate(IQuestionGenerationGate gate);
    }
} 