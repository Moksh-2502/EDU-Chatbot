using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.Models;
using UnityEngine;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// UI-specific data structure for fact set row display
    /// </summary>
    public class FactSetUIItemInfo
    {
        public FactSetProgress Data { get; }
        public Sprite Icon { get; set; }

        public FactSetUIItemInfo(FactSetProgress data, Sprite icon)
        {
            Data = data;
            Icon = icon;
        }
    }
}