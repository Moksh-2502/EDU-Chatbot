using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using UnityEngine;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Preview data wrapper for Character objects
    /// Contains all necessary information for previewing a character
    /// </summary>
    public class CharacterPreviewData : IPreviewData<Character>
    {
        public Character Data { get; private set; }
        public PreviwableAssetAddress AssetAddress { get; private set; }
        public TransformData? TransformData {get; private set;}
        public string CharacterDisplayName { get; private set; }
        public Sprite CharacterIcon { get; private set; }

        private CharacterPreviewData() { }

        public static CharacterPreviewData Create(
            Character character,
            TransformData? customTransformData = null)
        {
            if (character == null)
            {
                Debug.LogError("CharacterPreviewData: Cannot create preview data with null character");
                return null;
            }

            return new CharacterPreviewData
            {
                Data = character,
                AssetAddress = PreviwableAssetAddress.FromAssetAddress(character.characterName),
                TransformData = customTransformData,
                CharacterDisplayName = character.characterName,
                CharacterIcon = character.icon
            };
        }

        public static CharacterPreviewData CreateFromCharacterName(
            string characterName,
            TransformData? customTransformData = null)
        {
            var character = CharacterDatabase.GetCharacter(characterName);
            if (character == null)
            {
                Debug.LogError($"CharacterPreviewData: Character '{characterName}' not found in database");
                return null;
            }

            return Create(character, customTransformData);
        }
    }
} 