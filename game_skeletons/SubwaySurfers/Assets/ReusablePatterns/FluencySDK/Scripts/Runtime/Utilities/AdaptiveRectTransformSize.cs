using UnityEngine;

namespace FluencySDK.Unity
{
    /// <summary>
    /// Defines the type of adaptation to apply
    /// </summary>
    public enum AdaptationMode
    {
        None,             // No adaptation
        RectTransformSize,  // Adapts the RectTransform's sizeDelta
        Scale              // Adapts the transform's localScale
    }
    
    /// <summary>
    /// Component that automatically adjusts a RectTransform's width and/or height to fill the screen 
    /// up to maximum values, with configurable padding. Each dimension can be enabled/disabled independently.
    /// Supports both RectTransform size adaptation and scale adaptation.
    /// Width = min(maxWidth, Screen.width) - widthPadding
    /// Height = min(maxHeight, Screen.height) - heightPadding
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AdaptiveRectTransformSize : MonoBehaviour
    {
        [Header("Adaptation Modes")]
        [SerializeField] 
        [Tooltip("How to adapt the width: RectTransform size or Scale")]
        private AdaptationMode widthAdaptationMode = AdaptationMode.RectTransformSize;
        
        [SerializeField] 
        [Tooltip("How to adapt the height: RectTransform size or Scale")]
        private AdaptationMode heightAdaptationMode = AdaptationMode.RectTransformSize;
        
        [Header("Width Settings")]
        [SerializeField] 
        [Tooltip("Maximum width the RectTransform can have (for RectTransform mode) or reference width (for Scale mode)")]
        private float maxWidth = 1200f;
        
        [SerializeField] 
        [Tooltip("Padding to subtract from the calculated width")]
        private float widthPadding = 20f;
        
        [Header("Height Settings")]
        [SerializeField] 
        [Tooltip("Maximum height the RectTransform can have (for RectTransform mode) or reference height (for Scale mode)")]
        private float maxHeight = 800f;
        
        [SerializeField] 
        [Tooltip("Padding to subtract from the calculated height")]
        private float heightPadding = 20f;
        
        [Header("Scale Mode Settings")]
        [SerializeField]
        [Tooltip("Maximum scale factor to prevent excessive scaling")]
        private float maxScaleFactor = 2f;
        
        [SerializeField]
        [Tooltip("Minimum scale factor to prevent too small scaling")]
        private float minScaleFactor = 0.5f;
        [SerializeField]
        [Tooltip("Step size for scale factor")]
        private float scaleStep = 0.1f;
        
        [Header("Update Settings")]
        [SerializeField] 
        [Tooltip("Whether to continuously update the size (useful for responsive design)")]
        private bool updateContinuously = true;
        
        [SerializeField] 
        [Tooltip("How often to check for screen size changes (in seconds)")]
        private float updateInterval = 0.5f;
        [SerializeField]
        private float resolutionChangeThreshold = 30f;
        
        /// <summary>
        /// Current width adaptation mode
        /// </summary>
        public AdaptationMode WidthAdaptationMode => widthAdaptationMode;
        
        /// <summary>
        /// Current height adaptation mode
        /// </summary>
        public AdaptationMode HeightAdaptationMode => heightAdaptationMode;
        
        /// <summary>
        /// Current maximum width setting
        /// </summary>
        public float MaxWidth => maxWidth;
        
        /// <summary>
        /// Current maximum height setting
        /// </summary>
        public float MaxHeight => maxHeight;
        
        /// <summary>
        /// Current width padding setting
        /// </summary>
        public float WidthPadding => widthPadding;
        
        /// <summary>
        /// Current height padding setting
        /// </summary>
        public float HeightPadding => heightPadding;
        
        /// <summary>
        /// Whether continuous updates are enabled
        /// </summary>
        public bool IsContinuousUpdateEnabled => updateContinuously;
        
        private RectTransform _rectTransform;
        private float _lastScreenWidth;
        private float _lastScreenHeight;
        private float _updateTimer;
        private Vector2 _originalSizeDelta;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _originalSizeDelta = _rectTransform.sizeDelta;
        }
        
        private void Start()
        {
            UpdateSize();
        }
        
        private void Update()
        {
            if (!updateContinuously) return;
            
            _updateTimer += Time.unscaledDeltaTime;
            
            if (_updateTimer >= updateInterval)
            {
                _updateTimer = 0f;
                
                // Check if size should be updated based on screen changes
                if (HasSignificantSizeChange())
                {
                    UpdateSize();
                }
            }
        }
        
        /// <summary>
        /// Manually update the size. Can be called from external scripts.
        /// </summary>
        public void UpdateSize()
        {
            if (_rectTransform == null) return;
            
            Vector2 targetSizeOrScale = CalculateTargetSizeOrScale();
            
            // Apply size changes based on adaptation mode
            if (widthAdaptationMode != AdaptationMode.None)
            {
                if (widthAdaptationMode == AdaptationMode.RectTransformSize)
                {
                    Vector2 sizeDelta = _rectTransform.sizeDelta;
                    sizeDelta.x = targetSizeOrScale.x;
                    _rectTransform.sizeDelta = sizeDelta;
                }
                else // Scale mode
                {
                    Vector3 scale = _rectTransform.localScale;
                    scale.x = targetSizeOrScale.x;
                    _rectTransform.localScale = scale;
                }
            }
            
            if (heightAdaptationMode != AdaptationMode.None)
            {
                if (heightAdaptationMode == AdaptationMode.RectTransformSize)
                {
            Vector2 sizeDelta = _rectTransform.sizeDelta;
                    sizeDelta.y = targetSizeOrScale.y;
            _rectTransform.sizeDelta = sizeDelta;
                }
                else // Scale mode
                {
                    Vector3 scale = _rectTransform.localScale;
                    scale.y = targetSizeOrScale.y;
                    _rectTransform.localScale = scale;
                }
            }
        }
        
        /// <summary>
        /// Calculate the target size or scale based on screen size, max dimensions, and padding
        /// </summary>
        /// <returns>The calculated target size or scale (width, height)</returns>
        private Vector2 CalculateTargetSizeOrScale()
        {
            float finalTargetWidthValue = widthAdaptationMode == AdaptationMode.RectTransformSize ? _rectTransform.sizeDelta.x : _rectTransform.localScale.x;
            float finalTargetHeightValue = heightAdaptationMode == AdaptationMode.RectTransformSize ? _rectTransform.sizeDelta.y : _rectTransform.localScale.y;

            float targetWidth = finalTargetWidthValue;
            float targetHeight = finalTargetHeightValue;
            
            if (widthAdaptationMode != AdaptationMode.None)
            {
                if (widthAdaptationMode == AdaptationMode.RectTransformSize)
                {
                    float availableWidth = Mathf.Min(maxWidth, Screen.width) - widthPadding;
                    targetWidth = Mathf.Max(0f, availableWidth);
                }
                else // Scale mode
                {
                    var scaleToApply = maxScaleFactor;
                    while(scaleToApply > minScaleFactor)
                    {
                        if(AreCornersVisibleWithProposedScale(scaleToApply, scaleToApply))
                        {
                            break;
                        }
                        scaleToApply -= scaleStep;
                    }
                    targetWidth = Mathf.Clamp(scaleToApply, minScaleFactor, maxScaleFactor);
                }
            }
            
            if (heightAdaptationMode != AdaptationMode.None)
            {
                if (heightAdaptationMode == AdaptationMode.RectTransformSize)
                {
                    float availableHeight = Mathf.Min(maxHeight, Screen.height) - heightPadding;
                    targetHeight = Mathf.Max(0f, availableHeight);
                }
                else // Scale mode
                {
                    var scaleToApply = maxScaleFactor;
                    while(scaleToApply > minScaleFactor)
                    {
                        if(AreCornersVisibleWithProposedScale(scaleToApply, scaleToApply))
                        {
                            break;
                        }
                        scaleToApply -= scaleStep;
                    }
                    targetHeight = Mathf.Clamp(scaleToApply, minScaleFactor, maxScaleFactor);
                }
            }
            
            return new Vector2(targetWidth, targetHeight);
        }
        
        /// <summary>
        /// Check if the rendered size has changed significantly based on screen changes
        /// </summary>
        /// <returns>True if a significant change was detected</returns>
        private bool HasSignificantSizeChange()
        {
            bool hasChanged = false;
            
            if (widthAdaptationMode != AdaptationMode.None)
        {
                 // For RectTransform mode, check screen width change
                    if (Mathf.Abs(Screen.width - _lastScreenWidth) > resolutionChangeThreshold)
        {
                        _lastScreenWidth = Screen.width;
                        hasChanged = true;
        }
            }
            
            if (heightAdaptationMode != AdaptationMode.None)
        {
                // For RectTransform mode, check screen height change
                    if (Mathf.Abs(Screen.height - _lastScreenHeight) > resolutionChangeThreshold)
                    {
                        _lastScreenHeight = Screen.height;
                        hasChanged = true;
                    }
            }
            
            return hasChanged;
        }

        private bool AreCornersVisibleWithProposedScale(float proposedXScale, float proposedYScale)
        {
            if (_rectTransform == null) return false;

            Vector3 originalActualLScale = _rectTransform.localScale;
            _rectTransform.localScale = new Vector3(proposedXScale, proposedYScale, originalActualLScale.z);

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);

            bool allVisible = true;
            
            float minXBound = 0, maxXBound = Screen.width;
            float minYBound = 0, maxYBound = Screen.height;

            if (widthAdaptationMode == AdaptationMode.Scale) 
            {
                minXBound = widthPadding;
                maxXBound = Screen.width - widthPadding;
            }
            if (heightAdaptationMode == AdaptationMode.Scale) 
            {
                minYBound = heightPadding;
                maxYBound = Screen.height - heightPadding;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
                if (screenPoint.x < minXBound || screenPoint.x > maxXBound ||
                    screenPoint.y < minYBound || screenPoint.y > maxYBound)
                {
                    allVisible = false;
                    break;
                }
            }
            
            _rectTransform.localScale = originalActualLScale; // Revert
            return allVisible;
        }
    }
} 