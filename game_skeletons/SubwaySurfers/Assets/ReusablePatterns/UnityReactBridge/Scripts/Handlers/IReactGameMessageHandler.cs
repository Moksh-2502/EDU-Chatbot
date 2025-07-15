using AIEduChatbot.UnityReactBridge.Data;

namespace AIEduChatbot.UnityReactBridge.Handlers
{
    /// <summary>
    /// Interface for handling game events received from React
    /// Implement this interface in your game objects to receive custom events from the React interface
    /// </summary>
    public interface IReactGameMessageHandler
    {
        /// <summary>
        /// Called when a game event is received from React
        /// </summary>
        /// <param name="gameEvent">The game event data received from React</param>
        void OnGameEventReceived(ReactGameMessage gameMessage);
    }
}