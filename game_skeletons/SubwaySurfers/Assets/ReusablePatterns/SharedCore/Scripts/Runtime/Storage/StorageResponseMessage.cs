using System;
using UnityEngine;
using AIEduChatbot.UnityReactBridge.Data;
using Newtonsoft.Json;

namespace AIEduChatbot.SharedCore.Storage
{
    /// <summary>
    /// Message for storage responses from React to Unity
    /// </summary>
    [Serializable]
    public class StorageResponseMessage : BaseStorageMessage
    {
        public override string MessageType => "StorageResponse";

        [SerializeField] public bool success;
        [SerializeField] public string error;

        public StorageResponseMessage()
        {
        }

        public StorageResponseMessage(string correlationId, bool success, object data = null, string error = null)
            : base(correlationId, data)
        {
            this.success = success;
            this.error = error;
        }

        public override string ToString()
        {
            return $"StorageResponseMessage(correlationId: {correlationId}, success: {success}, hasData: {HasData}, error: {error})";
        }
    }
}