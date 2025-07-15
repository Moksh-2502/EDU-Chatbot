using System;
using UnityEngine;

namespace SubwaySurfers.DifficultySystem
{
    /// <summary>
    /// Data structure defining parameters for a specific difficulty level
    /// </summary>
    [Serializable]
    public class DifficultyLevel
    {
        [field: SerializeField] public string Id { get; private set; } = Guid.NewGuid().ToString();
        [Header("Basic Configuration")]
        public int rank = 0;
        public string displayName;               // "Beginner", "Easy", "Normal", "Hard", "Expert"
        
        [Header("Movement Parameters")]
        public Vector2 speedRange;
        public float accelerationRate;           // Speed increase rate
        public int speedStep = 1;                // Speed step for score multiplier calculations
        
        [Header("Obstacle Parameters")]
        public float obstacleDensityMultiplier; // 0.5f to 2.0f
        
        [Header("Collectable Parameters")]
        public CollectableFrequencyConfig collectableConfig;

        // ─── New fields for animation‐speed ratios ──────────────────────────
        [Header("Animation Speed Ratios")]
        [Tooltip("Multiplier for jump animation speed (base=0.6 at Difficulty 0)")]
        public float jumpAnimSpeedRatio = 0.6f;

        [Tooltip("Multiplier for slide animation speed (base=0.9 at Difficulty 0)")]
        public float slideAnimSpeedRatio = 0.9f;
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a default difficulty level configuration
        /// </summary>
        public static DifficultyLevel CreateDefault(int index)
        {
            var level = new DifficultyLevel
            {
                displayName = GetDefaultDisplayName(index),
                collectableConfig = CollectableFrequencyConfig.CreateDefault(index)
            };

            // Assign default speed‐range and acceleration
            switch (index)
            {
                case 0:
                    level.speedRange = new Vector2(8f, 12f);
                    level.accelerationRate = 0.1f;
                    level.jumpAnimSpeedRatio = 0.60f;
                    level.slideAnimSpeedRatio = 0.90f;
                    break;
                case 1:
                    level.speedRange = new Vector2(9f, 14f);
                    level.accelerationRate = 0.11f;
                    level.jumpAnimSpeedRatio = 0.63f;
                    level.slideAnimSpeedRatio = 0.93f;
                    break;
                case 2:
                    level.speedRange = new Vector2(10f, 16f);
                    level.accelerationRate = 0.12f;
                    level.jumpAnimSpeedRatio = 0.66f;
                    level.slideAnimSpeedRatio = 0.96f;
                    break;
                case 3:
                    level.speedRange = new Vector2(11f, 18f);
                    level.accelerationRate = 0.13f;
                    level.jumpAnimSpeedRatio = 0.69f;
                    level.slideAnimSpeedRatio = 0.98f;
                    break;
                case 4:
                    level.speedRange = new Vector2(12f, 20f);
                    level.accelerationRate = 0.15f;
                    level.jumpAnimSpeedRatio = 0.72f;
                    level.slideAnimSpeedRatio = 1.00f;
                    break;
                default:
                    level.speedRange = new Vector2(8f, 12f);
                    level.accelerationRate = 0.1f;
                    level.jumpAnimSpeedRatio = 0.60f;
                    level.slideAnimSpeedRatio = 0.90f;
                    break;
            }

            level.speedStep = 1;
            return level;
        }

        private static string GetDefaultDisplayName(int index)
        {
            return index switch
            {
                0 => "Accessible",
                1 => "Beginner", 
                2 => "Easy",
                3 => "Normal",
                4 => "Expert",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Configuration for collectable spawn frequencies relative to base frequency
    /// </summary>
    [Serializable]
    public class CollectableFrequencyConfig
    {
        [Header("Frequency Multipliers (relative to base)")]
        public float baseFrequency = 1.0f;               // Score multiplier frequency (base)
        public float magnetFrequency = 0.5f;             // 2x less frequent
        public float shieldFrequency = 0.2f;             // 5x less frequent  
        public float invincibilityFrequency = 0.1f;      // 10x less frequent
        public float extraLifeFrequency = 0.05f;         // 20x less frequent

        /// <summary>
        /// Creates default collectable frequency configuration for a difficulty level
        /// </summary>
        public static CollectableFrequencyConfig CreateDefault(int difficultyIndex)
        {
            // Higher difficulty = lower collectable frequency
            float difficultyMultiplier = difficultyIndex switch
            {
                0 => 1.5f,   // More collectables for accessibility
                1 => 1.2f,   // Slightly more collectables
                2 => 1.0f,   // Standard frequency
                3 => 0.8f,   // Slightly fewer collectables
                4 => 0.6f,   // Fewer collectables for challenge
                _ => 1.0f
            };

            return new CollectableFrequencyConfig
            {
                baseFrequency = 1.0f * difficultyMultiplier,
                magnetFrequency = 0.5f * difficultyMultiplier,
                shieldFrequency = 0.2f * difficultyMultiplier,
                invincibilityFrequency = 0.1f * difficultyMultiplier,
                extraLifeFrequency = 0.05f * difficultyMultiplier
            };
        }

        /// <summary>
        /// Gets the total weighted frequency for random selection
        /// </summary>
        public float GetTotalWeight()
        {
            return baseFrequency + magnetFrequency + shieldFrequency + 
                   invincibilityFrequency + extraLifeFrequency;
        }
    }
} 