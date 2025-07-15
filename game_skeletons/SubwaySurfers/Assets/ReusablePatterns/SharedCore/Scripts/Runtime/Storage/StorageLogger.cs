using System;
using UnityEngine;

namespace ReusablePatterns.SharedCore.Scripts.Runtime.Storage
{
    /// <summary>
    /// Centralized logging utility for storage operations with consistent prefixes and exception handling
    /// </summary>
    public static class StorageLogger
    {
        private const string LOG_PREFIX = "[StorageSystem]";
        
        /// <summary>
        /// Logs an informational message with storage system prefix
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object for additional information</param>
        public static void LogInfo(string message, object context = null)
        {
            var contextInfo = context != null ? $" | Context: {context}" : "";
            Debug.Log($"{LOG_PREFIX} {message}{contextInfo}");
        }
        
        /// <summary>
        /// Logs a warning message with storage system prefix
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object for additional information</param>
        public static void LogWarning(string message, object context = null)
        {
            var contextInfo = context != null ? $" | Context: {context}" : "";
            Debug.LogWarning($"{LOG_PREFIX} {message}{contextInfo}");
        }
        
        /// <summary>
        /// Logs an error message with storage system prefix
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional context object for additional information</param>
        public static void LogError(string message, object context = null)
        {
            var contextInfo = context != null ? $" | Context: {context}" : "";
            Debug.LogError($"{LOG_PREFIX} {message}{contextInfo}");
        }
        
        /// <summary>
        /// Logs an exception with custom context message for Sentry integration
        /// This combines the original exception with custom context in a single log entry
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <param name="contextMessage">Custom context message describing what was happening</param>
        /// <param name="additionalContext">Optional additional context object</param>
        public static void LogException(Exception exception, string contextMessage, object additionalContext = null)
        {
            var contextInfo = additionalContext != null ? $" | Additional Context: {additionalContext}" : "";
            var fullMessage = $"{LOG_PREFIX} {contextMessage}{contextInfo}";
            
            // Log the exception with the custom context as a single entry for Sentry
            Debug.LogError($"{fullMessage}\nException: {exception}");
        }
        
        /// <summary>
        /// Logs an exception with operation-specific context
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <param name="operation">The storage operation being performed</param>
        /// <param name="key">The storage key involved</param>
        /// <param name="serviceType">The type of storage service</param>
        /// <param name="additionalContext">Optional additional context</param>
        public static void LogStorageException(Exception exception, string operation, string key, string serviceType, object additionalContext = null)
        {
            var context = new
            {
                Operation = operation,
                Key = key,
                ServiceType = serviceType,
                Additional = additionalContext
            };
            
            LogException(exception, $"Storage operation '{operation}' failed for key '{key}' in {serviceType}", context);
        }
    }
} 