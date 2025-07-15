using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using SubwaySurfers.UI.PreviewSystem;

namespace SubwaySurfers.UI
{
    public class RewardPreviewUI : BasePreviewUI<ItemPreviewData, ItemData>
    {
        // Base class handles Show() and Hide() methods

        public void ShowRewardPreview(ItemPreviewData previewData)
        {
            if (previewData != null)
            {
                UpdateDisplay(previewData);
                Show();
            }
        }

        protected override string GetTitle(ItemPreviewData previewData)
        {
            return previewData?.Title ?? "New Item Unlocked!";
        }

        protected override string GetDescription(ItemPreviewData previewData)
        {
            return previewData?.Description ?? "You've earned a new item!";
        }

        protected override void OnUpdateDisplay(ItemPreviewData previewData)
        {
            
        }

        protected override void OnShown()
        {
            // Reward-specific show logic
        }

        protected override void OnHidden()
        {
            
        }
    }
} 