using System;
using AIEduChatbot.UnityReactBridge.Data;
using AIEduChatbot.UnityReactBridge.Handlers;
using UnityEngine;

namespace AIEduChatbot.UnityReactBridge.Core
{
    [DefaultExecutionOrder(-1000)]
    public class GameSessionDataHandler : IGameSessionProvider, IReactGameMessageHandler
    {
        public event Action<IGameSessionProvider> OnGameSessionChanged;
        public UserData UserData { get; private set; }
        public string SessionId { get; private set; }

        public GameSessionDataHandler()
        {
            IReactGameMessageHandlerCollection.Instance.RegisterHandler(this);
        }

        ~GameSessionDataHandler()
        {
            if (IReactGameMessageHandlerCollection.Instance != null)
            {
                IReactGameMessageHandlerCollection.Instance.UnregisterHandler(this);
            }
        }

        public void OnGameEventReceived(ReactGameMessage gameMessage)
        {
            if (gameMessage is SessionDataReactGameMessage sessionDataReactGameMessage)
            {
                UserData = sessionDataReactGameMessage.user;
                SessionId = sessionDataReactGameMessage.sessionId;

                if (string.IsNullOrWhiteSpace(SessionId) || SessionId == IGameSessionProvider.UNSET_STRING)
                {
                    Debug.LogWarning(
                        "[ReactBridge] Received user data with unset session ID. Please ensure the React app is sending valid session data.");
                }
                else
                {
                    Debug.Log($"[ReactBridge] User data received for session: {SessionId}");
                }
                
                // Notify subscribers about the session data change
                OnGameSessionChanged?.Invoke(this);
            }
        }
    }
}