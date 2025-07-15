using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using SubwaySurfers.Tutorial.Events;
using SubwaySurfers.Tutorial.Data;

namespace SubwaySurfers.Tutorial.Core
{
    public class TutorialObstacleSpawner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TutorialObstacleSequence[] obstacleSequences;
        
        [Header("Spawning Settings")]
        [SerializeField] private float laneOffset = 1.5f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Core dependencies
        private TrackManager _trackManager;
        private CharacterInputController _characterController;
        
        private readonly List<GameObject> _spawnedObstacles = new();
        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSpawner();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            CleanupAllObstacles();
        }

        #endregion

        #region Initialization

        private void InitializeSpawner()
        {
            // Find dependencies
            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            _characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);

            if (_trackManager == null)
            {
                Debug.LogError("TutorialObstacleSpawner: TrackManager not found!");
                return;
            }

            if (_characterController == null)
            {
                Debug.LogError("TutorialObstacleSpawner: CharacterInputController not found!");
                return;
            }
            
            _isInitialized = true;
            DebugLog("TutorialObstacleSpawner: Initialized successfully");
        }

        #endregion
        
        private bool TryGetSequenceForStep(TutorialStepType stepType, out TutorialObstacleSequence sequence)
        {
            sequence = null;
            if (obstacleSequences == null || obstacleSequences.Length == 0) return false;

            foreach (var seq in obstacleSequences)
            {
                if (seq.TargetStepType == stepType)
                {
                    sequence = seq;
                    return true;
                }
            }
            return false;
        }

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (!_isInitialized) return;
            
            TutorialEventBus.OnObstacleSpawnRequested += HandleObstacleSpawnRequest;
            TutorialEventBus.OnObstacleCleanupRequested += HandleObstacleCleanupRequest;
            TutorialEventBus.OnStepStarted += HandleStepStarted;
            TutorialEventBus.OnTutorialCompleted += HandleTutorialCompleted;
        }

        private void UnsubscribeFromEvents()
        {
            TutorialEventBus.OnObstacleSpawnRequested -= HandleObstacleSpawnRequest;
            TutorialEventBus.OnObstacleCleanupRequested -= HandleObstacleCleanupRequest;
            TutorialEventBus.OnStepStarted -= HandleStepStarted;
            TutorialEventBus.OnTutorialCompleted -= HandleTutorialCompleted;
        }

        private void HandleStepStarted(TutorialStepStartedEvent stepEvent)
        {
            DebugLog($"TutorialObstacleSpawner: Step started - {stepEvent.StepType}");
            
            // Clean up obstacles from previous step before spawning new ones
            CleanupObstaclesForPreviousSteps(stepEvent.StepType);
            
            // Auto-spawn obstacles for this step if sequence exists
            if (TryGetSequenceForStep(stepEvent.StepType, out var sequence))
            {
                SpawnObstaclesForSequence(sequence).Forget();
            }
        }

        private void HandleObstacleSpawnRequest(TutorialObstacleSpawnRequest request)
        {
            DebugLog($"TutorialObstacleSpawner: Spawn request for {request.StepType}");
            SpawnObstaclesFromRequest(request).Forget();
        }

        private void HandleObstacleCleanupRequest(TutorialObstacleCleanupRequest request)
        {
            DebugLog($"TutorialObstacleSpawner: Cleanup request for {request.StepType}");
            
            if (request.CleanupAll)
            {
                CleanupAllObstacles();
            }
            else
            {
                CleanupObstaclesForStep(request.StepType);
            }
        }

        private void HandleTutorialCompleted(TutorialCompletedEvent stepEvent)
        {
            DebugLog("TutorialObstacleSpawner: Tutorial completed");
            CleanupAllObstacles();
        }

        #endregion

        #region Obstacle Spawning

        private async UniTaskVoid SpawnObstaclesForSequence(TutorialObstacleSequence sequence)
        {
            DebugLog($"TutorialObstacleSpawner: Spawning obstacles for sequence {sequence.SequenceName}");
            
            var requests = sequence.GetSpawnRequests();
            foreach (var request in requests)
            {
                await SpawnObstaclesFromRequest(request);
            }
        }

        private async UniTask SpawnObstaclesFromRequest(TutorialObstacleSpawnRequest request)
        {
            if (_characterController == null || _trackManager == null)
            {
                Debug.LogError("TutorialObstacleSpawner: Missing dependencies for spawning");
                return;
            }

            // Get the sequence for asset references
            if (!TryGetSequenceForStep(request.StepType, out var sequence))
            {
                Debug.LogWarning($"TutorialObstacleSpawner: No sequence found for step {request.StepType}");
                return;
            }

            Vector3 playerPos = _characterController.transform.position;
            
            // Spawn multiple groups with separation
            for (int groupIndex = 0; groupIndex < request.GroupCount; groupIndex++)
            {
                float groupDistance = request.SpawnDistance + (groupIndex * request.GroupSeparation);
                Vector3 baseSpawnPos = playerPos + Vector3.forward * groupDistance;
                
                await SpawnObstacleGroup(sequence, request, baseSpawnPos, groupIndex);
            }
        }

        private async UniTask SpawnObstacleGroup(TutorialObstacleSequence sequence, TutorialObstacleSpawnRequest request, Vector3 basePosition, int groupIndex)
        {
            // Find matching obstacle group in sequence
            var obstacleGroup = System.Array.Find(sequence.ObstacleGroups, 
                g => g.obstacleType == request.ObstacleType);
            
            if (obstacleGroup?.obstaclePrefab == null)
            {
                Debug.LogWarning($"TutorialObstacleSpawner: No prefab found for obstacle type {request.ObstacleType}");
                return;
            }

            // Spawn obstacles in specified lanes
            foreach (int laneIndex in request.BlockedLanes)
            {
                if (laneIndex is < 0 or > 2) continue;
                
                Vector3 lanePosition = basePosition + Vector3.right * ((laneIndex - 1) * laneOffset);
                await SpawnSingleObstacle(obstacleGroup.obstaclePrefab, lanePosition, request.StepType);
            }

            DebugLog($"TutorialObstacleSpawner: Spawned group {groupIndex} for {request.StepType} at distance {Vector3.Distance(basePosition, _characterController.transform.position):F1}m");
        }

        private async UniTask SpawnSingleObstacle(AssetReference prefabRef, Vector3 position, TutorialStepType stepType)
        {
            try
            {
                var handle = Addressables.InstantiateAsync(prefabRef, position, Quaternion.identity);
                await handle;
                var obstacleObj = handle.Result;
                
                if (obstacleObj != null)
                {
                    // Configure obstacle for tutorial mode
                    ConfigureTutorialObstacle(obstacleObj, stepType);
                    _spawnedObstacles.Add(obstacleObj);
                    
                    DebugLog($"TutorialObstacleSpawner: Spawned {obstacleObj.name} at {position}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"TutorialObstacleSpawner: Failed to spawn obstacle - {ex.Message}");
            }
        }

        private void ConfigureTutorialObstacle(GameObject obstacleObj, TutorialStepType stepType)
        {
            // Add tutorial marker component for identification
            var marker = obstacleObj.AddComponent<TutorialObstacleMarker>();
            marker.SourceStepType = stepType;
            marker.SpawnTime = Time.time;
        }

        #endregion

        #region Cleanup

        private void CleanupObstaclesForStep(TutorialStepType stepType)
        {
            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = _spawnedObstacles[i];
                if (obstacle == null)
                {
                    _spawnedObstacles.RemoveAt(i);
                    continue;
                }

                if (obstacle.TryGetComponent<TutorialObstacleMarker>(out var marker) && marker.SourceStepType == stepType)
                {
                    Addressables.ReleaseInstance(obstacle);
                    _spawnedObstacles.RemoveAt(i);
                    DebugLog($"TutorialObstacleSpawner: Cleaned up obstacle from step {stepType}");
                }
            }
        }

        private void CleanupObstaclesForPreviousSteps(TutorialStepType currentStepType)
        {
            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = _spawnedObstacles[i];
                if (obstacle == null)
                {
                    _spawnedObstacles.RemoveAt(i);
                    continue;
                }

                if (obstacle.TryGetComponent<TutorialObstacleMarker>(out var marker) && marker.SourceStepType != currentStepType)
                {
                    Addressables.ReleaseInstance(obstacle);
                    _spawnedObstacles.RemoveAt(i);
                    DebugLog($"TutorialObstacleSpawner: Cleaned up obstacle from previous step {marker.SourceStepType}");
                }
            }
        }

        private void CleanupAllObstacles()
        {
            foreach (var obstacle in _spawnedObstacles)
            {
                if (obstacle != null)
                {
                    Addressables.ReleaseInstance(obstacle);
                }
            }
            _spawnedObstacles.Clear();
            DebugLog("TutorialObstacleSpawner: Cleaned up all obstacles");
        }

        #endregion

        #region Utility

        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Marker component to identify tutorial obstacles
    /// </summary>
    public class TutorialObstacleMarker : MonoBehaviour
    {
        public TutorialStepType SourceStepType;
        public float SpawnTime;
    }
} 