using System;
using System.Collections.Generic;
using AIEduChatbot.UnityReactBridge.Data;
using AIEduChatbot.UnityReactBridge.Handlers;

namespace AIEduChatbot.UnityReactBridge.Core
{
    [UnityEngine.Scripting.Preserve]
    public class ReactGameMessageHandlerCollection : IReactGameMessageHandlerCollection
    {
        private HashSet<IReactGameMessageHandler> _handlers = new();

        public void RegisterHandler(IReactGameMessageHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Handler cannot be null");
            }

            if (_handlers.Contains(handler))
            {
                throw new InvalidOperationException("Handler is already registered");
            }

            _handlers.Add(handler);
        }

        public void UnregisterHandler(IReactGameMessageHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Handler cannot be null");
            }

            if (!_handlers.Contains(handler))
            {
                throw new InvalidOperationException("Handler is not registered");
            }

            _handlers.Remove(handler);
        }

        public void OnGameMessageReceived(ReactGameMessage gameMessage)
        {
            if (gameMessage == null)
            {
                throw new ArgumentNullException(nameof(gameMessage), "Game message cannot be null");
            }

            foreach (var handler in _handlers)
            {
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    handler.OnGameEventReceived(gameMessage);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ReactBridge] Error in handler {handler.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}