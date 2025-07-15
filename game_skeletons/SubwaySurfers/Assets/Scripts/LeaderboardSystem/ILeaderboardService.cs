using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Leaderboards.Models;

namespace SubwaySurfers.LeaderboardSystem
{
    /// <summary>
    /// Extended leaderboard service that adds smart local player inclusion logic
    /// on top of Unity's native leaderboard service
    /// </summary>
    public interface ILeaderboardService
    {
        static ILeaderboardService Instance { get; } = new LeaderboardService();

        /// <summary>
        /// Gets the top X leaderboard entries, always including the local player's entry.
        /// If the local player is not in the top X, the last entry will be replaced with the local player's entry.
        /// </summary>
        /// <param name="topCount">Number of top entries to fetch</param>
        /// <param name="leaderboardId">ID of the leaderboard to query</param>
        /// <returns>List of Unity SDK LeaderboardEntry objects with local player guaranteed to be included</returns>
        UniTask<List<LeaderboardEntryWithLocalFlag>> GetTopEntriesWithLocalPlayerAsync(int topCount, string leaderboardId, CancellationToken cancellationToken);

        /// <summary>
        /// Submits a score for the local player to the specified leaderboard
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard</param>
        /// <param name="score">Score to submit</param>
        /// <returns>True if submission was successful</returns>
        UniTask<bool> SubmitScoreAsync(string leaderboardId, double score);

        /// <summary>
        /// Gets the local player's current leaderboard entry
        /// </summary>
        /// <param name="leaderboardId">ID of the leaderboard to query</param>
        /// <returns>Local player's leaderboard entry or null if not found</returns>
        UniTask<LeaderboardEntry> GetLocalPlayerEntryAsync(string leaderboardId);
    }
} 