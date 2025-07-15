using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FluencySDK.Events;
using UnityEngine;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;
using ReusablePatterns.FluencySDK.Enums;
using FluencySDK.Unity.Data;

namespace FluencySDK.Unity
{
    /// <summary>
    /// Handles question generation using the FluencySDK without any UI dependencies
    /// </summary>
    public class BaseQuestionProvider : IQuestionProvider, IQuestionGenerationGateRegistry
    {
        private static readonly TimeSpan PeriodicCheckInterval = TimeSpan.FromMilliseconds(20);

        private ILearningAlgorithm _generator;
        public ILearningAlgorithm Algorithm => _generator;
        private LearningAlgorithmConfig _config;
        private readonly List<IQuestionGenerationGate> _questionGenerationGates = new List<IQuestionGenerationGate>();

        // Events
        public event QuestionStartedEventArgs OnQuestionStarted;
        public event QuestionEndedEventArgs OnQuestionEnded; // bool parameter indicates if answer was correct

        public event QuestionReadyEventArgs OnQuestionReady;

        public event QuestionAnswerSubmitEventArgs OnQuestionAnswerSubmitAttempted;

        private static BaseQuestionProvider _instance;

        public static BaseQuestionProvider Instance
        {
            get { return _instance ??= new BaseQuestionProvider(); }
        }

        private IQuestion _currentQuestion;
        private double _lastQuestionEndTime = 0;
        private float _currentQuestionInterval = 5;

        private CancellationTokenSource _engineCts;


        private ISDKTimeProvider _timeProvider;
        private IQuestionModifierFactory _questionModifierFactory;

        public QuestionGenerationMode QuestionGenerationMode =>
            _timeProvider == null ? QuestionGenerationMode.None : _timeProvider.QuestionMode;

        public StudentState StudentState => _generator?.StudentState;

        /// <summary>
        /// Exposes the current learning algorithm configuration
        /// </summary>
        public LearningAlgorithmConfig Config => _config;

        private BaseQuestionProvider()
        {
        }

        /// <summary>
        /// Registers a question generation gate that can block question generation
        /// </summary>
        /// <param name="gate">The gate to register</param>
        public void RegisterQuestionGenerationGate(IQuestionGenerationGate gate)
        {
            if (gate != null && _questionGenerationGates.Contains(gate) == false)
            {
                _questionGenerationGates.Add(gate);
                Debug.Log($"[BaseQuestionProvider] Registered question generation gate: {gate.GateIdentifier}");
            }
        }

        /// <summary>
        /// Unregisters a question generation gate
        /// </summary>
        /// <param name="gate">The gate to unregister</param>
        public void UnregisterQuestionGenerationGate(IQuestionGenerationGate gate)
        {
            if (gate != null && _questionGenerationGates.Remove(gate))
            {
                Debug.Log($"[BaseQuestionProvider] Unregistered question generation gate: {gate.GateIdentifier}");
            }
        }

        public void Initialize(ISDKTimeProvider timeProvider, LearningAlgorithmConfig config,
            FluencyAudioConfig fluencyAudioConfig)
        {
            _questionModifierFactory = new QuestionAudioQuestionModifierFactory();
            var audioQuestionModifier = new QuestionAudioQuestionModifier(fluencyAudioConfig);
            _questionModifierFactory.RegisterModifier(audioQuestionModifier);

            if (_timeProvider != null)
            {
                _timeProvider.OnTimeRestarted -= OnTimeRestarted;
            }

            _timeProvider = timeProvider;
            _timeProvider.OnTimeRestarted += OnTimeRestarted;
            _config = config;

            _currentQuestionInterval = (_config.MinQuestionInterval + _config.MaxQuestionInterval) / 2;
            _generator = LearningAlgorithmFactory.CreateAlgorithm(_config);
            FinalizeInitialization().Forget();
        }

        private async UniTaskVoid FinalizeInitialization()
        {
            try
            {
                await _generator.Initialize();
                FluencySDKEventBus.RaiseFluencySDKReady();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Start()
        {
            _engineCts?.Cancel();
            _engineCts = new CancellationTokenSource();
            StartPeriodicQuestionPublishingAsync(_engineCts.Token).Forget();
        }

        public void Stop()
        {
            _engineCts?.Cancel();
        }

        private async UniTaskVoid StartPeriodicQuestionPublishingAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.Delay(PeriodicCheckInterval, ignoreTimeScale: true, cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (_currentQuestion != null)
                {
                    continue;
                }

                var currentCanShowQuestionError = GetCanShowQuestionError();
                if (string.IsNullOrWhiteSpace(currentCanShowQuestionError) == false)
                {
                    continue;
                }

                var question = await GenerateQuestionAsync();
                cancellationToken.ThrowIfCancellationRequested();
                if (question != null)
                {
                    _currentQuestion = question;
                    OnQuestionReady?.Invoke(_currentQuestion);
                    Debug.Log($"[BaseQuestionProvider] Question ready: {question.Id}");
                }
            }
        }

        private async UniTask<IQuestion> GenerateQuestionAsync()
        {
            var question = await _generator.GetNextQuestion();
            if(question != null)
            {
                _questionModifierFactory.ModifyQuestion(question);
            }
            return question;
        }

        /// <summary>
        /// Submits an answer to the current question
        /// </summary>
        /// <param name="question">The question</param>
        /// <param name="userAnswerSubmission">The user's answer</param>
        public void SubmitAnswer(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            // Stop the timer immediately when answer is submitted
            question.Timer?.StopTimer();
            SubmitAnswerAsync(question, userAnswerSubmission).Forget();
        }

        public async UniTask StartQuestion(IQuestion question)
        {
            if (question == null)
            {
                return;
            }

            await this._generator.StartQuestion(question);

            // Create and start timer if question has TimeToAnswer
            if (question.TimeToAnswer.HasValue && QuestionGenerationMode == QuestionGenerationMode.NoBreaks)
            {
                question.Timer = new QuestionTimer(question);
                question.Timer.OnTimerExpired += HandleTimerExpired;
                question.Timer.StartTimer(question.TimeToAnswer.Value);
            }

            OnQuestionStarted?.Invoke(question);
        }

        private async UniTaskVoid SubmitAnswerAsync(IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            var result = await _generator.SubmitAnswer(question, userAnswerSubmission);

            OnQuestionAnswerSubmitAttempted?.Invoke(question, result);

            if (result.ShouldRetry)
            {
                return;
            }

            _lastQuestionEndTime = GetCurrentTime();

            // Clean up timer subscription
            if (question.Timer != null)
            {
                question.Timer.OnTimerExpired -= HandleTimerExpired;
            }

            OnQuestionEnded?.Invoke(question, userAnswerSubmission);

            _currentQuestionInterval = Math.Max(
                _config.MinQuestionInterval,
                Math.Min(_config.MaxQuestionInterval, result.TimeToNextQuestion)
            );

            _currentQuestion = null;
        }

        public string GetCanShowQuestionError()
        {
            if (_timeProvider == null)
            {
                return "Time provider is not initialized.";
            }

            if (QuestionGenerationMode == QuestionGenerationMode.None)
            {
                return "SDK user question mode is not set.";
            }
            
            foreach (var generationGate in _questionGenerationGates)
            {
                if (generationGate.CanGenerateNextQuestion == false)
                {
                    return $"Question generation blocked by gate: {generationGate.GateIdentifier}";
                }
            }

            if (QuestionGenerationMode == QuestionGenerationMode.BreakBased)
            {
                var currentTime = GetCurrentTime();

                // never shown a question before
                if (_lastQuestionEndTime == 0)
                {
                    return null;
                }

                // Check if enough time has passed since the last question
                var hasEnoughTimePassedSinceLastQuestionEnd =
                    currentTime - _lastQuestionEndTime >=
                    _currentQuestionInterval;

                if (hasEnoughTimePassedSinceLastQuestionEnd == false)
                {
                    return $"Last question ended at {_lastQuestionEndTime:F2}, " +
                           $"current time is {currentTime:F2}, " +
                           $"current question interval is {_currentQuestionInterval}";
                }
            }
            
            return null;
        }

        private double GetCurrentTime()
        {
            return Time.realtimeSinceStartupAsDouble - _timeProvider.StartTime;
        }

        private void OnTimeRestarted()
        {
            _lastQuestionEndTime = 0;
            Start();
        }

        private void HandleTimerExpired(IQuestion question)
        {
            SubmitAnswer(question, UserAnswerSubmission.FromTimedOut());
        }
    }
}