using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using FluencySDK.Services;
using FluencySDK.Algorithm;
using FluencySDK.Tests.Mocks;
using AIEduChatbot.UnityReactBridge.Storage;
using Cysharp.Threading.Tasks;
using FluencySDK.Migrations;

namespace FluencySDK.Tests
{
    [TestFixture]
    public class PromotionEngineTests
    {
        private Mock<IGameStorageService> _mockStorageService;
        private MockTimeProvider _mockTimeProvider;
        private DifficultyManager _difficultyManager;
        private LearningAlgorithmConfig _config;
        private PromotionEngine _promotionEngine;
        private DifficultyConfig _difficultyConfig;
        private MockEventHandler _mockEventHandler;
        private StorageManager _storageManager;

        [SetUp]
        public void SetUp()
        {
            MigrationsRegistry.InitializeStudentStatesMigrations();

            _mockStorageService = new Mock<IGameStorageService>();
            SetupMockStorageService();

            _config = LearningAlgorithmConfig.CreateSpeedRun();
            _config.DisableRandomization = true;
            _mockTimeProvider = new MockTimeProvider(DateTime.Now);
            _mockEventHandler = new MockEventHandler();
            _difficultyManager = new DifficultyManager(_config.DynamicDifficulty);
            _difficultyConfig = _difficultyManager.GetCurrentDifficultyConfig();
            _storageManager = new StorageManager(_config, _mockStorageService.Object);

            _promotionEngine = new PromotionEngine(_config, _difficultyManager, _mockTimeProvider, _mockEventHandler, _storageManager);
        }

        private string GetStageId(LearningStageType stageType)
        {
            return _config.Stages.First(s => s.Type == stageType).Id;
        }

        [TearDown]
        public void TearDown()
        {
            _mockStorageService?.Reset();
        }

        #region Promotion Tests

        [Test]
        public void PromoteFacts_AssessmentToPracticeSlow_ShouldPromoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.IndividualPromotionThreshold;

            // Act - Answer correctly up to threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual(LearningStageType.Practice, _config.GetStageById(factItem.StageId).Type);
            Assert.AreEqual(0, factItem.ConsecutiveCorrect, "Consecutive correct should reset after promotion");
        }

        [Test]
        public void PromoteFacts_PracticeSlowToPracticeFast_ShouldPromoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", "practice-slow");
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.PracticePromotionThreshold;

            // Act - Answer correctly up to practice promotion threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual("practice-fast", factItem.StageId);
            Assert.AreEqual(0, factItem.ConsecutiveCorrect, "Consecutive correct should reset after promotion");
        }

        [Test]
        public void PromoteFacts_PracticeFastToReview_ShouldPromoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", "practice-fast");
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.PracticePromotionThreshold;

            // Act - Answer correctly up to practice promotion threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual(LearningStageType.Review, _config.GetStageById(factItem.StageId).Type);
            Assert.AreEqual(0, factItem.ConsecutiveCorrect, "Consecutive correct should reset after promotion");
        }

        [Test]
        public void PromoteFacts_ReviewToRepetition_ShouldPromoteAfterAllReviews()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Review));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act - Progress through all review stages until reaching repetition
            int maxIterations = 10; // Safety limit
            int iterations = 0;
            var initialStageId = factItem.StageId;
            
            while (iterations < maxIterations)
            {
                var currentStage = _config.GetStageById(factItem.StageId);
                if (currentStage.Type == LearningStageType.Repetition)
                {
                    break;
                }

                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

                // Advance time for next review cycle if still in review
                var newStage = _config.GetStageById(factItem.StageId);
                if (newStage.Type == LearningStageType.Review)
                {
                    _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(5)); // Advance enough time for next review
                }

                iterations++;
            }

            // Assert
            Assert.AreEqual(LearningStageType.Repetition, _config.GetStageById(factItem.StageId).Type);
            Assert.IsTrue(iterations < maxIterations, $"Should have reached repetition stage within {maxIterations} iterations");
            Assert.AreNotEqual(initialStageId, factItem.StageId, "Should have progressed from initial review stage");
            Assert.AreEqual(0, factItem.ConsecutiveCorrect, "Consecutive correct should reset after promotion");
        }

        [Test]
        public void PromoteFacts_RepetitionToMastered_ShouldPromoteAfterAllRepetitions()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Repetition));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act - Progress through all repetition stages until mastered
            int maxIterations = 10; // Safety limit
            int iterations = 0;
            
            while (iterations < maxIterations)
            {
                var currentStage = _config.GetStageById(factItem.StageId);
                if (currentStage.Type == LearningStageType.Mastered)
                {
                    break;
                }

                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

                // Advance time for next repetition cycle if still in repetition
                var newStage = _config.GetStageById(factItem.StageId);
                if (newStage.Type == LearningStageType.Repetition)
                {
                    _mockTimeProvider.AdvanceTime(TimeSpan.FromDays(2)); // Advance enough time for next repetition
                }

                iterations++;
            }

            // Assert
            Assert.AreEqual(LearningStageType.Mastered, _config.GetStageById(factItem.StageId).Type);
            Assert.IsTrue(iterations < maxIterations, $"Should have reached mastered stage within {maxIterations} iterations");
        }

        [Test]
        public void PromoteFacts_OneShortOfThreshold_ShouldNotPromote()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.IndividualPromotionThreshold;

            // Act - Answer correctly one short of threshold
            for (int i = 0; i < threshold - 1; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual(LearningStageType.Assessment, _config.GetStageById(factItem.StageId).Type, "Should not promote if threshold not reached");
            Assert.AreEqual(threshold - 1, factItem.ConsecutiveCorrect);
        }

        #endregion

        #region Demotion Tests

        [Test]
        public void PromoteFacts_AssessmentToGrounding_ShouldDemoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.DemotionThreshold;

            // Act - Answer incorrectly up to demotion threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            }

            // Assert
            Assert.AreEqual(LearningStageType.Grounding, _config.GetStageById(factItem.StageId).Type);
            Assert.AreEqual(0, factItem.ConsecutiveIncorrect, "Consecutive incorrect should reset after demotion");
        }

        [Test]
        public void PromoteFacts_PracticeFastToAssessment_ShouldDemoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", "practice-fast");
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.DemotionThreshold;

            // Act - Answer incorrectly up to demotion threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            }

            // Assert
            Assert.AreEqual(LearningStageType.Assessment, _config.GetStageById(factItem.StageId).Type);
            Assert.AreEqual(0, factItem.ConsecutiveIncorrect, "Consecutive incorrect should reset after demotion");
        }

        [Test]
        public void PromoteFacts_ReviewToPracticeFast_ShouldDemoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Review));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.DemotionThreshold;

            // Act - Answer incorrectly up to demotion threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            }

            // Assert
            Assert.AreEqual("practice-fast", factItem.StageId);
            Assert.AreEqual(0, factItem.ConsecutiveIncorrect, "Consecutive incorrect should reset after demotion");
        }

        [Test]
        public void PromoteFacts_RepetitionToPracticeFast_ShouldDemoteAfterThreshold()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Repetition));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.DemotionThreshold;

            // Act - Answer incorrectly up to demotion threshold
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            }

            // Assert - Should demote to previous stage in sequence (review-4min)
            Assert.AreEqual("review-4min", factItem.StageId);
            Assert.AreEqual(0, factItem.ConsecutiveIncorrect, "Consecutive incorrect should reset after demotion");
        }

        [Test]
        public void PromoteFacts_OneShortOfDemotionThreshold_ShouldNotDemote()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", "practice-fast");
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.DemotionThreshold;

            // Act - Answer incorrectly one short of demotion threshold
            for (int i = 0; i < threshold - 1; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            }

            // Assert
            Assert.AreEqual("practice-fast", factItem.StageId, "Should not demote if threshold not reached");
            Assert.AreEqual(threshold - 1, factItem.ConsecutiveIncorrect);
        }

        #endregion

        #region Mixed Answer Pattern Tests

        [Test]
        public void PromoteFacts_CorrectThenIncorrect_ShouldResetStreaks()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.IndividualPromotionThreshold = 5;
            config.DemotionThreshold = 5;
            config.DisableRandomization = true;

            var difficultyConfig = config.DynamicDifficulty.Difficulties[0];
            difficultyConfig.PromotionThresholds["assessment"] = 5;
            difficultyConfig.PromotionThresholds["practice-slow"] = 5;
            difficultyConfig.PromotionThresholds["practice-fast"] = 5;
            difficultyConfig.DemotionThresholds["assessment"] = 5;
            difficultyConfig.DemotionThresholds["practice-slow"] = 5;
            difficultyConfig.DemotionThresholds["practice-fast"] = 5;

            var difficultyManager = new DifficultyManager(config.DynamicDifficulty);
            var promotionEngine = new PromotionEngine(config, difficultyManager, _mockTimeProvider, _mockEventHandler, _storageManager);
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act - Build up correct streak, then break it
            promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

            var correctStreakBefore = factItem.ConsecutiveCorrect;
            promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);

            // Assert
            Assert.AreEqual(0, factItem.ConsecutiveCorrect, "Correct streak should reset after incorrect answer");
            Assert.AreEqual(1, factItem.ConsecutiveIncorrect, "Incorrect streak should start");
            Assert.IsTrue(correctStreakBefore > 0, "Should have had a correct streak before");
        }

        [Test]
        public void PromoteFacts_IncorrectThenCorrect_ShouldResetStreaks()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.IndividualPromotionThreshold = 5;
            config.DemotionThreshold = 5;
            config.DisableRandomization = true;

            var difficultyConfig = config.DynamicDifficulty.Difficulties[0];
            difficultyConfig.PromotionThresholds["assessment"] = 5;
            difficultyConfig.PromotionThresholds["practice-slow"] = 5;
            difficultyConfig.PromotionThresholds["practice-fast"] = 5;
            difficultyConfig.DemotionThresholds["assessment"] = 5;
            difficultyConfig.DemotionThresholds["practice-slow"] = 5;
            difficultyConfig.DemotionThresholds["practice-fast"] = 5;

            var difficultyManager = new DifficultyManager(config.DynamicDifficulty);
            var promotionEngine = new PromotionEngine(config, difficultyManager, _mockTimeProvider, _mockEventHandler, _storageManager);
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act - Build up incorrect streak, then break it
            promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);

            var incorrectStreakBefore = factItem.ConsecutiveIncorrect;
            promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

            // Assert
            Assert.AreEqual(0, factItem.ConsecutiveIncorrect, "Incorrect streak should reset after correct answer");
            Assert.AreEqual(1, factItem.ConsecutiveCorrect, "Correct streak should start");
            Assert.IsTrue(incorrectStreakBefore > 0, "Should have had an incorrect streak before");
        }

        [Test]
        public void PromoteFacts_AlternatingPattern_ShouldNeverPromoteOrDemote()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.IndividualPromotionThreshold = 3;
            config.DemotionThreshold = 3;
            config.DisableRandomization = true;

            var difficultyConfig = config.DynamicDifficulty.Difficulties[0];
            difficultyConfig.PromotionThresholds["assessment"] = 3;
            difficultyConfig.PromotionThresholds["practice-slow"] = 3;
            difficultyConfig.PromotionThresholds["practice-fast"] = 3;
            difficultyConfig.DemotionThresholds["assessment"] = 3;
            difficultyConfig.DemotionThresholds["practice-slow"] = 3;
            difficultyConfig.DemotionThresholds["practice-fast"] = 3;

            var difficultyManager = new DifficultyManager(config.DynamicDifficulty);
            var promotionEngine = new PromotionEngine(config, difficultyManager, _mockTimeProvider, _mockEventHandler, _storageManager);
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var initialStageId = factItem.StageId;

            // Act - Alternate correct and incorrect answers
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
                }
                else
                {
                    promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
                }
            }

            // Assert
            Assert.AreEqual(initialStageId, factItem.StageId, "Stage should not change with alternating pattern");
            Assert.IsTrue(factItem.ConsecutiveCorrect <= 1, "Consecutive correct should never build up");
            Assert.IsTrue(factItem.ConsecutiveIncorrect <= 1, "Consecutive incorrect should never build up");
        }

        #endregion

        #region Reinforcement Progression Tests

        [Test]
        public void PromoteFacts_ReviewProgressTracking_ShouldUpdateCorrectly()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Review));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act & Assert - Test progression through review stages
            int maxIterations = 10; // Safety limit
            int iterations = 0;
            var initialStageId = factItem.StageId;
            
            while (iterations < maxIterations)
            {
                var currentStage = _config.GetStageById(factItem.StageId);
                if (currentStage.Type == LearningStageType.Repetition)
                {
                    break;
                }

                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

                // Advance time for next cycle if still in review
                var newStage = _config.GetStageById(factItem.StageId);
                if (newStage.Type == LearningStageType.Review)
                {
                    _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(5)); // Advance enough time for next review
                }

                iterations++;
            }

            // Final check - should promote to Repetition after all review stages
            Assert.AreEqual(LearningStageType.Repetition, _config.GetStageById(factItem.StageId).Type);
            Assert.IsTrue(iterations < maxIterations, $"Should have reached repetition stage within {maxIterations} iterations");
            Assert.AreNotEqual(initialStageId, factItem.StageId, "Should have progressed from initial review stage");
        }

        [Test]
        public void PromoteFacts_RepetitionProgressTracking_ShouldUpdateCorrectly()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Repetition));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act & Assert - Test progression through repetition stages
            int maxIterations = 10; // Safety limit
            int iterations = 0;
            var initialStageId = factItem.StageId;
            
            while (iterations < maxIterations)
            {
                var currentStage = _config.GetStageById(factItem.StageId);
                if (currentStage.Type == LearningStageType.Mastered)
                {
                    break;
                }

                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

                // Advance time for next cycle if still in repetition
                var newStage = _config.GetStageById(factItem.StageId);
                if (newStage.Type == LearningStageType.Repetition)
                {
                    _mockTimeProvider.AdvanceTime(TimeSpan.FromDays(2)); // Advance enough time for next repetition
                }

                iterations++;
            }

            // Final check - should promote to Mastered after all repetition stages
            Assert.AreEqual(LearningStageType.Mastered, _config.GetStageById(factItem.StageId).Type);
            Assert.IsTrue(iterations < maxIterations, $"Should have reached mastered stage within {maxIterations} iterations");
            Assert.AreNotEqual(initialStageId, factItem.StageId, "Should have progressed from initial repetition stage");
        }

        #endregion

        #region Stage Skipping Integration Tests

        [Test]
        public void PromoteFacts_ShouldSkipWhenPromotionThresholdIsZero()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.DynamicDifficulty.Difficulties[0].PromotionThresholds["practice-slow"] = 0; // Skip this stage

            var difficultyManager = new DifficultyManager(config.DynamicDifficulty);
            var promotionEngine = new PromotionEngine(config, difficultyManager, _mockTimeProvider, _mockEventHandler, _storageManager);

            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);

            // Act - Answer correctly to trigger promotion from Assessment
            promotionEngine.PromoteFacts(factItem, AnswerType.Correct);

            // Assert - Should skip PracticeSlow and go to PracticeFast
            Assert.AreNotEqual("practice-slow", factItem.StageId, "Should skip PracticeSlow when threshold is 0");
            Assert.AreEqual("practice-fast", factItem.StageId, "Should go directly to PracticeFast");
        }

        [Test]
        public void PromoteFacts_ShouldSkipWhenDemotionThresholdIsZero()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.DynamicDifficulty.Difficulties[0].DemotionThresholds["assessment"] = 0; // Skip this stage
            config.DynamicDifficulty.Difficulties[0].DemotionThresholds["practice-fast"] = 1;

            var difficultyManager = new DifficultyManager(config.DynamicDifficulty);
            var promotionEngine = new PromotionEngine(config, difficultyManager, _mockTimeProvider, _mockEventHandler, _storageManager);

            var factItem = CreateFactItem("test-fact", "practice-fast");
            _storageManager.StudentState.Facts.Add(factItem);

            // Act - Answer incorrectly to trigger demotion
            promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);

            // Assert - Should skip Assessment and go to Grounding
            Assert.AreNotEqual("assessment", factItem.StageId, "Should skip Assessment when demotion threshold is 0");
            Assert.AreEqual("grounding", factItem.StageId, "Should go directly to Grounding");
        }

        #endregion

        #region Event Integration Tests

        [Test]
        public void PromoteFacts_PromotionShouldFireEvent()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.IndividualPromotionThreshold;
            _mockEventHandler.Clear();

            // Act - Answer correctly to trigger promotion
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual(1, _mockEventHandler.ReceivedEvents.Count, "Should have received exactly one event");
            var eventInfo = _mockEventHandler.ReceivedEvents[0];

            Assert.AreEqual("test-fact", eventInfo.FactId);
            Assert.AreEqual("test-set", eventInfo.FactSetId);
            Assert.AreEqual("assessment", eventInfo.FromStageId);
            Assert.AreEqual("practice-slow", eventInfo.ToStageId);
            Assert.AreEqual(AnswerType.Correct, eventInfo.AnswerType);
            Assert.IsTrue(eventInfo.ConsecutiveCount >= 0, "Should have consecutive count");
        }

        [Test]
        public void PromoteFacts_DemotionShouldFireEvent()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.DemotionThreshold;
            _mockEventHandler.Clear();

            // Act - Answer incorrectly to trigger demotion
            for (int i = 0; i < threshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Incorrect);
            }

            // Assert
            Assert.AreEqual(1, _mockEventHandler.ReceivedEvents.Count, "Should have received exactly one event");
            var eventInfo = _mockEventHandler.ReceivedEvents[0];

            Assert.AreEqual("test-fact", eventInfo.FactId);
            Assert.AreEqual("test-set", eventInfo.FactSetId);
            Assert.AreEqual("assessment", eventInfo.FromStageId);
            Assert.AreEqual("grounding", eventInfo.ToStageId);
            Assert.AreEqual(AnswerType.Incorrect, eventInfo.AnswerType);
            Assert.IsTrue(eventInfo.ConsecutiveCount >= 0, "Should have consecutive count");
        }

        [Test]
        public void PromoteFacts_NoProgressionShouldNotFireEvent()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            var threshold = _config.IndividualPromotionThreshold;
            _mockEventHandler.Clear();

            // Act - Answer correctly but not enough to trigger promotion
            for (int i = 0; i < threshold - 1; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual(0, _mockEventHandler.ReceivedEvents.Count, "Should not have received any events");
        }

        [Test]
        public void PromoteFacts_MultiplePromotionsShouldFireMultipleEvents()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            var factItem = CreateFactItem("test-fact", GetStageId(LearningStageType.Assessment));
            _storageManager.StudentState.Facts.Add(factItem);
            _mockEventHandler.Clear();

            // Act - Promote from Assessment to PracticeSlow
            for (int i = 0; i < _config.IndividualPromotionThreshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Act - Promote from PracticeSlow to PracticeFast
            for (int i = 0; i < _config.PracticePromotionThreshold; i++)
            {
                _promotionEngine.PromoteFacts(factItem, AnswerType.Correct);
            }

            // Assert
            Assert.AreEqual(2, _mockEventHandler.ReceivedEvents.Count, "Should have received two events");

            // Check first event (Assessment -> PracticeSlow)
            var firstEvent = _mockEventHandler.ReceivedEvents[0];
            Assert.AreEqual("assessment", firstEvent.FromStageId);
            Assert.AreEqual("practice-slow", firstEvent.ToStageId);

            // Check second event (PracticeSlow -> PracticeFast)
            var secondEvent = _mockEventHandler.ReceivedEvents[1];
            Assert.AreEqual("practice-slow", secondEvent.FromStageId);
            Assert.AreEqual("practice-fast", secondEvent.ToStageId);
        }

        #endregion

        #region Bulk Promotion Tests

        [Test]
        public void PromoteFacts_BulkPromotionDisabled_ShouldNotApplyBulkPromotion()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.Enabled = false;
            
            var fact1 = CreateFactItem("fact1", "practice-slow");
            fact1.LastAskedTime = _mockTimeProvider.Now;
            var fact2 = CreateFactItem("fact2", "practice-slow");
            fact2.LastAskedTime = _mockTimeProvider.Now;
            
            _storageManager.StudentState.Facts.Add(fact1);
            _storageManager.StudentState.Facts.Add(fact2);
            
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-2));
            _storageManager.StudentState.AddAnswerRecord("fact2", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-1));

            // Act
            _promotionEngine.PromoteFacts(fact1, AnswerType.Correct);

            // Assert - Should fall back to individual promotion
            Assert.AreEqual("practice-fast", fact1.StageId);
            Assert.AreEqual("practice-slow", fact2.StageId);
        }

        [Test]
        public void PromoteFacts_BulkPromotionEnabled_ShouldApplyBulkPromotion()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.Enabled = true;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinConsecutiveCorrect = 2;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinFactSetCoveragePercent = 0.8f;
            
            var fact1 = CreateFactItem("fact1", "practice-slow");
            fact1.LastAskedTime = _mockTimeProvider.Now;
            var fact2 = CreateFactItem("fact2", "practice-slow");
            fact2.LastAskedTime = _mockTimeProvider.Now;
            
            _storageManager.StudentState.Facts.Add(fact1);
            _storageManager.StudentState.Facts.Add(fact2);
            
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-2));
            _storageManager.StudentState.AddAnswerRecord("fact2", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-1));

            // Act
            _promotionEngine.PromoteFacts(fact1, AnswerType.Correct);

            // Assert - Both facts should be bulk promoted
            Assert.AreEqual("practice-fast", fact1.StageId);
            Assert.AreEqual("practice-fast", fact2.StageId);
        }

        [Test]
        public void PromoteFacts_BulkPromotionInsufficientConsecutiveCorrect_ShouldNotApplyBulkPromotion()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.Enabled = true;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinConsecutiveCorrect = 3;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinFactSetCoveragePercent = 0.8f;
            
            var fact1 = CreateFactItem("fact1", "practice-slow");
            fact1.LastAskedTime = _mockTimeProvider.Now;
            var fact2 = CreateFactItem("fact2", "practice-slow");
            fact2.LastAskedTime = _mockTimeProvider.Now;
            
            _storageManager.StudentState.Facts.Add(fact1);
            _storageManager.StudentState.Facts.Add(fact2);
            
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-2));
            _storageManager.StudentState.AddAnswerRecord("fact2", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-1));

            // Act
            _promotionEngine.PromoteFacts(fact1, AnswerType.Correct);

            // Assert - Should fall back to individual promotion
            Assert.AreEqual("practice-fast", fact1.StageId);
            Assert.AreEqual("practice-slow", fact2.StageId);
        }

        [Test]
        public void PromoteFacts_BulkPromotionInsufficientCoverage_ShouldNotApplyBulkPromotion()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.Enabled = true;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinConsecutiveCorrect = 2;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinFactSetCoveragePercent = 0.9f;
            
            var fact1 = CreateFactItem("fact1", "practice-slow");
            fact1.LastAskedTime = _mockTimeProvider.Now;
            var fact2 = CreateFactItem("fact2", "practice-slow");
            fact2.LastAskedTime = _mockTimeProvider.Now;
            var fact3 = CreateFactItem("fact3", "practice-slow");
            fact3.LastAskedTime = _mockTimeProvider.Now;
            
            _storageManager.StudentState.Facts.Add(fact1);
            _storageManager.StudentState.Facts.Add(fact2);
            _storageManager.StudentState.Facts.Add(fact3);
            
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-2));
            _storageManager.StudentState.AddAnswerRecord("fact2", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-1));

            // Act
            _promotionEngine.PromoteFacts(fact1, AnswerType.Correct);

            // Assert - Should fall back to individual promotion (coverage only 66%, need 90%)
            Assert.AreEqual("practice-fast", fact1.StageId);
            Assert.AreEqual("practice-slow", fact2.StageId);
            Assert.AreEqual("practice-slow", fact3.StageId);
        }

        [Test]
        public void PromoteFacts_BulkPromotionOnlyPromotesSameStage_ShouldNotPromoteDifferentStages()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.Enabled = true;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinConsecutiveCorrect = 2;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinFactSetCoveragePercent = 0.8f;
            
            var fact1 = CreateFactItem("fact1", "practice-slow");
            fact1.LastAskedTime = _mockTimeProvider.Now;
            var fact2 = CreateFactItem("fact2", "practice-fast");
            fact2.LastAskedTime = _mockTimeProvider.Now;
            
            _storageManager.StudentState.Facts.Add(fact1);
            _storageManager.StudentState.Facts.Add(fact2);
            
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-2));
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-1));

            // Act
            _promotionEngine.PromoteFacts(fact1, AnswerType.Correct);

            // Assert - Only fact1 should be promoted (same stage as trigger fact)
            Assert.AreEqual("practice-fast", fact1.StageId);
            Assert.AreEqual("practice-fast", fact2.StageId);
        }

        [Test]
        public void PromoteFacts_BulkPromotionShouldFireBulkEvent()
        {
            // Arrange 
            InitializeStorageManager().GetAwaiter().GetResult();
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.Enabled = true;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinConsecutiveCorrect = 2;
            _config.DynamicDifficulty.Difficulties[0].BulkPromotion.MinFactSetCoveragePercent = 0.8f;
            
            var fact1 = CreateFactItem("fact1", "practice-slow");
            fact1.LastAskedTime = _mockTimeProvider.Now;
            var fact2 = CreateFactItem("fact2", "practice-slow");
            fact2.LastAskedTime = _mockTimeProvider.Now;
            
            _storageManager.StudentState.Facts.Add(fact1);
            _storageManager.StudentState.Facts.Add(fact2);
            
            _storageManager.StudentState.AddAnswerRecord("fact1", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-2));
            _storageManager.StudentState.AddAnswerRecord("fact2", AnswerType.Correct, "practice-slow", "test-set", _mockTimeProvider.Now.AddSeconds(-1));

            _mockEventHandler.Clear();

            // Act
            _promotionEngine.PromoteFacts(fact1, AnswerType.Correct);

            // Assert
            Assert.AreEqual(1, _mockEventHandler.ReceivedBulkPromotions.Count);
            Assert.AreEqual(2, _mockEventHandler.ReceivedEvents.Count);
            
            var bulkEvent = _mockEventHandler.ReceivedBulkPromotions[0];
            Assert.AreEqual("test-set", bulkEvent.FactSetId);
            Assert.AreEqual(2, bulkEvent.PromotedFactsCount);
            Assert.AreEqual(2, bulkEvent.ConsecutiveCorrectCount);
            Assert.AreEqual(1.0f, bulkEvent.CoveragePercentage);
        }

        #endregion

        #region Helper Methods

        private FactItem CreateFactItem(string factId, string stageId, string factSetId = "test-set")
        {
            return new FactItem(factId, factSetId, stageId);
        }

        private async UniTask InitializeStorageManager()
        {
            await _storageManager.Initialize();
        }

        private void SetupMockStorageService()
        {
            _mockStorageService.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                .Returns(UniTask.FromResult(false));

            _mockStorageService.Setup(x => x.LoadAsync<StudentState>(It.IsAny<string>()))
                .Returns(UniTask.FromResult<StudentState>(null));

            _mockStorageService.Setup(x => x.SetAsync<StudentState>(It.IsAny<string>(), It.IsAny<StudentState>()))
                .Returns(UniTask.CompletedTask);
        }

        #endregion
    }
}