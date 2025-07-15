using FluencySDK;
using FluencySDK.Unity;
using SubwaySurfers.Tutorial.Core;
using SubwaySurfers.Tutorial.Data;
using SubwaySurfers.Tutorial.Events;
using SubwaySurfers.Tutorial.Integration;
using SubwaySurfers.Tutorial.Integration.Education;
using UnityEngine;

namespace SubwaySurfers.Tutorial.Steps
{
    public class QuestionTutorialStep : TutorialStepBase
    {
        private IQuestionProvider _questionProvider;
        private IQuestion _mockQuestion;
        private bool _questionAnswered = false;
        private TutorialQuestionHandler _tutorialQuestionHandler;

        public QuestionTutorialStep(TutorialStepData stepData) : base(stepData) { }

        protected override void OnStepStarted()
        {
            _questionProvider = BaseQuestionProvider.Instance;
            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionEnded += OnQuestionAnswered;

                // Create and inject mock question
                _mockQuestion = CreateMockMasteryQuestion();

                _tutorialQuestionHandler = Object.FindFirstObjectByType<TutorialQuestionHandler>(FindObjectsInactive.Include);
                _tutorialQuestionHandler.HandleQuestion(_mockQuestion);

                Debug.Log("[QuestionTutorialStep] Mock mastery question created and injected for tutorial");
            }
            else
            {
                Debug.LogError("[QuestionTutorialStep] No BaseQuestionProvider found - tutorial question cannot be shown");
                // Complete the step immediately if no provider available
                CompleteStep();
            }
        }

        protected override void OnStepEnded()
        {
            if (_questionProvider != null)
            {
                _questionProvider.OnQuestionEnded -= OnQuestionAnswered;
            }
        }

        private void OnQuestionAnswered(IQuestion question, UserAnswerSubmission submission)
        {
            if (question?.Id == _mockQuestion?.Id)
            {
                _questionAnswered = true;
                Debug.Log($"[QuestionTutorialStep] Tutorial question answered with: {submission.AnswerType}");
                CompleteStep();
            }
        }

        protected override bool ValidateAction(TutorialActionPerformedEvent actionEvent)
        {
            return _questionAnswered;
        }

        private IQuestion CreateMockMasteryQuestion()
        {
            // Create a mock fact for tutorial
            var mockFact = new Fact(id: "tutorial_mock_fact",
                factSetId: "tutorial",
                factorA: 1,
                factorB: 1,
                text: "What is 1 × 1?");

            return new TutorialQuestion(mockFact)
            {
                Id = "tutorial_question_" + System.Guid.NewGuid(),
                Text = mockFact.Text,
                Choices = new[]
                {
                    new QuestionChoice<int> { Value = 1, IsCorrect = true },
                    new QuestionChoice<int> { Value = 2, IsCorrect = false },
                    new QuestionChoice<int> { Value = 3, IsCorrect = false },
                    new QuestionChoice<int> { Value = 4, IsCorrect = false }
                },
                LearningMode = LearningMode.Grounding,
                LearningStage = new GroundingStage(),
                TimeToAnswer = null 
            };
        }
    }
}