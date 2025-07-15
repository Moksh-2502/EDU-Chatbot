#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime
{
    public static class PlatformDetector
    {
        private static bool? _isMobileBrowser = null;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string GetUserAgent();
        
        [DllImport("__Internal")]
        private static extern bool IsMobileOS();
#endif

        /// <summary>
        /// Detects if the current platform is a mobile browser.
        /// Checks the actual operating system the browser is running on.
        /// </summary>
        public static bool IsMobileBrowser
        {
            get
            {
                Debug.Log("Unity-Side: IsMobileBrowser property accessed");
                
                if (_isMobileBrowser.HasValue)
                {
                    Debug.Log($"Unity-Side: Returning cached mobile browser detection result: {_isMobileBrowser.Value}");
                    return _isMobileBrowser.Value;
                }

                Debug.Log("Unity-Side: No cached result found, performing mobile browser detection");
                _isMobileBrowser = DetectMobileBrowser();
                Debug.Log($"Unity-Side: Mobile browser detection completed with result: {_isMobileBrowser.Value}");
                return _isMobileBrowser.Value;
            }
        }

        private static bool DetectMobileBrowser()
        {
            Debug.Log("Unity-Side: Starting DetectMobileBrowser()");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("Unity-Side: Platform is UNITY_WEBGL (not in editor), using JavaScript detection");
            try
            {
                Debug.Log("Unity-Side: Calling JavaScript IsMobileOS() function");
                bool result = IsMobileOS();
                Debug.Log($"Unity-Side: JavaScript IsMobileOS() returned: {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Unity-Side: JavaScript IsMobileOS() failed with exception: {ex.Message}");
                Debug.Log("Unity-Side: Falling back to user agent parsing");
                return ParseUserAgent();
            }
#elif UNITY_EDITOR
            Debug.Log("Unity-Side: Platform is UNITY_EDITOR, using screen orientation for testing");
            bool result = Screen.height > Screen.width;
            Debug.Log($"Unity-Side: Editor mobile simulation based on screen orientation - Screen.height ({Screen.height}) > Screen.width ({Screen.width}): {result}");
            return result;
#else
            Debug.Log("Unity-Side: Platform is native mobile, using Unity's platform detection");
            bool isAndroid = Application.platform == RuntimePlatform.Android;
            bool isIPhone = Application.platform == RuntimePlatform.IPhonePlayer;
            bool result = isAndroid || isIPhone;
            Debug.Log($"Unity-Side: Native platform detection - Android: {isAndroid}, iPhone: {isIPhone}, Result: {result}");
            Debug.Log($"Unity-Side: Current Application.platform: {Application.platform}");
            return result;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static bool ParseUserAgent()
        {
            Debug.Log("Unity-Side: Starting ParseUserAgent() fallback method");
            try
            {
                Debug.Log("Unity-Side: Calling JavaScript GetUserAgent() function");
                string userAgent = GetUserAgent().ToLower();
                Debug.Log($"Unity-Side: Retrieved user agent: {userAgent}");
                
                bool containsMobile = userAgent.Contains("mobile");
                bool containsAndroid = userAgent.Contains("android");
                bool containsIPhone = userAgent.Contains("iphone");
                bool containsIPad = userAgent.Contains("ipad");
                bool containsIPod = userAgent.Contains("ipod");
                bool containsBlackberry = userAgent.Contains("blackberry");
                bool containsWindowsPhone = userAgent.Contains("windows phone");
                
                Debug.Log($"Unity-Side: User agent analysis - mobile: {containsMobile}, android: {containsAndroid}, iphone: {containsIPhone}, ipad: {containsIPad}, ipod: {containsIPod}, blackberry: {containsBlackberry}, windows phone: {containsWindowsPhone}");
                
                bool result = containsMobile || containsAndroid || containsIPhone || containsIPad || containsIPod || containsBlackberry || containsWindowsPhone;
                Debug.Log($"Unity-Side: User agent parsing result: {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Unity-Side: ParseUserAgent() failed with exception: {ex.Message}");
                Debug.Log("Unity-Side: Using ultimate fallback to screen size comparison");
                bool result = Screen.height > Screen.width;
                Debug.Log($"Unity-Side: Ultimate fallback screen size check - Screen.height ({Screen.height}) > Screen.width ({Screen.width}): {result}");
                return result;
            }
        }
#endif

        /// <summary>
        /// Forces a refresh of the mobile detection.
        /// Useful when screen orientation or window size changes.
        /// </summary>
        public static void RefreshDetection()
        {
            Debug.Log($"Unity-Side: RefreshDetection() called - clearing cached result (was: {_isMobileBrowser?.ToString() ?? "null"})");
            _isMobileBrowser = null;
            Debug.Log("Unity-Side: Mobile browser detection cache cleared, next access will trigger new detection");
        }
    }
} 