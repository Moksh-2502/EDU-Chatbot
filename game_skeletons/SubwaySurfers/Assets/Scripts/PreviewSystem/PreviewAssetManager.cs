using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Centralized asset management for preview system
    /// Handles loading, caching, and cleanup of preview assets
    /// </summary>
    public class PreviewAssetManager : MonoBehaviour
    {
        public static PreviewAssetManager Instance {get; private set;}

        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _loadedAssets = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private readonly Dictionary<string, GameObject> _spawnedInstances = new Dictionary<string, GameObject>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            CleanupAllAssets();
        }

        /// <summary>
        /// Loads and instantiates a preview asset
        /// </summary>
        /// <param name="assetAddress">The addressable asset address</param>
        /// <param name="parent">Parent transform for the instantiated asset</param>
        /// <param name="instanceId">Unique identifier for this instance</param>
        /// <returns>The instantiated GameObject</returns>
        public async UniTask<GameObject> LoadPreviewAssetAsync(PreviwableAssetAddress assetAddress, Transform parent, string instanceId)
        {
            if (assetAddress == null)
            {
                Debug.LogWarning("PreviewAssetManager: Asset address is null or empty");
                return null;
            }

            if(assetAddress.HasValue() == false)
            {
                Debug.LogWarning("PreviewAssetManager: Asset address is not valid");
                return null;
            }

            try
            {
                // Clean up existing instance if it exists
                await CleanupInstanceAsync(instanceId);

                // Load the asset
                var handle = string.IsNullOrWhiteSpace(assetAddress.AssetAddress) ? assetAddress.AddressableReference.InstantiateAsync(parent) : Addressables.InstantiateAsync(assetAddress.AssetAddress, parent);
                await handle;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    _loadedAssets[instanceId] = handle;
                    _spawnedInstances[instanceId] = handle.Result;
                    
                    Debug.Log($"PreviewAssetManager: Successfully loaded asset '{assetAddress}' with instance ID '{instanceId}'");
                    return handle.Result;
                }
                else
                {
                    Debug.LogWarning($"PreviewAssetManager: Failed to load asset '{assetAddress}'");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PreviewAssetManager: Error loading asset '{assetAddress}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cleans up a specific preview instance
        /// </summary>
        /// <param name="instanceId">The instance ID to cleanup</param>
        public async UniTask CleanupInstanceAsync(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            // Remove spawned instance
            if (_spawnedInstances.TryGetValue(instanceId, out GameObject instance))
            {
                if (instance != null)
                {
                    DestroyImmediate(instance);
                }
                _spawnedInstances.Remove(instanceId);
            }

            // Release asset handle
            if (_loadedAssets.TryGetValue(instanceId, out AsyncOperationHandle<GameObject> handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                _loadedAssets.Remove(instanceId);
            }

            await UniTask.Yield();
        }

        /// <summary>
        /// Gets the spawned instance for a given instance ID
        /// </summary>
        /// <param name="instanceId">The instance ID</param>
        /// <returns>The spawned GameObject or null if not found</returns>
        public GameObject GetSpawnedInstance(string instanceId)
        {
            _spawnedInstances.TryGetValue(instanceId, out GameObject instance);
            return instance;
        }

        /// <summary>
        /// Checks if an instance is currently loaded
        /// </summary>
        /// <param name="instanceId">The instance ID to check</param>
        /// <returns>True if the instance is loaded</returns>
        public bool IsInstanceLoaded(string instanceId)
        {
            return !string.IsNullOrEmpty(instanceId) && _spawnedInstances.ContainsKey(instanceId);
        }

        /// <summary>
        /// Cleans up all loaded assets and instances
        /// </summary>
        public void CleanupAllAssets()
        {
            // Destroy all spawned instances
            foreach (var kvp in _spawnedInstances)
            {
                if (kvp.Value != null)
                {
                    DestroyImmediate(kvp.Value);
                }
            }
            _spawnedInstances.Clear();

            // Release all asset handles
            foreach (var kvp in _loadedAssets)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                }
            }
            _loadedAssets.Clear();
        }
    }
} 