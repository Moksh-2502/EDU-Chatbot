using UnityEngine;

namespace Consumables
{
    /// <summary>
    /// Base component for defining how consumables are processed when collected.
    /// Attach to consumable GameObjects to define their behavior.
    /// </summary>
    public abstract class ConsumableProcessor : MonoBehaviour
    {
        /// <summary>
        /// Process the consumption of this consumable by the player
        /// </summary>
        /// <param name="consumable">The consumable being consumed</param>
        /// <param name="player">The player collecting the consumable</param>
        public abstract void ProcessConsumption(Consumable consumable, CharacterInputController player);
        
        /// <summary>
        /// Check if this processor can process the consumable for the given player
        /// </summary>
        /// <param name="player">The player attempting to collect</param>
        /// <returns>True if consumption can be processed</returns>
        public virtual bool CanProcess(CharacterInputController player) => true;
    }
} 