using Cysharp.Threading.Tasks;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using UnityEngine;

namespace Characters
{
    public class CharacterRewardClaimHandler : MonoBehaviour
    {
        private CharacterEquipment _characterEquipment;

        private void Awake()
        {
            _characterEquipment = GetComponentInChildren<CharacterEquipment>(true);
        }

        private void OnEnable()
        {
            RewardsEventBus.OnRewardClaimResult += OnRewardClaimResult;
        }

        private void OnDisable()
        {
            RewardsEventBus.OnRewardClaimResult -= OnRewardClaimResult;
        }

        private void OnRewardClaimResult(RewardClaimResultEventArgs args)
        {
            if (args.Result.Status == RewardStatus.Claimed && args.Result.Reward != null && _characterEquipment != null)
            {
                _characterEquipment.TryEquipItem(args.Result.Reward).Forget();
            }
        }
    }
}