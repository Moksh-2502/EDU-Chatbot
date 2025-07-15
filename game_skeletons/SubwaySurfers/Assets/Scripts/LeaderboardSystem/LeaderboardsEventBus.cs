using System;

namespace SubwaySurfers.LeaderboardSystem
{
    public static class LeaderboardsEventBus
    {
        public static event Action<LeaderboardEventArgs> OnLeaderboardEvent;

        public static void RaiseLeaderboardEvent(LeaderboardEventArgs args)
        {
            OnLeaderboardEvent?.Invoke(args);
        }
    }
}