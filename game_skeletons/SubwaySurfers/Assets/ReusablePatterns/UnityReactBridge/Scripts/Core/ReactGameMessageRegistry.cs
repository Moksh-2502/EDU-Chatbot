using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIEduChatbot.UnityReactBridge.Data;

namespace AIEduChatbot.UnityReactBridge.Core
{
    /// <summary>
    /// Manages automatic registration of game event types for JSON deserialization
    /// Uses reflection to find all types inheriting from GameEvent and registers them automatically
    /// </summary>
    public static class ReactGameMessageRegistry
    {
        private static bool _isInitialized = false;

        public static Dictionary<string, Type> RegisteredTypes { get; private set; } = new();

        /// <summary>
        /// Initialize the event registry by automatically finding and registering all GameEvent types
        /// This should be called once at application startup
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                // Get all loaded assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // Find all non-abstract types that inherit from GameEvent
                        var eventTypes = assembly.GetTypes()
                            .Where(type => typeof(ReactGameMessage).IsAssignableFrom(type) &&
                                          !type.IsAbstract &&
                                          type != typeof(ReactGameMessage))
                            .ToArray();

                        foreach (var eventType in eventTypes)
                        {
                            try
                            {
                                // Create a temporary instance to get the EventType value
                                if (Activator.CreateInstance(eventType) is ReactGameMessage instance)
                                {
                                    var eventTypeName = instance.MessageType;
                                    if (!string.IsNullOrEmpty(eventTypeName))
                                    {
                                        RegisteredTypes[eventTypeName] = eventType;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[GameEventRegistry] Failed to register {eventType.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Some assemblies might not be accessible, skip them
                        Debug.LogWarning($"[GameEventRegistry] Failed to scan assembly {assembly.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameEventRegistry] Critical error during initialization: {ex.Message}");
            }

            _isInitialized = true;
        }
    }
}