using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FluencySDK;
using FluencySDK.Events;
using FluencySDK.Unity;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using UnityEngine;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress
{
    /// <summary>
    /// Service implementation for retrieving learning progress data from the FluencySDK
    /// </summary>
    public class LearningProgressService : ILearningProgressService
    {
        private readonly IQuestionProvider _questionProvider;
        private readonly HashSet<string> _previousClaimableRewards = new HashSet<string>();
        private bool _isCheckingRewards = false;

        public LearningProgressService()
        {
            _questionProvider = BaseQuestionProvider.Instance;
            _questionProvider.OnQuestionEnded += OnQuestionEnded;
            FluencySDKEventBus.OnFluencySDKReady += OnFluencySDKReady;
            ILearningAlgorithm.LearningAlgorithmEvent += OnLearningAlgorithmEvent;
        }

        ~LearningProgressService()
        {
            _questionProvider.OnQuestionEnded -= OnQuestionEnded;
            FluencySDKEventBus.OnFluencySDKReady -= OnFluencySDKReady;
            ILearningAlgorithm.LearningAlgorithmEvent -= OnLearningAlgorithmEvent;
        }

        
        private void OnLearningAlgorithmEvent(ILearningAlgorithmEvent eventInfo)
        {
            // Check for claimable rewards changes after a fact is promoted
            CheckClaimableRewardsChangesAsync().Forget();
        }
        
        private void OnFluencySDKReady(FluencySDKEventBus.FluencySDKReadyEventArgs args)
        {
            // Initialize previous claimable rewards state
            CheckClaimableRewardsChangesAsync().Forget();
        }

        private void OnQuestionEnded(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            // Check for claimable rewards changes without blocking
            CheckClaimableRewardsChangesAsync().Forget();
        }

        private async UniTaskVoid CheckClaimableRewardsChangesAsync()
        {
            // Prevent multiple concurrent checks
            if (_isCheckingRewards) 
            {
                return;
            }

            _isCheckingRewards = true;

            try
            {
                var (hasClaimableRewards, count, factSetIds) = await GetClaimableRewardsInfoAsync();
                
                var claimableStateChanged = false;
                foreach (var factSetId in factSetIds)
                {
                    if (_previousClaimableRewards.Contains(factSetId) == false)
                    {
                        claimableStateChanged = true;
                        break;
                    }
                }
                
                // Only raise event if there are new claimable rewards
                if (claimableStateChanged)
                {
                    RewardsEventBus.RaiseClaimableRewardsStatusChanged(hasClaimableRewards, count, factSetIds);
                }

                // Update previous state
                _previousClaimableRewards.Clear();
                foreach (var item in factSetIds)
                {
                    _previousClaimableRewards.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isCheckingRewards = false;
            }
        }

        public async UniTask<RewardStatus> GetFactSetRewardStatusAsync(string factSetId)
        {
#if UNITY_EDITOR
            if (IRewardsProvider.Instance != null && IRewardsProvider.Instance.IsClaimCheatOn)
            {
                return RewardStatus.Claimable;
            }
#endif
            var factSetProgress = GetFactSetProgress(factSetId);
            if (factSetProgress == null || factSetProgress.CanClaimReward() == false)
            {
                return RewardStatus.NotClaimable;
            }

            if (IRewardsProvider.Instance == null)
            {
                return RewardStatus.NotClaimable;
            }

            var rewardKey = LearningProgressUtils.GetFactSetRewardClaimKey(factSetId);
            var rewardStatus = await IRewardsProvider.Instance.GetRewardClaimStatusAsync(rewardKey);
            
            return rewardStatus;
        }

        /// <summary>
        /// Gets the learning progress for all available fact sets
        /// </summary>
        /// <returns>Collection of fact set progress data ordered by display priority</returns>
        public IList<FactSetProgress> GetFactSetProgresses()
        {
            var algorithm = _questionProvider.Algorithm;
            var studentState = _questionProvider.StudentState;

            if (algorithm?.FactSets == null || studentState == null)
            {
                return Array.Empty<FactSetProgress>();
            }

            var allFactSets = algorithm.FactSets.Values
                .OrderBy(fs => GetFactSetDisplayOrder(fs.Id))
                .ToList();

            return allFactSets.Select(factSet => CreateFactSetProgress(factSet, studentState)).ToList();
        }

        public OverallStats CalculateOverallStatistics(IList<FactSetProgress> customData = null)
        {
            var data = customData ?? GetFactSetProgresses();
            var stats = new OverallStats();
            var config = _questionProvider.Config;

            if (config == null)
                return stats;

            // Basic counts
            stats.TotalFactSets = data.Count;
            stats.CompletedFactSets = data.Count(fs => fs.IsCompleted());
            stats.TotalFacts = data.Sum(fs => fs.GetTotalFactsCount());
            stats.CompletedFacts = data.Sum(fs => fs.GetCompletedFactsCount());

            // Overall progress (weighted average)
            if (stats.TotalFacts > 0)
            {
                float totalProgress = data.Sum(fs => fs.GetProgressPercentage() * fs.GetTotalFactsCount());
                stats.OverallProgressPercent = totalProgress / stats.TotalFacts;
            }

            // Stage distribution
            foreach (var factSetProgress in data)
            {
                var distribution = factSetProgress.GetStageDistribution();
                foreach (var kvp in distribution)
                {
                    if (stats.StageDistribution.ContainsKey(kvp.Key))
                        stats.StageDistribution[kvp.Key] += kvp.Value;
                    else
                        stats.StageDistribution[kvp.Key] = kvp.Value;
                }
            }

            // Performance insights from StudentState
            if (_questionProvider.StudentState != null)
            {
                var studentState = _questionProvider.StudentState;
                stats.CurrentStreak = studentState.GetPersistentCorrectStreak();

                // Calculate overall accuracy and attempts
                if (studentState.Stats != null)
                {
                    stats.TotalAttempts = studentState.Stats.Values.Sum(s => s.TimesShown);
                    var totalCorrect = studentState.Stats.Values.Sum(s => s.TimesCorrect);
                    stats.OverallAccuracy = stats.TotalAttempts > 0 ? (float)totalCorrect / stats.TotalAttempts : 0f;
                }

                // Count struggling facts (facts with low accuracy)
                stats.StrugglingFactsCount = CountStrugglingFacts(studentState);
                
                // Find mastered stage ID and count facts
                var masteredStage = config.Stages.FirstOrDefault(s => s.IsFullyLearned);
                stats.MasteredFactsCount = masteredStage != null && stats.StageDistribution.ContainsKey(masteredStage)
                    ? stats.StageDistribution[masteredStage]
                    : 0;
            }

            return stats;
        }

        private int CountStrugglingFacts(StudentState studentState, IList<FactSetProgress> customData = null)
        {
            var data = customData ?? GetFactSetProgresses();
            int count = 0;

            foreach (var factSetProgress in data)
            {
                count += factSetProgress.GetFactsNeedingAttention(studentState).Count;
            }

            return count;
        }


        private FactSetProgress CreateFactSetProgress(FactSet factSet, StudentState studentState)
        {
            var factProgresses = new List<FactItem>();
            var studentStateFacts = studentState.GetFactsForSet(factSet.Id);
            var config = _questionProvider.Config;
            var defaultStage = config.GetFirstStage();

            foreach (var fact in factSet.Facts)
            {
                var factItem =
                    studentStateFacts.FirstOrDefault(sf => sf.FactId == fact.Id && sf.FactSetId == factSet.Id) ??
                    new FactItem(fact.Id, fact.FactSetId, defaultStage.Id);

                factProgresses.Add(factItem);
            }

            return new FactSetProgress(factSet, factProgresses, config);
        }

        public FactSetProgress GetFactSetProgress(string factSetId)
        {
            var factSet = _questionProvider.Algorithm.FactSets[factSetId];
            var studentState = _questionProvider.StudentState;
            return CreateFactSetProgress(factSet, studentState);
        }

        public async UniTask<bool> HasAnyClaimableRewardsAsync()
        {
            try
            {
                var (hasClaimableRewards, _, _) = await GetClaimableRewardsInfoAsync();
                return hasClaimableRewards;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        public async UniTask<(bool hasClaimableRewards, int count, IList<string> factSetIds)> GetClaimableRewardsInfoAsync()
        {
            try
            {
                var algorithm = _questionProvider.Algorithm;
                var studentState = _questionProvider.StudentState;

                if (algorithm?.FactSets == null || studentState == null)
                {
                    return (false, 0, Array.Empty<string>());
                }
                
                var resultFactSetIds = new List<string>();

                foreach (var factSet in algorithm.FactSets.Values)
                {
                    var rewardStatus = await GetFactSetRewardStatusAsync(factSet.Id);
                    if (rewardStatus == RewardStatus.Claimable)
                    {
                        resultFactSetIds.Add(factSet.Id);
                    }
                }

                return (resultFactSetIds.Count > 0, resultFactSetIds.Count, resultFactSetIds);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return (false, 0, Array.Empty<string>());
            }
        }

        private int GetFactSetDisplayOrder(string factSetId)
        {
            var config = _questionProvider.Config;
            if (config?.FactSetOrder != null)
            {
                var index = Array.IndexOf(config.FactSetOrder, factSetId);
                return index >= 0 ? index : int.MaxValue;
            }

            return factSetId?.GetHashCode() ?? 0;
        }
    }
}