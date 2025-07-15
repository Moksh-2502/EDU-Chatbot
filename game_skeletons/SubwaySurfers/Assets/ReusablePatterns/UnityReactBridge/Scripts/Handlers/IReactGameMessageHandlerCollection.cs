using AIEduChatbot.UnityReactBridge.Core;
using AIEduChatbot.UnityReactBridge.Data;

namespace AIEduChatbot.UnityReactBridge.Handlers
{
    public interface IReactGameMessageHandlerCollection
    {
        static IReactGameMessageHandlerCollection Instance { get; private set; } = new ReactGameMessageHandlerCollection();
        void RegisterHandler(IReactGameMessageHandler handler);
        void UnregisterHandler(IReactGameMessageHandler handler);
        void OnGameMessageReceived(ReactGameMessage gameMessage);
    }
}