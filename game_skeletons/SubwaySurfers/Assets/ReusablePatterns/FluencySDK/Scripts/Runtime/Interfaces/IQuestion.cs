using System.Linq;
using FluencySDK.Unity;

namespace FluencySDK
{
    public interface IQuestion
    {
        string Id { get; }
        string Text { get; }
        QuestionChoice<int>[] Choices { get; set; }
        long? TimeStarted { get; set; }
        long? TimeEnded { get; set; }
        float? TimeToAnswer { get; set; }
        IQuestionTimer Timer { get; set; }
        string FactId { get; }
        string FactSetId { get; }
        bool IsMock { get; }
        
        LearningMode LearningMode { get; }
        LearningStage LearningStage { get; }
        
        QuestionChoice<int> GetCorrectChoice()
        {
            return Choices?.FirstOrDefault(choice => choice.IsCorrect);
        }
    }
} 