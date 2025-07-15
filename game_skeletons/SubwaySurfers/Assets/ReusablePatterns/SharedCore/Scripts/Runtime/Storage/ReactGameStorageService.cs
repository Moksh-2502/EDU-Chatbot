using AIEduChatbot.UnityReactBridge.Handlers;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using AIEduChatbot.UnityReactBridge.Core;
using AIEduChatbot.UnityReactBridge.Data;
using Newtonsoft.Json;
using ReusablePatterns.SharedCore.Scripts.Runtime.Storage;

namespace AIEduChatbot.SharedCore.Storage
{
    /// <summary>
    /// React-based implementation of IGameStorageService that delegates storage operations
    /// to the React side, which can then implement local storage or cloud storage.
    /// </summary>
    public class ReactGameStorageService : BaseStorageService, IReactGameMessageHandler
    {
        private class DataExistsStatusCheck
        {
            [JsonProperty("exists")] public bool Exists { get; set; }
        }

        protected override string ServiceName => "ReactGameStorage";

        private readonly Dictionary<string, UniTaskCompletionSource<StorageResponseMessage>> _pendingRequests;

        /// <summary>
        /// Initializes a new instance of ReactGameStorageService.
        /// </summary>
        public ReactGameStorageService(IStorageCache cache) : base(cache)
        {
            _pendingRequests = new Dictionary<string, UniTaskCompletionSource<StorageResponseMessage>>();

            // Register to receive responses from React
            IReactGameMessageHandlerCollection.Instance.RegisterHandler(this);
        }

        /// <summary>
        /// Gets the current user ID from the session provider.
        /// </summary>
        /// <returns>The user ID or a fallback value</returns>
        private string GetUserId()
        {
            return IGameSessionProvider.Instance?.UserData?.id;
        }

        /// <summary>
        /// Generates a unique correlation ID for tracking requests.
        /// </summary>
        private string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Waits for user ID to be available from the session provider.
        /// </summary>
        private async UniTask EnsureUserIdAvailable()
        {
            await UniTask.WaitUntil(() => !string.IsNullOrWhiteSpace(GetUserId()));
        }

        /// <summary>
        /// Sends a storage request to React and waits for response.
        /// </summary>
        private async UniTask<StorageResponseMessage> SendRequestToReact(string operation, string key,
            object data = null)
        {
            var correlationId = GenerateCorrelationId();

            await EnsureUserIdAvailable();

            var completionSource = new UniTaskCompletionSource<StorageResponseMessage>();
            _pendingRequests[correlationId] = completionSource;

            try
            {
                // Create and send the storage request message
                var requestMessage = new StorageRequestMessage(operation, correlationId, GetUserId(), key, data);

                StorageLogger.LogInfo($"Sending storage request - Operation: {operation}, Key: {key}, UserId: {requestMessage.userId}, CorrelationId: {correlationId}", 
                    new { ServiceType = ServiceName });

                ReactBridge.SendGameMessage(requestMessage);

                // Wait for response without timeout
                var responseMessage = await completionSource.Task;

                return responseMessage;
            }
            finally
            {
                _pendingRequests.Remove(correlationId, out _);
            }
        }

        /// <summary>
        /// Sends a storage request for operations that return data (load, exists).
        /// </summary>
        private async UniTask<StorageOperationResult<T>> SendDataRequestAsync<T>(string operation, string key)
        {
            try
            {
                var response = await SendRequestToReact(operation, key);

                if (response == null)
                {
                    return StorageOperationResult<T>.Failure($"No response received for {operation} operation");
                }

                if (!response.success)
                {
                    return StorageOperationResult<T>.Failure(response.error ?? $"{operation} operation failed");
                }

                if (!response.HasData)
                {
                    return StorageOperationResult<T>.Success();
                }

                var (data, exception) = response.GetValue<T>();
                if (exception != null)
                {
                    return StorageOperationResult<T>.Failure(
                        $"Failed to deserialize {operation} response: {exception.Message}");
                }

                return StorageOperationResult<T>.Success(data);
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, operation, key, ServiceName, new { DataType = typeof(T).Name });
                return StorageOperationResult<T>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Sends a storage request for operations that don't return data (save, delete).
        /// </summary>
        private async UniTask<StorageOperationResult> SendActionRequestAsync(string operation, string key,
            object data = null)
        {
            try
            {
                var response = await SendRequestToReact(operation, key, data);

                if (response == null)
                {
                    return StorageOperationResult.Failure($"No response received for {operation} operation");
                }

                if (!response.success)
                {
                    return StorageOperationResult.Failure(response.error ?? $"{operation} operation failed");
                }

                return StorageOperationResult.Success();
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, operation, key, ServiceName);
                return StorageOperationResult.Failure(ex.Message);
            }
        }

        protected override async UniTask<T> LoadFromStorageAsync<T>(string key)
        {
            try
            {
                var result = await SendDataRequestAsync<T>("load", key);
                if (result.IsSuccess)
                {
                    return result.Data;
                }

                StorageLogger.LogWarning($"Load failed from React storage: {result.Error}", new { ServiceType = ServiceName, Key = key });
                return default(T);
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "LoadFromStorage", key, ServiceName, new { DataType = typeof(T).Name });
                return default(T);
            }
        }

        protected override async UniTask SaveToStorageAsync<T>(string key, T data)
        {
            try
            {
                var result = await SendActionRequestAsync("save", key, data);
                if (!result.IsSuccess)
                {
                    var errorMessage = $"Save failed to React storage: {result.Error}";
                    StorageLogger.LogError(errorMessage, new { ServiceType = ServiceName, Key = key });
                    throw new InvalidOperationException(errorMessage);
                }
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "SaveToStorage", key, ServiceName, new { DataType = typeof(T).Name });
                throw; // Re-throw to maintain error handling in base class
            }
        }

        protected override async UniTask DeleteFromStorageAsync(string key)
        {
            try
            {
                var result = await SendActionRequestAsync("delete", key);
                if (!result.IsSuccess)
                {
                    var errorMessage = $"Delete failed from React storage: {result.Error}";
                    StorageLogger.LogError(errorMessage, new { ServiceType = ServiceName, Key = key });
                    throw new InvalidOperationException(errorMessage);
                }
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "DeleteFromStorage", key, ServiceName);
                throw; // Re-throw to maintain error handling in base class
            }
        }

        protected override async UniTask<bool> ExistsInStorageAsync(string key)
        {
            try
            {
                var result = await SendDataRequestAsync<DataExistsStatusCheck>("exists", key);
                if (result.IsSuccess)
                {
                    return result.Data?.Exists ?? false;
                }

                StorageLogger.LogWarning($"Exists check failed from React storage: {result.Error}", new { ServiceType = ServiceName, Key = key });
                return false;
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "ExistsInStorage", key, ServiceName);
                return false;
            }
        }

        protected override UniTask FlushStorageAsync()
        {
            // React storage doesn't require explicit flushing
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Handles responses from React for storage operations.
        /// </summary>
        public void OnGameEventReceived(ReactGameMessage gameMessage)
        {
            if (gameMessage is StorageResponseMessage responseMessage)
            {
                try
                {
                    StorageLogger.LogInfo($"Received storage response for correlation ID: {responseMessage.correlationId}", 
                        new { ServiceType = ServiceName });

                    if (_pendingRequests.Remove(responseMessage.correlationId, out var completionSource))
                    {
                        completionSource.TrySetResult(responseMessage);
                    }
                    else
                    {
                        StorageLogger.LogWarning($"Received storage response for unknown correlation ID: {responseMessage.correlationId}", 
                            new { ServiceType = ServiceName });
                    }
                }
                catch (Exception ex)
                {
                    StorageLogger.LogStorageException(ex, "ProcessResponse", responseMessage.correlationId, ServiceName);
                }
            }
        }

        ~ReactGameStorageService()
        {
            // Unregister handler when disposing
            try
            {
                IReactGameMessageHandlerCollection.Instance?.UnregisterHandler(this);
            }
            catch (Exception ex)
            {
                StorageLogger.LogStorageException(ex, "Dispose", "N/A", ServiceName);
            }
        }
    }
}