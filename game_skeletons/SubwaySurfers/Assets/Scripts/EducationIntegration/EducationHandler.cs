using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using FluencySDK;
using FluencySDK.Unity;
using FluencySDK.Unity.Data;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;
using FluencySDK.Data;
using ReusablePatterns.SharedCore.Scripts.Runtime.Debugging;
using SubwaySurfers;

namespace EducationIntegration
{
    public class EducationHandler : MonoBehaviour, IQuestionHandlerFactory, IDebugInfoProvider
    {
        [SerializeField] private UnityFluencyGeneratorConfigDescriptor fluencySDKConfigDescriptor;
        [SerializeField] private FluencyAudioConfig fluencyAudioConfig;
        [SerializeField] private QuestionPresentationConfiguration questionPresentationConfiguration;

        private IQuestionProvider _questionProvider;
        private readonly HashSet<IQuestionGameplayHandler> _registeredQuestionHandlers = new();

        private readonly IList<IQuestionGameplayHandler> _validHandlersBuffer = new List<IQuestionGameplayHandler>();
        private bool _isInitialized = false;

        private bool _isEnabled;
        private LearningAlgorithmConfig _currentConfig;

        // Question queuing system
        private readonly Queue<IQuestion> _questionQueue = new();
        private bool _isProcessingQuestion = false;
        private IQuestionGameplayHandler _currentHandler;

        private IQuestionGameplayHandler
            _assignedHandler; // Handler we've called HandleQuestion on but hasn't started yet

        private readonly StringBuilder _infoBuilder = new(), _lastHandlerSearchInfo = new();

        private IGameManager _gameManager;

        private void Awake()
        {
            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            EnsureInitialized();
        }

        private void OnEnable()
        {
            _isEnabled = true;
            IDebugInfoRegistry.Instance.RegisterProvider(this);
            SubscribeToHandlerEvents();
        }

        private void OnDisable()
        {
            _isEnabled = false;
            IDebugInfoRegistry.Instance.UnregisterProvider(this);
            UnsubscribeFromHandlerEvents();
        }

        private void SubscribeToHandlerEvents()
        {
            IQuestionGameplayHandler.QuestionHandlerStartedEvent += OnQuestionHandlerStarted;
            IQuestionGameplayHandler.QuestionHandlerEndedEvent += OnQuestionHandlerEnded;
            IQuestionGameplayHandler.QuestionHandlerExpiredEvent += OnQuestionHandlerExpired;
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void UnsubscribeFromHandlerEvents()
        {
            IQuestionGameplayHandler.QuestionHandlerStartedEvent -= OnQuestionHandlerStarted;
            IQuestionGameplayHandler.QuestionHandlerEndedEvent -= OnQuestionHandlerEnded;
            IQuestionGameplayHandler.QuestionHandlerExpiredEvent -= OnQuestionHandlerExpired;

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(AState newState)
        {
            if (newState is not GameState)
            {
                _questionProvider.Stop();
            }
        }

        private void OnQuestionHandlerExpired(IQuestionGameplayHandler handler, IQuestion question)
        {
            Debug.Log(
                $"[EducationHandler] Question handler expired: {handler.HandlerIdentifier} for question {question.Id}");
            this._currentHandler = null;
            this._assignedHandler = null; // Clear assigned handler on expiration
            this._isProcessingQuestion = false;
            // return the question to the queue
            this.HandleQuestionReady(question);
        }

        private void OnQuestionHandlerStarted(IQuestionGameplayHandler handler, IQuestion question)
        {
            Debug.Log(
                $"[EducationHandler] Question handler started: {handler.HandlerIdentifier} for question {question.Id}");
            _currentHandler = handler;
            _assignedHandler = null; // Clear assigned handler since it's now officially started
        }

        private void OnQuestionHandlerEnded(IQuestionGameplayHandler handler, IQuestion question,
            UserAnswerSubmission userAnswerSubmission)
        {
            Debug.Log(
                $"[EducationHandler] Question handler ended: {handler.HandlerIdentifier} for question {question.Id}, answer type: {userAnswerSubmission}");

            if (_currentHandler == handler)
            {
                // Wait for the handler's post-answer presentation time before processing next question
                WaitForFeedbackTimeAndProcessNext(questionPresentationConfiguration.PostAnswerPresentationTime)
                    .Forget();
            }
        }

        private async UniTaskVoid WaitForFeedbackTimeAndProcessNext(float feedbackTime)
        {
            Debug.Log(
                $"[EducationHandler] Waiting {feedbackTime} seconds for feedback time before processing next question");

            // Wait for the feedback time
            await UniTask.WaitForSeconds(feedbackTime, ignoreTimeScale: true);

            // Reset current handler and processing state
            _currentHandler = null;
            _assignedHandler = null; // Clear assigned handler as well
            _isProcessingQuestion = false;

            Debug.Log(
                $"[EducationHandler] Feedback time completed. Processing next question in queue. Queue count: {_questionQueue.Count}");

            // Process next question in queue
            ProcessNextQuestionInQueue();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            Debug.Log("[EducationHandler] Awake: Initializing Education Handler");

            _currentConfig = fluencySDKConfigDescriptor.GetConfig();

            _questionProvider = BaseQuestionProvider.Instance;
            if (_questionProvider == null)
            {
                Debug.LogError(
                    "[EducationHandler] QuestionGenerator not found in the scene. EducationHandler requires a QuestionGenerator.");
            }
            else
            {
                Debug.Log("[EducationHandler] QuestionGenerator found: " + _questionProvider.GetType().Name);
            }

            // Find the game state
            var gameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (gameState == null)
            {
                Debug.LogError(
                    "[EducationHandler] GameState not found in the scene. EducationHandler requires GameState for pausing functionality.");
            }
            else
            {
                Debug.Log("[EducationHandler] GameState found successfully");
            }

            // Initialize FluencySDK
            if (_questionProvider != null && gameState != null)
            {
                Debug.Log($"[EducationHandler] Initializing QuestionGenerator with FluencySDK");

                _questionProvider.Initialize(gameState, _currentConfig, fluencyAudioConfig);
                _questionProvider.OnQuestionReady += HandleQuestionReady;
                Debug.Log("[EducationHandler] QuestionGenerator initialized and OnQuestionReady event subscribed");
            }
            else
            {
                Debug.LogError("[EducationHandler] Failed to initialize QuestionGenerator due to missing dependencies");
            }

            _isInitialized = true;
        }

        public void RegisterHandler(IQuestionGameplayHandler handler)
        {
            EnsureInitialized();
            if (handler == null)
            {
                Debug.LogError("[EducationHandler] Handler is null. Cannot register.");
                return;
            }

            Debug.Log($"[EducationHandler] Registering handler: {handler.HandlerIdentifier}");
            if (!_registeredQuestionHandlers.Add(handler))
            {
                Debug.LogWarning($"[EducationHandler] Handler {handler.HandlerIdentifier} already registered.");
                return;
            }

            Debug.Log(
                $"[EducationHandler] Handler {handler.HandlerIdentifier} registered successfully. Total handlers: {_registeredQuestionHandlers.Count}");
        }

        public void UnregisterHandler(IQuestionGameplayHandler handler)
        {
            if (handler == null)
            {
                Debug.LogError("[EducationHandler] Handler is null. Cannot unregister.");
                return;
            }

            if (!_registeredQuestionHandlers.Contains(handler))
            {
                Debug.LogWarning(
                    $"[EducationHandler] Handler {handler.HandlerIdentifier} not found in registered handlers.");
                return;
            }

            _registeredQuestionHandlers.Remove(handler);

            Debug.Log(
                $"[EducationHandler] Handler {handler.HandlerIdentifier} unregistered successfully. Remaining handlers: {_registeredQuestionHandlers.Count}");
        }

        private void ProcessNextQuestionInQueue()
        {
            // If already processing a question or queue is empty, return
            if (_isProcessingQuestion || _questionQueue.Count == 0)
            {
                return;
            }

            var question = _questionQueue.Dequeue();
            Debug.Log(
                $"[EducationHandler] Dequeued question {question.Id} for processing. Remaining in queue: {_questionQueue.Count}");

            _isProcessingQuestion = true;
            TryAssignQuestionToHandler(question).Forget();
        }

        private async UniTaskVoid TryAssignQuestionToHandler(IQuestion question)
        {
            while (_isEnabled && _isProcessingQuestion)
            {
                await UniTask.DelayFrame(1);

                _lastHandlerSearchInfo.Clear();
                _validHandlersBuffer.Clear();

                if (_registeredQuestionHandlers.Count == 0)
                {
                    _lastHandlerSearchInfo.AppendLine("No handlers registered");
                    continue;
                }

                foreach (var handler in _registeredQuestionHandlers)
                {
                    var checkResult = handler.CanHandleQuestionNow(question);

                    if (checkResult.Success)
                    {
                        _validHandlersBuffer.Add(handler);
                        _lastHandlerSearchInfo.AppendLine(
                            $"Handler {handler.HandlerIdentifier} can handle question: {question.Id}");
                    }
                    else
                    {
                        _lastHandlerSearchInfo.AppendLine(
                            $"Handler {handler.HandlerIdentifier} cannot handle question: {checkResult.Error}");
                    }
                }

                _lastHandlerSearchInfo.AppendLine(
                    $"Total valid handlers found: {_validHandlersBuffer.Count}");

                if (_validHandlersBuffer.Count == 0)
                {
                    continue;
                }

                // Choose a random handler from the valid ones
                int randomIndex = Random.Range(0, _validHandlersBuffer.Count);
                var selectedHandler = _validHandlersBuffer[randomIndex];
                var handleResult = selectedHandler.HandleQuestion(question);
                if (handleResult.Success == false)
                {
                    _lastHandlerSearchInfo.AppendLine(
                        $"Handler {selectedHandler.HandlerIdentifier} failed to handle question: {handleResult.Error}");
                    continue;
                }

                _assignedHandler = selectedHandler;
                break;
            }
        }

        /// <summary>
        /// Handles when a question is ready to be presented
        /// </summary>
        private void HandleQuestionReady(IQuestion question)
        {
            Debug.Log($"[EducationHandler] Question ready event received for ID: {question.Id}");

            // Add question to queue
            _questionQueue.Enqueue(question);
            Debug.Log($"[EducationHandler] Question {question.Id} added to queue. Queue count: {_questionQueue.Count}");

            // Try to process the question if not currently processing one
            ProcessNextQuestionInQueue();
        }

        public string DebugGroupName => "Education";
        public string DebugTitle => "Education Handler";

        public string GetDebugInfo()
        {
            if (_isInitialized == false)
            {
                return "EducationHandler is not initialized.";
            }

            if (_isEnabled == false)
            {
                return "EducationHandler is not enabled.";
            }

            _infoBuilder.Clear();
            _infoBuilder.AppendLine($"- Registered Handlers: {_registeredQuestionHandlers.Count}");
            _infoBuilder.AppendLine($"- Question Queue Count: {_questionQueue.Count}");
            _infoBuilder.AppendLine($"- Is Processing Question: {_isProcessingQuestion}");
            _infoBuilder.AppendLine($"- Assigned Handler: {_assignedHandler?.HandlerIdentifier ?? "None"}");
            _infoBuilder.AppendLine($"- Current Handler: {_currentHandler?.HandlerIdentifier ?? "None"}");

            if (_lastHandlerSearchInfo.Length > 0)
            {
                _infoBuilder.Append(_lastHandlerSearchInfo);
            }

            return _infoBuilder.ToString();
        }
    }
}