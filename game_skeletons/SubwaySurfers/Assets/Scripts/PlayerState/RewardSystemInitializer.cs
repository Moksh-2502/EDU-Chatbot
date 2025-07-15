using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using UnityEngine;

namespace SubwaySurfers
{
    [DefaultExecutionOrder(-10)]
    public class RewardSystemInitializer : MonoBehaviour
    {
        [SerializeField] private RewardsConfig rewardsConfig;

        private void Awake()
        {
            IRewardsProvider.Instance =
                new RewardsProvider(rewardsConfig, (PlayerDataProvider)IPlayerDataProvider.Instance);
        }
    }
}