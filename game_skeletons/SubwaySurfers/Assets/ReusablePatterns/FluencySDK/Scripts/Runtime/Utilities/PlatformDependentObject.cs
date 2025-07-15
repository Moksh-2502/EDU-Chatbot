using ReusablePatterns.SharedCore.Scripts.Runtime;
using UnityEngine;

namespace FluencySDK.Unity
{
    /// <summary>
    /// Controls the visibility of a GameObject based on the current platform.
    /// Uses PlatformDetector for accurate platform detection, especially in WebGL builds.
    /// </summary>
    public class PlatformDependentObject : MonoBehaviour
    {
        [Header("Platform Settings")]
        [SerializeField] private bool enableOnMobile = true;
        [SerializeField] private bool enableOnDesktop = true;
        
        private void Awake()
        {
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            // For WebGL, we need to check if it's a mobile browser
            bool isMobileBrowser = PlatformDetector.IsMobileBrowser;
            bool enabled = (isMobileBrowser && enableOnMobile) || (!isMobileBrowser && enableOnDesktop);
            Debug.Log($"PlatformDependentObject: {gameObject.name} is enabled: {enabled}");
            gameObject.SetActive(enabled);
        }
    }
} 