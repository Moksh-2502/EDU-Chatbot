using System;
using System.Collections.Generic;
using System.Web;
using UnityEngine;

namespace SubwaySurfers.Utilities
{
    /// <summary>
    /// Utility class for string manipulation and URL parameter extraction
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Extracts query parameters from a URL
        /// </summary>
        /// <param name="url">The URL to parse</param>
        /// <returns>Dictionary containing parameter key-value pairs</returns>
        public static Dictionary<string, string> ExtractUrlParameters(string url)
        {
            var parameters = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(url))
                return parameters;

            try
            {
                // Find the query string part (after ?)
                int queryIndex = url.IndexOf('?');
                if (queryIndex == -1)
                    return parameters;

                string queryString = url.Substring(queryIndex + 1);
                
                // Split by & to get individual parameters
                string[] paramPairs = queryString.Split('&');
                
                foreach (string paramPair in paramPairs)
                {
                    // Split by = to get key and value
                    string[] keyValue = paramPair.Split('=');
                    var key = keyValue[0];
                    var value = keyValue.Length > 1 ? keyValue[1] : "";
                    parameters[key] = value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse URL parameters from '{url}': {ex.Message}");
            }

            return parameters;
        }

        /// <summary>
        /// Gets a specific parameter value from a URL
        /// </summary>
        /// <param name="url">The URL to parse</param>
        /// <param name="parameterName">The parameter name to look for</param>
        /// <param name="defaultValue">Default value to return if parameter not found</param>
        /// <returns>Parameter value or default value</returns>
        public static string GetUrlParameter(string url, string parameterName, string defaultValue = "")
        {
            var parameters = ExtractUrlParameters(url);
            return parameters.TryGetValue(parameterName, out string value) ? value : defaultValue;
        }

        /// <summary>
        /// Checks if a URL parameter exists and has a specific value
        /// </summary>
        /// <param name="url">The URL to parse</param>
        /// <param name="parameterName">The parameter name to check</param>
        /// <param name="expectedValue">The expected value (case-insensitive)</param>
        /// <returns>True if parameter exists with the expected value</returns>
        public static bool HasUrlParameterWithValue(string url, string parameterName, string expectedValue)
        {
            string paramValue = GetUrlParameter(url, parameterName);
            return string.Equals(paramValue, expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a boolean URL parameter is set to true
        /// </summary>
        /// <param name="url">The URL to parse</param>
        /// <param name="parameterName">The parameter name to check</param>
        /// <returns>True if parameter exists and is set to "true" (case-insensitive)</returns>
        public static bool IsBooleanUrlParameterTrue(string url, string parameterName)
        {
            return HasUrlParameterWithValue(url, parameterName, "true");
        }

        /// <summary>
        /// Gets the current page URL from Application.absoluteURL (WebGL builds only)
        /// </summary>
        /// <returns>Current URL or empty string if not available</returns>
        public static string GetCurrentUrl()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Application.absoluteURL ?? "";
#else
            // For non-WebGL builds or editor, return empty string
            return "";
#endif
        }

        /// <summary>
        /// Checks if debug mode is enabled via URL parameter "debug=true"
        /// </summary>
        /// <returns>True if debug parameter is set to true in the current URL</returns>
        public static bool IsDebugModeEnabledViaUrl()
        {
            string currentUrl = GetCurrentUrl();
            return IsBooleanUrlParameterTrue(currentUrl, "debug");
        }
    }
} 