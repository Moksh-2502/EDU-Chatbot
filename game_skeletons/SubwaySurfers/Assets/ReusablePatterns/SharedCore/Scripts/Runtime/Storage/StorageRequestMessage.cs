using System;
using UnityEngine;
using Newtonsoft.Json;

namespace AIEduChatbot.SharedCore.Storage
{
    /// <summary>
    /// Message for storage requests from Unity to React
    /// </summary>
    [Serializable]
    public class StorageRequestMessage : BaseStorageMessage
    {
        public override string MessageType => "StorageRequest";

        public string operation;
        public string userId;
        public string key;

        public StorageRequestMessage()
        {
        }

        public StorageRequestMessage(string operation, string correlationId, string userId, string key, object data = null)
            : base(correlationId, data)
        {
            this.operation = operation;
            this.userId = userId;
            this.key = key;
        }

        public override string ToString()
        {
            return $"StorageRequestMessage(operation: {operation}, key: {key}, userId: {userId}, correlationId: {correlationId}, hasData: {HasData})";
        }
    }
}