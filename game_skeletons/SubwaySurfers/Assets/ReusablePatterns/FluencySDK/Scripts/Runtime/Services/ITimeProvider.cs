using System;

namespace FluencySDK.Services
{
    /// <summary>
    /// Time provider interface for dependency injection and testing
    /// </summary>
    public interface ITimeProvider
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Production implementation using system time
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}