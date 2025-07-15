using System;
using AIEduChatbot.SharedCore.Storage;
using AIEduChatbot.UnityReactBridge.Core;
using AIEduChatbot.UnityReactBridge.Handlers;
using AIEduChatbot.UnityReactBridge.Storage;
using ReusablePatterns.FluencySDK.Scripts.Runtime.LearningProgress;
using ReusablePatterns.SharedCore.Scripts.Runtime.Storage;
using SubwaySurfers;
using UnityEngine;

namespace Utilities
{
    public static class GameEntryPoint
    {
        /// <summary>
        /// This will be replaced later with DI to register services.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterServices()
        {
            try
            {
                IStorageCache storageCache = new StorageCache(IGameSessionProvider.Instance);
                if (ReactBridge.IsAvailable)
                {
                    IGameStorageService.Instance = new ReactGameStorageService(storageCache);
                }
                else
                {
                    IGameStorageService.Instance = new PlayerPrefsStorageService(storageCache);
                }

                var learningProgressService = ILearningProgressService.Instance;
                var playDataProvider = IPlayerDataProvider.Instance;
                var reactGameMsgCollection = IReactGameMessageHandlerCollection.Instance;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}