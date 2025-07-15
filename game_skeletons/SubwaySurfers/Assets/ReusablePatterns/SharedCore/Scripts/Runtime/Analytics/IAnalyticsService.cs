namespace SharedCore.Analytics
{
    /// <summary>
    /// Main interface for analytics services.
    /// Defines the contract for tracking analytics events across different analytics services.
    /// </summary>
    public interface IAnalyticsService
    {
        // TODO: add vcontainer and inject this instead of singletons
        static IAnalyticsService Instance { get; set; }
        /// <summary>
        /// Tracks an analytics event.
        /// </summary>
        /// <param name="analyticsEvent">The event to track</param>
        void TrackEvent(IAnalyticsEvent analyticsEvent);
    }
} 