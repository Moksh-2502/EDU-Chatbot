using System.Collections;
using Cysharp.Threading.Tasks;

namespace Consumables
{
    public class Invincibility : Consumable
    {
        public override string GetConsumableName()
        {
            return "Invincible";
        }

        public override ConsumableType GetConsumableType()
        {
            return ConsumableType.INVINCIBILITY;
        }

        public override int GetPrice()
        {
            return 1500;
        }

        public override int GetPremiumCost()
        {
            return 5;
        }

        protected override void TickInternal(CharacterInputController c)
        {
            base.TickInternal(c);
            c.characterCollider.SetInvincibleExplicit(true);
        }

        protected override async UniTask StartInternalAsync(CharacterInputController c)
        {
            await base.StartInternalAsync(c);
            c.characterCollider.SetInvincible(duration);
        }

        protected override void DoEnd(CharacterInputController c)
        {
            base.DoEnd(c);
            c.characterCollider.SetInvincibleExplicit(false);
        }
    }
}