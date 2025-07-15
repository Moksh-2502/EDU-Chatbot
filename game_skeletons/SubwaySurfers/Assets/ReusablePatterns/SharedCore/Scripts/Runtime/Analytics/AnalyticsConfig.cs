using UnityEngine;

namespace SharedCore.Analytics
{
    /// <summary>
    /// Configuration settings for the analytics system.
    /// This ScriptableObject can be created in the Unity Editor and configured through the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "ReusablePatterns/Analytics/Analytics Config")]
    public class AnalyticsConfig : ScriptableObject
    {
        [field: SerializeField] public bool TrackingEnabled { get; private set; } = true;
        [field: SerializeField] public string[] TrackableEnvironments { get; private set; }
        [field: SerializeField] public string[] TrackableBuildIds { get; private set; }
    }
} 