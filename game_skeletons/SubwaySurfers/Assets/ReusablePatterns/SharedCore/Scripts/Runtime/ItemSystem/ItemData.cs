using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    [CreateAssetMenu(fileName = "ItemData.asset", menuName = "Trash Dash/Item Data")]
    public class ItemData : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; } = Guid.NewGuid().ToString();
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public ItemType ItemType { get; private set; }
        [field: SerializeField] public SlotType EquipSlot { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public AssetReference Item { get; private set; }

        public bool IsValid()
        {
            return string.IsNullOrWhiteSpace(Id) == false && string.IsNullOrWhiteSpace(Name) == false
            && Item != null;
        }
    }
}