using System;
using AIEduChatbot.UnityReactBridge.Storage;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Storage
{
    /// <summary>
    /// PlayerPrefs-based storage service implementation
    /// </summary>
    public class PlayerPrefsStorageService : BaseStorageService
    {
        protected override string ServiceName => "PlayerPrefs";

        public PlayerPrefsStorageService(IStorageCache cache) : base(cache)
        {
        }

        protected override UniTask<T> LoadFromStorageAsync<T>(string key)
        {
            try
            {
                if (PlayerPrefs.HasKey(key))
                {
                    string json = PlayerPrefs.GetString(key);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var data = JsonConvert.DeserializeObject<T>(json);
                        return UniTask.FromResult(data);
                    }
                }
                return UniTask.FromResult(default(T));
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "LoadFromStorage", key, ServiceName, new { DataType = typeof(T).Name });
                return UniTask.FromResult(default(T));
            }
        }

        protected override UniTask SaveToStorageAsync<T>(string key, T data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.None);
                PlayerPrefs.SetString(key, json);
                return UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "SaveToStorage", key, ServiceName, new { DataType = typeof(T).Name });
                throw; // Re-throw to maintain error handling in base class
            }
        }

        protected override UniTask DeleteFromStorageAsync(string key)
        {
            try
            {
                PlayerPrefs.DeleteKey(key);
                return UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "DeleteFromStorage", key, ServiceName);
                throw; // Re-throw to maintain error handling in base class
            }
        }

        protected override UniTask<bool> ExistsInStorageAsync(string key)
        {
            try
            {
                return UniTask.FromResult(PlayerPrefs.HasKey(key));
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "ExistsInStorage", key, ServiceName);
                return UniTask.FromResult(false);
            }
        }

        protected override UniTask FlushStorageAsync()
        {
            try
            {
                PlayerPrefs.Save();
                return UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "FlushStorage", "N/A", ServiceName);
                throw; // Re-throw to maintain error handling in base class
            }
        }
    }
}