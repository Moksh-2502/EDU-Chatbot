using Cysharp.Threading.Tasks;

namespace AIEduChatbot.UnityReactBridge.Storage
{
    /// <summary>
    /// Interface for game storage services that handle persistent data storage
    /// with user-specific paths and JSON serialization.
    /// </summary>
    public interface IGameStorageService
    {
        static IGameStorageService Instance { get; set; }
        /// <summary>
        /// Loads data from storage for the current user.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the data to</typeparam>
        /// <param name="key">The storage key</param>
        /// <returns>The deserialized data or default(T) if not found</returns>
        UniTask<T> LoadAsync<T>(string key);

        /// <summary>
        /// Sets data to storage for the current user.
        /// </summary>
        /// <typeparam name="T">The type of data to serialize</typeparam>
        /// <param name="key">The storage key</param>
        /// <param name="data">The data to save</param>
        UniTask SetAsync<T>(string key, T data);

        /// <summary>
        /// Saves data from cache to storage for a specific key.
        /// </summary>
        /// <param name="key">The storage key to save</param>
        /// <returns>True if the save was successful, false otherwise</returns>
        UniTask<bool> SaveAsync(string key);

        /// <summary>
        /// Saves all cached data to storage.
        /// </summary>
        /// <returns>True if all saves were successful, false otherwise</returns>
        UniTask<bool> SaveAllAsync();

        /// <summary>
        /// Deletes data from storage for the current user.
        /// </summary>
        /// <param name="key">The storage key</param>
        UniTask DeleteAsync(string key);

        /// <summary>
        /// Checks if data exists in storage for the current user.
        /// </summary>
        /// <param name="key">The storage key</param>
        /// <returns>True if the data exists, false otherwise</returns>
        UniTask<bool> ExistsAsync(string key);
    }
} 