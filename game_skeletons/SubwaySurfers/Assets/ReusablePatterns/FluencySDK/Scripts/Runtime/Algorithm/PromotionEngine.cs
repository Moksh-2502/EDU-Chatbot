using System;
using System.Collections.Generic;
using System.Linq;
using FluencySDK.Services;
using UnityEngine;

namespace FluencySDK.Algorithm
{
    public class PromotionEngine
    {
        private readonly LearningAlgorithmConfig _config;
        private readonly DifficultyManager _difficultyManager;
        private readonly ITimeProvider _timeProvider;
        private readonly ILearningAlgorithmEventHandler _eventHandler;
        private readonly StorageManager _storageManager;

        public PromotionEngine(LearningAlgorithmConfig config, DifficultyManager difficultyManager, ITimeProvider timeProvider, ILearningAlgorithmEventHandler eventHandler = null, StorageManager storageManager = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _difficultyManager = difficultyManager ?? throw new ArgumentNullException(nameof(difficultyManager));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _eventHandler = eventHandler;
        }

        public void PromoteFacts(FactItem factItem, AnswerType answerType)
        {
            factItem.UpdateStreak(answerType);
            var difficultyConfig = _difficultyManager.GetCurrentDifficultyConfig();

            if (answerType == AnswerType.Correct)
            {
                var stage = _config.GetStageById(factItem.StageId);
                if (stage?.Type == LearningStageType.Review)
                {
                    Debug.Log($"[PromotionEngine] Updating last asked time for review fact {factItem.FactId}");
                    factItem.UpdateLastAskedTime(_timeProvider.Now);
                }
                else if (stage?.Type == LearningStageType.Repetition)
                {
                    Debug.Log($"[PromotionEngine] Updating last asked time for repetition fact {factItem.FactId}");
                    factItem.UpdateLastAskedTime(_timeProvider.Now);
                }

                if (CheckBulkPromotion(factItem, difficultyConfig))
                {
                    Debug.Log($"[PromotionEngine] Bulk promotion triggered for fact set {factItem.FactSetId} at stage {factItem.StageId}");
                    ApplyBulkPromotion(factItem, difficultyConfig);
                    return;
                }
            }

            if (answerType == AnswerType.Correct && ShouldPromoteFact(factItem, difficultyConfig))
            {
                Debug.Log($"[PromotionEngine] Promoting fact {factItem.FactId} in stage {factItem.StageId}");
                PromoteFact(factItem, difficultyConfig);
            }
            else if (answerType == AnswerType.Incorrect && ShouldDemoteFact(factItem, difficultyConfig))
            {
                Debug.Log($"[PromotionEngine] Demoting fact {factItem.FactId} in stage {factItem.StageId}");
                DemoteFact(factItem, difficultyConfig);
            }
        }

        private bool ShouldPromoteFact(FactItem factItem, DifficultyConfig difficultyConfig)
        {
            var threshold = difficultyConfig.PromotionThresholds.TryGetValue(factItem.StageId, out var promoteThreshold) ? promoteThreshold : 1;

            if (threshold == 0)
                return true;

            return factItem.ConsecutiveCorrect >= threshold;
        }

        private bool ShouldDemoteFact(FactItem factItem, DifficultyConfig difficultyConfig)
        {
            var threshold = difficultyConfig.DemotionThresholds.TryGetValue(factItem.StageId, out var demoteThreshold) ? demoteThreshold : 2;

            if (threshold == 0)
                return true;

            return factItem.ConsecutiveIncorrect >= threshold;
        }

        public void PromoteFact(FactItem factItem)
        {
            var difficultyConfig = _difficultyManager.GetCurrentDifficultyConfig();
            PromoteFact(factItem, difficultyConfig);
        }

        public void DemoteFact(FactItem factItem)
        {
            var difficultyConfig = _difficultyManager.GetCurrentDifficultyConfig();
            DemoteFact(factItem, difficultyConfig);
        }

        private void PromoteFact(FactItem factItem, DifficultyConfig difficultyConfig)
        {
            var oldStageId = factItem.StageId;
            var oldStage = _config.GetStageById(oldStageId);
            var newStage = GetPromotionStage(factItem, difficultyConfig);

            if (newStage?.Id != oldStageId)
            {
                factItem.StageId = newStage?.Id ?? oldStageId;
                factItem.ResetStreak();

                var eventInfo = CreatePromotionEvent(factItem, oldStage, newStage, AnswerType.Correct);
                _eventHandler?.OnIndividualFactProgression(eventInfo);
            }
        }

        private void DemoteFact(FactItem factItem, DifficultyConfig difficultyConfig)
        {
            var oldStageId = factItem.StageId;
            var oldStage = _config.GetStageById(oldStageId);
            var newStage = GetDemotionStage(factItem, difficultyConfig);

            if (newStage?.Id != oldStageId)
            {
                factItem.StageId = newStage?.Id ?? oldStageId;
                factItem.ResetStreak();

                var eventInfo = CreateDemotionEvent(factItem, oldStage, newStage, AnswerType.Incorrect);
                _eventHandler?.OnIndividualFactProgression(eventInfo);
            }
        }

        private LearningStage GetPromotionStage(FactItem factItem, DifficultyConfig difficultyConfig)
        {
            var currentStage = _config.GetStageById(factItem.StageId);
            if (currentStage == null)
                return _config.GetFirstStage();

            var orderedStages = _config.Stages.OrderBy(s => s.Order).ToList();
            var currentIndex = orderedStages.FindIndex(s => s.Id == factItem.StageId);
            
            if (currentIndex == -1 || currentIndex >= orderedStages.Count - 1)
            {
                return orderedStages.LastOrDefault();
            }

            for (int i = currentIndex + 1; i < orderedStages.Count; i++)
            {
                var candidateStage = orderedStages[i];

                if (candidateStage.IsFullyLearned)
                    return candidateStage;

                var threshold = difficultyConfig.PromotionThresholds.TryGetValue(candidateStage.Id, out var candidatePromoteThreshold) ? candidatePromoteThreshold : 1;
                if (threshold > 0)
                {
                    return candidateStage;
                }
            }

            return orderedStages.LastOrDefault();
        }

        private LearningStage GetDemotionStage(FactItem factItem, DifficultyConfig difficultyConfig)
        {
            var currentStage = _config.GetStageById(factItem.StageId);
            if (currentStage == null)
                return _config.GetFirstStage();

            var orderedStages = _config.Stages.OrderBy(s => s.Order).ToList();
            var currentIndex = orderedStages.FindIndex(s => s.Id == factItem.StageId);

            if (currentIndex <= 0)
            {
                return orderedStages.FirstOrDefault();
            }

            for (int i = currentIndex - 1; i >= 0; i--)
            {
                var candidateStage = orderedStages[i];

                if (candidateStage.Type == LearningStageType.Grounding)
                    return candidateStage;

                var threshold = difficultyConfig.DemotionThresholds.TryGetValue(candidateStage.Id, out var candidateDemoteThreshold) ? candidateDemoteThreshold : 2;
                if (threshold > 0)
                {
                    return candidateStage;
                }
            }

            return orderedStages.FirstOrDefault();
        }

        private IndividualFactProgressionInfo CreatePromotionEvent(FactItem factItem, LearningStage oldStage, LearningStage newStage, AnswerType answerType)
        {
            return new IndividualFactProgressionInfo(
                factItem.FactId, factItem.FactSetId, oldStage?.Id, newStage?.Id,
                answerType, factItem.ConsecutiveCorrect, DateTimeOffset.UtcNow);
        }

        private IndividualFactProgressionInfo CreateDemotionEvent(FactItem factItem, LearningStage oldStage, LearningStage newStage, AnswerType answerType)
        {
            return new IndividualFactProgressionInfo(
                factItem.FactId, factItem.FactSetId, oldStage?.Id, newStage?.Id,
                answerType, factItem.ConsecutiveIncorrect, DateTimeOffset.UtcNow);
        }

        public FactSetReviewReadyInfo CreateFactSetReviewReadyEvent(string factSetId, string nextFactSetId, int totalAnswers, int totalFacts)
        {
            return new FactSetReviewReadyInfo(
                factSetId,
                nextFactSetId,
                totalAnswers,
                totalFacts,
                DateTimeOffset.UtcNow
            );
        }

        public FactSetCompletionInfo CreateFactSetCompletionEvent(string factSetId, string nextFactSetId, int totalAnswers, int totalFacts)
        {
            return new FactSetCompletionInfo(
                factSetId,
                nextFactSetId,
                totalAnswers,
                totalFacts,
                DateTimeOffset.UtcNow
            );
        }

        private (List<AnswerRecord> recentAnswers, float stageCoverage) GetBulkPromotionAnalytics(string factSetId, string stageId, StudentState studentState, int minConsecutiveCorrect)
        {
            var recentAnswers = studentState.AnswerHistory
                .Where(a => a.FactSetId == factSetId && a.StageId == stageId)
                .OrderByDescending(a => a.AnswerTime)
                .Take(minConsecutiveCorrect)
                .ToList();

            var totalFactsInStage = studentState.Facts.Count(f => f.FactSetId == factSetId && f.StageId == stageId);
            var answeredFactsInStage = studentState.AnswerHistory
                .Where(a => a.FactSetId == factSetId && a.StageId == stageId)
                .Select(a => a.FactId)
                .Distinct()
                .Count();

            var stageCoverage = totalFactsInStage > 0 ? (float)answeredFactsInStage / totalFactsInStage : 0f;

            return (recentAnswers, stageCoverage);
        }

        private bool CheckBulkPromotion(FactItem currentFact, DifficultyConfig difficultyConfig)
        {
            var bulkConfig = difficultyConfig.BulkPromotion;
            var studentState = _storageManager.StudentState;

            if (!bulkConfig.Enabled)
            {
                return false;
            }

            var factsAtStage = studentState.Facts
                .Where(f => f.FactSetId == currentFact.FactSetId && f.StageId == currentFact.StageId)
                .ToList();

            if (factsAtStage.Count < 2)
            {
                return false;
            }

            var analytics = GetBulkPromotionAnalytics(currentFact.FactSetId, currentFact.StageId, studentState, bulkConfig.MinConsecutiveCorrect);
            var recentAnswers = analytics.recentAnswers;
            var stageCoverage = analytics.stageCoverage;

            Debug.Log($"[PromotionEngine] Bulk promotion coverage: {stageCoverage * 100f}% (needed: {bulkConfig.MinFactSetCoveragePercent * 100f}%)");

            bool hasConsecutiveCorrect = recentAnswers.Count >= bulkConfig.MinConsecutiveCorrect &&
                                        recentAnswers.All(a => a.AnswerType == AnswerType.Correct);

            if (!hasConsecutiveCorrect)
            {
                return false;
            }

            bool meetsCoverage = stageCoverage >= bulkConfig.MinFactSetCoveragePercent;

            return meetsCoverage;
        }

        private void ApplyBulkPromotion(FactItem currentFact, DifficultyConfig difficultyConfig)
        {
            var bulkConfig = difficultyConfig.BulkPromotion;
            var studentState = _storageManager.StudentState;

            var factsToPromote = studentState.Facts
                .Where(f => f.FactSetId == currentFact.FactSetId && f.StageId == currentFact.StageId)
                .ToList();

            var analytics = GetBulkPromotionAnalytics(currentFact.FactSetId, currentFact.StageId, studentState, bulkConfig.MinConsecutiveCorrect);
            var recentAnswers = analytics.recentAnswers;
            var stageCoverage = analytics.stageCoverage;

            Debug.Log($"[PromotionEngine] Bulk promoting {factsToPromote.Count} facts in set {currentFact.FactSetId} at stage {currentFact.StageId}");

            var promotedCount = 0;
            foreach (var factItem in factsToPromote)
            {
                var oldStageId = factItem.StageId;
                var oldStage = _config.GetStageById(oldStageId);
                var newStage = GetPromotionStage(factItem, difficultyConfig);

                if (newStage?.Id != oldStageId)
                {
                    factItem.StageId = newStage?.Id ?? oldStageId;
                    factItem.ResetStreak();

                    var eventInfo = CreatePromotionEvent(factItem, oldStage, newStage, AnswerType.Correct);
                    _eventHandler?.OnIndividualFactProgression(eventInfo);
                }

                promotedCount++;
            }

            var bulkEventInfo = new BulkPromotionInfo(
                currentFact.FactSetId, 
                promotedCount, 
                recentAnswers.Count, 
                stageCoverage, 
                DateTimeOffset.UtcNow);
            
            _eventHandler?.OnBulkPromotion(bulkEventInfo);
        }

    }
} 