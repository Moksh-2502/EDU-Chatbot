using UnityEngine;
using UnityEngine.UI;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using Cysharp.Threading.Tasks;
using SubwaySurfers.UI.PreviewSystem;

namespace SubwaySurfers.UI
{
    public class RewardPreviewController : BasePreviewController<ItemPreviewData, ItemData>
    {
        [Header("UI References")] [SerializeField]
        private GameObject[] previewObjects;

        [SerializeField] private RewardPreviewUI rewardPreviewUI;
        [SerializeField] private Button dismissButton;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        protected override void Initialize()
        {
            base.Initialize();
            InitializeRewardPreview();
        }

        private void InitializeRewardPreview()
        {
            // Hide all preview UI objects initially
            ToggleObjects(false);

            // Setup dismiss button
            if (dismissButton != null)
            {
                dismissButton.onClick.AddListener(HidePreview);
            }

            Debug.Log("RewardPreviewController: Initialized");
        }

        private void SubscribeToEvents()
        {
            RewardsEventBus.OnRewardClaimResult += OnRewardClaimResult;
        }

        private void UnsubscribeFromEvents()
        {
            RewardsEventBus.OnRewardClaimResult -= OnRewardClaimResult;
        }

        private void OnRewardClaimResult(RewardClaimResultEventArgs args)
        {
            if (args.Result.Status == RewardStatus.Claimed && args.Result.Reward != null)
            {
                ShowPreview(args.Result.Reward);
            }
        }

        public void ShowPreview(ItemData itemData)
        {
            if (itemData == null)
            {
                Debug.LogWarning("RewardPreviewController: Cannot show preview - ItemData is null");
                return;
            }

            var previewData = ItemPreviewData.Create(itemData);
            if (previewData != null)
            {
                ShowPreviewAsync(previewData).Forget();
            }
        }

        protected override async UniTask OnPreviewStarting(ItemPreviewData previewData)
        {
            if (previewData == null)
            {
                Debug.LogError("RewardPreviewController: Invalid preview data type");
                return;
            }

            // Show UI objects
            ToggleObjects(true);

            if (rewardPreviewUI != null)
            {
                rewardPreviewUI.ShowRewardPreview(previewData);
            }

            await UniTask.Yield();
        }

        protected override UniTask OnPreviewReady(ItemPreviewData previewData)
        {
            return UniTask.CompletedTask;
        }

        protected override void OnPreviewHiding()
        {
            // Hide UI objects
            ToggleObjects(false);

            if (rewardPreviewUI != null)
            {
                rewardPreviewUI.Hide();
            }
        }

        protected override void OnPreviewInstanceSetup(GameObject instance, ItemPreviewData previewData)
        {
            // Item-specific instance setup can go here
        }

        // Removed - now handled by base class

        private void ToggleObjects(bool isVisible)
        {
            if (previewObjects != null)
            {
                foreach (var obj in previewObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(isVisible);
                    }
                }
            }
        }
    }
}