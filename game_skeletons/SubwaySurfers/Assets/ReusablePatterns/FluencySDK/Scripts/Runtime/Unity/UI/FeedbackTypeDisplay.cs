using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using FluencySDK.Events;

namespace FluencySDK.UI
{
    /// <summary>
    /// Handles the display and animation of a single feedback type
    /// </summary>
    public class FeedbackTypeDisplay : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private TMP_Text feedbackText;

        [SerializeField] private Image feedbackIcon;
        [SerializeField] private RectTransform animationTarget;

        private CanvasGroup _canvasGroup;
        private Vector3 _startPosition;
        private Sequence _animationSequence;

        private bool _isActive = false, _isInitialized = false;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (animationTarget == null)
            {
                animationTarget = transform as RectTransform;
            }

            _startPosition = animationTarget.anchoredPosition;
            _isInitialized = true;
        }

        private void OnEnable()
        {
            _isActive = true;
        }

        private void OnDisable()
        {
            _isActive = false;
        }

        /// <summary>
        /// Display feedback with animation
        /// </summary>
        public async UniTask ShowFeedback(QuestionFeedbackEventArgs feedbackArgs)
        {
            // Setup content
            await SetupContent(feedbackArgs);

            // Animate based on feedback type
            await AnimateFeedbackByType(feedbackArgs.feedbackType);
        }

        private async UniTask SetupContent(QuestionFeedbackEventArgs feedbackArgs)
        {
            await UniTask.WaitUntil(() => _isInitialized,
                cancellationToken: this.GetCancellationTokenOnDestroy());
            // Set text
            if (feedbackText != null)
            {
                feedbackText.text = feedbackArgs.feedbackText;
            }

            // Set icon
            if (feedbackIcon != null)
            {
                if (feedbackArgs.feedbackIcon != null)
                {
                    feedbackIcon.sprite = feedbackArgs.feedbackIcon;
                    feedbackIcon.gameObject.SetActive(true);
                }
                else
                {
                    feedbackIcon.gameObject.SetActive(false);
                }
            }

            // Reset position and visibility
            animationTarget.anchoredPosition = _startPosition;
            animationTarget.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
        }

        private async UniTask AnimateFeedbackByType(FeedbackType feedbackType)
        {
            // Kill any existing animation
            if (_animationSequence != null && _animationSequence.IsActive())
            {
                _animationSequence.Kill();
            }

            _animationSequence = DOTween.Sequence();
            _animationSequence.SetUpdate(UpdateType.Normal, true);

            switch (feedbackType)
            {
                case FeedbackType.CorrectWord:
                    await AnimateCorrect();
                    break;
                case FeedbackType.CorrectReward:
                    await AnimateTimeBonus();
                    break;
                case FeedbackType.IncorrectWord:
                    await AnimateIncorrectWord();
                    break;
                case FeedbackType.IncorrectPenalty:
                    await AnimateIncorrect();
                    break;
                case FeedbackType.CorrectStreak:
                    await AnimateStreak();
                    break;
                case FeedbackType.CompleteCorrectStreak:
                    await AnimatePerfect();
                    break;
                case FeedbackType.IncorrectStreak:
                    await AnimateIncorrectStreak();
                    break;
                case FeedbackType.CompleteIncorrectStreak:
                    await AnimateIncorrectStreakComplete();
                    break;
            }

            if (_isActive)
            {
                // Hide when done
                gameObject.SetActive(false);
            }
        }

        private async UniTask AnimateCorrect()
        {
            // Simple, clean scale up with gentle float
            var upwardFloat = Vector3.up * 50f; // Gentle upward movement for positive feedback

            if (feedbackText != null)
            {
                _animationSequence.Append(feedbackText.DOColor(Color.green, 0.2f));
            }

            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.2f));
            _animationSequence.Join(animationTarget.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack));
            _animationSequence.Append(animationTarget.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad));
            _animationSequence.Join(animationTarget.DOAnchorPosY(_startPosition.y + upwardFloat.y, 1.2f)
                .SetEase(Ease.OutQuad));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 0.8f).SetDelay(0.4f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateIncorrect()
        {
            // Shake animation with red tint
            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.1f));
            _animationSequence.Join(animationTarget.DOScale(1f, 0.2f).SetEase(Ease.OutQuad));

            // Change color to red
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(Color.red, 0.2f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(Color.red, 0.2f));

            // Shake effect
            _animationSequence.Append(animationTarget.DOShakePosition(0.5f, 20f, 20, 90, false, true));

            // Fade out
            _animationSequence.Append(_canvasGroup.DOFade(0f, 0.3f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateIncorrectWord()
        {
            // Gentle "Try Again!" animation - encouraging rather than punishing
            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.2f));
            _animationSequence.Join(animationTarget.DOScale(1f, 0.2f).SetEase(Ease.OutQuad));

            // Change color to red for "Try Again!" message
            var tryAgainRed = new Color(0.9f, 0.2f, 0.2f); // Slightly softer red
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(tryAgainRed, 0.2f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(tryAgainRed, 0.2f));

            // Gentle bounce to encourage rather than punish
            _animationSequence.Append(animationTarget.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack));
            _animationSequence.Append(animationTarget.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad));

            // Subtle side-to-side nudge (gentler than shake)
            _animationSequence.Append(animationTarget.DOShakePosition(0.4f, 10f, 10, 90, false, true));

            // Hold for a moment to let player read "Try Again!"
            _animationSequence.AppendInterval(0.3f);

            // Gentle fade out
            _animationSequence.Append(_canvasGroup.DOFade(0f, 0.4f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateBonus()
        {
            // Bouncy scale with sparkle effect
            var highFloat = Vector3.up * 120f; // Higher float for bonus excitement

            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.1f));
            _animationSequence.Join(animationTarget.DOScale(1.3f, 0.4f).SetEase(Ease.OutBounce));

            // Change color to gold
            var goldColor = new Color(1f, 0.84f, 0f);
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(goldColor, 0.2f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(goldColor, 0.2f));

            // Multiple bounces
            _animationSequence.Append(animationTarget.DOScale(1.1f, 0.2f).SetEase(Ease.InOutQuad));
            _animationSequence.Append(animationTarget.DOScale(1.2f, 0.2f).SetEase(Ease.InOutQuad));
            _animationSequence.Append(animationTarget.DOScale(1f, 0.2f).SetEase(Ease.InOutQuad));

            // Float up and fade
            _animationSequence.Join(animationTarget.DOAnchorPosY(_startPosition.y + highFloat.y, 0.8f)
                .SetEase(Ease.OutQuad));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 0.8f).SetDelay(0.2f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateStreak()
        {
            // Multiple quick pulses
            var mediumFloat = Vector3.up * 30f; // Modest float for streak building

            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.1f));
            _animationSequence.Join(animationTarget.DOScale(1f, 0.1f));

            // Change color to orange
            var orangeColor = new Color(1f, 0.5f, 0f);
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(orangeColor, 0.1f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(orangeColor, 0.1f));

            // Quick pulses
            for (int i = 0; i < 3; i++)
            {
                _animationSequence.Append(animationTarget.DOScale(1.2f, 0.15f).SetEase(Ease.OutQuad));
                _animationSequence.Append(animationTarget.DOScale(1f, 0.15f).SetEase(Ease.InQuad));
            }

            // Final scale and fade
            _animationSequence.Append(animationTarget.DOScale(1.1f, 0.2f));
            _animationSequence.Join(animationTarget.DOAnchorPosY(_startPosition.y + mediumFloat.y, 0.6f));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 0.6f).SetDelay(0.1f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateTimeBonus()
        {
            // Fast zoom in/out with time-related effects
            var fastFloat = Vector3.up * 80f; // Quick upward movement for time bonus

            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.05f));
            _animationSequence.Join(animationTarget.DOScale(1.5f, 0.2f).SetEase(Ease.OutQuart));

            // Change color to cyan (time-related)
            var cyanColor = new Color(0f, 1f, 1f);
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(cyanColor, 0.1f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(cyanColor, 0.1f));

            // Quick zoom out
            _animationSequence.Append(animationTarget.DOScale(0.9f, 0.2f).SetEase(Ease.InQuart));
            _animationSequence.Append(animationTarget.DOScale(1f, 0.2f).SetEase(Ease.OutQuart));

            // Fast upward movement and fade
            _animationSequence.Join(animationTarget.DOAnchorPosY(_startPosition.y + fastFloat.y, 0.8f)
                .SetEase(Ease.OutCubic));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 0.5f).SetDelay(0.2f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimatePerfect()
        {
            // Elegant grow with glow effect
            var grandFloat = Vector3.up * 100f; // Grand upward movement for perfect score

            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.3f));
            _animationSequence.Join(animationTarget.DOScale(1.2f, 0.5f).SetEase(Ease.OutElastic));

            // Change color to bright white/gold
            var perfectColor = new Color(1f, 1f, 0.8f);
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(perfectColor, 0.3f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(perfectColor, 0.3f));

            // Hold the scale for dramatic effect
            _animationSequence.AppendInterval(0.5f);

            // Gentle float and fade
            _animationSequence.Append(animationTarget.DOScale(1f, 0.3f).SetEase(Ease.InOutQuad));
            _animationSequence.Join(animationTarget.DOAnchorPosY(_startPosition.y + grandFloat.y, 1f)
                .SetEase(Ease.OutQuart));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 1f).SetDelay(0.2f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateIncorrectStreak()
        {
            // Warning animation - building tension, user is getting close to losing a life
            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.1f));
            _animationSequence.Join(animationTarget.DOScale(1f, 0.1f).SetEase(Ease.OutQuad));

            // Warning color - orange/yellow to indicate danger approaching
            var warningColor = new Color(1f, 0.6f, 0f); // Orange
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(warningColor, 0.2f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(warningColor, 0.2f));

            // Pulsing warning effect - creates tension
            _animationSequence.Append(animationTarget.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad));
            _animationSequence.Append(animationTarget.DOScale(0.95f, 0.2f).SetEase(Ease.InQuad));
            _animationSequence.Append(animationTarget.DOScale(1.05f, 0.15f).SetEase(Ease.OutQuad));
            _animationSequence.Append(animationTarget.DOScale(1f, 0.15f).SetEase(Ease.InQuad));

            // Subtle shake to indicate instability
            _animationSequence.Append(animationTarget.DOShakePosition(0.3f, 8f, 15, 90, false, true));

            // Flash effect for urgency
            _animationSequence.Join(_canvasGroup.DOFade(0.7f, 0.1f).SetLoops(2, LoopType.Yoyo));

            // Final fade out with slight downward drift
            _animationSequence.Append(animationTarget.DOAnchorPosY(_startPosition.y - 20f, 0.5f).SetEase(Ease.InQuad));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 0.4f));

            await _animationSequence.ToUniTask();
        }

        private async UniTask AnimateIncorrectStreakComplete()
        {
            // Life lost animation - dramatic and impactful but not overly punishing
            _animationSequence.Append(_canvasGroup.DOFade(1f, 0.05f));
            _animationSequence.Join(animationTarget.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuart));

            // Deep red color for life lost
            var lostLifeColor = new Color(0.8f, 0.1f, 0.1f); // Dark red
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(lostLifeColor, 0.1f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(lostLifeColor, 0.1f));

            // Initial impact - quick scale burst
            _animationSequence.Append(animationTarget.DOScale(1.4f, 0.1f).SetEase(Ease.OutBack));

            // Dramatic shake sequence - more intense than warning
            _animationSequence.Append(animationTarget.DOShakePosition(0.6f, 30f, 25, 90, false, true));
            _animationSequence.Join(animationTarget.DOShakeScale(0.4f, 0.3f, 10, 90, true));

            // Flash multiple times for dramatic effect
            _animationSequence.Join(_canvasGroup.DOFade(0.3f, 0.08f).SetLoops(4, LoopType.Yoyo));

            // Slow deflate effect - like losing energy/life
            _animationSequence.Append(animationTarget.DOScale(0.8f, 0.3f).SetEase(Ease.InQuart));

            // Color fade to darker
            if (feedbackText != null)
                _animationSequence.Join(feedbackText.DOColor(new Color(0.4f, 0.05f, 0.05f), 0.3f));
            if (feedbackIcon != null)
                _animationSequence.Join(feedbackIcon.DOColor(new Color(0.4f, 0.05f, 0.05f), 0.3f));

            // Final dramatic exit - drop down and fade
            _animationSequence.Append(animationTarget.DOAnchorPosY(_startPosition.y - 50f, 0.8f).SetEase(Ease.InCubic));
            _animationSequence.Join(animationTarget.DOScale(0.6f, 0.8f).SetEase(Ease.InCubic));
            _animationSequence.Join(_canvasGroup.DOFade(0f, 0.8f).SetEase(Ease.InQuart));

            await _animationSequence.ToUniTask();
        }


        private void OnDestroy()
        {
            if (_animationSequence != null && _animationSequence.IsActive())
            {
                _animationSequence.Kill();
            }
        }
    }
}