using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace Characters
{
    /// <summary>
    /// Encapsulates all equipment-related data and operations
    /// Keeps equipment domain separate from general player data
    /// </summary>
    [System.Serializable]
    public class EquipmentState
    {
        /// <summary>
        /// Dictionary mapping SlotType to equipped ItemData ID. 
        /// Key: SlotType (converted to int for JSON serialization)
        /// Value: ItemData.Id (string)
        /// </summary>
        public Dictionary<int, string> equippedItems = new Dictionary<int, string>();
        
        /// <summary>
        /// List of owned equipment item IDs that the player has acquired
        /// </summary>
        public List<string> ownedEquipmentItems = new List<string>();

        [JsonConstructor]
        private EquipmentState()
        {
        }

        public static EquipmentState Create()
        {
            return new EquipmentState
            {
                equippedItems = new Dictionary<int, string>(),
                ownedEquipmentItems = new List<string>()
            };
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Initialize collections if they're null (for backward compatibility)
            if (equippedItems == null)
            {
                equippedItems = new Dictionary<int, string>();
            }
            
            if (ownedEquipmentItems == null)
            {
                ownedEquipmentItems = new List<string>();
            }
        }

        /// <summary>
        /// Gets the equipped item ID for a specific slot type
        /// </summary>
        public string GetEquippedItem(SlotType slotType)
        {
            return equippedItems.TryGetValue((int)slotType, out var itemId) ? itemId : null;
        }
        
        /// <summary>
        /// Sets the equipped item for a specific slot type
        /// </summary>
        public void SetEquippedItem(SlotType slotType, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                equippedItems.Remove((int)slotType);
            }
            else
            {
                equippedItems[(int)slotType] = itemId;
            }
        }
        
        /// <summary>
        /// Checks if the player owns a specific equipment item
        /// </summary>
        public bool OwnsEquipmentItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && ownedEquipmentItems.Contains(itemId);
        }
        
        /// <summary>
        /// Adds an equipment item to the player's inventory
        /// </summary>
        public void AddEquipmentItem(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId) && !ownedEquipmentItems.Contains(itemId))
            {
                ownedEquipmentItems.Add(itemId);
            }
        }
        
        /// <summary>
        /// Removes an equipment item from the player's inventory
        /// </summary>
        public void RemoveEquipmentItem(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                ownedEquipmentItems.Remove(itemId);
                
                // Also remove from equipped items if it was equipped
                var slotsToRemove = new List<int>();
                foreach (var kvp in equippedItems)
                {
                    if (kvp.Value == itemId)
                    {
                        slotsToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var slotKey in slotsToRemove)
                {
                    equippedItems.Remove(slotKey);
                }
            }
        }

        /// <summary>
        /// Gets all currently equipped items as a dictionary
        /// </summary>
        public Dictionary<SlotType, string> GetAllEquippedItems()
        {
            var result = new Dictionary<SlotType, string>();
            foreach (var kvp in equippedItems)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    result[(SlotType)kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Clears all equipped items
        /// </summary>
        public void ClearAllEquippedItems()
        {
            equippedItems.Clear();
        }

        /// <summary>
        /// Gets all owned equipment item IDs
        /// </summary>
        public List<string> GetOwnedEquipmentItems()
        {
            return new List<string>(ownedEquipmentItems);
        }
    }
} 