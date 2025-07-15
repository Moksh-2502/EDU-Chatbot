using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FluencySDK.Algorithm;
using FluencySDK.Services;
using FluencySDK.Tests.Mocks;

namespace FluencySDK.Tests
{
    [TestFixture]
    public class DifficultyManagerTests
    {
        private DynamicDifficultyConfig _config;
        private DifficultyManager _difficultyManager;

        private AssessmentStage _assessmentStage = new AssessmentStage() {
            Id = "assessment",
        };

        private PracticeStage _practiceSlowStage = new PracticeStage() {
            Id = "practice-slow",
        };

        private PracticeStage _practiceFastStage = new PracticeStage() {
            Id = "practice-fast",
        };

        [SetUp]
        public void SetUp()
        {
            // Create test configuration with multiple difficulties
            _config = new DynamicDifficultyConfig
            {
                MinAnswersForDifficultyChange = 3,
                RecentAnswerWindow = 10,
                Difficulties = new List<DifficultyConfig>
                {
                    new DifficultyConfig
                    {
                        Name = "Easy",
                        MinAccuracyThreshold = 0.0f,
                        MaxFactsBeingLearned = 2,
                        PromotionThresholds = new Dictionary<string, int>
                        {
                            { _assessmentStage.Id, 2 },
                            { _practiceSlowStage.Id, 2 },
                            { _practiceFastStage.Id, 2 }
                        },
                        DemotionThresholds = new Dictionary<string, int>
                        {
                            { _assessmentStage.Id, 3 },
                            { _practiceSlowStage.Id, 3 },
                            { _practiceFastStage.Id, 3 }
                        }
                    },
                    new DifficultyConfig
                    {
                        Name = "Medium",
                        MinAccuracyThreshold = 0.5f,
                        MaxFactsBeingLearned = 5,
                        PromotionThresholds = new Dictionary<string, int>
                        {
                            { _assessmentStage.Id, 1 },
                            { _practiceSlowStage.Id, 1 },
                            { _practiceFastStage.Id, 1 }
                        },
                        DemotionThresholds = new Dictionary<string, int>
                        {
                            { _assessmentStage.Id, 2 },
                            { _practiceSlowStage.Id, 2 },
                            { _practiceFastStage.Id, 2 }
                        }
                    },
                    new DifficultyConfig
                    {
                        Name = "Hard",
                        MinAccuracyThreshold = 0.8f,
                        MaxFactsBeingLearned = 10,
                        PromotionThresholds = new Dictionary<string, int>
                        {
                            { _assessmentStage.Id, 1 },
                            { _practiceSlowStage.Id, 1 },
                            { _practiceFastStage.Id, 1 }
                        },
                        DemotionThresholds = new Dictionary<string, int>
                        {
                            { _assessmentStage.Id, 2 },
                            { _practiceSlowStage.Id, 2 },
                            { _practiceFastStage.Id, 2 }
                        }
                    }
                }
            };

            _difficultyManager = new DifficultyManager(_config);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidConfig_ShouldInitializeWithLowestThresholdDifficulty()
        {
            // Act & Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void Constructor_SingleDifficulty_ShouldInitializeWithThatDifficulty()
        {
            // Arrange
            var singleConfig = new DynamicDifficultyConfig
            {
                Difficulties = new List<DifficultyConfig>
                {
                    new DifficultyConfig { Name = "OnlyOne", MinAccuracyThreshold = 0.5f }
                }
            };

            // Act
            var manager = new DifficultyManager(singleConfig);

            // Assert
            Assert.AreEqual("OnlyOne", manager.CurrentDifficulty);
        }

        #endregion

        #region UpdateDifficulty Tests

        [Test]
        public void UpdateDifficulty_InsufficientAnswers_ShouldNotChangeDifficulty()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_HighAccuracy_ShouldUpgradeToHardDifficulty()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Hard", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_MediumAccuracy_ShouldUpgradeToMediumDifficulty()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Medium", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_LowAccuracy_ShouldStayOnEasyDifficulty()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_FromHardToEasy_ShouldDowngradeCorrectly()
        {
            // Arrange - First upgrade to Hard
            var highAccuracyAnswers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            _difficultyManager.UpdateDifficulty(highAccuracyAnswers);
            Assert.AreEqual("Hard", _difficultyManager.CurrentDifficulty);

            // Act - Now downgrade with low accuracy
            var lowAccuracyAnswers = new List<AnswerRecord>
            {
                new AnswerRecord("fact6", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact7", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact8", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact9", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact10", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            _difficultyManager.UpdateDifficulty(lowAccuracyAnswers);

            // Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_ExactThresholdAccuracy_ShouldUpgradeToCorrectDifficulty()
        {
            // Arrange - Exactly 80% accuracy (Hard threshold)
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Hard", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_EmptyAnswers_ShouldNotChangeDifficulty()
        {
            // Arrange
            var answers = new List<AnswerRecord>();

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        #endregion

        #region GetCurrentDifficultyConfig Tests

        [Test]
        public void GetCurrentDifficultyConfig_InitialState_ShouldReturnEasyConfig()
        {
            // Act
            var config = _difficultyManager.GetCurrentDifficultyConfig();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual("Easy", config.Name);
            Assert.AreEqual(0.0f, config.MinAccuracyThreshold);
            Assert.AreEqual(2, config.MaxFactsBeingLearned);
        }

        [Test]
        public void GetCurrentDifficultyConfig_AfterDifficultyChange_ShouldReturnNewConfig()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            _difficultyManager.UpdateDifficulty(answers);

            // Act
            var config = _difficultyManager.GetCurrentDifficultyConfig();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual("Hard", config.Name);
            Assert.AreEqual(0.8f, config.MinAccuracyThreshold);
            Assert.AreEqual(10, config.MaxFactsBeingLearned);
        }



        #endregion

        #region CurrentDifficulty Property Tests

        [Test]
        public void CurrentDifficulty_InitialState_ShouldReturnEasy()
        {
            // Act & Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void CurrentDifficulty_AfterUpdate_ShouldReflectChange()
        {
            // Arrange - 60% accuracy to trigger Medium difficulty
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Medium", _difficultyManager.CurrentDifficulty);
        }

        #endregion

        #region Accuracy Calculation Tests

        [Test]
        public void UpdateDifficulty_MixedAnswerTypes_ShouldCalculateAccuracyCorrectly()
        {
            // Arrange - 6 correct out of 10 = 60% accuracy (should trigger Medium)
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact6", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact7", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact8", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact9", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact10", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Medium", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_AllCorrect_ShouldUpgradeToHardest()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Hard", _difficultyManager.CurrentDifficulty);
        }

        [Test]
        public void UpdateDifficulty_AllIncorrect_ShouldStayOnEasiest()
        {
            // Arrange
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Incorrect, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Incorrect, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Easy", _difficultyManager.CurrentDifficulty);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateDifficulty_RepeatedCalls_ShouldMaintainCorrectDifficulty()
        {
            // Arrange
            var highAccuracyAnswers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact4", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact5", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            // Act - Call multiple times with same high accuracy
            _difficultyManager.UpdateDifficulty(highAccuracyAnswers);
            var difficultyAfterFirst = _difficultyManager.CurrentDifficulty;

            _difficultyManager.UpdateDifficulty(highAccuracyAnswers);
            var difficultyAfterSecond = _difficultyManager.CurrentDifficulty;

            // Assert
            Assert.AreEqual("Hard", difficultyAfterFirst);
            Assert.AreEqual("Hard", difficultyAfterSecond);
        }

        [Test]
        public void UpdateDifficulty_MinimumThresholdAnswers_ShouldUpdateCorrectly()
        {
            // Arrange - Exactly the minimum required answers
            var answers = new List<AnswerRecord>
            {
                new AnswerRecord("fact1", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact2", AnswerType.Correct, _assessmentStage.Id, "set1"),
                new AnswerRecord("fact3", AnswerType.Correct, _assessmentStage.Id, "set1")
            };

            // Act
            _difficultyManager.UpdateDifficulty(answers);

            // Assert
            Assert.AreEqual("Hard", _difficultyManager.CurrentDifficulty);
        }

        #endregion
    }
} 