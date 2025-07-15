using System.Text.RegularExpressions;

public static class StringUtilities
{

    /// <summary>
        /// Extracts multiplication factors from question text using regex pattern matching.
        /// </summary>
        /// <param name="questionText">The question text (e.g., "5 × 8 = ?")</param>
        /// <returns>Array of factors [factorA, factorB] or null if parsing fails</returns>
        public static int[] ExtractFactorsFromQuestionText(string questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText))
            {
                return null;
            }

            // Pattern to match multiplication questions like "5 × 8 = ?" or "5 x 8 = ?"
            // This handles both × (multiplication symbol) and x (letter x)
            var pattern = @"(\d+)\s*[×x]\s*(\d+)\s*=";
            var match = Regex.Match(questionText, pattern);

            if (match.Success && match.Groups.Count >= 3)
            {
                if (int.TryParse(match.Groups[1].Value, out int factorA) &&
                    int.TryParse(match.Groups[2].Value, out int factorB))
                {
                    return new int[] { factorA, factorB };
                }
            }

            return null;
        }
}
