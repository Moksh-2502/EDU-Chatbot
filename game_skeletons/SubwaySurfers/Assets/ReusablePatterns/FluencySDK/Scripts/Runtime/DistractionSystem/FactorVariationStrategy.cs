using System.Collections.Generic;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Generates distractors by varying the factors in the multiplication
    /// Example: 7×6 = 42 → distractors: 7×5=35, 8×6=48, 7×7=49
    /// </summary>
    public class FactorVariationStrategy : BaseDistractorStrategy
    {
        public override string StrategyName => "FactorVariation";
        public override bool IsEnabled => _config.EnableFactorVariation;

        public FactorVariationStrategy(DistractorGenerationConfig config) : base(config)
        {
        }

        protected override List<int> GenerateDistractorsInternal(Fact fact, int correctAnswer, DistractorContext context)
        {
            var distractors = new List<int>();

            // Generate variations by changing factors
            var factorVariations = GenerateFactorVariations(fact.FactorA, fact.FactorB, context);
            distractors.AddRange(factorVariations);

            // Add some strategic variations based on common patterns
            AddCommonFactorMistakes(fact, distractors, context);

            return distractors;
        }

        /// <summary>
        /// Adds common factor-based mistakes students make
        /// </summary>
        private void AddCommonFactorMistakes(Fact fact, List<int> distractors, DistractorContext context)
        {
            // Double one factor (7×6 → 14×6 or 7×12)
            if (fact.FactorA * 2 <= context.MaxMultiplicationFactor)
            {
                distractors.Add((fact.FactorA * 2) * fact.FactorB);
            }
            if (fact.FactorB * 2 <= context.MaxMultiplicationFactor)
            {
                distractors.Add(fact.FactorA * (fact.FactorB * 2));
            }

            // Half one factor if it's even
            if (fact.FactorA % 2 == 0 && fact.FactorA > 0)
            {
                distractors.Add((fact.FactorA / 2) * fact.FactorB);
            }
            if (fact.FactorB % 2 == 0 && fact.FactorB > 0)
            {
                distractors.Add(fact.FactorA * (fact.FactorB / 2));
            }

            // Square variations (7×6 → 6×6 or 7×7)
            if (fact.FactorA != fact.FactorB)
            {
                distractors.Add(fact.FactorA * fact.FactorA); // Square first factor
                distractors.Add(fact.FactorB * fact.FactorB); // Square second factor
            }
        }
    }
} 