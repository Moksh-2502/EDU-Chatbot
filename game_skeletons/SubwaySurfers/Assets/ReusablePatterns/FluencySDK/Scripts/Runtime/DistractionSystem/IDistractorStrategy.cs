using System.Collections.Generic;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Interface for distractor generation strategies that create incorrect answer choices
    /// based on different approaches (factor variation, arithmetic errors, etc.)
    /// </summary>
    public interface IDistractorStrategy
    {
        /// <summary>
        /// Generates a list of potential distractors for the given fact and correct answer
        /// </summary>
        /// <param name="fact">The math fact being questioned</param>
        /// <param name="correctAnswer">The correct answer to the multiplication</param>
        /// <param name="context">Additional context for distractor generation</param>
        /// <returns>List of potential distractor values</returns>
        List<int> GenerateDistractors(Fact fact, int correctAnswer, DistractorContext context);

        /// <summary>
        /// Gets the strategy name for analytics and debugging
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// Indicates if this strategy is enabled in the current configuration
        /// </summary>
        bool IsEnabled { get; }
    }
} 