using System;
using UnityEngine;

namespace FluencySDK
{
    /// <summary>
    /// Factory for creating fluency generators based on configuration mode
    /// </summary>
    public static class LearningAlgorithmFactory
    {
        /// <summary>
        /// Creates a fluency generator based on the provided configuration
        /// </summary>
        /// <param name="config">Configuration determining which generator to create</param>
        /// <returns>An instance of ILearningAlgorithm</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported mode is provided</exception>
        public static ILearningAlgorithm CreateAlgorithm(LearningAlgorithmConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
            }

            Debug.Log($"[LearningAlgorithmFactory] Creating LearningAlgorithmV3");
            return new LearningAlgorithmV3(config);
        }
    }
}
