using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Consumables;
using Cysharp.Threading.Tasks;
using FluencySDK;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using SubwaySurfers;
using Utilities;

namespace EducationIntegration.QuestionHandlers
{
    /// <summary>
    /// Question handler that spawns an obstacle with 3 AnswerObjects in front of it.
    /// Player must go through the correct answer object to proceed.
    /// </summary>
    public class FinishLineQuestionHandler : BaseQuestionHandler
    {
        public override string HandlerIdentifier => "finish_line";
        
        [Header("Debug")]
        [Tooltip("Enable debug logging for finish line spawning and management")]
        public bool enableFinishLineDebugLogs = false;
        
        [Header("Prefabs")] [SerializeField] private AssetReference obstaclePrefab;
        [SerializeField] private AssetReference answerObjectPrefab;

        [Header("Spawn Settings")] [Tooltip("Base distance for obstacle spawning")] [SerializeField]
        private float fallbackObstacleSpawnDistance = 15f;

        [Tooltip("Distance in front of obstacle to place answer objects")] [SerializeField]
        private float answerObjectZOffset = 0.1f;

        [SerializeField] private float autoAnswerMissDistance = 5f;

        [Header("Object Clearing")] [Tooltip("Radius to clear existing objects around spawn area")] [SerializeField]
        private float clearanceRadius = 5f;

        [Tooltip("Layers to check for existing obstacles")] [SerializeField]
        private LayerMask obstacleLayerMask = -1;

        [Tooltip("Layers to check for coins/collectibles")] [SerializeField]
        private LayerMask collectibleLayerMask = -1;

        // References to game systems
        private TrackManager _trackManager;
        private CharacterInputController _characterController;
        private CharacterCollider _characterCollider;
        private ILootablesSpawner _powerUpSpawner;

        // State tracking
        private GameObject _spawnedObstacle;
        private readonly List<GameObject> _spawnedAnswerObjects = new();
        private CancellationTokenSource _spawningCancellationTokenSource;
        private MissCheckStatus _missCheckStatus = MissCheckStatus.None;

        private enum MissCheckStatus
        {
            None,
            Ready,
            Missed,
        }

        #region Debug Logging Helpers
        
        private void DebugLog(string message)
        {
            if (enableFinishLineDebugLogs)
                Debug.Log(message);
        }
        
        private void DebugLogWarning(string message)
        {
            if (enableFinishLineDebugLogs)
                Debug.LogWarning(message);
        }
        
        #endregion

        protected override void Initialize()
        {
            base.Initialize();

            DebugLog("[FinishLineQuestionHandler] Starting initialization...");

            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            if (_trackManager == null)
            {
                Debug.LogError("[FinishLineQuestionHandler] TrackManager not found");
                return;
            }

            DebugLog($"[FinishLineQuestionHandler] TrackManager found: {_trackManager.name}");

            _characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            if (_characterController == null)
            {
                Debug.LogError("[FinishLineQuestionHandler] CharacterInputController not found");
                return;
            }

            DebugLog($"[FinishLineQuestionHandler] CharacterInputController found: {_characterController.name}");

            _characterCollider = FindFirstObjectByType<CharacterCollider>(FindObjectsInactive.Include);
            if (_characterCollider == null)
            {
                Debug.LogError("[FinishLineQuestionHandler] CharacterCollider not found");
                return;
            }

            DebugLog($"[FinishLineQuestionHandler] CharacterCollider found: {_characterCollider.name}");

            _powerUpSpawner = FindFirstObjectByType<LootablesSpawner>(FindObjectsInactive.Include);
            if (_powerUpSpawner == null)
            {
                Debug.LogError("[FinishLineQuestionHandler] LootablesSpawner not found");
                return;
            }

            DebugLog($"[FinishLineQuestionHandler] LootablesSpawner found: {_powerUpSpawner.GetType().Name}");

            DebugLog("[FinishLineQuestionHandler] Initialization completed successfully");
        }

        protected override void DoSubscribeToEvents()
        {
            base.DoSubscribeToEvents();

            if (_characterCollider != null)
            {
                _characterCollider.OnObjectEnteredPlayerTrigger += OnObjectEnteredPlayerTrigger;
                DebugLog("[FinishLineQuestionHandler] Successfully subscribed to OnObjectEnteredPlayerTrigger event");
            }
            else
            {
                Debug.LogError("[FinishLineQuestionHandler] Cannot subscribe to events - CharacterCollider is null!");
            }
        }

        protected override void ProcessOnQuestionStarted()
        {
        }

        protected override void DoUnsubscribeFromEvents()
        {
            base.DoUnsubscribeFromEvents();

            if (_characterCollider != null)
            {
                _characterCollider.OnObjectEnteredPlayerTrigger -= OnObjectEnteredPlayerTrigger;
                DebugLog("[FinishLineQuestionHandler] Successfully unsubscribed from OnObjectEnteredPlayerTrigger event");
            }
        }

        private void OnObjectEnteredPlayerTrigger(GameObject obj)
        {
            if (Question == null || IsQuestionStarted == false || _missCheckStatus == MissCheckStatus.Missed)
            {
                return;
            }

            if (obj.TryGetComponent<AnswerObject>(out var answerObject))
            {
                DebugLog($"[FinishLineQuestionHandler] Answer object component found! Answer: {answerObject.Answer}");
                QuestionProvider.SubmitAnswer(Question, UserAnswerSubmission.FromAnswer(answerObject.Answer));
            }
        }

        private void ClearExistingObjects(Vector3 center, float radius)
        {
            // Find all obstacles in radius
            Collider[] obstacles = Physics.OverlapSphere(center, radius, obstacleLayerMask);
            foreach (var obstacle in obstacles)
            {
                if (obstacle != null && obstacle.gameObject != null)
                {
                    DebugLog($"[FinishLineQuestionHandler] Clearing obstacle: {obstacle.name}");
                    Addressables.ReleaseInstance(obstacle.gameObject);
                }
            }

            // Find all collectibles in radius  
            Collider[] collectibles = Physics.OverlapSphere(center, radius, collectibleLayerMask);
            foreach (var collectible in collectibles)
            {
                if (collectible != null && collectible.gameObject != null)
                {
                    DebugLog($"[FinishLineQuestionHandler] Clearing collectible: {collectible.name}");

                    // Use appropriate cleanup method for coins
                    if (collectible.TryGetComponent<Coin>(out var coin))
                    {
                        Coin.coinPool.Free(collectible.gameObject);
                    }
                    else
                    {
                        Addressables.ReleaseInstance(collectible.gameObject);
                    }
                }
            }
        }

        private async UniTask SpawnObstacle(TrackSegment segment, Vector3 position, Quaternion rotation)
        {
            var obstacleHandle = Addressables.InstantiateAsync(obstaclePrefab, position, rotation);
            await obstacleHandle;

            if (obstacleHandle.Result is GameObject obstacleObject)
            {
                _spawnedObstacle = obstacleObject;

                // Apply the same positioning hack used by other obstacles
                Vector3 oldPos = _spawnedObstacle.transform.position;
                _spawnedObstacle.transform.position += Vector3.back;
                _spawnedObstacle.transform.position = oldPos;

                DebugLog($"[FinishLineQuestionHandler] Obstacle spawned at {position}");
            }
            else
            {
                Debug.LogError("[FinishLineQuestionHandler] Failed to instantiate obstacle prefab");
            }
        }

        private async UniTask SpawnAnswerObjects(Vector3 basePos, Quaternion rotation,
            QuestionChoice<int>[] choices, QuestionChoice<int> correctChoice, CancellationToken cancellationToken)
        {
            // Randomize which lane gets the correct answer
            int correctLane = Random.Range(0, 3);
            var incorrectChoices = choices.Where(c => c.Value != correctChoice.Value).ToArray();

            for (int lane = 0; lane < 3; lane++)
            {
                // Calculate lane position
                Vector3 lanePos = basePos + ((lane - 1) * _trackManager.laneOffset * (rotation * Vector3.right));
                lanePos.y = 0.5f; // Set proper elevation

                DebugLog($"[FinishLineQuestionHandler] Calculated lane {lane} position: {lanePos} (basePos: {basePos}, laneOffset: {_trackManager.laneOffset}, rotation: {rotation})");

                // Determine which choice for this lane
                QuestionChoice<int> choiceForLane;
                if (lane == correctLane)
                {
                    choiceForLane = correctChoice;
                }
                else
                {
                    // Get an incorrect choice (ensure unique if possible)
                    int incorrectIndex = Mathf.Min(lane % incorrectChoices.Length, incorrectChoices.Length - 1);
                    choiceForLane = incorrectChoices[incorrectIndex];
                }

                var answerObject =
                    await AddressablesSafeSpawner.SpawnAsync<AnswerObject>(answerObjectPrefab, lanePos,
                        Quaternion.identity);
                // Check for cancellation after async spawn
                if (cancellationToken.IsCancellationRequested)
                {
                    DebugLog("[FinishLineQuestionHandler] Answer object spawning cancelled after spawning, cleaning up");
                    if (answerObject != null && answerObject.gameObject != null)
                    {
                        Addressables.ReleaseInstance(answerObject.gameObject);
                    }

                    return;
                }

                if (answerObject != null)
                {
                    DebugLog($"[FinishLineQuestionHandler] AnswerObject spawned successfully in lane {lane}");

                    // Set the answer data
                    answerObject.Repaint(choiceForLane);
                    DebugLog($"[FinishLineQuestionHandler] Answer data set: {choiceForLane})");

                    // Ensure text faces the player
                    answerObject.transform.rotation = Quaternion.identity;

                    _spawnedAnswerObjects.Add(answerObject.gameObject);

                    DebugLog($"[FinishLineQuestionHandler] Answer object spawned in lane {lane} with choice: {choiceForLane}");
                }
                else
                {
                    Debug.LogError($"[FinishLineQuestionHandler] Failed to spawn answer object for lane {lane}");
                }
            }
        }

        private async UniTask SpawnObstacleAndAnswers(IQuestion question, CancellationToken cancellationToken)
        {
            try
            {
                // Store local references to avoid null reference issues during async operations
                if (question?.Choices == null)
                {
                    Debug.LogError("[FinishLineQuestionHandler] Question or choices are null at start of spawning");
                    return;
                }
                
                var timeToAnswer = question.TimeToAnswer;

                // Clean up any existing spawned objects
                CleanupSpawnedObjects();

                // Calculate positions
                float playerZ = _characterController.transform.position.z;
                float spawnDistance = timeToAnswer == null
                    ? fallbackObstacleSpawnDistance
                    : _trackManager.speed * timeToAnswer.Value;
                float obstacleZ = playerZ + spawnDistance;
                float answerObjectsZ = obstacleZ - answerObjectZOffset;

                // Find target segment for obstacle
                TrackSegment targetSegment = FindSegmentForPosition(obstacleZ);
                if (targetSegment == null)
                {
                    Debug.LogError("[FinishLineQuestionHandler] Could not find segment for obstacle spawning");
                    return;
                }

                // Get spawn positions
                Vector3 obstaclePos, answerPos;
                Quaternion rotation;

                float segmentT = CalculateSegmentT(targetSegment, obstacleZ);
                targetSegment.GetPointAt(segmentT, out obstaclePos, out rotation);

                segmentT = CalculateSegmentT(targetSegment, answerObjectsZ);
                targetSegment.GetPointAt(segmentT, out answerPos, out rotation);

                // Clear existing objects in both spawn areas
                ClearExistingObjects(obstaclePos, clearanceRadius);
                ClearExistingObjects(answerPos, clearanceRadius);

                // Spawn obstacle
                await SpawnObstacle(targetSegment, obstaclePos, rotation);
                cancellationToken.ThrowIfCancellationRequested();

                // Spawn answer objects
                await SpawnAnswerObjects(answerPos, rotation, question.Choices, question.GetCorrectChoice(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Start the question
                await QuestionProvider.StartQuestion(this.Question);
                
                // Set miss check status to Ready after successfully spawning everything
                _missCheckStatus = MissCheckStatus.Ready;
                DebugLog("[FinishLineQuestionHandler] Set miss check status to Ready after spawning completed");
            }
            catch (System.OperationCanceledException)
            {
                DebugLog("[FinishLineQuestionHandler] Spawning was cancelled");
                CleanupSpawnedObjects();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FinishLineQuestionHandler] Error during spawning: {ex.Message}");
            }
        }

        private void CleanupSpawnedObjects()
        {
            // Clean up obstacle
            if (_spawnedObstacle != null)
            {
                Addressables.ReleaseInstance(_spawnedObstacle);
                _spawnedObstacle = null;
            }

            // Clean up answer objects
            foreach (var answerObj in _spawnedAnswerObjects)
            {
                if (answerObj != null)
                {
                    Addressables.ReleaseInstance(answerObj);
                }
            }

            _spawnedAnswerObjects.Clear();
        }

        private TrackSegment FindSegmentForPosition(float worldZ)
        {
            // Try subsequent segments
            foreach (var segment in _trackManager.segments)
            {
                var segmentStartZ = segment.transform.position.z;
                var segmentEndZ = segmentStartZ + segment.worldLength;

                if (worldZ >= segmentStartZ && worldZ <= segmentEndZ)
                {
                    return segment;
                }
            }

            // If no suitable segment found, return the last one
            return _trackManager.segments.LastOrDefault();
        }

        private float CalculateSegmentT(TrackSegment segment, float worldZ)
        {
            // Calculate the t-parameter (0-1) for a position in the segment
            float segmentStartZ = segment.transform.position.z;
            float segmentLength = segment.worldLength;

            return Mathf.Clamp01((worldZ - segmentStartZ) / segmentLength);
        }

        protected override bool DoHandleQuestion(IQuestion question)
        {
            DebugLog($"[FinishLineQuestionHandler] DoHandleQuestion called for question: {question?.Id}");
            
            _missCheckStatus = MissCheckStatus.None;

            // Create new cancellation token for this question's spawning
            _spawningCancellationTokenSource?.Cancel();
            _spawningCancellationTokenSource?.Dispose();
            _spawningCancellationTokenSource = new CancellationTokenSource();

            DebugLog("[FinishLineQuestionHandler] Starting to spawn obstacle and answer objects");

            // Spawn the obstacle and answer objects
            SpawnObstacleAndAnswers(question, _spawningCancellationTokenSource.Token).Forget();
            return true;
        }

        protected override void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission)
        {
            DebugLog($"[FinishLineQuestionHandler] Question ended. IsCorrect: {userAnswerSubmission}");

            // Cancel any ongoing spawning operations
            _spawningCancellationTokenSource?.Cancel();

            CleanupSpawnedObjects();
            _missCheckStatus = MissCheckStatus.None;
            DebugLog("[FinishLineQuestionHandler] ProcessOnQuestionEnded: Reset miss check status to None");
            // Reward/penalty logic is now handled by the uniform question result processor
        }

        private void OnDestroy()
        {
            _spawningCancellationTokenSource?.Cancel();
            _spawningCancellationTokenSource?.Dispose();
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
                return QuestionHandlerResult.CreateError(question, "Track manager is not available.");
            }

            if (!_trackManager.isMoving)
            {
                return QuestionHandlerResult.CreateError(question, "Track is not moving.");
            }

            if (obstaclePrefab == null)
            {
                return QuestionHandlerResult.CreateError(question, "Obstacle prefab is not assigned.");
            }

            if (answerObjectPrefab == null)
            {
                return QuestionHandlerResult.CreateError(question, "Answer object prefab is not assigned.");
            }

            return QuestionHandlerResult.CreateSuccess(question);
        }

        private bool IsBehindPlayer(Transform target)
        {
            if (target == null || _characterController == null)
            {
                return true;
            }

            // Get player's current position
            float playerZ = _characterController.transform.position.z;

            // Check if the answer object is behind the player
            return playerZ - target.position.z >= autoAnswerMissDistance;
        }

        private bool CheckIfMissedCorrectAnswer()
        {
            if (_spawnedAnswerObjects.Count == 0)
            {
                DebugLog("[FinishLineQuestionHandler] CheckIfMissedCorrectAnswer: No spawned answers to check");
                return false;
            }

            foreach (var answerObj in _spawnedAnswerObjects)
            {
                if (answerObj == null)
                {
                    continue;
                }

                var answerObject = answerObj.GetComponent<AnswerObject>();
                if (answerObject == null || answerObject.Answer == null || !answerObject.Answer.IsCorrect)
                {
                    continue;
                }

                bool isBehind = IsBehindPlayer(answerObj.transform);
                if (isBehind)
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            // Check if the player has passed the correct answer
            if (IsQuestionStarted && _missCheckStatus == MissCheckStatus.Ready)
            {
                if (CheckIfMissedCorrectAnswer())
                {
                    _missCheckStatus = MissCheckStatus.Missed;
                    DebugLog("[FinishLineQuestionHandler] Update: Player has missed all answer objects - skipping question.");
                    QuestionProvider.SubmitAnswer(this.Question, UserAnswerSubmission.FromSkipped());
                }
            }
        }
    }
}