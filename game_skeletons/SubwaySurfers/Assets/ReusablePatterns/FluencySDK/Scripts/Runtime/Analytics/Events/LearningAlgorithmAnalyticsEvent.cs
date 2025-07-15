using System.Collections.Generic;
using SharedCore.Analytics;

namespace FluencySDK.Analytics
{
    /// <summary>
    /// Wrapper to adapt ILearningAlgorithmEvent to IAnalyticsEvent
    /// </summary>
    public class LearningAlgorithmAnalyticsEvent : BaseAnalyticsEvent
    {
        private readonly ILearningAlgorithmEvent _algorithmEvent;

        public override string EventName => _algorithmEvent.EventName;

        public LearningAlgorithmAnalyticsEvent(ILearningAlgorithmEvent algorithmEvent)
        {
            _algorithmEvent = algorithmEvent;
        }

        public override Dictionary<string, object> GetProperties()
        {
            var properties = base.GetProperties();
            var algorithmProperties = _algorithmEvent.ToAnalyticsData();
            foreach (var kvp in algorithmProperties)
            {
                properties[kvp.Key] = kvp.Value;
            }
            
            return properties;
        }
    }
} 