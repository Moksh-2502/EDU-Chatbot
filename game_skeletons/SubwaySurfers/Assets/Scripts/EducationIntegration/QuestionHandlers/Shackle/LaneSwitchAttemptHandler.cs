using UnityEngine;
using System.Collections;

namespace SubwaySurfers.Runtime
{
    /// <summary>
    /// Component that blocks lane switching when attached to a character with CharacterInputController.
    /// Used by the Shackle consumable.
    /// </summary>
    public class LaneSwitchAttemptHandler : MonoBehaviour
    {
        [SerializeField] private AudioClip shackleSound;
        [SerializeField] private Transform failedAttemptShakeTransform;
        
        [SerializeField] private float shakeAmount = 0.1f;
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float scaleUpAmount = 1f;
        
        // Reference to the input controller
        private CharacterInputController m_InputController;
        private Vector3 m_OriginalPosition;

        private IEnumerator _shakeRoutine;
        
        private void Awake()
        {
            // Get the input controller
            m_InputController = FindFirstObjectByType<CharacterInputController>(FindObjectsInactive.Include);
            
            if (m_InputController == null)
            {
                Debug.LogError("LaneSwitchBlocker requires a CharacterInputController component on the same GameObject.");
                Destroy(this);
                return;
            }

            m_InputController.OnSwitchLaneAttempt += OnSwitchLaneAttempt;
        }
        
        private void Start()
        {
            if (failedAttemptShakeTransform != null)
            {
                // Store original position for shake effect
                m_OriginalPosition = failedAttemptShakeTransform.localPosition;
                // Initialize scale to zero
                failedAttemptShakeTransform.localScale = Vector3.zero;
            }
        }
        
        private void OnSwitchLaneAttempt(bool success)
        {
            if (success == false)
            {
                ShowBlockedFeedback();
            }
        }
        
        /// <summary>
        /// Show visual feedback that lane switching is blocked
        /// </summary>
        private void ShowBlockedFeedback()
        {
            // Play shackle sound effect if available
            AudioSource audioSource = m_InputController.GetComponent<AudioSource>();
            if (audioSource != null && shackleSound != null)
            {
                audioSource.PlayOneShot(shackleSound);
            }
            
            // Show visual feedback if transform is assigned
            if (failedAttemptShakeTransform != null)
            {
                if(_shakeRoutine != null)
                {
                    StopCoroutine(_shakeRoutine);
                }
                _shakeRoutine = ShakeAndScaleEffect();
                StartCoroutine(_shakeRoutine);
            }
            
            // For debugging
            Debug.Log("Lane switching blocked!");
        }
        
        private IEnumerator ShakeAndScaleEffect()
        {
            float elapsed = 0f;
            
            // Make sure the transform is visible
            failedAttemptShakeTransform.localScale = Vector3.one * scaleUpAmount;
            
            // Shake and scale effect
            while (elapsed < shakeDuration)
            {
                // Apply random shake
                Vector3 randomOffset = Random.insideUnitSphere * shakeAmount;
                failedAttemptShakeTransform.localPosition = m_OriginalPosition + randomOffset;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Reset position and scale
            failedAttemptShakeTransform.localPosition = m_OriginalPosition;
            failedAttemptShakeTransform.localScale = Vector3.zero;
        }
        
        private void OnDestroy()
        {
            if (m_InputController != null)
            {
                m_InputController.OnSwitchLaneAttempt -= OnSwitchLaneAttempt;
            }
        }
    }
} 