namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Complete user data payload received from React
    /// </summary>
    public class SessionDataReactGameMessage : ReactGameMessage
    {
        public override string MessageType => "SessionData";
        
        public UserData user;
        public string sessionId;
    }
}