using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Characters;
using Newtonsoft.Json;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using Sounds;
using Random = UnityEngine.Random;

namespace Data
{
	/// <summary>
	/// Save data for the game. This is stored locally in this case, but a "better" way to do it would be to store it on a server
	/// somewhere to avoid player tampering with it. Here potentially a player could modify the binary file to add premium currency.
	/// </summary>
	public class PlayerData
	{
		private const string DefaultCharacter = "Trash Cat";
		private const string DefaultTheme = "Day";
    
		public int coins;
		public int premium;
		public Dictionary<Consumable.ConsumableType, int> consumables = new Dictionary<Consumable.ConsumableType, int>();   // Inventory of owned consumables and quantity.

		public List<string> characters = new List<string>();    // Inventory of characters owned.
		public int usedCharacter;                               // Currently equipped character.
		public int usedAccessory = -1;
		public List<string> characterAccessories = new List<string>();  // List of owned accessories, in the form "charName:accessoryName".
		
		/// <summary>
		/// Encapsulates all equipment-related data and operations
		/// </summary>
		public EquipmentState Equipment { get; set; } = EquipmentState.Create();
		
		public List<string> themes = new List<string>();                // Owned themes.
		public int usedTheme;                                           // Currently used theme.
		// ignore for now.
		[JsonIgnore]
		public List<MissionBase> missions = new List<MissionBase>();

		public string LastCompletedTutorialVersion { get; set; }

		public List<AudioMixerData> audioData = new List<AudioMixerData>(4);

		//ftue = First Time User Expeerience. This var is used to track thing a player do for the first time. It increment everytime the user do one of the step
		//e.g. it will increment to 1 when they click Start, to 2 when doing the first run, 3 when running at least 300m etc.
		public int ftueLevel = 0;
		//Player win a rank ever 300m (e.g. a player having reached 1200m at least once will be rank 4)
		public int rank = 0;
		/// <summary>
		/// This is used to track the status of rewards claimed.
		/// The key is the reward id (can be the fact set, an action, a mission or whatever), the value is a boolean indicating if the reward has been claimed.
		/// </summary>
		public IDictionary<string, bool> RewardsClaimStatus { get; set; } = new Dictionary<string, bool>();

		public Guid RuntimeId { get; private set; }

		[JsonConstructor]
		private PlayerData()
		{
			RuntimeId = Guid.NewGuid();
		}
    
		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			// This is called after deserialization, so we can check the missions count.
			CheckMissionsCount();
			
			// Initialize equipment state if it's null (for backward compatibility)
			if (Equipment == null)
			{
				Equipment = EquipmentState.Create();
			}

			RuntimeId = Guid.NewGuid();
		}

		// Mission management

		// Will add missions until we reach 2 missions.
		public void CheckMissionsCount()
		{
			while (missions.Count < 2)
				AddMission();
		}

		public void AddMission()
		{
			int val = Random.Range(0, (int)MissionBase.MissionType.MAX);
        
			MissionBase newMission = MissionBase.GetNewMissionFromType((MissionBase.MissionType)val);
			newMission.Created();

			missions.Add(newMission);
		}

		public void StartRunMissions(TrackManager manager)
		{
			for(int i = 0; i < missions.Count; ++i)
			{
				missions[i].RunStart(manager);
			}
		}

		public void UpdateMissions(TrackManager manager)
		{
			for(int i = 0; i < missions.Count; ++i)
			{
				missions[i].Update(manager);
			}
		}

		public bool AnyMissionComplete()
		{
			for (int i = 0; i < missions.Count; ++i)
			{
				if (missions[i].isComplete) return true;
			}

			return false;
		}

		// High Score management

		public static PlayerData Create()
		{
			PlayerData data = new PlayerData();
			data.characters.Clear();
			data.themes.Clear();
			data.missions.Clear();
			data.characterAccessories.Clear();
			data.consumables.Clear();
			data.Equipment = EquipmentState.Create();

			data.usedCharacter = 0;
			data.usedTheme = 0;
			data.usedAccessory = -1;

			data.coins = 0;
			data.premium = 0;

			data.characters.Add(DefaultCharacter);
			data.themes.Add(DefaultTheme);

			data.ftueLevel = 0;
			data.rank = 0;

			data.CheckMissionsCount();
			return data;
		}
		

	}
}