using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Utilities
{
    public static class AddressablesSafeSpawner
    {
        public static async UniTask<T> SpawnAsync<T>(string address, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError("Address is null or empty.");
                return null;
            }
            try
            {
                var handle = Addressables.InstantiateAsync(address, position, rotation);
                await handle.Task;
                if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    UnityEngine.Debug.LogError($"Failed to spawn Addressable '{address}': {handle.OperationException?.Message}");
                    return null;
                }
                return handle.Result.GetComponent<T>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to spawn Addressable '{address}': {ex.Message}");
                return null;
            }
        }
        
        public static async UniTask<T> SpawnAsync<T>(AssetReference asset, Vector3 position, Quaternion rotation)
        where T : UnityEngine.Component
        {
            if (asset == null)
            {
                Debug.LogError("AssetReference is null.");
                return null;
            }
            try
            {
                var handle = asset.InstantiateAsync(position, rotation);
                await handle.Task;
                if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load Addressable asset '{asset}' of type '{typeof(T).Name}': {handle.OperationException?.Message}");
                    return null;
                }
                var go = handle.Result;
                if (go == null)
                {
                    Debug.LogError($"Addressable asset '{asset}' returned null.");
                    return null;
                }
                // if component find that component on the go
                if (go.TryGetComponent<T>(out var component))
                {
                    return component;
                }

                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load Addressable asset '{asset}' of type '{typeof(T).Name}': {ex.Message}");
                return null;
            }
        }
    }
}