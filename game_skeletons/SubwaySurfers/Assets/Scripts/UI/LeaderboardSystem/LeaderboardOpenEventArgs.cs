using SubwaySurfers.LeaderboardSystem;

namespace SubwaySurfers.Scripts.UI
{
    public class LeaderboardOpenEventArgs : LeaderboardEventArgs
    {
        public bool Show { get; }

        public LeaderboardOpenEventArgs(string leaderboardId, bool show)
            : base(leaderboardId)
        {
            Show = show;
        }
    }
}