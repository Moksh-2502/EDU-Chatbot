using System;
using System.Collections.Generic;
using ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem;

namespace FluencySDK
{
    /// <summary>
    /// Utility class containing shared functionality between fluency generators
    /// </summary>
    public static class LearningAlgorithmUtils
    {
        private static ContextAwareDistractorGenerator _distractorGenerator;

        /// <summary>
        /// Gets or creates the distractor generator instance
        /// </summary>
        private static ContextAwareDistractorGenerator GetDistractorGenerator(DistractorGenerationConfig config = null)
        {
            if (_distractorGenerator == null || config != null)
            {
                _distractorGenerator = new ContextAwareDistractorGenerator(config ?? new DistractorGenerationConfig());
            }
            return _distractorGenerator;
        }

        /// <summary>
        /// Generates answer options for a given correct answer using legacy method (for backward compatibility).
        /// Creates 4 unique options including the correct one.
        /// </summary>
        /// <param name="correctAnswer">The correct answer value</param>
        /// <returns>Array of 4 QuestionChoice objects with exactly one correct answer</returns>
        public static QuestionChoice<int>[] GenerateAnswerOptions(int correctAnswer)
        {
            var options = new List<QuestionChoice<int>>
            {
                new() { Value = correctAnswer, IsCorrect = true }
            };

            HashSet<int> usedValues = new HashSet<int> { correctAnswer };

            for (int i = 0; i < 3; i++)
            {
                int offset = UnityEngine.Random.Range(-5, 6);
                int distractor = correctAnswer + offset;

                while (distractor < 0 || usedValues.Contains(distractor))
                {
                    offset = UnityEngine.Random.Range(-5, 6);
                    distractor = correctAnswer + offset;
                }

                usedValues.Add(distractor);
                options.Add(new QuestionChoice<int> { Value = distractor, IsCorrect = false });
            }

            SharpShuffleBag.Shuffle.FisherYates(options);

            return options.ToArray();
        }

        /// <summary>
        /// Generates context-aware answer options for a given fact and correct answer.
        /// Creates 4 unique options including the correct one using educational strategies.
        /// </summary>
        /// <param name="fact">The math fact being questioned</param>
        /// <param name="correctAnswer">The correct answer value</param>
        /// <param name="context">Context for distractor generation</param>
        /// <param name="config">Configuration for distractor generation</param>
        /// <returns>Array of 4 QuestionChoice objects with exactly one correct answer</returns>
        public static QuestionChoice<int>[] GenerateContextAwareAnswerOptions(Fact fact, int correctAnswer, DistractorContext context, DistractorGenerationConfig config = null)
        {
            var generator = GetDistractorGenerator(config);
            return generator.GenerateAnswerOptions(fact, correctAnswer, context);
        }

        /// <summary>
        /// Generates context-aware answer options with simplified parameters
        /// </summary>
        /// <param name="fact">The math fact being questioned</param>
        /// <param name="correctAnswer">The correct answer value</param>
        /// <param name="learningStageType">Current learning stage type</param>
        /// <param name="learningMode">Current learning mode</param>
        /// <param name="maxFactor">Maximum multiplication factor</param>
        /// <param name="config">Configuration for distractor generation</param>
        /// <returns>Array of 4 QuestionChoice objects with exactly one correct answer</returns>
        public static QuestionChoice<int>[] GenerateContextAwareAnswerOptions(Fact fact, int correctAnswer, LearningStageType learningStageType, LearningMode learningMode, int maxFactor, DistractorGenerationConfig config = null)
        {
            var context = new DistractorContext(learningStageType, learningMode, maxFactor);
            return GenerateContextAwareAnswerOptions(fact, correctAnswer, context, config);
        }

        /// <summary>
        /// Gets or creates fact stats for a given fact ID
        /// </summary>
        public static FactStats GetOrCreateFactStats(Dictionary<string, FactStats> statsCollection, string factId)
        {
            if (!statsCollection.TryGetValue(factId, out FactStats stats))
            {
                stats = new FactStats();
                statsCollection[factId] = stats;
            }
            return stats;
        }

        /// <summary>
        /// Updates fact statistics for correct/incorrect answers
        /// </summary>
        public static void UpdateFactStats(Dictionary<string, FactStats> statsCollection, string factId, AnswerType answerType, bool updateLastSeen = false)
        {
            var stats = GetOrCreateFactStats(statsCollection, factId);

            stats.TimesShown++;
            if (updateLastSeen)
            {
                stats.LastSeenUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            if (answerType == AnswerType.Correct)
            {
                stats.TimesCorrect++;
            }
            else if (answerType == AnswerType.Incorrect)
            {
                stats.TimesIncorrect++;
            }
        }
    }
} 