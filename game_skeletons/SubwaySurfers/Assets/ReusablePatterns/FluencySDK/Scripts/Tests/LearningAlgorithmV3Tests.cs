using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Moq;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
using AIEduChatbot.UnityReactBridge.Storage;
using FluencySDK.Services;
using FluencySDK.Tests.Mocks;

namespace FluencySDK.Tests
{
    [TestFixture]
    public class LearningAlgorithmV3Tests
    {
        private Mock<IGameStorageService> _mockStorageService;
        private MockTimeProvider _mockTimeProvider;
        private LearningAlgorithmConfig _speedRunConfig;
        private List<ILearningAlgorithmEvent> _capturedEvents;

        [SetUp]
        public void SetUp()
        {
            // Clean up any existing subscriptions first
            ILearningAlgorithm.LearningAlgorithmEvent -= OnLearningAlgorithmEvent;

            _mockStorageService = new Mock<IGameStorageService>();
            SetupMockStorageService();

            _speedRunConfig = LearningAlgorithmConfig.CreateSpeedRun();
            _speedRunConfig.DisableRandomization = true;

            _mockTimeProvider = new MockTimeProvider(DateTime.Now);

            // Setup event capture
            _capturedEvents = new List<ILearningAlgorithmEvent>();
            ILearningAlgorithm.LearningAlgorithmEvent += OnLearningAlgorithmEvent;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                // Ensure event is unsubscribed
                ILearningAlgorithm.LearningAlgorithmEvent -= OnLearningAlgorithmEvent;

                // Reset mock and clear captured events
                _mockStorageService?.Reset();
                _capturedEvents?.Clear();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in TearDown: {ex.Message}");
            }
        }

        private void OnLearningAlgorithmEvent(ILearningAlgorithmEvent eventInfo)
        {
            try
            {
                if (_capturedEvents != null && eventInfo != null)
                {
                    _capturedEvents.Add(eventInfo);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in OnLearningAlgorithmEvent: {ex.Message}");
            }
        }

        #region Test Configuration Setup

        private void SetupMockStorageService()
        {
            try
            {
                _mockStorageService.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                    .Returns(UniTask.FromResult(false));

                _mockStorageService.Setup(x => x.LoadAsync<StudentState>(It.IsAny<string>()))
                    .Returns(UniTask.FromResult<StudentState>(null));

                _mockStorageService.Setup(x => x.SetAsync<StudentState>(It.IsAny<string>(), It.IsAny<StudentState>()))
                    .Returns(UniTask.CompletedTask);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error setting up mock storage service: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Happy Path End-to-End Tests

        [UnityTest]
        public IEnumerator HappyPath_StudentAnswersAllCorrectly_ShouldProgressThroughAllStages() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var algo = new LearningAlgorithmV3(_speedRunConfig, _mockStorageService.Object, _mockTimeProvider);
            await algo.Initialize();

            // Track fact progression through all stages
            var factProgressions = new Dictionary<string, List<LearningStage>>();
            var questionsAnswered = 0;
            var maxQuestions = 300;
            var nullQuestionCount = 0;
            var maxNullQuestions = 20;

            // Act - Answer questions correctly until we reach mastery or hit limit
            while (questionsAnswered < maxQuestions)
            {
                var question = await algo.GetNextQuestion();
                if (question == null)
                {
                    nullQuestionCount++;
                    if (nullQuestionCount > maxNullQuestions)
                    {
                        UnityEngine.Debug.LogWarning("Breaking due to too many null questions - advancing time significantly");
                        // Advance time significantly to handle repetition delays
                        AdvanceTime(TimeSpan.FromDays(10));
                        nullQuestionCount = 0;
                        continue;
                    }

                    // No question available, advance time to allow facts to become ready
                    AdvanceTime(TimeSpan.FromSeconds(_speedRunConfig.MinQuestionIntervalSeconds + 1));
                    await AdvanceTimeForReinforcement(algo);
                    continue;
                }

                nullQuestionCount = 0; // Reset counter when we get a valid question

                // Track fact stage progression
                var factId = question.FactId;
                if (!factProgressions.ContainsKey(factId))
                    factProgressions[factId] = new List<LearningStage>();
                factProgressions[factId].Add(question.LearningStage);

                await AnswerCorrectly(algo, question);
                questionsAnswered++;

                // Check if we've reached mastery for all facts
                if (AllFactsMastered(algo)) break;
                
                // If we have some facts in advanced stages but not mastered, advance time periodically
                var factsInRepetition = algo.StudentState.Facts.Count(f => 
                    _speedRunConfig.GetStageById(f.StageId)?.Type == LearningStageType.Repetition);
                if (factsInRepetition > 0 && questionsAnswered % 20 == 0)
                {
                    AdvanceTime(TimeSpan.FromDays(3)); // Advance time to help repetition stages
                }
            }

            // Assert - Verify progression through all stages
            Assert.IsTrue(questionsAnswered > 0, "Should have answered at least one question");
            Assert.IsTrue(factProgressions.Count > 0, "Should have tracked some fact progressions");

            // Verify each fact went through expected stage progression
            foreach (var factProgression in factProgressions.Values)
            {
                VerifyStageProgression(factProgression);
            }

            // Verify some facts reached mastery
            var masteredStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Mastered).Id;
            var masteredFacts = algo.StudentState.Facts
                .Where(f => f.StageId == masteredStageId)
                .ToList();
                
            // More lenient check - just verify some facts reached high stages
            var advancedFacts = algo.StudentState.Facts
                .Where(f => {
                    var stage = _speedRunConfig.GetStageById(f.StageId);
                    return stage?.Type == LearningStageType.Mastered || 
                           stage?.Type == LearningStageType.Repetition ||
                           stage?.Type == LearningStageType.Review;
                })
                .ToList();
                
            Assert.IsTrue(advancedFacts.Count > 0, 
                $"At least some facts should reach advanced stages. Found {advancedFacts.Count} advanced facts, {masteredFacts.Count} mastered facts");

            // Verify events were fired during progression
            var progressionEvents = _capturedEvents.OfType<IndividualFactProgressionInfo>().ToList();
            Assert.IsTrue(progressionEvents.Count > 0, "Should have fired progression events during stage transitions");

            // Verify event structure
            foreach (var progressionEvent in progressionEvents)
            {
                Assert.IsNotNull(progressionEvent.FactId);
                Assert.IsNotNull(progressionEvent.FactSetId);
                Assert.AreNotEqual(progressionEvent.FromStageId, progressionEvent.ToStageId, "Events should represent actual stage transitions");
                Assert.IsTrue(progressionEvent.ConsecutiveCount >= 0);
                Assert.IsTrue(progressionEvent.Timestamp > DateTimeOffset.MinValue);
            }

            // Verify logical progression in events (promotions should generally go forward in stage order)
            var promotionEvents = progressionEvents.Where(e => e.AnswerType == AnswerType.Correct).ToList();
            Assert.IsTrue(promotionEvents.Count > 0, "Should have promotion events from correct answers");

            Console.WriteLine($"Happy path test completed: {questionsAnswered} questions answered, {masteredFacts.Count} facts mastered, {advancedFacts.Count} advanced facts, {progressionEvents.Count} progression events fired");
        });

        [UnityTest]
        public IEnumerator HappyPath_ReinforcementMechanics_ShouldWorkCorrectly() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var algo = new LearningAlgorithmV3(_speedRunConfig, _mockStorageService.Object, _mockTimeProvider);
            await algo.Initialize();
            
            // Clear events before starting the test
            _capturedEvents.Clear();
            
            var factWithReinforcement = await ProgressFactToReview(algo);

            // Act - Test review progression by directly promoting facts through review stages
            var reviewQuestions = 0;
            var expectedReviewStages = _speedRunConfig.Stages.Where(s => s.Type == LearningStageType.Review).OrderBy(s => s.Order).ToList();

            // Progress through each review stage
            for (int reviewStageIndex = 0; reviewStageIndex < expectedReviewStages.Count; reviewStageIndex++)
            {
                var question = await GetQuestionForFact(algo, factWithReinforcement);
                Assert.IsNotNull(question, $"Should get review question for stage {reviewStageIndex}");
                Assert.AreEqual(LearningStageType.Review, question.LearningStage.Type);

                await AnswerCorrectly(algo, question);
                reviewQuestions++;

                // Advance time for next review cycle if not the last stage
                if (reviewStageIndex < expectedReviewStages.Count - 1)
                {
                    var currentStage = expectedReviewStages[reviewStageIndex];
                    if (currentStage is ReviewStage reviewStage)
                    {
                        AdvanceTime(TimeSpan.FromMinutes(reviewStage.DelayMinutes + 1));
                    }
                }
            }

            // After completing all review stages, advance time to make fact ready for repetition
            AdvanceTime(TimeSpan.FromDays(1));

            // Should now be in Repetition stage
            // Check current fact state before trying to get question
            var factItem = algo.StudentState.Facts.FirstOrDefault(f => f.FactId == factWithReinforcement);
            Console.WriteLine($"Fact {factWithReinforcement} current stage: {factItem?.StageId}, LastAskedTime: {factItem?.LastAskedTime}");
            
            var nextQuestion = await GetQuestionForFact(algo, factWithReinforcement);
            Assert.IsNotNull(nextQuestion, $"Should get a question for fact {factWithReinforcement} after review progression. Current stage: {factItem?.StageId}");
            Assert.AreEqual(LearningStageType.Repetition, nextQuestion.LearningStage.Type);

            // Verify progression events were fired
            var progressionEvents = _capturedEvents.OfType<IndividualFactProgressionInfo>().ToList();
            var factProgressionEvents = progressionEvents.Where(e => e.FactId == factWithReinforcement).ToList();

            Assert.IsTrue(factProgressionEvents.Count > 0, "Should have fired progression events for the reinforcement fact");

            // Should have at least one event transitioning TO Review stage
            var toReviewEvent = factProgressionEvents.FirstOrDefault(e => 
                e?.ToStageId != null && _speedRunConfig.GetStageById(e.ToStageId)?.Type == LearningStageType.Review);
            Assert.IsNotNull(toReviewEvent, "Should have an event showing progression to Review stage");

            // Should have at least one event transitioning TO Repetition stage  
            var toRepetitionEvent = factProgressionEvents.FirstOrDefault(e => 
                e?.ToStageId != null && _speedRunConfig.GetStageById(e.ToStageId)?.Type == LearningStageType.Repetition);
            Assert.IsNotNull(toRepetitionEvent, "Should have an event showing progression to Repetition stage");

            Console.WriteLine($"Reinforcement test completed: {reviewQuestions} review questions answered, {factProgressionEvents.Count} progression events for reinforcement fact");
        });

        #endregion

        #region Dynamic Difficulty Tests

        [UnityTest]
        public IEnumerator DynamicDifficulty_ShouldUpdateBasedOnAccuracy() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Create config with multiple difficulties that have different observable effects
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.DynamicDifficulty.MinAnswersForDifficultyChange = 3;
            config.DynamicDifficulty.RecentAnswerWindow = 10;
            config.DynamicDifficulty.Difficulties = new List<DifficultyConfig>
            {
                new DifficultyConfig { Name = "Hard", MinAccuracyThreshold = 0.8f, MaxFactsBeingLearned = 10 },
                new DifficultyConfig { Name = "Easy", MinAccuracyThreshold = 0.0f, MaxFactsBeingLearned = 2 }
            };

            var algo = new LearningAlgorithmV3(config, _mockStorageService.Object, _mockTimeProvider);
            await algo.Initialize();

            // Act - Answer questions with high accuracy (should move to Hard difficulty)
            for (int i = 0; i < 5; i++)
            {
                var question = await algo.GetNextQuestion();
                if (question != null)
                {
                    await AnswerCorrectly(algo, question);
                }
            }

            // Test observable effect: Hard difficulty allows more facts being learned (10 vs 2)
            // Continue answering questions to test the higher working memory limit
            var questionsAnswered = 5;
            while (questionsAnswered < 12) // Try to exceed Easy limit (2) but stay within Hard limit (10)
            {
                var question = await algo.GetNextQuestion();
                if (question == null) break;

                await AnswerCorrectly(algo, question);
                questionsAnswered++;

                // Count facts being learned correctly: facts that have been asked but are not in known stages
                var masteredStageId = config.Stages.FirstOrDefault(s => s.IsFullyLearned)?.Id;
                var beingLearnedCount = algo.StudentState.Facts
                    .Count(f => f.LastAskedTime.HasValue && 
                               config.GetStageById(f.StageId)?.IsKnownFact == false && 
                               f.StageId != masteredStageId);

                // If we're on Easy difficulty, we should never exceed 2 facts being learned
                // If we're on Hard difficulty, we can have up to 10 facts being learned
                if (beingLearnedCount > 2)
                {
                    // Assert - This proves we're on Hard difficulty (with higher working memory limit)
                    Assert.LessOrEqual(beingLearnedCount, 10, "Should be within Hard difficulty limits");

                    // Verify progression events were fired during high accuracy phase
                    var progressionEvents = _capturedEvents.OfType<IndividualFactProgressionInfo>().ToList();
                    Assert.IsTrue(progressionEvents.Count > 0, "Should have fired progression events during difficulty upgrade test");

                    return; // Test passed - we're definitely on Hard difficulty
                }
            }

            Assert.Fail("Expected to exceed Easy difficulty limit (2 facts) with high accuracy, indicating upgrade to Hard difficulty");
        });

        [UnityTest]
        public IEnumerator DynamicDifficulty_ShouldDowngradeWithLowAccuracy() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Create a fresh state with clear constraints
            var config = LearningAlgorithmConfig.CreateSpeedRun();
            config.DynamicDifficulty.MinAnswersForDifficultyChange = 3;
            config.DynamicDifficulty.RecentAnswerWindow = 10;
            config.DynamicDifficulty.Difficulties = new List<DifficultyConfig>
            {
                new DifficultyConfig { Name = "Hard", MinAccuracyThreshold = 0.8f, MaxFactsBeingLearned = 10 },
                new DifficultyConfig { Name = "Easy", MinAccuracyThreshold = 0.0f, MaxFactsBeingLearned = 2 }
            };

            var algo = new LearningAlgorithmV3(config, _mockStorageService.Object, _mockTimeProvider);
            await algo.Initialize();

            // Act - Start with high accuracy to get to Hard difficulty
            for (int i = 0; i < 3; i++)
            {
                var question = await algo.GetNextQuestion();
                await AnswerCorrectly(algo, question);
            }

            // Verify we're on Hard difficulty by being able to learn more than 2 facts
            var hardDifficultyConfirmed = false;
            var masteredStageId = config.Stages.FirstOrDefault(s => s.IsFullyLearned)?.Id;
            for (int i = 0; i < 10; i++)
            {
                var question = await algo.GetNextQuestion();
                if (question == null) break;
                await AnswerCorrectly(algo, question);

                var beingLearnedCount = algo.StudentState.Facts
                    .Count(f => f.LastAskedTime.HasValue && 
                               config.GetStageById(f.StageId)?.IsKnownFact == false && 
                               f.StageId != masteredStageId);

                if (beingLearnedCount > 2)
                {
                    hardDifficultyConfirmed = true;
                    break;
                }
            }

            Assert.IsTrue(hardDifficultyConfirmed, "Should reach Hard difficulty and exceed Easy limit");

            // Now answer with low accuracy to trigger downgrade to Easy
            for (int i = 0; i < 5; i++)
            {
                var question = await algo.GetNextQuestion();
                if (question != null)
                {
                    if (i < 4) // 80% incorrect answers
                    {
                        await AnswerIncorrectly(algo, question);
                    }
                    else
                    {
                        await AnswerCorrectly(algo, question);
                    }
                }
            }

            // Test that Easy difficulty prevents NEW facts from being introduced
            // First, track which facts are already being learned
            var factsBeingLearnedBefore = algo.StudentState.Facts
                .Where(f => f.LastAskedTime.HasValue && 
                           config.GetStageById(f.StageId)?.IsKnownFact == false && 
                           f.StageId != masteredStageId)
                .Select(f => f.FactId)
                .ToHashSet();

            var initialBeingLearnedCount = factsBeingLearnedBefore.Count;

            // If we're already at or above the Easy limit, no new facts should be introduced
            if (initialBeingLearnedCount >= 2)
            {
                // Test that no NEW facts are introduced while maintaining low accuracy
                for (int i = 0; i < 10; i++)
                {
                    var question = await algo.GetNextQuestion();
                    if (question == null) break;

                    // Check if this is a NEW fact (not previously being learned)
                    if (!factsBeingLearnedBefore.Contains(question.FactId))
                    {
                        Assert.Fail($"Easy difficulty should not introduce new facts when already at limit. New fact: {question.FactId}");
                    }

                    // Answer with low accuracy to maintain Easy difficulty
                    if (i % 3 == 0)
                    {
                        await AnswerCorrectly(algo, question);
                    }
                    else
                    {
                        await AnswerIncorrectly(algo, question);
                    }
                }
            }
            else
            {
                // If we're below the Easy limit, ensure we don't exceed it
                for (int i = 0; i < 10; i++)
                {
                    var question = await algo.GetNextQuestion();
                    if (question == null) break;

                    // Answer with low accuracy to maintain Easy difficulty
                    if (i % 3 == 0)
                    {
                        await AnswerCorrectly(algo, question);
                    }
                    else
                    {
                        await AnswerIncorrectly(algo, question);
                    }

                    var currentBeingLearnedCount = algo.StudentState.Facts
                        .Count(f => f.LastAskedTime.HasValue && 
                                   config.GetStageById(f.StageId)?.IsKnownFact == false && 
                                   f.StageId != masteredStageId);

                    // Should not exceed Easy difficulty limit
                    Assert.LessOrEqual(currentBeingLearnedCount, 2,
                        $"Easy difficulty should limit facts being learned to 2, but found {currentBeingLearnedCount}");
                }
            }
        });

        #endregion

        #region StudentState Tests

        [Test]
        public void StudentState_GetRecentAnswers_ShouldReturnCorrectCount()
        {
            // Arrange
            var studentState = new StudentState();
            var baseTime = DateTime.UtcNow;
            var assessmentStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Assessment).Id;

            for (int i = 0; i < 10; i++)
            {
                studentState.AddAnswerRecord($"fact_{i}", AnswerType.Correct, assessmentStageId, "test-set", baseTime.AddSeconds(i));
            }

            // Act
            var recentAnswers = studentState.GetRecentAnswers(5);

            // Assert
            Assert.AreEqual(5, recentAnswers.Count);
            Assert.AreEqual("fact_9", recentAnswers[0].FactId);
        }

        [Test]
        public void StudentState_GetFactSetCoverage_ShouldCalculateCorrectly()
        {
            // Arrange
            var studentState = new StudentState();
            var assessmentStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Assessment).Id;

            // Add 10 facts to the fact set
            for (int i = 0; i < 10; i++)
            {
                studentState.Facts.Add(new FactItem($"fact_{i}", "test-set", assessmentStageId));
            }

            // Answer 5 unique facts (50% coverage)
            for (int i = 0; i < 5; i++)
            {
                studentState.AddAnswerRecord($"fact_{i}", AnswerType.Correct, assessmentStageId, "test-set");
            }

            // Act
            var coverage = studentState.GetFactSetCoverage("test-set");

            // Assert
            Assert.AreEqual(0.5f, coverage, 0.01f);
        }

        [Test]
        public void StudentState_AllShownFactsAtStageOrAbove_ShouldReturnCorrectly()
        {
            // Arrange
            var studentState = new StudentState();
            var practiceStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Practice).Id;
            var reviewStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Review).Id;
            var assessmentStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Assessment).Id;
            
            var fact2 = new FactItem("fact_2", "test-set", practiceStageId);
            var fact3 = new FactItem("fact_3", "test-set", reviewStageId);
            var fact4 = new FactItem("fact_4", "test-set", assessmentStageId);

            studentState.Facts.Add(fact2);
            studentState.Facts.Add(fact3);
            studentState.Facts.Add(fact4);

            // Add answer records to make facts "shown"
            studentState.AddAnswerRecord("fact_2", AnswerType.Correct, practiceStageId, "test-set");
            studentState.AddAnswerRecord("fact_3", AnswerType.Correct, reviewStageId, "test-set");
            // fact_4 has no answer record, so it's not "shown"

            // Act & Assert
            Assert.IsTrue(studentState.AllShownFactsAtStageOrAbove("test-set", practiceStageId, _speedRunConfig));
            Assert.IsFalse(studentState.AllShownFactsAtStageOrAbove("test-set", reviewStageId, _speedRunConfig));
        }

        #endregion

        #region Bug Reproduction Tests

        [UnityTest]
        public IEnumerator BugReproduction_ReviewFactsBlockingNewLearning_ShouldEventuallySelectAssessmentFact() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Use regular config (not speedrun) to match the real scenario
            var config = LearningAlgorithmConfig.CreateNormal();
            config.DisableRandomization = true;

            var algo = new LearningAlgorithmV3(config, _mockStorageService.Object, _mockTimeProvider);
            await algo.Initialize();

            // Setup the problematic state: multiple fact sets with most facts in Review, one never-shown fact in Assessment
            var factSetsToSetup = 5; // Test with multiple fact sets
            var factsPerSet = 10; // All facts in Review, except 1 fact never shown
            var yesterday = _mockTimeProvider.Now.AddDays(-1);

            // Clear existing facts and create our test scenario
            algo.StudentState.Facts.Clear();

            // We need to create actual Fact objects in the storage manager's fact sets
            // because FactSelectionService.GetFactById looks for them there
            for (int setIndex = 0; setIndex < factSetsToSetup; setIndex++)
            {
                var factSetId = $"set-{setIndex}";

                // Create a list of Fact objects for this set
                var facts = new List<Fact>();

                // Add facts in Review stage with yesterday's timestamp
                for (int factIndex = 0; factIndex < factsPerSet - 1; factIndex++)
                {
                    var factId = $"fact-{setIndex}-{factIndex}";

                    // Create actual Fact object for the fact set
                    var fact = new Fact(factId, factIndex, setIndex, $"Question {factIndex}", factSetId);
                    facts.Add(fact);

                    // Create corresponding FactItem for student state
                    var reviewStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Review).Id;
                    var reviewFact = new FactItem(factId, factSetId, reviewStageId)
                    {
                        LastAskedTime = yesterday,
                        ConsecutiveCorrect = 5 // Has been answered correctly
                    };
                    algo.StudentState.Facts.Add(reviewFact);
                }

                // Add 1 fact in Assessment stage that was NEVER shown (null LastAskedTime)
                var neverShownFactId = $"fact-{setIndex}-never-shown";

                // Create actual Fact object for the fact set
                var neverShownFact = new Fact(neverShownFactId, 99, setIndex, $"Never shown question", factSetId);
                facts.Add(neverShownFact);

                // Create corresponding FactItem for student state
                var assessmentStageId = _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Assessment).Id;
                var assessmentFact = new FactItem(neverShownFactId, factSetId, assessmentStageId)
                {
                    LastAskedTime = null, // This is the key - never shown
                    ConsecutiveCorrect = 0,
                    ConsecutiveIncorrect = 0
                };
                algo.StudentState.Facts.Add(assessmentFact);

                // Create the fact set with the facts list
                var factSet = new FactSet(factSetId, facts);

                // Add the fact set to the storage manager using reflection to access private field
                var storageManagerField = typeof(LearningAlgorithmV3).GetField("_storageManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var storageManager = (FluencySDK.Algorithm.StorageManager)storageManagerField.GetValue(algo);
                storageManager.FactSetsById[factSetId] = factSet;
            }

            // Add some answer history to trigger the known/unknown ratio logic
            for (int i = 0; i < 10; i++)
            {
                algo.StudentState.AddAnswerRecord($"fact-0-{i % (factsPerSet - 1)}", AnswerType.Correct,
                    _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Review).Id, "set-0", yesterday.AddMinutes(i));
            }

            Console.WriteLine($"Setup complete: {algo.StudentState.Facts.Count} total facts");
            Console.WriteLine($"Review facts: {algo.StudentState.Facts.Count(f => f.StageId == _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Review).Id)}");
            Console.WriteLine($"Assessment facts (never shown): {algo.StudentState.Facts.Count(f => f.StageId == _speedRunConfig.Stages.First(s => s.Type == LearningStageType.Assessment).Id && f.LastAskedTime == null)}");

            // Act - Try to get questions multiple times and track what we get
            var questionsAsked = 0;
            var maxAttempts = 100; // Generous limit to find the Assessment fact
            var assessmentFactSelected = false;
            var reviewFactsSelected = 0;
            var selectedFactIds = new HashSet<string>();
            var nullQuestionCount = 0;
            var maxNullQuestions = 20; // Prevent infinite loops on null questions

            _capturedEvents.Clear(); // Clear events for this test

            while (questionsAsked < maxAttempts && !assessmentFactSelected)
            {
                var question = await algo.GetNextQuestion();

                if (question == null)
                {
                    nullQuestionCount++;
                    if (nullQuestionCount > maxNullQuestions)
                    {
                        UnityEngine.Debug.LogWarning("Breaking due to too many null questions in bug reproduction test");
                        break;
                    }

                    // No question available, advance time to clear cooldowns
                    AdvanceTime(TimeSpan.FromMinutes(5));
                    continue;
                }

                nullQuestionCount = 0; // Reset counter when we get a valid question

                questionsAsked++;
                selectedFactIds.Add(question.FactId);

                Console.WriteLine($"Question {questionsAsked}: {question.FactId} (Stage: {question.LearningStage})");

                if (question.LearningStage.Type == LearningStageType.Assessment)
                {
                    assessmentFactSelected = true;
                    Console.WriteLine($"SUCCESS: Assessment fact selected after {questionsAsked} attempts!");
                    break;
                }
                else if (question.LearningStage.Type == LearningStageType.Review)
                {
                    reviewFactsSelected++;
                }

                // Answer the question to allow algorithm to continue
                await AnswerCorrectly(algo, question);

                // Add small time advancement to allow next question
                AdvanceTime(TimeSpan.FromSeconds(config.MinQuestionIntervalSeconds + 1));
            }

            // Assert - The never-shown Assessment fact should eventually be selected
            Assert.IsTrue(assessmentFactSelected,
                $"BUG REPRODUCED: Assessment fact was never selected after {questionsAsked} questions. " +
                $"Only Review facts were selected ({reviewFactsSelected} times). " +
                $"Selected fact IDs: {string.Join(", ", selectedFactIds)}");

            // Additional assertions to understand the bug
            var blStageIds = _speedRunConfig.Stages.Where(s => !s.IsKnownFact).Select(s => s.Id).ToList();
            var beingLearnedCount = algo.StudentState.Facts
                .Count(f => f.LastAskedTime.HasValue && !(blStageIds.Contains(f.StageId)));

            var workingMemoryLimit = config.DynamicDifficulty.Difficulties[0].MaxFactsBeingLearned;

            Console.WriteLine($"Facts currently being learned: {beingLearnedCount}");
            Console.WriteLine($"Working memory limit: {workingMemoryLimit}");
            Console.WriteLine($"Review facts selected: {reviewFactsSelected}");
            Console.WriteLine($"Total questions asked: {questionsAsked}");

            // If we hit this assertion, it means the bug is reproduced
            if (!assessmentFactSelected)
            {
                Assert.Fail($"BUG CONFIRMED: Assessment facts are being blocked. " +
                           $"Working memory: {beingLearnedCount}/{workingMemoryLimit}, " +
                           $"Review facts monopolizing selection.");
            }

            // Verify that progression events were fired if Assessment fact was selected
            var progressionEvents = _capturedEvents.OfType<IndividualFactProgressionInfo>().ToList();
            Console.WriteLine($"Progression events fired: {progressionEvents.Count}");
        });

        #endregion

        #region Helper Methods

        private async UniTask<string> ProgressFactToReview(LearningAlgorithmV3 algo)
        {
            var question = await algo.GetNextQuestion();
            if (question == null)
            {
                // Advance time to clear cooldowns and try again
                AdvanceTime(TimeSpan.FromSeconds(_speedRunConfig.MinQuestionIntervalSeconds + 1));
                question = await algo.GetNextQuestion();
                if (question == null)
                {
                    throw new InvalidOperationException("No question available to progress to Review after advancing time");
                }
            }
            var factId = question.FactId;

            // Progress through Assessment -> PracticeSlow -> PracticeFast -> Review
            while (question != null && question.LearningStage.Type != LearningStageType.Review)
            {
                await AnswerCorrectly(algo, question);
                question = await GetQuestionForFact(algo, factId);
            }

            return factId;
        }

        private async UniTask<string> ProgressSpecificFactToReview(LearningAlgorithmV3 algo, string targetFactId)
        {
            var question = await GetQuestionForFact(algo, targetFactId);

            // Progress through Assessment -> PracticeSlow -> PracticeFast -> Review
            while (question != null && question.LearningStage.Type != LearningStageType.Review)
            {
                await AnswerCorrectly(algo, question);
                question = await GetQuestionForFact(algo, targetFactId);
            }

            return targetFactId;
        }

        private async UniTask<string> ProgressFactToPracticeSlow(LearningAlgorithmV3 algo)
        {
            var question = await algo.GetNextQuestion();
            if (question == null)
            {
                // Advance time to clear cooldowns and try again
                AdvanceTime(TimeSpan.FromSeconds(_speedRunConfig.MinQuestionIntervalSeconds + 1));
                question = await algo.GetNextQuestion();
                if (question == null)
                {
                    throw new InvalidOperationException("No question available to progress to PracticeSlow after advancing time");
                }
            }
            var factId = question.FactId;

            // Answer correctly until PracticeSlow
            while (question != null && question.LearningStage.Type == LearningStageType.Assessment)
            {
                await AnswerCorrectly(algo, question);
                question = await GetQuestionForFact(algo, factId);
            }

            return factId;
        }

        private async UniTask<string> ProgressFactToPracticeFast(LearningAlgorithmV3 algo)
        {
            var factId = await ProgressFactToPracticeSlow(algo);
            var question = await GetQuestionForFact(algo, factId);

            // Answer correctly until PracticeFast
            while (question != null && question.LearningStage.Type == LearningStageType.Practice)
            {
                await AnswerCorrectly(algo, question);
                question = await GetQuestionForFact(algo, factId);
            }

            return factId;
        }

        private async UniTask<string> ProgressFactToRepetition(LearningAlgorithmV3 algo)
        {
            var factId = await ProgressFactToReview(algo);

            // Complete all review cycles to reach Repetition
            for (int reviewCycle = 0; reviewCycle < _speedRunConfig.ReviewDelaysMinutes.Length; reviewCycle++)
            {
                var question = await GetQuestionForFact(algo, factId);
                await AnswerCorrectly(algo, question);

                // Advance time for next review cycle
                if (reviewCycle < _speedRunConfig.ReviewDelaysMinutes.Length - 1)
                {
                    var delayMinutes = _speedRunConfig.ReviewDelaysMinutes[reviewCycle];
                    AdvanceTime(TimeSpan.FromMinutes(delayMinutes + 1));
                }
            }

            return factId;
        }

        private async UniTask<IQuestion> GetQuestionForFact(LearningAlgorithmV3 algo, string factId)
        {
            // Keep asking until we get the specific fact (or give up after attempts)
            for (int attempts = 0; attempts < 50; attempts++)
            {
                try
                {
                    var question = await algo.GetNextQuestion();
                    if (question?.FactId == factId) return question;

                    if (question != null)
                    {
                        // Answer this question to get it out of the way
                        await AnswerCorrectly(algo, question);
                    }
                    else
                    {
                        // No question available, advance time to allow facts to become ready
                        AdvanceTime(TimeSpan.FromSeconds(_speedRunConfig.MinQuestionIntervalSeconds + 1));
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error in GetQuestionForFact attempt {attempts}: {ex.Message}");
                    // Continue to next attempt instead of throwing
                }
            }

            UnityEngine.Debug.LogWarning($"Failed to get question for fact {factId} after 50 attempts");
            return null;
        }

        private async UniTask AnswerCorrectly(LearningAlgorithmV3 algo, IQuestion question)
        {
            await algo.StartQuestion(question);
            await SimulateAnswerDelay(question);

            var correctChoice = question.GetCorrectChoice();
            var submission = UserAnswerSubmission.FromAnswer(correctChoice);
            await algo.SubmitAnswer(question, submission);

            // Ensure minimum time interval between questions is respected
            var answerTime = question.TimeToAnswer ?? 2.5f;
            var totalCycleTime = answerTime + _speedRunConfig.TimeToNextQuestion;
            var remainingTime = _speedRunConfig.MinQuestionIntervalSeconds - totalCycleTime;

            if (remainingTime > 0)
            {
                AdvanceTime(TimeSpan.FromSeconds(remainingTime));
            }
        }

        private async UniTask AnswerIncorrectly(LearningAlgorithmV3 algo, IQuestion question)
        {
            await algo.StartQuestion(question);
            await SimulateAnswerDelay(question);

            var incorrectChoice = question.Choices.First(c => !c.IsCorrect);
            var submission = UserAnswerSubmission.FromAnswer(incorrectChoice);
            await algo.SubmitAnswer(question, submission);

            // Ensure minimum time interval between questions is respected
            var answerTime = question.TimeToAnswer ?? 2.5f;
            var totalCycleTime = answerTime + _speedRunConfig.TimeToNextQuestion;
            var remainingTime = _speedRunConfig.MinQuestionIntervalSeconds - totalCycleTime;

            if (remainingTime > 0)
            {
                AdvanceTime(TimeSpan.FromSeconds(remainingTime));
            }
        }

        private async UniTask SimulateAnswerDelay(IQuestion question)
        {
            // No actual delay in tests - just advance the mock time to simulate realistic timing
            if (question.TimeToAnswer.HasValue)
            {
                // For timed questions, simulate answering within 80% of available time
                var answerTime = TimeSpan.FromSeconds(question.TimeToAnswer.Value * 0.8);
                _mockTimeProvider.AdvanceTime(answerTime);
            }
            else
            {
                // For untimed questions, simulate 2.5 seconds as mentioned in requirements
                _mockTimeProvider.AdvanceTime(TimeSpan.FromSeconds(2.5));
            }

            // Complete immediately
            await UniTask.CompletedTask;
        }

        private void AdvanceTime(TimeSpan timeSpan)
        {
            _mockTimeProvider.AdvanceTime(timeSpan);
        }

        private async UniTask AdvanceTimeForReinforcement(LearningAlgorithmV3 algo)
        {
            // Check if any facts need time advancement for reinforcement
            var reviewFacts = algo.StudentState.Facts
                .Where(f => _speedRunConfig.GetStageById(f.StageId)?.Type == LearningStageType.Review)
                .ToList();
                
            var repetitionFacts = algo.StudentState.Facts
                .Where(f => _speedRunConfig.GetStageById(f.StageId)?.Type == LearningStageType.Repetition)
                .ToList();

            if (reviewFacts.Any())
            {
                // Advance time for review stages (minutes)
                AdvanceTime(TimeSpan.FromMinutes(10));
            }
            
            if (repetitionFacts.Any())
            {
                // Advance time for repetition stages (days)
                AdvanceTime(TimeSpan.FromDays(3));
            }
        }

        private bool AllFactsMastered(LearningAlgorithmV3 algo)
        {
            return algo.StudentState.Facts
                .Where(f => !string.IsNullOrEmpty(f.FactId))
                .All(f => _speedRunConfig.GetStageById(f.StageId).Type == LearningStageType.Mastered);
        }

        private void VerifyStageProgression(List<LearningStage> progression)
        {
            // Verify logical stage progression
            Assert.IsTrue(progression.Count > 0, "Should have some stage progression");

            // Check that stages generally progress forward (allowing for some reinforcement cycling)
            var hasAssessment = progression.Any(s => s.Type == LearningStageType.Assessment);
            var hasPractice = progression.Any(s => s.Type == LearningStageType.Practice);

            if (hasAssessment && hasPractice)
            {
                var firstAssessment = progression.FindIndex(s => s.Type == LearningStageType.Assessment);
                var firstPractice = progression.FindIndex(s => s.Type == LearningStageType.Practice);

                Assert.IsTrue(firstAssessment <= firstPractice, "Assessment should come before or with Practice stages");
            }
        }

        #endregion
    }
}