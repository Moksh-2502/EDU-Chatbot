using System.Collections.Generic;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Storage
{
    public interface IStorageCache
    {
        (bool exists, T data) TryGetCachedData<T>(string dataKey);
        void SetCachedData<T>(string dataKey, T data);
        void Remove(string dataKey);
        bool Exists(string dataKey);
        
        /// <summary>
        /// Gets all cached keys for the current session
        /// </summary>
        /// <returns>List of data keys (without session prefix)</returns>
        IReadOnlyList<string> GetAllCachedKeys();
        
        /// <summary>
        /// Clears all cached data for the current session
        /// </summary>
        void ClearSession();
        
        /// <summary>
        /// Gets the count of cached items for the current session
        /// </summary>
        /// <returns>Number of cached items</returns>
        int GetCachedItemCount();
    }
}