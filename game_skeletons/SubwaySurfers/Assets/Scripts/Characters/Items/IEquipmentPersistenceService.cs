using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace Characters
{
    /// <summary>
    /// Pure data persistence interface - only handles saving/loading equipment data
    /// No knowledge of equipment logic or item instances
    /// </summary>
    public interface IEquipmentPersistenceService
    {
        /// <summary>
        /// Gets the saved equipped item data from persistence
        /// </summary>
        UniTask<Dictionary<SlotType, string>> GetEquippedItemDataAsync();
        
        /// <summary>
        /// Saves equipped item data to persistence
        /// </summary>
        UniTask SaveEquippedItemDataAsync(Dictionary<SlotType, string> equippedItems);
        
        /// <summary>
        /// Saves a single equipped item
        /// </summary>
        UniTask SaveEquippedItemAsync(SlotType slotType, string itemId);
        
        /// <summary>
        /// Removes an equipped item from persistence
        /// </summary>
        UniTask RemoveEquippedItemAsync(SlotType slotType);
        
        /// <summary>
        /// Clears all equipped items from persistence
        /// </summary>
        UniTask ClearAllEquippedItemsAsync();
        
        /// <summary>
        /// Validates that the player owns the items they have equipped
        /// Returns list of slot types that should be unequipped
        /// </summary>
        UniTask<List<SlotType>> ValidateEquippedItemOwnershipAsync();
    }
} 