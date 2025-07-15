using System;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Message sent when the player wants to logout from the application
    /// </summary>
    [Serializable]
    public class LogoutMessage : ReactGameMessage
    {
        public override string MessageType => "logout";

        [JsonProperty("reason")]
        public string Reason { get; set; }

        public LogoutMessage() : base()
        {
            Reason = "user_requested";
        }

        public LogoutMessage(string reason) : base()
        {
            Reason = reason ?? "user_requested";
        }
    }
}