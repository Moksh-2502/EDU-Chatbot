namespace Consumables
{
    /// <summary>
    /// Processor that immediately applies the consumable effect to the player.
    /// This is the default behavior for normal gameplay powerups.
    /// </summary>
    public class ImmediateConsumableProcessor : ConsumableProcessor
    {
        public override void ProcessConsumption(Consumable consumable, CharacterInputController player)
        {
            if (consumable != null && player != null)
            {
                player.UseConsumable(consumable);
            }
        }
    }
} 