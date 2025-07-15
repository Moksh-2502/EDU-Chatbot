using System;
using UnityEngine;
using System.Collections.Generic;
using Consumables;
using Consumables.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Utilities;
using SubwaySurfers.Assets.Scripts.Tracks;
using Random = UnityEngine.Random;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace SubwaySurfers
{
    /// <summary>
    /// The PowerUpManager handles spawning and tracking powerups in the game.
    /// It implements IPowerUpSpawner to provide control over powerup spawning.
    /// </summary>
    public class LootablesSpawner : MonoBehaviour, ILootablesSpawner
    {
        [SerializeField] private float powerupSpawnMultiplier = 0.001f;
        [SerializeField] private float premiumSpawnMultiplier = 0.0001f;

        // Public properties for debug access
        public float PowerupSpawnMultiplier 
        { 
            get => powerupSpawnMultiplier; 
            set => powerupSpawnMultiplier = Mathf.Max(0f, value); 
        }
        
        public float PremiumSpawnMultiplier 
        { 
            get => premiumSpawnMultiplier; 
            set => premiumSpawnMultiplier = Mathf.Max(0f, value); 
        }
        [SerializeField] private ConsumableDatabase consumableDatabase;
        [SerializeField] private LayerMask avoidanceLayerMask;
        [SerializeField] private float avoidanceRadius = 1;


        private TrackManager m_TrackManager;
        private float m_TimeSincePowerup;
        private ITrackRunnerConfigProvider m_ConfigProvider;
        private float m_TimeSinceLastPremium;
        private List<GameObject> m_SpawnedPowerUps = new();

        private IGamePauser _gamePauser;

        private void Awake()
        {
            m_TrackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            _gamePauser = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
            m_ConfigProvider = ITrackRunnerConfigProvider.Instance;

            if (m_ConfigProvider == null)
            {
                Debug.LogWarning(
                    "[LootablesSpawner] No ITrackRunnerConfigProvider found. Spawn chances will use fallback values.");
            }

            m_TrackManager.newSegmentCreated += OnSegmentCreated;

            m_TimeSincePowerup = m_TimeSinceLastPremium = 0.0f;
        }

        private void OnSegmentCreated(TrackSegment segment)
        {
            if (segment == null)
            {
                Debug.LogError("Segment is null");
                return;
            }

            AutoSpawnLootablesOnSegmentAsync(segment).Forget();
        }

        private void Update()
        {
            if (GetCanSpawnLootable() == CanLootablesSpawnError.None)
            {
                m_TimeSincePowerup += Time.deltaTime;
                m_TimeSinceLastPremium += Time.deltaTime;
            }
        }

        private CanLootablesSpawnError GetCanSpawnLootable()
        {
            if (m_TrackManager == null)
            {
                return CanLootablesSpawnError.TracksNotDefined;
            }

            if (m_TrackManager.isMoving == false)
            {
                return CanLootablesSpawnError.TrackNotMoving;
            }

            if (_gamePauser.IsPaused)
            {
                return CanLootablesSpawnError.GamePaused;
            }

            return CanLootablesSpawnError.None;
        }

        private TComponent ProcessSpawnedItem<TComponent>(GameObject instance)
            where TComponent : Component
        {
            if (instance == null)
            {
                Debug.LogError($"Trying to process a null instance.");
                return null;
            }

            bool hasTargetComponent = instance.TryGetComponent<TComponent>(out var component);
            if (hasTargetComponent == false)
            {
                Addressables.ReleaseInstance(instance);
                return null;
            }


            if (component is Consumable)
            {
                m_SpawnedPowerUps.Add(instance);
            }

            return component;
        }

        public async UniTask<TComponent> SpawnAsync<TComponent>(AssetReference asset, Vector3 position,
            Quaternion rotation)
            where TComponent : Component
        {
            var item = await AddressablesSafeSpawner.SpawnAsync<TComponent>(asset, position, rotation);
            if (item == null)
            {
                return null;
            }

            return ProcessSpawnedItem<TComponent>(item.gameObject);
        }

        public async UniTask<TComponent> SpawnAsync<TComponent>(GameObject prefab, Vector3 position,
            Quaternion rotation)
            where TComponent : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"Prefab is null");
                return null;
            }

            try
            {
                var op = Addressables.InstantiateAsync(prefab.name, position, rotation);
                await op;
                if (op.Result is not GameObject result)
                {
                    Debug.LogWarning($"Unable to load consumable {prefab.name}.");
                    return null;
                }

                return ProcessSpawnedItem<TComponent>(result);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to spawn prefab '{prefab.name}' via Addressables. Error: {ex.Message}. " +
                               $"Make sure the prefab name corresponds to a valid Addressable key, or use the AssetReference overload instead.");
                return null;
            }
        }

        public void RemovePowerUp(Consumable powerup)
        {
            if (powerup != null)
            {
                m_SpawnedPowerUps.Remove(powerup.gameObject);
                Addressables.ReleaseInstance(powerup.gameObject);
            }
        }

        /// <summary>
        /// Selects a consumable based on difficulty-adjusted spawn frequencies
        /// </summary>
        private Consumable SelectConsumableByDifficulty()
        {
            if (consumableDatabase?.consumbales == null || consumableDatabase.consumbales.Length == 0)
            {
                return null;
            }

            // Create weighted list based on difficulty frequencies
            var weightedConsumables = new List<(Consumable consumable, float weight)>();
            float totalWeight = 0f;

            foreach (var consumable in consumableDatabase.consumbales)
            {
                if (consumable?.canBeSpawned == true)
                {
                    // Use the consumable type directly
                    float frequency = m_ConfigProvider?.GetConsumableSpawnFrequency(consumable.GetConsumableType()) ??
                                      1.0f;
                    if (frequency > 0f)
                    {
                        weightedConsumables.Add((consumable, frequency));
                        totalWeight += frequency;
                    }
                }
            }

            if (weightedConsumables.Count == 0 || totalWeight <= 0f)
            {
                // Fallback to random selection if no weighted options
                var spawnableConsumables = new List<Consumable>();
                foreach (var consumable in consumableDatabase.consumbales)
                {
                    if (consumable?.canBeSpawned == true)
                    {
                        spawnableConsumables.Add(consumable);
                    }
                }

                return spawnableConsumables.Count > 0
                    ? spawnableConsumables[Random.Range(0, spawnableConsumables.Count)]
                    : null;
            }

            // Select based on weighted probability
            float randomValue = Random.value * totalWeight;
            float currentWeight = 0f;

            foreach (var (consumable, weight) in weightedConsumables)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                {
                    return consumable;
                }
            }

            // Fallback to last consumable (shouldn't happen but safety measure)
            return weightedConsumables[^1].consumable;
        }

        private bool IsSegmentStillValid(TrackSegment segment)
        {
            return segment != null && segment.gameObject != null && m_TrackManager.segments.Contains(segment);
        }

        private async UniTask AutoSpawnLootablesOnSegmentInternalAsync(TrackSegment segment)
        {
            await UniTask.WaitUntil(() => GetCanSpawnLootable() == CanLootablesSpawnError.None);
            if (IsSegmentStillValid(segment) == false)
            {
                return;
            }

            await UniTask.WaitUntil(() => segment == null || segment.IsReady);
            if (IsSegmentStillValid(segment) == false)
            {
                return;
            }
            float laneOffset = m_TrackManager.laneOffset;
            const float increment = 1.5f;
            float currentWorldPos = 0.0f;
            int currentLane = Random.Range(0, 3);
            float powerupChance = Mathf.Clamp01(Mathf.Floor(m_TimeSincePowerup) * 0.5f * powerupSpawnMultiplier);
            float premiumChance = Mathf.Clamp01(Mathf.Floor(m_TimeSinceLastPremium) * 0.5f * premiumSpawnMultiplier);

            while (currentWorldPos < segment.worldLength)
            {
                segment.GetPointAtInWorldUnit(currentWorldPos, out var pos, out var rot);

                bool laneValid = true;
                int testedLane = currentLane;
                while (Physics.CheckSphere(pos + ((testedLane - 1) * laneOffset * (rot * Vector3.right)),
                           avoidanceRadius, avoidanceLayerMask))
                {
                    testedLane = (testedLane + 1) % 3;
                    if (currentLane == testedLane)
                    {
                        // Couldn't find a valid lane.
                        laneValid = false;
                        break;
                    }
                }

                currentLane = testedLane;

                if (laneValid)
                {
                    pos += (currentLane - 1) * laneOffset * (rot * Vector3.right);

                    var roll = Random.value;

                    GameObject toUse = null;
                    if (roll < powerupChance)
                    {
                        // Select consumable based on difficulty-adjusted frequencies
                        var selectedConsumable = SelectConsumableByDifficulty();

                        //if the powerup can't be spawned, we don't reset the time since powerup to continue to have a high chance of picking one next track segment
                        if (selectedConsumable != null)
                        {
                            m_TimeSincePowerup = 0.0f;
                            powerupChance = 0.0f;
                            var obj = await SpawnAsync<Consumable>(selectedConsumable.gameObject, pos, rot);
                            if (segment == null)
                            {
                                return;
                            }

                            if (obj == null)
                            {
                                break;
                            }

                            toUse = obj.gameObject;
                            toUse.transform.SetParent(segment.transform, true);
                        }
                    }
                    else if (roll < premiumChance)
                    {
                        m_TimeSinceLastPremium = 0.0f;
                        premiumChance = 0.0f;
                        var item = await SpawnAsync<Transform>(m_TrackManager.currentTheme.premiumCollectible, pos,
                            rot);
                        if (item == null)
                        {
                            break;
                        }

                        if (segment == null)
                        {
                            return;
                        }

                        toUse = item.gameObject;
                        toUse.transform.SetParent(segment.transform, true);
                    }
                    else
                    {
                        toUse = Coin.coinPool.Get(pos, rot);
                        toUse.transform.SetParent(segment.collectibleTransform, true);
                    }
                }

                currentWorldPos += increment;
            }
        }

        private async UniTaskVoid AutoSpawnLootablesOnSegmentAsync(TrackSegment segment)
        {
            try
            {
                await AutoSpawnLootablesOnSegmentInternalAsync(segment);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}