using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem.UI
{
    public class ItemCard : MonoBehaviour
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemNameText;
        public ItemData ItemData { get; private set; }

        public void Repaint(ItemData itemData)
        {
            this.ItemData = itemData;
            // Set item name
            if (itemNameText != null)
                itemNameText.text = string.IsNullOrEmpty(ItemData.Name) ? "Unknown Item" : ItemData.Name;

            // For simplicity, we'll use a placeholder icon for now
            // In a full implementation, this would load from ItemData.Item addressable
            if (itemIcon != null)
            {
                itemIcon.overrideSprite = ItemData.Icon;
            }
        }
    }
}