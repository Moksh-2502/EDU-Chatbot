using System.Collections.Generic;
using System.Linq;
using FluencySDK;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Generates distractors by using nearby values from multiplication tables
    /// Example: 7×6 = 42 → distractors: 6×7=42 (same), 7×5=35, 8×6=48, 6×6=36
    /// </summary>
    public class TableConfusionStrategy : BaseDistractorStrategy
    {
        public override string StrategyName => "TableConfusion";
        public override bool IsEnabled => _config.EnableTableConfusion;

        public TableConfusionStrategy(DistractorGenerationConfig config) : base(config)
        {
        }

        protected override List<int> GenerateDistractorsInternal(Fact fact, int correctAnswer, DistractorContext context)
        {
            var distractors = new List<int>();

            // Get nearby table values
            var nearbyValues = GetNearbyTableValues(correctAnswer, context);
            distractors.AddRange(nearbyValues.Take(4)); // Take first 4 closest values

            // Add values from the same row/column in tables
            AddSameRowColumnValues(fact, correctAnswer, distractors, context);

            // Add common table confusion patterns
            AddTableConfusionPatterns(fact, correctAnswer, distractors, context);

            return distractors;
        }

        /// <summary>
        /// Adds values from the same row or column in multiplication tables
        /// </summary>
        private void AddSameRowColumnValues(Fact fact, int correctAnswer, List<int> distractors, DistractorContext context)
        {
            // Same row (same first factor, different second factor)
            for (int b = 1; b <= context.MaxMultiplicationFactor; b++)
            {
                if (b != fact.FactorB)
                {
                    int value = fact.FactorA * b;
                    if (value != correctAnswer)
                    {
                        distractors.Add(value);
                    }
                }
            }

            // Same column (same second factor, different first factor)
            for (int a = 1; a <= context.MaxMultiplicationFactor; a++)
            {
                if (a != fact.FactorA)
                {
                    int value = a * fact.FactorB;
                    if (value != correctAnswer)
                    {
                        distractors.Add(value);
                    }
                }
            }
        }

        /// <summary>
        /// Adds common table confusion patterns
        /// </summary>
        private void AddTableConfusionPatterns(Fact fact, int correctAnswer, List<int> distractors, DistractorContext context)
        {
            // Square numbers (students might think 7×6 = 7×7 or 6×6)
            if (fact.FactorA <= context.MaxMultiplicationFactor)
            {
                int squareA = fact.FactorA * fact.FactorA;
                if (squareA != correctAnswer) distractors.Add(squareA);
            }
            
            if (fact.FactorB <= context.MaxMultiplicationFactor)
            {
                int squareB = fact.FactorB * fact.FactorB;
                if (squareB != correctAnswer) distractors.Add(squareB);
            }

            // Diagonal confusion (7×6 might be confused with 6×5, 8×7)
            AddDiagonalConfusion(fact, correctAnswer, distractors, context);

            // Common fact family confusions
            AddFactFamilyConfusions(fact, correctAnswer, distractors, context);
        }

        /// <summary>
        /// Adds diagonal confusion patterns from multiplication table
        /// </summary>
        private void AddDiagonalConfusion(Fact fact, int correctAnswer, List<int> distractors, DistractorContext context)
        {
            // Upper-left diagonal (both factors -1)
            if (fact.FactorA > 1 && fact.FactorB > 1)
            {
                int diagonal1 = (fact.FactorA - 1) * (fact.FactorB - 1);
                if (diagonal1 != correctAnswer) distractors.Add(diagonal1);
            }

            // Lower-right diagonal (both factors +1)
            if (fact.FactorA < context.MaxMultiplicationFactor && fact.FactorB < context.MaxMultiplicationFactor)
            {
                int diagonal2 = (fact.FactorA + 1) * (fact.FactorB + 1);
                if (diagonal2 != correctAnswer) distractors.Add(diagonal2);
            }

            // Upper-right diagonal (factorA-1, factorB+1)
            if (fact.FactorA > 1 && fact.FactorB < context.MaxMultiplicationFactor)
            {
                int diagonal3 = (fact.FactorA - 1) * (fact.FactorB + 1);
                if (diagonal3 != correctAnswer) distractors.Add(diagonal3);
            }

            // Lower-left diagonal (factorA+1, factorB-1)
            if (fact.FactorA < context.MaxMultiplicationFactor && fact.FactorB > 1)
            {
                int diagonal4 = (fact.FactorA + 1) * (fact.FactorB - 1);
                if (diagonal4 != correctAnswer) distractors.Add(diagonal4);
            }
        }

        /// <summary>
        /// Adds confusions within the same fact family
        /// </summary>
        private void AddFactFamilyConfusions(Fact fact, int correctAnswer, List<int> distractors, DistractorContext context)
        {
            // For fact families like 2s, 5s, 10s, there are common patterns
            
            // Multiples of 10 confusion
            if (fact.FactorA == 10 || fact.FactorB == 10)
            {
                // Students might forget the zero
                int withoutZero = fact.FactorA == 10 ? fact.FactorB : fact.FactorA;
                if (withoutZero != correctAnswer) distractors.Add(withoutZero);
            }

            // 5s table often confused with counting by 5s
            if (fact.FactorA == 5 || fact.FactorB == 5)
            {
                int otherFactor = fact.FactorA == 5 ? fact.FactorB : fact.FactorA;
                int countingBy5 = otherFactor * 5; // This is correct, but let's add some variations
                
                // Add some off-by-5 errors
                if (correctAnswer >= 5) distractors.Add(correctAnswer - 5);
                distractors.Add(correctAnswer + 5);
            }

            // 9s table tricks (might use finger method incorrectly)
            if (fact.FactorA == 9 || fact.FactorB == 9)
            {
                int otherFactor = fact.FactorA == 9 ? fact.FactorB : fact.FactorA;
                
                // Common 9s mistakes: forget to subtract 9 from the tens place
                if (otherFactor > 1)
                {
                    int tensDigit = otherFactor - 1;
                    int onesDigit = 10 - otherFactor;
                    int ninesMethod = tensDigit * 10 + onesDigit;
                    
                    // Add some variations of this method done incorrectly
                    if (tensDigit > 0) distractors.Add(tensDigit * 10 + (onesDigit + 1));
                    if (onesDigit > 0) distractors.Add((tensDigit + 1) * 10 + onesDigit);
                }
            }
        }
    }
} 