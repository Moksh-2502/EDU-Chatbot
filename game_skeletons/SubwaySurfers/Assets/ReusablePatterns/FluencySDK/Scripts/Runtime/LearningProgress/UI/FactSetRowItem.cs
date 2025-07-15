using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Individual row item for fact set progress display
    /// Shows: Fact Set Name | Learning Stage + Icon | Progress | Claim Button
    /// </summary>
    public class FactSetRowItem : MonoBehaviour, IFactSetRowItem<FactSetUIItemInfo>
    {
        [Header("UI Components")] [SerializeField]
        private TMP_Text factSetNameText;

        [SerializeField] private Image stageIcon;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimButtonText, stageLabel;

        public FactSetUIItemInfo DataContext { get; private set; }

        // IFactSetRowItem implementation
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;

        private void Awake()
        {
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        private void OnEnable()
        {
            RewardsEventBus.OnRewardClaimResult += OnRewardClaimResult;
            RewardsEventBus.OnClaimableRewardsStatusChanged += OnClaimableRewardsStatusChanged;
        }

        private void OnDisable()
        {
            RewardsEventBus.OnRewardClaimResult -= OnRewardClaimResult;
            RewardsEventBus.OnClaimableRewardsStatusChanged -= OnClaimableRewardsStatusChanged;
        }

        private void OnRewardClaimResult(RewardClaimResultEventArgs args)
        {
            RefreshClaimButton();
        }
        
        private void OnClaimableRewardsStatusChanged(ClaimableRewardsStatusEventArgs args)
        {
            // Check if this fact set is in the changed list
            RefreshClaimButton();
        }

        /// <summary>
        /// Initialize the row with fact set data
        /// </summary>
        public void Repaint(FactSetUIItemInfo data)
        {
            this.DataContext = data;

            // Set fact set name
            if (factSetNameText != null)
                factSetNameText.text = data.Data.FactSet.Id;

            // Set stage icon
            if (this.stageIcon != null && stageIcon != null)
                this.stageIcon.sprite = data.Icon;

            if (this.stageLabel != null)
            {
                this.stageLabel.text = data.Data.GetDominantStageName();
            }

            // Refresh claim button
            RefreshClaimButton();
        }

        private async UniTaskVoid RefreshClaimButtonAsync()
        {
            try
            {
                var claimStatus = await ILearningProgressService.Instance.GetFactSetRewardStatusAsync(DataContext.Data.FactSet.Id);
                claimButton.interactable = claimStatus == RewardStatus.Claimable;

                if (claimButtonText != null)
                {
                    claimButtonText.text = claimStatus == RewardStatus.Claimed ? "Claimed" : "Claim";
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void RefreshClaimButton()
        {
            if (claimButton == null)
                return;
            RefreshClaimButtonAsync().Forget();
        }

        private async UniTaskVoid ProcessClaimButtonClickedAsync()
        {
            try
            {
                if (await ILearningProgressService.Instance.GetFactSetRewardStatusAsync(DataContext.Data.FactSet.Id) !=
                    RewardStatus.Claimable)
                {
                    Debug.Log($"[FactSetRowItem] Cannot claim {DataContext.Data.FactSet.Id} - not completed yet");
                    return;
                }

                Debug.Log($"[FactSetRowItem] Opening reward selection for fact set: {DataContext.Data.FactSet.Id}");
                RewardsEventBus.RaiseSelectRewardClaimRequest(
                    title: "Select Reward",
                    rewardsId: LearningProgressUtils.GetFactSetRewardClaimKey(DataContext.Data.FactSet.Id));
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[FactSetRowItem] Error claiming reward for fact set: {DataContext.Data.FactSet.Id} - {ex.Message}");
            }
        }

        private void OnClaimButtonClicked()
        {
            ProcessClaimButtonClickedAsync().Forget();
        }
    }
}