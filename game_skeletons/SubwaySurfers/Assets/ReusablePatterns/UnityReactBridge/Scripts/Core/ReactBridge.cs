using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AIEduChatbot.UnityReactBridge.Data;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Core
{
    /// <summary>
    /// Main static API for communicating with React
    /// This class provides a simple, static interface for all Unity-React communication
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public static class ReactBridge
    {
        #region External JavaScript Functions

        // Import JavaScript functions from the ReactBridge.jslib file
        // These functions are only available in WebGL builds

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SendMessageToReact(string jsonData);
        
        [DllImport("__Internal")]
        private static extern bool IsReactBridgeAvailable();
#else
        // Mock implementations for Unity Editor and non-WebGL builds
        private static void SendMessageToReact(string jsonData)
        {
            Debug.Log($"[ReactBridge] Mock: SendMessageToReact({jsonData})");
        }

        private static bool IsReactBridgeAvailable()
        {
            return false; // Always false in editor/non-WebGL
        }
#endif

        #endregion

        #region Public API

        /// <summary>
        /// Check if the React bridge is available and functional
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                try
                {
                    return IsReactBridgeAvailable();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ReactBridge] Error checking availability: {ex.Message}");
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public static void SendGameMessage(ReactGameMessage gameMessage)
        {
            if (gameMessage == null)
            {
                Debug.LogWarning("[ReactBridge] Game event cannot be null");
                return;
            }

            try
            {
                string jsonPayload = JsonConvert.SerializeObject(gameMessage);
                SendMessageToReact(jsonPayload);
                Debug.Log($"[ReactBridge] Sent message: {jsonPayload}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ReactBridge] Failed to send game event: {ex.Message}");
            }
        }



        #endregion
    }
}