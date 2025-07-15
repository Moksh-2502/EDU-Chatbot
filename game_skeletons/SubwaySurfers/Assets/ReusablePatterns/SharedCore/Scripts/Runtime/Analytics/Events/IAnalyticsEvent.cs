using System.Collections.Generic;

namespace SharedCore.Analytics
{
    /// <summary>
    /// Base interface for all analytics events.
    /// Events implementing this interface can be tracked through the analytics system.
    /// </summary>
    public interface IAnalyticsEvent
    {
        /// <summary>
        /// The name of the event as it will appear in analytics.
        /// </summary>
        string EventName { get; }

        /// <summary>
        /// The session id for the event.
        /// </summary>
        string SessionId { get; set; }
        
        /// <summary>
        /// Gets all properties for this event that will be sent to the analytics provider.
        /// </summary>
        /// <returns>Dictionary of property names and values to track</returns>
        Dictionary<string, object> GetProperties();
    }
} 