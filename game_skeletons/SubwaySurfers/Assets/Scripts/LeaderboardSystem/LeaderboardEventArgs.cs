using System;

namespace SubwaySurfers.LeaderboardSystem
{
    public abstract class LeaderboardEventArgs : EventArgs
    {
        public string LeaderboardId { get; }
        
        protected LeaderboardEventArgs(string leaderboardId)
        {
            LeaderboardId = leaderboardId ?? throw new ArgumentNullException(nameof(leaderboardId), "Leaderboard ID cannot be null");
        }
    }
}