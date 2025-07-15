using System;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Base class for all React-Unity messages using the simplified messaging format
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ReactGameMessageJsonConverter))]
    public abstract class ReactGameMessage
    {
        /// <summary>
        /// The message type identifier - must be implemented by all inheriting classes
        /// </summary>
        [JsonProperty("messageType")]
        public abstract string MessageType { get; }

        [JsonProperty("timestamp")]
        public long timestamp;

        protected ReactGameMessage()
        {
            this.timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public DateTime GetDateTime()
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(messageType: {MessageType}, timestamp: {timestamp})";
        }

        /// <summary>
        /// Serialize this message to JSON string for sending to React
        /// </summary>
        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}