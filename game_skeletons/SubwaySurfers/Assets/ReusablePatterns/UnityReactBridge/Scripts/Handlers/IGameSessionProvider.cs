using System;
using AIEduChatbot.UnityReactBridge.Core;
using AIEduChatbot.UnityReactBridge.Data;

namespace AIEduChatbot.UnityReactBridge.Handlers
{
    public interface IGameSessionProvider
    {
        static IGameSessionProvider Instance { get; private set; } = new GameSessionDataHandler();
        event Action<IGameSessionProvider> OnGameSessionChanged;
        const string UNSET_STRING = "UNSET";
        string SessionId { get; }
        UserData UserData { get; }
    }
}