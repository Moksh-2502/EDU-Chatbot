using System;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Message sent when Unity finishes loading and is ready to receive messages
    /// </summary>
    [Serializable]
    public class UnityReadyMessage : ReactGameMessage
    {
        public override string MessageType => "UnityReady";

        [JsonProperty("status")]
        public string Status { get; set; } = "ready";

        public UnityReadyMessage() : base()
        {
        }

        public UnityReadyMessage(string status) : base()
        {
            Status = status ?? "ready";
        }
    }
}