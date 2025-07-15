using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluencySDK
{
    /// <summary>
    /// Responsible for building fact sets according to the canonical order.
    /// </summary>
    public class FactSetBuilder
    {
        private readonly HashSet<string> _processedFactIds = new HashSet<string>();
        private readonly LearningAlgorithmConfig _config;

        public FactSetBuilder(LearningAlgorithmConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Builds all fact sets according to the canonical order.
        /// </summary>
        public Dictionary<string, FactSet> BuildAllFactSets()
        {
            var factSetsById = new Dictionary<string, FactSet>();
            _processedFactIds.Clear();
            
            foreach (string factSetId in _config.FactSetOrder)
            {
                var facts = CreateFactsForSet(factSetId);
                factSetsById[factSetId] = new FactSet(factSetId, facts);
            }
            
            return factSetsById;
        }

        /// <summary>
        /// Creates facts for a specific fact set ID.
        /// </summary>
        private List<Fact> CreateFactsForSet(string factSetId)
        {
            var facts = new List<Fact>();
            
            if (factSetId == "0-1")
            {
                for (int i = 0; i <= _config.MaxMultiplicationFactor; i++)
                {
                    AddFactIfNotProcessed(facts, 0, i, factSetId);
                    AddFactIfNotProcessed(facts, 1, i, factSetId);
                }
            }
            else if (int.TryParse(factSetId, out int factor))
            {
                // Facts for a specific factor (e.g., "5" means 5×0 through 5×10)
                for (int i = 0; i <= _config.MaxMultiplicationFactor; i++)
                {
                    AddFactIfNotProcessed(facts, factor, i, factSetId);
                }
            }
            else
            {
                Debug.LogWarning($"Unknown fact set ID: {factSetId}");
            }
            
            return facts;
        }

        /// <summary>
        /// Adds a fact to the list if it hasn't been processed yet.
        /// </summary>
        private void AddFactIfNotProcessed(List<Fact> facts, int factorA, int factorB, string factSetId)
        {
            string factId = $"{factorA}x{factorB}";
            
            if (!_processedFactIds.Contains(factId))
            {
                _processedFactIds.Add(factId);
                facts.Add(new Fact(
                    factId, 
                    factorA, 
                    factorB, 
                    $"{factorA} × {factorB} = ?",
                    factSetId
                ));
            }
        }
    }
} 