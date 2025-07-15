using Cysharp.Threading.Tasks;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public interface IRewardsProvider
    {
        static IRewardsProvider Instance { get; set; }
#if UNITY_EDITOR
        bool IsClaimCheatOn { get; }
#endif
        UniTask<RewardStatus> GetRewardClaimStatusAsync(string rewardId, string specificRewardId = null);
        UniTask<ClaimRewardResult> ClaimRandomRewardAsync(string rewardId);
        UniTask<ClaimRewardResult> ClaimSpecificRewardAsync(string rewardId, string itemId);
    }
}