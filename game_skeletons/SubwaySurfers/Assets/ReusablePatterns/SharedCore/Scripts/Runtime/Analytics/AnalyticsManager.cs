using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SharedCore.Analytics
{
    public class AnalyticsManager : MonoBehaviour
    {
        [SerializeField] private AnalyticsConfig _config;
        
        private readonly List<IAnalyticsEventSender> _eventSenders = new List<IAnalyticsEventSender>();
        private void Awake()
        {
            var gameBuildConfig = GameBuildConfigLoader.LoadBuildConfiguration();
            // Initialize the analytics service first
            IAnalyticsService analyticsService = new MixPanelAnalyticsService(_config, gameBuildConfig);
            IAnalyticsService.Instance = analyticsService;
            
            // Then discover and initialize all event senders
            DiscoverAndInitializeEventSenders();
        }
        
        private void OnDestroy()
        {
            // Clean up all event senders
            foreach (var sender in _eventSenders)
            {
                try
                {
                    sender?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing analytics event sender: {ex.Message}");
                }
            }
            _eventSenders.Clear();
        }
        
        /// <summary>
        /// Discovers all IAnalyticsEventSender implementations via reflection and initializes them.
        /// Uses IL2CPP-safe reflection by getting types from loaded assemblies.
        /// </summary>
        private void DiscoverAndInitializeEventSenders()
        {
            try
            {
                var senderImplementations = new List<Type>();
                
                // Get all loaded assemblies to find IAnalyticsEventSender implementations
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // Find all types that implement IAnalyticsEventSender
                        var types = assembly.GetTypes()
                            .Where(t => typeof(IAnalyticsEventSender).IsAssignableFrom(t) 
                                       && !t.IsInterface 
                                       && !t.IsAbstract)
                            .ToArray();
                        
                        senderImplementations.AddRange(types);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Handle cases where some types can't be loaded
                        Debug.LogWarning($"Could not load some types from assembly {assembly.FullName}: {ex.Message}");
                        
                        // Add the types that could be loaded
                        var loadedTypes = ex.Types.Where(t => t != null 
                                                              && typeof(IAnalyticsEventSender).IsAssignableFrom(t) 
                                                              && !t.IsInterface 
                                                              && !t.IsAbstract);
                        senderImplementations.AddRange(loadedTypes);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error processing assembly {assembly.FullName}: {ex.Message}");
                    }
                }
                
                // Create instances of all found senders
                var senderInstances = new List<IAnalyticsEventSender>();
                foreach (var senderType in senderImplementations)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(senderType) as IAnalyticsEventSender;
                        if (instance != null)
                        {
                            senderInstances.Add(instance);
                            Debug.Log($"Discovered analytics event sender: {senderType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create instance of analytics sender {senderType.Name}: {ex.Message}");
                    }
                }
                
                // Sort by initialization priority and initialize
                var sortedSenders = senderInstances.OrderBy(s => s.InitializationPriority).ToList();
                
                foreach (var sender in sortedSenders)
                {
                    try
                    {
                        sender.Initialize();
                        _eventSenders.Add(sender);
                        Debug.Log($"Initialized analytics event sender: {sender.GetType().Name} (Priority: {sender.InitializationPriority})");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to initialize analytics sender {sender.GetType().Name}: {ex.Message}");
                        sender.Dispose(); // Clean up failed sender
                    }
                }
                
                Debug.Log($"Analytics Manager initialized {_eventSenders.Count} event senders");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Critical error during analytics event sender discovery: {ex.Message}");
            }
        }
    }
}
