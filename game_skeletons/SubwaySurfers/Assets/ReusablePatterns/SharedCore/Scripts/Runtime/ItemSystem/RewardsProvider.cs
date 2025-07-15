using Cysharp.Threading.Tasks;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public class RewardsProvider : IRewardsProvider
    {
        private readonly RewardsConfig _rewardsConfig;
        private readonly IRewardsSaver _rewardsSaver;

        public RewardsProvider(RewardsConfig rewardsConfig, IRewardsSaver saver)
        {
            _rewardsConfig = rewardsConfig;
            _rewardsSaver = saver;
        }

#if UNITY_EDITOR
        public bool IsClaimCheatOn => _rewardsConfig != null && _rewardsConfig.InfiniteRewardsCheat;
#endif

        public async UniTask<RewardStatus> GetRewardClaimStatusAsync(string rewardId, string specificRewardId = null)
        {
            if (_rewardsConfig == null || _rewardsConfig.TryGetRewardData(rewardId, out var rewardData) == false
                                       || rewardData.Items == null || rewardData.Items.Length == 0)
            {
                return RewardStatus.NotClaimable;
            }

            if (string.IsNullOrWhiteSpace(specificRewardId) == false &&
                _rewardsConfig.TryGetSpecificRewardItem(rewardId, specificRewardId, out var itemData)
                == false)
            {
                return RewardStatus.NotClaimable;
            }
#if UNITY_EDITOR
            if (IsClaimCheatOn)
            {
                return RewardStatus.Claimable;
            }
#endif

            if (_rewardsSaver == null)
            {
                return RewardStatus.NotClaimable;
            }

            bool isClaimed = await _rewardsSaver.GetIsRewardClaimedAsync(rewardId);

            if (isClaimed)
            {
                return RewardStatus.Claimed;
            }

            return RewardStatus.Claimable;
        }

        public async UniTask<ClaimRewardResult> ClaimRandomRewardAsync(string rewardId)
        {
            if (await GetRewardClaimStatusAsync(rewardId) != RewardStatus.Claimable)
            {
                return ClaimRewardResult.NotClaimable;
            }

            if (_rewardsConfig == null || _rewardsConfig.TryGetRandomRewardItem(rewardId, out var reward) == false)
            {
                return ClaimRewardResult.NotClaimable;
            }

            return await ClaimSpecificRewardAsync(rewardId, reward.Id);
        }

        /// <summary>
        /// Claims a specific reward by item ID
        /// </summary>
        public async UniTask<ClaimRewardResult> ClaimSpecificRewardAsync(string rewardId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || await GetRewardClaimStatusAsync(rewardId, itemId) != RewardStatus.Claimable)
            {
                return ClaimRewardResult.NotClaimable;
            }

            if (_rewardsSaver == null)
            {
                return ClaimRewardResult.NotClaimable;
            }

            if (_rewardsConfig.TryGetSpecificRewardItem(rewardId, itemId, out var selectedReward) == false)
            {
                return ClaimRewardResult.NotClaimable;
            }

            await _rewardsSaver.SetIsRewardClaimedAsync(rewardId, true);
            var result = new ClaimRewardResult(RewardStatus.Claimed, selectedReward);
            RewardsEventBus.RaiseRewardClaimResult(rewardId, result);
            return result;
        }
    }
}