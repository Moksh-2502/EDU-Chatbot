using System;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Message sent when the player wants to give feedback
    /// </summary>
    [Serializable]
    public class GiveFeedbackMessage : ReactGameMessage
    {
        public override string MessageType => "giveFeedback";

        [JsonProperty("data")]
        public FeedbackData Data { get; set; }

        public GiveFeedbackMessage() : base()
        {
            Data = new FeedbackData();
        }

        public GiveFeedbackMessage(FeedbackData data) : base()
        {
            Data = data ?? new FeedbackData();
        }
    }
}