using Cysharp.Threading.Tasks;
using FluencySDK;
using EducationIntegration.QuestionHandlers;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using SubwaySurfers.Tutorial.Integration.Education;
using UnityEngine;

namespace SubwaySurfers.Tutorial.Integration
{
    /// <summary>
    /// Special question handler for tutorial questions that doesn't affect progression or analytics
    /// </summary>
    public class TutorialQuestionHandler : BaseQuestionHandler
    {
        public override string HandlerIdentifier => "TutorialQuestionHandler";

        protected override void Initialize()
        {
            base.Initialize();
            this.QuestionResultProcessor = new TutorialQuestionResultProcessor();
        }

        protected override bool DoHandleQuestion(IQuestion question)
        {
            QuestionProvider.StartQuestion(question).Forget();
            return true;
        }

        protected override void ProcessOnQuestionStarted()
        {
            Debug.Log("[TutorialQuestionHandler] Tutorial question started - mastery mode with retry enabled");
            // Tutorial-specific processing if needed
        }

        protected override void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission)
        {
            Debug.Log($"[TutorialQuestionHandler] Tutorial question ended with: {userAnswerSubmission.AnswerType}");
            
            // IMPORTANT: Don't process results for tutorial questions
            // No progression tracking, no analytics, no score updates
            // This ensures the tutorial question doesn't affect the student's actual progress
        }

        public override QuestionHandlerResult CanHandleQuestionNow(IQuestion question)
        {
            // First check base conditions
            var baseResult = base.CanHandleQuestionNow(question);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            // Only handle tutorial questions
            if (question is TutorialQuestion)
            {
                Debug.Log($"[TutorialQuestionHandler] Can handle tutorial question: {question.Id}");
                return QuestionHandlerResult.CreateSuccess(question);
            }
            
            return QuestionHandlerResult.CreateError(question, "Not a tutorial question - only handles questions with FactSetId='tutorial'");
        }
    }
} 