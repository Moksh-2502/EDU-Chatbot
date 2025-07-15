using System.Collections.Generic;
using System.Linq;
using FluencySDK;
using UnityEngine;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Context-aware distractor generator that uses multiple strategies to create
    /// educationally meaningful incorrect answer choices
    /// </summary>
    public class ContextAwareDistractorGenerator
    {
        private readonly DistractorGenerationConfig _config;
        private readonly List<IDistractorStrategy> _strategies;

        public ContextAwareDistractorGenerator(DistractorGenerationConfig config)
        {
            _config = config;
            _config.NormalizeWeights(); // Ensure weights sum to 1.0

            // Initialize strategies
            _strategies = new List<IDistractorStrategy>
            {
                new FactorVariationStrategy(_config),
                new ArithmeticErrorStrategy(_config),
                new TableConfusionStrategy(_config)
            };
        }

        /// <summary>
        /// Generates answer options for a given fact and correct answer
        /// </summary>
        /// <param name="fact">The math fact being questioned</param>
        /// <param name="correctAnswer">The correct answer</param>
        /// <param name="context">Context for distractor generation</param>
        /// <returns>Array of 4 QuestionChoice objects with exactly one correct answer</returns>
        public QuestionChoice<int>[] GenerateAnswerOptions(Fact fact, int correctAnswer, DistractorContext context)
        {
            var options = new List<QuestionChoice<int>>
            {
                new() { Value = correctAnswer, IsCorrect = true }
            };

            context.UsedValues.Add(correctAnswer);

            // Generate distractors using strategies
            var distractors = GenerateDistractorsUsingStrategies(fact, correctAnswer, context);

            // If we don't have enough distractors, use fallback method
            if (distractors.Count < 3)
            {
                var fallbackDistractors = GenerateFallbackDistractors(correctAnswer, context, 3 - distractors.Count);
                distractors.AddRange(fallbackDistractors);
            }

            // Take the first 3 unique distractors
            var finalDistractors = distractors
                .Where(d => !context.UsedValues.Contains(d))
                .Distinct()
                .Take(3)
                .ToList();

            // Add them to options
            foreach (var distractor in finalDistractors)
            {
                options.Add(new QuestionChoice<int> { Value = distractor, IsCorrect = false });
                context.UsedValues.Add(distractor);
            }

            // If we still don't have enough options, use the old random method as final fallback
            while (options.Count < 4)
            {
                var fallbackDistractor = GenerateRandomDistractor(correctAnswer, context);
                if (fallbackDistractor.HasValue)
                {
                    options.Add(new QuestionChoice<int> { Value = fallbackDistractor.Value, IsCorrect = false });
                    context.UsedValues.Add(fallbackDistractor.Value);
                }
                else
                {
                    break; // Avoid infinite loop if we can't generate more distractors
                }
            }

            // Shuffle the options
            SharpShuffleBag.Shuffle.FisherYates(options);

            return options.ToArray();
        }

        /// <summary>
        /// Generates distractors using the configured strategies
        /// </summary>
        private List<int> GenerateDistractorsUsingStrategies(Fact fact, int correctAnswer, DistractorContext context)
        {
            var allDistractors = new List<int>();
            var strategyWeights = new List<(IDistractorStrategy strategy, float weight)>();

            // Build weighted strategy list
            foreach (var strategy in _strategies.Where(s => s.IsEnabled))
            {
                float weight = GetStrategyWeight(strategy);
                if (weight > 0)
                {
                    strategyWeights.Add((strategy, weight));
                }
            }

            // Generate distractors from each strategy based on weights
            foreach (var (strategy, weight) in strategyWeights)
            {
                int targetCount = Mathf.RoundToInt(weight * context.DistractorsNeeded);
                if (targetCount > 0)
                {
                    var strategyDistractors = strategy.GenerateDistractors(fact, correctAnswer, context);
                    allDistractors.AddRange(strategyDistractors.Take(targetCount));
                }
            }

            // Remove duplicates and invalid values
            return allDistractors
                .Where(d => IsValidDistractor(d, context))
                .Distinct()
                .OrderBy(d => UnityEngine.Random.value) // Randomize order
                .ToList();
        }

        /// <summary>
        /// Gets the weight for a specific strategy
        /// </summary>
        private float GetStrategyWeight(IDistractorStrategy strategy)
        {
            return strategy.StrategyName switch
            {
                "FactorVariation" => _config.FactorVariationWeight,
                "ArithmeticError" => _config.ArithmeticErrorWeight,
                "TableConfusion" => _config.TableConfusionWeight,
                _ => 0f
            };
        }

        /// <summary>
        /// Generates fallback distractors when strategies don't provide enough
        /// </summary>
        private List<int> GenerateFallbackDistractors(int correctAnswer, DistractorContext context, int count)
        {
            var fallbackDistractors = new List<int>();
            
            for (int i = 0; i < count * 3; i++) // Try 3 times per needed distractor
            {
                if (fallbackDistractors.Count >= count) break;

                int offset = UnityEngine.Random.Range(-_config.FallbackRandomRange, _config.FallbackRandomRange + 1);
                if (offset == 0) offset = UnityEngine.Random.Range(1, _config.FallbackRandomRange + 1) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
                
                int distractor = correctAnswer + offset;
                
                if (IsValidDistractor(distractor, context))
                {
                    fallbackDistractors.Add(distractor);
                    context.UsedValues.Add(distractor);
                }
            }

            return fallbackDistractors;
        }

        /// <summary>
        /// Generates a single random distractor (final fallback)
        /// </summary>
        private int? GenerateRandomDistractor(int correctAnswer, DistractorContext context)
        {
            for (int attempts = 0; attempts < 50; attempts++)
            {
                int offset = UnityEngine.Random.Range(-_config.FallbackRandomRange, _config.FallbackRandomRange + 1);
                if (offset == 0) continue;
                
                int distractor = correctAnswer + offset;
                
                if (IsValidDistractor(distractor, context))
                {
                    return distractor;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a distractor value is valid
        /// </summary>
        private bool IsValidDistractor(int value, DistractorContext context)
        {
            return value >= _config.MinDistractorValue && 
                   value <= _config.MaxDistractorValue && 
                   !context.UsedValues.Contains(value);
        }
    }
} 