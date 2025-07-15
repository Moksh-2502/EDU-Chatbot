using System.Collections.Generic;

namespace FluencySDK
{
    /// <summary>
    /// Generic learning algorithm event that can be emitted by any learning algorithm
    /// and transformed to key-value pairs for analytics services like Mixpanel
    /// </summary>
    public interface ILearningAlgorithmEvent
    {
        string EventName { get; }
        Dictionary<string, object> ToAnalyticsData();
    }
} 