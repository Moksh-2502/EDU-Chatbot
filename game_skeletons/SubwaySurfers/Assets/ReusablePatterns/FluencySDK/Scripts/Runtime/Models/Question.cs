using FluencySDK.Unity;

namespace FluencySDK
{
    public class Question : IQuestion
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public QuestionChoice<int>[] Choices { get; set; }
        public long? TimeStarted { get; set; }
        public long? TimeEnded { get; set; }
        public float? TimeToAnswer { get; set; }
        public IQuestionTimer Timer { get; set; }
        public string FactId { get; private set; }
        public string FactSetId { get; private set; }
        public LearningMode LearningMode { get; set; }
        public LearningStage LearningStage { get; set; }

        public virtual bool IsMock { get; } = false;

        public Question(Fact fact)
        {
            this.FactId = fact.Id;
            this.FactSetId = fact.FactSetId;
            this.LearningMode = LearningMode.Assessment;
        }
    }
}