using System.Linq;
using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    [CreateAssetMenu(fileName = "RewardsConfig", menuName = "Trash Dash/RewardsConfig")]
    public class RewardsConfig : ScriptableObject
    {
        [field: SerializeField] public bool InfiniteRewardsCheat { get; private set; }
        [field: SerializeField] public RewardData[] Rewards { get; private set; }

        public bool TryGetRewardData(string id, out RewardData data)
        {
            data = Rewards?.FirstOrDefault(o => o.Id == id);
            return data is { Items: not null } && data.Items.Count(o=> o != null) > 0;
        }

        public bool TryGetSpecificRewardItem(string rewardId, string itemId, out ItemData itemData)
        {
            if (TryGetRewardData(rewardId, out var rewardData))
            {
                itemData = rewardData.Items.FirstOrDefault(o => o.Id == itemId);
                return itemData != null && itemData.IsValid();
            }

            itemData = null;
            return false;
        }

        public bool TryGetRandomRewardItem(string rewardId, out ItemData itemData)
        {
            if (TryGetRewardData(rewardId, out var rewardData))
            {
                if (rewardData.Items is { Length: > 0 })
                {
                    // take out null ones then pick a random one
                    var validItems = rewardData.Items.Where(o => o != null && o.IsValid()).ToArray();
                    if (validItems.Length > 0)
                    {
                        itemData = validItems[Random.Range(0, validItems.Length)];
                        return true;
                    }
                }
            }

            itemData = null;
            return false;
        }
    }
}