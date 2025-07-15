using UnityEngine;
using TMPro;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Specialized preview UI for Character objects
    /// Handles character-specific UI elements like name, accessory info, and character icon
    /// </summary>
    public class CharacterPreviewUI : BasePreviewUI<CharacterPreviewData, Character>
    {
        [Header("Character UI Components")]
        [SerializeField] private TMP_Text characterNameText;


        public System.Action<int> OnAccessoryChangeRequested;


        protected override string GetTitle(CharacterPreviewData previewData)
        {
            return previewData?.CharacterDisplayName ?? "Unknown Character";
        }

        protected override string GetDescription(CharacterPreviewData previewData)
        {
            return "";
        }

        protected override void OnUpdateDisplay(CharacterPreviewData previewData)
        {
            UpdateCharacterInfo();
        }

        private void UpdateCharacterInfo()
        {
            // Update character name
            if (characterNameText != null)
            {
                characterNameText.text = CurrentPreviewData.CharacterDisplayName;
            }
        }


        protected override void OnShown()
        {
            // Character preview specific show logic
        }

        protected override void OnHidden()
        {
            // Character preview specific hide logic
        }
    }
} 