using System.Threading.Tasks;

namespace FluencySDK
{
    public interface IFluencyGenerator
    {
        Task Initialize(FluencyGeneratorConfig config = null);
        Task<IQuestion[]> GetNextQuestionBlock();
        Task<SubmitAnswerResult> SubmitAnswer(string questionId, int answer, int responseTimeMs);
        Task<StudentState> GetStudentState();
        Task SetMode(LearningMode mode);
        Task<MasteryCheckResult> CheckMastery();
        Task ResetState(); // Added based on README, useful for users.
    }
} 