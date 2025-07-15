using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using Cysharp.Threading.Tasks;
using AIEduChatbot.UnityReactBridge.Storage;
using FluencySDK.Services;
using FluencySDK.Algorithm;
using FluencySDK.Tests.Mocks;
using FluencySDK.Migrations;

namespace FluencySDK.Tests
{
    [TestFixture]
    public class FactSelectionServiceTests
    {
        private Mock<IGameStorageService> _mockStorageService;
        private MockTimeProvider _mockTimeProvider;
        private LearningAlgorithmConfig _config;
        private StorageManager _storageManager;
        private FactSelectionService _factSelectionService;

        [SetUp]
        public void SetUp()
        {
            MigrationsRegistry.InitializeStudentStatesMigrations();

            _mockStorageService = new Mock<IGameStorageService>();
            SetupMockStorageService();

            _config = LearningAlgorithmConfig.CreateSpeedRun();
            _config.DisableRandomization = true;

            _mockTimeProvider = new MockTimeProvider(DateTime.Now);
            _storageManager = new StorageManager(_config, _mockStorageService.Object);
            _factSelectionService = new FactSelectionService(_storageManager, _config);
        }

        [TearDown]
        public void TearDown()
        {
            _mockStorageService?.Reset();
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

        private async UniTask InitializeStorageManager()
        {
            await _storageManager.Initialize();
        }

        #region Fact Prioritization Tests

        [Test]
        public void SelectNextFact_ShouldPrioritizeOldestFacts()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            var now = _mockTimeProvider.Now;

            // Create facts with different LastAskedTime values
            var fact1 = _storageManager.StudentState.Facts[0];
            var fact2 = _storageManager.StudentState.Facts[1];
            var fact3 = _storageManager.StudentState.Facts[2];

            // Set up facts with known ages (oldest should be selected first)
            fact1.LastAskedTime = now.AddMinutes(-10); // Oldest
            fact2.LastAskedTime = now.AddMinutes(-5);
            fact3.LastAskedTime = now.AddMinutes(-1);  // Newest

            // Move time forward to clear cooldowns
            _mockTimeProvider.AdvanceTime(TimeSpan.FromSeconds(_config.MinQuestionIntervalSeconds + 1));

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(selectedFact);
            Assert.AreEqual(fact1.FactId, selectedFact.Id, "Should prioritize the oldest fact");
        }

        [Test]
        public void SelectNextFact_NullTimeShouldBePrioritized()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            difficultyConfig.MaxFactsBeingLearned = 100; // Allow high limit
            var now = _mockTimeProvider.Now;

            // Set LastAskedTime on ALL facts first
            foreach (var fact in _storageManager.StudentState.Facts)
            {
                fact.LastAskedTime = now.AddMinutes(-5);
            }

            // Reset LastAskedTime on one specific fact (make it completely unknown)
            var targetFact = _storageManager.StudentState.Facts.First(f => f.FactId == "0x0");
            targetFact.LastAskedTime = null;

            // Move time forward to clear cooldowns
            _mockTimeProvider.AdvanceTime(TimeSpan.FromSeconds(_config.MinQuestionIntervalSeconds + 1));

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(selectedFact);
            Assert.AreEqual("0x0", selectedFact.Id, "Should prioritize facts with null LastAskedTime");
        }

        [Test]
        public void SelectNextFact_OnlyKnownFactsAvailable_ShouldSelectKnownFacts()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];

            // Set up known facts (Review/Repetition stage)
            var knownFacts = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var fact = _storageManager.StudentState.Facts[i];
                fact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
                fact.LastAskedTime = _mockTimeProvider.Now.AddMinutes(-30);
                knownFacts.Add(fact.FactId);
            }

            // Move all other facts to mastered
            foreach (var fact in _storageManager.StudentState.Facts.Skip(3))
            {
                fact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Mastered).Id;
            }

            // Advance time to clear cooldowns
            _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(_config.ReviewDelaysMinutes[0] + 1));

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(selectedFact);
            Assert.IsTrue(knownFacts.Contains(selectedFact.Id), "Should select from known facts");
            Assert.IsTrue(stage.Type == LearningStageType.Review || stage.Type == LearningStageType.Repetition);
        }

        [Test]
        public void SelectNextFact_TooManyUnknownFacts_ShouldPreferKnownFacts()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];

            // Create at least one known fact
            var knownFact = _storageManager.StudentState.Facts[0];
            knownFact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
            knownFact.LastAskedTime = _mockTimeProvider.Now.AddMinutes(-30);

            // Advance time to make the Review fact ready
            _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(_config.ReviewDelaysMinutes[0] + 1));

            // Simulate many unknown facts asked recently to exceed KnownFactMaxRatio
            var recentQuestionCount = _config.RecentQuestionHistorySize;
            var unknownFactCount = Math.Max(1, (int)Math.Ceiling(recentQuestionCount * (difficultyConfig.KnownFactMaxRatio + 0.1f)));

            var assessmentStageId = _config.Stages.First(s => s.Type == LearningStageType.Assessment).Id;
            for (int i = 0; i < unknownFactCount; i++)
            {
                var unknownFactId = $"unknown_{i}";
                var answerRecord = new AnswerRecord(unknownFactId, AnswerType.Incorrect,
                    assessmentStageId, "test-set", _mockTimeProvider.Now.AddSeconds(-i))
                {
                    WasKnownFact = false
                };
                _storageManager.StudentState.AnswerHistory.Add(answerRecord);
            }

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(selectedFact);
            Assert.AreEqual(knownFact.FactId, selectedFact.Id, "Should prefer known fact over unknown when too many unknowns");
            Assert.IsTrue(stage.Type == LearningStageType.Review || stage.Type == LearningStageType.Repetition);
        }

        #endregion

        #region Fact Categorization Tests

        [Test]
        public void SelectNextFact_CompletelyUnknownSortedByFactSetId()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            difficultyConfig.MaxFactsBeingLearned = 100; // Allow high limit

            var selectedFactSetIds = new List<string>();

            // Act - Get next fact multiple times to see fact set ordering
            for (int i = 0; i < 5; i++)
            {
                var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);
                if (selectedFact != null)
                {
                    var factItem = _storageManager.StudentState.Facts.First(f => f.FactId == selectedFact.Id);
                    if (!selectedFactSetIds.Contains(factItem.FactSetId))
                    {
                        selectedFactSetIds.Add(factItem.FactSetId);
                    }
                    // Update the fact to simulate it being asked
                    _factSelectionService.UpdateLastAskedTime(factItem, _mockTimeProvider.Now);
                }
            }

            // Assert - Should follow fact set order from config
            var expectedOrder = _config.FactSetOrder.Take(selectedFactSetIds.Count).ToList();
            CollectionAssert.AreEqual(expectedOrder, selectedFactSetIds, "Fact sets should be introduced in configured order");
        }

        #endregion

        #region Working Memory Constraint Tests

        [Test]
        public void SelectNextFact_ShouldLimitBeingLearnedFacts()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            var maxBeingLearned = difficultyConfig.MaxFactsBeingLearned;

            // Act - Select facts to build up being learned facts
            for (int i = 0; i < maxBeingLearned + 5; i++)
            {
                var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);
                if (selectedFact == null) break;

                var factItem = _storageManager.StudentState.Facts.First(f => f.FactId == selectedFact.Id);
                _factSelectionService.UpdateLastAskedTime(factItem, _mockTimeProvider.Now);

                // Check constraint after each selection
                var beingLearnedStageIds = _config.Stages.Where(s => !s.IsKnownFact).Select(s => s.Id).ToList();
                var beingLearnedCount = _storageManager.StudentState.Facts
                    .Count(f => f.LastAskedTime.HasValue && !(beingLearnedStageIds.Contains(f.StageId)));

                Assert.LessOrEqual(beingLearnedCount, maxBeingLearned,
                    $"Being learned facts ({beingLearnedCount}) should not exceed limit ({maxBeingLearned})");
            }
        }

        [Test]
        public void SelectNextFact_ShouldPromoteFromCompletelyUnknownWhenSlotsAvailable()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            var maxBeingLearned = difficultyConfig.MaxFactsBeingLearned;

            // Fill up some slots but not all
            for (int i = 0; i < maxBeingLearned - 1; i++)
            {
                var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);
                if (selectedFact != null)
                {
                    var factItem = _storageManager.StudentState.Facts.First(f => f.FactId == selectedFact.Id);
                    _factSelectionService.UpdateLastAskedTime(factItem, _mockTimeProvider.Now);
                }
            }

            var initialCompletelyUnknown = _storageManager.StudentState.Facts
                .Count(f => !f.LastAskedTime.HasValue);

            // Act - Get next fact (should promote from completely unknown)
            var (nextFact, nextStage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(nextFact, "Should get a fact when slots are available");

            // Update the fact to simulate it being asked
            if (nextFact != null)
            {
                var factItem = _storageManager.StudentState.Facts.First(f => f.FactId == nextFact.Id);
                _factSelectionService.UpdateLastAskedTime(factItem, _mockTimeProvider.Now);
            }

            var finalCompletelyUnknown = _storageManager.StudentState.Facts
                .Count(f => !f.LastAskedTime.HasValue);

            Assert.Less(finalCompletelyUnknown, initialCompletelyUnknown,
                "Should have moved fact from completely unknown to being learned");
        }

        #endregion

        #region Cooldown Integration Tests

        [Test]
        public void SelectNextFact_ShouldRespectGeneralCooldown()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            var fact = _storageManager.StudentState.Facts[0];

            // Set fact to be within cooldown period
            fact.LastAskedTime = _mockTimeProvider.Now.AddSeconds(-(_config.MinQuestionIntervalSeconds / 2));

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert - Should not select the fact on cooldown (should select a different one)
            Assert.IsTrue(selectedFact == null || selectedFact.Id != fact.FactId,
                "Should not select fact on general cooldown");
        }

        [Test]
        public void SelectNextFact_ShouldRespectReinforcementCooldown()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            var fact = _storageManager.StudentState.Facts[0];

            // Set up a Review fact that just had a review (should be on reinforcement cooldown)
            fact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
            fact.LastAskedTime = _mockTimeProvider.Now;

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert - Should not select the fact on reinforcement cooldown
            Assert.IsTrue(selectedFact == null || selectedFact.Id != fact.FactId,
                "Should not select fact on reinforcement cooldown");
        }

        [Test]
        public void SelectNextFact_AfterCooldownExpires_ShouldSelectFact()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];
            var fact = _storageManager.StudentState.Facts[0];

            // Set up a Review fact and advance time beyond cooldown
            fact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
            fact.LastAskedTime = _mockTimeProvider.Now.AddMinutes(-30); // Clear general cooldown

            // Move all other facts to mastered so only this one is available
            foreach (var otherFact in _storageManager.StudentState.Facts.Skip(1))
            {
                otherFact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Mastered).Id;
            }

            // Advance time beyond reinforcement cooldown
            _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(_config.ReviewDelaysMinutes[0] + 1));

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert - Should now select the fact after cooldown expires
            Assert.IsNotNull(selectedFact);
            Assert.AreEqual(fact.FactId, selectedFact.Id, "Should select fact after cooldown expires");
        }

        #endregion

        #region Known Fact Ratio Tests

        [Test]
        public void SelectNextFact_BelowMinRatio_ShouldPreferKnownFacts()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];

            // Set up known and unknown facts
            var knownFact = _storageManager.StudentState.Facts[0];
            knownFact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
            knownFact.LastAskedTime = _mockTimeProvider.Now.AddMinutes(-30);

            var unknownFact = _storageManager.StudentState.Facts[1];
            unknownFact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Assessment).Id;
            unknownFact.LastAskedTime = _mockTimeProvider.Now.AddMinutes(-10);

            // Advance time to clear all cooldowns
            _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(_config.ReviewDelaysMinutes[0] + 1));

            // Create answer history with ratio below minimum (all unknown facts)
            var recentQuestionCount = _config.RecentQuestionHistorySize;
            var assessmentStageId = _config.Stages.First(s => s.Type == LearningStageType.Assessment).Id;
            for (int i = 0; i < recentQuestionCount; i++)
            {
                var answerRecord = new AnswerRecord($"unknown_fact_{i}", AnswerType.Correct,
                    assessmentStageId, "test-set", _mockTimeProvider.Now.AddSeconds(-i))
                {
                    WasKnownFact = false
                };
                _storageManager.StudentState.AnswerHistory.Add(answerRecord);
            }

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(selectedFact);
            Assert.AreEqual(knownFact.FactId, selectedFact.Id, "Should prefer known fact when ratio is below minimum");
            Assert.AreEqual(LearningStageType.Review, stage.Type, "Should select from known facts when ratio is below minimum");
        }

        [Test]
        public void SelectNextFact_AboveMaxRatio_ShouldPreferUnknownFacts()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var difficultyConfig = _config.DynamicDifficulty.Difficulties[0];

            // Set up known and unknown facts
            var knownFact = _storageManager.StudentState.Facts[0];
            knownFact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
            knownFact.LastAskedTime = _mockTimeProvider.Now.AddMinutes(-30);

            var unknownFact = _storageManager.StudentState.Facts[1];
            unknownFact.StageId = _config.Stages.First(s => s.Type == LearningStageType.Assessment).Id;
            unknownFact.LastAskedTime = _mockTimeProvider.Now.AddSeconds(-(_config.MinQuestionIntervalSeconds + 1));

            // Advance time to clear all cooldowns
            _mockTimeProvider.AdvanceTime(TimeSpan.FromMinutes(_config.ReviewDelaysMinutes[0] + 1));

            // Create answer history with ratio above maximum (all known facts)
            var recentQuestionCount = _config.RecentQuestionHistorySize;
            var reviewStageId = _config.Stages.First(s => s.Type == LearningStageType.Review).Id;
            for (int i = 0; i < recentQuestionCount; i++)
            {
                var answerRecord = new AnswerRecord($"known_fact_{i}", AnswerType.Correct,
                    reviewStageId, "test-set", _mockTimeProvider.Now.AddSeconds(-i))
                {
                    WasKnownFact = true
                };
                _storageManager.StudentState.AnswerHistory.Add(answerRecord);
            }

            // Act
            var (selectedFact, stage) = _factSelectionService.SelectNextFact(difficultyConfig, _mockTimeProvider.Now);

            // Assert
            Assert.IsNotNull(selectedFact);
            // The algorithm should prefer unknown facts when ratio is above maximum
            // But let's verify it's selecting from the unknown pool (Assessment stage facts)
            Assert.AreEqual(LearningStageType.Assessment, stage.Type, "Should select from unknown facts when ratio is above maximum");
        }

        [Test]
        public void DifficultyConfig_ShouldHaveValidKnownFactRatios()
        {
            // Arrange & Act
            var difficulties = _config.DynamicDifficulty.Difficulties;

            // Assert
            Assert.IsTrue(difficulties.Count > 0, "Should have at least one difficulty configuration");

            foreach (var difficulty in difficulties)
            {
                Assert.IsTrue(difficulty.KnownFactMinRatio >= 0.0f && difficulty.KnownFactMinRatio <= 1.0f,
                    $"Difficulty {difficulty.Name} should have valid KnownFactMinRatio (0.0-1.0)");
                Assert.IsTrue(difficulty.KnownFactMaxRatio >= 0.0f && difficulty.KnownFactMaxRatio <= 1.0f,
                    $"Difficulty {difficulty.Name} should have valid KnownFactMaxRatio (0.0-1.0)");
                Assert.IsTrue(difficulty.KnownFactMinRatio <= difficulty.KnownFactMaxRatio,
                    $"Difficulty {difficulty.Name} should have MinRatio <= MaxRatio");
            }
        }

        #endregion

        #region Update Methods Tests

        [Test]
        public void UpdateLastAskedTime_ShouldUpdateTimeAndRandomFactor()
        {
            // Arrange
            InitializeStorageManager().GetAwaiter().GetResult();
            var fact = _storageManager.StudentState.Facts[0];
            var originalRandomFactor = fact.RandomFactor;
            var updateTime = _mockTimeProvider.Now;

            // Act
            _factSelectionService.UpdateLastAskedTime(fact, updateTime);

            // Assert
            Assert.AreEqual(updateTime, fact.LastAskedTime, "Should update LastAskedTime");
            Assert.AreNotEqual(originalRandomFactor, fact.RandomFactor, "Should generate new random factor");
        }

        #endregion
    }
}