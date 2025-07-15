using UnityEngine;
using SubwaySurfers.LeaderboardSystem;
using TMPro;

namespace SubwaySurfers.Scripts.UI
{
	public class LeaderboardEntryUIItem : MonoBehaviour
	{
		[Header("UI References")]
		public TMP_Text number;
		public TMP_Text playerName;
		public TMP_Text score;
		
		public GameObject localPlayerIndicator;

		private LeaderboardEntryWithLocalFlag _currentEntry;

		public void SetData(LeaderboardEntryWithLocalFlag entryWithFlag)
		{
			_currentEntry = entryWithFlag;
			
			if (entryWithFlag?.Entry == null)
			{
				SetVisible(false);
				return;
			}

			SetVisible(true);
			
			var entry = entryWithFlag.Entry;
			
			// Set text values (Unity SDK uses 0-based rank, so add 1 for display)
			if (number != null)
				number.text = (entry.Rank + 1).ToString();

			if (playerName != null)
				playerName.text = entryWithFlag.GetActualPlayerName();
			
			if (score != null)
				score.text = entry.Score.ToString("N0");

			// Apply visual styling for local player
			ApplyLocalPlayerStyling(entryWithFlag.IsLocalPlayer);
		}

		private void ApplyLocalPlayerStyling(bool isLocalPlayer)
		{
			if (localPlayerIndicator != null)
				localPlayerIndicator.SetActive(isLocalPlayer);
		}

		private void SetVisible(bool visible)
		{
			gameObject.SetActive(visible);
		}

		public LeaderboardEntryWithLocalFlag GetCurrentEntry()
		{
			return _currentEntry;
		}
	}
}