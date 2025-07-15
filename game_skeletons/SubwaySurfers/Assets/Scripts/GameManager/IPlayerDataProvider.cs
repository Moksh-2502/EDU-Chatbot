using Cysharp.Threading.Tasks;
using Data;

namespace SubwaySurfers
{
    public interface IPlayerDataProvider
    {
        static IPlayerDataProvider Instance { get; private set; } = new PlayerDataProvider();
        UniTask<PlayerData> GetAsync();
        UniTask SaveAsync();
        UniTask ClaimMissionAsync(MissionBase mission);
        UniTask AddConsumableAsync(Consumable.ConsumableType consumable);
        UniTask BuyConsumableAsync(Consumable consumable);
        UniTask ConsumeConsumableAsync(Consumable.ConsumableType consumable);

        UniTask SetLastCompletedTutorialVersionAsync(string version);
        UniTask SetFTUELevelAsync(int level);
        
        UniTask AddCoinsAsync(int coins);
        UniTask AddPremiumAsync(int premium);
        UniTask ChangeThemeAsync(int direction);
        UniTask ChangeCharacterAsync(int direction);
        UniTask BuyCharacterAsync(Character character);
        UniTask AddThemeAsync(ThemeData themeId);
        UniTask AddMissionAsync();
        UniTask BuyAccessoryAsync(string name, int cost, int premiumCost);
        UniTask SaveVolumeAsync(string name, float volume);
        UniTask ResetAsync();
        

    }
}