namespace FluencySDK
{
    public class FluencyGeneratorConfig
    {
        // Default pedagogical sequence for multiplication facts (Common Core aligned)
        public static readonly int[] DefaultSequence = { 2, 5, 10, 0, 1, 3, 4, 6, 7, 8, 9, 11, 12 };
        public static readonly int DefaultMaxFactor = 12;
        public static readonly int DefaultQuestionsPerBlock = 10;
        // Default spacing intervals for spaced repetition (e.g., 1 day, 3 days, 7 days, 14 days in milliseconds)
        public static readonly long[] DefaultSpacingIntervals = { 86400000L, 259200000L, 604800000L, 1209600000L };
        public static readonly double DefaultRandomizeWindow = 0.2; // 20% randomization

        public int[] Sequence { get; set; } = DefaultSequence;
        public int MaxFactor { get; set; } = DefaultMaxFactor;
        public int QuestionsPerBlock { get; set; } = DefaultQuestionsPerBlock;
        public long[] SpacingIntervals { get; set; } = DefaultSpacingIntervals; // In milliseconds
        public double RandomizeWindow { get; set; } = DefaultRandomizeWindow; // Percentage for randomizing fact selection
    }
} 