using UnityEngine;
using TMPro;
using DG.Tweening;

namespace SubwaySurfers.UI.PreviewSystem
{
    /// <summary>
    /// Base implementation for preview UI components
    /// Provides common UI functionality like animations and display management
    /// </summary>
    /// <typeparam name="TPreviewData">The type of preview data used by this UI</typeparam>
    /// <typeparam name="TData">The type being previewed</typeparam>
    public abstract class BasePreviewUI<TPreviewData, TData> : MonoBehaviour, IPreviewUI<TPreviewData, TData>
        where TData : class
        where TPreviewData : IPreviewData<TData>
    {
        [Header("UI Components")] [SerializeField]
        protected GameObject rootPanel;

        [SerializeField] protected TMP_Text titleText;
        [SerializeField] protected TMP_Text descriptionText;

        [Header("Animation Settings")] [SerializeField]
        protected bool useAnimations = true;

        [SerializeField] protected float showAnimationDuration = 0.3f;
        [SerializeField] protected float hideAnimationDuration = 0.25f;
        [SerializeField] protected Ease showEase = Ease.OutBack;
        [SerializeField] protected Ease hideEase = Ease.InBack;

        protected bool _isVisible = false;
        protected TPreviewData CurrentPreviewData;
        protected Tween _currentTween;

        public bool IsVisible => _isVisible;
        private bool _initialized = false;

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            _currentTween?.Kill();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }

            InitializeInternal();
        }

        protected virtual void InitializeInternal()
        {
            
        }

        public virtual void Show()
        {
            if (_isVisible) return;

            Initialize();

            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            _isVisible = true;

            if (useAnimations)
            {
                ApplyShowAnimation();
            }

            OnShown();
            Debug.Log($"BasePreviewUI: Shown for {typeof(TData).Name}");
        }

        public virtual void Hide()
        {
            if (!_isVisible) return;

            Initialize();

            _isVisible = false;

            if (useAnimations)
            {
                ApplyHideAnimation();
            }
            else
            {
                CompleteHide();
            }

            OnHidden();
            Debug.Log($"BasePreviewUI: Hidden for {typeof(TData).Name}");
        }

        public virtual void UpdateDisplay(TPreviewData previewData)
        {
            if (previewData == null) return;

            Initialize();

            CurrentPreviewData = previewData;

            // Update title
            if (titleText != null)
            {
                titleText.text = GetTitle(previewData);
            }

            // Update description
            if (descriptionText != null)
            {
                descriptionText.text = GetDescription(previewData);
            }

            // Allow derived classes to update additional UI elements
            OnUpdateDisplay(previewData);
        }

        protected virtual void ApplyShowAnimation()
        {
            _currentTween?.Kill();

            // Scale animation
            transform.localScale = Vector3.zero;
            _currentTween = transform.DOScale(Vector3.one, showAnimationDuration)
                .SetEase(showEase).SetUpdate(true);
        }

        protected virtual void ApplyHideAnimation()
        {
            _currentTween?.Kill();

            _currentTween = transform.DOScale(Vector3.zero, hideAnimationDuration)
                .SetEase(hideEase).SetUpdate(true)
                .OnComplete(CompleteHide);
        }

        protected virtual void CompleteHide()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }

        // Abstract methods for derived classes to implement
        protected abstract string GetTitle(TPreviewData previewData);
        protected abstract string GetDescription(TPreviewData previewData);
        protected abstract void OnUpdateDisplay(TPreviewData previewData);
        protected abstract void OnShown();
        protected abstract void OnHidden();
    }
}