using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using AIEduChatbot.UnityReactBridge.Storage;
using FluencySDK.Migrations;
using FluencySDK.Services;
using FluencySDK.Algorithm;
using UnityEngine;

namespace FluencySDK
{
    public class LearningAlgorithmV3 : ILearningAlgorithm, ILearningAlgorithmEventHandler
    {
        public StudentState StudentState => _storageManager.StudentState;
        public IReadOnlyDictionary<string, FactSet> FactSets => _storageManager.FactSetsById;

        private LearningAlgorithmConfig _config;
        public LearningAlgorithmConfig Config => _config;

        private ITimeProvider _timeProvider;
        public ITimeProvider TimeProvider => _timeProvider;

        private readonly FactSelectionService _factSelection;
        public FactSelectionService FactSelection => _factSelection;

        private readonly DifficultyManager _difficultyManager;
        public DifficultyManager DifficultyManager => _difficultyManager;

        private readonly PromotionEngine _promotionEngine;
        public PromotionEngine PromotionEngine => _promotionEngine;

        private readonly QuestionFactory _questionFactory;
        public QuestionFactory QuestionFactory => _questionFactory;

        private readonly StorageManager _storageManager;
        public StorageManager StorageManager => _storageManager;

        public LearningAlgorithmV3(
            LearningAlgorithmConfig config = null, 
            IGameStorageService gameStorageService = null, 
            ITimeProvider timeProvider = null,
            StorageManager storageManager = null,
            DifficultyManager difficultyManager = null,
            FactSelectionService factSelection = null,
            PromotionEngine promotionEngine = null,
            QuestionFactory questionFactory = null)
        {
            _config = config ?? new LearningAlgorithmConfig();
            _timeProvider = timeProvider ?? new SystemTimeProvider();
            var storageService = gameStorageService ?? IGameStorageService.Instance;
            _storageManager = storageManager ?? new StorageManager(_config, storageService);
            _difficultyManager = difficultyManager ?? new DifficultyManager(_config.DynamicDifficulty);
            _factSelection = factSelection ?? new FactSelectionService(_storageManager, _config);
            _promotionEngine = promotionEngine ?? new PromotionEngine(_config, _difficultyManager, _timeProvider, this, _storageManager);
            _questionFactory = questionFactory ?? new QuestionFactory(_config);
            
            MigrationsRegistry.InitializeStudentStatesMigrations();
        }

        public async UniTask Initialize()
        {
            await _storageManager.Initialize();
        }

        public UniTask<IQuestion> GetNextQuestion()
        {
            Debug.Log($"[LearningAlgorithmV3] Getting next question");
            if (_storageManager.StudentState == null)
                throw new InvalidOperationException("Initialize the generator first.");

            var (fact, stage) = SelectNextFact();
            if (fact == null)
            {
                Debug.LogWarning("[LearningAlgorithmV3] No facts available for questioning");
                return UniTask.FromResult<IQuestion>(null);
            }
            Debug.Log($"[LearningAlgorithmV3] Selected fact {fact.Id} in {stage} stage");

            var question = _questionFactory.CreateQuestionForStage(fact, stage);
            UpdateLastAskedTime(fact.Id);

            _storageManager.SaveStateAsync().Forget();
            Debug.Log($"[LearningAlgorithmV3] Generated question {question.Id} ({question.TimeToAnswer}s) from fact {fact.Id} in {stage} stage");
            return UniTask.FromResult<IQuestion>(question);
        }

        private (Fact fact, LearningStage stage) SelectNextFact()
        {
            var difficultyConfig = _difficultyManager.GetCurrentDifficultyConfig();
            return _factSelection.SelectNextFact(difficultyConfig, _timeProvider.Now);
        }

        private void UpdateLastAskedTime(string factId)
        {
            var factItem = _storageManager.StudentState.Facts.FirstOrDefault(f => f.FactId == factId);
            if (factItem != null)
            {
                _factSelection.UpdateLastAskedTime(factItem, _timeProvider.Now);
            }
        }

        public UniTask StartQuestion(IQuestion question)
        {
            if (question != null)
            {
                question.TimeStarted = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            return UniTask.CompletedTask;
        }

        private bool ShouldRetrySubmission(IQuestion question, UserAnswerSubmission submission)
        {
            return question is { LearningMode: LearningMode.Grounding } &&
                   submission.AnswerType != AnswerType.Correct;
        }

        public UniTask<SubmitAnswerResult> SubmitAnswer(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            if (ShouldRetrySubmission(question, userAnswerSubmission))
            {
                return UniTask.FromResult(new SubmitAnswerResult(userAnswerSubmission,
                    question.GetCorrectChoice(), _config.TimeToNextQuestion, true));
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            question.TimeEnded ??= currentTime;

            if (userAnswerSubmission.AnswerType != AnswerType.Correct &&
                userAnswerSubmission.AnswerType != AnswerType.Incorrect)
            {
                return UniTask.FromResult(new SubmitAnswerResult(userAnswerSubmission,
                    question.GetCorrectChoice(), _config.TimeToNextQuestion, false));
            }

            if (question.IsMock)
            {
                return UniTask.FromResult(new SubmitAnswerResult(userAnswerSubmission,
                    question.GetCorrectChoice(), _config.TimeToNextQuestion, false));
            }

            ProcessAnswer(question.FactId, question.LearningStage, userAnswerSubmission.AnswerType);
            _storageManager.SaveStateAsync().Forget();

            return UniTask.FromResult(new SubmitAnswerResult(userAnswerSubmission,
                question.GetCorrectChoice(), _config.TimeToNextQuestion, false));
        }

        private void ProcessAnswer(string factId, LearningStage stage, AnswerType answerType)
        {
            var factItem = _storageManager.StudentState.Facts.FirstOrDefault(f => f.FactId == factId);
            if (factItem == null)
            {
                Debug.LogError($"Fact {factId} not found in student state!");
                return;
            }

            var wasKnownFact = stage?.IsKnownFact ?? false;
            _storageManager.StudentState.AddAnswerRecord(factId, answerType, stage.Id, factItem.FactSetId, _timeProvider.Now, wasKnownFact);
            LearningAlgorithmUtils.UpdateFactStats(_storageManager.StudentState.Stats, factId, answerType);
            
            var recentAnswers = _storageManager.StudentState.GetRecentAnswers(_config.DynamicDifficulty.RecentAnswerWindow);
            _difficultyManager.UpdateDifficulty(recentAnswers);
            _promotionEngine.PromoteFacts(factItem, answerType);

            if (answerType == AnswerType.Correct)
            {
                CheckFactSetEvents(factItem);
            }
        }

        private void CheckFactSetEvents(FactItem factItem)
        {
            var stage = _config.GetStageById(factItem.StageId);
            if (stage != null)
            {
                if (stage.Type == LearningStageType.Review)
                {
                    CheckFactSetReviewReady(factItem.FactSetId);
                }
                else if (stage.IsFullyLearned)
                {
                    CheckFactSetCompletion(factItem.FactSetId);
                }
            }
        }

        private void CheckFactSetReviewReady(string factSetId)
        {
            var allFacts = _storageManager.StudentState.GetFactsForSet(factSetId);
            var reviewStage = _config.Stages.FirstOrDefault(s => s.Type == LearningStageType.Review);
            var hasLowerStageFacts = reviewStage != null && _storageManager.StudentState.HasFactsInLowerStages(factSetId, reviewStage.Id, _config);

            if (!hasLowerStageFacts && allFacts.Count > 0)
            {
                Debug.Log($"[LearningAlgorithmV3] Fact set {factSetId} is review ready! All facts at Review+ stage.");

                var nextFactSetId = GetNextFactSetIdInOrder(factSetId);
                var totalAnswers = _storageManager.StudentState.AnswerHistory.Count(a => a.FactSetId == factSetId);
                var totalFacts = allFacts.Count;

                var eventInfo = _promotionEngine.CreateFactSetReviewReadyEvent(factSetId, nextFactSetId, totalAnswers, totalFacts);
                (this as ILearningAlgorithm).NotifyLearningAlgorithmEvent(eventInfo);
            }
        }

        private void CheckFactSetCompletion(string completedFactSetId)
        {
            var allFacts = _storageManager.StudentState.GetFactsForSet(completedFactSetId);
            var masteredStage = _config.Stages.FirstOrDefault(s => s.IsFullyLearned);
            var hasLowerStageFacts = masteredStage != null && _storageManager.StudentState.HasFactsInLowerStages(completedFactSetId, masteredStage.Id, _config);

            if (!hasLowerStageFacts && allFacts.Count > 0)
            {
                Debug.Log($"[LearningAlgorithmV3] Fact set {completedFactSetId} completed! All facts mastered.");

                var nextFactSetId = GetNextFactSetIdInOrder(completedFactSetId);
                var totalAnswers = _storageManager.StudentState.AnswerHistory.Count(a => a.FactSetId == completedFactSetId);
                var totalFacts = allFacts.Count;

                var eventInfo = _promotionEngine.CreateFactSetCompletionEvent(completedFactSetId, nextFactSetId, totalAnswers, totalFacts);
                (this as ILearningAlgorithm).NotifyLearningAlgorithmEvent(eventInfo);
            }
        }

        private string GetNextFactSetIdInOrder(string currentFactSetId)
        {
            var currentIndex = Array.IndexOf(_config.FactSetOrder, currentFactSetId);
            if (currentIndex >= 0 && currentIndex < _config.FactSetOrder.Length - 1)
            {
                return _config.FactSetOrder[currentIndex + 1];
            }
            return "";
        }

        public void OnIndividualFactProgression(IndividualFactProgressionInfo eventInfo)
        {
            (this as ILearningAlgorithm).NotifyLearningAlgorithmEvent(eventInfo);
        }

        public void OnBulkPromotion(BulkPromotionInfo eventInfo)
        {
            (this as ILearningAlgorithm).NotifyLearningAlgorithmEvent(eventInfo);
        }
    }
}