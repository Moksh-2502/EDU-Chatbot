using System;
using AIEduChatbot.UnityReactBridge.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// Custom JSON converter for GameEvent objects that deserializes based on eventType
    /// </summary>
    public class ReactGameMessageJsonConverter : JsonCreationConverter<ReactGameMessage>
    {
        public override bool CanWrite => false;
        
        protected override ReactGameMessage Create(Type objectType, JObject jObject)
        {
            // Extract the eventType to determine which class to deserialize to
            var messageTypeToken = jObject["messageType"];
            if (messageTypeToken == null)
            {
                Debug.LogWarning("[ReactGameMessageJsonConverter] No messageType property found in JSON");
                return null;
            }

            var messageType = messageTypeToken.Value<string>();
            if (string.IsNullOrEmpty(messageType))
            {
                Debug.LogWarning("[ReactGameMessageJsonConverter] Empty messageType property in JSON");
                return null;
            }

            // Look up the registered type for this eventType
            if (!ReactGameMessageRegistry.RegisteredTypes.TryGetValue(messageType, out var targetType))
            {
                Debug.LogWarning($"[ReactGameMessageJsonConverter] No registered type found for messageType: {messageType}");
                return null;
            }
            var instance = Activator.CreateInstance(targetType) as ReactGameMessage;
            return instance;
        }
    }
}