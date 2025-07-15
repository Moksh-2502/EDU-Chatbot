using System;
using System.Collections.Generic;
using System.Linq;
using AIEduChatbot.UnityReactBridge.Handlers;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Storage
{
    /// <summary>
    /// Storage cache implementation with improved consistency handling
    /// </summary>
    public class StorageCache : IStorageCache
    {
        private readonly IGameSessionProvider _sessionProvider;
        private readonly IDictionary<string, object> _data = new Dictionary<string, object>();

        public StorageCache(IGameSessionProvider sessionProvider)
        {
            _sessionProvider = sessionProvider;
        }

        private string GetFullCacheKey(string dataKey)
        {
            return $"{_sessionProvider.SessionId}_{dataKey}";
        }

        public (bool exists, T data) TryGetCachedData<T>(string dataKey)
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return default;
            }

            try
            {
                if (_data.TryGetValue(GetFullCacheKey(dataKey), out var data) && data != null)
                {
                    return (true, (T)data);
                }
            }
            catch (Exception ex)
            {
                StorageLogger.LogException(ex, $"Exception trying to get cached data of type {typeof(T).Name} and key {dataKey}");
            }

            return default;
        }

        public void SetCachedData<T>(string dataKey, T data)
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return;
            }

            _data[GetFullCacheKey(dataKey)] = data;
        }

        public void Remove(string dataKey)
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return;
            }

            _data.Remove(GetFullCacheKey(dataKey));
        }

        public bool Exists(string dataKey)
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return false;
            }

            return _data.ContainsKey(GetFullCacheKey(dataKey));
        }

        /// <summary>
        /// Gets all cached keys for the current session
        /// </summary>
        /// <returns>List of data keys (without session prefix)</returns>
        public IReadOnlyList<string> GetAllCachedKeys()
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return new List<string>();
            }

            var sessionPrefix = $"{_sessionProvider.SessionId}_";
            return _data.Keys
                .Where(key => key.StartsWith(sessionPrefix))
                .Select(key => key.Substring(sessionPrefix.Length))
                .ToList();
        }

        /// <summary>
        /// Clears all cached data for the current session
        /// </summary>
        public void ClearSession()
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return;
            }

            var sessionPrefix = $"{_sessionProvider.SessionId}_";
            var keysToRemove = _data.Keys
                .Where(key => key.StartsWith(sessionPrefix))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _data.Remove(key);
            }
        }

        /// <summary>
        /// Gets the count of cached items for the current session
        /// </summary>
        /// <returns>Number of cached items</returns>
        public int GetCachedItemCount()
        {
            if (string.IsNullOrWhiteSpace(_sessionProvider.SessionId))
            {
                return 0;
            }

            var sessionPrefix = $"{_sessionProvider.SessionId}_";
            return _data.Keys.Count(key => key.StartsWith(sessionPrefix));
        }
    }
}