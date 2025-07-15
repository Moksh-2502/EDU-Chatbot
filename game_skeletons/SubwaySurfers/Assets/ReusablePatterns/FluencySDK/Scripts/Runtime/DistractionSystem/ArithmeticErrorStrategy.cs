using System.Collections.Generic;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Generates distractors by simulating common arithmetic errors
    /// Example: 7×6 = 42 → distractors: 7+6=13, 76, 67, 24 (reversed multiplication)
    /// </summary>
    public class ArithmeticErrorStrategy : BaseDistractorStrategy
    {
        public override string StrategyName => "ArithmeticError";
        public override bool IsEnabled => _config.EnableArithmeticError;

        public ArithmeticErrorStrategy(DistractorGenerationConfig config) : base(config)
        {
        }

        protected override List<int> GenerateDistractorsInternal(Fact fact, int correctAnswer, DistractorContext context)
        {
            var distractors = new List<int>();

            // Addition instead of multiplication (7×6 → 7+6)
            int additionResult = fact.FactorA + fact.FactorB;
            if (additionResult != correctAnswer)
            {
                distractors.Add(additionResult);
            }

            // Digit concatenation (7×6 → 76)
            if (fact.FactorA < 10 && fact.FactorB < 10)
            {
                int concatenated1 = fact.FactorA * 10 + fact.FactorB;
                int concatenated2 = fact.FactorB * 10 + fact.FactorA;
                
                if (concatenated1 != correctAnswer) distractors.Add(concatenated1);
                if (concatenated2 != correctAnswer && concatenated2 != concatenated1)
                {
                    distractors.Add(concatenated2);
                }
            }

            // Reversed multiplication (sometimes students compute 6×7 instead of 7×6)
            // This is actually the same result, so we'll use factor confusion instead
            AddFactorConfusionErrors(fact, correctAnswer, distractors, context);

            // Off-by-one multiplication errors
            AddOffByOneErrors(fact, correctAnswer, distractors, context);

            // Common calculation mistakes
            AddCalculationMistakes(fact, correctAnswer, distractors);

            return distractors;
        }

        /// <summary>
        /// Adds errors where students confuse factors with nearby numbers
        /// </summary>
        private void AddFactorConfusionErrors(Fact fact, int correctAnswer, List<int> distractors, DistractorContext context)
        {
            // Use 9 instead of 6, 6 instead of 9 (common visual confusion)
            var confusionPairs = new Dictionary<int, int[]>
            {
                { 6, new[] { 9 } },
                { 9, new[] { 6 } },
                { 1, new[] { 7 } },
                { 7, new[] { 1 } },
                { 3, new[] { 8 } },
                { 8, new[] { 3 } }
            };

            if (confusionPairs.ContainsKey(fact.FactorA))
            {
                foreach (int confusedFactor in confusionPairs[fact.FactorA])
                {
                    int result = confusedFactor * fact.FactorB;
                    if (result != correctAnswer)
                    {
                        distractors.Add(result);
                    }
                }
            }

            if (confusionPairs.ContainsKey(fact.FactorB))
            {
                foreach (int confusedFactor in confusionPairs[fact.FactorB])
                {
                    int result = fact.FactorA * confusedFactor;
                    if (result != correctAnswer)
                    {
                        distractors.Add(result);
                    }
                }
            }
        }

        /// <summary>
        /// Adds off-by-one errors in calculation
        /// </summary>
        private void AddOffByOneErrors(Fact fact, int correctAnswer, List<int> distractors, DistractorContext context)
        {
            // Off by one in final result
            if (correctAnswer > 1) distractors.Add(correctAnswer - 1);
            distractors.Add(correctAnswer + 1);

            // Off by ten (decimal place errors)
            if (correctAnswer >= 10) distractors.Add(correctAnswer - 10);
            distractors.Add(correctAnswer + 10);
        }

        /// <summary>
        /// Adds common calculation mistakes
        /// </summary>
        private void AddCalculationMistakes(Fact fact, int correctAnswer, List<int> distractors)
        {
            // Partial products (for larger numbers, students sometimes add only part)
            if (fact.FactorA >= 10 || fact.FactorB >= 10)
            {
                // Break down into partial products and make mistakes
                int onesA = fact.FactorA % 10;
                int tensA = fact.FactorA / 10;
                int onesB = fact.FactorB % 10;
                int tensB = fact.FactorB / 10;

                if (tensA > 0 && tensB > 0)
                {
                    // Forget to multiply by 10 properly
                    int partialMistake = (tensA * tensB) + (onesA * onesB);
                    if (partialMistake != correctAnswer)
                    {
                        distractors.Add(partialMistake);
                    }
                }
            }

            // Subtraction instead of multiplication (less common but happens)
            if (fact.FactorA >= fact.FactorB)
            {
                int subtractionResult = fact.FactorA - fact.FactorB;
                if (subtractionResult != correctAnswer && subtractionResult > 0)
                {
                    distractors.Add(subtractionResult);
                }
            }
        }
    }
} 