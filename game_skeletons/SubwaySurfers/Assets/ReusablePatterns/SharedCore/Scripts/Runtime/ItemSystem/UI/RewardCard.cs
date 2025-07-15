using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem.UI
{
    /// <summary>
    /// Simple reward card component for displaying reward items
    /// </summary>
    public class RewardCard : MonoBehaviour
    {
        [Header("UI Components")] [SerializeField]
        private ItemCard itemCard;
        [SerializeField] private Button claimButton;

        public (string rewardsId, ItemData item) DataContext { get; private set; }

        private void Awake()
        {
            SetupButton();
        }

        private void SetupButton()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnCardButtonClicked);
            }
        }

        /// <summary>
        /// Initializes the reward card with item data
        /// </summary>
        public void Repaint((string rewardsId, ItemData item) data)
        {
            this.DataContext = data;
            itemCard.Repaint(data.item);
        }

        private void OnCardButtonClicked()
        {
            Debug.Log($"Card clicked");
            if (DataContext.item != null)
            {
                Debug.Log($"Card for item {DataContext.item.Name} clicked");
                IRewardsProvider.Instance.ClaimSpecificRewardAsync(this.DataContext.rewardsId, this.DataContext.item.Id)
                    .Forget();
            }
        }
    }
}