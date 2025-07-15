using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AIEduChatbot.UnityReactBridge.Handlers;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace SubwaySurfers.LeaderboardSystem
{
    public class LeaderboardService : ILeaderboardService
    {
        private IDictionary<string, string> GetPlayerLeaderboardMetadata()
        {
            if (IGameSessionProvider.Instance == null || IGameSessionProvider.Instance.UserData == null)
            {
                Debug.LogWarning($"Player session undefined, leaderboard metadata will not be set.");
                return null;
            }

            IDictionary<string, string> result = new Dictionary<string, string>(1);
            result[LeaderboardSystemConstants.PlayerNameMetadataKey] = IGameSessionProvider.Instance.UserData.name;
            return result;
        }

        public async UniTask<List<LeaderboardEntryWithLocalFlag>> GetTopEntriesWithLocalPlayerAsync(int topCount,
            string leaderboardId, CancellationToken cancellationToken)
        {
            try
            {
                // Get top entries and local player entry
                var topEntriesTask = LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions
                {
                    IncludeMetadata = true,
                    Limit = topCount
                }).AsUniTask();

                var localPlayerEntry = await GetOrCreateLocalPlayerEntryAsync(leaderboardId, cancellationToken);
                var topEntriesResponse = await topEntriesTask;
                cancellationToken.ThrowIfCancellationRequested();

                // Combine results
                return CombineTopEntriesWithLocalPlayer(topEntriesResponse.Results, localPlayerEntry, topCount);
            }
            catch (LeaderboardsValidationException ex)
            {
                Debug.LogError($"Leaderboards validation error: {ex.Message} (Code: {ex.ErrorCode})");
                return new List<LeaderboardEntryWithLocalFlag>();
            }
            catch (LeaderboardsException ex)
            {
                Debug.LogError($"Leaderboards API error: {ex.Message} (Code: {ex.ErrorCode})");
                return new List<LeaderboardEntryWithLocalFlag>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get leaderboard entries: {ex.Message}");
                return new List<LeaderboardEntryWithLocalFlag>();
            }
        }

        private async UniTask<LeaderboardEntry> GetOrCreateLocalPlayerEntryAsync(string leaderboardId, CancellationToken cancellationToken)
        {
            // Try to get existing local player entry
            LeaderboardEntry localPlayerEntry = null;
            try
            {
                localPlayerEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, new GetPlayerScoreOptions()
                {
                    IncludeMetadata = true,
                });
            }
            catch (LeaderboardsException ex) when (ex.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                // Player has no entry yet - this is expected, treat as null
                localPlayerEntry = null;
            }

            // If local player has no entry, create one with score 0
            if (localPlayerEntry == null)
            {
                Debug.Log("Local player has no leaderboard entry. Creating entry with score 0.");
                bool scoreSubmitted = await SubmitScoreAsync(leaderboardId, 0);
                cancellationToken.ThrowIfCancellationRequested();
                if (scoreSubmitted)
                {
                    // Get the newly created entry
                    localPlayerEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return localPlayerEntry;
        }

        private List<LeaderboardEntryWithLocalFlag> CombineTopEntriesWithLocalPlayer(
            IList<LeaderboardEntry> topEntries, LeaderboardEntry localPlayerEntry, int topCount)
        {
            var result = new List<LeaderboardEntryWithLocalFlag>();
            var topEntriesList = topEntries ?? new List<LeaderboardEntry>();

            // At this point, localPlayerEntry should always exist (either was already there or we created one with score 0)
            if (localPlayerEntry != null)
            {
                // Check if local player is already in top entries
                bool localPlayerInTop = topEntriesList.Any(e => e.PlayerId == localPlayerEntry.PlayerId);

                if (localPlayerInTop)
                {
                    // Mark local player entry and add all entries
                    foreach (var entry in topEntriesList)
                    {
                        bool isLocal = entry.PlayerId == localPlayerEntry.PlayerId;
                        result.Add(new LeaderboardEntryWithLocalFlag(entry, isLocal));
                    }
                }
                else
                {
                    // Local player not in top entries
                    if (topEntriesList.Count > 0)
                    {
                        // Add top (X-1) entries + local player as last
                        var entriesToAdd = topEntriesList.Take(topCount - 1);
                        foreach (var entry in entriesToAdd)
                        {
                            result.Add(new LeaderboardEntryWithLocalFlag(entry, false));
                        }

                        result.Add(new LeaderboardEntryWithLocalFlag(localPlayerEntry, true));
                    }
                    else
                    {
                        // Only local player available
                        result.Add(new LeaderboardEntryWithLocalFlag(localPlayerEntry, true));
                    }
                }
            }
            else
            {
                // Fallback: if we still don't have a local player entry, just add top entries
                Debug.LogWarning(
                    "Failed to ensure local player has a leaderboard entry. Showing only top entries.");
                foreach (var entry in topEntriesList)
                {
                    result.Add(new LeaderboardEntryWithLocalFlag(entry, false));
                }
            }

            return result;
        }

        public async UniTask<bool> SubmitScoreAsync(string leaderboardId, double score)
        {
            try
            {
                var playerMetadata = GetPlayerLeaderboardMetadata();
                await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, new AddPlayerScoreOptions()
                {
                    Metadata = playerMetadata,
                });
                Debug.Log($"Score {score} submitted successfully to leaderboard {leaderboardId}");
                return true;
            }
            catch (LeaderboardsRateLimitedException ex)
            {
                Debug.LogWarning($"Rate limited when submitting score: {ex.Message}");
                return false;
            }
            catch (LeaderboardsValidationException ex)
            {
                Debug.LogError($"Leaderboards validation error: {ex.Message} (Code: {ex.ErrorCode})");
                return false;
            }
            catch (LeaderboardsException ex)
            {
                Debug.LogError($"Leaderboards API error: {ex.Message} (Code: {ex.ErrorCode})");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to submit score: {ex.Message}");
                return false;
            }
        }

        public async UniTask<LeaderboardEntry> GetLocalPlayerEntryAsync(string leaderboardId)
        {
            try
            {
                return await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            }
            catch (LeaderboardsException ex)
            {
                Debug.LogWarning($"Failed to get local player entry: {ex.Message} (Code: {ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get local player entry: {ex.Message}");
            }

            return null;
        }
    }
}