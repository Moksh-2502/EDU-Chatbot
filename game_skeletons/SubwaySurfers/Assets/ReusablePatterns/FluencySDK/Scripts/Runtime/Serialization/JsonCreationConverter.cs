using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace FluencySDK.Serialization
{
    /// <summary>
    /// Base class for custom JSON converters that need to create objects based on JSON content
    /// Similar to the pattern used in ReactGameMessageJsonConverter
    /// </summary>
    [Preserve]
    public abstract class JsonCreationConverter<T> : JsonConverter<T>
    {
        /// <summary>
        /// Create an instance of the object based on the JSON object
        /// </summary>
        /// <param name="objectType">Type of object expected</param>
        /// <param name="jObject">Contents of JSON object that will be deserialized</param>
        /// <returns>Instance of the object to deserialize</returns>
        protected abstract T Create(Type objectType, JObject jObject);

        public override bool CanWrite => false;

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, 
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            // Load JSON object from stream
            JObject jObject = JObject.Load(reader);

            // Create target object based on JSON content
            T target = Create(objectType, jObject);

            if (target == null)
                return default;

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            throw new NotImplementedException("JsonCreationConverter should only be used for reading JSON");
        }
    }
} 