using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FluencySDK;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utilities;

namespace EducationIntegration.QuestionHandlers
{
    /// <summary>
    /// Question handler where answers appear as physical objects on the track
    /// Player loses a life for selecting wrong answers and gets a random buff for correct answers
    /// </summary>
    public class AnswerObjectsQuestionHandler : BaseQuestionHandler
    {
        public override string HandlerIdentifier => "pickable_answers";

        [Header("Debug")] [Tooltip("Enable debug logging for answer object spawning and management")]
        public bool enableAnswerObjectDebugLogs = false;

        [SerializeField] private AssetReference answerPrefab;

        [Header("Spawn Settings")] [Tooltip("Minimum distance between answer objects")] [SerializeField]
        private float minDistanceBetweenAnswers = 10f;

        [Tooltip("Additional random distance between answer objects")] [SerializeField]
        private float randomAdditionalDistance = 5f;

        [Tooltip("Distance multiplier for calculating safe distance for first answer")] [SerializeField]
        private float firstAnswerTimeToAnswerMultiplier = 0.7f;

        [Tooltip("Minimum distance from player")] [SerializeField]
        private float minDistanceFromPlayer = 5f;

        [SerializeField] private float autoAnswerMissDistance = 5f;

        [Header("Object Clearing")] [Tooltip("Radius to clear existing objects around spawn area")] [SerializeField]
        private float clearanceRadius = 5f;

        [Tooltip("Layers to check for existing obstacles")] [SerializeField]
        private LayerMask obstacleLayerMask = -1;

        [Tooltip("Layers to check for coins/collectibles")] [SerializeField]
        private LayerMask collectibleLayerMask = -1;

        [Header("Dynamic Distance Settings")]
        
        [Tooltip("Minimum distance between answers when using dynamic calculation")] [SerializeField]
        private float dynamicMinDistance = 8f;
        
        [Tooltip("Maximum distance between answers when using dynamic calculation")] [SerializeField]
        private float dynamicMaxDistance = 25f;
        
        [Tooltip("Additional buffer distance for obstacle clearing beyond answer span")] [SerializeField]
        private float obstacleClearanceBuffer = 10f;

        // Game continues while question is active - configured via Flags property

        private TrackManager _trackManager;
        private CharacterInputController _characterInputController;
        private CharacterCollider _characterCollider;

        private readonly List<AnswerObject> _spawnedAnswers = new(4);
        private MissCheckStatus _missCheckStatus = MissCheckStatus.None; // Flag to ensure we only trigger once
        private CancellationTokenSource _spawningCancellationTokenSource;

        private enum MissCheckStatus
        {
            None,
            Ready,
            Missed,
        }

        #region Debug Logging Helpers

        private void DebugLog(string message)
        {
            if (enableAnswerObjectDebugLogs)
                Debug.Log(message);
        }

        private void DebugLogWarning(string message)
        {
            if (enableAnswerObjectDebugLogs)
                Debug.LogWarning(message);
        }

        #endregion

        protected override void Initialize()
        {
            base.Initialize();

            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            if (_trackManager == null)
            {
                Debug.LogError("No TrackManager found in the scene");
            }

            _characterCollider = FindFirstObjectByType<CharacterCollider>(FindObjectsInactive.Include);
            if (_characterCollider == null)
            {
                Debug.LogError("No CharacterCollider found in the scene");
            }

            _characterInputController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            if (_characterInputController == null)
            {
                Debug.LogError("No CharacterInputController found in the scene");
            }
        }

        protected override void DoSubscribeToEvents()
        {
            base.DoSubscribeToEvents();
            if (_characterCollider != null)
            {
                _characterCollider.OnObjectEnteredPlayerTrigger += OnObjectEnteredPlayerTrigger;
            }
        }

        protected override void DoUnsubscribeFromEvents()
        {
            base.DoUnsubscribeFromEvents();
            if (_characterCollider != null)
            {
                _characterCollider.OnObjectEnteredPlayerTrigger -= OnObjectEnteredPlayerTrigger;
            }
        }

        private void CleanUp()
        {
            if (_spawnedAnswers.Count == 0)
            {
                DebugLog("[AnswerObjects] CleanUp: No spawned answers to clean up");
                return;
            }

            DebugLog($"[AnswerObjects] CleanUp: Cleaning up {_spawnedAnswers.Count} spawned answer objects");
            foreach (var item in _spawnedAnswers)
            {
                if (item != null)
                {
                    DebugLog($"[AnswerObjects] CleanUp: Releasing answer object at position {item.transform.position}");
                    Addressables.ReleaseInstance(item.gameObject);
                }
                else
                {
                    DebugLogWarning("[AnswerObjects] CleanUp: Found null answer object in spawned list");
                }
            }

            _spawnedAnswers.Clear();
            DebugLog("[AnswerObjects] CleanUp: All answer objects cleaned up");
        }

        private void OnObjectEnteredPlayerTrigger(GameObject obj)
        {
            if (obj == null || Question == null || IsQuestionStarted == false || _missCheckStatus == MissCheckStatus.Missed)
            {
                return;
            }

            if (obj.TryGetComponent<AnswerObject>(out var answerObject))
            {
                DebugLog(
                    $"[AnswerObjects] OnObjectEnteredPlayerTrigger: Player picked answer: '{answerObject.Answer}' at position {obj.transform.position}");
                QuestionProvider.SubmitAnswer(Question, UserAnswerSubmission.FromAnswer(answerObject.Answer));
            }
        }

        private bool IsBehindPlayer(Transform trgt)
        {
            if (trgt == null || _characterInputController == null)
            {
                return true;
            }

            // Get player's current position
            float playerZ = _characterInputController.transform.position.z;

            // Check if the answer object is behind the player
            return playerZ - trgt.position.z >= autoAnswerMissDistance;
        }

        private bool CheckIfMissedCorrectAnswer()
        {
            if (_spawnedAnswers.Count == 0)
            {
                DebugLog("[AnswerObjects] CheckIfHasMissedAllAnswers: No spawned answers to check");
                return false;
            }

            foreach (var item in _spawnedAnswers)
            {
                if (item == null || item.Answer == null || item.Answer.IsCorrect == false)
                {
                    continue;
                }

                bool isBehind = IsBehindPlayer(item.transform);
                if (isBehind)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearExistingObjects(Vector3 center, float radius)
        {
            // Find all obstacles in radius
            Collider[] obstacles = Physics.OverlapSphere(center, radius, obstacleLayerMask);
            foreach (var obstacle in obstacles)
            {
                if (obstacle != null && obstacle.gameObject != null)
                {
                    DebugLog($"[AnswerObjects] Clearing obstacle: {obstacle.name}");
                    Addressables.ReleaseInstance(obstacle.gameObject);
                }
            }

            // Find all collectibles in radius  
            Collider[] collectibles = Physics.OverlapSphere(center, radius, collectibleLayerMask);
            foreach (var collectible in collectibles)
            {
                if (collectible != null && collectible.gameObject != null)
                {
                    DebugLog($"[AnswerObjects] Clearing collectible: {collectible.name}");

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

        private void Update()
        {
            // Check if the player has passed the correct answer
            if (IsQuestionStarted && _missCheckStatus == MissCheckStatus.Ready)
            {
                if (CheckIfMissedCorrectAnswer())
                {
                    _missCheckStatus = MissCheckStatus.Missed;
                    DebugLog("[AnswerObjects] Update: Player has missed all answer objects - skipping question.");
                    QuestionProvider.SubmitAnswer(this.Question, UserAnswerSubmission.FromSkipped());
                }
            }
        }

        protected override bool DoHandleQuestion(IQuestion question)
        {
            QuestionProvider.StartQuestion(this.Question).Forget();
            return true;
        }

        protected override void ProcessOnQuestionStarted()
        {
            DebugLog(
                $"[AnswerObjects] ProcessOnQuestionStarted: Starting question with {Question?.Choices?.Length ?? 0} choices");
            _missCheckStatus = MissCheckStatus.None;

            // Create new cancellation token for this question's spawning
            _spawningCancellationTokenSource?.Cancel();
            _spawningCancellationTokenSource?.Dispose();
            _spawningCancellationTokenSource = new CancellationTokenSource();

            DebugLog("[AnswerObjects] ProcessOnQuestionStarted: Starting answer object spawning");
            SpawnAnswerObjects(_spawningCancellationTokenSource.Token).Forget();
        }

        /// <summary>
        /// Gets a random lane different from the previous one for better distribution
        /// </summary>
        /// <param name="previousLane">Previously used lane index (0-2), or -1 if none</param>
        /// <returns>Random lane index (0-2)</returns>
        private int GetRandomLane(int previousLane)
        {
            DebugLog($"[AnswerObjects] GetRandomLane: previousLane: {previousLane}");

            // Create list of all available lanes
            List<int> availableLanes = new List<int> { 0, 1, 2 };

            // Prefer lanes different from previous one for better distribution
            if (previousLane != -1)
            {
                List<int> differentLanes = new List<int>();
                foreach (int lane in availableLanes)
                {
                    if (lane != previousLane)
                    {
                        differentLanes.Add(lane);
                    }
                }

                if (differentLanes.Count > 0)
                {
                    int selectedLane = differentLanes[Random.Range(0, differentLanes.Count)];
                    DebugLog($"[AnswerObjects] GetRandomLane: Selected different lane: {selectedLane}");
                    return selectedLane;
                }
            }

            int finalLane = availableLanes[Random.Range(0, availableLanes.Count)];
            DebugLog($"[AnswerObjects] GetRandomLane: Selected random lane: {finalLane}");
            return finalLane;
        }

        /// <summary>
        /// Find suitable spawn positions for answer objects that avoid obstacles and create a natural pattern
        /// Uses a similar algorithm to LootablesSpawner for better consistency
        /// </summary>
        private async UniTask SpawnAnswerObjects(CancellationToken cancellationToken)
        {
            try
            {
                DebugLog("[AnswerObjects] SpawnAnswerObjects: Starting spawning process");

                // Store local references to avoid null reference issues during async operations
                var currentQuestion = Question;
                if (currentQuestion == null || currentQuestion.Choices == null)
                {
                    Debug.LogError(
                        "[AnswerObjects] SpawnAnswerObjects: Question or choices are null at start of spawning");
                    return;
                }

                var questionId = currentQuestion.Id;
                var choices = currentQuestion.Choices;
                var timeToAnswer = currentQuestion.TimeToAnswer;

                DebugLog(
                    $"[AnswerObjects] SpawnAnswerObjects: Question ID: {questionId}, Choices: {choices.Length}, TimeToAnswer: {timeToAnswer}");

                // Validate that we have all required components
                if (answerPrefab == null || _trackManager == null)
                {
                    Debug.LogError(
                        "[AnswerObjects] SpawnAnswerObjects: Missing required components to spawn answer objects");
                    return;
                }

                if (_characterInputController == null)
                {
                    Debug.LogError(
                        "[AnswerObjects] SpawnAnswerObjects: CharacterInputController is null - cannot spawn answer objects");
                    return;
                }

                // Get player position and calculate starting segment
                Vector3 playerPosition = _characterInputController.transform.position;
                float playerZ = playerPosition.z;

                DebugLog($"[AnswerObjects] SpawnAnswerObjects: Player position: {playerPosition}, Player Z: {playerZ}");

                // Calculate minimum time needed to read and answer the question
                float answerTime = (timeToAnswer ?? 0) * firstAnswerTimeToAnswerMultiplier;
                float firstAnswerDistance = _trackManager.speed * answerTime;
                float startZ = playerZ + firstAnswerDistance;
                startZ = Mathf.Max(startZ, playerZ + minDistanceFromPlayer);

                DebugLog(
                    $"[AnswerObjects] SpawnAnswerObjects: AnswerTime: {answerTime}, TrackSpeed: {_trackManager.speed}, FirstAnswerDistance: {firstAnswerDistance}, StartZ: {startZ}");

                // Calculate spacing between answers using dynamic distance calculation
                float dynamicDistance = CalculateDynamicDistance(timeToAnswer);
                float totalDistanceNeeded = dynamicDistance * (choices.Length - 1);
                float baseSpacing = choices.Length > 1 ? totalDistanceNeeded / (choices.Length - 1) : dynamicDistance;

                baseSpacing = Mathf.Max(baseSpacing, dynamicMinDistance);

                DebugLog(
                    $"[AnswerObjects] SpawnAnswerObjects: DynamicDistance: {dynamicDistance:F2}, TotalDistanceNeeded: {totalDistanceNeeded:F2}, BaseSpacing: {baseSpacing:F2}, DynamicMinDistance: {dynamicMinDistance:F2}");

                int previousLane = -1;
                float currentWorldZ = startZ;

                DebugLog($"[AnswerObjects] SpawnAnswerObjects: Starting spawn loop for {choices.Length} answers");

                for (int i = 0; i < choices.Length; i++)
                {
                    // Check for cancellation before each spawn
                    if (cancellationToken.IsCancellationRequested)
                    {
                        DebugLog(
                            $"[AnswerObjects] SpawnAnswerObjects: Answer spawning cancelled before spawning answer {i}");
                        return;
                    }

                    DebugLog(
                        $"[AnswerObjects] SpawnAnswerObjects: === Spawning Answer {i}/{choices.Length - 1}: '{choices[i]}' ===");

                    // Add some randomization to spacing for more natural distribution
                    float randomOffset = Random.Range(0, randomAdditionalDistance);
                    if (i > 0) // Don't offset the first answer
                    {
                        currentWorldZ += baseSpacing + randomOffset;
                        DebugLog(
                            $"[AnswerObjects] SpawnAnswerObjects: Answer {i} - Applied spacing: {baseSpacing} + random: {randomOffset} = total offset: {baseSpacing + randomOffset}");
                    }

                    DebugLog($"[AnswerObjects] SpawnAnswerObjects: Answer {i} - Target WorldZ: {currentWorldZ}");

                    // Get a random lane (different from previous for variety)
                    int selectedLane = GetRandomLane(previousLane);

                    // Calculate final position directly using simple world coordinates
                    Vector3 finalPos = new Vector3(
                        (selectedLane - 1) * _trackManager.laneOffset, // X: Lane position
                        0.5f, // Y: Fixed height
                        currentWorldZ // Z: Calculated position
                    );

                    DebugLog(
                        $"[AnswerObjects] SpawnAnswerObjects: Answer {i} - Final position: {finalPos} (Lane: {selectedLane})");

                    // Clear existing objects in the spawn area
                    ClearExistingObjects(finalPos, clearanceRadius);

                    // Spawn the answer object
                    DebugLog($"[AnswerObjects] SpawnAnswerObjects: Answer {i} - Starting async spawn...");
                    var answerObject = await AddressablesSafeSpawner.SpawnAsync<AnswerObject>(answerPrefab,
                        finalPos, Quaternion.identity);

                    // Check for cancellation after async spawn
                    if (cancellationToken.IsCancellationRequested)
                    {
                        DebugLog(
                            $"[AnswerObjects] SpawnAnswerObjects: Answer spawning cancelled after spawning answer object {i}, cleaning up");
                        if (answerObject != null && answerObject.gameObject != null)
                        {
                            Addressables.ReleaseInstance(answerObject.gameObject);
                        }

                        return;
                    }

                    if (answerObject != null)
                    {
                        DebugLog(
                            $"[AnswerObjects] SpawnAnswerObjects: Answer {i} - Spawn successful, configuring object...");

                        // Set the answer data
                        answerObject.Repaint(choices[i]);

                        // Set rotation and position
                        answerObject.transform.rotation = Quaternion.identity;
                        answerObject.transform.position = finalPos;

                        _spawnedAnswers.Add(answerObject);
                        previousLane = selectedLane;

                        DebugLog(
                            $"[AnswerObjects] SpawnAnswerObjects: ✓ Answer {i} '{choices[i]}' successfully placed at lane {selectedLane}, world position {answerObject.transform.position}");
                    }
                    else
                    {
                        DebugLogWarning($"[AnswerObjects] SpawnAnswerObjects: Failed to spawn answer object {i}");
                    }
                }

                DebugLog(
                    $"[AnswerObjects] SpawnAnswerObjects: Answer object spawning completed successfully. Total spawned: {_spawnedAnswers.Count}");

                // Clear obstacles in the entire span between first and last answer
                if (_spawnedAnswers.Count > 1)
                {
                    Vector3 firstAnswerPos = _spawnedAnswers[0].transform.position;
                    Vector3 lastAnswerPos = _spawnedAnswers[^1].transform.position;
                    ClearObstaclesInSpan(firstAnswerPos, lastAnswerPos, obstacleClearanceBuffer);
                }

                // Log final positions of all spawned answers
                if (enableAnswerObjectDebugLogs)
                {
                    for (int j = 0; j < _spawnedAnswers.Count; j++)
                    {
                        if (_spawnedAnswers[j] != null)
                        {
                            var answerObj = _spawnedAnswers[j].GetComponent<AnswerObject>();
                            string answerText = answerObj?.Answer.ToString() ?? "Unknown";
                            Debug.Log(
                                $"[AnswerObjects] SpawnAnswerObjects: Final Answer {j}: '{answerText}' at {_spawnedAnswers[j].transform.position}");
                        }
                    }
                }

                _missCheckStatus = MissCheckStatus.Ready;
            }
            catch (System.OperationCanceledException)
            {
                DebugLog("[AnswerObjects] SpawnAnswerObjects: Answer spawning was cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[AnswerObjects] SpawnAnswerObjects: Error during answer spawning: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }

            DebugLog(
                $"[AnswerObjects] SpawnAnswerObjects: Set miss check status to Ready, spawned {_spawnedAnswers.Count} answers");
        }

        protected override void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission)
        {
            DebugLog($"[AnswerObjects] ProcessOnQuestionEnded: Question ended with result: {userAnswerSubmission}");

            // Cancel any ongoing spawning operations
            _spawningCancellationTokenSource?.Cancel();
            CleanUp();
            _missCheckStatus = MissCheckStatus.None;
            if (enableAnswerObjectDebugLogs)
                Debug.Log("[AnswerObjects] ProcessOnQuestionEnded: Reset miss check status to None");
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

            return QuestionHandlerResult.CreateSuccess(question);
        }

        /// <summary>
        /// Calculates dynamic distance between answers based on question TimeToAnswer value
        /// Maps TimeToAnswer between MinFluencyTimer and MaxFluencyTimer to dynamicMinDistance and dynamicMaxDistance
        /// </summary>
        /// <param name="timeToAnswer">The question's time to answer</param>
        /// <returns>Calculated dynamic distance</returns>
        private float CalculateDynamicDistance(float? timeToAnswer)
        {
            if (!timeToAnswer.HasValue)
            {
                return minDistanceBetweenAnswers;
            }

            var config = QuestionProvider?.Config;
            if (config == null)
            {
                DebugLogWarning("[AnswerObjects] CalculateDynamicDistance: Config is null, using static distance");
                return minDistanceBetweenAnswers;
            }

            float minTimer = config.MinFluencyTimer;
            float maxTimer = config.MaxFluencyTimer;
            float currentTime = timeToAnswer.Value;

            // Validate timer range
            if (minTimer >= maxTimer)
            {
                DebugLogWarning($"[AnswerObjects] CalculateDynamicDistance: Invalid timer range (min: {minTimer}, max: {maxTimer}), using static distance");
                return minDistanceBetweenAnswers;
            }

            // Clamp the time value to the expected range
            currentTime = Mathf.Clamp(currentTime, minTimer, maxTimer);

            // Calculate the normalized position (0 to 1) within the timer range
            float normalizedTime = (currentTime - minTimer) / (maxTimer - minTimer);

            // Map to distance range - shorter times get smaller distances (more challenging)
            float dynamicDistance = Mathf.Lerp(dynamicMinDistance, dynamicMaxDistance, normalizedTime);

            // Ensure result is within reasonable bounds
            dynamicDistance = Mathf.Max(dynamicDistance, dynamicMinDistance);

            DebugLog($"[AnswerObjects] CalculateDynamicDistance: TimeToAnswer: {timeToAnswer}, MinTimer: {minTimer}, MaxTimer: {maxTimer}, Normalized: {normalizedTime:F2}, DynamicDistance: {dynamicDistance:F2}");

            return dynamicDistance;
        }

        /// <summary>
        /// Clears obstacles in a larger area spanning from first to last answer position
        /// </summary>
        /// <param name="startPosition">Position of first answer</param>
        /// <param name="endPosition">Position of last answer</param>
        /// <param name="extraBuffer">Additional buffer distance beyond start/end</param>
        private void ClearObstaclesInSpan(Vector3 startPosition, Vector3 endPosition, float extraBuffer)
        {
            float startZ = Mathf.Min(startPosition.z, endPosition.z) - extraBuffer;
            float endZ = Mathf.Max(startPosition.z, endPosition.z) + extraBuffer;
            float spanLength = endZ - startZ;
            Vector3 centerPosition = new Vector3(0, startPosition.y, (startZ + endZ) / 2f);

            DebugLog($"[AnswerObjects] ClearObstaclesInSpan: Clearing from Z:{startZ:F1} to Z:{endZ:F1} (length: {spanLength:F1}) with center at {centerPosition}");

            // Use a capsule-like approach: clear multiple overlapping spheres along the path
            int numClearancePoints = Mathf.CeilToInt(spanLength / clearanceRadius) + 1;
            for (int i = 0; i < numClearancePoints; i++)
            {
                float t = numClearancePoints > 1 ? (float)i / (numClearancePoints - 1) : 0f;
                float currentZ = Mathf.Lerp(startZ, endZ, t);
                Vector3 clearanceCenter = new Vector3(0, startPosition.y, currentZ);
                
                ClearExistingObjects(clearanceCenter, clearanceRadius);
            }
        }
    }
}