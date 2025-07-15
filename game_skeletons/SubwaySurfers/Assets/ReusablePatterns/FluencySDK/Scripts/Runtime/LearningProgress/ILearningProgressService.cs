using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress
{
    /// <summary>
    /// Service interface for retrieving learning progress data
    /// </summary>
    public interface ILearningProgressService
    {
        static ILearningProgressService Instance { get; private set; } = new LearningProgressService();

        /// <summary>
        /// Gets the learning progress for all available fact sets
        /// </summary>
        /// <returns>Collection of fact set progress data ordered by display priority</returns>
        IList<FactSetProgress> GetFactSetProgresses();
        FactSetProgress GetFactSetProgress(string factSetId);
        OverallStats CalculateOverallStatistics(IList<FactSetProgress> customData = null);

        UniTask<RewardStatus> GetFactSetRewardStatusAsync(string factSetId);

        /// <summary>
        /// Checks if there are any claimable rewards across all fact sets
        /// </summary>
        /// <returns>Task that returns true if any rewards are claimable</returns>
        UniTask<bool> HasAnyClaimableRewardsAsync();

        /// <summary>
        /// Gets detailed information about claimable rewards across all fact sets
        /// </summary>
        /// <returns>Task that returns claimable rewards information</returns>
        UniTask<(bool hasClaimableRewards, int count, IList<string> factSetIds)> GetClaimableRewardsInfoAsync();
    }
}