using System;
using System.Collections.Generic;
using System.Linq;
using FluencySDK.Services;
using UnityEngine;

namespace FluencySDK.Algorithm
{
    public class FactSelectionService
    {
        private readonly StorageManager _storageManager;
        private readonly LearningAlgorithmConfig _config;

        public FactSelectionService(StorageManager storageManager, LearningAlgorithmConfig config)
        {
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public (Fact fact, LearningStage stage) SelectNextFact(DifficultyConfig difficultyConfig, DateTime currentTime)
        {
            var masteredStage = _config.Stages.FirstOrDefault(s => s.IsFullyLearned);
            var allBeingLearnedCount = _storageManager.StudentState.Facts
                .Count(f => f.LastAskedTime.HasValue && !IsKnownFact(f) && f.StageId != masteredStage?.Id);
            var (elligibleKnownFacts, elligibleBeingLearned, completelyUnknown) = GetCategorizedElligibleFacts(currentTime);
            var availableLearningSlots = Math.Max(0, difficultyConfig.MaxFactsBeingLearned - allBeingLearnedCount);
            
            var factsToMoveFromUnknown = completelyUnknown
                .OrderBy(f => GetFactSetOrderIndex(f.FactSetId))
                .Take(availableLearningSlots)
                .ToList();
            var unknownFacts = elligibleBeingLearned.Concat(factsToMoveFromUnknown).ToList();

            if (elligibleKnownFacts.Count == 0 && unknownFacts.Count == 0)
            {
                Debug.Log($"[FactSelectionService] No facts eligible for selection");
                var defaultStage = _config.GetFirstStage();
                return (null, defaultStage);
            }

            var recentQuestions = _storageManager.StudentState.GetRecentQuestions(_config.RecentQuestionHistorySize);
            var useKnownPool = ShouldSelectFromKnownPool(elligibleKnownFacts, unknownFacts, recentQuestions, difficultyConfig);

            List<FactItem> selectedPool;
            if (elligibleKnownFacts.Count > 0 && (useKnownPool || unknownFacts.Count == 0))
            {
                Debug.Log($"[FactSelectionService] Selecting from known pool");
                selectedPool = SortKnownFacts(elligibleKnownFacts);
            }
            else
            {
                Debug.Log($"[FactSelectionService] Selecting from unknown pool");
                selectedPool = SortUnknownFacts(unknownFacts);
            }

            var selectedFactItem = selectedPool.First();
            var fact = _storageManager.GetFactById(selectedFactItem.FactId);
            var stage = _config.GetStageById(selectedFactItem.StageId);
            return (fact, stage);
        }

        private (List<FactItem> knownFacts, List<FactItem> beingLearned, List<FactItem> completelyUnknown) 
            GetCategorizedElligibleFacts(DateTime currentTime)
        {
            var knownFacts = new List<FactItem>();
            var beingLearned = new List<FactItem>();
            var completelyUnknown = new List<FactItem>();

            var masteredStage = _config.Stages.FirstOrDefault(s => s.IsFullyLearned);

            foreach (var fact in _storageManager.StudentState.Facts)
            {
                if (fact.StageId == masteredStage?.Id)
                {
                    continue;
                }

                if (IsKnownFact(fact))
                {
                    knownFacts.Add(fact);
                }
                else if (fact.LastAskedTime.HasValue)
                {
                    beingLearned.Add(fact);
                }
                else
                {
                    completelyUnknown.Add(fact);
                }
            }

            var eligibleKnownFacts = knownFacts
                .Where(f => !IsFactOnGeneralCooldown(f, currentTime))
                .Where(f => !IsFactOnReinforcementCooldown(f, currentTime))
                .ToList();

            var eligibleBeingLearned = beingLearned
                .Where(f => !IsFactOnGeneralCooldown(f, currentTime))
                .ToList();

            return (eligibleKnownFacts, eligibleBeingLearned, completelyUnknown);
        }

        public void UpdateLastAskedTime(FactItem fact, DateTime currentTime)
        {
            Debug.Log($"[FactSelectionService] Updating LastAskedTime for fact {fact.FactId} to {currentTime}");
            fact.LastAskedTime = currentTime;
            fact.GenerateRandomFactor();
        }

        private bool IsFactOnGeneralCooldown(FactItem fact, DateTime currentTime)
        {
            if (fact.LastAskedTime == null)
            {
                return false;
            }
            var randomizedInterval = GetRandomizedInterval(fact, _config.MinQuestionIntervalSeconds);
            var secondsSinceAsked = (currentTime - fact.LastAskedTime.Value).TotalSeconds;
            var onCooldown = secondsSinceAsked < randomizedInterval;
            return onCooldown;
        }

        private bool IsFactOnReinforcementCooldown(FactItem fact, DateTime currentTime)
        {
            var nextReinforcementTime = GetNextReinforcementTime(fact);
            var onCooldown = currentTime < nextReinforcementTime;
            return onCooldown;
        }

        private DateTime GetNextReinforcementTime(FactItem fact)
        {
            if (fact.LastAskedTime == null)
            {
                return DateTime.MinValue;
            }

            var stage = _config.GetStageById(fact.StageId);
            if (stage == null)
            {
                return DateTime.MinValue;
            }

            // Only review and repetition stages have reinforcement delays
            if (stage is ReviewStage reviewStage)
            {
                var randomizedDelayMinutes = GetRandomizedInterval(fact, reviewStage.DelayMinutes);
                return fact.LastAskedTime.Value.AddMinutes(randomizedDelayMinutes);
            }
            else if (stage is RepetitionStage repetitionStage)
            {
                var randomizedDelayDays = GetRandomizedInterval(fact, repetitionStage.DelayDays);
                return fact.LastAskedTime.Value.AddDays(randomizedDelayDays);
            }

            return DateTime.MinValue;
        }

        private double GetRandomizedInterval(FactItem fact, double baseInterval)
        {
            if (_config.DisableRandomization)
            {
                return baseInterval;
            }

            var randomized = baseInterval + (baseInterval / 4.0 * fact.RandomFactor);
            return randomized;
        }

        private bool IsKnownFact(FactItem fact)
        {
            var stage = _config.GetStageById(fact.StageId);
            if (stage == null) return false;
            return stage.IsKnownFact;
        }

        private bool ShouldSelectFromKnownPool(List<FactItem> knownFacts, List<FactItem> unknownFacts, 
            List<AnswerRecord> recentQuestions, DifficultyConfig difficultyConfig)
        {
            if (knownFacts.Count == 0)
            {
                return false;
            }
            if (unknownFacts.Count == 0)
            {
                return true;
            }

            if (recentQuestions.Count == 0)
            {
                var pick = UnityEngine.Random.Range(0f, 1f) < 0.5f;
                return pick;
            }

            var knownCount = recentQuestions.Count(q => q.WasKnownFact);
            var knownRatio = (float)knownCount / recentQuestions.Count;

            if (knownRatio < difficultyConfig.KnownFactMinRatio)
            {
                Debug.Log($"[FactSelectionService] Known ratio below min, select from known pool");
                return true;
            }
            else if (knownRatio > difficultyConfig.KnownFactMaxRatio)
            {
                Debug.Log($"[FactSelectionService] Known ratio above max, select from unknown pool");
                return false;
            }
            else
            {
                var pick = UnityEngine.Random.Range(0f, 1f) < 0.5f;
                Debug.Log($"[FactSelectionService] Known ratio in range, random pick: {pick}");
                return pick;
            }
        }

        private List<FactItem> SortUnknownFacts(List<FactItem> unknownFacts)
        {
            return unknownFacts
                .OrderBy(f => GetFactSetOrderIndex(f.FactSetId))
                .ThenBy(f => _config.GetStageById(f.StageId)?.Order ?? int.MaxValue)
                .ThenBy(f => f.LastAskedTime ?? DateTime.MinValue)
                .ToList();
        }

        private List<FactItem> SortKnownFacts(List<FactItem> knownFacts)
        {
            return knownFacts
                .OrderBy(f => GetFactSetOrderIndex(f.FactSetId))
                .ThenBy(f => _config.GetStageById(f.StageId)?.Order ?? int.MaxValue)
                .ThenBy(f => GetNextReinforcementTime(f))
                .ThenBy(f => f.LastAskedTime ?? DateTime.MinValue)
                .ToList();
        }

        private int GetFactSetOrderIndex(string factSetId)
        {
            var index = Array.IndexOf(_config.FactSetOrder, factSetId);
            return index == -1 ? int.MaxValue : index;
        }
    }
} 