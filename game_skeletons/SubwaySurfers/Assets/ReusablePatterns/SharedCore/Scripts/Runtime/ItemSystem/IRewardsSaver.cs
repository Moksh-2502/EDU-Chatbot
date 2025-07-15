using Cysharp.Threading.Tasks;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public interface IRewardsSaver
    {
        UniTask<bool> GetIsRewardClaimedAsync(string id);
        UniTask SetIsRewardClaimedAsync(string id, bool isClaimed);
    }
}