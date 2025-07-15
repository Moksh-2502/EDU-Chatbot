using Cysharp.Threading.Tasks;

namespace Consumables
{
    public class Shield : Consumable
    {
        public override string GetConsumableName()
        {
            return "Shield";
        }

        public override ConsumableType GetConsumableType()
        {
            return ConsumableType.SHIELD;
        }
    
        public override int GetPrice()
        {
            return 1000;
        }
    
        public override int GetPremiumCost()
        {
            return 4;
        }

        protected override async UniTask StartInternalAsync(CharacterInputController c)
        {
            await base.StartInternalAsync(c);
            c.characterCollider.SetShielded(true);
        }
    
        protected override void DoEnd(CharacterInputController c)
        {
            base.DoEnd(c);
            c.characterCollider.SetShielded(false);
        }

        protected override void TickInternal(CharacterInputController c)
        {
            base.TickInternal(c);
            c.characterCollider.SetShielded(true);
        }
    }
}