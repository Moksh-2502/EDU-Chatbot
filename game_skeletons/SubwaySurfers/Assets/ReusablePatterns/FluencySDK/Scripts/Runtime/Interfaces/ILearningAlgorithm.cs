using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace FluencySDK
{
    public interface ILearningAlgorithm
    {
        static event Action<ILearningAlgorithmEvent> LearningAlgorithmEvent;

        IReadOnlyDictionary<string, FactSet> FactSets { get; }
        StudentState StudentState { get; }
        UniTask Initialize();
        UniTask<IQuestion> GetNextQuestion();
        UniTask StartQuestion(IQuestion question);
        UniTask<SubmitAnswerResult> SubmitAnswer(IQuestion question, UserAnswerSubmission userAnswerSubmission);
        void NotifyLearningAlgorithmEvent(ILearningAlgorithmEvent eventInfo)
        {
            LearningAlgorithmEvent?.Invoke(eventInfo);
        }
    }
}