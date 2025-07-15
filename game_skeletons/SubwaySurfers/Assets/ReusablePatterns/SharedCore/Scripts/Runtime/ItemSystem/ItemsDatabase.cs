using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem
{
    [CreateAssetMenu(fileName = "ItemsDatabase.asset", menuName = "Trash Dash/New Items Database")]
    public class ItemsDatabase : ScriptableObject
    {
        [field: SerializeField] public ItemData[] Items { get; private set; }
    }
}