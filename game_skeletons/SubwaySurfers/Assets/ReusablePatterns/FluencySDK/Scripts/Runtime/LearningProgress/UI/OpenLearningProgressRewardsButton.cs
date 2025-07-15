using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;
using TMPro;
using Random = UnityEngine.Random;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress.UI
{
    /// <summary>
    /// Manages the reward badge system for the learning progress rewards button.
    /// Handles badge visibility, animations, and visual effects based on reward availability.
    /// </summary>
    public class OpenLearningProgressRewardsButton : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Button rewardsButton;

        [SerializeField] private GameObject badgeGameObject;
        [SerializeField] private RectTransform badgeTransform;
        [SerializeField] private CanvasGroup badgeCanvasGroup;
        [SerializeField] private TMP_Text countText;

        [Header("Animation Settings")] [SerializeField]
        private float showAnimationDuration = 0.3f;

        [SerializeField] private float hideAnimationDuration = 0.2f;
        [SerializeField] private float pulseAnimationDuration = 1.5f;
        [SerializeField] private float pulseScaleMultiplier = 1.2f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        [Header("Flying Effect Settings")] [SerializeField]
        private GameObject flyingEffectPrefab;

        [SerializeField] private float flyingEffectDuration = 1.0f;
        [SerializeField] private Ease flyingEffectEase = Ease.OutQuad;
        [SerializeField] private float buttonEnlargeScale = 1.15f;
        [SerializeField] private float buttonEnlargeDuration = 0.2f;
        [SerializeField] private Transform flyContainer, flySource;

        private bool _isBadgeVisible = false;
        private Sequence _pulseSequence;
        private Sequence _showHideSequence;
        private Vector3 _originalBadgeScale;
        private Vector3 _originalButtonScale;
        private Camera _camera;

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateComponents();
            InitializeComponents();
        }

        private void Start()
        {
            CheckRewardStatusAsync(false).Forget();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            CleanupAnimations();
        }

        private void OnDestroy()
        {
            CleanupAnimations();
        }

        #endregion

        #region Initialization

        private void ValidateComponents()
        {
            if (rewardsButton == null)
            {
                Debug.LogError($"[{nameof(OpenLearningProgressRewardsButton)}] Rewards button reference is missing!",
                    this);
                return;
            }

            if (badgeGameObject == null)
            {
                Debug.LogError($"[{nameof(OpenLearningProgressRewardsButton)}] Badge GameObject reference is missing!",
                    this);
                return;
            }

            if (badgeTransform == null)
                badgeTransform = badgeGameObject.GetComponent<RectTransform>();

            if (badgeCanvasGroup == null)
                badgeCanvasGroup = badgeGameObject.GetComponent<CanvasGroup>();
        }

        private void InitializeComponents()
        {
            if (badgeTransform != null)
                _originalBadgeScale = badgeTransform.localScale;

            if (rewardsButton != null)
                _originalButtonScale = rewardsButton.transform.localScale;

            // Initialize badge as hidden
            if (badgeGameObject != null)
            {
                badgeGameObject.SetActive(false);
                _isBadgeVisible = false;
            }

            if (badgeCanvasGroup != null)
            {
                badgeCanvasGroup.alpha = 0f;
                badgeCanvasGroup.interactable = false;
                badgeCanvasGroup.blocksRaycasts = false;
            }

            _camera = Camera.main;
        }

        #endregion

        #region Event Management

        private void SubscribeToEvents()
        {
            RewardsEventBus.OnRewardClaimResult += OnRewardClaimResult;
            RewardsEventBus.OnClaimableRewardsStatusChanged += OnClaimableRewardsStatusChanged;
        }

        private void UnsubscribeFromEvents()
        {
            RewardsEventBus.OnRewardClaimResult -= OnRewardClaimResult;
            RewardsEventBus.OnClaimableRewardsStatusChanged -= OnClaimableRewardsStatusChanged;
        }

        #endregion

        #region Reward Status Management

        private async UniTaskVoid CheckRewardStatusAsync(bool addFlyingObjects)
        {
            try
            {
                var claimableStatus = await ILearningProgressService.Instance.GetClaimableRewardsInfoAsync();
                var hasAnyClaimableRewards = claimableStatus.hasClaimableRewards;
                
                if (hasAnyClaimableRewards)
                {
                    if(_isBadgeVisible)
                    {
                        // If we have flying objects, delay the count update until the flying effect completes
                        if (addFlyingObjects && flySource != null)
                        {
                            PlayFlyingEffectWithCountUpdate(flySource.position, claimableStatus.count);
                        }
                        else
                        {
                            // No flying objects, update immediately
                            UpdateBadgeCount(claimableStatus.count);
                        }
                    }
                    else
                    {
                        ShowBadge(claimableStatus.count);
                        if (addFlyingObjects && flySource != null)
                        {
                            PlayFlyingEffect(flySource.position);
                        }
                    }
                }
                else
                {
                    if(_isBadgeVisible)
                    {
                        HideBadge();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[{nameof(OpenLearningProgressRewardsButton)}] Error checking reward status: {ex.Message}", this);
            }
        }

        #endregion

        #region Badge Visibility Management

        public void ShowBadge(int count = 0)
        {
            if (_isBadgeVisible || badgeGameObject == null) return;

            _isBadgeVisible = true;
            badgeGameObject.SetActive(true);

            UpdateBadgeCount(count);

            PlayShowAnimation();
            StartPulseAnimation();

            Debug.Log($"[{nameof(OpenLearningProgressRewardsButton)}] Badge shown - rewards are claimable", this);
        }

        public void UpdateBadgeCount(int count)
        {
            if (countText != null)
            {
                countText.text = count > 0 ? count.ToString() : string.Empty;
                Debug.Log($"[{nameof(OpenLearningProgressRewardsButton)}] Badge count updated to: {count}", this);
            }
        }

        public void HideBadge()
        {
            if (!_isBadgeVisible || badgeGameObject == null) return;

            _isBadgeVisible = false;

            PlayHideAnimation(() => { badgeGameObject.SetActive(false); });

            StopPulseAnimation();

            Debug.Log($"[{nameof(OpenLearningProgressRewardsButton)}] Badge hidden - no claimable rewards", this);
        }

        #endregion

        #region Animation Management

        private void PlayShowAnimation()
        {
            CleanupShowHideAnimation();

            if (badgeTransform == null || badgeCanvasGroup == null) return;

            // Set initial states
            badgeTransform.localScale = Vector3.zero;
            badgeCanvasGroup.alpha = 0f;

            _showHideSequence = DOTween.Sequence();
            _showHideSequence.SetUpdate(true);
            _showHideSequence.Join(badgeTransform.DOScale(_originalBadgeScale, showAnimationDuration)
                .SetEase(showEase));
            _showHideSequence.Join(badgeCanvasGroup.DOFade(1f, showAnimationDuration).SetEase(Ease.OutQuad));
            _showHideSequence.OnComplete(() =>
            {
                if (badgeCanvasGroup != null)
                {
                    badgeCanvasGroup.interactable = true;
                    badgeCanvasGroup.blocksRaycasts = true;
                }
            });
        }

        private void PlayHideAnimation(Action onComplete = null)
        {
            CleanupShowHideAnimation();

            if (badgeTransform == null || badgeCanvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (badgeCanvasGroup != null)
            {
                badgeCanvasGroup.interactable = false;
                badgeCanvasGroup.blocksRaycasts = false;
            }

            _showHideSequence = DOTween.Sequence();
            _showHideSequence.SetUpdate(true);
            _showHideSequence.Join(badgeTransform.DOScale(Vector3.zero, hideAnimationDuration).SetEase(hideEase));
            _showHideSequence.Join(badgeCanvasGroup.DOFade(0f, hideAnimationDuration).SetEase(Ease.InQuad));
            _showHideSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void StartPulseAnimation()
        {
            if (badgeTransform == null || !_isBadgeVisible) return;

            StopPulseAnimation();

            _pulseSequence = DOTween.Sequence();
            _pulseSequence.SetUpdate(true);
            _pulseSequence.Append(badgeTransform
                .DOScale(_originalBadgeScale * pulseScaleMultiplier, pulseAnimationDuration * 0.5f)
                .SetEase(Ease.InOutSine));
            _pulseSequence.Append(badgeTransform.DOScale(_originalBadgeScale, pulseAnimationDuration * 0.5f)
                .SetEase(Ease.InOutSine));
            _pulseSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void StopPulseAnimation()
        {
            if (_pulseSequence != null)
            {
                _pulseSequence.Kill();
                _pulseSequence = null;
            }

            if (badgeTransform != null)
            {
                badgeTransform.DOScale(_originalBadgeScale, 0.2f).SetEase(Ease.OutQuad);
            }
        }

        private void CleanupShowHideAnimation()
        {
            if (_showHideSequence != null)
            {
                _showHideSequence.Kill();
                _showHideSequence = null;
            }
        }

        private void CleanupAnimations()
        {
            StopPulseAnimation();
            CleanupShowHideAnimation();
        }

        #endregion

        #region Flying Effect

        public void PlayFlyingEffect(Vector3 worldPosition)
        {
            if (flyingEffectPrefab == null || rewardsButton == null)
            {
                Debug.LogWarning(
                    $"[{nameof(OpenLearningProgressRewardsButton)}] Flying effect prefab or rewards button is null",
                    this);
                return;
            }

            PlayFlyingEffectAsync(worldPosition).Forget();
        }

        public void PlayFlyingEffectWithCountUpdate(Vector3 worldPosition, int newCount)
        {
            if (flyingEffectPrefab == null || rewardsButton == null)
            {
                Debug.LogWarning(
                    $"[{nameof(OpenLearningProgressRewardsButton)}] Flying effect prefab or rewards button is null",
                    this);
                return;
            }

            PlayFlyingEffectAsync(worldPosition, newCount).Forget();
        }

        private async UniTaskVoid PlayFlyingEffectAsync(Vector3 worldPosition, int? countToUpdate = null)
        {
            try
            {
                // Convert world position to screen space
                if (_camera == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(OpenLearningProgressRewardsButton)}] Main camera not found for flying effect", this);
                    return;
                }

                Vector3 screenPosition = _camera.WorldToScreenPoint(worldPosition);

                // Create flying effect instance
                GameObject flyingEffect = Instantiate(flyingEffectPrefab, flyContainer);
                RectTransform flyingEffectTransform = flyingEffect.GetComponent<RectTransform>();

                if (flyingEffectTransform == null)
                {
                    Debug.LogError(
                        $"[{nameof(OpenLearningProgressRewardsButton)}] Flying effect prefab must have RectTransform component",
                        this);
                    Destroy(flyingEffect);
                    return;
                }

                // Set start position
                flyingEffectTransform.position = screenPosition;

                // Get target position (rewards button)
                Vector3 targetPosition = flyContainer.position;

                // Animate flying effect
                var flyingSequence = DOTween.Sequence();
                flyingSequence.SetUpdate(true);
                flyingSequence.Append(flyingEffectTransform.DOMove(targetPosition, flyingEffectDuration)
                    .SetEase(flyingEffectEase));
                flyingSequence.Join(flyingEffectTransform.DOScale(Vector3.zero, flyingEffectDuration * 0.8f)
                    .SetEase(Ease.InQuad).SetDelay(flyingEffectDuration * 0.2f));

                // Button enlarge effect and count update (when the object is absorbed)
                flyingSequence.InsertCallback(flyingEffectDuration * 0.6f, () =>
                {
                    PlayButtonEnlargeEffect();
                    
                    // Update count when the flying object is absorbed
                    if (countToUpdate.HasValue)
                    {
                        UpdateBadgeCount(countToUpdate.Value);
                    }
                });

                flyingSequence.OnComplete(() => { Destroy(flyingEffect); });

                await flyingSequence.AsyncWaitForCompletion();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[{nameof(OpenLearningProgressRewardsButton)}] Error playing flying effect: {ex.Message}", this);
            }
        }

        private void PlayButtonEnlargeEffect()
        {
            if (rewardsButton == null) return;

            var buttonTransform = rewardsButton.transform;
            var enlargeSequence = DOTween.Sequence();
            enlargeSequence.SetUpdate(true);
            enlargeSequence.Append(buttonTransform
                .DOScale(_originalButtonScale * buttonEnlargeScale, buttonEnlargeDuration).SetEase(Ease.OutQuad));
            enlargeSequence.Append(buttonTransform.DOScale(_originalButtonScale, buttonEnlargeDuration)
                .SetEase(Ease.InQuad));
        }

        #endregion

        #region Event Handlers
        
        private void OnRewardClaimResult(RewardClaimResultEventArgs args)
        {
            // Check if the claim was successful
            CheckRewardStatusAsync(false).Forget();
        }

        private void OnClaimableRewardsStatusChanged(ClaimableRewardsStatusEventArgs args)
        {
            // Update badge visibility based on claimable rewards status
            CheckRewardStatusAsync(true).Forget();
        }

        #endregion

        #region Public API

        public bool IsBadgeVisible => _isBadgeVisible;

        public void ForceRefreshRewardStatus()
        {
            // Force refresh the reward status, which will trigger the event
            ILearningProgressService.Instance.GetClaimableRewardsInfoAsync().Forget();
        }

        #endregion

        #region Editor Support

        [ContextMenu("Test Show Badge")]
        private void TestShowBadge()
        {
            OnClaimableRewardsStatusChanged(
                new ClaimableRewardsStatusEventArgs(true, Random.Range(1, 10), new[] { "TestFactSet" }));
        }

        [ContextMenu("Test Hide Badge")]
        private void TestHideBadge()
        {
            HideBadge();
        }

        [ContextMenu("Test Flying Effect")]
        private void TestFlyingEffect()
        {
            if (_camera != null && flySource)
            {
                Vector3 testPosition = flySource.position;
                PlayFlyingEffect(testPosition);
            }
        }

        #endregion
    }
}