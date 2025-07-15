using UnityEngine;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Preview data wrapper for ItemData objects
    /// Contains all necessary information for previewing an item/reward
    /// </summary>
    public class ItemPreviewData : IPreviewData<ItemData>
    {
        public ItemData Data { get; private set; }
        public PreviwableAssetAddress AssetAddress { get; private set; }
        public TransformData? TransformData {get; private set;}
        public Vector3? Scale { get; private set; }

        // Item-specific properties
        public string Title { get; private set; }
        public string Description { get; private set; }
        public Sprite Icon { get; private set; }

        private ItemPreviewData() { }

        public static ItemPreviewData Create(
            ItemData itemData,
            TransformData? customTransformData = null,
            string customTitle = null,
            string customDescription = null)
        {
            if (itemData == null)
            {
                Debug.LogError("ItemPreviewData: Cannot create preview data with null ItemData");
                return null;
            }



            return new ItemPreviewData
            {
                Data = itemData,
                AssetAddress = PreviwableAssetAddress.FromAssetReference(itemData.Item),
                TransformData = customTransformData,
                Title = customTitle ?? GenerateTitle(itemData),
                Description = customDescription ?? GenerateDescription(itemData),
                Icon = null // ItemData doesn't have a built-in icon, could be extended if needed
            };
        }

        private static string GenerateTitle(ItemData itemData)
        {
            if (!string.IsNullOrWhiteSpace(itemData.Name))
            {
                return $"New {itemData.Name} Unlocked!";
            }

            switch (itemData.ItemType)
            {
                case ItemType.Equipment:
                    switch (itemData.EquipSlot)
                    {
                        case SlotType.Head:
                            return "New Hat Unlocked!";
                        case SlotType.Back:
                            return "New Backpack Unlocked!";
                        case SlotType.Plushie:
                            return "New Plushie Unlocked!";
                        case SlotType.Glasses:
                            return "New Glasses Unlocked!";
                        default:
                            return "New Item Unlocked!";
                    }
                case ItemType.Loot:
                    return "New Loot Unlocked!";
                default:
                    return "New Item Unlocked!";
            }
        }

        private static string GenerateDescription(ItemData itemData)
        {
            var itemName = itemData.EquipSlot != SlotType.None ? itemData.EquipSlot.ToString().ToLower() : "item";
            return $"You've earned a new {itemName}! Check it out in your collection.";
        }
    }
} 