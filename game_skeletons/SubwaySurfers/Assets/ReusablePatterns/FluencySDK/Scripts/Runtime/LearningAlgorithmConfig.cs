using System.Collections.Generic;
using System.Linq;
using ReusablePatterns.FluencySDK.Scripts.Runtime.DistractionSystem;
using UnityEngine;

namespace FluencySDK
{
    [System.Serializable]
    public class BulkPromotionConfig
    {
        public bool Enabled { get; set; } = false;
        public int MinConsecutiveCorrect { get; set; } = 5;
        public float MinFactSetCoveragePercent { get; set; } = 0.8f;
    }

    [System.Serializable]
    public class DifficultyConfig
    {
        public string Name { get; set; } = "Medium";
        public float MinAccuracyThreshold { get; set; } = 0.0f;
        public int MaxFactsBeingLearned { get; set; } = 5;
        public float KnownFactMinRatio { get; set; } = 0.66f;
        public float KnownFactMaxRatio { get; set; } = 0.90f;
        public Dictionary<string, int> PromotionThresholds { get; set; } = new();
        public Dictionary<string, int> DemotionThresholds { get; set; } = new();
        public BulkPromotionConfig BulkPromotion { get; set; } = new();
    }

    [System.Serializable]
    public class DynamicDifficultyConfig
    {
        public int RecentAnswerWindow { get; set; } = 10;
        public int MinAnswersForDifficultyChange { get; set; } = 5;
        public List<DifficultyConfig> Difficulties { get; set; } = new List<DifficultyConfig>
        {
            new DifficultyConfig
            {
                Name = "Hard",
                MinAccuracyThreshold = 0.9f,
                MaxFactsBeingLearned = 100,
                KnownFactMinRatio = 0.30f,
                KnownFactMaxRatio = 0.60f,
                PromotionThresholds = new Dictionary<string, int>
                {
                    { "assessment", 1 },
                    { "grounding", 1 },
                    { "practice-slow", 1 },
                    { "practice-fast", 1 },
                    { "review-1min", 1 },
                    { "review-2min", 1 },
                    { "review-4min", 1 },
                    { "repetition-1day", 1 },
                    { "repetition-2day", 1 },
                    { "repetition-4day", 1 },
                    { "repetition-1week", 1 }
                },
                DemotionThresholds = new Dictionary<string, int>
                {
                    { "assessment", 1 },
                    { "grounding", 1 },
                    { "practice-slow", 1 },
                    { "practice-fast", 1 },
                    { "review-1min", 1 },
                    { "review-2min", 1 },
                    { "review-4min", 1 },
                    { "repetition-1day", 1 },
                    { "repetition-2day", 1 },
                    { "repetition-4day", 1 },
                    { "repetition-1week", 1 }
                },
                BulkPromotion = new BulkPromotionConfig
                {
                    Enabled = true,
                    MinConsecutiveCorrect = 5,
                    MinFactSetCoveragePercent = 0.25f
                }
            },
            new DifficultyConfig
            {
                Name = "Medium",
                MinAccuracyThreshold = 0.7f,
                MaxFactsBeingLearned = 8,
                KnownFactMinRatio = 0.60f,
                KnownFactMaxRatio = 0.80f,
                PromotionThresholds = new Dictionary<string, int>
                {
                    { "assessment", 1 },
                    { "grounding", 2 },
                    { "practice-slow", 1 },
                    { "practice-fast", 2 },
                    { "review-1min", 1 },
                    { "review-2min", 1 },
                    { "review-4min", 1 },
                    { "repetition-1day", 1 },
                    { "repetition-2day", 1 },
                    { "repetition-4day", 1 },
                    { "repetition-1week", 1 }
                },
                DemotionThresholds = new Dictionary<string, int>
                {
                    { "assessment", 1 },
                    { "grounding", 1 },
                    { "practice-slow", 1 },
                    { "practice-fast", 1 },
                    { "review-1min", 2 },
                    { "review-2min", 2 },
                    { "review-4min", 2 },
                    { "repetition-1day", 2 },
                    { "repetition-2day", 2 },
                    { "repetition-4day", 2 },
                    { "repetition-1week", 2 }
                },
                BulkPromotion = new BulkPromotionConfig { Enabled = false }
            },
            new DifficultyConfig
            {
                Name = "Easy",
                MinAccuracyThreshold = 0.0f,
                MaxFactsBeingLearned = 5,
                KnownFactMinRatio = 0.70f,
                KnownFactMaxRatio = 0.90f,
                PromotionThresholds = new Dictionary<string, int>
                {
                    { "assessment", 2 },
                    { "grounding", 2 },
                    { "practice-slow", 2 },
                    { "practice-fast", 2 },
                    { "review-1min", 1 },
                    { "review-2min", 1 },
                    { "review-4min", 1 },
                    { "repetition-1day", 1 },
                    { "repetition-2day", 1 },
                    { "repetition-4day", 1 },
                    { "repetition-1week", 1 }
                },
                DemotionThresholds = new Dictionary<string, int>
                {
                    { "assessment", 2 },
                    { "grounding", 2 },
                    { "practice-slow", 2 },
                    { "practice-fast", 2 },
                    { "review-1min", 2 },
                    { "review-2min", 2 },
                    { "review-4min", 2 },
                    { "repetition-1day", 2 },
                    { "repetition-2day", 2 },
                    { "repetition-4day", 2 },
                    { "repetition-1week", 2 }
                },
                BulkPromotion = new BulkPromotionConfig { Enabled = false }
            }
        };
    }

    [System.Serializable]
    public class LearningAlgorithmConfig
    {
        public enum Mode
        {
            Normal,
            SpeedRun
        }

        /// <summary>
        /// Creates the default stage configuration
        /// </summary>
        public static List<LearningStage> CreateDefaultStages()
        {
            return new List<LearningStage>
            {
                new GroundingStage
                {
                    Id = "grounding",
                    Icon = "🎯",
                    Order = 0,
                    ProgressWeight = 0.1f
                },
                new AssessmentStage
                {
                    Id = "assessment",
                    Icon = "📊",
                    Order = 1,
                    ProgressWeight = 0.2f,
                    TimerSeconds = DefaultAssessmentTimer
                },
                new PracticeStage
                {
                    Id = "practice-slow",
                    DisplayName = "Practice (Slow)",
                    Icon = "⚡",
                    Order = 2,
                    TimerSeconds = DefaultFluencyBigTimer,
                    ProgressWeight = 0.3f
                },
                new PracticeStage
                {
                    Id = "practice-fast",
                    DisplayName = "Practice (Fast)",
                    Icon = "🚀",
                    Order = 3,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.4f
                },
                new ReviewStage
                {
                    Id = "review-1min",
                    DisplayName = "Review (1 min)",
                    Icon = "🔄",
                    Order = 4,
                    DelayMinutes = 1,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.5f
                },
                new ReviewStage
                {
                    Id = "review-2min",
                    DisplayName = "Review (2 min)",
                    Icon = "🔄",
                    Order = 5,
                    DelayMinutes = 2,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.6f
                },
                new ReviewStage
                {
                    Id = "review-4min",
                    DisplayName = "Review (4 min)",
                    Icon = "🔄",
                    Order = 6,
                    DelayMinutes = 4,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.7f
                },
                new RepetitionStage
                {
                    Id = "repetition-1day",
                    DisplayName = "Repetition (1 day)",
                    Icon = "⏪",
                    Order = 7,
                    DelayDays = 1,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.8f,
                },
                new RepetitionStage
                {
                    Id = "repetition-2day",
                    DisplayName = "Repetition (2 day)",
                    Icon = "⏪",
                    Order = 8,
                    DelayDays = 2,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.85f,
                },
                new RepetitionStage
                {
                    Id = "repetition-4day",
                    DisplayName = "Repetition (4 day)",
                    Icon = "⏪",
                    Order = 9,
                    DelayDays = 4,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.9f,
                },
                new RepetitionStage
                {
                    Id = "repetition-1week",
                    DisplayName = "Repetition (1 week)",
                    Icon = "⏪",
                    Order = 10,
                    DelayDays = 7,
                    TimerSeconds = DefaultFluencySmallTimer,
                    ProgressWeight = 0.95f,
                },
                new MasteredStage
                {
                    Id = "mastered",
                    Icon = "✅",
                    Order = 11,
                    ProgressWeight = 1.0f,
                }
            };
        }

        // Canonical fact set order for multiplication facts
        public static readonly string[] DefaultFactSetOrder = { "0-1", "10", "5", "2", "4", "8", "9", "3", "6", "7" };

        public List<LearningStage> Stages { get; set; } = CreateDefaultStages();

        public static readonly float DefaultMinQuestionInterval = 2.0f; // Minimum time between questions (seconds)
        public static readonly float DefaultMaxQuestionInterval = 8.0f; // Maximum time between questions (seconds)

        public static readonly float
            DefaultTimeToNextQuestion = 2.0f; // Default time to next question after submitting an answer (seconds)

        public static readonly float DefaultMaxFluencyTimer = 12.0f; // Maximum timer for fluency mode (seconds)
        public static readonly float DefaultMinFluencyTimer = 2.5f; // Minimum timer for fluency mode (seconds)

        public static readonly int
            DefaultMaxMultiplicationFactor = 10; // Maximum factor for multiplication facts (0 to this value)

        public const float DefaultAssessmentTimer = 5.0f; 
        public const float DefaultFluencyBigTimer = 4.0f;
        public const float DefaultFluencySmallTimer = 2.0f;

        public static readonly bool DefaultAlwaysStartFresh = false; // Default behavior is to load from storage

        public static readonly float DefaultMinQuestionIntervalSeconds = 30.0f; // Time filter for fact selection

        public static readonly int
            DefaultIndividualPromotionThreshold = 1; // N consecutive correct for individual promotion

        public static readonly int DefaultPracticePromotionThreshold = 2;
        public static readonly int DefaultDemotionThreshold = 2;
        public static readonly int[] DefaultReviewDelaysMinutes = { 1, 2 };
        public static readonly int[] DefaultRepetitionDelaysDays = { 1, 2, 4, 7 };
        public static readonly int DefaultMaxUnknownFacts = 5;
        public static readonly float DefaultUnknownFactWindowSeconds = 20.0f;

        public static readonly int DefaultRecentQuestionHistorySize = 5;
        public static readonly bool DefaultDisableRandomization = false;

        // Fact set configuration
        [field: SerializeField]
        public string[] FactSetOrder
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultFactSetOrder;

        // Question timing settings
        [field: SerializeField]
        public float MinQuestionInterval
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMinQuestionInterval;

        [field: SerializeField]
        public float MaxQuestionInterval
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMaxQuestionInterval;

        [field: SerializeField]
        public float TimeToNextQuestion
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultTimeToNextQuestion;

        [field: SerializeField]
        public float MaxFluencyTimer
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMaxFluencyTimer;

        [field: SerializeField]
        public float MinFluencyTimer
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMinFluencyTimer;

        [field: SerializeField]
        public int MaxMultiplicationFactor
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMaxMultiplicationFactor;

        [field: SerializeField]
        public float FluencyBigTimer
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultFluencyBigTimer;

        [field: SerializeField]
        public float FluencySmallTimer
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultFluencySmallTimer;

        [field: SerializeField]
        public float AssessmentTimer
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultAssessmentTimer;

        [field: SerializeField]
        public float MinQuestionIntervalSeconds
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMinQuestionIntervalSeconds;

        [field: SerializeField]
        public int IndividualPromotionThreshold
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultIndividualPromotionThreshold;

        /// <summary>
        /// If true, always start with a new state instead of loading from storage
        /// </summary>
        [field: SerializeField]
        public bool AlwaysStartFresh
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultAlwaysStartFresh;

        // Learning Algorithm V3 Properties
        [field: SerializeField]
        public int PracticePromotionThreshold
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultPracticePromotionThreshold;

        [field: SerializeField]
        public int DemotionThreshold
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultDemotionThreshold;

        [field: SerializeField]
        public int[] ReviewDelaysMinutes
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultReviewDelaysMinutes;

        [field: SerializeField]
        public int[] RepetitionDelaysDays
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultRepetitionDelaysDays;

        [field: SerializeField]
        public int MaxUnknownFacts
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultMaxUnknownFacts;

        [field: SerializeField]
        public float UnknownFactWindowSeconds
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultUnknownFactWindowSeconds;



        [field: SerializeField]
        public int RecentQuestionHistorySize
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultRecentQuestionHistorySize;

        /// <summary>
        /// Disables timing randomization for deterministic behavior (useful for testing)
        /// </summary>
        [field: SerializeField]
        public bool DisableRandomization
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = DefaultDisableRandomization;

        // Dynamic Difficulty Configuration
        [field: SerializeField]
        public DynamicDifficultyConfig DynamicDifficulty
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = new DynamicDifficultyConfig();

        // Distractor Generation Configuration
        [field: SerializeField]
        public DistractorGenerationConfig DistractorConfig
        {
            get;
#if !UNITY_EDITOR
            private
#endif
            set;
        } = new DistractorGenerationConfig();

        /// <summary>
        /// Creates a normal/default configuration
        /// </summary>
        public static LearningAlgorithmConfig CreateNormal()
        {
            return new LearningAlgorithmConfig();
        }

        /// <summary>
        /// Creates a speed run configuration with simplified timing for faster testing
        /// </summary>
        public static LearningAlgorithmConfig CreateSpeedRun()
        {
            var config = new LearningAlgorithmConfig
            {
                FactSetOrder = new[] { "0-1", "5", "2", "4" },
                MaxMultiplicationFactor = 5,
                TimeToNextQuestion = 1,
                MinQuestionIntervalSeconds = 10.0f,
                IndividualPromotionThreshold = 1,
                PracticePromotionThreshold = 1,
                DemotionThreshold = 1,
                MaxUnknownFacts = 3,
                UnknownFactWindowSeconds = 10.0f,
                ReviewDelaysMinutes = new[] { 1, 2 },
                RepetitionDelaysDays = new[] { 1, 2 }
            };

            config.DynamicDifficulty = new DynamicDifficultyConfig
            {
                RecentAnswerWindow = 5,
                MinAnswersForDifficultyChange = 3,
                Difficulties = new List<DifficultyConfig>
                {
                    new DifficultyConfig
                    {
                        Name = "SpeedRun",
                        MinAccuracyThreshold = 0.0f,
                        MaxFactsBeingLearned = 2,
                        KnownFactMinRatio = 0.60f,
                        KnownFactMaxRatio = 0.85f,
                        PromotionThresholds = new Dictionary<string, int>
                        {
                            { "assessment", 1 },
                            { "grounding", 1 },
                            { "practice-slow", 1 },
                            { "practice-fast", 1 },
                            { "review-1min", 1 },
                            { "review-2min", 1 },
                            { "repetition-1day", 1 },
                            { "repetition-2day", 1 }
                        },
                        DemotionThresholds = new Dictionary<string, int>
                        {
                            { "assessment", 1 },
                            { "grounding", 1 },
                            { "practice-slow", 0 },
                            { "practice-fast", 1 },
                            { "review-1min", 0 },
                            { "review-2min", 0 },
                            { "repetition-1day", 1 },
                            { "repetition-2day", 1 }
                        },
                        BulkPromotion = new BulkPromotionConfig { Enabled = false }
                    }
                }
            };

            return config;
        }

        public LearningStage GetStageById(string id)
        {
            return Stages?.FirstOrDefault(s => s.Id == id);
        }

        public LearningStage GetFirstStage()
        {
            return GetStageById("assessment") ?? Stages?.OrderBy(s => s.Order).FirstOrDefault();
        }

    }
}