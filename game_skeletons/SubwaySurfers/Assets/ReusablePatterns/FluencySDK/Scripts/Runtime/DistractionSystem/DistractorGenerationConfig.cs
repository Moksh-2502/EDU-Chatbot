using UnityEngine;

namespace ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem
{
    /// <summary>
    /// Configuration for context-aware distractor generation
    /// </summary>
    [System.Serializable]
    public class DistractorGenerationConfig
    {
        [Header("Strategy Weights")]
        [Range(0f, 1f)]
        [field: SerializeField]
        public float FactorVariationWeight { get; set; } = 0.4f;

        [Range(0f, 1f)]
        [field: SerializeField]
        public float ArithmeticErrorWeight { get; set; } = 0.3f;

        [Range(0f, 1f)]
        [field: SerializeField]
        public float TableConfusionWeight { get; set; } = 0.2f;

        [Range(0f, 1f)]
        [field: SerializeField]
        public float FallbackRandomWeight { get; set; } = 0.1f;

        [Header("Strategy Settings")]
        [field: SerializeField]
        public bool EnableFactorVariation { get; set; } = true;

        [field: SerializeField]
        public bool EnableArithmeticError { get; set; } = true;

        [field: SerializeField]
        public bool EnableTableConfusion { get; set; } = true;

        [Header("Generation Limits")]
        [field: SerializeField]
        public int MaxDistractorsPerStrategy { get; set; } = 2;

        [field: SerializeField]
        public int MinDistractorValue { get; set; } = 0;

        [field: SerializeField]
        public int MaxDistractorValue { get; set; } = 144; // 12x12

        [Header("Fallback Settings")]
        [field: SerializeField]
        public int FallbackRandomRange { get; set; } = 5;

        /// <summary>
        /// Normalizes the strategy weights to ensure they sum to 1.0
        /// </summary>
        public void NormalizeWeights()
        {
            float totalWeight = FactorVariationWeight + ArithmeticErrorWeight + TableConfusionWeight + FallbackRandomWeight;
            if (totalWeight > 0)
            {
                FactorVariationWeight /= totalWeight;
                ArithmeticErrorWeight /= totalWeight;
                TableConfusionWeight /= totalWeight;
                FallbackRandomWeight /= totalWeight;
            }
        }
    }
} 