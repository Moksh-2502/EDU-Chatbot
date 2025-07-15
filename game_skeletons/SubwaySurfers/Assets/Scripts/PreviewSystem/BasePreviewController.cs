using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using ReusablePatterns.SharedCore.Scripts.Runtime.ItemSystem;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Base implementation for preview controllers
    /// Provides common functionality like asset loading, animations, and lifecycle management
    /// </summary>
    /// <typeparam name="TData">The type being previewed</typeparam>
    public abstract class BasePreviewController<TPreviewData, TData> : MonoBehaviour,
        IPreviewController<TPreviewData, TData> where TData : class
        where TPreviewData : IPreviewData<TData>
    {
        [Header("Preview Settings")] [SerializeField]
        protected Transform previewHolder;

        [SerializeField] protected LayerMask previewObjectLayer = 0;
        [SerializeField] protected bool enableRotation = true;
        [SerializeField] protected float rotationSpeed = 45f;

        [Header("Auto Hide Settings")] [SerializeField]
        protected bool enableAutoHide = true;

        [SerializeField] protected float autoHideDelay = 5f;

        [Header("Animation Settings")] [SerializeField]
        protected bool useShowAnimation = true;

        [SerializeField] protected float showAnimationDuration = 0.8f;
        [SerializeField] protected float emergingDistance = 2f;
        [SerializeField] protected float anticipationScale = 0.8f;
        [SerializeField] protected float overshootScale = 1.15f;

        protected GameObject _currentPreviewInstance;
        protected string _currentInstanceId;
        protected bool _isVisible = false;
        protected bool _isLoading = false;
        protected Tween _currentTween;
        protected Tween _rotationTween, _floatingTween;
        protected Sequence _showSequence;

        public bool IsVisible => _isVisible;
        public bool IsLoading => _isLoading;

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        protected virtual void Initialize()
        {
            if (previewHolder == null)
            {
                Debug.LogWarning($"BasePreviewController: PreviewHolder is not assigned on {gameObject.name}");
            }
        }

        public virtual async UniTask ShowPreviewAsync(TPreviewData previewData)
        {
            if (previewData == null)
            {
                Debug.LogWarning("BasePreviewController: Cannot show preview - PreviewData is null");
                return;
            }

            if (_isLoading)
            {
                Debug.LogWarning("BasePreviewController: Already loading, ignoring request");
                return;
            }

            _isLoading = true;

            try
            {
                // Cleanup previous preview
                await CleanupCurrentPreview();
                _currentInstanceId = System.Guid.NewGuid().ToString();

                // Show UI first
                await OnPreviewStarting(previewData);

                // Load the preview asset
                if (previewData.AssetAddress.HasValue())
                {
                    _currentPreviewInstance = await PreviewAssetManager.Instance.LoadPreviewAssetAsync(
                        previewData.AssetAddress, previewHolder, _currentInstanceId);

                    if (_currentPreviewInstance != null)
                    {
                        SetupPreviewInstance(_currentPreviewInstance, previewData);
                    }
                }

                // Finalize the preview display
                await OnPreviewReady(previewData);

                _isVisible = true;

                // Setup auto-hide if enabled
                if (enableAutoHide && autoHideDelay > 0)
                {
                    DOVirtual.DelayedCall(autoHideDelay, HidePreview);
                }

                Debug.Log($"BasePreviewController: Preview shown for {typeof(TData).Name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BasePreviewController: Error showing preview: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        public virtual void HidePreview()
        {
            if (!_isVisible && !_isLoading) return;

            _isVisible = false;

            // Cancel auto-hide timer
            CancelInvoke(nameof(HidePreview));

            // Stop any running animations
            _currentTween?.Kill();
            _rotationTween?.Kill();
            _showSequence?.Kill();
            _floatingTween?.Kill();

            // Notify derived classes
            OnPreviewHiding();

            // Cleanup preview instance
            CleanupCurrentPreview().Forget();

            Debug.Log($"BasePreviewController: Preview hidden for {typeof(TData).Name}");
        }

        protected virtual async UniTask CleanupCurrentPreview()
        {
            if (!string.IsNullOrEmpty(_currentInstanceId))
            {
                await PreviewAssetManager.Instance.CleanupInstanceAsync(_currentInstanceId);
                _currentInstanceId = null;
            }

            _currentPreviewInstance = null;
        }

        protected virtual void SetupPreviewInstance(GameObject instance, TPreviewData previewData)
        {
            if (instance == null) return;

            if (instance.TryGetComponent<ItemWorldInfo>(out var worldInfo))
            {
                worldInfo.ApplyPreviewState();
            }
            else if (previewData.TransformData != null)
            {
                previewData.TransformData.Value.ApplyToTransform(instance.transform);
            }

            GameObjectUtils.SetLayerRecursively(instance, previewObjectLayer);

            // Apply show animation if enabled
            if (useShowAnimation)
            {
                ApplyShowAnimation(instance);
            }

            // Allow derived classes to customize setup
            OnPreviewInstanceSetup(instance, previewData);
        }

        protected virtual void ApplyShowAnimation(GameObject instance)
        {
            if (instance == null) return;

            // Store original position and scale
            Vector3 originalPosition = previewHolder.localPosition;
            Vector3 originalScale = previewHolder.localScale;

            // Set initial state - below, small, and slightly tilted
            previewHolder.localPosition = originalPosition + Vector3.down * emergingDistance;
            previewHolder.localScale = Vector3.zero;
            previewHolder.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

            // Create the emerging animation sequence
            _showSequence = DOTween.Sequence();
            _showSequence.SetUpdate(true);
            // Phase 1: Quick anticipation scale
            _showSequence.Append(previewHolder.DOScale(Vector3.one * anticipationScale, showAnimationDuration * 0.15f)
                .SetEase(Ease.OutQuad));

            // Phase 2: Main emergence - move up and scale to overshoot
            _showSequence.Append(previewHolder
                .DOLocalMove(originalPosition + Vector3.up * 0.3f, showAnimationDuration * 0.5f)
                .SetEase(Ease.OutCubic));

            _showSequence.Join(previewHolder.DOScale(Vector3.one * overshootScale, showAnimationDuration * 0.5f)
                .SetEase(Ease.OutBack));

            // Add juice effects at peak of emergence
            _showSequence.AppendCallback(() => OnEmergingEffects(instance));

            // Phase 3: Settle to final position and scale
            _showSequence.Append(previewHolder.DOLocalMove(originalPosition, showAnimationDuration * 0.25f)
                .SetEase(Ease.OutBounce));

            _showSequence.Join(previewHolder.DOScale(originalScale, showAnimationDuration * 0.25f)
                .SetEase(Ease.OutBounce));

            // Phase 4: Straighten rotation
            _showSequence.Join(previewHolder.DOLocalRotate(Vector3.zero, showAnimationDuration * 0.3f)
                .SetEase(Ease.OutBack));

            // Start continuous rotation after show animation
            _showSequence.OnComplete(() =>
            {
                StartContinuousRotation(instance);
                OnEmergingAnimationComplete(instance);
            });

            _currentTween = _showSequence;
        }

        protected virtual void StartContinuousRotation(GameObject instance)
        {
            if (instance == null || !enableRotation) return;

            // Create a more interesting rotation pattern
            _rotationTween = instance.transform
                .DORotate(new Vector3(0, 360, 0), 360f / rotationSpeed, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetUpdate(true)
                .SetEase(Ease.Linear);

            // Add subtle floating animation
            _floatingTween = instance.transform.DOLocalMoveY(instance.transform.localPosition.y + 0.1f, 2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetEase(Ease.InOutSine);
        }

        protected virtual void Cleanup()
        {
            _currentTween?.Kill();
            _rotationTween?.Kill();
            _showSequence?.Kill();
            _floatingTween?.Kill();
            CancelInvoke();
            CleanupCurrentPreview().Forget();
        }

        public virtual void SetAutoHide(bool enabled, float delay = 5f)
        {
            enableAutoHide = enabled;
            autoHideDelay = delay;
        }

        // Virtual methods for additional effects
        protected virtual void OnEmergingEffects(GameObject instance)
        {
            // Override in derived classes to add particle effects, screen shake, etc.
        }

        protected virtual void OnEmergingAnimationComplete(GameObject instance)
        {
            // Override in derived classes to add completion effects
        }

        // Abstract methods for derived classes to implement
        protected abstract UniTask OnPreviewStarting(TPreviewData previewData);
        protected abstract UniTask OnPreviewReady(TPreviewData previewData);
        protected abstract void OnPreviewHiding();
        protected abstract void OnPreviewInstanceSetup(GameObject instance, TPreviewData previewData);
    }
}