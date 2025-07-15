using System.Collections;
using Characters;
using Cysharp.Threading.Tasks;

public class ExtraLife : Consumable
{
    protected const int k_CoinValue = 10;

    public override string GetConsumableName()
    {
        return "Life";
    }

    public override ConsumableType GetConsumableType()
    {
        return ConsumableType.EXTRALIFE;
    }

    public override int GetPrice()
    {
        return 2000;
    }

    public override int GetPremiumCost()
    {
        return 5;
    }

    public override bool CanBeUsed(CharacterInputController c)
    {
        return IPlayerStateProvider.Instance.CanGainLives();
    }

    protected override async UniTask StartInternalAsync(CharacterInputController c)
    {
        await base.StartInternalAsync(c);
        if (IPlayerStateProvider.Instance.CanGainLives())
        {
            IPlayerStateProvider.Instance.ChangeLives(1);
        }
        else
        {
            IPlayerStateProvider.Instance.ProcessPickedCoins(k_CoinValue);
        }
    }
}