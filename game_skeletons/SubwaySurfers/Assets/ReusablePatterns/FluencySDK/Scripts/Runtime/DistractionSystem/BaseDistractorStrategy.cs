using System.Collections.Generic;
using System.Linq;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Base class for distractor generation strategies with common functionality
    /// </summary>
    public abstract class BaseDistractorStrategy : IDistractorStrategy
    {
        protected DistractorGenerationConfig _config;

        public abstract string StrategyName { get; }
        public abstract bool IsEnabled { get; }

        public BaseDistractorStrategy(DistractorGenerationConfig config)
        {
            _config = config;
        }

        public List<int> GenerateDistractors(Fact fact, int correctAnswer, DistractorContext context)
        {
            if (!IsEnabled)
                return new List<int>();

            var distractors = GenerateDistractorsInternal(fact, correctAnswer, context);
            return FilterAndLimitDistractors(distractors, context);
        }

        /// <summary>
        /// Strategy-specific distractor generation logic
        /// </summary>
        protected abstract List<int> GenerateDistractorsInternal(Fact fact, int correctAnswer, DistractorContext context);

        /// <summary>
        /// Filters distractors to ensure they're valid and within limits
        /// </summary>
        protected List<int> FilterAndLimitDistractors(List<int> distractors, DistractorContext context)
        {
            return distractors
                .Where(d => IsValidDistractor(d, context))
                .Distinct()
                .Take(_config.MaxDistractorsPerStrategy)
                .ToList();
        }

        /// <summary>
        /// Checks if a distractor value is valid
        /// </summary>
        protected bool IsValidDistractor(int value, DistractorContext context)
        {
            return value >= _config.MinDistractorValue && 
                   value <= _config.MaxDistractorValue && 
                   !context.UsedValues.Contains(value);
        }

        /// <summary>
        /// Generates factor variations (e.g., 7×6 → 7×5, 8×6, 7×7)
        /// </summary>
        protected List<int> GenerateFactorVariations(int factorA, int factorB, DistractorContext context)
        {
            var variations = new List<int>();
            
            // Vary second factor by ±1, ±2
            for (int delta = -2; delta <= 2; delta++)
            {
                if (delta == 0) continue;
                int newFactorB = factorB + delta;
                if (newFactorB >= 0 && newFactorB <= context.MaxMultiplicationFactor)
                {
                    variations.Add(factorA * newFactorB);
                }
            }

            // Vary first factor by ±1, ±2
            for (int delta = -2; delta <= 2; delta++)
            {
                if (delta == 0) continue;
                int newFactorA = factorA + delta;
                if (newFactorA >= 0 && newFactorA <= context.MaxMultiplicationFactor)
                {
                    variations.Add(newFactorA * factorB);
                }
            }

            return variations;
        }

        /// <summary>
        /// Gets nearby values from multiplication tables
        /// </summary>
        protected List<int> GetNearbyTableValues(int correctAnswer, DistractorContext context)
        {
            var nearbyValues = new List<int>();

            // Get values from nearby positions in multiplication tables
            for (int a = 1; a <= context.MaxMultiplicationFactor; a++)
            {
                for (int b = 1; b <= context.MaxMultiplicationFactor; b++)
                {
                    int product = a * b;
                    int difference = System.Math.Abs(product - correctAnswer);
                    
                    // Include products that are close but not exact
                    if (difference > 0 && difference <= 10)
                    {
                        nearbyValues.Add(product);
                    }
                }
            }

            return nearbyValues.OrderBy(v => System.Math.Abs(v - correctAnswer)).ToList();
        }
    }
} 