using System;
using UnityEngine;
using AIEduChatbot.UnityReactBridge.Data;
using AIEduChatbot.UnityReactBridge.Handlers;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Core
{
    /// <summary>
    /// Main communication manager that handles incoming messages from React
    /// This should be the only GameObject with this script in your scene
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class WebGLCommunicationManager : MonoBehaviour
    {
        private static WebGLCommunicationManager _instance;

        [Header("Settings")][SerializeField] private bool enableLogging = true;

        [Header("Status")][SerializeField] private bool isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // Initialize and notify React that Unity is ready
            Initialize();
            SendUnityReadySignal();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (isInitialized) return;

            Log("Initializing Unity React Bridge...");

            if (ReactBridge.IsAvailable)
            {
                Log("Running in WebGL build - waiting for React communication");
            }
            else
            {
                Log("ReactBridge not available - will send mock user data");
                SendMockUserData();
            }

            isInitialized = true;
        }

        private void SendMockUserData()
        {
            var mockUser = UserData.CreateGuest();
            mockUser.name = "Developer";
            mockUser.email = "developer@unity.com";
            SessionDataReactGameMessage msg = new SessionDataReactGameMessage()
            {
                user = mockUser,
                sessionId = "editor_session_" + Time.time,
            };
            
            Log("Sending mock user data for development");
            ReceiveMessage(JsonConvert.SerializeObject(msg));
        }

        /// <summary>
        /// Send Unity ready signal to React using the new ReactGameMessage format
        /// </summary>
        private void SendUnityReadySignal()
        {
            try
            {
                var readyMessage = new UnityReadyMessage();
                ReactBridge.SendGameMessage(readyMessage);
                Debug.Log("[WebGLCommunicationManager] Unity ready signal sent to React");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WebGLCommunicationManager] Failed to send Unity ready signal: {ex.Message}");
            }
        }

        #endregion

        #region Message Handlers (Called from React via JSLib)

        /// <summary>
        /// Called by React to send any ReactGameMessage to Unity
        /// This is the main entry point for all messages from React
        /// </summary>
        /// <param name="messageJson">JSON string containing ReactGameMessage</param>
        public void ReceiveMessage(string messageJson)
        {
            try
            {
                Log($"Received message from React: {messageJson}");
                var message = JsonConvert.DeserializeObject<ReactGameMessage>(messageJson);
                IReactGameMessageHandlerCollection.Instance.OnGameMessageReceived(message);
            }
            catch (Exception ex)
            {
                LogError($"Error processing message: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[ReactBridge] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[ReactBridge] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[ReactBridge] {message}");
        }

        #endregion
    }
}