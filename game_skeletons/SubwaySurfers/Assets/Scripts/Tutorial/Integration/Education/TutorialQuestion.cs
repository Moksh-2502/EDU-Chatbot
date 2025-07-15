using FluencySDK;

namespace SubwaySurfers.Tutorial.Integration.Education
{
    public class TutorialQuestion : Question
    {
        public override bool IsMock => true;

        public TutorialQuestion(Fact fact) : base(fact)
        {
        }
    }
}