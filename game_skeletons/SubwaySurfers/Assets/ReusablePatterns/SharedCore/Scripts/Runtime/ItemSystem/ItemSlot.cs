using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    public class ItemSlot : MonoBehaviour
    {
        [field: SerializeField] public SlotType SlotType { get; private set; }

        public ItemData CurrentItem { get; private set; }
        public GameObject CurrentItemInstance { get; private set; }
        public bool IsOccupied => CurrentItemInstance != null;

        public bool CanEquipItem(ItemData itemData)
        {
            if (itemData == null || itemData.ItemType != ItemType.Equipment)
            {
                return false;
            }

            // Check slot compatibility based on item naming patterns
            return IsItemCompatibleWithSlot(itemData);
        }

        public bool TryEquipItem(ItemData itemData, GameObject itemInstance)
        {
            if (!CanEquipItem(itemData) || itemInstance == null)
            {
                return false;
            }

            // Unequip current item if any
            UnequipCurrentItem();

            // Equip new item
            itemInstance.transform.SetParent(transform, false);
            if(itemInstance.TryGetComponent<EquipmentWorldInfo>(out var worldInfo))
            {
                worldInfo.ApplyEquipState();
            }
            GameObjectUtils.SetLayerRecursively(itemInstance, gameObject.layer);

            CurrentItem = itemData;
            CurrentItemInstance = itemInstance;

            return true;
        }

        public void UnequipCurrentItem()
        {
            if (IsOccupied)
            {
                Addressables.ReleaseInstance(CurrentItemInstance);
                CurrentItemInstance = null;
            }

            CurrentItem = null;
        }

        private bool IsItemCompatibleWithSlot(ItemData itemData)
        {
            return itemData != null && itemData.ItemType == ItemType.Equipment && itemData.EquipSlot == this.SlotType;
        }
    }
}