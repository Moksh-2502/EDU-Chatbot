using System;
using System.Linq;
using NUnit.Framework;
using FluencySDK.Algorithm;

namespace FluencySDK.Tests
{
    [TestFixture]
    public class QuestionFactoryTests
    {
        private QuestionFactory _questionFactory;
        private LearningAlgorithmConfig _config;
        private Fact _testFact;

        [SetUp]
        public void SetUp()
        {
            _config = LearningAlgorithmConfig.CreateSpeedRun();
            _questionFactory = new QuestionFactory(_config);
            _testFact = new Fact("5x8", 5, 8, "5 × 8 = ?", "test-set");
        }

        #region CreateQuestionForStage Tests

        [Test]
        public void CreateQuestionForStage_Assessment_ShouldCreateQuestionWithCorrectProperties()
        {
            // Act
            var question = _questionFactory.CreateQuestionForStage(_testFact, new AssessmentStage() { TimerSeconds = 123 });

            // Assert
            Assert.IsNotNull(question);
            Assert.IsNotNull(question.Id);
            Assert.AreEqual(_testFact.Id, question.FactId);
            Assert.AreEqual(_testFact.FactSetId, question.FactSetId);
            Assert.AreEqual(_testFact.Text, question.Text);
            Assert.AreEqual(LearningStageType.Assessment, question.LearningStage.Type);
            Assert.AreEqual(LearningMode.Assessment, question.LearningMode);
            Assert.AreEqual(123, question.TimeToAnswer);
            Assert.IsNotNull(question.Choices);
            Assert.IsTrue(question.Choices.Length > 0);
        }

        [Test]
        public void CreateQuestionForStage_Grounding_ShouldCreateUntimedQuestion()
        {
            // Act
            var question = _questionFactory.CreateQuestionForStage(_testFact, new GroundingStage());

            // Assert
            Assert.IsNotNull(question);
            Assert.AreEqual(LearningStageType.Grounding, question.LearningStage.Type);
            Assert.AreEqual(LearningMode.Grounding, question.LearningMode);
            Assert.IsNull(question.TimeToAnswer, "Grounding questions should be untimed");
        }

        [Test]
        public void CreateQuestionForStage_Practice_ShouldUseTimer()
        {
            // Act
            var question = _questionFactory.CreateQuestionForStage(_testFact, new PracticeStage() { TimerSeconds = 5 });

            // Assert
            Assert.IsNotNull(question);
            Assert.AreEqual(LearningStageType.Practice, question.LearningStage.Type);
            Assert.AreEqual(LearningMode.Practice, question.LearningMode);
            Assert.AreEqual(5, question.TimeToAnswer);
        }

        [Test]
        public void CreateQuestionForStage_Review_ShouldUseTimer()
        {
            // Act
            var question = _questionFactory.CreateQuestionForStage(_testFact, new ReviewStage() { TimerSeconds = 7 });

            // Assert
            Assert.IsNotNull(question);
            Assert.AreEqual(LearningStageType.Review, question.LearningStage.Type);
            Assert.AreEqual(LearningMode.Practice, question.LearningMode);
            Assert.AreEqual(7, question.TimeToAnswer);
        }

        [Test]
        public void CreateQuestionForStage_Repetition_ShouldUseTimer()
        {
            // Act
            var question = _questionFactory.CreateQuestionForStage(_testFact, new RepetitionStage() { TimerSeconds = 10 });

            // Assert
            Assert.IsNotNull(question);
            Assert.AreEqual(LearningStageType.Repetition, question.LearningStage.Type);
            Assert.AreEqual(LearningMode.Practice, question.LearningMode);
            Assert.AreEqual(10, question.TimeToAnswer);
        }

        [Test]
        public void CreateQuestionForStage_Mastered_ShouldHaveNullTimer()
        {
            // Act
            var question = _questionFactory.CreateQuestionForStage(_testFact, new MasteredStage());

            // Assert
            Assert.IsNotNull(question);
            Assert.AreEqual(LearningStageType.Mastered, question.LearningStage.Type);
            Assert.AreEqual(LearningMode.Practice, question.LearningMode);
            Assert.IsNull(question.TimeToAnswer);
        }

        #endregion

        #region Question Structure Tests

        [Test]
        public void CreateQuestionForStage_AllStages_ShouldHaveUniqueIds()
        {
            // Arrange
            var stages = new LearningStage[]
            {
                new AssessmentStage(),
                new GroundingStage(),
                new PracticeStage() { TimerSeconds = 5 },
                new ReviewStage() { TimerSeconds = 7 },
                new RepetitionStage() { TimerSeconds = 10 },
                new MasteredStage()
            };

            // Act
            var questions = new Question[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                questions[i] = _questionFactory.CreateQuestionForStage(_testFact, stages[i]);
            }

            // Assert
            for (int i = 0; i < questions.Length; i++)
            {
                for (int j = i + 1; j < questions.Length; j++)
                {
                    Assert.AreNotEqual(questions[i].Id, questions[j].Id, 
                        $"Questions should have unique IDs: {stages[i]} vs {stages[j]}");
                }
            }
        }

        [Test]
        public void CreateQuestionForStage_AllStages_ShouldPreserveFactInformation()
        {
            // Arrange
            var stages = new LearningStage[]
            {
                new AssessmentStage(),
                new GroundingStage(),
                new PracticeStage() { TimerSeconds = 5 },
                new ReviewStage() { TimerSeconds = 7 },
                new RepetitionStage() { TimerSeconds = 10 },
                new MasteredStage()
            };

            // Act & Assert
            foreach (var stage in stages)
            {
                var question = _questionFactory.CreateQuestionForStage(_testFact, stage);
                
                Assert.AreEqual(_testFact.Id, question.FactId, $"FactId should be preserved for {stage}");
Assert.AreEqual(_testFact.FactSetId, question.FactSetId, $"FactSetId should be preserved for {stage}");
                Assert.AreEqual(_testFact.Text, question.Text, $"Text should be preserved for {stage}");
            }
        }

        [Test]
        public void CreateQuestionForStage_AllStages_ShouldHaveAnswerChoices()
        {
            // Arrange
            var stages = new LearningStage[]
            {
                new AssessmentStage(),
                new GroundingStage(),
                new PracticeStage() { TimerSeconds = 5 },
                new ReviewStage() { TimerSeconds = 7 },
                new RepetitionStage() { TimerSeconds = 10 },
                new MasteredStage()
            };

            // Act & Assert
            foreach (var stage in stages)
            {
                var question = _questionFactory.CreateQuestionForStage(_testFact, stage);
                
                Assert.IsNotNull(question.Choices, $"Choices should not be null for {stage}");
Assert.IsTrue(question.Choices.Length > 0, $"Should have answer choices for {stage}");
                
                // Check that exactly one choice is correct
var correctChoices = 0;
                foreach (var choice in question.Choices)
                {
                    if (choice.IsCorrect)
                        correctChoices++;
                }
                Assert.AreEqual(1, correctChoices, $"Should have exactly one correct choice for {stage}");
            }
        }

        [Test]
        public void CreateQuestionForStage_AllStages_ShouldHaveCorrectAnswer()
        {
            // Arrange
            var stages = new LearningStage[]
            {
                new AssessmentStage(),
                new GroundingStage(),
                new PracticeStage() { TimerSeconds = 5 },
                new ReviewStage() { TimerSeconds = 7 },
                new RepetitionStage() { TimerSeconds = 10 },
                new MasteredStage()
            };

            var expectedAnswer = _testFact.FactorA * _testFact.FactorB; // 5 * 8 = 40

            // Act & Assert
            foreach (var stage in stages)
            {
                var question = _questionFactory.CreateQuestionForStage(_testFact, stage);
                
                var correctChoice = question.Choices.FirstOrDefault(c => c.IsCorrect);
Assert.IsNotNull(correctChoice, $"Should have a correct choice for {stage}");
                Assert.AreEqual(expectedAnswer, correctChoice.Value, $"Correct answer should be {expectedAnswer} for {stage}");
            }
        }

        #endregion

        #region Learning Mode Conversion Tests

        [Test]
        public void CreateQuestionForStage_LearningModeConversion_ShouldBeCorrect()
        {
            // Test Assessment -> Assessment
            var assessmentQuestion = _questionFactory.CreateQuestionForStage(_testFact, new AssessmentStage());
            Assert.AreEqual(LearningMode.Assessment, assessmentQuestion.LearningMode);

            // Test Grounding -> Grounding
            var groundingQuestion = _questionFactory.CreateQuestionForStage(_testFact, new GroundingStage());
            Assert.AreEqual(LearningMode.Grounding, groundingQuestion.LearningMode);

            // Test Practice stages -> Practice
            var practiceStages = new LearningStage[]
            {
                new PracticeStage() { TimerSeconds = 5 },
                new ReviewStage() { TimerSeconds = 7 },
                new RepetitionStage() { TimerSeconds = 10 },
            };

            foreach (var stage in practiceStages)
            {
                var question = _questionFactory.CreateQuestionForStage(_testFact, stage);
                Assert.AreEqual(LearningMode.Practice, question.LearningMode, 
                    $"Stage {stage} should map to Practice learning mode");
            }
        }

        #endregion

        #region Timer Configuration Tests

        [Test]
        public void CreateQuestionForStage_TimerConfiguration_ShouldMatchConfig()
        {
            // Test Assessment timer
            var assessmentQuestion = _questionFactory.CreateQuestionForStage(_testFact, new AssessmentStage() { TimerSeconds = 123 });
            Assert.AreEqual(123, assessmentQuestion.TimeToAnswer, "Assessment timer should match config");

            // Test slow practice timer
            var practiceSlowQuestion = _questionFactory.CreateQuestionForStage(_testFact, new PracticeStage() { TimerSeconds = _config.FluencyBigTimer });
            Assert.AreEqual(_config.FluencyBigTimer, practiceSlowQuestion.TimeToAnswer, "PracticeSlow timer should match config");

            // Test fast practice timer (PracticeFast, Review, Repetition)
            var practiceStages = new LearningStage[]
            {
                new PracticeStage() { TimerSeconds = _config.FluencySmallTimer },
                new ReviewStage() { TimerSeconds = _config.FluencySmallTimer },
                new RepetitionStage() { TimerSeconds = _config.FluencySmallTimer },
            };

            foreach (var stage in practiceStages)
            {
                var question = _questionFactory.CreateQuestionForStage(_testFact, stage);
                Assert.AreEqual(_config.FluencySmallTimer, question.TimeToAnswer, 
                    $"{stage} timer should match FluencySmallTimer config");
            }

            // Test untimed stages
            var untimedStages = new LearningStage[]
            {
                new GroundingStage(),
                new MasteredStage()
            };

            foreach (var stage in untimedStages)
            {
                var question = _questionFactory.CreateQuestionForStage(_testFact, stage);
                Assert.IsNull(question.TimeToAnswer, $"{stage} should be untimed");
            }
        }

        #endregion

        #region Different Facts Tests

        [Test]
        public void CreateQuestionForStage_DifferentFacts_ShouldCreateDistinctQuestions()
        {
            // Arrange
            var fact1 = new Fact("2x3", 2, 3, "2 × 3 = ?", "test-set");
            var fact2 = new Fact("7x9", 7, 9, "7 × 9 = ?", "test-set");

            // Act
            var question1 = _questionFactory.CreateQuestionForStage(fact1, new AssessmentStage());
            var question2 = _questionFactory.CreateQuestionForStage(fact2, new AssessmentStage());

            // Assert
            Assert.AreNotEqual(question1.FactId, question2.FactId);
            Assert.AreNotEqual(question1.Text, question2.Text);
            Assert.AreNotEqual(question1.Choices.First(c => c.IsCorrect).Value, question2.Choices.First(c => c.IsCorrect).Value);
            Assert.AreEqual(6, question1.Choices.First(c => c.IsCorrect).Value); // 2 * 3
            Assert.AreEqual(63, question2.Choices.First(c => c.IsCorrect).Value); // 7 * 9
        }

        [Test]
        public void CreateQuestionForStage_DifferentFactSets_ShouldPreserveFactSetId()
        {
            // Arrange
            var factSetA = new Fact("4x5", 4, 5, "4 × 5 = ?", "set-a");
            var factSetB = new Fact("6x7", 6, 7, "6 × 7 = ?", "set-b");

            // Act
            var questionA = _questionFactory.CreateQuestionForStage(factSetA, new AssessmentStage());
            var questionB = _questionFactory.CreateQuestionForStage(factSetB, new AssessmentStage());

            // Assert
            Assert.AreEqual("set-a", questionA.FactSetId);
            Assert.AreEqual("set-b", questionB.FactSetId);
        }

        #endregion
    }
} 