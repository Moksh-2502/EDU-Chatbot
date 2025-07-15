using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using SubwaySurfers;
using UnityEngine;

namespace Characters
{
    /// <summary>
    /// Pure data persistence service - only handles saving/loading equipment data
    /// No knowledge of equipment logic or item instances
    /// </summary>
    public class EquipmentPersistenceService : IEquipmentPersistenceService
    {
        private readonly ItemsDatabase _itemsDatabase;

        public EquipmentPersistenceService(ItemsDatabase itemsDatabase)
        {
            this._itemsDatabase = itemsDatabase;
        }

        /// <summary>
        /// Gets the saved equipped item data from persistence
        /// </summary>
        public async UniTask<Dictionary<SlotType, string>> GetEquippedItemDataAsync()
        {
            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData?.Equipment == null)
            {
                return new Dictionary<SlotType, string>();
            }

            return playerData.Equipment.GetAllEquippedItems();
        }

        /// <summary>
        /// Saves equipped item data to persistence
        /// </summary>
        public async UniTask SaveEquippedItemDataAsync(Dictionary<SlotType, string> equippedItems)
        {
            if (equippedItems == null)
            {
                Debug.LogWarning("Cannot save null equipped items data");
                return;
            }

            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData == null)
            {
                Debug.LogError("PlayerData not available for saving equipped items");
                return;
            }

            // Update equipped items in player data
            foreach (var kvp in equippedItems)
            {
                playerData.Equipment.SetEquippedItem(kvp.Key, kvp.Value);
            }

            // Save the modified player data
            await IPlayerDataProvider.Instance.SaveAsync();
            Debug.Log($"Saved {equippedItems.Count} equipped items to persistent storage");
        }

        /// <summary>
        /// Saves a single equipped item
        /// </summary>
        public async UniTask SaveEquippedItemAsync(SlotType slotType, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning($"Cannot save empty item ID for slot {slotType}");
                return;
            }

            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData == null)
            {
                Debug.LogError("PlayerData not available for saving equipped item");
                return;
            }

            // Add to inventory if not owned
            if (!playerData.Equipment.OwnsEquipmentItem(itemId))
            {
                playerData.Equipment.AddEquipmentItem(itemId);
            }

            // Set equipped item
            playerData.Equipment.SetEquippedItem(slotType, itemId);
            
            // Save the modified player data
            await IPlayerDataProvider.Instance.SaveAsync();
        }

        /// <summary>
        /// Removes an equipped item from persistence
        /// </summary>
        public async UniTask RemoveEquippedItemAsync(SlotType slotType)
        {
            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData == null)
            {
                Debug.LogError("PlayerData not available for removing equipped item");
                return;
            }

            playerData.Equipment.SetEquippedItem(slotType, null);
            await IPlayerDataProvider.Instance.SaveAsync();
        }

        /// <summary>
        /// Clears all equipped items from persistence
        /// </summary>
        public async UniTask ClearAllEquippedItemsAsync()
        {
            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            if (playerData == null)
            {
                Debug.LogError("PlayerData not available for clearing equipped items");
                return;
            }

            // Clear all equipped items
            playerData.Equipment.ClearAllEquippedItems();
            await IPlayerDataProvider.Instance.SaveAsync();
        }

        /// <summary>
        /// Validates that the player owns the items they have equipped
        /// Returns list of slot types that should be unequipped
        /// </summary>
        public async UniTask<List<SlotType>> ValidateEquippedItemOwnershipAsync()
        {
            var slotsToUnequip = new List<SlotType>();
            var playerData = await IPlayerDataProvider.Instance.GetAsync();
            
            if (playerData?.Equipment == null)
            {
                return slotsToUnequip;
            }

            foreach (var equippedEntry in playerData.Equipment.GetAllEquippedItems())
            {
                var slotType = (SlotType)equippedEntry.Key;
                var itemId = equippedEntry.Value;

                if (string.IsNullOrEmpty(itemId))
                    continue;

                // Check if item still exists in database
                var itemData = FindItemById(itemId);
                if (itemData == null)
                {
                    Debug.LogWarning($"Equipped item {itemId} no longer exists in database");
                    slotsToUnequip.Add(slotType);
                    continue;
                }

                // Check if player still owns the item
                if (!playerData.Equipment.OwnsEquipmentItem(itemId))
                {
                    Debug.LogWarning($"Player no longer owns equipped item {itemId}");
                    slotsToUnequip.Add(slotType);
                }
            }

            // Remove invalid items from persistence
            bool hasChanges = false;
            foreach (var slotType in slotsToUnequip)
            {
                playerData.Equipment.SetEquippedItem(slotType, null);
                hasChanges = true;
            }

            // Save changes if any were made
            if (hasChanges)
            {
                await IPlayerDataProvider.Instance.SaveAsync();
            }

            if (slotsToUnequip.Count > 0)
            {
                Debug.Log($"Found {slotsToUnequip.Count} invalid equipped items during validation");
            }

            return slotsToUnequip;
        }

        /// <summary>
        /// Finds an ItemData by its unique ID
        /// </summary>
        private ItemData FindItemById(string itemId)
        {
            if (_itemsDatabase?.Items == null || string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            return _itemsDatabase.Items.FirstOrDefault(item => item != null && item.Id == itemId);
        }
    }
}