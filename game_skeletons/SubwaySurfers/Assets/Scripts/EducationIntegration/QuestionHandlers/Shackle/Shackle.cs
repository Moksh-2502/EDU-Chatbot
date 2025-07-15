namespace SubwaySurfers.Runtime
{
    public class Shackle : Consumable
    {
        public override string GetConsumableName()
        {
            return "Shackle";
        }

        public override ConsumableType GetConsumableType()
        {
            return ConsumableType.SHACKLE;
        }

        public override int GetPrice()
        {
            return 1000;
        }

        public override int GetPremiumCost()
        {
            return 3;
        }
    }
} 