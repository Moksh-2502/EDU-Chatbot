using UnityEngine;
using DG.Tweening;

namespace FluencySDK.Unity
{
    [RequireComponent(typeof(RectTransform))]
    public class FloatingUIElement : MonoBehaviour
    {
        [SerializeField] private float floatDistance = 10f;
        [SerializeField] private float floatDuration = 2f;
        [SerializeField] private bool startFloatingOnEnable = true;
        
        private RectTransform _rectTransform;
        private Vector2 _originalPosition;
        private Tween _floatingTween;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;
        }
        
        private void OnEnable()
        {
            if (startFloatingOnEnable)
            {
                StartFloating();
            }
        }
        
        private void OnDisable()
        {
            StopFloating();
        }
        
        private void OnDestroy()
        {
            _floatingTween?.Kill();
        }
        
        public void StartFloating()
        {
            // Kill any existing tween
            _floatingTween?.Kill();
            
            // Create a subtle floating animation that loops
            _floatingTween = DOTween.To(
                () => _rectTransform.anchoredPosition.y,
                y => _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, y),
                _originalPosition.y + floatDistance,
                floatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true); // Makes it timescale independent
        }
        
        public void StopFloating()
        {
            _floatingTween?.Kill();
            _rectTransform.anchoredPosition = _originalPosition;
        }
        
        public void SetFloatingParameters(float distance, float duration)
        {
            floatDistance = distance;
            floatDuration = duration;
        }
    }
} 