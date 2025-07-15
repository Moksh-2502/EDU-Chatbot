using Cysharp.Threading.Tasks;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Characters
{
    public class CharacterEquipment : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private ItemsDatabase itemDatabase;
        
        private ItemSlot[] _itemSlots;

        private IEquipmentPersistenceService _equipmentPersistenceService;

        private void Awake()
        {
            _equipmentPersistenceService = new EquipmentPersistenceService(itemDatabase);
            _itemSlots = GetComponentsInChildren<ItemSlot>(true);
        }

        private void Start()
        {
            RestoreEquipmentAsync().Forget();
        }

        public async UniTask<bool> TryEquipItem(ItemData itemData)
        {
            if (itemData == null || itemData.ItemType != ItemType.Equipment)
            {
                Debug.LogWarning($"Invalid item data: {itemData?.name ?? "null"}", gameObject);
                return false;
            }

            if (itemData.Item == null)
            {
                Debug.LogWarning($"Item data has no asset reference: {itemData.name}", gameObject);
                return false;
            }

            // Instantiate the item
            var spawnedObjectOp = itemData.Item.InstantiateAsync();
            await spawnedObjectOp;

            if (spawnedObjectOp.Status != AsyncOperationStatus.Succeeded || spawnedObjectOp.Result == null)
            {
                Debug.LogError($"Failed to instantiate item: {itemData.name}", gameObject);
                return false;
            }

            // Try to equip the instantiated item
            var success = TryEquipItem(itemData, spawnedObjectOp.Result);

            if (!success)
            {
                // Clean up if equipping failed
                UnityEngine.AddressableAssets.Addressables.ReleaseInstance(spawnedObjectOp.Result);
            }
            else
            {
                // Save to persistence if successfully equipped
                if (_equipmentPersistenceService != null)
                {
                    await _equipmentPersistenceService.SaveEquippedItemAsync(itemData.EquipSlot, itemData.Id);
                }
            }

            return success;
        }

        public bool TryEquipItem(ItemData itemData, GameObject itemInstance)
        {
            if (_itemSlots == null)
            {
                Debug.LogWarning("Invalid item slots for TryEquipItem", gameObject);
                return false;
            }

            if (itemData == null || itemInstance == null)
            {
                Debug.LogWarning("Invalid parameters for TryEquipItem", gameObject);
                return false;
            }

            // Find the appropriate slot for this item
            ItemSlot targetSlot = null;
            
            // First, try to find a slot that matches the item's designated slot type
            if (itemData.EquipSlot != SlotType.None)
            {
                targetSlot = System.Array.Find(_itemSlots, slot => 
                    slot != null && slot.SlotType == itemData.EquipSlot);
            }

            // Fallback: try to find any compatible slot
            if (targetSlot == null)
            {
                targetSlot = System.Array.Find(_itemSlots, slot => 
                    slot != null && slot.CanEquipItem(itemData));
            }

            if (targetSlot != null)
            {
                var success = targetSlot.TryEquipItem(itemData, itemInstance);
                if (success)
                {
                    Debug.Log($"Successfully equipped {itemData.name} in {targetSlot.SlotType} slot", gameObject);
                    return true;
                }
            }

            Debug.LogError($"No compatible slot found for {itemData.name} (EquipSlot: {itemData.EquipSlot})", gameObject);
            return false;
        }

        public async UniTask<bool> TryEquipItemInSlotAsync(ItemData itemData, SlotType slotType)
        {
            if (itemData == null)
            {
                Debug.LogWarning("Cannot equip null ItemData");
                return false;
            }

            var targetSlot = System.Array.Find(_itemSlots, slot => 
                slot != null && slot.SlotType == slotType);

            if (targetSlot == null)
            {
                Debug.LogWarning($"No slot found for SlotType {slotType}");
                return false;
            }

            if (!targetSlot.CanEquipItem(itemData))
            {
                Debug.LogWarning($"Item {itemData.name} is not compatible with slot {slotType}");
                return false;
            }

            // Use the main equip method to handle instantiation and persistence
            return await TryEquipItem(itemData);
        }

        public async UniTask UnequipSlotAsync(SlotType slotType, bool save)
        {
            var targetSlot = System.Array.Find(_itemSlots, slot => 
                slot != null && slot.SlotType == slotType);

            if (targetSlot == null)
            {
                Debug.LogWarning($"No slot found for SlotType {slotType}");
                return;
            }

            if (!targetSlot.IsOccupied)
            {
                Debug.LogWarning($"Slot {slotType} is not occupied");
                return;
            }

            targetSlot.UnequipCurrentItem();

            // Update persistence
            if (_equipmentPersistenceService != null)
            {
                await _equipmentPersistenceService.RemoveEquippedItemAsync(slotType);
            }

            Debug.Log($"Unequipped item from slot {slotType}");
        }

        public async UniTask UnequipAllItemsAsync(bool save)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot == null || !slot.IsOccupied)
                {
                    continue;
                }

                await UnequipSlotAsync(slot.SlotType, save);
            }
        }

        public ItemData[] GetAllEquippedItems()
        {
            var allItems = new System.Collections.Generic.List<ItemData>();

            foreach (var slot in _itemSlots)
            {
                if (slot == null || slot.IsOccupied == false)
                {
                    continue;
                }

                allItems.Add(slot.CurrentItem);
            }

            return allItems.ToArray();
        }

        public ItemData GetEquippedItemInSlot(SlotType slotType)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot == null || slot.SlotType != slotType || slot.IsOccupied == false)
                {
                    continue;
                }
                return slot.CurrentItem;
            }

            return null;
        }

        public ItemSlot GetSlot(SlotType slotType)
        {
            return System.Array.Find(_itemSlots, slot => 
                slot != null && slot.SlotType == slotType);
        }

        public ItemSlot[] GetAllSlots()
        {
            return _itemSlots ?? new ItemSlot[0];
        }

        public bool IsSlotOccupied(SlotType slotType)
        {
            var slot = GetSlot(slotType);
            return slot?.IsOccupied ?? false;
        }

        /// <summary>
        /// Saves current equipment state to persistence
        /// </summary>
        public async UniTask SaveEquipmentAsync()
        {
            if (_equipmentPersistenceService != null)
            {
                var equippedItemsData = new System.Collections.Generic.Dictionary<SlotType, string>();
                
                foreach (var slot in _itemSlots)
                {
                    if (slot != null && slot.IsOccupied)
                    {
                        equippedItemsData[slot.SlotType] = slot.CurrentItem.Id;
                    }
                }
                
                await _equipmentPersistenceService.SaveEquippedItemDataAsync(equippedItemsData);
            }
        }

        /// <summary>
        /// Restores equipment state from persistence
        /// </summary>
        public async UniTask RestoreEquipmentAsync()
        {
            if (_equipmentPersistenceService == null || itemDatabase == null)
            {
                Debug.LogError("Required components not available for restoring equipped items");
                return;
            }

            // Clear currently equipped items first
            await UnequipAllItemsAsync(false);

            var equippedItemsData = await _equipmentPersistenceService.GetEquippedItemDataAsync();
            int restoredCount = 0;

            foreach (var kvp in equippedItemsData)
            {
                var slotType = kvp.Key;
                var itemId = kvp.Value;

                if (string.IsNullOrEmpty(itemId))
                    continue;

                var itemData = FindItemById(itemId);
                if (itemData == null)
                {
                    Debug.LogWarning($"Could not find ItemData for ID {itemId}, removing from equipped items");
                    await _equipmentPersistenceService.RemoveEquippedItemAsync(slotType);
                    continue;
                }

                // Attempt to equip the item
                var success = await TryEquipItem(itemData);
                if (success)
                {
                    restoredCount++;
                    Debug.Log($"Restored equipped item: {itemData.name} in slot {slotType}");
                }
                else
                {
                    Debug.LogWarning($"Failed to restore equipped item: {itemData.name} in slot {slotType}");
                    await _equipmentPersistenceService.RemoveEquippedItemAsync(slotType);
                }
            }

            Debug.Log($"Restored {restoredCount} equipped items from persistent storage");
        }

        /// <summary>
        /// Validates that all equipped items are still valid
        /// </summary>
        public async UniTask ValidateEquipmentAsync()
        {
            if (_equipmentPersistenceService != null)
            {
                var slotsToUnequip = await _equipmentPersistenceService.ValidateEquippedItemOwnershipAsync();
                
                // Unequip items that are no longer valid
                foreach (var slotType in slotsToUnequip)
                {
                    await UnequipSlotAsync(slotType, false);
                }
            }
        }

        /// <summary>
        /// Finds an ItemData by its unique ID
        /// </summary>
        private ItemData FindItemById(string itemId)
        {
            if (itemDatabase?.Items == null || string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return System.Array.Find(itemDatabase.Items, item => item != null && item.Id == itemId);
        }
    }
}