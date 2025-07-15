using UnityEngine;
using EducationIntegration.QuestionHandlers;

namespace Consumables
{
    /// <summary>
    /// Processor that gates consumable consumption behind a question.
    /// Used by the PowerupQuestionHandler to create educational powerups.
    /// </summary>
    public class QuestionGatedConsumableProcessor : ConsumableProcessor
    {
        private ILootableQuestionHandler<Consumable> _lootableQuestionHandler; 
        /// <summary>
        /// Set the question handler that will process this consumable
        /// </summary>
        /// <param name="handler">The PowerupQuestionHandler instance</param>
        public void SetQuestionHandler(ILootableQuestionHandler<Consumable> handler)
        {
            _lootableQuestionHandler = handler;
        }
        
        public override void ProcessConsumption(Consumable consumable, CharacterInputController player)
        {
            if (_lootableQuestionHandler != null)
            {
                _lootableQuestionHandler.HandleLootableCollection(consumable);
            }
            else
            {
                Debug.LogWarning("[QuestionGatedConsumableProcessor] No question handler assigned, falling back to immediate consumption");
                player.UseConsumable(consumable);
            }
        }
        
        public override bool CanProcess(CharacterInputController player)
        {
            return _lootableQuestionHandler != null && _lootableQuestionHandler.CanProcessLootables();
        }
    }
} 