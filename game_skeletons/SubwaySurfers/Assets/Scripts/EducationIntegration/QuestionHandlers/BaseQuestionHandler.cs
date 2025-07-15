using FluencySDK;
using FluencySDK.Unity;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using UnityEngine;
using SubwaySurfers;
using Cysharp.Threading.Tasks;
using EducationIntegration.QuestionResultProcessor;
using ReusablePatterns.FluencySDK.Enums;
using FluencySDK.Data;
using System.Linq;
using ReusablePatterns.SharedCore.Runtime.PauseSystem;

namespace EducationIntegration.QuestionHandlers
{
    /// <summary>
    /// Base class for all question handlers.
    /// Configure behavior using the Flags property
    /// </summary>
    public abstract class BaseQuestionHandler : MonoBehaviour, IQuestionGameplayHandler
    {
        public abstract string HandlerIdentifier { get; }
        [SerializeField] private QuestionPresentationConfiguration questionPresentationConfiguration;
        [field: SerializeField] public QuestionPresentationType QuestionPresentationType { get; private set; }

        [field: SerializeField]
        public QuestionHandlerFlags Flags { get; private set; } = QuestionHandlerFlags.IsEnabled;

        // Learning mode this handler accepts
        [field: SerializeField]
        public LearningMode[] AcceptedLearningModes { get; private set; } = { LearningMode.Assessment };

        protected bool IsEnabled => Flags.HasFlag(QuestionHandlerFlags.IsEnabled);
        protected bool PauseGameWhenQuestionIsActive => Flags.HasFlag(QuestionHandlerFlags.PauseTheGame);
        protected bool DisablePauseCountdown => Flags.HasFlag(QuestionHandlerFlags.DisablePauseCountdown);
        protected bool WorksDuringFinishedGame => Flags.HasFlag(QuestionHandlerFlags.WorksDuringFinishedGame);

        protected bool ProcessResultAfterPresentation =>
            Flags.HasFlag(QuestionHandlerFlags.ProcessResultAfterPresentation);

        private IQuestionHandlerFactory _questionHandlerFactory;
        protected IQuestionProvider QuestionProvider { get; private set; }
        protected IGameState GameState { get; private set; }
        protected IGamePauser PauseManager { get; private set; }

        private IGameManager _gameManager;
        protected IQuestionResultProcessor QuestionResultProcessor { get; set; }

        protected IQuestion Question { get; private set; }

        protected bool IsQuestionStarted { get; private set; }

        private bool _subscribedToEvents, _initialized;

        protected virtual void Initialize()
        {
            _questionHandlerFactory = FindFirstObjectByType<EducationHandler>(FindObjectsInactive.Include);
            if (_questionHandlerFactory == null)
            {
                Debug.LogError("No IQuestionHandlerFactory found in the scene.");
                return;
            }

            GameState = FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (GameState == null)
            {
                Debug.LogError("No GameState found in the scene.");
                return;
            }

            PauseManager = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
            if (PauseManager == null)
            {
                Debug.LogError("No PauseManager found in the scene.");
                return;
            }

            _gameManager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

            // Find question result processor
            QuestionResultProcessor =
                FindFirstObjectByType<StreakBasedQuestionResultProcessor>(FindObjectsInactive.Include);

            _questionHandlerFactory.RegisterHandler(this);
            QuestionProvider = BaseQuestionProvider.Instance;
        }

        private void Awake()
        {
            Initialize();
            _initialized = true;
            SubscribeToEvents();
        }

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        protected virtual void DoSubscribeToEvents()
        {
            Debug.Log($"[{this.GetType().Name}]: Subscribing to events");
            if (QuestionProvider != null)
            {
                Debug.Log($"[{this.GetType().Name}]: Subscribing to QuestionProvider events");
                QuestionProvider.OnQuestionStarted += OnQuestionStarted;
                QuestionProvider.OnQuestionEnded += OnQuestionEnded;
            }

            if (GameState != null)
            {
                GameState.OnGameFinished += OnGameFinished;
            }

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        protected virtual void DoUnsubscribeFromEvents()
        {
            Debug.Log($"[{this.GetType().Name}]: Unsubscribing from events");
            if (QuestionProvider != null)
            {
                QuestionProvider.OnQuestionStarted -= OnQuestionStarted;
                QuestionProvider.OnQuestionEnded -= OnQuestionEnded;
            }

            if (GameState != null)
            {
                GameState.OnGameFinished -= OnGameFinished;
            }

            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void SubscribeToEvents()
        {
            if (_subscribedToEvents || _initialized == false)
            {
                return;
            }

            DoSubscribeToEvents();

            _subscribedToEvents = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (_subscribedToEvents == false)
            {
                return;
            }

            DoUnsubscribeFromEvents();

            _subscribedToEvents = false;
        }

        private void OnGameStateChanged(AState state)
        {
            if (state is not global::GameState)
            {
                HandleInterruptedHandler();
            }
        }

        private void OnGameFinished()
        {
            HandleInterruptedHandler();
        }

        private void HandleInterruptedHandler()
        {
            if (this.Question == null)
            {
                return;
            }

            if (this.IsQuestionStarted)
            {
                QuestionProvider.SubmitAnswer(this.Question, UserAnswerSubmission.FromSkipped());
            }
            else
            {
                var question = this.Question;
                this.Question = null;
                (this as IQuestionGameplayHandler).NotifyHandlerQuestionExpired(question);
            }
        }

        protected abstract void ProcessOnQuestionStarted();
        protected abstract void ProcessOnQuestionEnded(UserAnswerSubmission userAnswerSubmission);

        private void OnQuestionStarted(IQuestion question)
        {
            if (question?.Id == this.Question?.Id)
            {
                Debug.Log($"Question started: {question.Id}");
                IsQuestionStarted = true;

                // Pause the game if needed
                if (PauseGameWhenQuestionIsActive)
                {
                    PauseManager.Pause(new PauseData()
                    {
                        animateCharacter = false,
                        displayMenu = false,
                        resumeWithCountdown = !DisablePauseCountdown,
                    });
                }

                ProcessOnQuestionStarted();
                (this as IQuestionGameplayHandler).NotifyHandlerQuestionStarted(this.Question);
            }
        }

        private void OnQuestionEnded(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            if (question?.Id == this.Question?.Id)
            {
                Debug.Log($"Question ended: {question.Id}, Answer Type: {userAnswerSubmission}");
                IsQuestionStarted = false;
                ProcessOnQuestionEnded(userAnswerSubmission);
                Question = null;

                (this as IQuestionGameplayHandler).NotifyHandlerQuestionEnded(question, userAnswerSubmission);
                PostEndAsync(question, userAnswerSubmission).Forget();
            }
        }

        private async UniTaskVoid PostEndAsync(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            void TryProcessResult()
            {
                if (QuestionResultProcessor != null)
                {
                    QuestionResultProcessor.ProcessQuestionResult(question, userAnswerSubmission);
                }
                else
                {
                    Debug.LogWarning("[BaseQuestionHandler] No question result processor found");
                }
            }

            if (ProcessResultAfterPresentation == false)
            {
                TryProcessResult();
            }

            await UniTask.WaitForSeconds(questionPresentationConfiguration.PostAnswerPresentationTime,
                ignoreTimeScale: true);
            // Resume the game if needed
            if (PauseGameWhenQuestionIsActive)
            {
                PauseManager.Resume();
            }

            if (ProcessResultAfterPresentation)
            {
                TryProcessResult();
            }
        }

        protected abstract bool DoHandleQuestion(IQuestion question);

        public QuestionHandlerResult HandleQuestion(IQuestion question)
        {
            if (question == null)
            {
                Debug.LogError("Question is null.");
                return QuestionHandlerResult.CreateError(question, "Question is null.");
            }

            if (Question != null)
            {
                Debug.LogWarning("A question is already being handled.");
                return QuestionHandlerResult.CreateError(question, "A question is already being handled.");
            }

            Question = question;

            bool result = DoHandleQuestion(question);

            if (result == false)
            {
                Question = null;
                return QuestionHandlerResult.CreateError(question, "Handler failed to process the question.");
            }

            return QuestionHandlerResult.CreateSuccess(question);
        }

        public virtual QuestionHandlerResult CanHandleQuestionNow(IQuestion question)
        {
            if (!IsEnabled)
            {
                return QuestionHandlerResult.CreateError(question, "Handler is not enabled.");
            }

            if (AcceptedLearningModes.Contains(question.LearningMode) == false)
            {
                return QuestionHandlerResult.CreateError(question,
                    $"Handler accepts {string.Join(", ", AcceptedLearningModes)} mode but question is {question.LearningMode} mode.");
            }

            if (GameState.IsFinished && !WorksDuringFinishedGame)
            {
                return QuestionHandlerResult.CreateError(question,
                    "Game is finished and handler doesn't work during finished game.");
            }

            return QuestionHandlerResult.CreateSuccess(question);
        }
    }
}