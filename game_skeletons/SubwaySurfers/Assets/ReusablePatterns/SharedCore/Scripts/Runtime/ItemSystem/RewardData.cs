using System;
using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    [System.Serializable]
    public class RewardData
    {
        [field: SerializeField] public string Id { get; private set; } = Guid.NewGuid().ToString();
        [field: SerializeField] public ItemData[] Items { get; private set; } = Array.Empty<ItemData>();
    }
}