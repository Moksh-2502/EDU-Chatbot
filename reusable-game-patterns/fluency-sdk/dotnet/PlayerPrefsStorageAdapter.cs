// IMPORTANT: This adapter is intended for use within a Unity environment.
// It requires the UnityEngine.dll assembly, which is available in Unity projects.
// Ensure Newtonsoft.Json is also available in your Unity project (e.g., via a package or DLL).

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER || UNITY_EDITOR // Common Unity defines, added UNITY_EDITOR
using UnityEngine;
#endif
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FluencySDK
{
    public class PlayerPrefsStorageAdapter : IStorageAdapter
    {
        public Task<T> GetItemAsync<T>(string key)
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER || UNITY_EDITOR
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
                    }
                    catch (JsonException ex)
                    {
                        // Using Debug.LogError for Unity console
                        Debug.LogError($"FluencySDK: Failed to deserialize {key} from PlayerPrefs. Error: {ex.Message}");
                        return Task.FromResult(default(T));
                    }
                }
            }
            return Task.FromResult(default(T));
#else
            // Fallback for non-Unity environments - logs to System.Diagnostics or console.
            System.Diagnostics.Debug.WriteLine($"FluencySDK: PlayerPrefsStorageAdapter.GetItemAsync for key '{key}' called outside Unity environment (or UnityEngine symbols not defined).");
            return Task.FromResult(default(T));
#endif
        }

        public Task SetItemAsync<T>(string key, T value)
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER || UNITY_EDITOR
            try
            {
                string json = JsonConvert.SerializeObject(value, Formatting.None); 
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save(); // Explicitly save, especially important for editor or immediate persistence needs.
            }
            catch (JsonException ex)
            {
                Debug.LogError($"FluencySDK: Failed to serialize data for key {key} for PlayerPrefs. Error: {ex.Message}");
            }
            return Task.CompletedTask;
#else
            System.Diagnostics.Debug.WriteLine($"FluencySDK: PlayerPrefsStorageAdapter.SetItemAsync for key '{key}' called outside Unity environment (or UnityEngine symbols not defined).");
            return Task.CompletedTask;
#endif
        }

        public Task RemoveItemAsync(string key)
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER || UNITY_EDITOR
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
            return Task.CompletedTask;
#else
            System.Diagnostics.Debug.WriteLine($"FluencySDK: PlayerPrefsStorageAdapter.RemoveItemAsync for key '{key}' called outside Unity environment (or UnityEngine symbols not defined).");
            return Task.CompletedTask;
#endif
        }
    }
} 