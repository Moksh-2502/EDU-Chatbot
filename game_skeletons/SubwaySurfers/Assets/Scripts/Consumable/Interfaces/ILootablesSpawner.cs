using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Consumables
{
    public interface ILootablesSpawner
    {
        float PowerupSpawnMultiplier { get; set;}
        float PremiumSpawnMultiplier { get; set;}
        void RemovePowerUp(Consumable powerup);
        UniTask<TComponent> SpawnAsync<TComponent>(GameObject prefab, Vector3 position, Quaternion rotation)
            where TComponent : UnityEngine.Component;

        UniTask<TComponent> SpawnAsync<TComponent>(AssetReference asset, Vector3 position, Quaternion rotation)
            where TComponent : UnityEngine.Component;
    }
}