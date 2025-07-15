namespace FluencySDK
{
    /// <summary>
    /// Immutable definition of a math fact.
    /// Generated once at startup.
    /// </summary>
    public class Fact
    {
        /// <summary>
        /// Canonical key for the fact (e.g., "5x8").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The first factor in the multiplication.
        /// </summary>
        public int FactorA { get; }

        /// <summary>
        /// The second factor in the multiplication.
        /// </summary>
        public int FactorB { get; }

        /// <summary>
        /// Text representation of the question (e.g., "5 × 8 = ?").
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Identifier for the fact set this fact belongs to (e.g., "0-1", "5").
        /// </summary>
        public string FactSetId { get; }

        public Fact(string id, int factorA, int factorB, string text, string factSetId)
        {
            Id = id;
            FactorA = factorA;
            FactorB = factorB;
            Text = text;
            FactSetId = factSetId;
        }
    }
} 