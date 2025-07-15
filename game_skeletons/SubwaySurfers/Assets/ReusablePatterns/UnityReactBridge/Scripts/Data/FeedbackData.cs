using System;
using Newtonsoft.Json;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Data structure for feedback information
    /// </summary>
    [Serializable]
    public class FeedbackData
    {
        [JsonProperty("source")]
        public string Source { get; set; } = "unity";

        [JsonProperty("origin")]
        public string Origin { get; set; } = "unknown";

        [JsonProperty("metadata")]
        public object Metadata { get; set; } = null;

        public FeedbackData()
        {
        }

        public FeedbackData(string source = "unity", string origin = "unknown", object metadata = null)
        {
            Source = source;
            Origin = origin;
            Metadata = metadata;
        }
    }
}