using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using SharedCore.Analytics.Attributes;

namespace SharedCore.Analytics
{
    /// <summary>
    /// Base implementation of analytics events with common properties.
    /// Provides core properties like timestamp, session ID, and platform info.
    /// </summary>
    public abstract class BaseAnalyticsEvent : IAnalyticsEvent
    {
        public string Id { get; set; }
        /// <summary>
        /// The name of the event as it will appear in analytics.
        /// </summary>
        public abstract string EventName { get; }

        /// <summary>
        /// Timestamp when the event was created (UTC).
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Unique session identifier for this gameplay session.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Platform the game is running on.
        /// </summary>
        public string Platform { get; private set; }

        /// <summary>
        /// Application version.
        /// </summary>
        public string AppVersion { get; private set; }

        /// <summary>
        /// Unity version.
        /// </summary>
        public string UnityVersion { get; private set; }

        public string Environment { get; private set; }
        public string BuildId { get; private set; }
        public string SessionId { get; set; }

        protected BaseAnalyticsEvent()
        {
            var gameBuildConfig = GameBuildConfigLoader.LoadBuildConfiguration();
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
            DeviceId = SystemInfo.deviceUniqueIdentifier;
            Platform = Application.platform.ToString();
            AppVersion = Application.version;
            UnityVersion = Application.unityVersion;
            Environment = gameBuildConfig.Environment;
            BuildId = gameBuildConfig.BuildId;
        }

        /// <summary>
        /// Gets all properties for this event using reflection to automatically include all public properties.
        /// Property names are automatically converted from PascalCase to snake_case format.
        /// Properties marked with AnalyticsIgnoreAttribute are excluded from tracking.
        /// </summary>
        /// <returns>Dictionary of property names and values to track</returns>
        public virtual Dictionary<string, object> GetProperties()
        {
            var properties = new Dictionary<string, object>();

            // Use reflection to get all public properties
            var type = this.GetType();
            var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in publicProperties)
            {
                try
                {
                    // Skip properties marked with AnalyticsIgnoreAttribute
                    if (property.GetCustomAttribute<AnalyticsIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    var value = property.GetValue(this);
                    if (value != null)
                    {
                        var propertyName = ConvertToSnakeCase(property.Name);
                        
                        // Special handling for DateTime to ensure consistent format
                        if (value is DateTime dateTime)
                        {
                            properties[propertyName] = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                        }
                        else
                        {
                            properties[propertyName] = value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to get property {property.Name}: {ex.Message}");
                }
            }

            // Add custom properties from derived classes
            var customProperties = GetCustomProperties();
            if (customProperties != null)
            {
                foreach (var kvp in customProperties)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            return properties;
        }

        /// <summary>
        /// Converts PascalCase/camelCase property names to snake_case.
        /// </summary>
        /// <param name="input">The property name to convert</param>
        /// <returns>The snake_case version of the property name</returns>
        private string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                
                if (char.IsUpper(c) && i > 0)
                {
                    result.Append('_');
                }
                
                result.Append(char.ToLower(c));
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Override this method to add custom properties that are not class-defined properties.
        /// These will be added in addition to the automatically reflected properties.
        /// </summary>
        /// <returns>Dictionary of custom properties, or null if none</returns>
        protected virtual Dictionary<string, object> GetCustomProperties()
        {
            return new Dictionary<string, object>();
        }
    }
} 