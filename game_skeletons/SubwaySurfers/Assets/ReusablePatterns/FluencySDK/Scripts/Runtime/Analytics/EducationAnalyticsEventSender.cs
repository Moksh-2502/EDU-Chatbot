using SharedCore.Analytics;
using UnityEngine;
using FluencySDK.Unity;
using ReusablePatterns.FluencySDK.Scripts.Interfaces;

namespace FluencySDK.Analytics
{
    /// <summary>
    /// Analytics event sender responsible for tracking educational events
    /// Integrates with FluencySDK and question providers to track learning progress
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class FluencyAnalyticsEventSender : IAnalyticsEventSender
    {
        private IQuestionProvider _questionProvider;
        private bool _isInitialized;

        public int InitializationPriority => 2; // After session manager
        public bool IsActive => _isInitialized && _questionProvider != null;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                _questionProvider = BaseQuestionProvider.Instance;
                IQuestionGameplayHandler.QuestionHandlerStartedEvent += OnQuestionHandlerStarted;
                IQuestionGameplayHandler.QuestionHandlerEndedEvent += OnQuestionHandlerEnded;
                ILearningAlgorithm.LearningAlgorithmEvent += OnLearningAlgorithmEvent;

                _isInitialized = true;
                Debug.Log("EducationAnalyticsEventSender initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"EducationAnalyticsEventSender initialization failed: {ex.Message}");
            }
        }

        private void OnQuestionHandlerStarted(IQuestionGameplayHandler handler, IQuestion question)
        {
            if (!IsActive) return;

            try
            {
                var questionDisplayedEvent = new QuestionDisplayedEvent(question, _questionProvider.QuestionGenerationMode, question.LearningMode, handler.HandlerIdentifier);
                IAnalyticsService.Instance?.TrackEvent(questionDisplayedEvent);
                Debug.Log($"[EducationAnalyticsEventSender] Question handler started: {handler.GetType().Name} for {question.Id}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"EducationAnalyticsEventSender: Error tracking question handler start: {ex.Message}");
            }
        }

        private void OnQuestionHandlerEnded(IQuestionGameplayHandler handler, IQuestion question, UserAnswerSubmission userAnswerSubmission)
        {
            if (!IsActive || question.IsMock) return;

            try
            {
                var questionAnsweredEvent = new QuestionAnsweredEvent(question, userAnswerSubmission.AnswerType, _questionProvider.QuestionGenerationMode, question.LearningMode, handler.HandlerIdentifier);
                IAnalyticsService.Instance?.TrackEvent(questionAnsweredEvent);
                Debug.Log($"[EducationAnalyticsEventSender] Question handler ended: {handler.GetType().Name} for {question.Id}, Answer: {userAnswerSubmission}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"EducationAnalyticsEventSender: Error tracking question handler end: {ex.Message}");
            }
        }

        private void OnLearningAlgorithmEvent(ILearningAlgorithmEvent algorithmEvent)
        {
            if (!IsActive) return;

            try
            {
                Debug.Log($"[EducationAnalyticsEventSender] Learning algorithm event: {algorithmEvent.EventName}");
                
                var analyticsEvent = new LearningAlgorithmAnalyticsEvent(algorithmEvent);
                IAnalyticsService.Instance?.TrackEvent(analyticsEvent);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"EducationAnalyticsEventSender: Error tracking learning algorithm event: {ex.Message}");
            }
        }

        public void Dispose()
        {
            IQuestionGameplayHandler.QuestionHandlerStartedEvent -= OnQuestionHandlerStarted;
            IQuestionGameplayHandler.QuestionHandlerEndedEvent -= OnQuestionHandlerEnded;
            ILearningAlgorithm.LearningAlgorithmEvent -= OnLearningAlgorithmEvent;
            _questionProvider = null;
            _isInitialized = false;
        }
    }
} 