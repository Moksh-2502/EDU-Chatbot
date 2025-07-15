using UnityEngine;

namespace SharedCore
{
    public static class GameBuildConfigLoader
    {
        public static GameBuildConfig LoadBuildConfiguration()
        {
            // Load GameBuildConfig ScriptableObject from Resources folder
            GameBuildConfig config = Resources.Load<GameBuildConfig>($"{nameof(GameBuildConfig)}");

            if (config != null)
            {
                Debug.Log($"Loaded GameBuildConfig from Resources: {config}");
                return config;
            }
            else
            {
                Debug.LogWarning($"GameBuildConfig not found at Resources/{nameof(GameBuildConfig)}, using defaults");

                // Create a default configuration at runtime
                var defaultConfig = ScriptableObject.CreateInstance<GameBuildConfig>();
#if UNITY_EDITOR
                // save the default config to the Resources folder
                UnityEditor.AssetDatabase.CreateAsset(defaultConfig, $"Assets/ReusablePatterns/SharedCore/Resources/{nameof(GameBuildConfig)}.asset");
                UnityEditor.EditorUtility.SetDirty(defaultConfig);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
                Debug.Log($"Created default GameBuildConfig at Resources/{nameof(GameBuildConfig)}.asset");
                return defaultConfig;
            }
        }
    }
}