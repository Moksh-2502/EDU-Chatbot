using System;

namespace SharedCore.Analytics
{
    /// <summary>
    /// Interface for analytics event senders that can be automatically discovered and initialized.
    /// Implementing classes will be found via reflection and initialized by the AnalyticsManager.
    /// </summary>
    public interface IAnalyticsEventSender : IDisposable
    {
        /// <summary>
        /// Initialize the sender with necessary dependencies and register event handlers.
        /// Called automatically by the AnalyticsManager during startup.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Gets the priority of this sender for initialization order.
        /// Lower numbers are initialized first. Default should be 0.
        /// </summary>
        int InitializationPriority { get; }
        
        /// <summary>
        /// Gets whether this sender is currently active and should be processing events.
        /// </summary>
        bool IsActive { get; }
    }
} 