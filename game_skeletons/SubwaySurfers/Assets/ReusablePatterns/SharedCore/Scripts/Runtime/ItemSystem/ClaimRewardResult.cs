namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public class ClaimRewardResult
    {
        public static readonly ClaimRewardResult NotClaimable = new(RewardStatus.NotClaimable, null);
        public RewardStatus Status { get; private set; }
        public ItemData Reward { get; private set; }

        public ClaimRewardResult(RewardStatus status, ItemData reward)
        {
            Status = status;
            Reward = reward;
        }
    }
}