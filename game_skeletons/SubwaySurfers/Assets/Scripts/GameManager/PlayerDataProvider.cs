using System.Collections.Generic;
using AIEduChatbot.UnityReactBridge.Storage;
using Cysharp.Threading.Tasks;
using Data;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using Sounds;
using UnityEngine;

namespace SubwaySurfers
{
    public class PlayerDataProvider : IPlayerDataProvider, IRewardsSaver
    {
        private const string k_PlayerDataKey = "PlayerData";
        public async UniTask<PlayerData> GetAsync()
        {
            var data = await IGameStorageService.Instance.LoadAsync<PlayerData>(k_PlayerDataKey);
            if (data == null)
            {
                data = PlayerData.Create();
                await IGameStorageService.Instance.SetAsync(k_PlayerDataKey, data);
            }

            return data;
        }

        public UniTask SaveAsync() => IGameStorageService.Instance.SaveAsync(k_PlayerDataKey);

        public async UniTask ClaimMissionAsync(MissionBase mission)
        {
            var data = await GetAsync();
            data.premium += mission.reward;

            data.missions.Remove(mission);

            data.CheckMissionsCount();
            await SaveAsync();
        }

        public async UniTask AddConsumableAsync(Consumable.ConsumableType type)
        {
            var data = await GetAsync();
            data.consumables.TryAdd(type, 0);

            data.consumables[type] += 1;
            await SaveAsync();
        }

        public async UniTask BuyConsumableAsync(Consumable consumable)
        {
            var data = await GetAsync();
            if (data.coins < consumable.GetPrice() || data.premium < consumable.GetPremiumCost())
            {
                Debug.LogWarning("Not enough coins or premium to buy the consumable.");
                return;
            }
            
            data.coins -= consumable.GetPrice();
            data.premium -= consumable.GetPremiumCost();
            await AddConsumableAsync(consumable.GetConsumableType());
        }

        public async UniTask ConsumeConsumableAsync(Consumable.ConsumableType type)
        {
            var data = await GetAsync();
            if (!data.consumables.ContainsKey(type))
                return;

            data.consumables[type] -= 1;
            if (data.consumables[type] == 0)
            {
                data.consumables.Remove(type);
            }

            await SaveAsync();
        }

        public async UniTask SetLastCompletedTutorialVersionAsync(string version)
        {
            var data = await GetAsync();
            data.LastCompletedTutorialVersion = version;
            await SaveAsync();
        }

        public async UniTask SetFTUELevelAsync(int level)
        {
            var data = await GetAsync();
            data.ftueLevel = level;
            await SaveAsync();
        }

        public async UniTask ChangeThemeAsync(int direction)
        {
            var data = await GetAsync();
            data.usedTheme += direction;
            if (data.usedTheme >= data.themes.Count)
                data.usedTheme = 0;
            else if (data.usedTheme < 0)
                data.usedTheme = data.themes.Count - 1;
            await SaveAsync();
        }

        public async UniTask AddCoinsAsync(int coins)
        {
            var data = await GetAsync();
            data.coins += coins;
            await SaveAsync();
        }

        public async UniTask AddPremiumAsync(int premium)
        {
            var data = await GetAsync();
            data.premium += premium;
            await SaveAsync();
        }

        public async UniTask ChangeCharacterAsync(int direction)
        {
            var data = await GetAsync();
            data.usedCharacter += direction;
            if (data.usedCharacter >= data.characters.Count)
                data.usedCharacter = 0;
            else if (data.usedCharacter < 0)
                data.usedCharacter = data.characters.Count - 1;
            await SaveAsync();
        }

        public async UniTask BuyCharacterAsync(Character character)
        {
            var data = await GetAsync();
            if (data.characters.Contains(character.characterName))
                return;
            if(data.coins < character.cost || data.premium < character.premiumCost)
            {
                Debug.LogWarning("Not enough coins or premium to buy the character.");
                return;
            }
            data.coins -= character.cost;
            data.premium -= character.premiumCost;
            data.characters.Add(character.characterName);
            await SaveAsync();
        }

        public async UniTask AddThemeAsync(ThemeData theme)
        {
            var data = await GetAsync();
            if (data.themes.Contains(theme.themeName))
                return;
            if(data.coins < theme.cost || data.premium < theme.premiumCost)
            {
                Debug.LogWarning("Not enough coins or premium to buy the theme.");
                return;
            }
            data.coins -= theme.cost;
            data.premium -= theme.premiumCost;
            data.themes.Add(theme.themeName);
            await SaveAsync();
        }

        public async UniTask AddMissionAsync()
        {
            var data = await GetAsync();
            data.AddMission();
            await SaveAsync();
        }

        public async UniTask BuyAccessoryAsync(string name, int cost, int premiumCost)
        {
            var data = await GetAsync();
            if (data.coins < cost || data.premium < premiumCost)
            {
                Debug.LogWarning("Not enough coins or premium to buy the accessory.");
                return;
            }
            data.coins -= cost;
            data.premium -= premiumCost;
            data.characterAccessories.Add(name);
            await SaveAsync();
        }

        public UniTask ResetAsync() => IGameStorageService.Instance.DeleteAsync(k_PlayerDataKey);

        public async UniTask SaveVolumeAsync(string name, float volume)
        {
            var data = await GetAsync();
            if (data.audioData == null)
            {
                data.audioData = new System.Collections.Generic.List<AudioMixerData>();
            }
            
            var mixerData = data.audioData.Find(o => o.name == name);
            if (mixerData == null)
            {
                mixerData = new AudioMixerData { name = name, volume = volume };
                data.audioData.Add(mixerData);
            }
            else
            {
                mixerData.volume = volume;
            }
            
            await SaveAsync();
        }

        public async UniTask<bool> GetIsRewardClaimedAsync(string id)
        {
            var data = await GetAsync();
            if (data.RewardsClaimStatus == null)
            {
                            return false;
            }

            if (data.RewardsClaimStatus.TryGetValue(id, out var isClaimed))
            {
                return isClaimed;
            }

            return false;
        }

        public async UniTask SetIsRewardClaimedAsync(string id, bool isClaimed)
        {
            var data = await GetAsync();
            if (data.RewardsClaimStatus == null)
            {
                data.RewardsClaimStatus = new Dictionary<string, bool>();
            }

            data.RewardsClaimStatus[id] = isClaimed;
            await SaveAsync();
        }
    }
}