using System;
using FluencySDK.Services;

namespace FluencySDK.Tests.Mocks
{
    /// <summary>
    /// Test implementation with controllable time for unit testing
    /// </summary>
    public class MockTimeProvider : ITimeProvider
    {
        private DateTime _currentTime;

        public MockTimeProvider(DateTime? startTime = null)
        {
            _currentTime = startTime ?? DateTime.Now;
        }

        public DateTime Now => _currentTime;
        public DateTime UtcNow => _currentTime.ToUniversalTime();

        /// <summary>
        /// Advance the mock time by the specified amount
        /// </summary>
        public void AdvanceTime(TimeSpan timeSpan)
        {
            _currentTime = _currentTime.Add(timeSpan);
        }

        /// <summary>
        /// Set the mock time to a specific value
        /// </summary>
        public void SetTime(DateTime time)
        {
            _currentTime = time;
        }
    }
}