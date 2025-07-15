using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Consumables;
using Cysharp.Threading.Tasks;
using FluencySDK;
using UnityEngine;
using SubwaySurfers;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using SharpShuffleBag;
using UnityEngine.AddressableAssets;

namespace EducationIntegration.QuestionHandlers
{
    public class PowerupQuestionHandler : BaseQuestionHandler, ILootableQuestionHandler<Consumable>
    {
        public override string HandlerIdentifier => "powerup_questions";

        [Header("Debug")]
        [Tooltip("Enable debug logging for powerup spawning and management")]
        public bool enablePowerupDebugLogs = false;

        [Header("Continuous Spawning Configuration")] [SerializeField]
        private int powerupsPerSegment = 2;

        [SerializeField] private float minDistanceFromPlayer = 15f;
        [SerializeField] private Consumable.ConsumableType[] supportedConsumables;

        [Header("Segment Sampling Configuration")] [SerializeField]
        private float segmentSampleIncrement = 0.1f; // Distance between samples

        [SerializeField] private LayerMask avoidanceLayerMask = -1;
        [SerializeField] private float avoidanceRadius = 2f;


        private readonly List<GameObject> _allSpawnedPowerups = new();
        private readonly HashSet<TrackSegment> _processedSegments = new();
        private TrackManager _trackManager;
        private CancellationTokenSource _spawningToken;
        private Consumable _pendingConsumable;
        private IGameManager _gameManager;
        private CharacterInputController _characterInputController;

        #region Debug Logging Helpers
        
        private void DebugLog(string message)
        {
            if (enablePowerupDebugLogs)
                Debug.Log($"[PowerupQuestionHandler] {message}");
        }
        
        private void DebugLogWarning(string message)
        {
            if (enablePowerupDebugLogs)
                Debug.LogWarning($"[PowerupQuestionHandler] {message}");
        }
        
        private void DebugLogError(string message)
        {
            if (enablePowerupDebugLogs)
                Debug.LogError($"[PowerupQuestionHandler] {message}");
        }
        
        #endregion

        protected override void Initialize()
        {
            base.Initialize();
            DebugLog("Starting initialization...");
            
            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            if (_trackManager == null)
            {
                DebugLogError("TrackManager not found during initialization");
                return;
            }
            DebugLog($"TrackManager found: {_trackManager.name}");
            
            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (_gameManager == null)
            {
                DebugLogError("GameManager not found during initialization");
                return;
            }

            _characterInputController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            
            DebugLog("Initialization completed successfully");
        }

        protected override void DoSubscribeToEvents()
        {
            base.DoSubscribeToEvents();
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        protected override void DoUnsubscribeFromEvents()
        {
            base.DoUnsubscribeFromEvents();
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        protected override bool DoHandleQuestion(IQuestion question)
        {
            DebugLog($"DoHandleQuestion called for question: {question?.Id}");
            StartContinuousSpawning();
            return true;
        }

        public override QuestionHandlerResult CanHandleQuestionNow(IQuestion question)
        {
            var baseResult = base.CanHandleQuestionNow(question);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            if (_trackManager == null)
            {
                return QuestionHandlerResult.CreateError(question, "TrackManager is not available.");
            }

            if (!_trackManager.isMoving)
            {
                return QuestionHandlerResult.CreateError(question, "Track is not moving.");
            }

            return QuestionHandlerResult.CreateSuccess(question);
        }

        protected override void ProcessOnQuestionStarted()
        {
            // Spawning is already started in DoHandleQuestion
        }

        protected override void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission)
        {
            DebugLog($"Question ended. Answer: {userAnswerSubmission}");
            StopContinuousSpawning();

            // Handle pending consumable based on answer
            if (_pendingConsumable != null)
            {
                if (userAnswerSubmission.AnswerType == AnswerType.Correct)
                {
                    DebugLog("Correct answer - applying consumable to character");
                    if (_characterInputController != null)
                    {
                        _characterInputController.UseConsumable(_pendingConsumable);
                    }

                    _allSpawnedPowerups.Remove(_pendingConsumable.gameObject);
                }
                _pendingConsumable = null;
            }
            CleanupAllPowerups();
        }

        /// <summary>
        /// Called by QuestionGatedConsumableProcessor when a question powerup is collected
        /// </summary>
        public void HandleLootableCollection(Consumable consumable)
        {
            DebugLog($"HandleLootableCollection called for consumable: {consumable?.name}");
            
            if (_pendingConsumable == null && Question != null)
            {
                _pendingConsumable = consumable;
                DebugLog($"Starting question for consumable {consumable.name}");
                QuestionProvider.StartQuestion(Question).ContinueWith(() =>
                {
                    DebugLog($"Question started successfully for consumable {consumable.name}");
                });
            }
            else
            {
                DebugLogWarning($"Already processing a consumable ({_pendingConsumable?.name}) or no question available (Question: {Question?.Id})");
            }
        }

        /// <summary>
        /// Check if this handler can currently process consumables
        /// </summary>
        public bool CanProcessLootables()
        {
            bool canProcess = Question != null && _pendingConsumable == null;
            DebugLog($"CanProcessLootables: {canProcess} (Question: {Question?.Id}, PendingConsumable: {_pendingConsumable?.name})");
            return canProcess;
        }

        private void StartContinuousSpawning()
        {
            DebugLog("StartContinuousSpawning called");
            
            _spawningToken?.Cancel();
            _spawningToken = new CancellationTokenSource();
            _trackManager.newSegmentCreated += OnNewSegmentCreated;
            DebugLog("Subscribed to newSegmentCreated event");

            // Process all current active segments
            DebugLog("Starting to process existing segments");
            ProcessExistingSegments(_spawningToken.Token).Forget();
        }

        private void OnNewSegmentCreated(TrackSegment segment)
        {
            DebugLog($"OnNewSegmentCreated called for segment: {segment?.name}");
            
            if (_spawningToken is { IsCancellationRequested: false })
            {
                DebugLog($"Processing new segment: {segment.name}");
                ProcessSegmentAsync(segment, _spawningToken.Token).Forget();
            }
            else
            {
                DebugLog("Spawning token is cancelled or null, skipping new segment processing");
            }
        }

        private async UniTask ProcessExistingSegments(CancellationToken cancellationToken)
        {
            if (_trackManager == null)
            {
                DebugLogError("ProcessExistingSegments: TrackManager is null");
                return;
            }

            // Get all currently active segments
            var segments = _trackManager.segments.ToArray();
            DebugLog($"ProcessExistingSegments: Found {segments.Length} active segments");
            
            foreach (var segment in segments)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    DebugLog("ProcessExistingSegments: Cancellation requested, breaking loop");
                    break;
                }

                if (_trackManager.segments.Contains(segment) == false)
                {
                    DebugLog($"ProcessExistingSegments: Segment {segment?.name} no longer active, skipping");
                    continue;
                }

                DebugLog($"ProcessExistingSegments: Processing segment {segment.name}");
                await ProcessSegmentAsync(segment, cancellationToken);
            }
            
            DebugLog("ProcessExistingSegments: Completed processing all existing segments");
        }

        private async UniTask ProcessSegmentAsync(TrackSegment segment, CancellationToken token)
        {
            if (segment == null)
            {
                DebugLogWarning("ProcessSegmentAsync: segment is null");
                return;
            }

            await UniTask.WaitUntil(() => segment == null || segment.IsReady, cancellationToken: token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (segment == null || segment.gameObject == null || _trackManager.segments.Contains(segment) == false)
            {
                return;
            }
            
            if (!_processedSegments.Add(segment))
            {
                DebugLog($"ProcessSegmentAsync: Segment {segment.name} already processed, skipping");
                return;
            }
            
            DebugLog($"ProcessSegmentAsync: Starting to process segment {segment.name}");

            // Sample all valid positions on this segment
            var validPositions = SampleValidPositionsOnSegment(segment);
            DebugLog($"ProcessSegmentAsync: Found {validPositions.Count} valid positions on segment {segment.name}");

            if (validPositions.Count == 0)
            {
                DebugLog($"ProcessSegmentAsync: No valid positions found on segment {segment.name}");
                return;
            }

            Shuffle.FisherYates(validPositions);
            DebugLog($"ProcessSegmentAsync: Shuffled valid positions for segment {segment.name}");

            // Spawn up to the desired number of powerups
            int spawnsToCreate = Mathf.Min(powerupsPerSegment, validPositions.Count);
            DebugLog($"ProcessSegmentAsync: Will spawn {spawnsToCreate} powerups on segment {segment.name}");

            for (int i = 0; i < spawnsToCreate; i++)
            {
                if (token.IsCancellationRequested)
                {
                    DebugLog($"ProcessSegmentAsync: Cancellation requested during spawn {i}, breaking");
                    break;
                }

                Vector3 spawnPosition = validPositions[i];
                DebugLog($"ProcessSegmentAsync: Spawning powerup {i + 1}/{spawnsToCreate} at position {spawnPosition}");
                await SpawnQuestionPowerupAt(spawnPosition, segment.transform, token);

                if (token.IsCancellationRequested)
                {
                    DebugLog($"ProcessSegmentAsync: Cancellation requested after spawn {i}, breaking");
                    break;
                }

                // Small delay between spawns
                await UniTask.Delay(50, cancellationToken: token);
            }

            DebugLog($"ProcessSegmentAsync: Completed spawning {spawnsToCreate} powerups from {validPositions.Count} valid positions on segment {segment.name}");
        }

        private List<Vector3> SampleValidPositionsOnSegment(TrackSegment segment)
        {
            var validPositions = new List<Vector3>();
            
            if (_trackManager?.characterController == null)
            {
                DebugLogError("SampleValidPositionsOnSegment: TrackManager or characterController is null");
                return validPositions;
            }

            var playerZ = _trackManager.characterController.transform.position.z;
            DebugLog($"SampleValidPositionsOnSegment: Player Z position: {playerZ}, Min distance: {minDistanceFromPlayer}");

            // Sample the segment in increments (10 samples per world unit by default)
            float segmentLength = segment.worldLength;
            int totalSamples = Mathf.FloorToInt(segmentLength / segmentSampleIncrement);
            DebugLog($"SampleValidPositionsOnSegment: Segment {segment.name} length: {segmentLength}, total samples: {totalSamples}");

            int validSampleCount = 0;
            int skippedBehindPlayer = 0;
            int skippedNotClear = 0;

            for (int i = 0; i < totalSamples; i++)
            {
                float worldPosition = i * segmentSampleIncrement;

                // Get the point on the segment curve
                segment.GetPointAtInWorldUnit(worldPosition, out Vector3 centerPos, out Quaternion rotation);

                // Check if this position is ahead of the player
                if (centerPos.z < playerZ + minDistanceFromPlayer)
                {
                    skippedBehindPlayer++;
                    continue;
                }

                // Generate positions for all 3 lanes at this point
                for (int lane = 0; lane < 3; lane++)
                {
                    Vector3 laneOffset = (lane - 1) * _trackManager.laneOffset * (rotation * Vector3.right);
                    Vector3 lanePosition = centerPos + laneOffset;

                    // Check if this position is clear of existing objects
                    if (IsPositionClear(lanePosition))
                    {
                        validPositions.Add(lanePosition);
                        validSampleCount++;
                    }
                    else
                    {
                        skippedNotClear++;
                    }
                }
            }

            DebugLog($"SampleValidPositionsOnSegment: Found {validSampleCount} valid positions, skipped {skippedBehindPlayer} behind player, {skippedNotClear} not clear");
            return validPositions;
        }

        private bool IsPositionClear(Vector3 position)
        {
            // Check for obstacles
            bool isBlocked = Physics.CheckSphere(position, avoidanceRadius, avoidanceLayerMask);
            if (isBlocked)
            {
                DebugLog($"IsPositionClear: Position {position} is blocked by obstacle");
            }
            return !isBlocked;
        }

        private async UniTask SpawnQuestionPowerupAt(Vector3 position, Transform parent, CancellationToken token)
        {
            DebugLog($"SpawnQuestionPowerupAt: Attempting to spawn at position {position}");
            
            // Pick random consumable
            var spawnableConsumables = supportedConsumables.Select(o => ConsumableDatabase.GetConsumbale(o))
                .Where(o => o != null).ToArray();
                
            DebugLog($"SpawnQuestionPowerupAt: Found {spawnableConsumables.Length} spawnable consumables from {supportedConsumables.Length} supported types");
            
            if (spawnableConsumables.Length == 0)
            {
                DebugLogError("SpawnQuestionPowerupAt: No spawnable consumables available");
                return;
            }

            var selectedConsumable = spawnableConsumables[Random.Range(0, spawnableConsumables.Length)];
            DebugLog($"SpawnQuestionPowerupAt: Selected consumable: {selectedConsumable.name}");

            // Spawn powerup
            var powerup = await SpawnConsumableAsync(selectedConsumable, position, Quaternion.identity);

            if (powerup != null && !token.IsCancellationRequested)
            {
                DebugLog($"SpawnQuestionPowerupAt: Successfully spawned powerup {powerup.name}");
                
                // Attach question processor
                var processor = powerup.gameObject.AddComponent<QuestionGatedConsumableProcessor>();
                processor.SetQuestionHandler(this);
                DebugLog($"SpawnQuestionPowerupAt: Added QuestionGatedConsumableProcessor to {powerup.name}");

                powerup.transform.SetParent(parent);
                _allSpawnedPowerups.Add(powerup.gameObject);

                DebugLog($"SpawnQuestionPowerupAt: Spawned question powerup {powerup.name} at {position}, total spawned: {_allSpawnedPowerups.Count}");
            }
            else if (powerup == null)
            {
                DebugLogError($"SpawnQuestionPowerupAt: Failed to spawn consumable {selectedConsumable.name}");
            }
            else
            {
                DebugLog($"SpawnQuestionPowerupAt: Spawning was cancelled for {selectedConsumable.name}");
            }
        }

        private void StopContinuousSpawning()
        {
            DebugLog("StopContinuousSpawning called");
            
            _spawningToken?.Cancel();
            _spawningToken?.Dispose();
            _spawningToken = null;

            if (_trackManager != null)
            {
                _trackManager.newSegmentCreated -= OnNewSegmentCreated;
                DebugLog("Unsubscribed from newSegmentCreated event");
            }

            _processedSegments.Clear();
            DebugLog($"Cleared processed segments, stopped continuous spawning");
        }

        private void CleanupAllPowerups()
        {
            DebugLog($"CleanupAllPowerups: Cleaning up {_allSpawnedPowerups.Count} spawned powerups");
            
            foreach (var powerup in _allSpawnedPowerups)
            {
                if (powerup != null)
                {
                    Addressables.ReleaseInstance(powerup);
                }
            }

            _allSpawnedPowerups.Clear();
            DebugLog("CleanupAllPowerups: All powerups cleaned up");
        }

        private void OnGameStateChanged(AState newState)
        {
            DebugLog($"OnGameStateChanged: New state: {newState?.GetType().Name}");
            
            // Clean up if leaving game state
            if (!(newState is GameState))
            {
                DebugLog("OnGameStateChanged: Leaving game state, stopping spawning and cleaning up");
                StopContinuousSpawning();
                CleanupAllPowerups();
            }
        }

        private async UniTask<Consumable> SpawnConsumableAsync(Consumable prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                DebugLogError("SpawnConsumableAsync: prefab is null");
                return null;
            }

            DebugLog($"SpawnConsumableAsync: Attempting to spawn {prefab.name} at {position}");

            try
            {
                var op = Addressables.InstantiateAsync(prefab.name, position, rotation);
                await op;

                if (op.Result is GameObject result && result.TryGetComponent<Consumable>(out var consumable))
                {
                    DebugLog($"SpawnConsumableAsync: Successfully spawned and found Consumable component on {result.name}");
                    return consumable;
                }

                if (op.Result != null)
                {
                    DebugLogError($"SpawnConsumableAsync: Spawned object {op.Result.name} but no Consumable component found");
                    Addressables.ReleaseInstance(op.Result);
                }
                else
                {
                    DebugLogError($"SpawnConsumableAsync: Addressables.InstantiateAsync returned null result for {prefab.name}");
                }

                return null;
            }
            catch (System.Exception ex)
            {
                DebugLogError($"SpawnConsumableAsync: Failed to spawn consumable '{prefab.name}': {ex.Message}");
                return null;
            }
        }

        private void OnDestroy()
        {
            StopContinuousSpawning();
            CleanupAllPowerups();
        }
    }
}