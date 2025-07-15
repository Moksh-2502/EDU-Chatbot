using FluencySDK;
using EducationIntegration.QuestionResultProcessor;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;

namespace EducationIntegration.QuestionHandlers
{
    public class ReviveQuestionHandler : BaseQuestionHandler, IQuestionResultProcessor
    {
        public override string HandlerIdentifier => "revive_questions";
        public bool IsReviveAvailable {get; set; }
        protected override void Initialize()
        {
            base.Initialize();
            QuestionResultProcessor = this;
        }
        protected override void DoSubscribeToEvents()
        {
            base.DoSubscribeToEvents();
            GameState.OnSecondWindRequested += OnSecondWindRequested;
        }
        protected override void DoUnsubscribeFromEvents()
        {
            base.DoUnsubscribeFromEvents();
            GameState.OnSecondWindRequested -= OnSecondWindRequested;
        }
        protected override void ProcessOnQuestionStarted()
        {

        }

        protected override void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission)
        {
            
        }

        protected override bool DoHandleQuestion(IQuestion question)
        {
            QuestionProvider.StartQuestion(this.Question);
            IsReviveAvailable = false;
            return true;
        }

        private void OnSecondWindRequested()
        {
            IsReviveAvailable = true;
        }

        public void ProcessQuestionResult(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            if(userAnswerSubmission.AnswerType == AnswerType.Correct)
            {
                GameState.SecondWind();
            }
            else
            {
                GameState.GameOver();
            }
        }

        public override QuestionHandlerResult CanHandleQuestionNow(IQuestion question)
        {
            var baseResult = base.CanHandleQuestionNow(question);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            if (!IsReviveAvailable)
            {
                return QuestionHandlerResult.CreateError(question, "Revive is not available.");
            }

            return QuestionHandlerResult.CreateSuccess(question);
        }
    }
}
