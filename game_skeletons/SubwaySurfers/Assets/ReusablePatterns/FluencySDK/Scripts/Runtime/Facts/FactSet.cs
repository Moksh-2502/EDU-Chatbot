using System.Collections.Generic;

namespace FluencySDK
{
    /// <summary>
    /// Represents a set of facts.
    /// </summary>
    public class FactSet
    {
        /// <summary>
        /// Identifier for the fact set (e.g., "0-1", "10").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Read-only list of facts in this set.
        /// </summary>
        public IReadOnlyList<Fact> Facts { get; }

        public FactSet(string id, IReadOnlyList<Fact> facts)
        {
            Id = id;
            Facts = facts;
        }
    }
} 