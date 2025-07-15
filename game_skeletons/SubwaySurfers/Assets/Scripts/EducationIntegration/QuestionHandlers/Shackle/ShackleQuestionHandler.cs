using System.Linq;
using Consumables;
using Cysharp.Threading.Tasks;
using EducationIntegration.QuestionHandlers;
using FluencySDK;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SubwaySurfers.Runtime
{
    /// <summary>
    /// Handles the shackle debuff mechanics including question timing, obstacle detection,
    /// and shackle application/removal. Works with existing FluencySDK and Shackle consumable.
    /// </summary>
    public class ShackleQuestionHandler : BaseQuestionHandler, IQuestionGameplayHandler
    {
        public override string HandlerIdentifier => "shackle_questions";

        [Header("Shackle Settings")] [Tooltip("Asset reference to the shackle prefab")] [SerializeField]
        private AssetReference _shacklePrefabReference;

        [Tooltip("How much time variance is allowed when showing a question (0.2 = 20%)")] [SerializeField]
        private float _timeVariancePercentage = 0.2f;

        // State tracking
        private Consumable _restrictionConsumable = null;

        // References
        private TrackManager _trackManager;
        private CharacterInputController _characterController;
        private ObstacleDistanceTracker _obstacleTracker;
        private CharacterCollider _characterCollider;
        private ILootablesSpawner _powerUpSpawner;

        protected override void Initialize()
        {
            base.Initialize();
            _trackManager = FindFirstObjectByType<TrackManager>(FindObjectsInactive.Include);
            _characterController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            _characterCollider = FindFirstObjectByType<CharacterCollider>(FindObjectsInactive.Include);
            _powerUpSpawner = FindFirstObjectByType<LootablesSpawner>(FindObjectsInactive.Include);
            _obstacleTracker = FindFirstObjectByType<ObstacleDistanceTracker>(FindObjectsInactive.Include);
            Debug.Log(
                $"[ShackleQuestionHandler] Awake - TrackManager found: {_trackManager != null}, CharacterController found: {_characterController != null}");
            Debug.Log($"[ShackleQuestionHandler] ShacklePrefabReference set: {_shacklePrefabReference != null}");
            Debug.Log($"[ShackleQuestionHandler] CharacterCollider found: {_characterCollider != null}");
            Debug.Log($"[ShackleQuestionHandler] PowerUpSpawner found: {_powerUpSpawner != null}");
            Debug.Log($"[ShackleQuestionHandler] ObstacleTracker found: {_obstacleTracker != null}");
        }

        protected override void DoSubscribeToEvents()
        {
            base.DoSubscribeToEvents();
            if (_characterCollider != null)
            {
                Debug.Log("[ShackleQuestionHandler] Subscribing to OnObstacleHit");
                _characterCollider.OnObstacleHit += OnObstacleHit;
            }
            else
            {
                Debug.LogError("[ShackleQuestionHandler] CharacterCollider is null! Cannot subscribe to OnObstacleHit");
            }
        }

        protected override void DoUnsubscribeFromEvents()
        {
            base.DoUnsubscribeFromEvents();
            if (_characterCollider != null)
            {
                Debug.Log("[ShackleQuestionHandler] Unsubscribing from OnObstacleHit");
                _characterCollider.OnObstacleHit -= OnObstacleHit;
            }
        }

        protected override void ProcessOnQuestionStarted()
        {
            Debug.Log("[ShackleQuestionHandler] Question started, triggering shackle sequence");
            StartShackleSequence().Forget();
        }

        protected override void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission)
        {
            // If the answer is correct, remove the debuff
            if (userAnswerSubmission.AnswerType == AnswerType.Correct)
            {
                Debug.Log("[ShackleQuestionHandler] Correct answer, removing shackle debuff");
                RemoveShackleDebuff();
            }
            else
            {
                Debug.Log($"[ShackleQuestionHandler] {userAnswerSubmission} answer, shackle debuff remains until obstacle hit");
            }
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

            // Get distance to next obstacle
            GameObject nextObstacle = _obstacleTracker.GetNextObstacle();

            if (nextObstacle == null)
            {
                return QuestionHandlerResult.CreateError(question, "No obstacle found for shackle timing.");
            }

            float distanceToObstacle = _obstacleTracker.GetDistanceToNextObstacle();

            // Calculate time to reach obstacle based on current speed
            float timeToObstacle = distanceToObstacle / _trackManager.speed;

            // Calculate the allowed variance range
            float minTimeWindow = (question.TimeToAnswer ?? 0) * (1 - _timeVariancePercentage);
            float maxTimeWindow = (question.TimeToAnswer ?? 0) * (1 + _timeVariancePercentage);

            if (timeToObstacle < minTimeWindow || timeToObstacle > maxTimeWindow)
            {
                return QuestionHandlerResult.CreateError(question, $"Obstacle timing ({timeToObstacle:F1}s) doesn't match question window ({minTimeWindow:F1}s - {maxTimeWindow:F1}s).");
            }

            return QuestionHandlerResult.CreateSuccess(question);
        }

        protected override bool DoHandleQuestion(IQuestion question)
        {
            QuestionProvider.StartQuestion(question).Forget();
            return true;
        }

        /// <summary>
        /// Removes the shackle debuff from the player
        /// </summary>
        private void RemoveShackleDebuff()
        {
            if (_restrictionConsumable == null)
            {
                return;
            }

            Debug.Log("[ShackleQuestionHandler] Removing shackle debuff");

            // Mark the shackle as inactive to trigger its Ended method
            _restrictionConsumable.ForceEnd();
            _restrictionConsumable = null;
        }

        /// <summary>
        /// Called when the player hits an obstacle, to clean up any shackle debuff
        /// </summary>
        private void OnObstacleHit(string source)
        {
            if (IsQuestionStarted == false)
            {
                return;
            }

            // Pick a wrong answer intentionally
            Debug.Log("[ShackleQuestionHandler] Obstacle hit detected, skipping question and removing shackle debuff");
            // TODO, decide in this case what would happen with the question, previously we used to submit a wrong answer + remove shackle
        }

        /// <summary>
        /// Asynchronously instantiates and applies the shackle consumable
        /// </summary>
        private async UniTask StartShackleSequence()
        {
            // If the shackle reference is not set, log an error and return
            if (_shacklePrefabReference == null)
            {
                Debug.LogError("[ShackleQuestionHandler] Shackle prefab reference is not set!");
                return;
            }

            Debug.Log("[ShackleQuestionHandler] Instantiating shackle prefab");
            _restrictionConsumable = await _powerUpSpawner.SpawnAsync<Consumable>(_shacklePrefabReference, Vector3.one * 9999,
                Quaternion.identity);

            if (_restrictionConsumable == null)
            {
                return;
            }

            // Apply the shackle directly to the character
            if (_characterController != null)
            {
                _characterController.UseConsumable(_restrictionConsumable);
                Debug.Log("[ShackleQuestionHandler] Shackle applied to character successfully");
            }
            else
            {
                Debug.LogError("[ShackleQuestionHandler] CharacterController is null, cannot apply shackle!");
            }
        }
    }
}