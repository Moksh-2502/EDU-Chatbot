using System;
using System.Collections.Generic;
using UnityEngine;
using FluencySDK.Events;
using Cysharp.Threading.Tasks;

namespace FluencySDK.UI
{
    /// <summary>
    /// Simple manager for question feedback that maps feedback types to prefabs
    /// Queues feedbacks to prevent overlapping and processes them sequentially
    /// </summary>
    public class QuestionFeedbackManager : MonoBehaviour, IQuestionFeedbackDisplayer
    {
        [SerializeField] private FeedbackTypeDisplay prefab;

        [Header("Spawn Settings")] [SerializeField]
        private Transform spawnParent;

        [SerializeField] private Vector3 spawnPositionOffset = Vector3.zero;

        [Header("Queue Settings")] [SerializeField]
        private float delayBetweenFeedbacks = 0.5f;

        private readonly Queue<QuestionFeedbackEventArgs> _feedbackQueue = new();
        private bool _isProcessingQueue = false;

        private void Awake()
        {
            IQuestionFeedbackDisplayer.Instance = this;
        }

        /// <summary>
        /// Queue feedback for sequential display
        /// </summary>
        public void DisplayFeedback(QuestionFeedbackEventArgs feedbackArgs)
        {
            _feedbackQueue.Enqueue(feedbackArgs);

            // Start processing if not already doing so
            if (!_isProcessingQueue)
            {
                ProcessFeedbackQueue().Forget();
            }
        }

        /// <summary>
        /// Process the feedback queue sequentially with delays
        /// </summary>
        private async UniTaskVoid ProcessFeedbackQueue()
        {
            _isProcessingQueue = true;

            while (_feedbackQueue.Count > 0)
            {
                try
                {
                    var feedbackArgs = _feedbackQueue.Dequeue();
                    ShowSingleFeedback(feedbackArgs).Forget();
                    await UniTask.WaitForSeconds(delayBetweenFeedbacks, ignoreTimeScale: true);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    await UniTask.Yield();
                }
            }

            _isProcessingQueue = false;
        }

        /// <summary>
        /// Show a single feedback and wait for it to complete
        /// </summary>
        private async UniTask ShowSingleFeedback(QuestionFeedbackEventArgs feedbackArgs)
        {
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[QuestionFeedbackManager] No prefab assigned for feedback type: {feedbackArgs.feedbackType}");
                return;
            }

            try
            {
                // Instantiate and position the feedback display
                var parent = spawnParent != null ? spawnParent : transform;
                var feedbackDisplay = Instantiate(this.prefab, parent);

                // Apply position offset
                if (feedbackDisplay.transform is RectTransform rectTransform)
                {
                    rectTransform.anchoredPosition += (Vector2)spawnPositionOffset;
                }

                // Show the feedback and wait for it to complete
                await feedbackDisplay.ShowFeedback(feedbackArgs);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}