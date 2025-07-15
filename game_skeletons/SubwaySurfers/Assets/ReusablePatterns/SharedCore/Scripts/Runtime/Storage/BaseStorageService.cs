using System;
using System.Collections.Generic;
using System.Linq;
using AIEduChatbot.UnityReactBridge.Storage;
using Cysharp.Threading.Tasks;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Storage
{
    /// <summary>
    /// Abstract base class for storage services that centralizes cache logic and exception handling
    /// </summary>
    public abstract class BaseStorageService : IGameStorageService
    {
        private readonly IStorageCache _cache;
        protected abstract string ServiceName { get; }
        
        // Track which keys have been modified in cache but not yet saved to storage
        private readonly HashSet<string> _dirtyKeys = new();

        protected BaseStorageService(IStorageCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public virtual async UniTask<T> LoadAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StorageLogger.LogError($"Load operation failed: Key cannot be null or whitespace", new { ServiceType = ServiceName });
                return default(T);
            }

            try
            {
                // First check cache
                var cacheResult = _cache.TryGetCachedData<T>(key);
                if (cacheResult.exists)
                {
                    StorageLogger.LogInfo($"Data loaded from cache for key: {key}", new { ServiceType = ServiceName, DataType = typeof(T).Name });
                    return cacheResult.data;
                }

                // Load from storage if not in cache
                var storageData = await LoadFromStorageAsync<T>(key);
                if (storageData != null && !EqualityComparer<T>.Default.Equals(storageData, default(T)))
                {
                    // Cache the loaded data
                    _cache.SetCachedData(key, storageData);
                    StorageLogger.LogInfo($"Data loaded from storage and cached for key: {key}", new { ServiceType = ServiceName, DataType = typeof(T).Name });
                    return storageData;
                }

                StorageLogger.LogInfo($"No data found for key: {key}", new { ServiceType = ServiceName, DataType = typeof(T).Name });
                return default(T);
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "Load", key, ServiceName, new { DataType = typeof(T).Name });
                return default(T);
            }
        }

        public virtual async UniTask SetAsync<T>(string key, T data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StorageLogger.LogError($"Set operation failed: Key cannot be null or whitespace", new { ServiceType = ServiceName, DataType = typeof(T).Name });
                return;
            }

            try
            {
                // Update cache first
                _cache.SetCachedData(key, data);
                
                MarkKeyAsDirty(key);

                // Save to storage
                await SaveToStorageAsync(key, data);
                
                MarkKeyAsClean(key);

                StorageLogger.LogInfo($"Data set successfully for key: {key}", new { ServiceType = ServiceName, DataType = typeof(T).Name });
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "Set", key, ServiceName, new { DataType = typeof(T).Name });
                throw; // Re-throw to let caller handle the error
            }
        }

        public virtual async UniTask<bool> SaveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StorageLogger.LogError($"Save operation failed: Key cannot be null or whitespace", new { ServiceType = ServiceName });
                return false;
            }

            try
            {
                // Check if key exists in cache
                if (!_cache.Exists(key))
                {
                    StorageLogger.LogWarning($"Save operation skipped: Key not found in cache: {key}", new { ServiceType = ServiceName });
                    return false;
                }

                // Get the cached data
                var cacheResult = _cache.TryGetCachedData<object>(key);
                if (!cacheResult.exists)
                {
                    StorageLogger.LogWarning($"Save operation failed: Unable to retrieve cached data for key: {key}", new { ServiceType = ServiceName });
                    return false;
                }

                // Save to storage
                await SaveToStorageAsync(key, cacheResult.data);
                
                MarkKeyAsClean(key);

                StorageLogger.LogInfo($"Data saved successfully for key: {key}", new { ServiceType = ServiceName });
                return true;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "Save", key, ServiceName);
                return false;
            }
        }

        public virtual async UniTask<bool> SaveAllAsync()
        {
            try
            {
                var success = true;
                
                // Get a snapshot of dirty keys
                var keysToSave = GetDirtyKeys();

                if (keysToSave.Count == 0)
                {
                    StorageLogger.LogInfo("SaveAll operation completed: No dirty keys to save", new { ServiceType = ServiceName });
                    await FlushStorageAsync();
                    return true;
                }

                StorageLogger.LogInfo($"SaveAll operation started for {keysToSave.Count} keys", new { ServiceType = ServiceName, Keys = keysToSave });

                foreach (var key in keysToSave)
                {
                    var saveResult = await SaveAsync(key);
                    if (!saveResult)
                    {
                        success = false;
                        StorageLogger.LogError($"Failed to save key during SaveAll: {key}", new { ServiceType = ServiceName });
                    }
                }

                // Flush storage after all saves
                await FlushStorageAsync();

                StorageLogger.LogInfo($"SaveAll operation completed with {(success ? "success" : "some failures")}", new { ServiceType = ServiceName });
                return success;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "SaveAll", "N/A", ServiceName);
                return false;
            }
        }

        public virtual async UniTask DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StorageLogger.LogError($"Delete operation failed: Key cannot be null or whitespace", new { ServiceType = ServiceName });
                return;
            }

            try
            {
                // Check if exists first
                var exists = await ExistsAsync(key);
                if (!exists)
                {
                    StorageLogger.LogInfo($"Delete operation skipped: Key does not exist: {key}", new { ServiceType = ServiceName });
                    return;
                }

                // Remove from cache
                _cache.Remove(key);
                
                // Remove from dirty keys
                MarkKeyAsClean(key);

                // Delete from storage
                await DeleteFromStorageAsync(key);

                StorageLogger.LogInfo($"Data deleted successfully for key: {key}", new { ServiceType = ServiceName });
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "Delete", key, ServiceName);
                throw; // Re-throw to let caller handle the error
            }
        }

        public virtual async UniTask<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StorageLogger.LogError($"Exists operation failed: Key cannot be null or whitespace", new { ServiceType = ServiceName });
                return false;
            }

            try
            {
                // Check cache first
                if (_cache.Exists(key))
                {
                    return true;
                }

                // Check storage
                var exists = await ExistsInStorageAsync(key);
                return exists;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "Exists", key, ServiceName);
                return false;
            }
        }

        /// <summary>
        /// Returns all keys that have been modified in cache but not yet saved to storage
        /// </summary>
        protected IReadOnlyCollection<string> GetDirtyKeys()
        {
            return _dirtyKeys.ToList();
        }

        /// <summary>
        /// Marks a key as dirty (modified in cache but not saved to storage)
        /// </summary>
        protected void MarkKeyAsDirty(string key)
        {
            _dirtyKeys.Add(key);
        }

        /// <summary>
        /// Marks a key as clean (synchronized with storage)
        /// </summary>
        protected void MarkKeyAsClean(string key)
        {
            _dirtyKeys.Remove(key);
        }

        // Abstract methods that concrete implementations must provide
        protected abstract UniTask<T> LoadFromStorageAsync<T>(string key);
        protected abstract UniTask SaveToStorageAsync<T>(string key, T data);
        protected abstract UniTask DeleteFromStorageAsync(string key);
        protected abstract UniTask<bool> ExistsInStorageAsync(string key);
        protected abstract UniTask FlushStorageAsync();
    }
} 