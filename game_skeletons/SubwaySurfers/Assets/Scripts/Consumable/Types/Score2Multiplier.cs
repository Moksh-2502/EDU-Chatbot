using System.Collections;
using Characters;
using Cysharp.Threading.Tasks;

public class Score2Multiplier : Consumable, IScoreMultiplier
{
    public override string GetConsumableName()
    {
        return "x2";
    }

    public override ConsumableType GetConsumableType()
    {
        return ConsumableType.SCORE_MULTIPLAYER;
    }

    public override int GetPrice()
    {
        return 750;
    }

	public override int GetPremiumCost()
	{
		return 0;
	}

    protected override async UniTask StartInternalAsync(CharacterInputController c)
    {
        await base.StartInternalAsync(c);
        IPlayerStateProvider.Instance.RegisterScoreMultiplier(this);
    }

    protected override void DoEnd(CharacterInputController c)
    {
        base.DoEnd(c);
        IPlayerStateProvider.Instance.UnRegisterScoreMultiplier(this);
    }

    public float GetMultiplier()
    {
        return 2;
    }
}
