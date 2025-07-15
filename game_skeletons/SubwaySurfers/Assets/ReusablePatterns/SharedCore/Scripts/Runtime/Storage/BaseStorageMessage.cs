using System;
using UnityEngine;
using AIEduChatbot.UnityReactBridge.Data;
using Newtonsoft.Json;

namespace AIEduChatbot.SharedCore.Storage
{
    /// <summary>
    /// Represents the result of a storage operation with clear success/error states
    /// </summary>
    public class StorageOperationResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string Error { get; }
        public bool HasData => Data != null;

        private StorageOperationResult(bool isSuccess, T data, string error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
        }

        public static StorageOperationResult<T> Success(T data = default) =>
            new StorageOperationResult<T>(true, data, null);

        public static StorageOperationResult<T> Failure(string error) =>
            new StorageOperationResult<T>(false, default, error);
    }

    /// <summary>
    /// Represents the result of a storage operation that doesn't return data
    /// </summary>
    public class StorageOperationResult
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private StorageOperationResult(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static StorageOperationResult Success() =>
            new StorageOperationResult(true, null);

        public static StorageOperationResult Failure(string error) =>
            new StorageOperationResult(false, error);
    }

    /// <summary>
    /// Base abstract class for storage-related messages containing common functionality
    /// </summary>
    [Serializable]
    public abstract class BaseStorageMessage : ReactGameMessage
    {
        [SerializeField] public string correlationId;
        [SerializeField] public string jsonData;

        protected BaseStorageMessage() : base()
        {
        }

        protected BaseStorageMessage(string correlationId, object data = null)
        {
            this.correlationId = correlationId;
            this.jsonData = data != null ? JsonConvert.SerializeObject(data) : null;
        }

        /// <summary>
        /// Deserializes the JSON data field into the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <returns>The deserialized object or default(T) if data is null/empty</returns>
        public (T data, Exception ex) GetValue<T>()
        {
            if (string.IsNullOrWhiteSpace(jsonData))
                return (default, null);

            try
            {
                var data = JsonConvert.DeserializeObject<T>(jsonData);
                return (data, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize data for correlation '{correlationId}', jsonData: {jsonData}");
                Debug.LogException(ex);
                return (default, ex);
            }
        }

        /// <summary>
        /// Checks if the data field contains data
        /// </summary>
        public bool HasData => !string.IsNullOrEmpty(jsonData);
    }
}